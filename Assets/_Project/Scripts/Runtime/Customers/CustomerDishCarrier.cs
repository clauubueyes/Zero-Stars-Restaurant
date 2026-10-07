using System;
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
        public bool HasValidAnchor => _anchor != null && _anchor.IsChildOf(transform);
        public bool CanTake(DishItem dish) => isActiveAndEnabled && HasValidAnchor && Dish == null && dish != null && dish.isActiveAndEnabled &&
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
            if (result.Accepted) Attach(dish);
            return true;
        }

        public bool Take(DishItem dish)
        {
            if (!CanTake(dish) || !dish.State.IsSold) return false;
            Attach(dish);
            return true;
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
            dish.transform.SetParent(_anchor, false); dish.transform.localRotation = Quaternion.identity;
            BoxCollider proxy = dish.GetComponent<BoxCollider>();
            _carryLocalPosition = Vector3.up * (proxy.size.y * 0.5f - proxy.center.y);
            Dish = dish;
            SynchronizePose();
        }

        // Driven by the same service step as customer movement, never by player input or gaze.
        // Parenting alone does not explicitly move the native bodies of a nested aggregate.
        internal void SynchronizePose()
        {
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

        // The service unregisters originals before invoking this at exit (or cancellation).
        public void Clear()
        {
            DishItem retired = Dish; Dish = null;
            _bodies = Array.Empty<Rigidbody>(); _bodyLocalPoses = Array.Empty<Pose>();
            if (retired == null) return;
            retired.gameObject.SetActive(false);
            retired.transform.SetParent(null, true);
            Destroy(retired.gameObject);
        }
    }
}
