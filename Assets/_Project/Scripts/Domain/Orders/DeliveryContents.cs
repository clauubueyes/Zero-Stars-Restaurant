using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Orders
{
    // References the actual submitted units. A loose delivery needs no finalized DishState.
    public sealed class DeliveryContents
    {
        public Guid InstanceId { get; }
        public DishState Dish { get; }
        public IReadOnlyList<FoodState> Foods { get; }
        public DishProfile RecognizedRecipe { get; }
        public bool IsAvailable
        {
            get
            {
                if (Dish != null && (!Dish.IsFinalized || Dish.IsDisposed || Dish.IsSold)) return false;
                foreach (FoodState food in Foods)
                    if (food.IsSold || (food.AssemblyOwnerId.HasValue && (Dish == null || !Contains(Dish.Components, food)))) return false;
                return true;
            }
        }

        public DeliveryContents(DishState dish, IEnumerable<FoodState> additionalFoods = null)
        {
            if (dish == null || !dish.IsFinalized || dish.IsDisposed || dish.Components.Count == 0)
                throw new ArgumentException("An intact finalized dish is required.", nameof(dish));
            Dish = dish; InstanceId = dish.InstanceId.Value;
            var originals = new List<FoodState>(dish.Components);
            if (additionalFoods != null) originals.AddRange(additionalFoods);
            var ids = new HashSet<Guid>();
            foreach (FoodState food in originals)
                if (food == null || !ids.Add(food.InstanceId)) throw new ArgumentException("Delivery units must be distinct.", nameof(additionalFoods));
            Foods = originals.AsReadOnly(); RecognizedRecipe = originals.Count == dish.Components.Count ? dish.RecognizedDefinition : null;
        }
        private static bool Contains(IReadOnlyList<FoodState> foods, FoodState unit)
        { foreach (FoodState food in foods) if (ReferenceEquals(food, unit)) return true; return false; }

        public DeliveryContents(IEnumerable<FoodState> foods, IReadOnlyList<DishProfile> recipes = null, bool isStack = false)
        {
            if (foods == null) throw new ArgumentNullException(nameof(foods));
            var originals = new List<FoodState>(foods); var ids = new HashSet<Guid>();
            if (originals.Count == 0) throw new ArgumentException("A delivery cannot be empty.", nameof(foods));
            foreach (FoodState food in originals)
                if (food == null || !ids.Add(food.InstanceId)) throw new ArgumentException("Delivery units must be distinct.", nameof(foods));
            Foods = originals.AsReadOnly(); InstanceId = originals.Count == 1 ? originals[0].InstanceId : Guid.NewGuid();
            if (recipes != null)
                foreach (DishProfile recipe in recipes)
                    if (recipe != null && recipe.Matches(Foods, isStack)) { RecognizedRecipe = recipe; break; }
        }

        internal void MarkSold()
        {
            Dish?.MarkSold();
            foreach (FoodState food in Foods) food.MarkSold();
        }
    }
}
