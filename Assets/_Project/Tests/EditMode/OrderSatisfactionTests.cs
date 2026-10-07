using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Tests
{
    public sealed class OrderSatisfactionTests
    {
        private static DishProfile Hamburger => new DishProfile("hamburger", "Hamburger", new[] { "bun", "patty", "bun" });
        private static DishProfile Cheeseburger => new DishProfile("cheeseburger", "Cheeseburger", new[] { "bun", "patty", "cheese", "bun" });
        private static FoodState Food(string id) => new FoodState(new FoodProfile(id, id, FoodCategory.Meat, 35, 600, 60,
            cooking: id == "patty" ? new CookingProfile(60, 120, 45, 65, 90) : null));
        private static OrderState Order(bool cheese = false, int price = 500) => new OrderState(new OrderOffer(
            cheese ? Cheeseburger : Hamburger, cheese ? 650 : price, cheese ? new[] { 3, 4, 3, 3 } : new[] { 3, 4, 3 }));

        [TestCase("bun,patty,bun", false, 500, "", "")]
        [TestCase("bun,patty,cheese,bun", true, 650, "", "")]
        [TestCase("patty,bun", false, 350, "bun", "")]
        [TestCase("bun,bun", false, 300, "patty", "")]
        [TestCase("patty", false, 200, "bun,bun", "")]
        [TestCase("bun", false, 150, "patty,bun", "")]
        [TestCase("bun,patty,bun,cheese", false, 500, "", "cheese")]
        [TestCase("bun,patty,bun", true, 500, "cheese", "")]
        [TestCase("cheese", false, 0, "bun,patty,bun", "cheese")]
        [TestCase("tomato", false, 0, "bun,patty,bun", "tomato")]
        [TestCase("bun,bun,bun,bun", false, 300, "patty", "bun,bun")]
        public void ActualMultisetDeterminesPartialPaymentWithoutRequiringAFinalizedDish(string received, bool cheese, int payment, string missing, string extra)
        {
            FoodState[] foods = received.Split(',').Select(Food).ToArray();
            var delivery = new DeliveryContents(foods, new[] { Hamburger, Cheeseburger }, true);
            var order = Order(cheese); var ledger = new PaymentLedger();
            Assert.That(OrderDelivery.TryComplete(order, delivery, ledger, out OrderResult result), Is.True);
            Assert.That(result.Accepted, Is.EqualTo(payment > 0)); Assert.That(result.PaymentCents, Is.EqualTo(payment));
            var satisfaction = result.Evaluation.Satisfaction;
            Assert.That(satisfaction.Expected, Is.EqualTo(order.Offer.Dish.IngredientDefinitionIds));
            Assert.That(satisfaction.Received, Is.EqualTo(received.Split(',')));
            Assert.That(string.Join(",", satisfaction.Missing), Is.EqualTo(missing));
            Assert.That(string.Join(",", satisfaction.Extra), Is.EqualTo(extra));
            Assert.That(result.Evaluation.DeliveredDish.Ingredients.Select(f => f.InstanceId), Is.EqualTo(foods.Select(f => f.InstanceId)));
            Assert.That(ledger.BalanceCents, Is.EqualTo(payment));
            Assert.That(ledger.Transactions.Select(t => t.AmountCents), Is.EqualTo(payment > 0 ? new long[] { payment } : Array.Empty<long>()));
            Assert.That(foods.All(f => f.IsSold == result.Accepted), Is.True);
            Assert.That(OrderDelivery.TryComplete(order, delivery, ledger, out _), Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExactFinalDishPreservesRecognitionAndOriginalStates(bool cheese)
        {
            var dish = new DishState();
            foreach (string id in (cheese ? Cheeseburger : Hamburger).IngredientDefinitionIds) Assert.That(dish.TryAdd(Food(id)), Is.True);
            dish.TrySetOrder(dish.Components.Select(f => f.InstanceId).ToArray(), true);
            dish.TryFinalize(new[] { Hamburger, Cheeseburger });
            Assert.That(OrderDelivery.TryComplete(Order(cheese), dish, new PaymentLedger(), out var result), Is.True);
            Assert.That(result.Evaluation.CorrectOrder, Is.True); Assert.That(result.Evaluation.Satisfaction.IsExact, Is.True);
            Assert.That(result.PaymentCents, Is.EqualTo(cheese ? 650 : 500));
            Assert.That(dish.IsSold && dish.Components.All(f => f.IsSold), Is.True);
        }

        [Test]
        public void RecognitionAndRecipeMismatchDoNotHideCompleteness()
        {
            var wrongRecipe = new DeliveryContents(Cheeseburger.IngredientDefinitionIds.Select(Food), new[] { Cheeseburger }, true);
            Assert.That(OrderDelivery.TryComplete(Order(), wrongRecipe, new PaymentLedger(), out var result), Is.True);
            Assert.That(result.Accepted, Is.True); Assert.That(result.Evaluation.CorrectOrder, Is.False);
            Assert.That(result.Evaluation.Satisfaction.IsComplete, Is.True); Assert.That(result.Evaluation.DeliveredDish.DefinitionId, Is.EqualTo("cheeseburger"));
            Assert.That(result.PaymentCents, Is.EqualTo(500));
            var unrecognized = new DeliveryContents(Hamburger.IngredientDefinitionIds.Reverse().Select(Food));
            Assert.That(OrderDelivery.TryComplete(Order(), unrecognized, new PaymentLedger(), out var loose), Is.True);
            Assert.That(loose.Evaluation.CorrectOrder, Is.False); Assert.That(loose.PaymentCents, Is.EqualTo(500));
        }

        [Test]
        public void ExtrasCanNeverExceedTheMaximumAndRoundingUsesIntegerCents()
        {
            for (int count = 0; count < 20; count++)
            {
                var foods = Hamburger.IngredientDefinitionIds.Concat(Enumerable.Repeat("patty", count)).Select(Food);
                Assert.That(OrderDelivery.TryComplete(Order(price: 501), new DeliveryContents(foods), new PaymentLedger(), out var result), Is.True);
                Assert.That(result.PaymentCents, Is.EqualTo(501));
            }
            Assert.That(OrderDelivery.TryComplete(Order(price: 501), new DeliveryContents(new[] { Food("bun") }), new PaymentLedger(), out var partial), Is.True);
            Assert.That(partial.PaymentCents, Is.EqualTo(150));
        }

        [Test]
        public void SoldUnitCannotBeRebatchedReassembledOrPaidFromAnotherLedger()
        {
            FoodState patty = Food("patty"); var ledger = new PaymentLedger(); var order = Order();
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { patty }), ledger, out _), Is.True);
            var second = new DeliveryContents(new[] { patty, Food("bun") }); var otherOrder = Order();
            Assert.That(OrderDelivery.TryComplete(otherOrder, second, new PaymentLedger(), out _), Is.False);
            Assert.That(new DishState().TryAdd(patty), Is.False);
            Assert.That(otherOrder.IsCompleted, Is.False); Assert.That(ledger.BalanceCents, Is.EqualTo(200));
            var impostor = new FoodState(patty.Profile, instanceId: patty.InstanceId);
            Assert.That(OrderDelivery.TryComplete(otherOrder, new DeliveryContents(new[] { impostor }), ledger, out _), Is.False);
            Assert.That(impostor.IsSold, Is.False);
        }

        [Test]
        public void PartialSalePreservesAllQualityFactsAndHistoricalEvidence()
        {
            FoodState patty = Food("patty"); patty.Contaminate(); patty.SetTemperature(120);
            patty.Advance(900, new ThermalEnvironment(120, allowsCooking: true));
            Guid id = patty.InstanceId; double age = patty.AgeSeconds; double dose = patty.Cooking.EquivalentSeconds;
            Assert.That(OrderDelivery.TryComplete(Order(), new DeliveryContents(new[] { patty }), new PaymentLedger(), out var result), Is.True);
            IngredientSnapshot evidence = result.Evaluation.DeliveredDish.Ingredients.Single();
            Assert.That(result.PaymentCents, Is.EqualTo(200)); Assert.That(evidence.InstanceId, Is.EqualTo(id));
            Assert.That(evidence.Condition, Is.EqualTo(FoodCondition.Rotten)); Assert.That(evidence.IsContaminated, Is.True);
            Assert.That(evidence.CookingStage, Is.EqualTo(CookingStage.Burnt)); Assert.That(evidence.TemperatureCelsius, Is.EqualTo(120));
            Assert.That(evidence.AgeSeconds, Is.EqualTo(age)); Assert.That(evidence.CookingDoseSeconds, Is.EqualTo(dose));
            patty.Advance(100, 21); Assert.That(evidence.AgeSeconds, Is.EqualTo(age)); Assert.That(evidence.TemperatureCelsius, Is.EqualTo(120));
        }

        [Test]
        public void PartialSaleIsAtomicOnOverflowAndDailySummaryUsesActualRevenue()
        {
            FoodState patty = Food("patty"); var order = Order(); var delivery = new DeliveryContents(new[] { patty });
            var overflow = new PaymentLedger(long.MaxValue - 199);
            Assert.That(OrderDelivery.TryComplete(order, delivery, overflow, out _), Is.False);
            Assert.That(order.IsCompleted || patty.IsSold, Is.False); Assert.That(overflow.Transactions, Is.Empty);
            var ledger = new PaymentLedger(1000);
            Assert.That(OrderDelivery.TryComplete(order, delivery, ledger, out _), Is.True);
            Assert.That(ledger.TrySettleDay(new OperatingCostPolicy(30, new[] { new DailyFixedCost("rent", "Rent", 300) }), 0, out var summary), Is.True);
            Assert.That(summary.SalesCents, Is.EqualTo(200)); Assert.That(ledger.BalanceCents, Is.EqualTo(900));
        }

        [Test]
        public void DuplicateEmptyOwnedOrInvalidInputsCannotMutateAnOrder()
        {
            FoodState bun = Food("bun");
            Assert.Throws<ArgumentException>(() => new DeliveryContents(new[] { bun, bun }));
            Assert.Throws<ArgumentException>(() => new DeliveryContents(Array.Empty<FoodState>()));
            Assert.Throws<ArgumentException>(() => new OrderOffer(Hamburger, 500, new[] { 1, 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OrderOffer(Hamburger, 500, new[] { 1, 0, 1 }));
            Assert.Throws<ArgumentException>(() => new OrderOffer(Hamburger, 500, new[] { 1, 1, 2 }));
            using (var draft = new DishState())
            {
                draft.TryAdd(bun); var order = Order(); var ledger = new PaymentLedger();
                Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { bun }), ledger, out _), Is.False);
                Assert.That(ledger.Transactions, Is.Empty); Assert.That(order.IsCompleted, Is.False);
            }
            var invalidLedger = new PaymentLedger();
            Assert.That(invalidLedger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 500, 501, true, new[] { bun.InstanceId }), Is.False);
            Assert.That(invalidLedger.Transactions, Is.Empty);
        }

        [Test]
        public void RelevantZeroCentRoundingStillTransfersOnceAndWeightsAreCopied()
        {
            var weights = new[] { 3, 4, 3 }; var offer = new OrderOffer(Hamburger, 1, weights); weights[0] = 999;
            var order = new OrderState(offer); var bun = Food("bun"); var ledger = new PaymentLedger();
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { bun }), ledger, out var result), Is.True);
            Assert.That(result.Accepted, Is.True); Assert.That(result.PaymentCents, Is.Zero); Assert.That(bun.IsSold, Is.True);
            Assert.That(offer.IngredientWeights[0], Is.EqualTo(3)); Assert.That(ledger.Transactions.Single().AmountCents, Is.Zero);
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { Food("bun") }), ledger, out _), Is.False);
        }
    }
}
