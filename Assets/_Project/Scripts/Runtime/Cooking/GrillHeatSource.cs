using UnityEngine;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Cooking
{
    [DisallowMultipleComponent]
    public sealed class GrillHeatSource : HeatSource
    {
        [SerializeField, Tooltip("Thin trigger box immediately above the solid grill surface.")]
        private BoxCollider _effectiveZone;
        [SerializeField, Min(-273.15f)] private float _temperatureCelsius = 180f;
        [SerializeField, Min(0f), Tooltip("Thermal response multiplier; zero disables heat transfer.")]
        private float _transferMultiplier = 4f;

        public override bool TryGetEnvironment(FoodItem food, out ThermalEnvironment environment)
        {
            environment = default;
            if (!isActiveAndEnabled || food == null || !food.isActiveAndEnabled ||
                _effectiveZone == null || !_effectiveZone.enabled || !_effectiveZone.gameObject.activeInHierarchy ||
                !_effectiveZone.isTrigger || !Finite(_temperatureCelsius) || _temperatureCelsius < -273.15f ||
                !Finite(_transferMultiplier) || _transferMultiplier <= 0f)
                return false;

            Transform zone = _effectiveZone.transform;
            Vector3 scale = zone.lossyScale;
            Vector3 halfSize = Vector3.Scale(_effectiveZone.size * 0.5f,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            // Query the actual oriented volume each time. No enter/exit registry can become stale,
            // and multiple colliders on a unit still produce a single environment/result.
            Collider[] overlaps = Physics.OverlapBox(zone.TransformPoint(_effectiveZone.center), halfSize,
                zone.rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in overlaps)
            {
                if (collider.GetComponentInParent<FoodItem>() != food)
                    continue;
                environment = new ThermalEnvironment(_temperatureCelsius, _transferMultiplier, allowsCooking: true);
                return true;
            }
            return false;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
