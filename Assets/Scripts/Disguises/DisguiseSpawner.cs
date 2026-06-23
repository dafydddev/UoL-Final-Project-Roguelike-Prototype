using System.Collections.Generic;
using System.Linq;
using Generation;
using Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Disguises
{
    // Spawns a disguise pickup in some of the secondary objective rooms, by chance.
    // Placement is seeded from the graph so a given level always spawns the same disguises.
    public class DisguiseSpawner : MonoBehaviour
    {
        public GameObject disguisePrefab;

        [Range(0f, 1f)]
        [Tooltip("Chance that a given secondary objective room contains a disguise.")]
        public float spawnChance = 0.5f;

        // Rolls each secondary objective room and, on success, spawns a disguise at its centre.
        public void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            // Seed from the graph so the same level always spawns disguises the same way.
            var rng = new System.Random(graph.seed);

            // Secondary objective rooms are objective rooms that aren't the primary node.
            // (Primary uses missionNodeId "primary"; secondaries use "secondary_0", "secondary_1", ...)
            var rooms = graph.rooms.Where(r =>
                r.role == RoomRole.ObjectiveRoom &&
                r.missionNodeId != null &&
                r.missionNodeId.StartsWith("secondary"));

            foreach (var room in rooms)
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                // Only some secondary rooms get a disguise.
                if (rng.NextDouble() >= spawnChance) continue;

                // Spawn in the room centre.
                var worldPos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(disguisePrefab, worldPos, Quaternion.identity, transform);
                // Ensure it carries a Disguise component so the player can pick it up.
                if (go.GetComponent<Disguise>() == null) go.AddComponent<Disguise>();
                go.name = $"Disguise_{room.id}";
            }
        }
    }
}
