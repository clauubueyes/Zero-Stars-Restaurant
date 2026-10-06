using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Customers
{
    [DisallowMultipleComponent]
    public sealed class CustomerDishCarrier : MonoBehaviour
    {
        [SerializeField] private Transform _anchor;
        public DishItem Dish { get; private set; }
        public bool HasValidAnchor => _anchor != null && _anchor.IsChildOf(transform);
        public bool CanTake(DishItem dish) => isActiveAndEnabled && HasValidAnchor && Dish == null && dish != null && dish.isActiveAndEnabled &&
            dish.State != null && dish.State.IsFinalized && dish.IsIntact && dish.GetComponent<Pickup>() != null &&
            !dish.GetComponent<Pickup>().IsHeld;

        public bool Take(DishItem dish)
        {
            if (!CanTake(dish) || !dish.State.IsSold) return false;
            dish.GetComponent<Pickup>().enabled = false;
            Rigidbody body = dish.GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
            foreach (Collider collider in dish.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            dish.transform.SetParent(_anchor, false); dish.transform.localRotation = Quaternion.identity;
            BoxCollider proxy = dish.GetComponent<BoxCollider>();
            dish.transform.localPosition = Vector3.up * (proxy.size.y * 0.5f - proxy.center.y);
            Dish = dish;
            return true;
        }

        // The service unregisters originals before invoking this at exit (or cancellation).
        public void Clear()
        {
            DishItem retired = Dish; Dish = null;
            if (retired == null) return;
            retired.gameObject.SetActive(false);
            retired.transform.SetParent(null, true);
            Destroy(retired.gameObject);
        }
    }
}
