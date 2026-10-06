using System;
using ZeroStarRestaurant.Cooking;

namespace ZeroStarRestaurant.Food
{
    // Immutable configuration copied from an authoring asset, with no Unity references.
    public sealed class FoodProfile
    {
        public string Id { get; }
        public string DisplayName { get; }
        public FoodCategory Category { get; }
        public int ReferenceCostCents { get; }
        public double FreshnessLifetimeSeconds { get; }
        public double ThermalResponseSeconds { get; }
        public double FreshMinimumPercent { get; }
        public double SpoiledAtPercent { get; }
        public double RottenAtPercent { get; }
        public CookingProfile Cooking { get; }
        public bool IsCookable => Cooking != null;

        public FoodProfile(string id, string displayName, FoodCategory category,
            int referenceCostCents, double freshnessLifetimeSeconds, double thermalResponseSeconds,
            double freshMinimumPercent = 80.0, double spoiledAtPercent = 30.0, double rottenAtPercent = 5.0,
            CookingProfile cooking = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                throw new ArgumentException("A stable, non-empty identifier without surrounding whitespace is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A display name is required.", nameof(displayName));
            if (!Enum.IsDefined(typeof(FoodCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category));
            if (referenceCostCents < 0)
                throw new ArgumentOutOfRangeException(nameof(referenceCostCents));
            RequirePositiveFinite(freshnessLifetimeSeconds, nameof(freshnessLifetimeSeconds));
            RequirePositiveFinite(thermalResponseSeconds, nameof(thermalResponseSeconds));
            if (!IsPercent(freshMinimumPercent) || !IsPercent(spoiledAtPercent) || !IsPercent(rottenAtPercent) ||
                rottenAtPercent >= spoiledAtPercent || spoiledAtPercent >= freshMinimumPercent)
                throw new ArgumentException("Condition thresholds must satisfy 0 <= rotten < spoiled < fresh <= 100.");

            Id = id;
            DisplayName = displayName;
            Category = category;
            ReferenceCostCents = referenceCostCents;
            FreshnessLifetimeSeconds = freshnessLifetimeSeconds;
            ThermalResponseSeconds = thermalResponseSeconds;
            FreshMinimumPercent = freshMinimumPercent;
            SpoiledAtPercent = spoiledAtPercent;
            RottenAtPercent = rottenAtPercent;
            Cooking = cooking;
        }

        private static bool IsPercent(double value) => !double.IsNaN(value) && value >= 0.0 && value <= 100.0;

        private static void RequirePositiveFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                throw new ArgumentOutOfRangeException(parameter, "A positive finite value is required.");
        }
    }
}
