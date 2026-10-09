using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Presentation
{
    // Only the visual shell moves. Original ingredient poses, retired contact geometry and Dish proxy stay intact.
    [DisallowMultipleComponent]
    public sealed class BurgerIngredientVisual : MonoBehaviour
    {
        [SerializeField] private FoodItem _food;
        [SerializeField] private MeshFilter _bun;
        [SerializeField] private Mesh _bottomBun;
        [SerializeField] private Transform _crumb;
        [SerializeField] private Renderer _sesame;
        private Vector3 _position, _scale, _crumbPosition;
        private Quaternion _rotation;
        private Mesh _bunMesh;
        private bool _sesameEnabled, _initialized, _polishEnabled = true;
        private DishItem _cachedDish;
        private FoodItem[] _units;
        public bool PolishEnabled => _polishEnabled;
        private void OnEnable() { Remember(); Refresh(); }
        private void LateUpdate() => Refresh();
        private void OnDisable() => Restore();
        private void Remember()
        {
            if (_initialized) return;
            _initialized = true; _position = transform.localPosition; _rotation = transform.localRotation; _scale = transform.localScale;
            if (_bun != null) _bunMesh = _bun.sharedMesh;
            if (_crumb != null) _crumbPosition = _crumb.localPosition;
            if (_sesame != null) _sesameEnabled = _sesame.enabled;
        }
        public void SetPolishEnabled(bool enabled) { _polishEnabled = enabled; Refresh(); }
        public void Refresh()
        {
            Remember();
            if (!_polishEnabled || _food == null) { Restore(); return; }
            string id = _food.Definition.Id;
            float ratio = id == "food.bun" ? .45f : id == "food.cheese" ? .07f : .28f;
            var dish = _food.transform.parent == null ? null : _food.transform.parent.GetComponent<DishItem>();
            string recipe = dish?.State?.RecognizedDefinition?.Id;
            bool burger = dish != null && dish.State.IsFinalized && !dish.State.IsDisposed &&
                (recipe == "dish.hamburger" || recipe == "dish.cheeseburger");
            bool bottom = burger && ReferenceEquals(dish.State.Components[0], _food.State);
            if (bottom) ratio = .20f;
            transform.localScale = Vector3.Scale(_scale, new Vector3(.95f, ratio, .95f));
            transform.localRotation = _rotation;
            if (_bun != null) _bun.sharedMesh = bottom ? _bottomBun : _bunMesh;
            if (_crumb != null) _crumb.localPosition = bottom ? new Vector3(_crumbPosition.x, .47f, _crumbPosition.z) : _crumbPosition;
            if (_sesame != null) _sesame.enabled = !bottom && _sesameEnabled;
            var box = _food.GetComponent<BoxCollider>();
            if (!burger || box == null)
            {
                // Loose food still rests visually on its original support plane, inside its unchanged collider.
                transform.localPosition = _position + Vector3.down * (box == null ? 0 : box.size.y * (1 - ratio) * .5f);
                return;
            }
            if (_cachedDish != dish) { _cachedDish = dish; _units = dish.GetComponentsInChildren<FoodItem>(); }
            var units = _units;
            FoodItem first = null; float height = 0;
            foreach (var state in dish.State.Components)
            {
                FoodItem unit = null;
                foreach (var candidate in units) if (ReferenceEquals(candidate.State, state)) { unit = candidate; break; }
                if (unit == null) { Restore(); return; }
                if (first == null) first = unit;
                var unitBox = unit.GetComponent<BoxCollider>();
                if (unitBox == null) { Restore(); return; }
                float unitHeight = dish.transform.InverseTransformVector(unit.transform.TransformVector(Vector3.up * unitBox.size.y)).magnitude;
                float unitRatio = unit == first ? .20f : unit.Definition.Id == "food.bun" ? .45f : unit.Definition.Id == "food.cheese" ? .07f : .28f;
                float visualHeight = unitHeight * unitRatio;
                if (unit == _food)
                {
                    var firstBox = first.GetComponent<BoxCollider>();
                    Vector3 basePoint = dish.transform.InverseTransformPoint(first.transform.TransformPoint(firstBox.center - Vector3.up * firstBox.size.y * .5f));
                    Vector3 originalCenter = dish.transform.InverseTransformPoint(_food.transform.TransformPoint(box.center));
                    originalCenter.y = basePoint.y + height + visualHeight * .5f;
                    transform.localPosition = _position + _food.transform.InverseTransformPoint(dish.transform.TransformPoint(originalCenter)) - box.center;
                    return;
                }
                height += visualHeight - .001f; // Tiny overlap avoids artificial seams, without moving food or contact geometry.
            }
            Restore();
        }
        private void Restore()
        {
            if (!_initialized) return;
            transform.SetLocalPositionAndRotation(_position, _rotation); transform.localScale = _scale;
            if (_bun != null) _bun.sharedMesh = _bunMesh;
            if (_crumb != null) _crumb.localPosition = _crumbPosition;
            if (_sesame != null) _sesame.enabled = _sesameEnabled;
        }
    }
}
