using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<FoodItem, Guid> _processedFoods = new Dictionary<FoodItem, Guid>();
        public CustomerServiceLoop Service => _service;
        public BoxCollider Support => _support;
        public string PlacementMessage { get; private set; } = "";
        private void Awake()
        {
            if (_zone == null || !_zone.isTrigger || _support == null || _support.isTrigger || _service == null || _service.DeliveryZone != this)
            { Debug.LogError("DeliveryZone needs a trigger, solid support and customer service.", this); enabled = false; }
        }
        private void FixedUpdate() => Poll();
        private void OnDisable() { _processedPlacements.Clear(); _processedFoods.Clear(); PlacementMessage = ""; }
        public void Poll()
        {
            if (!isActiveAndEnabled || _zone == null || !_zone.enabled || !_zone.isTrigger || !_zone.gameObject.activeInHierarchy ||
                _support == null || !_support.enabled || !_support.gameObject.activeInHierarchy || _service == null)
            { _processedPlacements.Clear(); _processedFoods.Clear(); PlacementMessage = "Delivery unavailable"; return; }
            Physics.SyncTransforms();
            SensorGeometry(out Vector3 center, out Vector3 half);
            var present = new HashSet<DishItem>();
            bool heldDish = false;
            foreach (Collider collider in Physics.OverlapBox(center, half,
                         _support.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                DishItem dish = collider.GetComponentInParent<DishItem>();
                if (dish == null)
                { continue; }
                if (!dish.isActiveAndEnabled || dish.State == null || !dish.State.IsFinalized || dish.State.IsSold || !dish.IsIntact) continue;
                Pickup pickup = dish.GetComponent<Pickup>();
                heldDish |= pickup != null && pickup.IsHeld;
                if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || pickup.Body == null || pickup.Body.isKinematic) continue;
                present.Add(dish);
            }
            // A rejection belongs to one order, never to every future customer visiting this pad.
            foreach (DishItem tracked in new List<DishItem>(_processedPlacements.Keys))
                if (tracked == null || !present.Contains(tracked)) _processedPlacements.Remove(tracked);
            PlacementMessage = heldDish ? "Delivery: release the dish on the green pad" : "Delivery: place food or a dish on the green pad";
            if (_service.Visit == null || _service.Visit.Stage != CustomerStage.Wait)
            { PlacementMessage += "\nWaiting for a customer ready to receive"; return; }
            var ordered = new List<DishItem>(present);
            if (ordered.Count > 1) { Decline("Delivery: submit one dish at a time"); return; }
            ordered.Sort((a, b) => a.State.InstanceId.Value.CompareTo(b.State.InstanceId.Value));
            foreach (DishItem dish in ordered)
            {
                if (TryDeliver(dish)) return;
            }
            TryDeliverLoose(null);
        }

        public bool TryDeliver(FoodItem food) => food != null && TryDeliverLoose(food);

        private bool TryDeliverLoose(FoodItem required, DishItem includedDish = null)
        {
            if (!isActiveAndEnabled || _service == null || _service.DeliveryZone != this || _service.FoodSimulation == null ||
                _zone == null || !_zone.enabled || !_zone.isTrigger || !_zone.gameObject.activeInHierarchy ||
                _support == null || !_support.enabled || !_support.gameObject.activeInHierarchy) return false;
            Physics.SyncTransforms(); SensorGeometry(out Vector3 center, out Vector3 half);
            var foods = new HashSet<FoodItem>(); var plates = new HashSet<PlateItem>();
            foreach (Collider collider in Physics.OverlapBox(center, half, _support.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (collider.GetComponentInParent<DishItem>() != null) continue;
                PlateItem plate = collider.GetComponentInParent<PlateItem>();
                if (plate != null && plate.CanAttach) plates.Add(plate);
                FoodItem food = collider.GetComponentInParent<FoodItem>();
                if (food != null && food.isActiveAndEnabled && food.State != null && !food.State.IsSold &&
                    _service.FoodSimulation.Foods.Contains(food)) foods.Add(food);
            }
            foreach (FoodItem tracked in new List<FoodItem>(_processedFoods.Keys))
                if (tracked == null || !foods.Contains(tracked)) _processedFoods.Remove(tracked);
            if (foods.Count == 0 || (required != null && !foods.Contains(required))) return false;
            if (_service.Visit == null || _service.Visit.Stage != CustomerStage.Wait) return Decline("Delivery: waiting for a customer ready to receive");
            foreach (FoodItem food in foods)
            {
                if (_processedFoods.TryGetValue(food, out Guid orderId) && orderId == _service.Visit.Order.InstanceId)
                    return Decline("Delivery: this order already evaluated the food");
                Pickup pickup = food.GetComponent<Pickup>(); Rigidbody body = food.GetComponent<Rigidbody>();
                BoxCollider box = food.GetComponent<BoxCollider>();
                if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || body == null || body.isKinematic ||
                    box == null || !box.enabled || box.isTrigger) return Decline("Delivery: release the food first");
                if (body.linearVelocity.sqrMagnitude > _maximumPlacementSpeed * _maximumPlacementSpeed || body.angularVelocity.sqrMagnitude > 4f)
                    return Decline("Delivery: waiting for the food to settle");
                if (!HasPlacementOverlap(BoundsInSupportSpace(box))) return Decline("Delivery: move the food further onto the green pad");
            }
            // Every submitted unit must be connected by support to PASS. Airborne units and
            // food merely beside the pad cannot become extra ingredients in another delivery.
            var supported = new HashSet<FoodItem>(); PlateItem carriedPlate = null;
            foreach (PlateItem plate in plates)
                if (OnSupport(plate.GetComponent<BoxCollider>(), _support))
                {
                    Rigidbody body = plate.GetComponent<Rigidbody>();
                    if (body.linearVelocity.sqrMagnitude > _maximumPlacementSpeed * _maximumPlacementSpeed || body.angularVelocity.sqrMagnitude > 4f)
                        return Decline("Delivery: waiting for the plate to settle");
                }
            bool changed;
            do
            {
                changed = false;
                foreach (FoodItem food in foods)
                {
                    if (supported.Contains(food)) continue;
                    BoxCollider box = food.GetComponent<BoxCollider>(); bool rests = OnSupport(box, _support);
                    if (includedDish != null) rests |= OnSupport(box, includedDish.GetComponent<BoxCollider>());
                    foreach (FoodItem lower in supported) rests |= OnSupport(box, lower.GetComponent<BoxCollider>());
                    foreach (PlateItem plate in plates)
                        if (OnSupport(plate.GetComponent<BoxCollider>(), _support) && OnSupport(box, plate.GetComponent<BoxCollider>()))
                        {
                            if (carriedPlate != null && carriedPlate != plate) return Decline("Delivery: submit one plate at a time");
                            rests = true; carriedPlate = plate;
                        }
                    if (rests) { supported.Add(food); changed = true; }
                }
            } while (changed);
            if (supported.Count != foods.Count) return Decline("Delivery: rest all submitted food on the green pad");
            var ordered = foods.OrderBy(food => food.GetComponent<Rigidbody>().worldCenterOfMass.y)
                .ThenBy(food => food.State.InstanceId).ToList();
            bool stack = true;
            for (int i = 1; i < ordered.Count; i++)
            {
                Vector3 delta = ordered[i].transform.position - ordered[0].transform.position;
                if (new Vector2(delta.x, delta.z).magnitude > .18f ||
                    ordered[i].transform.position.y - ordered[i - 1].transform.position.y < .02f) stack = false;
            }
            if (includedDish != null && carriedPlate != null) return Decline("Delivery: submit a dish or a loose plate, not both");
            var delivery = includedDish != null ? new PhysicalDelivery(includedDish, ordered) :
                new PhysicalDelivery(ordered, _service.DeliveryRecipes, stack, carriedPlate);
            if (!_service.TryAcceptDelivery(this, delivery)) return Decline("Delivery: food unavailable or customer unable to receive");
            foreach (FoodItem food in foods) _processedFoods[food] = _service.Visit.Order.InstanceId;
            if (includedDish != null) _processedPlacements[includedDish] = _service.Visit.Order.InstanceId;
            PlacementMessage = _service.LastResult.Accepted ? "Delivery accepted" : "Delivery rejected: no expected ingredients";
            return true;
        }

        private bool OnSupport(BoxCollider upper, BoxCollider lower)
        {
            Bounds a = upper.bounds, b = lower.bounds;
            float gap = a.min.y - b.max.y;
            if (gap < -_placementTolerance || gap > _placementTolerance ||
                a.min.x >= b.max.x || a.max.x <= b.min.x || a.min.z >= b.max.z || a.max.z <= b.min.z) return false;
            // Verify actual upward support, rather than an AABB corner of a rotated object.
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                Vector3 point = new Vector3(a.center.x + a.extents.x * x * .8f, a.min.y, a.center.z + a.extents.z * z * .8f);
                if (lower.Raycast(new Ray(point + Vector3.up * _placementTolerance, Vector3.down), out RaycastHit hit,
                    _placementTolerance * 2f) && hit.normal.y >= .6f) return true;
            }
            return false;
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
            bool hasLooseFood = Physics.OverlapBox(center, half, _support.transform.rotation, ~0, QueryTriggerInteraction.Ignore)
                .Any(collider => collider.GetComponentInParent<DishItem>() == null && collider.GetComponentInParent<FoodItem>() != null);
            if (hasLooseFood) return TryDeliverLoose(null, dish);
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
