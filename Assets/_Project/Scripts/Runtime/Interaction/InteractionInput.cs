using UnityEngine;
using UnityEngine.InputSystem;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Hygiene;

namespace ZeroStarRestaurant.Interaction
{
    [DefaultExecutionOrder(100)] // Read intent after M1 has updated camera and cursor state.
    public sealed class InteractionInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private DishAssemblyInteraction _assembly;
        [SerializeField] private CleaningInteraction _cleaning;
        private InputActionAsset _ownedActions;
        private InputAction _interact;
        private InputAction _drop;
        private InputAction _throw;
        private InputAction _physicalHold;
        private bool _mouseHolding, _hadControl;
        public bool IsMouseHolding => _mouseHolding && _interaction != null && _interaction.HasHeldObject;
        public string AssistedPlacementPrompt => IsMouseHolding && _assembly != null ? _assembly.AssistedPlacementPrompt : null;

        public string InteractBinding => BindingLabel(_interact);
        public string DropBinding => BindingLabel(_drop);
        public string ThrowBinding => BindingLabel(_throw);

        private static string BindingLabel(InputAction action)
        {
            return action != null ? action.GetBindingDisplayString(group: "Keyboard&Mouse") : "?";
        }

        private void Awake()
        {
            if (_inputActions == null || _interaction == null)
            {
                Debug.LogError("Interaction input needs its action asset and PlayerInteraction.", this);
                enabled = false;
                return;
            }
            _ownedActions = Instantiate(_inputActions);
            _ownedActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            _interact = _ownedActions.FindAction("Player/Interact");
            _drop = _ownedActions.FindAction("Player/Drop");
            _throw = _ownedActions.FindAction("Player/Throw");
            _physicalHold = new InputAction("PhysicalHold", InputActionType.Button, "<Mouse>/leftButton");
            if (_interact == null || _drop == null || _throw == null)
            {
                Debug.LogError("Interaction input needs Player/Interact, Player/Drop and Player/Throw.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_interact == null || _drop == null || _throw == null)
                return;
            _interact.Enable(); _drop.Enable(); _throw.Enable();
            _physicalHold?.Enable();
        }

        private void OnDisable()
        {
            _ownedActions?.Disable();
            _physicalHold?.Disable();
            _mouseHolding = false; _hadControl = false;
            _cleaning?.Stop();
            if (_interaction != null)
                _interaction.Drop();
        }

        private void OnDestroy()
        {
            _physicalHold?.Dispose();
            if (_ownedActions != null)
                Destroy(_ownedActions);
        }

        private void Update() => ProcessInput(_interaction.HasControl, Time.deltaTime);

        // Explicit control/time lets the same input path run without an OS cursor in tests.
        private void ProcessInput(bool hasControl, double elapsedSeconds)
        {
            if (!hasControl)
            {
                _cleaning?.Stop();
                if (_mouseHolding) _interaction.Drop();
                _mouseHolding = false; _hadControl = false; return;
            }
            bool canGrab = _hadControl;
            _hadControl = true;
            if (UpdateMouseHold(canGrab && !_drop.WasPressedThisFrame() && !_interact.WasPressedThisFrame())) { _cleaning?.Stop(); return; }
            // One intent per frame. A simultaneous drop/throw cannot also grab a new object.
            // Legacy Throw remains callable for development; release is the primary mouse path.
            if (_drop.WasPressedThisFrame()) { _cleaning?.Stop(); _interaction.Drop(); _mouseHolding = false; }
            else if (_cleaning != null && _cleaning.HasHeldTool) _cleaning.Advance(elapsedSeconds, _interact.IsPressed(), true);
            else { _cleaning?.Stop(); if (_interact.WasPressedThisFrame()) _interaction.TryInteract(); }
        }

        private bool UpdateMouseHold(bool canGrab)
        {
            if (_mouseHolding && !_physicalHold.IsPressed())
            {
                if (_drop.WasPressedThisFrame()) _interaction.Drop();
                else if (_assembly == null || !_assembly.TryAssistRelease()) _interaction.ReleaseFromMouse();
                _mouseHolding = false; return true;
            }
            if (!canGrab || !_physicalHold.WasPressedThisFrame()) return false;
            _mouseHolding = _interaction.HasHeldObject || _interaction.TryGrabPhysical();
            return _mouseHolding;
        }
    }
}
