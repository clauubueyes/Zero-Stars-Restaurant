using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Orders
{
    [DisallowMultipleComponent]
    public sealed class DeliveryZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider _zone;
        [SerializeField] private BoxCollider _support;
        [SerializeField] private CustomerServiceLoop _service;
        [SerializeField, Min(0.001f)] private float _placementTolerance = 0.06f;
        [SerializeField, Min(0f)] private float _maximumPlacementSpeed = 0.5f;
        [SerializeField, Range(0.01f, 1f)] private float _minimumFootprintOverlap = 0.2f;
        private readonly Dictionary<DishItem, Guid> _processedPlacements = new Dictionary<DishItem, Guid>();
        public CustomerServiceLoop Service => _service;
        public BoxCollider Support => _support;
        public string PlacementMessage { get; private set; } = "";
        private void Awake()
        {
            if (_zone == null || !_zone.isTrigger || _support == null || _support.isTrigger || _service == null || _service.DeliveryZone != this)
            { Debug.LogError("DeliveryZone needs a trigger, solid support and customer service.", this); enabled = false; }
        }
        private void FixedUpdate() => Poll();
        private void OnDisable() { _processedPlacements.Clear(); PlacementMessage = ""; }
        public void Poll()
        {
            if (!isActiveAndEnabled || _zone == null || !_zone.enabled || !_zone.isTrigger || !_zone.gameObject.activeInHierarchy ||
                _support == null || !_support.enabled || !_support.gameObject.activeInHierarchy || _service == null)
            { _processedPlacements.Clear(); PlacementMessage = "Delivery unavailable"; return; }
            Physics.SyncTransforms();
            SensorGeometry(out Vector3 center, out Vector3 half);
            var present = new HashSet<DishItem>();
            bool unconfirmedFood = false, heldDish = false;
            foreach (Collider collider in Physics.OverlapBox(center, half,
                         _support.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                DishItem dish = collider.GetComponentInParent<DishItem>();
                if (dish == null)
                { unconfirmedFood |= collider.GetComponentInParent<FoodItem>() != null; continue; }
                if (!dish.isActiveAndEnabled || dish.State == null || !dish.State.IsFinalized || dish.State.IsSold || !dish.IsIntact) continue;
                Pickup pickup = dish.GetComponent<Pickup>();
                heldDish |= pickup != null && pickup.IsHeld;
                if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || pickup.Body == null || pickup.Body.isKinematic) continue;
                present.Add(dish);
            }
            // A rejection belongs to one order, never to every future customer visiting this pad.
            foreach (DishItem tracked in new List<DishItem>(_processedPlacements.Keys))
                if (tracked == null || !present.Contains(tracked)) _processedPlacements.Remove(tracked);
            PlacementMessage = unconfirmedFood ? "Delivery: look at the stack and press F to finalize" :
                heldDish ? "Delivery: release the dish on the green pad" : "Delivery: place a finalized dish on the green pad";
            if (_service.Visit == null || _service.Visit.Stage != CustomerStage.Wait)
            { PlacementMessage += "\nWaiting for a customer ready to receive"; return; }
            var ordered = new List<DishItem>(present);
            ordered.Sort((a, b) => a.State.InstanceId.Value.CompareTo(b.State.InstanceId.Value));
            foreach (DishItem dish in ordered)
            {
                if (TryDeliver(dish)) break;
            }
        }

        // One spatial gate for automatic detection and every API caller. No actor/input dependency.
        public bool TryDeliver(DishItem dish)
        {
            if (!isActiveAndEnabled || _service == null || _service.DeliveryZone != this ||
                _zone == null || !_zone.enabled || !_zone.isTrigger || !_zone.gameObject.activeInHierarchy ||
                _support == null || !_support.enabled || !_support.gameObject.activeInHierarchy ||
                dish == null || !dish.isActiveAndEnabled || dish.State == null || !dish.State.IsFinalized ||
                dish.State.IsSold || !dish.IsIntact) return Decline("Delivery: finalize an intact dish first (F)");
            if (_service.Visit != null && _processedPlacements.TryGetValue(dish, out Guid processedOrder) &&
                processedOrder == _service.Visit.Order.InstanceId)
                return Decline("Delivery: this order already evaluated the dish");
            Pickup pickup = dish.GetComponent<Pickup>();
            BoxCollider proxy = dish.GetComponent<BoxCollider>();
            Rigidbody body = dish.GetComponent<Rigidbody>();
            if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || body == null || body.isKinematic ||
                proxy == null || !proxy.enabled || proxy.isTrigger) return Decline("Delivery: release the dish first");
            Physics.SyncTransforms();
            SensorGeometry(out Vector3 center, out Vector3 half);
            bool overlapsSensor = false;
            foreach (Collider collider in Physics.OverlapBox(center, half, _support.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                if (collider == proxy) { overlapsSensor = true; break; }
            if (!overlapsSensor) return Decline("Delivery: place the dish on the green pad");
            Bounds bounds = BoundsInSupportSpace(proxy);
            float bottomGap = (bounds.min.y - (_support.center.y + _support.size.y * .5f)) * Mathf.Abs(_support.transform.lossyScale.y);
            if (!HasPlacementOverlap(bounds)) return Decline("Delivery: move the dish further onto the green pad");
            if (bottomGap < -_placementTolerance || bottomGap > _placementTolerance)
                return Decline("Delivery: rest the dish on the green pad");
            if (body.linearVelocity.sqrMagnitude > _maximumPlacementSpeed * _maximumPlacementSpeed || body.angularVelocity.sqrMagnitude > 4f)
                return Decline("Delivery: waiting for the dish to settle");
            if (!_service.TryAcceptDelivery(this, dish)) return Decline("Delivery: waiting for a customer ready to receive");
            _processedPlacements[dish] = _service.Visit.Order.InstanceId;
            PlacementMessage = dish.State.IsSold ? "Delivery accepted" :
                "Delivery rejected: ordered " + _service.LastResult.Evaluation.RequestedDish.DisplayName +
                "; delivered " + _service.LastResult.Evaluation.DeliveredDish.DisplayName;
            return true;
        }

        private bool Decline(string message) { PlacementMessage = message; return false; }

        private bool HasPlacementOverlap(Bounds dish)
        {
            // Compare footprints in the actual pad's local axes, never require a centered Dish.
            Bounds pad = new Bounds(_support.center, _support.size);
            float width = Mathf.Max(0f, Mathf.Min(dish.max.x, pad.max.x) - Mathf.Max(dish.min.x, pad.min.x));
            float depth = Mathf.Max(0f, Mathf.Min(dish.max.z, pad.max.z) - Mathf.Max(dish.min.z, pad.min.z));
            float smallerArea = Mathf.Min(dish.size.x * dish.size.z, pad.size.x * pad.size.z);
            // Scale with both small and large dishes, but reject a corner/edge merely grazing the pad.
            return smallerArea > 0f && width * depth >= smallerArea * Mathf.Clamp(_minimumFootprintOverlap, 0.01f, 1f);
        }

        private void SensorGeometry(out Vector3 center, out Vector3 half)
        {
            Vector3 scale = _support.transform.lossyScale;
            // The existing trigger supplies a detection height, not an independent delivery position.
            float height = Mathf.Abs(_zone.size.y * _zone.transform.lossyScale.y);
            center = _support.transform.TransformPoint(_support.center + Vector3.up * _support.size.y * .5f) +
                _support.transform.up * height * .5f;
            half = new Vector3(_support.size.x * Mathf.Abs(scale.x) * .5f, height * .5f, _support.size.z * Mathf.Abs(scale.z) * .5f);
        }

        private Bounds BoundsInSupportSpace(BoxCollider proxy)
        {
            Bounds result = default;
            Rigidbody body = proxy.attachedRigidbody;
            // Rigidbody.position is authoritative before the interpolated render Transform catches up.
            Matrix4x4 physicalPose = Matrix4x4.TRS(body.position, body.rotation, proxy.transform.lossyScale);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = proxy.center + Vector3.Scale(proxy.size * .5f,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                point = _support.transform.InverseTransformPoint(physicalPose.MultiplyPoint3x4(point));
                if (corner == 0) result = new Bounds(point, Vector3.zero); else result.Encapsulate(point);
            }
            return result;
        }
    }
}
