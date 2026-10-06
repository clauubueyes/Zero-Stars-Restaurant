using System;

namespace ZeroStarRestaurant.Food
{
    // Immutable prototype policy: rates depend on the unit's actual temperature, never its container.
    public sealed class FoodPreservationProfile
    {
        public double RefrigeratedAtCelsius { get; }
        public double FrozenAtCelsius { get; }
        public double RefrigeratedRate { get; }
        public double FrozenRate { get; }

        public FoodPreservationProfile(double refrigeratedAtCelsius = 5.0, double frozenAtCelsius = 0.0,
            double refrigeratedRate = 0.1, double frozenRate = 0.001)
        {
            if (!Temperature(refrigeratedAtCelsius) || !Temperature(frozenAtCelsius) || frozenAtCelsius >= refrigeratedAtCelsius)
                throw new ArgumentException("Preservation temperatures must be finite, ordered frozen < refrigerated, and above absolute zero.");
            if (!Rate(refrigeratedRate) || !Rate(frozenRate) || frozenRate > refrigeratedRate)
                throw new ArgumentException("Preservation rates must satisfy 0 <= frozen <= refrigerated <= 1.");
            RefrigeratedAtCelsius = refrigeratedAtCelsius; FrozenAtCelsius = frozenAtCelsius;
            RefrigeratedRate = refrigeratedRate; FrozenRate = frozenRate;
        }

        public double RateAt(double temperatureCelsius)
        {
            if (!Temperature(temperatureCelsius)) throw new ArgumentOutOfRangeException(nameof(temperatureCelsius));
            return temperatureCelsius <= FrozenAtCelsius ? FrozenRate :
                temperatureCelsius <= RefrigeratedAtCelsius ? RefrigeratedRate : 1.0;
        }

        // FoodState validates time/environment. Integrate the exponential's two threshold crossings
        // analytically so manual jumps and small frames preserve the same deterioration history.
        internal double ExposureSeconds(double seconds, double initial, double target, double responseSeconds)
        {
            double retained = responseSeconds == 0.0 ? 0.0 : Math.Exp(-seconds / responseSeconds);
            double final = initial * retained + target * (1.0 - retained);
            double refrigerated = TimeAtOrBelow(RefrigeratedAtCelsius, seconds, initial, target, final, responseSeconds);
            double frozen = TimeAtOrBelow(FrozenAtCelsius, seconds, initial, target, final, responseSeconds);
            return Math.Min(seconds, Math.Max(0.0, seconds - refrigerated) +
                Math.Max(0.0, refrigerated - frozen) * RefrigeratedRate + frozen * FrozenRate);
        }

        private static double TimeAtOrBelow(double threshold, double seconds, double initial, double target,
            double final, double responseSeconds)
        {
            if (responseSeconds == 0.0) return target <= threshold ? seconds : 0.0;
            if (initial <= threshold && final <= threshold) return seconds;
            if (initial > threshold && final > threshold) return 0.0;
            double crossing = responseSeconds * (Math.Log(Math.Abs(initial - target)) - Math.Log(Math.Abs(threshold - target)));
            crossing = Math.Max(0.0, Math.Min(seconds, crossing));
            return initial <= threshold ? crossing : seconds - crossing;
        }

        private static bool Temperature(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= FoodState.AbsoluteZeroCelsius;
        private static bool Rate(double value) => !double.IsNaN(value) && value >= 0.0 && value <= 1.0;
    }
}
