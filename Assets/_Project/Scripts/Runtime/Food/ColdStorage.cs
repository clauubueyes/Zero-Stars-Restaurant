using UnityEngine;
using ZeroStarRestaurant.Cooking;

namespace ZeroStarRestaurant.Food
{
    // An open greybox cabinet describes cold air. It never owns food or advances time.
    [DisallowMultipleComponent]
    public sealed class ColdStorage : HeatSource
    {
        [SerializeField, Tooltip("Trigger describing the clear interior, excluding the solid shell.")]
        private BoxCollider _interior;
        [SerializeField, Min(-273.15f)] private float _temperatureCelsius = 4f;
        [SerializeField, Min(0f), Tooltip("Zero disables thermal transfer; disabled storage falls back to ambient.")]
        private float _transferMultiplier = 2f;

        public override bool TryGetEnvironment(FoodItem food, out ThermalEnvironment environment)
        {
            environment = default;
            if (!isActiveAndEnabled || food == null || !food.isActiveAndEnabled || _interior == null ||
                !_interior.enabled || !_interior.isTrigger || !_interior.gameObject.activeInHierarchy ||
                !Finite(_temperatureCelsius) || _temperatureCelsius < FoodState.AbsoluteZeroCelsius ||
                !Finite(_transferMultiplier) || _transferMultiplier <= 0f)
                return false;
            // Query the current unit position in the oriented interior. This also works for the
            // original FoodItems inside a finalized Dish, whose individual colliders are disabled.
            Vector3 point = _interior.transform.InverseTransformPoint(food.transform.position) - _interior.center;
            Vector3 half = _interior.size * 0.5f;
            if (Mathf.Abs(point.x) > half.x || Mathf.Abs(point.y) > half.y || Mathf.Abs(point.z) > half.z)
                return false;
            environment = new ThermalEnvironment(_temperatureCelsius, _transferMultiplier);
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
