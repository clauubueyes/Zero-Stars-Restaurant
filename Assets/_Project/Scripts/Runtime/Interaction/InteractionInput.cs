using UnityEngine;
using UnityEngine.InputSystem;

namespace ZeroStarRestaurant.Interaction
{
    [DefaultExecutionOrder(100)] // Read intent after M1 has updated camera and cursor state.
    public sealed class InteractionInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private PlayerInteraction _interaction;
        private InputActionAsset _ownedActions;
        private InputAction _interact;
        private InputAction _drop;
        private InputAction _throw;

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
        }

        private void OnDisable()
        {
            _ownedActions?.Disable();
            if (_interaction != null)
                _interaction.Drop();
        }

        private void OnDestroy()
        {
            if (_ownedActions != null)
                Destroy(_ownedActions);
        }

        private void Update()
        {
            if (!_interaction.HasControl)
                return;
            // One intent per frame. A simultaneous drop/throw cannot also grab a new object.
            if (_throw.WasPressedThisFrame()) _interaction.Throw();
            else if (_drop.WasPressedThisFrame()) _interaction.Drop();
            else if (_interact.WasPressedThisFrame()) _interaction.TryInteract();
        }
    }
}
