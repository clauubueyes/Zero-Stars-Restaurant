using System;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Orders
{
    // Historical delivery evidence; immutable values from the concrete unit, with its real identity.
    public sealed class IngredientSnapshot
    {
        public Guid InstanceId { get; }
        public FoodProfile Profile { get; }
        public double FreshnessPercent { get; }
        public FoodCondition Condition { get; }
        public bool IsContaminated { get; }
        public double TemperatureCelsius { get; }
        public double AgeSeconds { get; }
        public CookingStage? CookingStage { get; }
        public double? CookingDoseSeconds { get; }
        internal IngredientSnapshot(FoodState food)
        {
            InstanceId = food.InstanceId; Profile = food.Profile;
            FreshnessPercent = food.FreshnessPercent; Condition = food.Condition;
            IsContaminated = food.IsContaminated; TemperatureCelsius = food.TemperatureCelsius;
            AgeSeconds = food.AgeSeconds; CookingStage = food.Cooking?.Stage;
            CookingDoseSeconds = food.Cooking?.EquivalentSeconds;
        }
    }
}
