using System.Collections.Generic;
using System.Linq;

namespace Guards.Goap
{
    // The world-state facts the GOAP planner reasons about (all boolean).
    public enum Key
    {
        SeesPlayer,
        AtPlayer,
        Patrolling,
        HasLastSeen,
        AtLastSeen,
        HasDistraction,
        AtDistraction
    }

    // A set of boolean facts. Used both for the guard's current world state and for the
    // preconditions/effects/goals the planner compares against. Unset keys read as false.
    public class State
    {
        private readonly Dictionary<Key, bool> _f = new();

        // Reads a fact, defaulting to false if it was never set.
        public bool Get(Key k) => _f.TryGetValue(k, out var v) && v;

        // Sets a fact's value.
        public void Set(Key k, bool v) => _f[k] = v;

        // Returns an independent copy, so the planner can simulate changes without mutating the original.
        public State Clone() { var s = new State(); foreach (var kv in _f) s._f[kv.Key] = kv.Value; return s; }

        // All facts in this state.
        public IEnumerable<KeyValuePair<Key, bool>> Facts => _f;

        // True if this state satisfies every fact required by the given goal state.
        public bool Matches(State goal)
        {
            return goal._f.All(kv => Get(kv.Key) == kv.Value);
        }

        // Overlays another state's facts onto this one (used to apply an action's effects).
        public void Apply(State effects) { foreach (var kv in effects._f) _f[kv.Key] = kv.Value; }
    }
}