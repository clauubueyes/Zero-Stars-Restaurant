using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class DishStateTests
    {
        private static FoodState Food(string id, double freshness = 100.0, bool contaminated = false, Guid? identity = null)
            => new FoodState(new FoodProfile(id, id, FoodCategory.Meat, id == "bun" ? 35 : id == "cheese" ? 25 : 80,
                600.0, 60.0, cooking: id == "patty" ? new CookingProfile(60, 120, 45, 65, 90) : null),
                freshnessPercent: freshness, temperatureCelsius: 120.0, isContaminated: contaminated, instanceId: identity);
        private static DishProfile Hamburger => new DishProfile("dish.hamburger", "Hamburger", new[] { "bun", "patty", "bun" });
        private static DishProfile Cheeseburger => new DishProfile("dish.cheeseburger", "Cheeseburger", new[] { "bun", "patty", "cheese", "bun" });
        private static DishState Compose(params FoodState[] foods)
        {
            var dish = new DishState();
            foreach (FoodState food in foods) Assert.That(dish.TryAdd(food), Is.True);
            Assert.That(dish.TrySetOrder(foods.Select(food => food.InstanceId).ToArray(), true), Is.True);
            return dish;
        }

        [Test]
        public void ComponentsAreRealIndependentUnitsWithStableIdentitiesAndLiveState()
        {
            FoodState bottom = Food("bun"), top = Food("bun");
            DishState dish = Compose(bottom, top);
            Assert.That(bottom.InstanceId, Is.Not.EqualTo(top.InstanceId));
            Assert.That(dish.Components[0], Is.SameAs(bottom));
            Assert.That(dish.Components[1], Is.SameAs(top));
            Guid id = bottom.InstanceId;
            bottom.Advance(60.0, 21.0); bottom.Contaminate();
            Assert.That(bottom.InstanceId, Is.EqualTo(id));
            Assert.That(dish.Components[0].FreshnessPercent, Is.EqualTo(90.0));
            Assert.That(top.FreshnessPercent, Is.EqualTo(100.0));
            Assert.That(dish.ContainsContamination, Is.True);
        }

        [Test]
        public void OrderedKnownCombinationsRecognizeFromData()
        {
            DishState hamburger = Compose(Food("bun"), Food("patty"), Food("bun"));
            DishState cheeseburger = Compose(Food("bun"), Food("patty"), Food("cheese"), Food("bun"));
            DishProfile[] definitions = { Hamburger, Cheeseburger };
            Assert.That(hamburger.Recognize(definitions).Id, Is.EqualTo("dish.hamburger"));
            Assert.That(cheeseburger.Recognize(definitions).Id, Is.EqualTo("dish.cheeseburger"));
            Assert.That(hamburger.TryFinalize(definitions), Is.True);
            Assert.That(hamburger.DisplayName, Is.EqualTo("Hamburger"));
        }

        [TestCase("cheese,patty,patty")]
        [TestCase("bun,bun,bun")]
        [TestCase("bun,patty,cheese,cheese,bun")]
        [TestCase("patty,patty")]
        public void UnknownCombinationsAreValidFinalizedDishes(string combination)
        {
            DishState dish = Compose(combination.Split(',').Select(id => Food(id)).ToArray());
            Assert.That(dish.Recognize(new[] { Hamburger, Cheeseburger }), Is.Null);
            Assert.That(dish.TryFinalize(new[] { Hamburger, Cheeseburger }), Is.True);
            Assert.That(dish.InstanceId, Is.Not.Null);
            Assert.That(dish.DisplayName, Is.EqualTo("Custom Dish"));
        }

        [Test]
        public void OrderAndPhysicalStackFlagAffectRecognitionWithoutInvalidatingComposition()
        {
            FoodState bun = Food("bun"), patty = Food("patty"), top = Food("bun");
            DishState dish = Compose(bun, patty, top);
            Assert.That(dish.TrySetOrder(new[] { bun.InstanceId, patty.InstanceId, top.InstanceId }, false), Is.True);
            Assert.That(dish.Recognize(new[] { Hamburger }), Is.Null);
            Assert.That(dish.TrySetOrder(new[] { patty.InstanceId, bun.InstanceId, top.InstanceId }, true), Is.True);
            Assert.That(dish.Recognize(new[] { Hamburger }), Is.Null);
            Assert.That(dish.Components[0], Is.SameAs(patty));
            Assert.That(dish.TrySetOrder(new[] { top.InstanceId, patty.InstanceId, bun.InstanceId }, true), Is.True);
            Assert.That(dish.Recognize(new[] { Hamburger }).Id, Is.EqualTo("dish.hamburger"));
        }

        [Test]
        public void UnitCannotBeDuplicatedOrOwnedByTwoDishesAndRemovalAllowsTransfer()
        {
            FoodState food = Food("patty");
            var first = new DishState(); var second = new DishState();
            Assert.That(first.TryAdd(food), Is.True);
            Assert.That(first.TryAdd(food), Is.False);
            Assert.That(second.TryAdd(food), Is.False);
            FoodState duplicateIdentity = Food("patty", identity: food.InstanceId);
            Assert.That(first.TryAdd(duplicateIdentity), Is.False);
            Assert.That(first.TryRemove(food.InstanceId), Is.True);
            Assert.That(second.TryAdd(food), Is.True);
            Assert.That(second.Components[0], Is.SameAs(food));
        }

        [Test]
        public void InvalidOrderIsRejectedWithoutMutation()
        {
            FoodState a = Food("bun"), b = Food("patty");
            DishState dish = Compose(a, b);
            Assert.That(dish.TrySetOrder(new[] { a.InstanceId, a.InstanceId }, false), Is.False);
            Assert.That(dish.TrySetOrder(new[] { Guid.NewGuid(), b.InstanceId }, false), Is.False);
            Assert.That(dish.TrySetOrder(new[] { a.InstanceId }, false), Is.False);
            Assert.That(dish.Components, Is.EqualTo(new[] { a, b }));
            Assert.That(dish.IsStack, Is.True);
        }

        [Test]
        public void FinalizationFreezesMembershipAndOrderButDoesNotFreezeIngredientState()
        {
            FoodState food = Food("patty"); DishState dish = Compose(food);
            Assert.That(dish.InstanceId, Is.Null);
            Assert.That(dish.TryFinalize(Array.Empty<DishProfile>()), Is.True);
            Guid? id = dish.InstanceId;
            Assert.That(dish.TryFinalize(Array.Empty<DishProfile>()), Is.False);
            Assert.That(dish.TryAdd(Food("bun")), Is.False);
            Assert.That(dish.TryRemove(food.InstanceId), Is.False);
            Assert.That(dish.TrySetOrder(new[] { food.InstanceId }, true), Is.False);
            food.Advance(45.0, new ThermalEnvironment(120.0, allowsCooking: true));
            Assert.That(dish.Components[0].Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(dish.Components[0].TemperatureCelsius, Is.EqualTo(120.0));
            Assert.That(dish.InstanceId, Is.EqualTo(id));
        }

        [Test]
        public void AggregateQueriesStayLivePreserveRottenCookedAndContaminatedStateAndSumCosts()
        {
            FoodState bun = Food("bun", 80.0), patty = Food("patty", 0.0, true), cheese = Food("cheese", 40.0);
            patty.Advance(45.0, new ThermalEnvironment(120.0, allowsCooking: true));
            DishState dish = Compose(bun, patty, cheese);
            Assert.That(dish.MinimumFreshnessPercent, Is.EqualTo(0.0));
            Assert.That(dish.MeanFreshnessPercent, Is.EqualTo(40.0));
            Assert.That(dish.ContainsSpoiledOrRotten, Is.True);
            Assert.That(dish.ContainsContamination, Is.True);
            Assert.That(dish.TotalIngredientCostCents, Is.EqualTo(140));
            Assert.That(dish.Components[1].Condition, Is.EqualTo(FoodCondition.Rotten));
            Assert.That(dish.Components[1].Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            bun.Advance(60.0, 21.0);
            Assert.That(dish.MeanFreshnessPercent, Is.EqualTo(110.0 / 3.0).Within(1e-9));
        }

        [Test]
        public void EmptyDishCannotFinalizeAndHasNoInventedFreshness()
        {
            var dish = new DishState();
            Assert.That(dish.TryAdd(null), Is.False);
            Assert.That(dish.TryFinalize(Array.Empty<DishProfile>()), Is.False);
            Assert.That(dish.MinimumFreshnessPercent, Is.Null);
            Assert.That(dish.MeanFreshnessPercent, Is.Null);
            Assert.That(dish.ContainsContamination, Is.False);
            Assert.That(dish.TotalIngredientCostCents, Is.Zero);
        }

        [Test]
        public void DisposedAssemblyReleasesOwnershipAndCannotMutateAgain()
        {
            FoodState food = Food("patty"); DishState first = Compose(food);
            first.Dispose(); first.Dispose();
            Assert.That(first.TryAdd(Food("bun")), Is.False);
            Assert.That(first.TryFinalize(Array.Empty<DishProfile>()), Is.False);
            Assert.That(new DishState().TryAdd(food), Is.True);
        }

        [Test]
        public void DefinitionsCopyRecognitionDataAndRejectInvalidConfiguration()
        {
            string[] ids = { "bun", "patty", "bun" };
            var profile = new DishProfile("dish.hamburger", "Hamburger", ids);
            ids[1] = "cheese";
            Assert.That(profile.IngredientDefinitionIds[1], Is.EqualTo("patty"));
            Assert.Throws<ArgumentException>(() => new DishProfile("", "Name", ids));
            Assert.Throws<ArgumentException>(() => new DishProfile("dish.x", "Name", Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => new DishProfile("dish.x", "Name", new[] { " " }));
            Assert.Throws<ArgumentException>(() => Food("bun", identity: Guid.Empty));
        }
    }
}
