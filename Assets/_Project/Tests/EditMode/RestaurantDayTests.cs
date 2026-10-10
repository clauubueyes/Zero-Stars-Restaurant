using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed class RestaurantDayTests
    {
        private static RestaurantDay Started(int start = 480, int open = 540, int close = 1020)
        { var day = new RestaurantDay(start, open, close); Assert.That(day.TryStartDay(0), Is.True); return day; }

        [Test]
        public void ClockAdvancesExplicitWorldSecondsWithHoursMinutesAndStableDayNumber()
        {
            var clock = new GameTime(); clock.Advance(14 * 3600 + 35 * 60 + 59.5);
            Assert.That(clock.DayNumber, Is.EqualTo(1)); Assert.That(clock.Hour, Is.EqualTo(14)); Assert.That(clock.Minute, Is.EqualTo(35));
            clock.Advance(.5); Assert.That(clock.Minute, Is.EqualTo(36)); clock.Advance(double.MaxValue);
            Assert.That(clock.Hour, Is.EqualTo(23)); Assert.That(clock.Minute, Is.EqualTo(59)); Assert.That(clock.DayNumber, Is.EqualTo(1));
        }
        [Test]
        public void PreparationNeverOpensAutomaticallyAndExplicitOpenAdmitsCustomers()
        {
            var day = Started(); Assert.That(day.Calendar.CurrentDay, Is.EqualTo(1)); Assert.That(day.Calendar.TotalDaysElapsed, Is.Zero);
            Assert.That(day.Calendar, Is.SameAs(day.Clock.Calendar)); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Preparation));
            day.Advance(100000, 0); Assert.That(day.CanAdmitCustomers, Is.False); Assert.That(day.Clock.Hour, Is.EqualTo(8));
            Assert.That(day.TryEndDay(0), Is.False); Assert.That(day.TryStartDay(0), Is.False);
            Assert.That(day.TryOpenRestaurant(), Is.True); Assert.That(day.TryOpenRestaurant(), Is.False);
            Assert.That(day.Clock.Hour, Is.EqualTo(9)); Assert.That(day.Clock.ElapsedWorldSeconds, Is.Zero); Assert.That(day.CanAdmitCustomers, Is.True);
        }
        [Test]
        public void ClosingBlocksAdmissionAndWaitsForAllCustomersBeforeClosed()
        {
            var day = Started(); day.TryOpenRestaurant(); day.Advance(8 * 3600 - 1, 4); Assert.That(day.CanAdmitCustomers, Is.True);
            day.Advance(1, 4); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(day.CanAdmitCustomers, Is.False);
            Assert.That(day.TryEndDay(4), Is.False); Assert.That(day.TryStartDay(4), Is.False);
            day.Advance(5, 1); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closing));
            day.Advance(0, 0); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            double time = day.Clock.ElapsedWorldSeconds; day.Advance(100000, 0);
            Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(day.Calendar.CurrentDay, Is.EqualTo(1));
            Assert.That(day.Clock.ElapsedWorldSeconds, Is.EqualTo(time)); Assert.That(day.TryStartDay(0), Is.False);
        }
        [Test]
        public void FourDaysRequireOneEndAndOneStartEachAndPublishOrderedEventsOnce()
        {
            var day = new RestaurantDay(480, 540, 1020); var events = new List<string>();
            day.DayStarted += n => events.Add(n + ":start"); day.RestaurantOpened += n => events.Add(n + ":open");
            day.RestaurantClosing += n => events.Add(n + ":closing"); day.RestaurantClosed += n => events.Add(n + ":closed");
            day.DayEnded += n => events.Add(n + ":end");
            var expected = new List<string>(); Assert.That(day.TryStartDay(0), Is.True);
            for (int number = 1; number <= 4; number++)
            {
                Assert.That(day.Calendar.CurrentDay, Is.EqualTo(number)); Assert.That(day.Calendar.TotalDaysElapsed, Is.EqualTo(number - 1));
                Assert.That(day.Calendar.HasReachedDay(number), Is.True); Assert.That(day.Calendar.HasReachedDay(number + 1), Is.False);
                Assert.That(day.TryStartDay(0), Is.False); Assert.That(day.TryOpenRestaurant(), Is.True); Assert.That(day.TryOpenRestaurant(), Is.False);
                Assert.That(day.TryForceClose(1), Is.True); Assert.That(day.TryForceClose(1), Is.False);
                Assert.That(day.TryEndDay(1), Is.False); day.Advance(0, 0); Assert.That(day.TryEndDay(0), Is.True);
                Assert.That(day.TryEndDay(0), Is.False); Assert.That(day.TryStartDay(1), Is.False);
                foreach (string action in new[] { "start", "open", "closing", "closed", "end" }) expected.Add(number + ":" + action);
                if (number < 4) Assert.That(day.TryStartDay(0), Is.True);
            }
            Assert.That(events, Is.EqualTo(expected)); Assert.That(day.Clock.ElapsedWorldSeconds, Is.Zero, "Force Close and transitions add no fictitious time.");
        }
        [Test]
        public void PausingStopsOnlyClockAndEmptyClosingCanStillFinish()
        {
            var day = Started(540, 540, 541); day.TryOpenRestaurant(); day.SetPaused(true); day.Advance(10000, 2);
            Assert.That(day.Clock.SecondsOfDay, Is.EqualTo(540 * 60)); Assert.That(day.CanAdmitCustomers, Is.True);
            day.SetPaused(false); day.Advance(60, 1); day.SetPaused(true); day.Advance(10000, 0);
            Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(day.Clock.Minute, Is.EqualTo(1));
            day.TryEndDay(0); day.TryStartDay(0); Assert.That(day.IsPaused, Is.True); Assert.That(day.Clock.DayNumber, Is.EqualTo(2));
        }
        [TestCase(0)] [TestCase(4)]
        public void LargeStepsAndPartitionsProduceTheSameClosingClock(int occupancy)
        {
            var once = Started(); var many = Started(); once.TryOpenRestaurant(); many.TryOpenRestaurant();
            once.Advance(40000, occupancy); for (int step = 0; step < 400; step++) many.Advance(100, occupancy);
            Assert.That(once.Stage, Is.EqualTo(occupancy == 0 ? RestaurantDayStage.Closed : RestaurantDayStage.Closing));
            Assert.That(many.Stage, Is.EqualTo(once.Stage)); Assert.That(many.Clock.SecondsOfDay, Is.EqualTo(once.Clock.SecondsOfDay));
        }
        [TestCase(-1, 540, 1020)] [TestCase(541, 540, 1020)] [TestCase(540, 540, 540)]
        [TestCase(540, 1020, 540)] [TestCase(540, 540, 1440)]
        public void InvalidOrOvernightSchedulesAreRejected(int start, int open, int close)
            => Assert.Throws<ArgumentException>(() => new RestaurantDay(start, open, close));
        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedTimeCannotMutateClockOrDay(double invalid)
        {
            var day = Started(); Assert.Throws<ArgumentOutOfRangeException>(() => day.Advance(invalid, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => day.Clock.Advance(invalid));
            Assert.That(day.Clock.Hour, Is.EqualTo(8)); Assert.That(day.CanAdmitCustomers, Is.False);
        }
        [Test]
        public void InvalidOccupancyAndCalendarQueriesAreRejectedWithoutMutation()
        {
            var day = Started(); Assert.Throws<ArgumentOutOfRangeException>(() => day.TryStartDay(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => day.TryEndDay(-1)); Assert.Throws<ArgumentOutOfRangeException>(() => day.TryForceClose(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => day.Advance(1, -1)); Assert.Throws<ArgumentOutOfRangeException>(() => day.Calendar.HasReachedDay(0));
            Assert.That(day.Calendar.CurrentDay, Is.EqualTo(1)); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Preparation));
        }
    }
}
