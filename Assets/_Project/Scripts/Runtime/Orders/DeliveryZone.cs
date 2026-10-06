using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Orders
{
    public sealed class DeliveryZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider _zone;
        [SerializeField] private BoxCollider _support;
        [SerializeField] private CustomerServiceLoop _service;
        [SerializeField, Min(0.001f)] private float _placementTolerance = 0.06f;
        [SerializeField, Min(0f)] private float _maximumPlacementSpeed = 0.5f;
        [SerializeField, Range(0.01f, 1f)] private float _minimumFootprintOverlap = 0.2f;
        private readonly HashSet<DishItem> _processedPlacements = new HashSet<DishItem>();
        private void Awake()
        {
            if (_zone == null || !_zone.isTrigger || _support == null || _support.isTrigger || _service == null)
            { Debug.LogError("DeliveryZone needs a trigger, solid support and customer service.", this); enabled = false; }
        }
        private void FixedUpdate() => Poll();
        private void OnDisable() => _processedPlacements.Clear();
        public void Poll()
        {
            if (!isActiveAndEnabled || _zone == null || !_zone.enabled || !_zone.isTrigger || !_zone.gameObject.activeInHierarchy ||
                _support == null || !_support.enabled || !_support.gameObject.activeInHierarchy || _service == null)
            { _processedPlacements.Clear(); return; }
            Physics.SyncTransforms();
            Vector3 scale = _zone.transform.lossyScale;
            Vector3 half = Vector3.Scale(_zone.size * 0.5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            var present = new HashSet<DishItem>();
            foreach (Collider collider in Physics.OverlapBox(_zone.transform.TransformPoint(_zone.center), half,
                         _zone.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                DishItem dish = collider.GetComponentInParent<DishItem>();
                if (dish == null || !dish.isActiveAndEnabled || dish.State == null || !dish.State.IsFinalized || dish.State.IsSold || !dish.IsIntact) continue;
                Pickup pickup = dish.GetComponent<Pickup>();
                if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || pickup.Body == null || pickup.Body.isKinematic) continue;
                present.Add(dish);
            }
            // A rejected dish must leave (or be picked up) before it can be submitted to another customer.
            _processedPlacements.RemoveWhere(dish => dish == null || !present.Contains(dish));
            if (_service.Visit == null || _service.Visit.Stage != CustomerStage.Wait) return;
            var ordered = new List<DishItem>(present);
            ordered.Sort((a, b) => a.State.InstanceId.Value.CompareTo(b.State.InstanceId.Value));
            foreach (DishItem dish in ordered)
            {
                if (_processedPlacements.Contains(dish)) continue;
                Rigidbody body = dish.GetComponent<Rigidbody>();
                Bounds bounds = dish.GetComponent<BoxCollider>().bounds;
                float bottomGap = bounds.min.y - _support.bounds.max.y;
                if (!HasPlacementOverlap(bounds) || bottomGap < -_placementTolerance || bottomGap > _placementTolerance ||
                    body.linearVelocity.sqrMagnitude > _maximumPlacementSpeed * _maximumPlacementSpeed || body.angularVelocity.sqrMagnitude > 4f) continue;
                if (_service.TryDeliver(dish)) { _processedPlacements.Add(dish); break; }
            }
        }

        private bool HasPlacementOverlap(Bounds dish)
        {
            // Horizontal greybox support: compare footprints, never require the dish's center.
            // The actual collider must also overlap the trigger queried in Poll.
            Bounds pad = _support.bounds;
            float width = Mathf.Max(0f, Mathf.Min(dish.max.x, pad.max.x) - Mathf.Max(dish.min.x, pad.min.x));
            float depth = Mathf.Max(0f, Mathf.Min(dish.max.z, pad.max.z) - Mathf.Max(dish.min.z, pad.min.z));
            float smallerArea = Mathf.Min(dish.size.x * dish.size.z, pad.size.x * pad.size.z);
            // Scale with both small and large dishes, but reject a corner/edge merely grazing the pad.
            return smallerArea > 0f && width * depth >= smallerArea * Mathf.Clamp(_minimumFootprintOverlap, 0.01f, 1f);
        }
    }
}
