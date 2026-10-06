using System;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FoodPreservationTests
    {
        private static FoodProfile FoodProfile(bool cookable = false) => new FoodProfile("food.storage-test", "Storage Food",
            FoodCategory.Meat, 80, 1000, 60, cooking: cookable ? new CookingProfile(60, 120, 45, 65, 90) : null);
        private static readonly FoodPreservationProfile Preservation = new FoodPreservationProfile();

        [Test]
        public void AmbientFoodKeepsNormalDeteriorationAndFullChronologicalAge()
        {
            var food = new FoodState(FoodProfile());
            food.Advance(60, 21, preservation: Preservation);
            Assert.That(food.TemperatureCelsius, Is.EqualTo(21));
            Assert.That(food.AgeSeconds, Is.EqualTo(60)); Assert.That(food.FreshnessPercent, Is.EqualTo(94).Within(1e-9));
        }

        [Test]
        public void FridgeCoolsProgressivelyAndDoesNotGrantPreservationBeforeFoodCools()
        {
            var food = new FoodState(FoodProfile());
            var fridge = new ThermalEnvironment(4, 2);
            food.Advance(10, fridge, preservation: Preservation);
            Assert.That(food.TemperatureCelsius, Is.InRange(4.01, 20.99));
            Assert.That(food.DeteriorationSeconds, Is.EqualTo(10).Within(1e-9));
            food.Advance(290, fridge, preservation: Preservation);
            double coolingToFive = 30 * Math.Log(17);
            Assert.That(food.DeteriorationSeconds, Is.EqualTo(coolingToFive + (300 - coolingToFive) * 0.1).Within(1e-9));
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(4).And.LessThan(4.01));
            Assert.That(food.FreshnessPercent, Is.GreaterThan(89));
        }

        [Test]
        public void FreezerPassesThroughZeroGraduallyInsteadOfChangingCookingState()
        {
            var food = new FoodState(FoodProfile(true));
            var freezer = new ThermalEnvironment(-18, 2);
            food.Advance(10, freezer, preservation: Preservation);
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(0).And.LessThan(21));
            food.Advance(60, freezer, preservation: Preservation);
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(-18).And.LessThan(0));
            Assert.That(food.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
            Assert.That(food.AgeSeconds, Is.EqualTo(70));
        }

        [TestCase(4, 10)]
        [TestCase(-18, 0.1)]
        public void EquilibratedColdFoodHasReducedDeteriorationWithoutPausingAge(double temperature, double loss)
        {
            var food = new FoodState(FoodProfile(), temperatureCelsius: temperature);
            food.Advance(1000, temperature, preservation: Preservation);
            Assert.That(food.AgeSeconds, Is.EqualTo(1000));
            Assert.That(food.FreshnessPercent, Is.EqualTo(100 - loss).Within(1e-9));
        }

        [Test]
        public void RemovalWarmsGraduallyAndPreservationFollowsTheColdUnitOutsideStorage()
        {
            var food = new FoodState(FoodProfile(), temperatureCelsius: -18);
            food.Advance(10, 21, preservation: Preservation);
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(-18).And.LessThan(0));
            Assert.That(food.DeteriorationSeconds, Is.EqualTo(0.01).Within(1e-9));
            food.Advance(290, 21, preservation: Preservation);
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(20).And.LessThan(21));
            double before = food.DeteriorationSeconds;
            food.Advance(10, 21, preservation: Preservation);
            Assert.That(food.DeteriorationSeconds - before, Is.EqualTo(10).Within(1e-9));
        }

        [TestCase(21, 4, 2)]
        [TestCase(21, -18, 2)]
        [TestCase(-18, 21, 1)]
        [TestCase(120, -18, 2)]
        [TestCase(5, 21, 1)]
        [TestCase(0, -18, 2)]
        [TestCase(4, 4, 2)]
        public void ThresholdCrossingsGiveEquivalentResultsForLargeAndSmallSteps(double initial, double target, double transfer)
        {
            var batched = new FoodState(FoodProfile(), temperatureCelsius: initial);
            var stepped = new FoodState(FoodProfile(), temperatureCelsius: initial);
            var environment = new ThermalEnvironment(target, transfer);
            batched.Advance(300, environment, preservation: Preservation);
            for (int index = 0; index < 1200; index++) stepped.Advance(0.25, environment, preservation: Preservation);
            Assert.That(stepped.TemperatureCelsius, Is.EqualTo(batched.TemperatureCelsius).Within(1e-9));
            Assert.That(stepped.DeteriorationSeconds, Is.EqualTo(batched.DeteriorationSeconds).Within(1e-8));
            Assert.That(stepped.AgeSeconds, Is.EqualTo(batched.AgeSeconds));
        }

        [Test]
        public void AmbientFridgeFreezerAmbientPreservesIdentityHistoryContaminationAndCookingDose()
        {
            var food = new FoodState(FoodProfile(true), ageSeconds: 100, freshnessPercent: 75, temperatureCelsius: 120, isContaminated: true);
            food.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0);
            Guid id = food.InstanceId; CookingState cooking = food.Cooking; double dose = cooking.EquivalentSeconds;
            double freshness = food.FreshnessPercent;
            foreach (var environment in new[] { new ThermalEnvironment(21), new ThermalEnvironment(4, 2),
                         new ThermalEnvironment(-18, 2), new ThermalEnvironment(21) })
            {
                food.Advance(120, environment, preservation: Preservation);
                Assert.That(food.InstanceId, Is.EqualTo(id)); Assert.That(food.Cooking, Is.SameAs(cooking));
                Assert.That(cooking.EquivalentSeconds, Is.EqualTo(dose)); Assert.That(food.IsContaminated, Is.True);
                Assert.That(food.FreshnessPercent, Is.LessThanOrEqualTo(freshness)); freshness = food.FreshnessPercent;
            }
            Assert.That(food.AgeSeconds, Is.EqualTo(625));
        }

        [Test]
        public void SpoiledFoodNeverBecomesFreshAndFrozenExposureRemainsFiniteForHugeTime()
        {
            var rotten = new FoodState(FoodProfile(), freshnessPercent: 0, temperatureCelsius: -18, isContaminated: true);
            rotten.Advance(double.MaxValue, -18, preservation: Preservation);
            Assert.That(rotten.Condition, Is.EqualTo(FoodCondition.Rotten)); Assert.That(rotten.IsContaminated, Is.True);
            Assert.That(rotten.AgeSeconds, Is.EqualTo(double.MaxValue));
            var food = new FoodState(FoodProfile(), temperatureCelsius: -18);
            food.Advance(double.MaxValue, -18, double.MaxValue, Preservation);
            Assert.That(food.DeteriorationSeconds, Is.EqualTo(1000)); Assert.That(food.FreshnessPercent, Is.Zero);
        }

        [TestCase(5, 5, 0.1, 0.001)]
        [TestCase(5, 6, 0.1, 0.001)]
        [TestCase(double.NaN, 0, 0.1, 0.001)]
        [TestCase(5, -300, 0.1, 0.001)]
        [TestCase(5, 0, -1, 0.001)]
        [TestCase(5, 0, 0.1, 0.2)]
        [TestCase(5, 0, double.PositiveInfinity, 0.001)]
        public void InvalidPreservationPolicyCannotEnterSimulation(double refrigerated, double frozen, double chilledRate, double frozenRate)
        { Assert.Throws<ArgumentException>(() => new FoodPreservationProfile(refrigerated, frozen, chilledRate, frozenRate)); }

        [Test]
        public void PolicyBoundariesAreInclusiveAndZeroRateStillAdvancesTemperatureAndAge()
        {
            Assert.That(Preservation.RateAt(5), Is.EqualTo(0.1)); Assert.That(Preservation.RateAt(0), Is.EqualTo(0.001));
            Assert.That(Preservation.RateAt(5.001), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Preservation.RateAt(double.NaN));
            var food = new FoodState(FoodProfile(), temperatureCelsius: -10);
            food.Advance(60, -18, preservation: new FoodPreservationProfile(frozenRate: 0));
            Assert.That(food.AgeSeconds, Is.EqualTo(60)); Assert.That(food.DeteriorationSeconds, Is.Zero);
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(-18).And.LessThan(-10));
        }
    }
}
