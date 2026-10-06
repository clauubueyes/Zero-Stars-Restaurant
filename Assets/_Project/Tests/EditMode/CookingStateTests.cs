using System;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class CookingStateTests
    {
        private static FoodState Food(double temperature = 21.0, double freshness = 100.0, bool contaminated = false,
            bool cookable = true) => new FoodState(new FoodProfile("test.food", "Test food", FoodCategory.Meat,
                10, 600.0, 60.0, cooking: cookable ? new CookingProfile(60.0, 120.0, 45.0, 65.0, 90.0) : null),
                freshnessPercent: freshness, temperatureCelsius: temperature, isContaminated: contaminated);

        private static ThermalEnvironment Source(double temperature, double power = 1.0)
            => new ThermalEnvironment(temperature, power, allowsCooking: true);

        [Test]
        public void NonCookableFoodWarmsWithoutAcquiringCookingState()
        {
            FoodState food = Food(cookable: false);
            food.Advance(600.0, Source(180.0, 4.0));
            Assert.That(food.TemperatureCelsius, Is.EqualTo(180.0).Within(1e-9));
            Assert.That(food.Cooking, Is.Null);
        }

        [TestCase(-18.0)]
        [TestCase(21.0)]
        [TestCase(60.0)]
        public void AmbientOrColdSourceDoesNotCook(double sourceTemperature)
        {
            FoodState food = Food();
            food.Advance(1e12, Source(sourceTemperature, 4.0));
            Assert.That(food.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
            Assert.That(food.Cooking.EquivalentSeconds, Is.Zero);
        }

        [Test]
        public void WarmupIsIntegratedInsteadOfCountingTheFinalTemperatureForTheWholeStep()
        {
            FoodState food = Food();
            double crossing = 15.0 * Math.Log((180.0 - 21.0) / (180.0 - 60.0));
            food.Advance(crossing - 0.01, Source(180.0, 4.0));
            Assert.That(food.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
            food.Advance(0.02, Source(180.0, 4.0));
            Assert.That(food.Cooking.Stage, Is.EqualTo(CookingStage.Undercooked));
            Assert.That(food.Cooking.EquivalentSeconds, Is.InRange(0.0, 0.001));
        }

        [TestCase(0.0, CookingStage.Raw)]
        [TestCase(44.999, CookingStage.Undercooked)]
        [TestCase(45.0, CookingStage.Cooked)]
        [TestCase(64.999, CookingStage.Cooked)]
        [TestCase(65.0, CookingStage.Overcooked)]
        [TestCase(89.999, CookingStage.Overcooked)]
        [TestCase(90.0, CookingStage.Burnt)]
        public void ContinuousDoseDeterminesStageAtConfiguredBoundaries(double seconds, CookingStage stage)
        {
            FoodState food = Food(120.0);
            food.Advance(seconds, Source(120.0), 0.0);
            Assert.That(food.Cooking.Stage, Is.EqualTo(stage));
            Assert.That(food.Cooking.ProgressPercent, Is.EqualTo(seconds / 45.0 * 100.0).Within(1e-9));
            Assert.That(food.FreshnessPercent, Is.EqualTo(100.0));
        }

        [Test]
        public void FrozenFoodAndLowerPowerTakeLongerToCook()
        {
            FoodState frozen = Food(-18.0), warm = Food(), lowPower = Food();
            frozen.Advance(20.0, Source(180.0, 4.0));
            warm.Advance(20.0, Source(180.0, 4.0));
            lowPower.Advance(20.0, Source(180.0, 1.0));
            Assert.That(frozen.Cooking.EquivalentSeconds, Is.LessThan(warm.Cooking.EquivalentSeconds));
            Assert.That(lowPower.Cooking.EquivalentSeconds, Is.LessThan(warm.Cooking.EquivalentSeconds));
            Assert.That(lowPower.TemperatureCelsius, Is.LessThan(warm.TemperatureCelsius));
        }

        [TestCase(180.0, 4.0, 21.0, 40.0)]
        [TestCase(21.0, 1.0, 180.0, 100.0)]
        [TestCase(70.0, 0.2, -18.0, 300.0)]
        public void OneLargeStepMatchesSmallStepsIncludingThresholdCrossings(double target, double power, double initial, double time)
        {
            FoodState batch = Food(initial), steps = Food(initial);
            var environment = Source(target, power);
            batch.Advance(time, environment);
            for (int i = 0; i < 1000; i++) steps.Advance(time / 1000.0, environment);
            Assert.That(batch.TemperatureCelsius, Is.EqualTo(steps.TemperatureCelsius).Within(1e-8));
            Assert.That(batch.Cooking.EquivalentSeconds, Is.EqualTo(steps.Cooking.EquivalentSeconds).Within(1e-8));
            Assert.That(batch.FreshnessPercent, Is.EqualTo(steps.FreshnessPercent).Within(1e-8));
        }

        [Test]
        public void RemovalPausesCookingButCoolsGraduallyAndReplacementPreservesDose()
        {
            FoodState food = Food();
            food.Advance(20.0, Source(180.0, 4.0));
            double hot = food.TemperatureCelsius, dose = food.Cooking.EquivalentSeconds;
            food.Advance(10.0, 21.0);
            Assert.That(food.TemperatureCelsius, Is.LessThan(hot).And.GreaterThan(21.0));
            Assert.That(food.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            food.Advance(600.0, 21.0);
            double cooledDose = food.Cooking.EquivalentSeconds;
            food.Advance(600.0, 21.0);
            Assert.That(food.Cooking.EquivalentSeconds, Is.EqualTo(cooledDose));
            food.Advance(20.0, Source(180.0, 4.0));
            Assert.That(food.Cooking.EquivalentSeconds, Is.GreaterThan(cooledDose));
        }

        [Test]
        public void RottenAndContaminatedFoodRemainSoAfterCookingAndBurning()
        {
            FoodState rotten = Food(120.0, 0.0, true), fresh = Food(120.0);
            rotten.Advance(45.0, Source(120.0));
            fresh.Advance(45.0, Source(120.0));
            Assert.That(rotten.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(rotten.Condition, Is.EqualTo(FoodCondition.Rotten));
            Assert.That(fresh.Condition, Is.EqualTo(FoodCondition.Fresh));
            rotten.Advance(45.0, Source(120.0));
            fresh.Advance(45.0, Source(120.0));
            Assert.That(fresh.Cooking.Stage, Is.EqualTo(CookingStage.Burnt));
            Assert.That(fresh.Condition, Is.EqualTo(FoodCondition.Fresh));
            Assert.That(rotten.FreshnessPercent, Is.Zero);
            Assert.That(rotten.IsContaminated, Is.True);
        }

        [Test]
        public void ExtremeElapsedTimeSaturatesDoseWithoutOverflowOrIteration()
        {
            FoodState food = Food();
            food.Advance(double.MaxValue, Source(180.0, 4.0));
            food.Advance(double.MaxValue, Source(180.0, 4.0));
            Assert.That(food.Cooking.Stage, Is.EqualTo(CookingStage.Burnt));
            Assert.That(food.Cooking.EquivalentSeconds, Is.EqualTo(90.0));
            Assert.That(food.TemperatureCelsius, Is.EqualTo(180.0));
            Assert.That(food.AgeSeconds, Is.EqualTo(double.MaxValue));
        }

        [Test]
        public void InvalidThermalAndCookingConfigurationIsRejectedBeforeMutation()
        {
            Assert.Throws<ArgumentException>(() => new CookingProfile(60.0, 60.0, 45.0, 65.0, 90.0));
            Assert.Throws<ArgumentException>(() => new CookingProfile(60.0, 120.0, 65.0, 45.0, 90.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThermalEnvironment(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThermalEnvironment(180.0, 0.0));
            FoodState food = Food();
            Assert.Throws<ArgumentOutOfRangeException>(() => food.Advance(100.0, default(ThermalEnvironment)));
            Assert.That(food.AgeSeconds, Is.Zero);
            Assert.That(food.Cooking.EquivalentSeconds, Is.Zero);
            Assert.That(food.TemperatureCelsius, Is.EqualTo(21.0));
        }
    }
}
