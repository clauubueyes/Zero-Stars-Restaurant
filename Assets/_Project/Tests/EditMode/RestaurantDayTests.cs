using System;
using NUnit.Framework;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed class RestaurantDayTests
    {
        [Test]
        public void ClockAdvancesExplicitWorldSecondsWithHoursMinutesAndStableDayNumber()
        {
            var clock = new GameTime(); clock.Advance(14 * 3600 + 35 * 60 + 59.5);
            Assert.That(clock.DayNumber, Is.EqualTo(1)); Assert.That(clock.Hour, Is.EqualTo(14)); Assert.That(clock.Minute, Is.EqualTo(35));
            clock.Advance(0.5); Assert.That(clock.Minute, Is.EqualTo(36));
            clock.Advance(double.MaxValue); Assert.That(clock.Hour, Is.EqualTo(23)); Assert.That(clock.Minute, Is.EqualTo(59));
            Assert.That(clock.DayNumber, Is.EqualTo(1), "Only starting the next restaurant day increments the day.");
        }
        [Test]
        public void StartOpensAtTheConfiguredBoundaryAndClosingWaitsForExistingCustomers()
        {
            var day = new RestaurantDay(8 * 60, 9 * 60, 17 * 60);
            day.Advance(100000, 0); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Ready));
            Assert.That(day.CanAdmitCustomers, Is.False); Assert.That(day.TryStartDay(0), Is.True);
            Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.BeforeOpen));
            day.Advance(3599, 0); Assert.That(day.CanAdmitCustomers, Is.False);
            day.Advance(1, 0); Assert.That(day.CanAdmitCustomers, Is.True);
            day.Advance(8 * 3600 - 1, 4); Assert.That(day.CanAdmitCustomers, Is.True);
            day.Advance(1, 4); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(day.CanAdmitCustomers, Is.False);
            Assert.That(day.TryStartDay(4), Is.False); day.Advance(5, 1); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closing));
            day.Advance(0, 0); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed));
        }
        [Test]
        public void EmptyRestaurantClosesOnceAndNextDayResetsOnlyWorldClock()
        {
            var day = new RestaurantDay(540, 540, 541); day.TryStartDay(0);
            Assert.That(day.TryStartDay(0), Is.False); day.Advance(60, 0);
            Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            double closedTime = day.Clock.SecondsOfDay; day.Advance(10000, 0); Assert.That(day.Clock.SecondsOfDay, Is.EqualTo(closedTime));
            Assert.That(day.TryStartDay(1), Is.False); Assert.That(day.TryStartDay(0), Is.True);
            Assert.That(day.Clock.DayNumber, Is.EqualTo(2)); Assert.That(day.Clock.Hour, Is.EqualTo(9)); Assert.That(day.Clock.Minute, Is.Zero);
            Assert.That(day.CanAdmitCustomers, Is.True); day.Advance(60, 0); day.TryStartDay(0);
            Assert.That(day.Clock.DayNumber, Is.EqualTo(3));
        }
        [Test]
        public void PausingStopsOnlyClockAndEmptyClosingCanStillFinish()
        {
            var day = new RestaurantDay(540, 540, 541); day.TryStartDay(0); day.SetPaused(true);
            day.Advance(10000, 2); Assert.That(day.Clock.SecondsOfDay, Is.EqualTo(540 * 60)); Assert.That(day.CanAdmitCustomers, Is.True);
            day.SetPaused(false); day.Advance(60, 1); day.SetPaused(true); day.Advance(10000, 0);
            Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(day.Clock.Minute, Is.EqualTo(1));
            day.TryStartDay(0); Assert.That(day.IsPaused, Is.True); Assert.That(day.Clock.DayNumber, Is.EqualTo(2));
        }
        [Test]
        public void EmptyClosingStopsAtTheBoundaryEvenWhenOneAdvanceCrossesTheEntireDay()
        {
            var once = new RestaurantDay(480, 540, 1020); var many = new RestaurantDay(480, 540, 1020);
            once.TryStartDay(0); many.TryStartDay(0); once.Advance(100000, 0);
            for (int step = 0; step < 1000; step++) many.Advance(100, 0);
            Assert.That(once.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(many.Stage, Is.EqualTo(once.Stage));
            Assert.That(once.Clock.SecondsOfDay, Is.EqualTo(1020 * 60)); Assert.That(many.Clock.SecondsOfDay, Is.EqualTo(once.Clock.SecondsOfDay));
        }
        [Test]
        public void LargeAdvanceCrossesBothBoundariesAndMatchesPartitionedClock()
        {
            var once = new RestaurantDay(480, 540, 1020); var many = new RestaurantDay(480, 540, 1020);
            once.TryStartDay(0); many.TryStartDay(0); once.Advance(40000, 4);
            for (int step = 0; step < 400; step++) many.Advance(100, 4);
            Assert.That(once.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(many.Stage, Is.EqualTo(once.Stage));
            Assert.That(many.Clock.SecondsOfDay, Is.EqualTo(once.Clock.SecondsOfDay));
        }
        [TestCase(-1, 540, 1020)] [TestCase(541, 540, 1020)] [TestCase(540, 540, 540)]
        [TestCase(540, 1020, 540)] [TestCase(540, 540, 1440)]
        public void InvalidOrOvernightSchedulesAreRejected(int start, int open, int close)
            => Assert.Throws<ArgumentException>(() => new RestaurantDay(start, open, close));
        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedTimeCannotMutateClockOrDay(double invalid)
        {
            var day = new RestaurantDay(540, 540, 1020); day.TryStartDay(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => day.Advance(invalid, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => day.Clock.Advance(invalid));
            Assert.That(day.Clock.SecondsOfDay, Is.EqualTo(540 * 60)); Assert.That(day.CanAdmitCustomers, Is.True);
        }
        [Test]
        public void InvalidOccupancyCannotEndOrStartADay()
        {
            var day = new RestaurantDay(540, 540, 1020);
            Assert.Throws<ArgumentOutOfRangeException>(() => day.TryStartDay(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => day.Advance(1, -1)); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Ready));
        }
    }
}
