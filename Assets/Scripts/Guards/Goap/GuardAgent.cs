using System;
using System.Collections.Generic;
using Navigation;
using Items;
using UnityEngine;

namespace Guards.GOAP
{
    // The guard's brain. Each frame it picks the highest-priority relevant goal,
    // has the planner build a plan of actions to reach it, and runs that plan step by step.
    // Vision and other systems feed it world facts (sees player, last-seen position, distractions).
    [RequireComponent(typeof(GuardMovement))]
    public class GuardAgent : MonoBehaviour
    {
        public GuardMovement Movement { get; private set; }
        public Transform Player { get; private set; }
        public bool SeesPlayer { get; private set; }

        public bool IsChasing { get; private set; }

        // Where the player was last seen, once sight is lost.
        public Vector3 LastSeen { get; private set; }
        public bool HasLastSeen { get; private set; }

        // A distraction the guard has noticed and may go investigate.
        public Distraction Distraction { get; private set; }
        public Vector3 DistractionPos { get; private set; }
        public bool HasDistraction { get; private set; }

        // Name of the current goal, exposed for the debug label.
        public string CurrentGoal => _activeGoal?.name ?? "—";

        private Vector3[] _waypoints; // patrol route
        private int _wp;              // current patrol waypoint index

        // The actions available to the planner.
        private readonly List<GuardAction> _actions = new()
            { new Patrol(), new MoveToPlayer(), new Investigate(), new InvestigateDistraction(), new Apprehend() };

        private List<Goal> _goals;

        // The current plan and where we are in it.
        private List<GuardAction> _plan;
        private int _planIndex;
        private GuardAction _current;
        private Goal _activeGoal;
        
        // The Actions events from the Guard agent.
        public static event Action OnPlayerCaught;
        public void CatchPlayer() => OnPlayerCaught?.Invoke();

        // Wires up movement and defines the goal set (priority order: Chase > Investigate > Distraction > Patrol).
        public void Init(FacilityNavigation nav, Vector3[] waypoints)
        {
            Movement = GetComponent<GuardMovement>();
            Movement.Init(nav);
            _waypoints = waypoints;

            _goals = new List<Goal>
            {
                new("Chase", 3, g => g.SeesPlayer),
                new("Investigate", 2, g => g.HasLastSeen),
                new("Distraction", 1, g => g.HasDistraction),
                new("Patrol", 0, g => true) // always relevant — the fallback goal
            };
            // The world state each goal wants to achieve.
            _goals[0].desired.Set(Key.CaughtPlayer, true);
            _goals[1].desired.Set(Key.AtLastSeen, true);
            _goals[2].desired.Set(Key.AtDistraction, true);
            _goals[3].desired.Set(Key.Patrolling, true);
        }

        private void Update()
        {
            if (!Movement || _goals == null) return;

            var goal = SelectGoal();

            // Replan when the goal changes (or we have no plan), then keep running the plan.
            if (goal != _activeGoal || _plan == null)
            {
                _activeGoal = goal;
                _plan = Planner.Plan(WorldState(), goal.desired, _actions);
                _planIndex = 0;
                _current = null;
            }

            RunPlan();
        }

        // Picks the highest-priority goal whose relevance test currently passes.
        private Goal SelectGoal()
        {
            Goal best = null;
            foreach (var g in _goals)
            {
                if (g.relevant(this) && (best == null || g.priority > best.priority))
                {
                    best = g;
                }
            }

            return best;
        }

        // Snapshots the facts the planner needs from the agent's current state.
        private State WorldState()
        {
            var s = new State();
            s.Set(Key.SeesPlayer, SeesPlayer);
            s.Set(Key.HasLastSeen, HasLastSeen);
            s.Set(Key.HasDistraction, HasDistraction);
            return s;
        }

        // Advances the current plan: enters the next action, ticks it, and reacts to its status.
        private void RunPlan()
        {
            if (_plan == null) return;

            if (_current == null)
            {
                if (_planIndex >= _plan.Count)
                {
                    // Patrol loops forever; other completed plans just idle until the goal changes.
                    if (_activeGoal.name == "Patrol") _planIndex = 0;
                    else return;
                }

                _current = _plan[_planIndex];
                _current.Enter(this);
            }

            switch (_current.Tick(this))
            {
                case Status.Running:
                    break;
                case Status.Done:
                    // Step finished — move on to the next action.
                    _planIndex++;
                    _current = null;
                    break;
                case Status.Failed:
                    // Action can't proceed (e.g. lost the player) — drop the plan and replan next frame.
                    _plan = null;
                    _current = null;
                    _activeGoal = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Sends the guard to the current patrol waypoint.
        public void GoToWaypoint()
        {
            if (_waypoints is { Length: > 0 }) Movement.SetDestination(_waypoints[_wp]);
        }

        // Advances to the next patrol waypoint (wrapping) and heads there.
        public void NextWaypoint()
        {
            if (_waypoints is { Length: > 0 }) _wp = (_wp + 1) % _waypoints.Length;
            GoToWaypoint();
        }

        // Called by vision when the player comes into view.
        public void OnPlayerSighted(Transform player)
        {
            Player = player;
            SeesPlayer = true;
            HasLastSeen = false; // sighting supersedes the stale last-seen point
            IsChasing = true;
        }

        // Called by vision when the player is lost; remembers where they were for investigation.
        public void OnPlayerLost()
        {
            SeesPlayer = false;
            IsChasing = false;
            if (!Player) return;
            LastSeen = Player.position;
            HasLastSeen = true;
        }

        // Called by vision when a distraction is noticed — but only if nothing more important is going on.
        public void OnDistractionSeen(Distraction d)
        {
            if (SeesPlayer || HasLastSeen || HasDistraction) return;
            Distraction = d;
            DistractionPos = d.transform.position;
            HasDistraction = true;
        }

        // Clears the last-seen point once it's been investigated.
        public void ClearLastSeen() => HasLastSeen = false;

        // Consumes the current distraction, destroying its object.
        public void ClearDistraction()
        {
            if (Distraction) Destroy(Distraction.gameObject);
            Distraction = null;
            HasDistraction = false;
        }
    }
}