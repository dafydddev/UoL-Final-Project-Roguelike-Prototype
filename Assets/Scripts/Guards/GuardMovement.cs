using System.Collections.Generic;
using Navigation;
using UnityEngine;

namespace Guards
{
    // Drives a guard's Rigidbody2D along either an A* path (SetDestination) or straight at a moving target (SeekDirect).
    // Applies the personality's sway and coasts smoothly through behaviour swaps rather than snapping to a stop.
    [RequireComponent(typeof(Rigidbody2D))]
    public class GuardMovement : MonoBehaviour
    {
        [Header("Movement")] public float moveSpeed = 3f;
        public float arriveTolerance = 0.1f; // how close counts as reaching a waypoint

        private FacilityNavigation _nav;
        private Rigidbody2D _rb;
        private GuardPersonality _personality;

        private readonly List<Vector3> _path = new(); // current A* path, in world space
        private int _index; // index of the waypoint we're heading to

        private bool _seeking; // true when chasing a direct target instead of a path
        private Vector3 _seekTarget;
        private bool _halted; // true only after an explicit Stop(); coast otherwise

        private bool HasPath => _index < _path.Count;
        public bool HasArrived => !_seeking && !HasPath;

        private void Awake()
        {
            // Configure the body for top-down 2D movement (no gravity/rotation, smooth motion).
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Init(FacilityNavigation navigation) => _nav = navigation;
        public void SetPersonality(GuardPersonality p) => _personality = p;

        // Paths to a world target via A*. Returns false if navigation isn't ready or no route exists.
        public bool SetDestination(Vector3 worldTarget)
        {
            _seeking = false;
            _halted = false;
            _index = 0;

            if (!_nav || !_nav.Ready)
            {
                _path.Clear();
                return false;
            }

            var route = _nav.FindWorldPath(transform.position, worldTarget);
            if (route == null || route.Count == 0)
            {
                _path.Clear();
                return false;
            }

            _path.Clear();
            _path.AddRange(route);

            // route[0] is the guard's own cell centre; skip it so it doesn't back-step there first.
            if (_path.Count > 1) _index = 1;

            return true;
        }

        // Heads straight at a target (e.g. the player) without pathfinding; updated each tick by the caller.
        public void SeekDirect(Vector3 worldTarget)
        {
            _seeking = true;
            _halted = false;
            _seekTarget = worldTarget;
            _path.Clear();
            _index = 0;
        }

        // Stops dead and clears any path. Velocity stays zero until a new command is issued.
        public void Stop()
        {
            _seeking = false;
            _halted = true;
            _path.Clear();
            _index = 0;
            _rb.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            // Explicitly stopped: stay put.
            if (_halted)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            // Chasing a direct target: steer straight at it.
            if (_seeking)
            {
                Drive(_seekTarget - transform.position);
                return;
            }

            if (!HasPath)
            {
                // No path and not halted: a behaviour swap is in progress. Coast on the
                // current velocity instead of snapping to zero, so the transition is smooth.
                return;
            }

            // Steer toward the current waypoint.
            var target = _path[_index];
            var toTarget = (Vector2)(target - transform.position);

            // Reached this waypoint: advance to the next one (or coast if the path is done).
            if (toTarget.magnitude <= arriveTolerance)
            {
                _index++;
                if (!HasPath) return; // coast; the next action will issue a steer
                target = _path[_index];
                toTarget = target - transform.position;
            }

            Drive(toTarget);
        }

        // Sets velocity toward the given offset, applying the personality's sideways sway.
        private void Drive(Vector2 toTarget)
        {
            // Effectively on top of the target: stop to avoid jitter.
            if (toTarget.sqrMagnitude <= 0.0004f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var dir = toTarget.normalized;
            var perp = new Vector2(-dir.y, dir.x); // sideways direction
            var sway = _personality ? _personality.SwayOffset : 0f;

            // Blend in the sway offset, then drive at full speed along the result.
            var steer = (dir + perp * sway).normalized;
            _rb.linearVelocity = steer * moveSpeed;
        }
    }
}