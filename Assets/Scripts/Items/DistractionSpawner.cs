using System.Collections.Generic;
using System.Linq;
using Generation;
using Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Items
{
    // Scatters a fixed number of throwable items across the facility, in corridors and guard posts,
    // choosing rooms randomly (seeded) so placement is repeatable per level.
    public class DistractionSpawner : MonoBehaviour
    {
        public GameObject throwablePrefab;
        [Tooltip("How many throwables to scatter across the facility.")]
        public int count = 4;

        // Places up to count throwables in randomly chosen eligible rooms.
        public void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            // Seed from the graph so the same level always scatters items the same way.
            var rng = new System.Random(graph.seed);

            // Eligible rooms: corridors and guard posts that have a rectangle.
            var candidates = graph.rooms
                .Where(r => r.role is RoomRole.Corridor or RoomRole.GuardPost)
                .Where(r => rects.ContainsKey(r.id))
                .ToList();

            // Fisher yates shuffle so the chosen rooms vary.
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            // Spawn one throwable per room, up to the requested count (or however many rooms exist).
            var n = Mathf.Min(count, candidates.Count);
            for (var i = 0; i < n; i++)
            {
                var rect = rects[candidates[i].id];
                var pos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(throwablePrefab, pos, Quaternion.identity, transform);
                go.name = $"Throwable_{candidates[i].id}";
            }
        }
    }
}