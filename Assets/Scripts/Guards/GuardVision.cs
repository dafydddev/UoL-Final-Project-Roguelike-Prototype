using Guards.Goap;
using Items;
using Player;
using UnityEngine;

namespace Guards
{
    [RequireComponent(typeof(GuardAgent))]
    public class GuardVision : MonoBehaviour
    {
        [Header("Cone")]
        public float viewRange = 6f;
        public float viewAngle = 90f;
        public float pointBlankRange = 1.2f;
        public float loseSightGrace = 0.3f;

        [Header("Layers")]
        public LayerMask obstacleMask;
        public LayerMask playerMask;

        private GuardAgent _agent;
        private Rigidbody2D _rb;
        private Transform _player;
        private PlayerHiding _hiding;
        private bool _seenLastFrame;
        private float _unseenTimer;

        private Vector2 _facing = Vector2.right;

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
                _unseenTimer = 0f;
                if (!_seenLastFrame) _agent.OnPlayerSighted(_player);
                _seenLastFrame = true;
            }
            else if (_seenLastFrame)
            {
                _unseenTimer += Time.deltaTime;
                if (_unseenTimer >= loseSightGrace)
                {
                    _agent.OnPlayerLost();
                    _seenLastFrame = false;
                }
            }

            var distraction = Distraction.Nearest(transform.position);
            if (distraction && CanSee(distraction.transform.position)) _agent.OnDistractionSeen(distraction);
        }

        private void UpdateFacing()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.01f)
                _facing = _rb.linearVelocity.normalized;
        }

        private bool CanSeePlayer()
        {
            var hit = Physics2D.OverlapCircle(transform.position, viewRange, playerMask);
            if (!hit) return false;

            if (hit.transform != _player)
            {
                _player = hit.transform;
                _hiding = hit.GetComponentInParent<PlayerHiding>();
            }

            if (_hiding && _hiding.IsHidden && !_agent.IsChasing) return false;

            return CanSee(hit.transform.position);
        }

        private bool CanSee(Vector3 worldPoint)
        {
            var to = (Vector2)(worldPoint - transform.position);
            var dist = to.magnitude;
            if (dist > viewRange) return false;

            var pointBlank = dist <= pointBlankRange;
            if (!pointBlank && Vector2.Angle(_facing, to) > viewAngle * 0.5f) return false;

            var blocker = Physics2D.Raycast(transform.position, to.normalized, dist, obstacleMask);
            return !blocker.collider;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.4f);
            var f = Application.isPlaying ? _facing : Vector2.right;
            var half = viewAngle * 0.5f;
            var left = Quaternion.Euler(0, 0, half) * f;
            var right = Quaternion.Euler(0, 0, -half) * f;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)left * viewRange);
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)right * viewRange);
            Gizmos.DrawWireSphere(transform.position, viewRange);
        }
    }
}