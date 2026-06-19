using Guards.GOAP;
using Items;
using Player;
using UnityEngine;

namespace Guards
{
    // The guard's "eyes". Each frame it checks whether the player is within a view cone
    // (with a point-blank bubble) and unobstructed, and feeds sightings, sight-loss, and noticed
    // distractions to the GuardAgent. A grace period smooths brief losses of sight.
    [RequireComponent(typeof(GuardAgent))]
    public class GuardVision : MonoBehaviour
    {
        [Header("Cone")] public float viewRange = 6f; // how far the guard can see
        public float viewAngle = 90f; // full width of the view cone, in degrees
        public float pointBlankRange = 1.2f; // within this distance the guard sees in all directions
        public float loseSightGrace = 0.3f; // keep "seen" this long after sight is lost, to avoid flicker

        [Header("Layers")] public LayerMask obstacleMask; // what blocks line of sight
        public LayerMask playerMask; // what counts as the player

        private GuardAgent _agent;
        private Rigidbody2D _rb;
        private Transform _player;
        private bool _playerIsHidden;
        private bool _seenLastFrame;
        private float _unseenTimer; // time since the player was last visible

        private Vector2 _facing = Vector2.right; // current look direction (from movement)

        private void OnEnable() => PlayerHiding.OnHiddenChanged += OnHiddenChanged;
        private void OnDisable() => PlayerHiding.OnHiddenChanged -= OnHiddenChanged;

        private void Awake()
        {
            _agent = GetComponent<GuardAgent>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            UpdateFacing();

            var seen = CanSeePlayer();
            if (seen)
            {
                // Visible now: reset the grace timer and report a sighting on the rising edge.
                _unseenTimer = 0f;
                if (!_seenLastFrame) _agent.OnPlayerSighted(_player);
                _seenLastFrame = true;
            }
            else if (_seenLastFrame)
            {
                // Just lost sight: wait out the grace period before declaring the player lost.
                _unseenTimer += Time.deltaTime;
                if (_unseenTimer >= loseSightGrace)
                {
                    _agent.OnPlayerLost();
                    _seenLastFrame = false;
                }
            }

            // Independently, notice the nearest visible distraction.
            var distraction = Distraction.Nearest(transform.position);
            if (distraction && CanSee(distraction.transform.position)) _agent.OnDistractionSeen(distraction);
        }

        // Face the direction of travel (only while actually moving, so facing doesn't reset when idle).
        private void UpdateFacing()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.01f)
                _facing = _rb.linearVelocity.normalized;
        }

        private void OnHiddenChanged(bool hidden) => _playerIsHidden = hidden;

        // True if the player is in range, not hidden (unless already being chased), and in line of sight.
        private bool CanSeePlayer()
        {
            var hit = Physics2D.OverlapCircle(transform.position, viewRange, playerMask);
            if (!hit) return false;

            // Cache the player transform when it changes 
            if (!_player) _player = hit.transform;

            // A hidden player is invisible unless this guard is already actively chasing.
            if (_playerIsHidden && !_agent.IsChasing) return false;

            return CanSee(hit.transform.position);
        }

        // True if a world point is within range, inside the cone (or point-blank), and not occluded.
        private bool CanSee(Vector3 worldPoint)
        {
            var to = (Vector2)(worldPoint - transform.position);
            var dist = to.magnitude;
            if (dist > viewRange) return false;

            // Outside the cone counts as unseen, except at point-blank range.
            var pointBlank = dist <= pointBlankRange;
            if (!pointBlank && Vector2.Angle(_facing, to) > viewAngle * 0.5f) return false;

            // Blocked by an obstacle between guard and point → can't see it.
            var blocker = Physics2D.Raycast(transform.position, to.normalized, dist, obstacleMask);
            return !blocker.collider;
        }

        // Editor-only: draws the view cone edges and range when the guard is selected.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.4f);
            var f = Application.isPlaying ? _facing : Vector2.right; // use live facing in play mode
            var half = viewAngle * 0.5f;
            var left = Quaternion.Euler(0, 0, half) * f;
            var right = Quaternion.Euler(0, 0, -half) * f;
            Gizmos.DrawLine(transform.position, transform.position + left * viewRange);
            Gizmos.DrawLine(transform.position, transform.position + right * viewRange);
            Gizmos.DrawWireSphere(transform.position, viewRange);
        }
    }
}