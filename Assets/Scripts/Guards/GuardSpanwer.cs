using System.Collections.Generic;
using System.Linq;
using Generation;
using Guards.GOAP;
using Navigation;
using Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Guards
{
    // Spawns a guard at every guard-post room, giving each its patrol waypoints,
    // and a reference to the shared navigation so its GOAP agent can pathfind.
    public class GuardSpawner : MonoBehaviour
    {
        public GameObject guardPrefab;

        // Places a guard in each GuardPost room and initialises its agent.
        public void Spawn(
            RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap, FacilityNavigation nav)
        {
            foreach (var room in graph.rooms.Where(r => r.role == RoomRole.GuardPost))
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;

                // Spawn the guard in the room centre.
                var worldPos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(guardPrefab, worldPos, Quaternion.identity, transform);
                go.name = $"Guard_{room.id}";

                // Work out this guard's patrol route from the room layout.
                var waypoints = WaypointDeriver.Derive(room.id, graph, rects, tilemap);

                // Ensure it has a GOAP agent and hand it navigation + waypoints.
                var agent = go.GetComponent<GuardAgent>() ?? go.AddComponent<GuardAgent>();
                agent.Init(nav, waypoints);
            }
        }
    }
}