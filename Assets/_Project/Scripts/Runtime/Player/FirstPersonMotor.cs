using UnityEngine;

namespace ZeroStarRestaurant.Player
{
    // Unity physics adapter: accepts intent without knowing input, camera or visuals.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _moveSpeed = 4f;
        [SerializeField, Min(0f)] private float _jumpHeight = 1f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField, Min(1f)] private float _terminalFallSpeed = 40f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void Step(Vector2 movement, bool jumpPressed, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            movement = Vector2.ClampMagnitude(movement, 1f);
            Vector3 horizontal = (transform.right * movement.x + transform.forward * movement.y) * _moveSpeed;
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            if (_controller.isGrounded && jumpPressed && _jumpHeight > 0f)
                _verticalVelocity = Mathf.Sqrt(-2f * _gravity * _jumpHeight);

            _verticalVelocity = Mathf.Max(_verticalVelocity + _gravity * deltaTime, -_terminalFallSpeed);
            CollisionFlags collisions = _controller.Move((horizontal + Vector3.up * _verticalVelocity) * deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0.1f, _moveSpeed);
            _jumpHeight = Mathf.Max(0f, _jumpHeight);
            _gravity = Mathf.Min(-0.1f, _gravity);
            _terminalFallSpeed = Mathf.Max(1f, _terminalFallSpeed);
        }
    }
}
