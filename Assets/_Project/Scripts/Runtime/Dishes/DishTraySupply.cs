using UnityEngine;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Dishes
{
    // An empty, authored tray prefab is the only object replenished. Sold food is never cloned.
    [DisallowMultipleComponent]
    public sealed class DishTraySupply : MonoBehaviour
    {
        [SerializeField] private AssemblySurface _surface;
        [SerializeField] private DishItem _emptyTrayPrefab;
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private BoxCollider _shape;

        private void Awake()
        {
            if (_surface == null || _surface.Dish == null || _emptyTrayPrefab == null ||
                _emptyTrayPrefab.GetComponentsInChildren<FoodItem>(true).Length != 0 ||
                _emptyTrayPrefab.GetComponentsInChildren<DishItem>(true).Length != 1)
            { Debug.LogError("Tray supply needs a station and a clean empty-tray prefab.", this); enabled = false; return; }
            Transform tray = _surface.Dish.transform;
            _position = tray.position; _rotation = tray.rotation; _scale = tray.lossyScale;
            _shape = _emptyTrayPrefab.GetComponent<BoxCollider>();
        }

        private void Update() => TryReplenish();

        public bool TryReplenish()
        {
            if (!isActiveAndEnabled || _surface == null || !_surface.isActiveAndEnabled || _shape == null) return false;
            DishItem previous = _surface.Dish;
            if (previous != null && (previous.State == null || !previous.State.IsFinalized)) return false;
            Physics.SyncTransforms();
            Vector3 center = _position + _rotation * Vector3.Scale(_shape.center, _scale);
            Vector3 half = Vector3.Scale(_shape.size * .5f, _scale) + Vector3.one * .002f;
            // The previous dish, dropped food or another tray can block the outlet. Retry without duplicating.
            if (Physics.OverlapBox(center, half, _rotation, ~0, QueryTriggerInteraction.Ignore).Length != 0) return false;
            DishItem tray = Instantiate(_emptyTrayPrefab, _position, _rotation, transform);
            tray.transform.localScale = _scale; // Stations in the prototype have unit scale.
            tray.gameObject.SetActive(true);
            if (!_surface.TryReplaceTray(previous, tray))
            { tray.gameObject.SetActive(false); Destroy(tray.gameObject); return false; }
            return true;
        }
    }
}
