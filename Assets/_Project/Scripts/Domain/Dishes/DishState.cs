using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Dishes
{
    public sealed class DishState : IDisposable
    {
        private readonly Guid _ownershipId = Guid.NewGuid();
        private readonly List<FoodState> _components = new List<FoodState>();
        public IReadOnlyList<FoodState> Components { get; }
        public Guid? InstanceId => IsFinalized ? _ownershipId : (Guid?)null;
        public bool IsFinalized { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsStack { get; private set; }
        public DishProfile RecognizedDefinition { get; private set; }
        public string DisplayName => RecognizedDefinition?.DisplayName ?? "Custom Dish";

        public double? MinimumFreshnessPercent
        {
            get
            {
                if (_components.Count == 0) return null;
                double minimum = 100.0;
                foreach (FoodState food in _components) minimum = Math.Min(minimum, food.FreshnessPercent);
                return minimum;
            }
        }
        public double? MeanFreshnessPercent
        {
            get
            {
                if (_components.Count == 0) return null;
                double mean = 0.0;
                foreach (FoodState food in _components) mean += food.FreshnessPercent / _components.Count;
                return mean;
            }
        }
        public bool ContainsContamination
        {
            get { foreach (FoodState food in _components) if (food.IsContaminated) return true; return false; }
        }
        public bool ContainsSpoiledOrRotten
        {
            get { foreach (FoodState food in _components) if (food.Condition == FoodCondition.Spoiled || food.Condition == FoodCondition.Rotten) return true; return false; }
        }
        public long TotalIngredientCostCents
        {
            get { long total = 0; foreach (FoodState food in _components) total += food.Profile.ReferenceCostCents; return total; }
        }

        public DishState() => Components = _components.AsReadOnly();

        public bool TryAdd(FoodState food)
        {
            if (IsFinalized || IsDisposed || food == null) return false;
            foreach (FoodState existing in _components) if (existing.InstanceId == food.InstanceId) return false;
            if (!food.TryClaimAssembly(_ownershipId)) return false;
            _components.Add(food); IsStack = false; return true;
        }

        public bool TryRemove(Guid foodId)
        {
            if (IsFinalized || IsDisposed) return false;
            for (int index = 0; index < _components.Count; index++)
            {
                if (_components[index].InstanceId != foodId) continue;
                _components[index].ReleaseAssembly(_ownershipId);
                _components.RemoveAt(index); IsStack = false; return true;
            }
            return false;
        }

        // The adapter supplies physical order; validation rejects missing/duplicate/foreign identities atomically.
        public bool TrySetOrder(IReadOnlyList<Guid> orderedIds, bool isStack)
        {
            if (IsFinalized || IsDisposed || orderedIds == null || orderedIds.Count != _components.Count) return false;
            var reordered = new List<FoodState>();
            var seen = new HashSet<Guid>();
            foreach (Guid id in orderedIds)
            {
                if (!seen.Add(id)) return false;
                FoodState found = _components.Find(food => food.InstanceId == id);
                if (found == null) return false;
                reordered.Add(found);
            }
            _components.Clear(); _components.AddRange(reordered); IsStack = isStack; return true;
        }

        public DishProfile Recognize(IReadOnlyList<DishProfile> definitions)
        {
            if (definitions == null) return null;
            foreach (DishProfile definition in definitions)
                if (definition != null && definition.Matches(Components, IsStack)) return definition;
            return null;
        }

        public bool TryFinalize(IReadOnlyList<DishProfile> definitions)
        {
            if (IsFinalized || IsDisposed || _components.Count == 0) return false;
            RecognizedDefinition = Recognize(definitions);
            IsFinalized = true;
            return true;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            foreach (FoodState food in _components) food.ReleaseAssembly(_ownershipId);
            IsDisposed = true;
        }
    }
}
