using System;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Hygiene
{
    // Deterministic contact doses; no clock, Unity, dirt, payment or reputation dependency.
    public sealed class FoodSafetyPolicy
    {
        public double RawMeatIntensity { get; }
        public double FoodToSurfaceFraction { get; }
        public double SurfaceToFoodFraction { get; }
        public FoodSafetyPolicy(double rawMeatIntensity = .8, double foodToSurfaceFraction = .5, double surfaceToFoodFraction = .5)
        {
            ContaminationState.RequireUnit(rawMeatIntensity);
            ContaminationState.RequireUnit(foodToSurfaceFraction);
            ContaminationState.RequireUnit(surfaceToFoodFraction);
            RawMeatIntensity = rawMeatIntensity; FoodToSurfaceFraction = foodToSurfaceFraction;
            SurfaceToFoodFraction = surfaceToFoodFraction;
        }

        public ContaminationTrace[] FoodExposure(FoodState food)
        {
            if (food == null) throw new ArgumentNullException(nameof(food));
            var exposure = new ContaminationState();
            foreach (var trace in food.Contamination.Snapshot()) exposure.Receive(trace);
            if (food.Profile.Category == FoodCategory.Meat && food.Cooking != null &&
                (food.Cooking.Stage == CookingStage.Raw || food.Cooking.Stage == CookingStage.Undercooked))
                exposure.Receive(new ContaminationTrace(ContaminationKind.RawMeat, food.InstanceId,
                    "Raw/undercooked meat", RawMeatIntensity, food.InstanceId, food.Profile.Id, food.Profile.Category));
            return exposure.Snapshot();
        }

        // Capture both donors before calling: a recipient cannot emit its newly received dose in the same event.
        public void TransferContact(FoodState food, ContaminationState surface, Guid surfaceId, string surfaceName,
            ContaminationTrace[] foodExposure, ContaminationTrace[] surfaceExposure)
        {
            if (food == null || surface == null || foodExposure == null || surfaceExposure == null)
                throw new ArgumentNullException("Contact requires original states and captured exposure.");
            if (surfaceId == Guid.Empty || string.IsNullOrWhiteSpace(surfaceName)) throw new ArgumentException("Surface identity is required.");
            if (food.IsSold) return;
            foreach (var trace in foodExposure)
                surface.Receive(trace.Transfer(FoodToSurfaceFraction, food.InstanceId, food.Profile.DisplayName));
            foreach (var trace in surfaceExposure)
                food.Contamination.Receive(trace.Transfer(SurfaceToFoodFraction, surfaceId, surfaceName));
        }

        public void TransferContact(FoodState food, ContaminationState surface, Guid surfaceId, string surfaceName)
            => TransferContact(food, surface, surfaceId, surfaceName, FoodExposure(food), surface.Snapshot());
    }
}
