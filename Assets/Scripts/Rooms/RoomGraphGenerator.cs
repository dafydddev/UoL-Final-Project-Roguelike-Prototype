using System;
using System.Collections.Generic;
using System.Linq;
using Generation;

namespace Rooms
{
    // The gameplay purpose of a room, which drives colouring, spawning, and layout.
    public enum RoomRole
    {
        Entrance,
        ObjectiveRoom,
        KeycardRoom,
        GuardPost,
        Corridor,
        Exit
    }

    // A single room in the layout graph, linked back to the mission node it represents.
    [Serializable]
    public class RoomNode
    {
        public string id;
        public RoomRole role;
        public string missionNodeId; // mission node this room came from, if any
    }

    // A connection between two rooms, optionally locked behind a keycard room.
    [Serializable]
    public class RoomEdge
    {
        public string fromId;
        public string toId;
        public bool locked;
        public string keyRoomId; // the keycard room whose key opens this edge
    }

    // The full room layout: rooms, the edges connecting them, and the seed/difficulty used.
    [Serializable]
    public class RoomGraph
    {
        public List<RoomNode> rooms = new();
        public List<RoomEdge> edges = new();
        public int seed;
        public int difficulty;

        // Finds a room by id, or null if not present.
        public RoomNode GetRoom(string id) => rooms.Find(r => r.id == id);
    }

    // Expands an abstract mission into a concrete room graph: one room per mission node,
    // Plus an entrance, optional locked keycard rooms, guard posts, connecting corridors, and exits
    public static class RoomGraphGenerator
    {
        // How many exits the level has: 3 on easy, 2 on medium, 0 (primary-only) on hard.
        private static int ExitCount(int difficulty) => difficulty == 2 ? 0 : 3 - difficulty;

