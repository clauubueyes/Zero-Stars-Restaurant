using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Dishes;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderOffer
    {
        public DishProfile Dish { get; }
        public int SalePriceCents { get; }
        public IReadOnlyList<int> IngredientWeights { get; }
        public OrderOffer(DishProfile dish, int salePriceCents, IEnumerable<int> ingredientWeights = null)
        {
            Dish = dish ?? throw new ArgumentNullException(nameof(dish));
            if (salePriceCents <= 0) throw new ArgumentOutOfRangeException(nameof(salePriceCents));
            SalePriceCents = salePriceCents;
            var weights = ingredientWeights != null ? new List<int>(ingredientWeights) : new List<int>();
            if (ingredientWeights == null) foreach (string id in dish.IngredientDefinitionIds) weights.Add(1);
            if (weights.Count != dish.IngredientDefinitionIds.Count) throw new ArgumentException("One weight per expected ingredient is required.", nameof(ingredientWeights));
            foreach (int weight in weights) if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(ingredientWeights));
            // Repeated ingredients must have the same value, regardless of which identical unit arrived.
            for (int i = 0; i < weights.Count; i++) for (int j = 0; j < i; j++)
                if (dish.IngredientDefinitionIds[i] == dish.IngredientDefinitionIds[j] && weights[i] != weights[j])
                    throw new ArgumentException("Repeated ingredient IDs need equal weights.", nameof(ingredientWeights));
            IngredientWeights = weights.AsReadOnly();
        }
    }
}
