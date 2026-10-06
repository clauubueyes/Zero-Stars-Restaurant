using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Dishes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class DishItem : MonoBehaviour
    {
        private readonly List<FoodItem> _ingredients = new List<FoodItem>();
        public DishState State { get; private set; }
        public bool IsIntact
        {
            get
            {
                if (State == null || State.IsDisposed) return false;
                if (!State.IsFinalized) return true;
                if (_ingredients.Count != State.Components.Count) return false;
                for (int index = 0; index < _ingredients.Count; index++)
                    if (_ingredients[index] == null || !ReferenceEquals(_ingredients[index].State, State.Components[index])) return false;
                return true;
            }
        }

        private void Awake() => State = new DishState();
        private void OnDestroy() => State?.Dispose();

        public bool FinalizeAssembly(IReadOnlyList<FoodItem> ingredients, IReadOnlyList<DishProfile> definitions)
        {
            if (!isActiveAndEnabled || State == null || State.IsFinalized || ingredients == null ||
                ingredients.Count != State.Components.Count || ingredients.Count == 0) return false;
            Rigidbody body = GetComponent<Rigidbody>();
            BoxCollider proxy = GetComponent<BoxCollider>();
            // Validate the complete physical transaction before committing the Domain composition.
            Bounds bounds = proxy.bounds;
            float mass = 0.2f;
            for (int index = 0; index < ingredients.Count; index++)
            {
                FoodItem food = ingredients[index];
                if (food == null || !food.isActiveAndEnabled || !ReferenceEquals(food.State, State.Components[index])) return false;
                Rigidbody ingredientBody = food.GetComponent<Rigidbody>();
                Pickup pickup = food.GetComponent<Pickup>();
                if (ingredientBody == null || ingredientBody.isKinematic || pickup == null || !pickup.enabled || pickup.IsHeld) return false;
                bool hasCollider = false;
                foreach (Collider collider in food.GetComponentsInChildren<Collider>())
                    if (collider.enabled && !collider.isTrigger && collider.attachedRigidbody == ingredientBody)
                    { bounds.Encapsulate(collider.bounds); hasCollider = true; }
                if (!hasCollider) return false;
                mass += ingredientBody.mass;
            }
            if (!State.TryFinalize(definitions)) return false;

            // Original FoodItems/states remain alive and registered in FoodSimulation.
            // Only their independent physics/pickup are retired; one conservative proxy transports all visuals.
            transform.SetParent(null, true);
            foreach (FoodItem food in ingredients)
            {
                food.GetComponent<Pickup>().enabled = false;
                Rigidbody ingredientBody = food.GetComponent<Rigidbody>();
                ingredientBody.linearVelocity = Vector3.zero; ingredientBody.angularVelocity = Vector3.zero;
                ingredientBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                ingredientBody.interpolation = RigidbodyInterpolation.None;
                ingredientBody.isKinematic = true; ingredientBody.detectCollisions = false;
                foreach (Collider collider in food.GetComponentsInChildren<Collider>()) collider.enabled = false;
                food.transform.SetParent(transform, true);
                _ingredients.Add(food);
            }
            // Bounds are converted through all corners, also supporting an authored rotated tray.
            var localBounds = new Bounds(transform.InverseTransformPoint(bounds.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new Vector3((corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                    (corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
                    (corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                localBounds.Encapsulate(transform.InverseTransformPoint(bounds.center + offset));
            }
            proxy.center = localBounds.center; proxy.size = localBounds.size;
            body.mass = mass; body.isKinematic = false; body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.solverIterations = 12; body.maxLinearVelocity = 20f; body.maxAngularVelocity = 10f;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            gameObject.name = State.DisplayName;
            gameObject.AddComponent<Pickup>();
            return true;
        }
    }
}
