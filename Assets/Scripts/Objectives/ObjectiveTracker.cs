using System;
using System.Collections.Generic;
using System.Linq;
using Generation;
using Rooms;
using UnityEngine;

namespace Objectives
{
    // Runtime state of a single objective: its identity and whether it's been completed.
    public class ObjectiveState
    {
        public string id;
        public string name;
        public bool isPrimary;
        public bool complete;
    }

    // Tracks the level's objectives and their completion, raising Changed whenever they update (e.g. for the HUD).
    public class ObjectiveTracker : MonoBehaviour
    {
        // Raised whenever the objective list or any completion state changes.
        public event Action Changed;

        private readonly List<ObjectiveState> _objectives = new();
        public IReadOnlyList<ObjectiveState> Objectives => _objectives;

        // Builds the objective list from the objective rooms in the graph,
        // pulling display text from the matching mission nodes. Called when a level is generated.
        public void Init(RoomGraph graph, MissionGraph mission)
        {
            _objectives.Clear();
            foreach (var room in graph.rooms.Where(r => r.role == RoomRole.ObjectiveRoom))
            {
                var node = mission.nodes.Find(n => n.id == room.missionNodeId);
                _objectives.Add(new ObjectiveState
                {
                    id = room.id,
                    name = node?.text ?? "Objective",
                    isPrimary = room.missionNodeId == "primary",
                    complete = false
                });
            }

            Changed?.Invoke();
        }

        // Marks an objective complete (no-op if unknown or already done) and notifies listeners.
        public void Complete(string id)
        {
            var o = _objectives.Find(e => e.id == id);
            if (o == null || o.complete) return;
            o.complete = true;
            Changed?.Invoke();
        }

        // Whether a specific objective is complete.
        public bool IsComplete(string id) => _objectives.Find(e => e.id == id)?.complete ?? false;

        // True, once every primary objective is done (and at least one exists) — gates the exit.
        public bool PrimaryComplete =>
            _objectives.Any(e => e.isPrimary) && _objectives.Where(e => e.isPrimary).All(e => e.complete);

        // True, when every objective, primary and secondary, is complete.
        public bool AllComplete => _objectives.Count > 0 && _objectives.All(e => e.complete);

        // Progress counters, handy for HUD display.
        public int CompletedCount => _objectives.Count(e => e.complete);
        public int Total => _objectives.Count;
    }
}