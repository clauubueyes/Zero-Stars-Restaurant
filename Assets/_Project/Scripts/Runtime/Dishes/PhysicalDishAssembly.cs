using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Dishes
{
    // Recognizes a connected, supported physical stack; no station, snap or food-state mutation.
    public sealed class PhysicalDishAssembly : MonoBehaviour
    {
        [SerializeField] private FoodSimulation _simulation;
        [SerializeField] private DishDefinition[] _definitions = Array.Empty<DishDefinition>();
        [SerializeField, Min(.001f)] private float _contactTolerance = .025f;
        [SerializeField, Min(0f)] private float _alignmentTolerance = .18f;
        private DishProfile[] Profiles()
        {
            var profiles = new DishProfile[_definitions.Length];
            for (int i = 0; i < profiles.Length; i++) profiles[i] = _definitions[i].CreateProfile();
            return profiles;
        }

        public IReadOnlyList<FoodItem> FindStack(FoodItem focus)
        {
            var stack = new List<FoodItem>();
            if (!isActiveAndEnabled || _simulation == null || !Eligible(focus)) return stack;
            Physics.SyncTransforms();
            if (!Contains(_simulation.Foods, focus)) return stack;
            stack.Add(focus);
            // Connectivity, rather than a large volume, keeps nearby independent stacks separate.
            for (int index = 0; index < stack.Count; index++)
                foreach (FoodItem candidate in _simulation.Foods)
                    if (Eligible(candidate) && !stack.Contains(candidate) && Touching(stack[index], candidate)) stack.Add(candidate);
            stack.Sort((a, b) =>
            {
                int height = a.GetComponent<Rigidbody>().worldCenterOfMass.y.CompareTo(b.GetComponent<Rigidbody>().worldCenterOfMass.y);
                return height != 0 ? height : a.State.InstanceId.CompareTo(b.State.InstanceId);
            });
            if (!HasSupport(stack)) stack.Clear();
            return stack;
        }

        private static bool Contains(IReadOnlyList<FoodItem> foods, FoodItem food)
        { foreach (FoodItem unit in foods) if (unit == food) return true; return false; }

        private static bool Eligible(FoodItem food)
        {
            if (food == null || !food.isActiveAndEnabled || food.State == null) return false;
            Pickup pickup = food.GetComponent<Pickup>();
            return pickup != null && pickup.isActiveAndEnabled && !pickup.IsHeld && pickup.Body != null &&
                !pickup.Body.isKinematic && BoundsFor(food, out _);
        }

        private static bool BoundsFor(FoodItem food, out Bounds bounds)
        {
            Rigidbody body = food.GetComponent<Rigidbody>();
            bool found = AssemblyPlacement.TryBounds(body, body.rotation, out bounds);
            bounds.center += body.position;
            return found;
        }

        private bool Touching(FoodItem a, FoodItem b)
        {
            BoundsFor(a, out Bounds first); BoundsFor(b, out Bounds second);
            float gap = Mathf.Min(Mathf.Abs(first.max.y - second.min.y), Mathf.Abs(second.max.y - first.min.y));
            return gap <= _contactTolerance && first.min.x < second.max.x && first.max.x > second.min.x &&
                first.min.z < second.max.z && first.max.z > second.min.z;
        }

        private bool HasSupport(List<FoodItem> stack)
        {
            BoundsFor(stack[0], out Bounds bottom);
            Vector3 start = new Vector3(bottom.center.x, bottom.min.y + _contactTolerance, bottom.center.z);
            RaycastHit? nearest = null;
            foreach (RaycastHit hit in Physics.RaycastAll(start, Vector3.down, _contactTolerance * 2f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                FoodItem food = hit.collider.GetComponentInParent<FoodItem>();
                if (food != null && stack.Contains(food)) continue;
                if (!nearest.HasValue || hit.distance < nearest.Value.distance) nearest = hit;
            }
            return nearest.HasValue && nearest.Value.normal.y >= .6f &&
                nearest.Value.collider.GetComponentInParent<FoodItem>() == null &&
                nearest.Value.collider.GetComponentInParent<CharacterController>() == null;
        }

        private bool Compose(DishState state, IReadOnlyList<FoodItem> stack)
        {
            var ids = new List<Guid>();
            bool aligned = true;
            for (int i = 0; i < stack.Count; i++)
            {
                if (!state.TryAdd(stack[i].State)) return false;
                ids.Add(stack[i].State.InstanceId);
                Vector3 position = stack[i].GetComponent<Rigidbody>().worldCenterOfMass;
                Vector3 first = stack[0].GetComponent<Rigidbody>().worldCenterOfMass;
                if (new Vector2(position.x - first.x, position.z - first.z).magnitude > _alignmentTolerance ||
                    (i > 0 && position.y - stack[i - 1].GetComponent<Rigidbody>().worldCenterOfMass.y < .02f)) aligned = false;
            }
            return stack.Count > 0 && state.TrySetOrder(ids, aligned);
        }

        public string PreviewName(FoodItem focus)
        {
            using (var preview = new DishState())
                return Compose(preview, FindStack(focus)) ? preview.Recognize(Profiles())?.DisplayName ?? "Custom Dish" : null;
        }

        public bool CanPlaceHeld(PhysicalCarry carry, RaycastHit hit, float maximumDistance) =>
            TryPlanPlacement(carry, hit, maximumDistance, out _, out _, out _);

        private bool TryPlanPlacement(PhysicalCarry carry, RaycastHit hit, float maximumDistance,
            out FoodItem food, out Vector3 position, out Quaternion rotation)
        {
            food = null; position = default; rotation = Quaternion.identity;
            if (!isActiveAndEnabled || _simulation == null || carry == null || !carry.HasHeldObject ||
                hit.collider == null || !hit.collider.enabled || !hit.collider.gameObject.activeInHierarchy || hit.collider.isTrigger) return false;
            Rigidbody body = carry.HeldBody;
            food = body.GetComponent<FoodItem>();
            if (food == null || !food.isActiveAndEnabled || food.State == null || !Contains(_simulation.Foods, food)) return false;
            DishItem targetDish = hit.collider.GetComponentInParent<DishItem>();
            if (targetDish != null && (targetDish.State == null || targetDish.State.IsFinalized)) return false;
            FoodItem target = hit.collider.GetComponentInParent<FoodItem>();
            Vector3 center = hit.point;
            float top = hit.point.y;
            rotation = Quaternion.Euler(0, body.rotation.eulerAngles.y, 0);
            using (var preflight = new DishState())
            {
                if (target != null)
                {
                    IReadOnlyList<FoodItem> stack = FindStack(target);
                    if (!Compose(preflight, stack)) return false;
                    BoundsFor(stack[0], out Bounds bottom); center = bottom.center;
                    top = bottom.max.y;
                    foreach (FoodItem ingredient in stack)
                    { BoundsFor(ingredient, out Bounds bounds); top = Mathf.Max(top, bounds.max.y); }
                    rotation = Quaternion.Euler(0, stack[0].transform.eulerAngles.y, 0);
                }
                else if (hit.normal.y < .9f || hit.collider.GetComponentInParent<CharacterController>() != null) return false;
                if (!preflight.TryAdd(food.State)) return false;
            }
            if (!AssemblyPlacement.TryBounds(body, rotation, out Bounds relative)) return false;
            position = new Vector3(center.x - relative.center.x, top + .004f - relative.min.y, center.z - relative.center.z);
            return Vector3.Distance(body.position, position) <= maximumDistance &&
                AssemblyPlacement.IsClear(new Bounds(relative.center + position, relative.size), body);
        }

        public bool TryPlaceHeld(PhysicalCarry carry, RaycastHit hit, float maximumDistance)
        {
            if (!TryPlanPlacement(carry, hit, maximumDistance, out FoodItem food, out Vector3 position, out Quaternion rotation)) return false;
            Rigidbody body = carry.HeldBody;
            carry.Drop();
            body.position = position; body.rotation = rotation;
            food.transform.SetPositionAndRotation(position, rotation);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms(); body.Sleep();
            return true;
        }

        public bool TryFinalize(FoodItem focus, out DishItem dish)
        {
            dish = null;
            IReadOnlyList<FoodItem> stack = FindStack(focus);
            if (stack.Count == 0) return false;
            // Invisible transport root: the original food visuals are the dish. No tray/food cloning.
            var root = new GameObject("Physical Dish", typeof(Rigidbody), typeof(BoxCollider), typeof(DishItem));
            root.transform.position = stack[0].transform.position;
            root.GetComponent<BoxCollider>().size = Vector3.one * .001f;
            Physics.SyncTransforms();
            DishItem result = root.GetComponent<DishItem>();
            if (!Compose(result.State, stack) || !result.FinalizeAssembly(stack, Profiles()))
            { result.State.Dispose(); root.SetActive(false); Destroy(root); return false; }
            dish = result;
            return true;
        }
    }
}
