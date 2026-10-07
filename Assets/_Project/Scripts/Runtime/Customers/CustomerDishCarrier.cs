using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Food;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Customers
{
    [DisallowMultipleComponent]
    public sealed class CustomerDishCarrier : MonoBehaviour
    {
        [SerializeField] private Transform _anchor;
        private Rigidbody[] _bodies = Array.Empty<Rigidbody>();
        private Pose[] _bodyLocalPoses = Array.Empty<Pose>();
        private Vector3 _carryLocalPosition;
        public DishItem Dish { get; private set; }
        private PhysicalDelivery _delivery;
        private Transform[] _looseRoots = Array.Empty<Transform>();
        private Pose[] _rootPoses = Array.Empty<Pose>();
        public IReadOnlyList<FoodItem> Foods => _delivery != null ? _delivery.Foods : Array.Empty<FoodItem>();
        public bool HasValidAnchor => _anchor != null && _anchor.IsChildOf(transform);
        public bool CanTake(DishItem dish) => isActiveAndEnabled && HasValidAnchor && _delivery == null && Dish == null && dish != null && dish.isActiveAndEnabled &&
            dish.State != null && dish.State.IsFinalized && dish.IsIntact && dish.GetComponent<Pickup>() != null &&
            dish.GetComponent<Pickup>().isActiveAndEnabled && !dish.GetComponent<Pickup>().IsHeld &&
            dish.GetComponent<Rigidbody>() != null && !dish.GetComponent<Rigidbody>().isKinematic &&
            dish.GetComponent<BoxCollider>() != null && dish.GetComponent<BoxCollider>().enabled;

        // Preflight ownership before the Domain transaction. No callbacks or second fallible
        // handoff between marking a dish sold and attaching that exact object to this customer.
        public bool TryReceive(DishItem dish, OrderState order, PaymentLedger ledger, out OrderResult result)
        {
            result = null;
            if (!CanTake(dish) || dish.State.IsSold || !OrderDelivery.TryComplete(order, dish.State, ledger, out result)) return false;
            if (result.Accepted) { _delivery = new PhysicalDelivery(dish); Attach(dish); }
            return true;
        }

        internal bool CanTake(PhysicalDelivery delivery) => isActiveAndEnabled && HasValidAnchor && _delivery == null &&
            Dish == null && delivery != null && delivery.IsAvailable;

        internal bool TryReceive(PhysicalDelivery delivery, OrderState order, PaymentLedger ledger, out OrderResult result)
        {
            result = null;
            if (!CanTake(delivery) || !OrderDelivery.TryComplete(order, delivery.Contents, ledger, out result)) return false;
            if (result.Accepted)
            {
                _delivery = delivery;
                if (delivery.Dish != null)
                {
                    // Extend physical ownership with the extra original objects, without
                    // rewriting the finalized composition or manufacturing ingredient units.
                    for (int i = 1; i < delivery.Roots.Count; i++)
                    {
                        foreach (FoodItem food in delivery.Roots[i].GetComponentsInChildren<FoodItem>(true)) food.CaptureThermalGeometry();
                        delivery.Roots[i].SetParent(delivery.Dish.transform, true);
                    }
                    Attach(delivery.Dish);
                }
                else AttachLoose(delivery);
            }
            return true;
        }

        public bool Take(DishItem dish)
        {
            if (!CanTake(dish) || !dish.State.IsSold) return false;
            _delivery = new PhysicalDelivery(dish);
            Attach(dish);
            return true;
        }

        private void AttachLoose(PhysicalDelivery delivery)
        {
            _looseRoots = new Transform[delivery.Roots.Count]; _rootPoses = new Pose[_looseRoots.Length];
            Transform first = delivery.Roots[0]; Vector3 origin = first.position;
            Quaternion inverse = Quaternion.Inverse(first.rotation);
            Bounds bounds = first.GetComponent<Collider>().bounds;
            float lift = first.position.y - bounds.min.y;
            for (int i = 0; i < _looseRoots.Length; i++)
            {
                Transform root = delivery.Roots[i];
                Vector3 position = _anchor.position + _anchor.rotation * (inverse * (root.position - origin) + Vector3.up * lift);
                Quaternion rotation = _anchor.rotation * inverse * root.rotation;
                foreach (Pickup pickup in root.GetComponentsInChildren<Pickup>(true)) pickup.enabled = false;
                foreach (FoodItem food in root.GetComponentsInChildren<FoodItem>(true)) food.CaptureThermalGeometry();
                foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
                {
                    if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                    body.collisionDetectionMode = CollisionDetectionMode.Discrete; body.interpolation = RigidbodyInterpolation.None;
                    body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
                }
                foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                root.SetParent(_anchor, true); root.SetPositionAndRotation(position, rotation);
                _looseRoots[i] = root; _rootPoses[i] = new Pose(root.localPosition, root.localRotation);
            }
            SynchronizePose();
        }

        private void Attach(DishItem dish)
        {
            foreach (Pickup pickup in dish.GetComponentsInChildren<Pickup>(true)) pickup.enabled = false;
            // Retire physics ownership of the whole aggregate, including ingredient bodies.
            // Their interpolation must never overwrite the hierarchy's customer-relative pose.
            foreach (Rigidbody body in dish.GetComponentsInChildren<Rigidbody>(true))
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.interpolation = RigidbodyInterpolation.None;
                body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
            }
            foreach (Collider collider in dish.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            _bodies = dish.GetComponentsInChildren<Rigidbody>(true);
            _bodyLocalPoses = new Pose[_bodies.Length];
            for (int index = 0; index < _bodies.Length; index++)
                _bodyLocalPoses[index] = new Pose(dish.transform.InverseTransformPoint(_bodies[index].transform.position),
                    Quaternion.Inverse(dish.transform.rotation) * _bodies[index].transform.rotation);
            // Preserve the aggregate's world scale when the user's customer/anchor is scaled.
            dish.transform.SetParent(_anchor, true); dish.transform.localRotation = Quaternion.identity;
            BoxCollider proxy = dish.GetComponent<BoxCollider>();
            _carryLocalPosition = Vector3.up * (proxy.size.y * 0.5f - proxy.center.y);
            Dish = dish;
            SynchronizePose();
        }

        // Driven by the same service step as customer movement, never by player input or gaze.
        // Parenting alone does not explicitly move the native bodies of a nested aggregate.
        internal void SynchronizePose()
        {
            for (int i = 0; i < _looseRoots.Length; i++)
            {
                Transform root = _looseRoots[i]; if (root == null) continue;
                root.SetLocalPositionAndRotation(_rootPoses[i].position, _rootPoses[i].rotation);
                Rigidbody body = root.GetComponent<Rigidbody>();
                body.position = root.position; body.rotation = root.rotation;
            }
            if (Dish == null) return;
            Dish.transform.SetLocalPositionAndRotation(_carryLocalPosition, Quaternion.identity);
            for (int index = 0; index < _bodies.Length; index++)
            {
                Rigidbody body = _bodies[index];
                if (body == null) continue;
                Vector3 position = Dish.transform.TransformPoint(_bodyLocalPoses[index].position);
                Quaternion rotation = Dish.transform.rotation * _bodyLocalPoses[index].rotation;
                body.transform.SetPositionAndRotation(position, rotation);
                body.position = position; body.rotation = rotation;
            }
        }

        // Presentation only: reassert native-body poses after physics and before rendering.
        // This advances no customer or food clock.
        private void LateUpdate() => SynchronizePose();

        // The service unregisters originals before invoking this at exit (or cancellation).
        public void Clear()
        {
            foreach (Transform root in _looseRoots)
                if (root != null) { root.gameObject.SetActive(false); root.SetParent(null, true); Destroy(root.gameObject); }
            _looseRoots = Array.Empty<Transform>(); _rootPoses = Array.Empty<Pose>(); _delivery = null;
            DishItem retired = Dish; Dish = null;
            _bodies = Array.Empty<Rigidbody>(); _bodyLocalPoses = Array.Empty<Pose>();
            if (retired == null) return;
            retired.gameObject.SetActive(false);
            retired.transform.SetParent(null, true);
            Destroy(retired.gameObject);
        }
    }
}
