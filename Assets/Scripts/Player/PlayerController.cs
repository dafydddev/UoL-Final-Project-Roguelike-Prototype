using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    // Top-down player movement: reads a 2D move input and drives the Rigidbody2D directly.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")] public float moveSpeed = 5f;

        // The move action. Defaults to a Vector2 value action; bindings are added at runtime if none are set.
        // TODO this needs to be updated to make the controls rebindable.
        [Header("Input")] public InputActionProperty moveAction =
            new(new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2"));

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

        private void OnEnable()
        {
            // Provide default WASD + left-stick bindings if the action has none configured.
            var a = moveAction.action;
            if (a.bindings.Count == 0)
            {
                a.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                a.AddBinding("<Gamepad>/leftStick");
            }

            a.Enable();
        }

        // Stop listening for input when disabled.
        private void OnDisable() => moveAction.action.Disable();

        private void Update()
        {
            // Read input each frame, clamping diagonals so they aren't faster than cardinal moves.
            _input = moveAction.action.ReadValue<Vector2>();
            if (_input.sqrMagnitude > 1f) _input.Normalize();
        }

        // Apply movement in the physics step.
        private void FixedUpdate()
        {
            _rb.linearVelocity = _input * moveSpeed;
        }
    }
}