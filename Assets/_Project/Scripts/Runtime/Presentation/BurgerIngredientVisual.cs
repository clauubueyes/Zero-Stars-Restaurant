using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Presentation
{
    // Retains the script/GUID. Geometry is configured per prefab, never selected by recipe/food ID.
    [DisallowMultipleComponent]
    public sealed class BurgerIngredientVisual : MonoBehaviour
    {
        [SerializeField] private FoodItem _food;
        [SerializeField] private MeshFilter _bun;
        [SerializeField] private Mesh _bottomBun;
        [SerializeField] private Transform _crumb;
        [SerializeField] private Renderer _sesame;
        [SerializeField, Range(.01f, 1)] private float _heightScale = .28f;
        [SerializeField, Range(.01f, 1)] private float _supportingHeightScale = .28f;
        [SerializeField, Range(.1f, 1)] private float _diameterScale = .95f;
        private const float ContactTolerance = .025f;
        private const float SeamOverlap = .001f;
        private Vector3 _position, _scale, _crumbPosition;
        private Quaternion _rotation;
        private Mesh _bunMesh;
        private bool _sesameEnabled, _initialized, _polishEnabled = true;
        private BoxCollider _proxy;
        private Pickup _pickup;
        private MeshFilter[] _shapes;
        private Renderer[] _renderers;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly BurgerIngredientVisual[] _pickCandidates = new BurgerIngredientVisual[32];
        private DishItem _cachedDish;
        private FoodItem[] _units;
        private float _baseLift;
        public bool PolishEnabled => _polishEnabled;
        public FoodItem Food => _food;
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
            _shapes = GetComponentsInChildren<MeshFilter>(true); _renderers = new Renderer[_shapes.Length];
            for (int i = 0; i < _shapes.Length; i++) _renderers[i] = _shapes[i].GetComponent<Renderer>();
        }
        public void SetPolishEnabled(bool enabled) { _polishEnabled = enabled; Refresh(); }
        public void Refresh() => Refresh(0);
        private void Refresh(int depth)
        {
            Remember();
            if (!_polishEnabled || _food == null || depth > 16) { Restore(); return; }
            if (_proxy == null) _proxy = _food.GetComponent<BoxCollider>();
            if (_pickup == null) _pickup = _food.GetComponent<Pickup>();
            if (_proxy == null) { Restore(); return; }
            var dish = _food.GetComponentInParent<DishItem>();
            if (dish != _cachedDish) { _cachedDish = dish; _units = dish == null ? null : dish.GetComponentsInChildren<FoodItem>(); }
            Transform frame = dish == null ? null : dish.transform;
            Bounds physical = ShapeBounds(_proxy.transform, new Bounds(_proxy.center, _proxy.size), frame);
            BurgerIngredientVisual lower = null;
            bool upper = false;
            if (dish != null)
            {
                float nearest = float.PositiveInfinity;
                foreach (var unit in _units)
                {
                    if (unit == _food) continue;
                    var box = unit.GetComponent<BoxCollider>(); if (box == null) continue;
                    Bounds other = ShapeBounds(box.transform, new Bounds(box.center, box.size), frame);
                    if (!OverlapsFootprint(physical, other)) continue;
                    float gap = Mathf.Abs(physical.min.y - other.max.y);
                    if (other.center.y < physical.center.y && gap <= ContactTolerance && gap < nearest)
                    { lower = unit.transform.Find("Visual_VP1BC")?.GetComponent<BurgerIngredientVisual>(); nearest = gap; }
                    if (other.center.y > physical.center.y && Mathf.Abs(other.min.y - physical.max.y) <= ContactTolerance) upper = true;
                }
            }
            else if (_pickup != null && !_pickup.IsHeld && Vector3.Dot(_food.transform.up, Vector3.up) > .8f)
            {
                var support = Contact(physical, false);
                if (support.HasValue)
                {
                    var unit = support.Value.collider.GetComponentInParent<FoodItem>();
                    lower = unit == null ? null : unit.transform.Find("Visual_VP1BC")?.GetComponent<BurgerIngredientVisual>();
                    var surface = support.Value.collider.GetComponent<FoodVisualSupportSurface>();
                    _baseLift = surface == null ? 0 : surface.LiftAt(support.Value.point);
                }
                else _baseLift = 0;
                upper = Contact(physical, true)?.collider.GetComponentInParent<FoodItem>() != null;
            }
            else _baseLift = 0;

            bool baseAppearance = lower == null && upper;
            float ratio = baseAppearance ? _supportingHeightScale : _heightScale;
            transform.SetLocalPositionAndRotation(_position, _rotation);
            transform.localScale = Vector3.Scale(_scale, new Vector3(_diameterScale, ratio, _diameterScale));
            if (_bun != null) _bun.sharedMesh = baseAppearance ? _bottomBun : _bunMesh;
            if (_crumb != null) _crumb.localPosition = baseAppearance ? new Vector3(_crumbPosition.x, .47f, _crumbPosition.z) : _crumbPosition;
            if (_sesame != null) _sesame.enabled = !baseAppearance && _sesameEnabled;
            float bottom = physical.min.y + _baseLift;
            if (lower != null && lower != this && lower.isActiveAndEnabled && lower.PolishEnabled)
            {
                lower.Refresh(depth + 1);
                if (lower.TryGetVisualBounds(frame, out var supportBounds)) bottom = supportBounds.max.y - SeamOverlap;
            }
            if (TryGetVisualBounds(frame, out var visual))
            {
                Vector3 offset = Vector3.up * (bottom - visual.min.y);
                transform.position += frame == null ? offset : frame.TransformVector(offset);
            }
        }
        private static bool OverlapsFootprint(Bounds a, Bounds b) =>
            a.min.x < b.max.x && a.max.x > b.min.x && a.min.z < b.max.z && a.max.z > b.min.z;
        private RaycastHit? Contact(Bounds physical, bool above)
        {
            RaycastHit? nearest = null;
            for (int sample = 0; sample < 5; sample++)
            {
                float x = sample == 0 ? 0 : (sample <= 2 ? -.8f : .8f), z = sample == 0 ? 0 : (sample % 2 == 0 ? -.8f : .8f);
                Vector3 start = new Vector3(physical.center.x + physical.extents.x * x,
                    above ? physical.max.y - ContactTolerance : physical.min.y + ContactTolerance, physical.center.z + physical.extents.z * z);
                int count = Physics.RaycastNonAlloc(start, above ? Vector3.up : Vector3.down, _hits, ContactTolerance * 2, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = _hits[i]; var unit = hit.collider.GetComponentInParent<FoodItem>();
                    if (unit == _food || hit.collider.GetComponentInParent<CharacterController>() != null || hit.collider.GetComponentInParent<DishItem>() != null ||
                        (above ? hit.normal.y > -.6f : hit.normal.y < .6f)) continue;
                    if (unit != null)
                    {
                        var box = unit.GetComponent<BoxCollider>();
                        if (box == null || unit.GetComponent<Pickup>()?.IsHeld == true || (box.bounds.center.y <= physical.center.y) == above) continue;
                    }
                    if (!nearest.HasValue || hit.distance < nearest.Value.distance) nearest = hit;
                }
            }
            return nearest;
        }
        // Selection may follow visible food inside a connected column, while placement keeps physical ray hits.
        public bool TryFindVisibleFood(Ray ray, float maximumDistance, Rigidbody ignoredBody, out FoodItem selected)
        {
            selected = null;
            if (!_polishEnabled || _food == null || _food.GetComponentInParent<DishItem>() != null) return false;
            int count = 1; _pickCandidates[0] = this;
            float nearest = maximumDistance;
            for (int index = 0; index < count; index++)
            {
                var view = _pickCandidates[index]; view.Refresh();
                if (view._pickup != null && view._pickup.isActiveAndEnabled && !view._pickup.IsHeld && view._pickup.Body != ignoredBody &&
                    view.TryGetVisualBounds(out var visual) && visual.IntersectRay(ray, out float distance) && distance <= nearest)
                { selected = view._food; nearest = distance; }
                Bounds physical = ShapeBounds(view._proxy.transform, new Bounds(view._proxy.center, view._proxy.size), null);
                for (int side = 0; side < 2; side++)
                {
                    var contact = view.Contact(physical, side == 1);
                    var unit = contact?.collider.GetComponentInParent<FoodItem>();
                    var neighbor = unit == null ? null : unit.transform.Find("Visual_VP1BC")?.GetComponent<BurgerIngredientVisual>();
                    if (neighbor == null || !neighbor.isActiveAndEnabled || !neighbor.PolishEnabled || count == _pickCandidates.Length) continue;
                    bool seen = false; for (int i = 0; i < count; i++) if (_pickCandidates[i] == neighbor) seen = true;
                    if (!seen) _pickCandidates[count++] = neighbor;
                }
            }
            return selected != null;
        }
        public bool TryGetVisualBounds(out Bounds bounds) { Remember(); return TryGetVisualBounds(null, out bounds); }
        private bool TryGetVisualBounds(Transform frame, out Bounds bounds)
        {
            bounds = default; bool found = false;
            for (int i = 0; i < _shapes.Length; i++)
            {
                if (_renderers[i] == null || !_renderers[i].enabled || !_shapes[i].gameObject.activeInHierarchy || _shapes[i].sharedMesh == null) continue;
                var next = ShapeBounds(_shapes[i].transform, _shapes[i].sharedMesh.bounds, frame);
                if (!found) bounds = next; else bounds.Encapsulate(next); found = true;
            }
            return found;
        }
        private static Bounds ShapeBounds(Transform shape, Bounds local, Transform frame)
        {
            Matrix4x4 matrix = frame == null ? shape.localToWorldMatrix : frame.worldToLocalMatrix * shape.localToWorldMatrix;
            var result = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++) result.Encapsulate(matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return result;
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
