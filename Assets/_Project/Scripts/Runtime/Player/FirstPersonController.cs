using UnityEngine;
using UnityEngine.InputSystem;

namespace ZeroStarRestaurant.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FirstPersonMotor))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Transform _viewTransform;

        [Header("Mouse look")]
        [SerializeField, Min(0.01f)] private float _mouseSensitivity = 0.1f;
        [SerializeField, Range(1f, 89f)] private float _pitchLimit = 85f;

        private FirstPersonMotor _motor;
        private InputActionAsset _ownedActions;
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _releaseCursor;
        private InputAction _captureCursor;
        private float _pitch;
        private bool _cursorCaptured;
        private bool _skipLookFrame;

        private void Awake()
        {
            _motor = GetComponent<FirstPersonMotor>();
            if (_inputActions == null || _viewTransform == null ||
                !_viewTransform.IsChildOf(transform))
            {
                Debug.LogError("FPS player needs an InputActionAsset and a child camera transform.", this);
                enabled = false;
                return;
            }

            // Own action state: enabling this player never enables/mutates the shared asset.
            _ownedActions = Instantiate(_inputActions);
            _ownedActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            _move = _ownedActions.FindAction("Player/Move");
            _look = _ownedActions.FindAction("Player/Look");
            _jump = _ownedActions.FindAction("Player/Jump");
            if (_move == null || _look == null || _jump == null)
            {
                Debug.LogError("FPS input needs Player/Move, Player/Look and Player/Jump actions.", this);
                enabled = false;
                return;
            }

            // Cursor controls belong to this adapter, not to world interaction.
            _releaseCursor = new InputAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
            _captureCursor = new InputAction("CaptureCursor", InputActionType.Button, "<Mouse>/leftButton");
            _pitch = Mathf.DeltaAngle(0f, _viewTransform.localEulerAngles.x);
        }

        private void OnEnable()
        {
            if (_releaseCursor == null)
                return;

            _move.Enable();
            _look.Enable();
            _jump.Enable();
            _releaseCursor.Enable();
            _captureCursor.Enable();
            CaptureCursor();
        }

        private void OnDisable()
        {
            _ownedActions?.Disable();
            _releaseCursor?.Disable();
            _captureCursor?.Disable();
            ReleaseCursor();
        }

        private void OnDestroy()
        {
            _releaseCursor?.Dispose();
            _captureCursor?.Dispose();
            if (_ownedActions != null)
                Destroy(_ownedActions);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                ReleaseCursor();
        }

        private void Update()
        {
            if (_releaseCursor.WasPressedThisFrame())
                ReleaseCursor();
            else if (!_cursorCaptured && _captureCursor.WasPressedThisFrame())
                CaptureCursor();

            // Unity can unlock independently (Escape, focus loss, Editor controls).
            if (_cursorCaptured && Cursor.lockState != CursorLockMode.Locked)
                ReleaseCursor();

            if (_cursorCaptured && !_skipLookFrame)
            {
                // Mouse delta is displacement, so it must NOT be multiplied by deltaTime.
                Vector2 look = _look.ReadValue<Vector2>() * _mouseSensitivity;
                transform.Rotate(0f, look.x, 0f, Space.Self);
                _pitch = Mathf.Clamp(_pitch - look.y, -_pitchLimit, _pitchLimit);
                _viewTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
            _skipLookFrame = false;

            Vector2 input = _cursorCaptured ? _move.ReadValue<Vector2>() : Vector2.zero;
            // Gravity continues while the cursor is free, including during a jump.
            _motor.Step(input, _cursorCaptured && _jump.WasPressedThisFrame(), Time.deltaTime);
        }

        private void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorCaptured = true;
            _skipLookFrame = true;
        }

        private void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorCaptured = false;
        }

        private void OnValidate()
        {
            _mouseSensitivity = Mathf.Max(0.01f, _mouseSensitivity);
            _pitchLimit = Mathf.Clamp(_pitchLimit, 1f, 89f);
        }
    }
}
