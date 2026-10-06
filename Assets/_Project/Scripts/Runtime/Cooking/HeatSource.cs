using UnityEngine;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Cooking
{
    // A source describes a local environment; it never advances food time or owns food state.
    public abstract class HeatSource : MonoBehaviour
    {
        public abstract bool TryGetEnvironment(FoodItem food, out ThermalEnvironment environment);
    }
}
