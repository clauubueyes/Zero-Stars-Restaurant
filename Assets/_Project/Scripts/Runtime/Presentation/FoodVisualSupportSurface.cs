using UnityEngine;

namespace ZeroStarRestaurant.Presentation
{
    // Presentation metadata on an existing physical support. Never moves its proxy or renderer.
    [DisallowMultipleComponent]
    public sealed class FoodVisualSupportSurface : MonoBehaviour
    {
        [SerializeField] private Renderer[] _overlays;
        public float LiftAt(Vector3 contact)
        {
            float top = contact.y;
            if (_overlays != null) foreach (var overlay in _overlays)
            {
                if (overlay == null || !overlay.enabled || !overlay.gameObject.activeInHierarchy) continue;
                Bounds b = overlay.bounds;
                if (contact.x >= b.min.x && contact.x <= b.max.x && contact.z >= b.min.z && contact.z <= b.max.z)
                    top = Mathf.Max(top, b.max.y);
            }
            return Mathf.Clamp(top - contact.y + .001f, 0, .05f);
        }
    }
}
