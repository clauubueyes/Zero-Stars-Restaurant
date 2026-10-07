using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FoodQualityConsequenceTests
    {
        private static readonly DishProfile Hamburger = new DishProfile("hamburger", "Hamburger", new[] { "bun", "patty", "bun" });
        private static FoodState Food(string id = "patty", double dose = 45, double freshness = 100,
            bool contaminated = false, FoodCategory category = FoodCategory.Meat, bool cookable = true)
        {
            var food = new FoodState(new FoodProfile(id, id, category, 25, 600, 60,
                cooking: cookable ? new CookingProfile(60, 120, 45, 65, 90) : null),
                freshnessPercent: freshness, isContaminated: contaminated, temperatureCelsius: 120);
            food.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
            return food;
        }
        private static OrderState Order(int price = 500) => new OrderState(new OrderOffer(Hamburger, price, new[] { 3, 4, 3 }));
        private static CustomerVisit Visit(OrderState order, Guid? customer = null)
        {
            var visit = customer.HasValue ? new CustomerVisit(order, customer.Value) : new CustomerVisit(order);
            Assert.That(visit.Arrive() && visit.BeginWaiting() && visit.Receive() && visit.BeginEvaluation(), Is.True);
            return visit;
        }
        private static CustomerVisit Serve(FoodState food, PaymentLedger ledger = null)
        {
            var order = Order(); var visit = Visit(order);
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { food }), ledger ?? new PaymentLedger(), out _), Is.True);
            Assert.That(visit.Resolve(), Is.True); return visit;
        }

        [TestCase(0, FoodQualityIssue.Raw, CustomerReaction.HealthIncident)]
        [TestCase(20, FoodQualityIssue.Undercooked, CustomerReaction.HealthIncident)]
        [TestCase(65, FoodQualityIssue.Overcooked, CustomerReaction.Unhappy)]
        [TestCase(90, FoodQualityIssue.Burnt, CustomerReaction.Complaint)]
        public void EachCookingProblemRetainsItsExactCauseAndOriginalEvidence(double dose, FoodQualityIssue issue, CustomerReaction reaction)
        {
            var food = Food(dose: dose); var visit = Serve(food); var consequence = visit.Consequence;
            Assert.That(consequence.Reaction, Is.EqualTo(reaction));
            Assert.That(consequence.Quality.Issues, Is.EqualTo(new[] { issue }));
            Assert.That(consequence.Quality.Causes.Single().Ingredient.InstanceId, Is.EqualTo(food.InstanceId));
            Assert.That(consequence.Quality.Causes.Single().Ingredient, Is.SameAs(visit.Order.Result.Evaluation.DeliveredDish.Ingredients.Single()));
            Assert.That(consequence.CustomerId, Is.EqualTo(visit.InstanceId)); Assert.That(consequence.OrderId, Is.EqualTo(visit.Order.InstanceId));
            Assert.That(visit.Order.Result.PaymentCents, Is.EqualTo(200));
        }

        [TestCase(45)] [TestCase(64)]
        public void FreshCookedMeatIsGoodAndSatisfied(double dose)
        {
            var consequence = Serve(Food(dose: dose)).Consequence;
            Assert.That(consequence.Quality.IsGood, Is.True); Assert.That(consequence.Quality.Causes, Is.Empty);
            Assert.That(consequence.Reaction, Is.EqualTo(CustomerReaction.Satisfied));
            Assert.That(consequence.Quality.Delivery.Ingredients.Single().CookingStage, Is.EqualTo(CookingStage.Cooked));
        }

        [TestCase(80, null, CustomerReaction.Satisfied)]
        [TestCase(79, FoodQualityIssue.Stale, CustomerReaction.Unhappy)]
        [TestCase(31, FoodQualityIssue.Stale, CustomerReaction.Unhappy)]
        [TestCase(30, FoodQualityIssue.Spoiled, CustomerReaction.HealthIncident)]
        [TestCase(6, FoodQualityIssue.Spoiled, CustomerReaction.HealthIncident)]
        [TestCase(5, FoodQualityIssue.Rotten, CustomerReaction.HealthIncident)]
        [TestCase(0, FoodQualityIssue.Rotten, CustomerReaction.HealthIncident)]
        public void FreshnessUsesTheExistingDefinitionThresholds(double freshness, FoodQualityIssue? issue, CustomerReaction reaction)
        {
            var consequence = Serve(Food(freshness: freshness)).Consequence;
            Assert.That(consequence.Reaction, Is.EqualTo(reaction));
            Assert.That(consequence.Quality.Issues, Is.EqualTo(issue.HasValue ? new[] { issue.Value } : Array.Empty<FoodQualityIssue>()));
            Assert.That(consequence.Quality.Delivery.Ingredients.Single().FreshnessPercent, Is.EqualTo(freshness).Within(.000001));
        }

        [Test]
        public void ContaminationIsAnIndependentHazardEvenWhenCooked()
        {
            var consequence = Serve(Food(contaminated: true)).Consequence;
            Assert.That(consequence.Quality.Issues, Is.EqualTo(new[] { FoodQualityIssue.Contaminated }));
            Assert.That(consequence.Quality.Causes.Single().IsHealthHazard, Is.True);
            Assert.That(consequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
        }

        [TestCase(0, true)] [TestCase(20, true)] [TestCase(0, false)]
        public void BakeryDoesNotBecomeDangerousMerelyBecauseItIsUncooked(double dose, bool cookable)
        {
            var consequence = Serve(Food("bun", dose, category: FoodCategory.Bakery, cookable: cookable)).Consequence;
            Assert.That(consequence.Reaction, Is.EqualTo(cookable ? CustomerReaction.Unhappy : CustomerReaction.Satisfied));
            Assert.That(consequence.Quality.Causes.All(c => !c.IsHealthHazard), Is.True);
        }

        [Test]
        public void AllSimultaneousProblemsAndDistinctUnitsSurviveWithOneHealthReaction()
        {
            var patty = Food(dose: 90, freshness: 0, contaminated: true);
            var bun = Food("bun", freshness: 20, contaminated: true, cookable: false);
            var order = Order(); var visit = Visit(order); var ledger = new PaymentLedger();
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { patty, bun }), ledger, out _), Is.True);
            visit.Resolve(); var quality = visit.Consequence.Quality;
            Assert.That(quality.Issues, Is.EquivalentTo(new[] { FoodQualityIssue.Rotten, FoodQualityIssue.Contaminated, FoodQualityIssue.Burnt, FoodQualityIssue.Spoiled }));
            Assert.That(quality.Causes.Count, Is.EqualTo(5));
            Assert.That(quality.Causes.Count(c => c.Issue == FoodQualityIssue.Contaminated), Is.EqualTo(2));
            Assert.That(visit.Consequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
            Assert.That(ledger.BalanceCents, Is.EqualTo(350));
            Assert.That(quality.Delivery, Is.SameAs(order.Result.Evaluation.DeliveredDish));
        }

        [TestCase("bun,patty,bun", true, 500)]
        [TestCase("bun,patty", true, 350)]
        [TestCase("patty", false, 200)]
        [TestCase("bun", false, 150)]
        public void QualityDoesNotDependOnCompletenessOrFinalization(string composition, bool finalized, int payment)
        {
            var foods = composition.Split(',').Select(id => Food(id, dose: id == "patty" ? 90 : 0, cookable: id == "patty")).ToArray();
            var order = Order(); var visit = Visit(order); var ledger = new PaymentLedger();
            DeliveryContents delivery;
            if (finalized)
            {
                var dish = new DishState(); foreach (var food in foods) dish.TryAdd(food);
                dish.TrySetOrder(foods.Select(f => f.InstanceId).ToArray(), true); dish.TryFinalize(new[] { Hamburger });
                delivery = new DeliveryContents(dish);
            }
            else delivery = new DeliveryContents(foods);
            Assert.That(OrderDelivery.TryComplete(order, delivery, ledger, out var result), Is.True); visit.Resolve();
            Assert.That(result.PaymentCents, Is.EqualTo(payment)); Assert.That(ledger.Transactions.Single().AmountCents, Is.EqualTo(payment));
            Assert.That(visit.Consequence.Reaction, Is.EqualTo(composition.Contains("patty") ? CustomerReaction.Complaint : CustomerReaction.Satisfied));
        }

        [Test]
        public void DangerousExtraIsEvaluatedAlthoughItAddsNoPayment()
        {
            var order = Order(); var visit = Visit(order);
            var foods = new[] { Food("bun", cookable: false), Food(), Food("bun", cookable: false), Food("cheese", contaminated: true, cookable: false) };
            OrderDelivery.TryComplete(order, new DeliveryContents(foods), new PaymentLedger(), out var result); visit.Resolve();
            Assert.That(result.PaymentCents, Is.EqualTo(500)); Assert.That(result.Evaluation.Satisfaction.Extra, Is.EqualTo(new[] { "cheese" }));
            Assert.That(visit.Consequence.Quality.Causes.Single().Ingredient.InstanceId, Is.EqualTo(foods[3].InstanceId));
            Assert.That(visit.Consequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
        }

        [Test]
        public void ReactionAndIncidentAreRecordedOnceAndRetainedAfterExit()
        {
            var ledger = new PaymentLedger(); var visit = Serve(Food(contaminated: true), ledger); var consequence = visit.Consequence;
            var statistics = new CustomerServiceStatistics(); Assert.That(statistics.TryRecord(consequence), Is.True);
            Assert.That(visit.Resolve(), Is.False); Assert.That(statistics.TryRecord(consequence), Is.False);
            Assert.That(visit.BeginLeaving() && visit.Finish(), Is.True); Assert.That(visit.Consequence, Is.SameAs(consequence));
            Assert.That(statistics.TryRecord(consequence), Is.False); Assert.That(statistics.TryRecord(null), Is.False);
            Assert.That(statistics.CustomersServed, Is.EqualTo(1)); Assert.That(statistics.HealthIncidents, Is.EqualTo(1));
            Assert.That(statistics.HealthIncidentHistory.Single(), Is.SameAs(consequence));
            Assert.That(ledger.Transactions.Count, Is.EqualTo(1)); Assert.That(ledger.BalanceCents, Is.EqualTo(200));
        }

        [Test]
        public void StatisticsAccumulateExclusiveReactionsAcrossVisitsAndRejectDuplicateIdentities()
        {
            var statistics = new CustomerServiceStatistics();
            var firstOrder = Order(); var firstVisit = Visit(firstOrder); var duplicateOrder = Visit(firstOrder);
            OrderDelivery.TryComplete(firstOrder, new DeliveryContents(new[] { Food() }), new PaymentLedger(), out _);
            firstVisit.Resolve(); duplicateOrder.Resolve();
            var visits = new[] { firstVisit, Serve(Food(dose: 65)), Serve(Food(dose: 90)), Serve(Food(dose: 20, contaminated: true)) };
            foreach (var visit in visits) Assert.That(statistics.TryRecord(visit.Consequence), Is.True);
            Assert.That(statistics.CustomersServed, Is.EqualTo(4)); Assert.That(statistics.Satisfied, Is.EqualTo(1));
            Assert.That(statistics.Unhappy, Is.EqualTo(1)); Assert.That(statistics.Complaints, Is.EqualTo(1)); Assert.That(statistics.HealthIncidents, Is.EqualTo(1));
            Assert.That(statistics.History.Select(c => c.CustomerId).Distinct().Count(), Is.EqualTo(4));
            Assert.That(statistics.TryRecord(duplicateOrder.Consequence), Is.False);
            var anotherOrder = Order(); var duplicateCustomer = Visit(anotherOrder, visits[0].InstanceId);
            OrderDelivery.TryComplete(anotherOrder, new DeliveryContents(new[] { Food() }), new PaymentLedger(), out _); duplicateCustomer.Resolve();
            Assert.That(statistics.TryRecord(duplicateCustomer.Consequence), Is.False); Assert.That(statistics.CustomersServed, Is.EqualTo(4));
        }

        [Test]
        public void RejectedOrIncompleteTransactionsDoNotProduceAReaction()
        {
            var order = Order(); var visit = Visit(order); Assert.That(visit.Resolve(), Is.False); Assert.That(visit.Consequence, Is.Null);
            OrderDelivery.TryComplete(order, new DeliveryContents(new[] { Food("cheese", contaminated: true) }), new PaymentLedger(), out _);
            Assert.That(visit.Resolve(), Is.True); Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Reject)); Assert.That(visit.Consequence, Is.Null);
        }

        [Test]
        public void ReceiptIsStableDespiteLaterCookingDeteriorationAndDisposal()
        {
            var food = Food(); food.SetTemperature(4); var visit = Serve(food); var quality = visit.Consequence.Quality;
            food.Contaminate(); food.SetTemperature(120); food.Advance(900, new ThermalEnvironment(120, allowsCooking: true));
            Assert.That(food.Condition, Is.EqualTo(FoodCondition.Rotten)); Assert.That(food.Cooking.Stage, Is.EqualTo(CookingStage.Burnt));
            Assert.That(quality.IsGood, Is.True); Assert.That(visit.Consequence.Reaction, Is.EqualTo(CustomerReaction.Satisfied));
            var evidence = quality.Delivery.Ingredients.Single(); Assert.That(evidence.TemperatureCelsius, Is.EqualTo(4));
            Assert.That(evidence.IsContaminated, Is.False); Assert.That(evidence.Condition, Is.EqualTo(FoodCondition.Fresh));
            Assert.That(evidence.CookingStage, Is.EqualTo(CookingStage.Cooked));
        }

        [Test]
        public void RelevantZeroCentDeliveryStillCountsAsServedAndSafetyDoesNotChangeThePrice()
        {
            var order = Order(1); var visit = Visit(order); var ledger = new PaymentLedger();
            OrderDelivery.TryComplete(order, new DeliveryContents(new[] { Food(dose: 0) }), ledger, out var result); visit.Resolve();
            Assert.That(result.Accepted, Is.True); Assert.That(result.PaymentCents, Is.Zero);
            var statistics = new CustomerServiceStatistics(); statistics.TryRecord(visit.Consequence);
            Assert.That(statistics.CustomersServed, Is.EqualTo(1)); Assert.That(statistics.HealthIncidents, Is.EqualTo(1));
        }
    }
}
