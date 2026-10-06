using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Dishes
{
    [DisallowMultipleComponent]
    public sealed class AssemblySurface : Interactable
    {
        [SerializeField] private DishItem _dish;
        [SerializeField] private BoxCollider _assemblyZone;
        [SerializeField] private FoodSimulation _simulation;
        [SerializeField] private DishDefinition[] _definitions = Array.Empty<DishDefinition>();
        [SerializeField, Min(0.001f)] private float _heightBand = 0.02f;
        [SerializeField, Min(0f)] private float _stackAlignmentTolerance = 0.18f;
        [SerializeField, Min(0.001f)] private float _snapGap = 0.004f;
        private DishProfile[] _profiles;
        private readonly List<FoodItem> _ingredients = new List<FoodItem>();
        public DishItem Dish => _dish;
        public DishProfile PreviewDefinition => _dish != null && _dish.State != null ? _dish.State.Recognize(_profiles) : null;
        public override string DisplayName => PreviewDefinition?.DisplayName ?? "Custom Dish";
        public override string ActionLabel => "Finalize";

        private void Awake()
        {
            if (_dish == null || _assemblyZone == null || !_assemblyZone.isTrigger || _simulation == null)
            {
                Debug.LogError("AssemblySurface needs a dish, trigger volume and food simulation.", this);
                enabled = false; return;
            }
            try
            {
                _profiles = new DishProfile[_definitions.Length];
                for (int index = 0; index < _definitions.Length; index++)
                {
                    if (_definitions[index] == null) throw new ArgumentException("Assembly needs valid recognition definitions.");
                    _profiles[index] = _definitions[index].CreateProfile();
                }
            }
            catch (ArgumentException exception)
            { Debug.LogError("Invalid dish recognition: " + exception.Message, this); enabled = false; }
        }

        private void Update() => RefreshComposition();
        private void OnDisable() => ClearDraft();
        private void ClearDraft()
        {
            if (_dish == null || _dish.State == null || _dish.State.IsFinalized) return;
            for (int index = _dish.State.Components.Count - 1; index >= 0; index--)
                _dish.State.TryRemove(_dish.State.Components[index].InstanceId);
            _ingredients.Clear();
        }

        public void RefreshComposition()
        {
            if (!isActiveAndEnabled || _dish == null || !_dish.isActiveAndEnabled || _dish.State == null ||
                _dish.State.IsFinalized || _dish.State.IsDisposed || _simulation == null) return;
            if (_assemblyZone == null || !_assemblyZone.enabled || !_assemblyZone.isTrigger || !_assemblyZone.gameObject.activeInHierarchy)
            { ClearDraft(); return; }
            Physics.SyncTransforms();
            Transform zone = _assemblyZone.transform;
            Vector3 scale = zone.lossyScale;
            Vector3 halfSize = Vector3.Scale(_assemblyZone.size * 0.5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Collider[] overlaps = Physics.OverlapBox(zone.TransformPoint(_assemblyZone.center), halfSize, zone.rotation, ~0, QueryTriggerInteraction.Ignore);
            var candidates = new HashSet<FoodItem>();
            foreach (Collider collider in overlaps)
            {
                FoodItem food = collider.GetComponentInParent<FoodItem>();
                if (food == null || !food.isActiveAndEnabled || food.State == null) continue;
                Pickup pickup = food.GetComponent<Pickup>();
                if (pickup == null || !pickup.enabled || pickup.IsHeld || pickup.Body == null || pickup.Body.isKinematic) continue;
                bool registered = false;
                foreach (FoodItem registeredFood in _simulation.Foods) if (registeredFood == food) { registered = true; break; }
                if (registered) candidates.Add(food);
            }
            // Remove absent/deactivated/destroyed units before claiming additions; ownership is Domain-validated.
            var absent = new List<Guid>();
            foreach (FoodState component in _dish.State.Components)
            {
                bool present = false;
                foreach (FoodItem food in candidates) if (ReferenceEquals(food.State, component)) { present = true; break; }
                if (!present) absent.Add(component.InstanceId);
            }
            foreach (Guid id in absent) _dish.State.TryRemove(id);
            foreach (FoodItem food in candidates) _dish.State.TryAdd(food.State);
            _ingredients.Clear();
            foreach (FoodState component in _dish.State.Components)
                foreach (FoodItem food in candidates) if (ReferenceEquals(food.State, component)) { _ingredients.Add(food); break; }
            _ingredients.Sort(CompareIngredients);
            var order = new List<Guid>();
            foreach (FoodItem food in _ingredients) order.Add(food.State.InstanceId);
            _dish.State.TrySetOrder(order, IsAlignedStack());
        }

        private Vector3 Position(FoodItem food) => _assemblyZone.transform.InverseTransformPoint(food.GetComponent<Rigidbody>().worldCenterOfMass);
        private int CompareIngredients(FoodItem first, FoodItem second)
        {
            Vector3 a = Position(first), b = Position(second);
            float band = Mathf.Max(0.001f, _heightBand);
            int height = Mathf.RoundToInt(a.y / band).CompareTo(Mathf.RoundToInt(b.y / band));
            if (height != 0) return height;
            int x = a.x.CompareTo(b.x); if (x != 0) return x;
            int z = a.z.CompareTo(b.z); return z != 0 ? z : first.State.InstanceId.CompareTo(second.State.InstanceId);
        }
        private bool IsAlignedStack()
        {
            if (_ingredients.Count == 0) return false;
            Vector3 first = Position(_ingredients[0]);
            for (int index = 1; index < _ingredients.Count; index++)
            {
                Vector3 current = Position(_ingredients[index]), previous = Position(_ingredients[index - 1]);
                if (new Vector2(current.x - first.x, current.z - first.z).magnitude > _stackAlignmentTolerance ||
                    current.y - previous.y < Mathf.Max(0.001f, _heightBand)) return false;
            }
            return true;
        }

        public bool CanPlaceHeld(PhysicalCarry carry) => TryPlanPlacement(carry, out _, out _, out _);

        private bool TryPlanPlacement(PhysicalCarry carry, out FoodItem food, out Vector3 position, out Quaternion rotation)
        {
            food = null; position = default; rotation = Quaternion.identity;
            if (!isActiveAndEnabled || _dish == null || !_dish.isActiveAndEnabled || _dish.State == null ||
                _dish.State.IsFinalized || _dish.State.IsDisposed || _simulation == null || carry == null ||
                !carry.isActiveAndEnabled || !carry.HasHeldObject || _assemblyZone == null || !_assemblyZone.enabled ||
                !_assemblyZone.isTrigger || !_assemblyZone.gameObject.activeInHierarchy) return false;
            food = carry.HeldBody.GetComponent<FoodItem>();
            if (food == null || !food.isActiveAndEnabled || food.State == null) return false;
            bool registered = false;
            foreach (FoodItem unit in _simulation.Foods) if (unit == food) { registered = true; break; }
            if (!registered) return false;
            RefreshComposition();
            Rigidbody body = carry.HeldBody;
            rotation = _dish.transform.rotation;
            if (!AssemblyPlacement.TryBounds(body, rotation, out Bounds relative)) return false;
            BoxCollider tray = _dish.GetComponent<BoxCollider>();
            if (tray == null || !tray.enabled) return false;
            float top = tray.bounds.max.y;
            foreach (FoodItem ingredient in _ingredients)
                if (ingredient != null && AssemblyPlacement.TryBounds(ingredient.GetComponent<Rigidbody>(), ingredient.transform.rotation, out Bounds item))
                    top = Mathf.Max(top, ingredient.transform.position.y + item.max.y);
            position = new Vector3(tray.bounds.center.x - relative.center.x, top + Mathf.Max(0.001f, _snapGap) - relative.min.y,
                tray.bounds.center.z - relative.center.z);
            var proposed = new Bounds(relative.center + position, relative.size);
            return AssemblyPlacement.Fits(_assemblyZone, proposed) && AssemblyPlacement.IsClear(proposed, body);
        }

        public bool TryPlaceHeld(PhysicalCarry carry)
        {
            if (!TryPlanPlacement(carry, out FoodItem food, out Vector3 position, out Quaternion rotation) ||
                !_dish.State.TryAdd(food.State)) return false; // Claim only after preflight; foreign ownership fails before release.
            Rigidbody body = carry.HeldBody;
            carry.Drop(); // Restore settings, claim and player collision pairs through the existing M2 path.
            body.position = position; body.rotation = rotation;
            food.transform.SetPositionAndRotation(position, rotation);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms(); body.Sleep();
            RefreshComposition();
            return true;
        }

        public override bool CanInteract(InteractionContext context) => isActiveAndEnabled && _dish != null &&
            _dish.isActiveAndEnabled && _dish.State != null && !_dish.State.IsFinalized && !_dish.State.IsDisposed && _dish.State.Components.Count > 0 &&
            (context.Carry == null || !context.Carry.HasHeldObject);

        public override bool TryInteract(InteractionContext context)
        {
            RefreshComposition();
            return CanInteract(context) && _dish.FinalizeAssembly(_ingredients, _profiles);
        }
    }
}
