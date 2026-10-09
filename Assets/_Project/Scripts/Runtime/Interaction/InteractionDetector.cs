using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Interaction
{
    public sealed class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private Transform _origin;
        [SerializeField] private Transform _actorRoot;
        [SerializeField, Min(0.1f)] private float _range = 3f;
        [SerializeField] private LayerMask _collisionMask = ~0;

        public Interactable Detect(Rigidbody ignoredBody = null)
        {
            if (_origin == null)
                return null;
            return Detect(new Ray(_origin.position, _origin.forward), ignoredBody);
        }

        public Interactable Detect(Ray ray, Rigidbody ignoredBody = null)
        {
            if (!TryDetectHit(ray, out RaycastHit closest, ignoredBody)) return null;
            var food = closest.collider.GetComponentInParent<FoodItem>();
            var visual = food == null ? null : food.transform.Find("Visual_VP1BC")?.GetComponent<BurgerIngredientVisual>();
            if (visual != null && visual.PolishEnabled)
            {
                float visibleRange = _range;
                // Original solid geometry still occludes selection. Food proxies alone can contain empty visual volume.
                foreach (var hit in Physics.RaycastAll(ray, _range, _collisionMask, QueryTriggerInteraction.Ignore))
                    if ((_actorRoot == null || !hit.transform.IsChildOf(_actorRoot)) && (ignoredBody == null || hit.rigidbody != ignoredBody) &&
                        hit.collider.GetComponentInParent<FoodItem>() == null) visibleRange = Mathf.Min(visibleRange, hit.distance);
                if (visual.TryFindVisibleFood(ray, visibleRange, ignoredBody, out var visibleFood)) return visibleFood.GetComponent<Pickup>();
            }
            // A retired child interactable must not hide the active aggregate owning the collider.
            for (Transform owner = closest.collider.transform; owner != null; owner = owner.parent)
            {
                Interactable target = owner.GetComponent<Interactable>();
                if (target != null && target.isActiveAndEnabled) return target;
            }
            return null;
        }

        public bool TryDetectHit(out RaycastHit hit, Rigidbody ignoredBody = null)
        {
            hit = default;
            return _origin != null && TryDetectHit(new Ray(_origin.position, _origin.forward), out hit, ignoredBody);
        }

        private bool TryDetectHit(Ray ray, out RaycastHit closest, Rigidbody ignoredBody)
        {
            closest = default;
            float distance = float.PositiveInfinity;
            // Include non-interactable geometry. Filtering to pickups would see through walls.
            foreach (RaycastHit hit in Physics.RaycastAll(ray, _range, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                if ((_actorRoot != null && hit.transform.IsChildOf(_actorRoot)) ||
                    (ignoredBody != null && hit.rigidbody == ignoredBody))
                    continue;
                if (hit.distance < distance)
                {
                    distance = hit.distance;
                    closest = hit;
                }
            }
            return closest.collider != null;
        }
    }
}
