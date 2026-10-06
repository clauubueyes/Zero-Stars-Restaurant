using System;
using NUnit.Framework;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FoodStateTests
    {
        private static FoodProfile Profile() => new FoodProfile("food.test", "Test Food", FoodCategory.Meat, 80, 1000.0, 60.0);

        [Test]
        public void UnitsShareOnlyImmutableConfiguration()
        {
            FoodProfile profile = Profile();
            var first = new FoodState(profile);
            var second = new FoodState(profile);
            first.Advance(250.0, 35.0);
            first.Contaminate();
            Assert.That(first.Profile, Is.SameAs(second.Profile));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.FreshnessPercent, Is.EqualTo(75.0).Within(1e-9));
            Assert.That(second.FreshnessPercent, Is.EqualTo(100.0));
            Assert.That(second.AgeSeconds, Is.Zero);
            Assert.That(second.TemperatureCelsius, Is.EqualTo(21.0));
            Assert.That(second.IsContaminated, Is.False);
            Assert.That(profile.FreshnessLifetimeSeconds, Is.EqualTo(1000.0));
            Assert.That(profile.ReferenceCostCents, Is.EqualTo(80));
        }

        [TestCase(0.0, 100.0, FoodCondition.Fresh)]
        [TestCase(199.999, 80.0001, FoodCondition.Fresh)]
        [TestCase(200.0, 80.0, FoodCondition.Fresh)]
        [TestCase(200.001, 79.9999, FoodCondition.Acceptable)]
        [TestCase(699.999, 30.0001, FoodCondition.Acceptable)]
        [TestCase(700.0, 30.0, FoodCondition.Spoiled)]
        [TestCase(700.001, 29.9999, FoodCondition.Spoiled)]
        [TestCase(949.999, 5.0001, FoodCondition.Spoiled)]
        [TestCase(950.0, 5.0, FoodCondition.Rotten)]
        [TestCase(950.001, 4.9999, FoodCondition.Rotten)]
        [TestCase(1000.0, 0.0, FoodCondition.Rotten)]
        [TestCase(1e12, 0.0, FoodCondition.Rotten)]
        public void DeteriorationHasExplicitInclusiveBoundaries(double seconds, double freshness, FoodCondition condition)
        {
            var food = new FoodState(Profile());
            food.Advance(seconds, 21.0);
            Assert.That(food.AgeSeconds, Is.EqualTo(seconds));
            Assert.That(food.FreshnessPercent, Is.EqualTo(freshness).Within(1e-8));
            Assert.That(food.FreshnessPercent, Is.InRange(0.0, 100.0));
            Assert.That(food.Condition, Is.EqualTo(condition));
        }

        [Test]
        public void ConstantEnvironmentProducesSameResultForBatchedAndSmallSteps()
        {
            var batched = new FoodState(Profile(), temperatureCelsius: 5.0);
            var incremental = new FoodState(Profile(), temperatureCelsius: 5.0);
            batched.Advance(245.0, 21.0, 1.7);
            for (int index = 0; index < 245; index++) incremental.Advance(1.0, 21.0, 1.7);
            Assert.That(incremental.AgeSeconds, Is.EqualTo(batched.AgeSeconds));
            Assert.That(incremental.FreshnessPercent, Is.EqualTo(batched.FreshnessPercent).Within(1e-9));
            Assert.That(incremental.TemperatureCelsius, Is.EqualTo(batched.TemperatureCelsius).Within(1e-9));
            Assert.That(incremental.Condition, Is.EqualTo(batched.Condition));
        }

        [TestCase(0.0, 100.0)]
        [TestCase(0.5, 95.0)]
        [TestCase(2.0, 80.0)]
        public void CallerCanChangeDeteriorationRateWithoutChangingAge(double multiplier, double expectedFreshness)
        {
            var food = new FoodState(Profile(), temperatureCelsius: 5.0);
            food.Advance(100.0, 21.0, multiplier);
            Assert.That(food.AgeSeconds, Is.EqualTo(100.0));
            Assert.That(food.FreshnessPercent, Is.EqualTo(expectedFreshness).Within(1e-9));
            Assert.That(food.TemperatureCelsius, Is.GreaterThan(5.0).And.LessThan(21.0));
        }

        [Test]
        public void TemperatureRelaxesFromEitherSideAndCanBeSetExplicitly()
        {
            var cold = new FoodState(Profile(), temperatureCelsius: 5.0);
            var warm = new FoodState(Profile(), temperatureCelsius: 35.0);
            cold.Advance(60.0, 21.0);
            warm.Advance(60.0, 21.0);
            Assert.That(cold.TemperatureCelsius, Is.EqualTo(21.0 - 16.0 / Math.E).Within(1e-9));
            Assert.That(warm.TemperatureCelsius, Is.EqualTo(21.0 + 14.0 / Math.E).Within(1e-9));
            cold.SetTemperature(-18.0);
            Assert.That(cold.TemperatureCelsius, Is.EqualTo(-18.0));
            Assert.That(cold.AgeSeconds, Is.EqualTo(60.0));
            Assert.That(cold.FreshnessPercent, Is.EqualTo(94.0).Within(1e-9));
            cold.Advance(1e12, 21.0);
            Assert.That(cold.TemperatureCelsius, Is.EqualTo(21.0));
        }

        [Test]
        public void HugeTimeAndRateStayFiniteAndFreshnessNeverGoesNegative()
        {
            var food = new FoodState(Profile());
            food.Advance(double.MaxValue, 21.0, 0.0);
            food.Advance(double.MaxValue, 21.0, 0.0);
            Assert.That(food.AgeSeconds, Is.EqualTo(double.MaxValue));
            Assert.That(food.FreshnessPercent, Is.EqualTo(100.0));
            food.Advance(1.0, 21.0, double.MaxValue);
            Assert.That(food.FreshnessPercent, Is.Zero);
            Assert.That(food.DeteriorationSeconds, Is.EqualTo(1000.0));
            Assert.That(food.TemperatureCelsius, Is.EqualTo(21.0));
            Assert.That(food.Condition, Is.EqualTo(FoodCondition.Rotten));
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedTimeDoesNotMutateState(double seconds)
        {
            var food = new FoodState(Profile());
            Assert.Throws<ArgumentOutOfRangeException>(() => food.Advance(seconds, 21.0));
            AssertUnchanged(food);
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidDeteriorationRateDoesNotMutateState(double multiplier)
        {
            var food = new FoodState(Profile());
            Assert.Throws<ArgumentOutOfRangeException>(() => food.Advance(60.0, 21.0, multiplier));
            AssertUnchanged(food);
        }

        [TestCase(-300.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidTemperatureDoesNotMutateState(double temperature)
        {
            var food = new FoodState(Profile());
            Assert.Throws<ArgumentOutOfRangeException>(() => food.Advance(60.0, temperature));
            Assert.Throws<ArgumentOutOfRangeException>(() => food.SetTemperature(temperature));
            AssertUnchanged(food);
        }

        [TestCase(-1.0)]
        [TestCase(101.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InitialFreshnessMustBeFiniteAndWithinZeroToOneHundred(double freshness)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodState(Profile(), freshnessPercent: freshness));
        }

        [Test]
        public void SeededAgeExposureAndContaminationAreIndependent()
        {
            var food = new FoodState(Profile(), 1000.0, 95.0, 21.0, true);
            food.Contaminate();
            food.Advance(100.0, 21.0, 0.0);
            Assert.That(food.AgeSeconds, Is.EqualTo(1100.0));
            Assert.That(food.DeteriorationSeconds, Is.EqualTo(50.0).Within(1e-9));
            Assert.That(food.FreshnessPercent, Is.EqualTo(95.0).Within(1e-9));
            Assert.That(food.Condition, Is.EqualTo(FoodCondition.Fresh));
            Assert.That(food.IsContaminated, Is.True);
            food.Advance(0.0, 35.0);
            Assert.That(food.TemperatureCelsius, Is.EqualTo(21.0));
        }

        [Test]
        public void InvalidProfilesCannotEnterTheSimulation()
        {
            Assert.Throws<ArgumentException>(() => new FoodProfile("", "Food", FoodCategory.Meat, 0, 1000.0, 60.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodProfile("food.test", "Food", FoodCategory.Meat, -1, 1000.0, 60.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodProfile("food.test", "Food", FoodCategory.Meat, 0, 0.0, 60.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodProfile("food.test", "Food", FoodCategory.Meat, 0, 1000.0, double.NaN));
            Assert.Throws<ArgumentException>(() => new FoodProfile("food.test", "Food", FoodCategory.Meat, 0, 1000.0, 60.0, 80.0, 5.0, 30.0));
        }

        private static void AssertUnchanged(FoodState food)
        {
            Assert.That(food.AgeSeconds, Is.Zero);
            Assert.That(food.FreshnessPercent, Is.EqualTo(100.0));
            Assert.That(food.TemperatureCelsius, Is.EqualTo(21.0));
        }
    }
}
