using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Orders
{
    public sealed class DishSnapshot
    {
        public Guid InstanceId { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<IngredientSnapshot> Ingredients { get; }
        public double MinimumFreshnessPercent { get; }
        public double MeanFreshnessPercent { get; }
        public bool ContainsSpoiled { get; }
        public bool ContainsRotten { get; }
        public bool ContainsSpoiledOrRotten => ContainsSpoiled || ContainsRotten;
        public bool ContainsContamination { get; }
        public long TotalIngredientCostCents { get; }
        internal DishSnapshot(DeliveryContents delivery)
        {
            InstanceId = delivery.InstanceId;
            DefinitionId = delivery.RecognizedRecipe?.Id;
            DisplayName = delivery.RecognizedRecipe?.DisplayName ?? (delivery.Dish != null ? "Custom Dish" : "Loose ingredients");
            var ingredients = new List<IngredientSnapshot>();
            double sum = 0, minimum = 100; long cost = 0;
            foreach (FoodState food in delivery.Foods)
            {
                var snapshot = new IngredientSnapshot(food); ingredients.Add(snapshot);
                sum += snapshot.FreshnessPercent; minimum = Math.Min(minimum, snapshot.FreshnessPercent);
                ContainsContamination |= snapshot.IsContaminated;
                ContainsSpoiled |= snapshot.Condition == FoodCondition.Spoiled;
                ContainsRotten |= snapshot.Condition == FoodCondition.Rotten;
                cost = checked(cost + snapshot.Profile.ReferenceCostCents);
            }
            Ingredients = ingredients.AsReadOnly(); MinimumFreshnessPercent = minimum;
            MeanFreshnessPercent = sum / ingredients.Count; TotalIngredientCostCents = cost;
        }
    }
}
