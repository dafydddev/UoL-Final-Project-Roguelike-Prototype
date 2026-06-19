using UnityEngine;

namespace Guards.GOAP
{
    // The outcome of ticking an action: still working, finished, or no longer possible.
    public enum Status
    {
        Running,
        Done,
        Failed
    }

    // Base class for a GOAP action. "pre" are the world facts required before it can run;
    // "post" are the facts it makes true (used by the planner). Enter runs once when the action
    // starts; Tick runs every frame until it returns Done or Failed.
    public abstract class GuardAction
    {
        public readonly State pre = new();
        public readonly State post = new();

        public virtual void Enter(GuardAgent g)
        {
            
        }

        public abstract Status Tick(GuardAgent g);
    }

    // Walk the patrol route. Fails (forcing a re-plan) the moment anything more interesting appears.
    public sealed class Patrol : GuardAction
    {
        public Patrol() => post.Set(Key.Patrolling, true);

        // Head to the current waypoint when patrol begins.
        public override void Enter(GuardAgent g) => g.GoToWaypoint();

        public override Status Tick(GuardAgent g)
        {
            // Abandon patrol if the guard now sees the player, has a lead, or spots a distraction.
            if (g.SeesPlayer || g.HasLastSeen || g.HasDistraction) return Status.Failed;
            // On reaching a waypoint, move on to the next.
            if (g.Movement.HasArrived) g.NextWaypoint();
            return Status.Running;
        }
    }

    // Chase the player directly until within reach.
    public sealed class MoveToPlayer : GuardAction
    {
        private const float Reach = 0.6f; // distance at which the player counts as caught

        public MoveToPlayer()
        {
            pre.Set(Key.SeesPlayer, true);
            post.Set(Key.AtPlayer, true);
        }

        // Start heading straight at the player.
        public override void Enter(GuardAgent g)
        {
            if (g.Player) g.Movement.SeekDirect(g.Player.position);
        }

        public override Status Tick(GuardAgent g)
        {
            // Lost sight of the player — this action can no longer run.
            if (!g.SeesPlayer || !g.Player) return Status.Failed;

            // Close enough: stop and report success.
            if ((g.Player.position - g.transform.position).sqrMagnitude <= Reach * Reach)
            {
                g.Movement.Stop();
                return Status.Done;
            }

            // Otherwise keep steering at the player's (possibly moving) position.
            g.Movement.SeekDirect(g.Player.position);
            return Status.Running;
        }
    }

    // Go to where the player was last seen, then look around for a moment before giving up.
    public sealed class Investigate : GuardAction
    {
        private const float Reach = 0.4f; // arrival distance to the last-seen point
        private const float LookDuration = 1.5f; // how long to pause and look once there
        private float _timer;
        private bool _arrived;

        public Investigate()
        {
            pre.Set(Key.HasLastSeen, true);
            post.Set(Key.AtLastSeen, true);
            post.Set(Key.HasLastSeen, false);
        }

        // Reset state and path to the last-seen position.
        public override void Enter(GuardAgent g)
        {
            _arrived = false;
            _timer = 0f;
            g.Movement.SetDestination(g.LastSeen);
        }

        public override Status Tick(GuardAgent g)
        {
            // Spotting the player again takes over (Chase).
            if (g.SeesPlayer) return Status.Failed;

            // Phase 1: travel to the last-seen point.
            if (!_arrived)
            {
                if ((g.LastSeen - g.transform.position).sqrMagnitude <= Reach * Reach || g.Movement.HasArrived)
                {
                    _arrived = true;
                    g.Movement.Stop();
                }

                return Status.Running;
            }

            // Phase 2: wait and look, then clear the lead and finish.
            _timer += Time.deltaTime;
            if (_timer < LookDuration) return Status.Running;
            g.ClearLastSeen();
            return Status.Done;
        }
    }

    // Walk over to a noticed distraction, then consume it.
    public sealed class InvestigateDistraction : GuardAction
    {
        private const float Reach = 0.5f;

        public InvestigateDistraction()
        {
            pre.Set(Key.HasDistraction, true);
            post.Set(Key.AtDistraction, true);
            post.Set(Key.HasDistraction, false);
        }

        // Path to the distraction's position.
        public override void Enter(GuardAgent g) => g.Movement.SetDestination(g.DistractionPos);

        public override Status Tick(GuardAgent g)
        {
            // Higher-priority leads (seeing the player, a fresh last-seen) interrupt this.
            if (g.SeesPlayer || g.HasLastSeen) return Status.Failed;

            // On arrival, stop and remove the distraction.
            if ((g.DistractionPos - g.transform.position).sqrMagnitude <= Reach * Reach || g.Movement.HasArrived)
            {
                g.Movement.Stop();
                g.ClearDistraction();
                return Status.Done;
            }

            return Status.Running;
        }
    }

    // Terminal action once the player is caugh

    public sealed class Apprehend : GuardAction
    {
        private const float HoldDuration = 2f;
        private float _timer;

        public Apprehend()
        {
            pre.Set(Key.AtPlayer, true);
            post.Set(Key.CaughtPlayer, true);
        }

        public override void Enter(GuardAgent g)
        {
            _timer = 0f;
            g.Movement.Stop();
            g.CatchPlayer();
        }

        public override Status Tick(GuardAgent g)
        {
            _timer += Time.deltaTime;
            return _timer >= HoldDuration ? Status.Failed : Status.Running;
        }
    }
}