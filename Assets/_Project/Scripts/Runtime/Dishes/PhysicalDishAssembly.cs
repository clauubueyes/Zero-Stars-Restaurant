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
