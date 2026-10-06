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
    public sealed class OrderDeliveryTests
    {
        private static DishProfile Hamburger => new DishProfile("dish.hamburger", "Hamburger", new[] { "bun", "patty", "bun" });
        private static DishProfile Cheeseburger => new DishProfile("dish.cheeseburger", "Cheeseburger", new[] { "bun", "patty", "cheese", "bun" });
        private static FoodState Food(string id, double freshness = 100, bool contaminated = false)
            => new FoodState(new FoodProfile(id, id, FoodCategory.Meat, id == "patty" ? 80 : id == "cheese" ? 25 : 35,
                600, 60, cooking: id == "patty" ? new CookingProfile(60, 120, 45, 65, 90) : null),
                freshnessPercent: freshness, isContaminated: contaminated, temperatureCelsius: 120);
        private static DishState Dish(bool cheese = false, double freshness = 100, bool contaminated = false, double dose = 45)
        {
            FoodState patty = Food("patty", freshness, contaminated);
            patty.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
            var components = cheese ? new[] { Food("bun"), patty, Food("cheese"), Food("bun") } : new[] { Food("bun"), patty, Food("bun") };
            var dish = new DishState(); foreach (FoodState food in components) dish.TryAdd(food);
            dish.TrySetOrder(components.Select(food => food.InstanceId).ToArray(), true);
            Assert.That(dish.TryFinalize(new[] { Hamburger, Cheeseburger }), Is.True); return dish;
        }
        private static OrderState Order(bool cheese = false, Guid? id = null) => new OrderState(new OrderOffer(cheese ? Cheeseburger : Hamburger, cheese ? 650 : 500), id);

        [Test]
        public void OrdersHaveIndependentIdentityImmutableTermsAndValidatedConfiguration()
        {
            var offer = new OrderOffer(Hamburger, 500); var a = new OrderState(offer); var b = new OrderState(offer);
            Assert.That(a.InstanceId, Is.Not.EqualTo(b.InstanceId)); Assert.That(a.Offer, Is.SameAs(b.Offer));
            Assert.That(a.IsCompleted, Is.False); Assert.That(a.Offer.SalePriceCents, Is.EqualTo(500));
            Assert.Throws<ArgumentException>(() => new OrderState(offer, Guid.Empty));
            Assert.Throws<ArgumentNullException>(() => new OrderState(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OrderOffer(Hamburger, 0));
            Assert.Throws<ArgumentNullException>(() => new OrderOffer(null, 500));
        }

        [TestCase(false, 500)]
        [TestCase(true, 650)]
        public void CorrectKnownDishPaysConfiguredPriceAndMarksExactlyThatDishSold(bool cheese, int price)
        {
            var ledger = new PaymentLedger(); var order = Order(cheese); DishState dish = Dish(cheese);
            Assert.That(ledger.BalanceCents, Is.Zero);
            Assert.That(OrderDelivery.TryComplete(order, dish, ledger, out OrderResult result), Is.True);
            Assert.That(result.Accepted, Is.True); Assert.That(result.Evaluation.CorrectOrder, Is.True);
            Assert.That(result.PaymentCents, Is.EqualTo(price)); Assert.That(ledger.BalanceCents, Is.EqualTo(price));
            Assert.That(result.Evaluation.DeliveredDish.InstanceId, Is.EqualTo(dish.InstanceId));
            Assert.That(result.Evaluation.DeliveredDish.Ingredients.Select(food => food.InstanceId),
                Is.EqualTo(dish.Components.Select(food => food.InstanceId)));
            Assert.That(dish.IsSold, Is.True); Assert.That(order.IsCompleted, Is.True);
        }

        [Test]
        public void WrongKnownDishRejectsWithoutSellingOrChangingBalanceAndMayBeOfferedToANewOrder()
        {
            var ledger = new PaymentLedger(); DishState hamburger = Dish(); var order = Order(true);
            Assert.That(OrderDelivery.TryComplete(order, hamburger, ledger, out OrderResult result), Is.True);
            Assert.That(result.Evaluation.CorrectOrder, Is.False); Assert.That(result.Accepted, Is.False);
            Assert.That(result.PaymentCents, Is.Zero); Assert.That(ledger.BalanceCents, Is.Zero);
            Assert.That(hamburger.IsSold, Is.False); Assert.That(hamburger.IsDisposed, Is.False);
            Assert.That(OrderDelivery.TryComplete(Order(), hamburger, ledger, out _), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void CustomDishIsValidCompositionButDoesNotSatisfyAKnownOrder()
        {
            var dish = new DishState(); dish.TryAdd(Food("cheese")); dish.TryFinalize(new[] { Hamburger, Cheeseburger });
            Assert.That(OrderDelivery.TryComplete(Order(), dish, new PaymentLedger(), out OrderResult result), Is.True);
            Assert.That(result.Evaluation.DeliveredDish.DefinitionId, Is.Null);
            Assert.That(result.Evaluation.DeliveredDish.DisplayName, Is.EqualTo("Custom Dish"));
            Assert.That(result.Evaluation.CorrectOrder, Is.False); Assert.That(result.PaymentCents, Is.Zero);
        }

        [TestCase(100, false, 0, CookingStage.Raw, false)]
        [TestCase(100, false, 90, CookingStage.Burnt, false)]
        [TestCase(0, false, 45, CookingStage.Cooked, true)]
        [TestCase(20, false, 45, CookingStage.Cooked, true)]
        [TestCase(74, true, 45, CookingStage.Cooked, false)]
        public void CorrectnessAndPaymentStayIndependentOfFoodSafety(double freshness, bool contaminated, double dose, CookingStage stage, bool spoiled)
        {
            DishState dish = Dish(freshness: freshness, contaminated: contaminated, dose: dose);
            var ledger = new PaymentLedger();
            Assert.That(OrderDelivery.TryComplete(Order(), dish, ledger, out OrderResult result), Is.True);
            DishSnapshot snapshot = result.Evaluation.DeliveredDish;
            Assert.That(result.Evaluation.CorrectOrder, Is.True); Assert.That(result.PaymentCents, Is.EqualTo(500));
            Assert.That(snapshot.Ingredients[1].CookingStage, Is.EqualTo(stage));
            Assert.That(snapshot.ContainsSpoiledOrRotten, Is.EqualTo(spoiled)); Assert.That(snapshot.ContainsContamination, Is.EqualTo(contaminated));
            Assert.That(snapshot.ContainsRotten, Is.EqualTo(snapshot.Ingredients.Any(food => food.Condition == FoodCondition.Rotten)));
            Assert.That(snapshot.ContainsSpoiled, Is.EqualTo(snapshot.Ingredients.Any(food => food.Condition == FoodCondition.Spoiled)));
            Assert.That(snapshot.MinimumFreshnessPercent, Is.EqualTo(freshness));
            Assert.That(snapshot.MeanFreshnessPercent, Is.EqualTo((200 + freshness) / 3).Within(1e-9));
            Assert.That(snapshot.TotalIngredientCostCents, Is.EqualTo(150));
        }

        [Test]
        public void ReceiptIsHistoricalEvidenceAfterLiveFoodChangesAndDishDisposal()
        {
            DishState dish = Dish(true, freshness: 74); Guid[] ids = dish.Components.Select(food => food.InstanceId).ToArray();
            Assert.That(ids[0], Is.Not.EqualTo(ids[3])); // Same Bun definition, distinct units.
            Assert.That(OrderDelivery.TryComplete(Order(true), dish, new PaymentLedger(), out OrderResult result), Is.True);
            foreach (FoodState food in dish.Components) { food.Advance(600, 21); food.Contaminate(); }
            dish.Dispose(); DishSnapshot snapshot = result.Evaluation.DeliveredDish;
            Assert.That(snapshot.Ingredients.Select(food => food.InstanceId), Is.EqualTo(ids));
            Assert.That(snapshot.ContainsContamination, Is.False); Assert.That(snapshot.ContainsSpoiledOrRotten, Is.False);
            Assert.That(snapshot.MinimumFreshnessPercent, Is.EqualTo(74)); Assert.That(snapshot.TotalIngredientCostCents, Is.EqualTo(175));
            Assert.That(snapshot.Ingredients[1].CookingStage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(snapshot.Ingredients[1].TemperatureCelsius, Is.EqualTo(120));
        }

        [Test]
        public void SameOrderCannotBeProcessedOrPaidTwiceEvenViaAnotherObjectWithTheSameId()
        {
            var ledger = new PaymentLedger(); OrderState order = Order(); DishState dish = Dish();
            Assert.That(OrderDelivery.TryComplete(order, dish, ledger, out OrderResult original), Is.True);
            Assert.That(OrderDelivery.TryComplete(order, Dish(), ledger, out _), Is.False);
            OrderState duplicate = Order(id: order.InstanceId); DishState other = Dish();
            Assert.That(OrderDelivery.TryComplete(duplicate, other, ledger, out _), Is.False);
            Assert.That(other.IsSold, Is.False); Assert.That(duplicate.IsCompleted, Is.False);
            Assert.That(order.Result, Is.SameAs(original)); Assert.That(ledger.BalanceCents, Is.EqualTo(500));
            Assert.That(OrderDelivery.TryComplete(Order(), dish, new PaymentLedger(), out _), Is.False);
        }

        [Test]
        public void RejectedOrderAlsoClosesExactlyOnce()
        {
            var ledger = new PaymentLedger(); var order = Order(true); var dish = Dish();
            Assert.That(OrderDelivery.TryComplete(order, dish, ledger, out _), Is.True);
            Assert.That(OrderDelivery.TryComplete(order, Dish(true), ledger, out _), Is.False);
            Assert.That(OrderDelivery.TryComplete(Order(true, order.InstanceId), Dish(true), ledger, out _), Is.False);
            Assert.That(ledger.BalanceCents, Is.Zero);
        }

        [Test]
        public void LedgerUsesIntegerCentsAndRejectsOverflowOrInvalidTransactionsWithoutPartialMutation()
        {
            var ledger = new PaymentLedger(long.MaxValue - 499); Guid order = Guid.NewGuid(), dish = Guid.NewGuid();
            Assert.That(ledger.TryRecord(order, dish, 500, true), Is.False);
            Assert.That(ledger.BalanceCents, Is.EqualTo(long.MaxValue - 499));
            Assert.That(ledger.TryRecord(order, dish, 499, true), Is.True); Assert.That(ledger.BalanceCents, Is.EqualTo(long.MaxValue));
            Assert.That(ledger.TryRecord(Guid.Empty, Guid.NewGuid(), 1, false), Is.False);
            Assert.That(ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), -1, true), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaymentLedger(-1));
            var regular = new PaymentLedger(); for (int index = 0; index < 100; index++) regular.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 650, true);
            Assert.That(regular.BalanceCents, Is.EqualTo(65000));
        }

        [Test]
        public void OverflowDoesNotCompleteOrderOrSellDishAndInvalidDishesAreIgnored()
        {
            var order = Order(); var dish = Dish();
            Assert.That(OrderDelivery.TryComplete(order, dish, new PaymentLedger(long.MaxValue), out _), Is.False);
            Assert.That(order.IsCompleted, Is.False); Assert.That(dish.IsSold, Is.False);
            Assert.That(OrderDelivery.TryComplete(order, new DishState(), new PaymentLedger(), out _), Is.False);
            dish.Dispose(); Assert.That(OrderDelivery.TryComplete(order, dish, new PaymentLedger(), out _), Is.False);
            Assert.That(OrderDelivery.TryComplete(null, Dish(), new PaymentLedger(), out _), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CustomerLifecycleAcceptsOnlyTheDocumentedSequenceAndContainsNoFoodRules(bool reject)
        {
            var order = Order(reject); var visit = new CustomerVisit(order);
            Assert.That(visit.InstanceId, Is.Not.EqualTo(order.InstanceId)); Assert.That(visit.Order, Is.SameAs(order));
            Assert.That(visit.Receive(), Is.False); Assert.That(visit.BeginLeaving(), Is.False);
            Assert.That(visit.Arrive(), Is.True); Assert.That(visit.BeginWaiting(), Is.True);
            Assert.That(visit.Receive(), Is.True); Assert.That(visit.BeginEvaluation(), Is.True);
            Assert.That(visit.Resolve(), Is.False);
            Assert.That(OrderDelivery.TryComplete(order, Dish(), new PaymentLedger(), out _), Is.True);
            Assert.That(visit.Resolve(), Is.True); Assert.That(visit.Stage, Is.EqualTo(reject ? CustomerStage.Reject : CustomerStage.Pay));
            Assert.That(visit.Receive(), Is.False); Assert.That(visit.ResumeWaiting(), Is.False);
            Assert.That(visit.BeginLeaving(), Is.True); Assert.That(visit.Finish(), Is.True);
            Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Finished)); Assert.That(visit.Arrive(), Is.False);
        }

        [Test]
        public void FailedEvaluationCanReturnToWaitingWithoutCompletingOrder()
        {
            var visit = new CustomerVisit(Order()); visit.Arrive(); visit.BeginWaiting(); visit.Receive(); visit.BeginEvaluation();
            Assert.That(visit.ResumeWaiting(), Is.True); Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(visit.Order.IsCompleted, Is.False);
        }
    }
}
