using System;

namespace ZeroStarRestaurant.Cooking
{
    public sealed class CookingState
    {
        public CookingProfile Profile { get; }
        public double EquivalentSeconds { get; private set; }
        // 100% means cooked; values above 100% communicate overcooking, up to the burnt threshold.
        public double ProgressPercent => EquivalentSeconds / Profile.CookedAtSeconds * 100.0;
        public CookingStage Stage => EquivalentSeconds >= Profile.BurntAtSeconds ? CookingStage.Burnt :
            EquivalentSeconds >= Profile.OvercookedAtSeconds ? CookingStage.Overcooked :
            EquivalentSeconds >= Profile.CookedAtSeconds ? CookingStage.Cooked :
            EquivalentSeconds > 0.0 ? CookingStage.Undercooked : CookingStage.Raw;

        public CookingState(CookingProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        // Called only by FoodState's single advance, integrating its entire thermal trajectory.
        internal void Advance(double seconds, double initialTemperature, double targetTemperature, double responseSeconds)
        {
            if (seconds == 0.0 || Stage == CookingStage.Burnt)
                return;
            double minimum = Profile.MinimumTemperatureCelsius;
            double finalTemperature = responseSeconds == 0.0 ? targetTemperature :
                initialTemperature * Math.Exp(-seconds / responseSeconds) +
                targetTemperature * (1.0 - Math.Exp(-seconds / responseSeconds));
            if (initialTemperature <= minimum && finalTemperature <= minimum)
                return;

            double start = 0.0;
            double end = seconds;
            double warmStart = initialTemperature;
            // An exponential toward a fixed target crosses the minimum at most once.
            if (initialTemperature < minimum && targetTemperature > minimum)
            {
                start = responseSeconds * Math.Log((targetTemperature - initialTemperature) / (targetTemperature - minimum));
                warmStart = minimum;
            }
            else if (initialTemperature > minimum && targetTemperature < minimum)
                end = Math.Min(seconds, responseSeconds * Math.Log((initialTemperature - targetTemperature) / (minimum - targetTemperature)));

            double duration = Math.Max(0.0, end - start);
            if (duration == 0.0)
                return;
            // Average of T(t)-minimum over the above-threshold segment. The product with
            // duration is capped before multiplication, so huge time jumps cannot overflow dose.
            double ratio = responseSeconds == 0.0 ? double.PositiveInfinity : duration / responseSeconds;
            double averageRetention = ratio < 1e-5 ? 1.0 - ratio * 0.5 + ratio * ratio / 6.0 :
                (1.0 - Math.Exp(-ratio)) / ratio;
            double averageRate = Math.Max(0.0, ((targetTemperature - minimum) +
                (warmStart - targetTemperature) * averageRetention) /
                (Profile.ReferenceTemperatureCelsius - minimum));
            double remaining = Profile.BurntAtSeconds - EquivalentSeconds;
            if (averageRate > 0.0)
                EquivalentSeconds = duration >= remaining / averageRate ? Profile.BurntAtSeconds :
                    EquivalentSeconds + duration * averageRate;
        }
    }
}
