using System;
using ZeroStarRestaurant.Cooking;

namespace ZeroStarRestaurant.Food
{
    public sealed class FoodState
    {
        public const double AbsoluteZeroCelsius = -273.15;
        public FoodProfile Profile { get; }
        public double AgeSeconds { get; private set; }
        public double DeteriorationSeconds { get; private set; }
        public double TemperatureCelsius { get; private set; }
        public bool IsContaminated { get; private set; }
        public CookingState Cooking { get; }
        public double FreshnessPercent => Math.Max(0.0, Math.Min(100.0,
            100.0 - DeteriorationSeconds / Profile.FreshnessLifetimeSeconds * 100.0));

        public FoodCondition Condition
        {
            get
            {
                double freshness = FreshnessPercent;
                if (freshness <= Profile.RottenAtPercent) return FoodCondition.Rotten;
                if (freshness <= Profile.SpoiledAtPercent) return FoodCondition.Spoiled;
                if (freshness < Profile.FreshMinimumPercent) return FoodCondition.Acceptable;
                return FoodCondition.Fresh;
            }
        }

        public FoodState(FoodProfile profile, double ageSeconds = 0.0, double freshnessPercent = 100.0,
            double temperatureCelsius = 21.0, bool isContaminated = false)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Cooking = profile.IsCookable ? new CookingState(profile.Cooking) : null;
            RequireNonNegativeFinite(ageSeconds, nameof(ageSeconds));
            RequireNonNegativeFinite(freshnessPercent, nameof(freshnessPercent));
            if (freshnessPercent > 100.0)
                throw new ArgumentOutOfRangeException(nameof(freshnessPercent));
            RequireTemperature(temperatureCelsius, nameof(temperatureCelsius));
            AgeSeconds = ageSeconds;
            DeteriorationSeconds = (100.0 - freshnessPercent) / 100.0 * profile.FreshnessLifetimeSeconds;
            TemperatureCelsius = temperatureCelsius;
            IsContaminated = isContaminated;
        }

        // Time and the deterioration rate are supplied by the caller, never read from a game clock.
        public void Advance(double elapsedSeconds, double ambientTemperatureCelsius, double deteriorationMultiplier = 1.0)
            => Advance(elapsedSeconds, new ThermalEnvironment(ambientTemperatureCelsius), deteriorationMultiplier);

        public void Advance(double elapsedSeconds, ThermalEnvironment environment, double deteriorationMultiplier = 1.0)
        {
            // Validate every argument before mutating any part of this instance.
            RequireNonNegativeFinite(elapsedSeconds, nameof(elapsedSeconds));
            // Revalidate even a default(struct), which bypasses the constructor.
            environment = new ThermalEnvironment(environment.TemperatureCelsius, environment.ResponseMultiplier, environment.AllowsCooking);
            RequireNonNegativeFinite(deteriorationMultiplier, nameof(deteriorationMultiplier));
            if (elapsedSeconds == 0.0)
                return;

            AgeSeconds = elapsedSeconds > double.MaxValue - AgeSeconds ? double.MaxValue : AgeSeconds + elapsedSeconds;
            double remaining = Profile.FreshnessLifetimeSeconds - DeteriorationSeconds;
            if (deteriorationMultiplier > 0.0 && remaining > 0.0)
                DeteriorationSeconds = elapsedSeconds >= remaining / deteriorationMultiplier
                    ? Profile.FreshnessLifetimeSeconds
                    : DeteriorationSeconds + elapsedSeconds * deteriorationMultiplier;

            // Analytic relaxation avoids frame-size dependent overshoot, including very large time jumps.
            double responseSeconds = Profile.ThermalResponseSeconds / environment.ResponseMultiplier;
            if (environment.AllowsCooking)
                Cooking?.Advance(elapsedSeconds, TemperatureCelsius, environment.TemperatureCelsius, responseSeconds);
            double retained = responseSeconds == 0.0 ? 0.0 : Math.Exp(-elapsedSeconds / responseSeconds);
            TemperatureCelsius = TemperatureCelsius * retained + environment.TemperatureCelsius * (1.0 - retained);
        }

        public void SetTemperature(double temperatureCelsius)
        {
            RequireTemperature(temperatureCelsius, nameof(temperatureCelsius));
            TemperatureCelsius = temperatureCelsius;
        }

        public void Contaminate() => IsContaminated = true;

        private static void RequireNonNegativeFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
                throw new ArgumentOutOfRangeException(parameter, "A non-negative finite value is required.");
        }

        private static void RequireTemperature(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < AbsoluteZeroCelsius)
                throw new ArgumentOutOfRangeException(parameter, "Temperature must be finite and at least absolute zero.");
        }
    }
}
