using System;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Cooking
{
    public readonly struct ThermalEnvironment
    {
        public double TemperatureCelsius { get; }
        public double ResponseMultiplier { get; }
        public bool AllowsCooking { get; }

        public ThermalEnvironment(double temperatureCelsius, double responseMultiplier = 1.0, bool allowsCooking = false)
        {
            if (double.IsNaN(temperatureCelsius) || double.IsInfinity(temperatureCelsius) ||
                temperatureCelsius < FoodState.AbsoluteZeroCelsius)
                throw new ArgumentOutOfRangeException(nameof(temperatureCelsius));
            if (double.IsNaN(responseMultiplier) || double.IsInfinity(responseMultiplier) || responseMultiplier <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(responseMultiplier));
            TemperatureCelsius = temperatureCelsius;
            ResponseMultiplier = responseMultiplier;
            AllowsCooking = allowsCooking;
        }
    }
}
