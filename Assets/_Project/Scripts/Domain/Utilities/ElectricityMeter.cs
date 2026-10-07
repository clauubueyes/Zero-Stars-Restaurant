using System;

namespace ZeroStarRestaurant.Utilities
{
    public sealed class ElectricityMeter
    {
        public double RatedWatts { get; }
        public double ConsumedKilowattHours { get; private set; }

        public ElectricityMeter(double ratedWatts)
        {
            RequireNonnegativeFinite(ratedWatts, nameof(ratedWatts));
            RatedWatts = ratedWatts;
        }

        internal double ConsumptionFor(double elapsedSeconds) => RatedWatts / 3600000.0 * elapsedSeconds;
        internal void Record(double kilowattHours) => ConsumedKilowattHours += kilowattHours;

        internal static void RequireNonnegativeFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
