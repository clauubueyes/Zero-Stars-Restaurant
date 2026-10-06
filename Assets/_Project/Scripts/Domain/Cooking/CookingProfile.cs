using System;

namespace ZeroStarRestaurant.Cooking
{
    // Seconds are equivalent exposure at ReferenceTemperatureCelsius, not wall-clock time.
    public sealed class CookingProfile
    {
        public double MinimumTemperatureCelsius { get; }
        public double ReferenceTemperatureCelsius { get; }
        public double CookedAtSeconds { get; }
        public double OvercookedAtSeconds { get; }
        public double BurntAtSeconds { get; }

        public CookingProfile(double minimumTemperatureCelsius, double referenceTemperatureCelsius,
            double cookedAtSeconds, double overcookedAtSeconds, double burntAtSeconds)
        {
            if (!Finite(minimumTemperatureCelsius) || minimumTemperatureCelsius < Food.FoodState.AbsoluteZeroCelsius ||
                !Finite(referenceTemperatureCelsius) || referenceTemperatureCelsius <= minimumTemperatureCelsius)
                throw new ArgumentException("Cooking temperatures must be finite, with reference greater than minimum.");
            if (!Finite(cookedAtSeconds) || !Finite(overcookedAtSeconds) || !Finite(burntAtSeconds) ||
                cookedAtSeconds <= 0.0 || overcookedAtSeconds <= cookedAtSeconds || burntAtSeconds <= overcookedAtSeconds)
                throw new ArgumentException("Cooking thresholds must satisfy 0 < cooked < overcooked < burnt.");
            MinimumTemperatureCelsius = minimumTemperatureCelsius;
            ReferenceTemperatureCelsius = referenceTemperatureCelsius;
            CookedAtSeconds = cookedAtSeconds;
            OvercookedAtSeconds = overcookedAtSeconds;
            BurntAtSeconds = burntAtSeconds;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
