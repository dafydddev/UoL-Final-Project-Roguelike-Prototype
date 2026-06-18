using System.Collections.Generic;
using System.Linq;
using Generation;
using Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Guards
{
    // Derives a guard's patrol loop from the room-connectivity graph. A guard posted at
    // a room patrols that room plus its graph-adjacent rooms, so patrols stay local and
    // follow the facility's actual connectivity rather than arbitrary points.
    public static class WaypointDeriver
    {
        // Returns room-centre world positions forming a patrol loop around 'homeRoomId'.
        // Adjacency is undirected here: a guard walks edges in either direction.
        public static Vector3[] Derive(
            string homeRoomId, RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            var loop = new List<string> { homeRoomId };

            foreach (var neighbour in Neighbours(homeRoomId, graph))
            {
                // Patrols don't wander into exits or through entrance rooms.
                var room = graph.GetRoom(neighbour);
                if (room == null) continue;
                if (room.role is RoomRole.Exit or RoomRole.Entrance) continue;
                loop.Add(neighbour);
            }

            return loop
                .Where(rects.ContainsKey)
                .Select(id =>
                {
                    var r = rects[id];
                    return tilemap.GetCellCenterWorld(new Vector3Int(r.CenterX, r.CenterY, 0));
                })
                .ToArray();
        }

        // Undirected neighbours: rooms reachable across one edge in either direction.
        private static IEnumerable<string> Neighbours(string id, RoomGraph graph)
        {
            foreach (var e in graph.edges)
            {
                if (e.fromId == id) yield return e.toId;
                else if (e.toId == id) yield return e.fromId;
            }
        }
    }
}