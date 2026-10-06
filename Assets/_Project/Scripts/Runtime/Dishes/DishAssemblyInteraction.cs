using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Dishes
{
    // Secondary assembly intent leaves the nearest ingredient's generic E pickup available.
    [DefaultExecutionOrder(110)]
    public sealed class DishAssemblyInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private InteractionDetector _detector;
        [SerializeField] private PhysicalCarry _carry;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private AssemblySurface[] _surfaces = Array.Empty<AssemblySurface>();
        [SerializeField] private PhysicalDishAssembly _physicalAssembly;
        private InputActionAsset _ownedActions;
        private InputAction _finalize, _place, _interact, _drop, _throw;
        private bool _hadControl;
        public string FinalizeBinding => _finalize != null ? _finalize.GetBindingDisplayString(group: "Keyboard&Mouse") : "?";
        public string PlaceBinding => _place != null ? _place.GetBindingDisplayString(group: "Keyboard&Mouse") : "?";

        private void Awake()
        {
            if (_interaction == null || _detector == null || _carry == null || _inputActions == null)
            { Debug.LogError("Dish assembly input needs interaction, detector, carry and input actions.", this); enabled = false; return; }
            _ownedActions = Instantiate(_inputActions);
            _ownedActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            _finalize = _ownedActions.FindAction("Player/FinalizeDish");
            _place = _ownedActions.FindAction("Player/PlaceIngredient");
            _interact = _ownedActions.FindAction("Player/Interact");
            _drop = _ownedActions.FindAction("Player/Drop");
            _throw = _ownedActions.FindAction("Player/Throw");
            if (_finalize == null || _place == null || _interact == null || _drop == null || _throw == null)
            { Debug.LogError("Dish assembly needs FinalizeDish, PlaceIngredient and M2 intent actions.", this); enabled = false; }
        }

        private void OnEnable()
        {
            if (_finalize == null || _place == null || _interact == null || _drop == null || _throw == null) return;
            _finalize.Enable(); _place.Enable(); _interact.Enable(); _drop.Enable(); _throw.Enable();
        }
        private void OnDisable() { _ownedActions?.Disable(); _hadControl = false; }
        private void OnDestroy() { if (_ownedActions != null) Destroy(_ownedActions); }
        private void Update()
        {
            if (_interaction == null || !_interaction.HasControl || _finalize == null) { _hadControl = false; return; }
            if (!_hadControl) { _hadControl = true; return; } // The click recapturing the cursor never places food.
            // M2 wins simultaneous requests: dropping an ingredient must not also finalize it.
            if (_interact.WasPressedThisFrame() || _drop.WasPressedThisFrame() || _throw.WasPressedThisFrame()) return;
            if (_finalize.WasPressedThisFrame()) TryFinalize();
        }

        public AssemblySurface FindFocusedSurface()
        {
            if (!isActiveAndEnabled || _detector == null) return null;
            // Reuse the FIRST solid hit. Never skip unrelated objects or raycast through a wall.
            Interactable target = _detector.Detect(_carry != null ? _carry.HeldBody : null);
            if (target == null) return null;
            AssemblySurface direct = target as AssemblySurface;
            if (direct != null)
            { direct.RefreshComposition(); return IsDraft(direct) ? direct : null; }
            FoodItem food = target.GetComponentInParent<FoodItem>();
            if (food == null || !food.isActiveAndEnabled || food.State == null) return null;
            foreach (AssemblySurface surface in _surfaces)
            {
                if (!IsDraft(surface)) continue;
                surface.RefreshComposition();
                foreach (FoodState ingredient in surface.Dish.State.Components)
                    if (ReferenceEquals(ingredient, food.State)) return surface;
            }
            return null;
        }
        private static bool IsDraft(AssemblySurface surface) => surface != null && surface.isActiveAndEnabled &&
            surface.Dish != null && surface.Dish.isActiveAndEnabled && surface.Dish.State != null &&
            !surface.Dish.State.IsFinalized && !surface.Dish.State.IsDisposed;

        public string ConfirmationPrompt(AssemblySurface surface)
        {
            if (!IsDraft(surface)) return string.Empty;
            if (_carry != null && _carry.HasHeldObject)
                return "Release LMB / drop held object first";
            if (!surface.CanInteract(new InteractionContext(transform, _carry))) return "Place ingredients on this tray";
            return "[" + FinalizeBinding + "] Finalize " + surface.DisplayName;
        }

        public bool TryFinalize()
        {
            if (!isActiveAndEnabled || _detector == null) return false;
            AssemblySurface surface = FindFocusedSurface(); // Fresh hit, composition and ownership at intent time.
            if (surface != null) return surface.TryInteract(new InteractionContext(transform, _carry));
            if (_carry != null && _carry.HasHeldObject) return false;
            FoodItem food = _detector.Detect()?.GetComponentInParent<FoodItem>();
            return _physicalAssembly != null && _physicalAssembly.TryFinalize(food, out _);
        }

        public string PhysicalConfirmationPrompt()
        {
            if (!isActiveAndEnabled || _detector == null || _physicalAssembly == null ||
                (_carry != null && _carry.HasHeldObject) || FindFocusedSurface() != null) return null;
            FoodItem food = _detector.Detect()?.GetComponentInParent<FoodItem>();
            string name = _physicalAssembly.PreviewName(food);
            return name != null ? "Recognized: " + name + "\n[" + FinalizeBinding + "] Finalize " + name : null;
        }

        public bool TryPlace()
        {
            // Release any previous draft's claim on this held unit before selecting the current first hit.
            foreach (AssemblySurface candidate in _surfaces) if (candidate != null) candidate.RefreshComposition();
            AssemblySurface surface = FindFocusedSurface();
            return surface != null && surface.TryPlaceHeld(_carry);
        }
    }
}
