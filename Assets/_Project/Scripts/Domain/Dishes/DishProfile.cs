using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Dishes
{
    public sealed class DishProfile
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> IngredientDefinitionIds { get; }
        public bool RequiresStack { get; }

        public DishProfile(string id, string displayName, IEnumerable<string> ingredientDefinitionIds, bool requiresStack = true)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new ArgumentException("A stable dish definition ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A display name is required.", nameof(displayName));
            if (ingredientDefinitionIds == null) throw new ArgumentNullException(nameof(ingredientDefinitionIds));
            var ids = new List<string>(ingredientDefinitionIds);
            if (ids.Count == 0) throw new ArgumentException("Recognition needs at least one ingredient.", nameof(ingredientDefinitionIds));
            foreach (string ingredient in ids)
                if (string.IsNullOrWhiteSpace(ingredient) || ingredient != ingredient.Trim())
                    throw new ArgumentException("Ingredient definition IDs must be non-empty and stable.", nameof(ingredientDefinitionIds));
            Id = id; DisplayName = displayName;
            IngredientDefinitionIds = ids.AsReadOnly(); RequiresStack = requiresStack;
        }

        public bool Matches(IReadOnlyList<FoodState> components, bool isStack)
        {
            if (components == null || components.Count != IngredientDefinitionIds.Count || (RequiresStack && !isStack)) return false;
            for (int index = 0; index < components.Count; index++)
                if (components[index] == null || components[index].Profile.Id != IngredientDefinitionIds[index]) return false;
            return true;
        }
    }
}
