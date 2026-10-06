using UnityEngine;

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
            Collider closest = null;
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
                    closest = hit.collider;
                }
            }
            if (closest == null)
                return null;
            Interactable target = closest.GetComponentInParent<Interactable>();
            return target != null && target.isActiveAndEnabled ? target : null;
        }
    }
}
