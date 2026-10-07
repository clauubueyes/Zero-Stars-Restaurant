using System;

namespace ZeroStarRestaurant.Utilities
{
    public sealed class ElectricityMeter
    {
        public double RatedWatts { get; }
        public double ConsumedKilowattHours { get; private set; }
        public double DailyConsumedKilowattHours { get; private set; }
        private bool _recordDaily = true;

        public ElectricityMeter(double ratedWatts)
        {
            RequireNonnegativeFinite(ratedWatts, nameof(ratedWatts));
            RatedWatts = ratedWatts;
        }

        internal double ConsumptionFor(double elapsedSeconds) => RatedWatts / 3600000.0 * elapsedSeconds;
        internal void Record(double kilowattHours)
        {
            ConsumedKilowattHours += kilowattHours;
            if (_recordDaily) DailyConsumedKilowattHours += kilowattHours;
        }
        internal void ValidateRecord(double kilowattHours)
        {
            RequireNonnegativeFinite(ConsumedKilowattHours + kilowattHours, nameof(kilowattHours));
            if (_recordDaily) RequireNonnegativeFinite(DailyConsumedKilowattHours + kilowattHours, nameof(kilowattHours));
        }
        public void BeginDay() { DailyConsumedKilowattHours = 0; _recordDaily = true; }
        public void CompleteDay() => _recordDaily = false;

        internal static void RequireNonnegativeFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