        // Builds the room graph for a mission at the given difficulty and lock probability.
        public static RoomGraph Generate(MissionGraph mission, int difficulty, float lockChance)
        {
            var graph = new RoomGraph { seed = mission.seed, difficulty = difficulty };
            var rng = new Random(mission.seed);
            var missionRoomMap = new Dictionary<string, RoomNode>(); // mission node id → its room

            // 1. Create one room per mission node.
            foreach (var node in mission.nodes)
            {
                var room = new RoomNode
                {
                    id = $"room_{node.id}",
                    role = MissionRoleToRoomRole(node.nodeType, node.text),
                    missionNodeId = node.id
                };
                graph.rooms.Add(room);
                missionRoomMap[node.id] = room;
            }

            // 2. Add the entrance and wire it to the mission's entry room.
            var entrance = new RoomNode { id = "room_entrance", role = RoomRole.Entrance };
            graph.rooms.Add(entrance);
            graph.edges.Add(new RoomEdge { fromId = entrance.id, toId = missionRoomMap["entry"].id });

            // 3. Turn mission dependencies into edges, locking some objective/keycard rooms.
            var usedKeys = new HashSet<string>();
            foreach (var node in mission.nodes)
            {
                if (!missionRoomMap.TryGetValue(node.id, out var toRoom)) continue;
                foreach (var depId in node.dependencies)
                {
                    if (!missionRoomMap.TryGetValue(depId, out var fromRoom)) continue;

                    // Only objective and keycard rooms are eligible to be locked.
                    var eligible = toRoom.role is RoomRole.ObjectiveRoom or RoomRole.KeycardRoom;

                    string keyRoomId = null;
                    if (eligible && rng.NextDouble() < lockChance)
                    {
                        // Reuse an existing keycard room earlier in the chain if one is free...
                        var found = FindKeycardRoomInChain(depId, mission, missionRoomMap);
                        if (found != null && usedKeys.Add(found))
                        {
                            keyRoomId = found;
                        }
                        else
                        {
                            // ...otherwise spawn a fresh keycard room off the source room.
                            var key = new RoomNode
                                { id = $"room_key_{graph.rooms.Count}", role = RoomRole.KeycardRoom };
                            graph.rooms.Add(key);
                            graph.edges.Add(new RoomEdge { fromId = fromRoom.id, toId = key.id });
                            usedKeys.Add(key.id);
                            keyRoomId = key.id;
                        }
                    }

                    // Connect the dependency to its dependent, locked if a key was assigned.
                    graph.edges.Add(new RoomEdge
                    {
                        fromId = fromRoom.id,
                        toId = toRoom.id,
                        locked = keyRoomId != null,
                        keyRoomId = keyRoomId
                    });
                }
            }

            // 4. Insert guard posts in front of objective rooms (always) and keycard rooms (50%).
            var guardCandidates = graph.rooms.FindAll(r => r.role is RoomRole.ObjectiveRoom or RoomRole.KeycardRoom);
            foreach (var candidate in guardCandidates)
            {
                if (!(candidate.role == RoomRole.ObjectiveRoom || rng.NextDouble() > 0.5)) continue;

                var guard = new RoomNode { id = $"room_guard_{graph.rooms.Count}", role = RoomRole.GuardPost };
                graph.rooms.Add(guard);

                // Splice the guard post onto the candidate's inbound edge, moving any lock to the new edge.
                var inbound = graph.edges.Find(e => e.toId == candidate.id);
                if (inbound != null)
                {
                    graph.edges.Add(new RoomEdge
                    {
                        fromId = guard.id, toId = candidate.id, locked = inbound.locked, keyRoomId = inbound.keyRoomId
                    });
                    inbound.toId = guard.id; // redirect the old edge into the guard post
                    inbound.locked = false;
                    inbound.keyRoomId = null;
                }
                else
                {
                    graph.edges.Add(new RoomEdge { fromId = guard.id, toId = candidate.id });
                }
            }

            // 5. Insert a corridor between most connected rooms (skip ones already corridor/entrance/exit).
            var snapshot = new List<RoomEdge>(graph.edges); // iterate a copy since we mutate the list
            foreach (var edge in snapshot)
            {
                var from = graph.GetRoom(edge.fromId);
                var to = graph.GetRoom(edge.toId);
                if (from == null || to == null) continue;
                if (from.role is RoomRole.Corridor or RoomRole.Entrance) continue;
                if (to.role is RoomRole.Corridor or RoomRole.Exit) continue;

                // Replace the direct edge with from -> corridor -> to, carrying the lock onto the second half.
                var corridor = new RoomNode { id = $"room_corridor_{graph.rooms.Count}", role = RoomRole.Corridor };
                graph.rooms.Add(corridor);
                var idx = graph.edges.IndexOf(edge);
                if (idx != -1) graph.edges.RemoveAt(idx);
                graph.edges.Add(new RoomEdge { fromId = edge.fromId, toId = corridor.id });
                graph.edges.Add(new RoomEdge
                    { fromId = corridor.id, toId = edge.toId, locked = edge.locked, keyRoomId = edge.keyRoomId });
            }

            // 6. Add exits. There's always one from the primary objective room.
            var exitCount = ExitCount(difficulty);

            var primaryRoom = graph.rooms.Find(r => r.role == RoomRole.ObjectiveRoom && r.missionNodeId == "primary");
            var primaryExit = new RoomNode { id = "room_exit_0", role = RoomRole.Exit };
            graph.rooms.Add(primaryExit);
            if (primaryRoom != null)
            {
                graph.edges.Add(new RoomEdge { fromId = primaryRoom.id, toId = primaryExit.id });
            }

            // Add extra exits on non-critical rooms, if difficulty allows more than one.
            if (exitCount > 1)
            {
                // Rooms on the critical path must not host an alternate exit.
                var criticalIds = new HashSet<string>(
                    mission.nodes
                        .Where(n => n.nodeType is NodeType.Entry or NodeType.Prerequisite or NodeType.Primary)
                        .Select(n => n.id)
                );

                // Eligible hosts: corridors / objective rooms that aren't critical or the primary.
                var exitCandidates = graph.rooms
                    .Where(r => r.role is RoomRole.Corridor or RoomRole.ObjectiveRoom)
                    .Where(r => r.missionNodeId == null || !criticalIds.Contains(r.missionNodeId))
                    .Where(r => r.id != primaryExit.id && r.id != primaryRoom?.id)
                    .ToList();

                // Fisher–Yates shuffle so the chosen exit rooms vary by seed.
                for (var i = exitCandidates.Count - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (exitCandidates[i], exitCandidates[j]) = (exitCandidates[j], exitCandidates[i]);
                }

                // Attach as many additional exits as we have room for.
                var additionalCount = Math.Min(exitCount - 1, exitCandidates.Count);
                for (var i = 0; i < additionalCount; i++)
                {
                    var exit = new RoomNode { id = $"room_exit_{i + 1}", role = RoomRole.Exit };
                    graph.rooms.Add(exit);
                    graph.edges.Add(new RoomEdge { fromId = exitCandidates[i].id, toId = exit.id });
                }
            }

            return graph;
        }

        // Maps a mission node's type (and text) to the room role it should become.
        // Prerequisites become keycard rooms if their text mentions card/code/badge, else guard posts.
        private static RoomRole MissionRoleToRoomRole(NodeType nodeType, string text)
        {
            return nodeType switch
            {
                NodeType.Entry => RoomRole.Entrance,
                NodeType.Primary => RoomRole.ObjectiveRoom,
                NodeType.Secondary => RoomRole.ObjectiveRoom,
                NodeType.Prerequisite => text.ToLower().Contains("card") || text.ToLower().Contains("code") ||
                                         text.ToLower().Contains("badge")
                    ? RoomRole.KeycardRoom
                    : RoomRole.GuardPost,
                _ => RoomRole.Corridor
            };
        }

        // Searches the dependency chain upstream of a mission node for an existing keycard room,
        // returning its room id, or null if none is found. (Depth-first over dependencies.)
        private static string FindKeycardRoomInChain(string fromMissionId, MissionGraph mission,
            Dictionary<string, RoomNode> map)
        {
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(fromMissionId);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!visited.Add(id)) continue; // skip already-seen nodes
                if (map.TryGetValue(id, out var room) && room.role == RoomRole.KeycardRoom) return room.id;
                var node = mission.nodes.Find(n => n.id == id);
                if (node == null) continue;
                foreach (var dep in node.dependencies) stack.Push(dep);
            }

            return null;
        }
    }
}