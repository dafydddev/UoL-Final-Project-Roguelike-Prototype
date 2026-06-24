using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    // Top-down player movement: reads a 2D move input and drives the Rigidbody2D directly.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")] public float moveSpeed = 5f;

        // The move action, referencing the Move action in the InputSystem_Actions asset.
        [Header("Input")] public InputActionReference moveAction;

        private Rigidbody2D _rb;
        private Vector2 _input; // current movement input, cached each frame

        private void Awake()
        {
            // Configure the body for top-down 2D movement (no gravity, no rotation, smooth motion).
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void OnEnable() => moveAction.action.Enable();

        // Stop listening for input when disabled.
        private void OnDisable() => moveAction.action.Disable();

        private void Update()
        {
            // Read input each frame, clamping diagonals so they aren't faster than cardinal moves.
            _input = moveAction.action.ReadValue<Vector2>();
            if (_input.sqrMagnitude > 1f) _input.Normalize();
        }

        // Apply movement in the physics step.
        private void FixedUpdate() => _rb.linearVelocity = _input * moveSpeed;
    }
}