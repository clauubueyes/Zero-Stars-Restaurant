using System;
using UnityEngine;

namespace ZeroStarRestaurant.Food
{
    [DisallowMultipleComponent]
    public sealed class FoodItem : MonoBehaviour
    {
        [SerializeField] private FoodDefinition _definition;
        [SerializeField, Min(0f)] private float _initialAgeSeconds;
        [SerializeField, Range(0f, 100f)] private float _initialFreshnessPercent = 100f;
        [SerializeField, Min(-273.15f)] private float _initialTemperatureCelsius = 21f;
        [SerializeField] private bool _initiallyContaminated;

        public FoodDefinition Definition => _definition;
        public FoodState State { get; private set; }
        private Bounds? _retiredThermalBounds;

        // Use this ingredient's original geometry, never the entire Dish interaction proxy.
        internal bool TryGetContactBounds(out Bounds bounds)
        {
            bounds = default;
            if (_retiredThermalBounds.HasValue)
            {
                Vector3[] points = Corners(_retiredThermalBounds.Value);
                bounds = new Bounds(transform.TransformPoint(points[0]), Vector3.zero);
                foreach (Vector3 point in points) bounds.Encapsulate(transform.TransformPoint(point));
                return true;
            }
            bool found = false;
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy) continue;
                if (!found) bounds = collider.bounds; else bounds.Encapsulate(collider.bounds);
                found = true;
            }
            return found;
        }

        // Retired colliders no longer participate in Physics queries. Keep only their geometry,
        // in the ingredient's own coordinates, so temperature follows the original moving unit.
        internal void CaptureThermalGeometry()
        {
            Bounds? local = null;
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                BoxCollider box = collider as BoxCollider;
                Bounds shape = box != null ? new Bounds(box.center, box.size) : collider.bounds;
                foreach (Vector3 point in Corners(shape))
                {
                    Vector3 world = box != null ? box.transform.TransformPoint(point) : point;
                    Vector3 position = transform.InverseTransformPoint(world);
                    if (!local.HasValue) local = new Bounds(position, Vector3.zero);
                    else { Bounds accumulated = local.Value; accumulated.Encapsulate(position); local = accumulated; }
                }
            }
            _retiredThermalBounds = local;
        }

        internal bool RetiredGeometryOverlaps(BoxCollider zone)
        {
            if (!_retiredThermalBounds.HasValue) return false;
            Vector3[] food = Corners(_retiredThermalBounds.Value);
            Vector3[] source = Corners(new Bounds(zone.center, zone.size));
            for (int i = 0; i < 8; i++) { food[i] = transform.TransformPoint(food[i]); source[i] = zone.transform.TransformPoint(source[i]); }
            Vector3[] a = { transform.right, transform.up, transform.forward };
            Vector3[] b = { zone.transform.right, zone.transform.up, zone.transform.forward };
            foreach (Vector3 axis in a) if (Separated(food, source, axis)) return false;
            foreach (Vector3 axis in b) if (Separated(food, source, axis)) return false;
            foreach (Vector3 first in a) foreach (Vector3 second in b)
                if (Separated(food, source, Vector3.Cross(first, second))) return false;
            return true;
        }

        private static bool Separated(Vector3[] a, Vector3[] b, Vector3 axis)
        {
            if (axis.sqrMagnitude < .000001f) return false;
            float minA = float.PositiveInfinity, maxA = float.NegativeInfinity;
            float minB = float.PositiveInfinity, maxB = float.NegativeInfinity;
            foreach (Vector3 point in a) { float value = Vector3.Dot(point, axis); minA = Mathf.Min(minA, value); maxA = Mathf.Max(maxA, value); }
            foreach (Vector3 point in b) { float value = Vector3.Dot(point, axis); minB = Mathf.Min(minB, value); maxB = Mathf.Max(maxB, value); }
            return maxA < minB || maxB < minA;
        }

        private static Vector3[] Corners(Bounds bounds)
        {
            var points = new Vector3[8];
            for (int i = 0; i < 8; i++) points[i] = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            return points;
        }

        private void Awake()
        {
            TryInitialize();
        }

        // Allows an inactive purchase prefab to prepare its own state before payment/activation.
        // Repeated calls preserve the same state; they never restore freshness or identity.
        public bool TryInitialize()
        {
            if (State != null) return true;
            if (_definition == null)
            {
                Debug.LogError("FoodItem needs a FoodDefinition assigned before activation.", this);
                enabled = false;
                return false;
            }
            try
            {
                State = new FoodState(_definition.CreateProfile(), _initialAgeSeconds,
                    _initialFreshnessPercent, _initialTemperatureCelsius, _initiallyContaminated);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError("Invalid food configuration: " + exception.Message, this);
                enabled = false;
                return false;
            }
            return true;
        }
    }
}
