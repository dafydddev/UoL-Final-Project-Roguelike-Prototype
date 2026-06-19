using System.Collections.Generic;
using System.Text;

namespace Guards.GOAP
{
    // The GOAP planner. Finds a sequence of actions that takes the guard from its current
    // state to one satisfying the goal, via breadth-first search over action effects.
    public static class Planner
    {
        // Returns the shortest action sequence reaching goal from start, [] if already satisfied, or null.
        public static List<GuardAction> Plan(State start, State goal, IReadOnlyList<GuardAction> actions)
        {
            // Already at the goal — no actions needed.
            if (start.Matches(goal)) return new List<GuardAction>();

            // BFS frontier of (simulated state, actions taken to get there).
            var frontier = new Queue<(State state, List<GuardAction> path)>();
            frontier.Enqueue((start, new List<GuardAction>()));
            var seen = new HashSet<string> { Hash(start) }; // visited states to avoid revisiting

            while (frontier.Count > 0)
            {
                var (state, path) = frontier.Dequeue();
                foreach (var a in actions)
                {
                    // Only consider actions whose preconditions the current state meets.
                    if (!state.Matches(a.pre)) continue;

                    // Simulate applying the action's effects.
                    var next = state.Clone();
                    next.Apply(a.post);
                    if (!seen.Add(Hash(next))) continue; // skip states we've already explored

                    // Extend the plan; return it if this reaches the goal.
                    var nextPath = new List<GuardAction>(path) { a };
                    if (next.Matches(goal)) return nextPath;
                    frontier.Enqueue((next, nextPath));
                }
            }
            return null; // no plan found
        }

        // Builds a stable string key for a state (facts sorted by key)
        // so equivalent states hash identically regardless of insertion order.
        private static string Hash(State s)
        {
            var keys = new SortedDictionary<Key, bool>();
            foreach (var kv in s.Facts) keys[kv.Key] = kv.Value;
            var sb = new StringBuilder();
            foreach (var kv in keys) sb.Append((int)kv.Key).Append(kv.Value ? '1' : '0');
            return sb.ToString();
        }
    }
}