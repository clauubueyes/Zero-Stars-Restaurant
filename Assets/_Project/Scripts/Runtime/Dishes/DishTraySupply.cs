using UnityEngine;

namespace ZeroStarRestaurant.Dishes
{
    // Compatibility for scenes saved before plates became purchasable. Never spawns utensils.
    // The scene adapter removes this retired component while preserving its support geometry.
    [DisallowMultipleComponent]
    public sealed class DishTraySupply : MonoBehaviour
    {
        public bool TryReplenish() => false;
    }
}
