using System;

namespace Guards.GOAP
{
    // A candidate goal for the guard: the world state it wants to bring about,
    // how important it is, and a test for whether it's currently worth pursuing.
    // The agent picks the highest-priority relevant goal each tick.
    public sealed class Goal
    {
        public readonly string name;
        public readonly int priority; // higher wins when several goals are relevant
        public readonly State desired = new(); // the world state this goal aims to satisfy
        public readonly Func<GuardAgent, bool> relevant; // returns true when this goal applies right now

        public Goal(string name, int priority, Func<GuardAgent, bool> relevant)
        {
            this.name = name;
            this.priority = priority;
            this.relevant = relevant;
        }
    }
}