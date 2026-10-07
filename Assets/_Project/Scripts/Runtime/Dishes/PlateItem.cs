using System;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Dishes
{
    // A physical utensil, never an order identity or a food state.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(Pickup))]
    public sealed class PlateItem : MonoBehaviour
    {
        public Guid InstanceId { get; private set; }
        public DishItem Dish { get; private set; }
        private void Awake() => Initialize();
        public void Initialize() { if (InstanceId == Guid.Empty) InstanceId = Guid.NewGuid(); }
        internal bool CanAttach => isActiveAndEnabled && Dish == null && GetComponent<Pickup>().isActiveAndEnabled &&
            !GetComponent<Pickup>().IsHeld && !GetComponent<Rigidbody>().isKinematic &&
            GetComponent<BoxCollider>().enabled && !GetComponent<BoxCollider>().isTrigger;

        internal void AttachTo(DishItem dish)
        {
            Initialize(); Dish = dish;
            GetComponent<Pickup>().enabled = false;
            Rigidbody body = GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
            foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            transform.SetParent(dish.transform, true);
        }
    }
}
