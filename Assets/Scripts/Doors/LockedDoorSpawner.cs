using System.Collections.Generic;
using System.Linq;
using Generation;
using Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Doors
{
    // Spawns a locked-door prefab on each locked edge of the room graph, tagged with the key required to open it.
    public class LockedDoorSpawner : MonoBehaviour
    {
        public GameObject lockedDoorPrefab;

        // Places a locked door between the two rooms of every locked edge in the graph.
        public void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            foreach (var edge in graph.edges.Where(e => e.locked))
            {
                // Skip edges whose endpoints we have no rectangle for.
                if (!rects.TryGetValue(edge.fromId, out var a)) continue;
                if (!rects.TryGetValue(edge.toId, out var b)) continue;

                // Work out the boundary cell between the two rooms and convert it to world space.
                var cell = DoorCell(a, b);
                var worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(lockedDoorPrefab, worldPos, Quaternion.identity, transform);

                // Ensure the door has a LockedDoor component and stamp it with the required key.
                var door = go.GetComponent<LockedDoor>() ?? go.AddComponent<LockedDoor>();
                door.keyId = edge.keyRoomId;
                go.name = $"LockedDoor_{edge.keyRoomId}";
            }
        }

        // Picks the cell where the door sits, based on which side room b lies relative to room a.
        private static Vector2Int DoorCell(RoomRect a, RoomRect b)
        {
            if (b.x > a.x) return new Vector2Int(b.x, a.CenterY); // b east of a
            if (b.x < a.x) return new Vector2Int(a.x, a.CenterY); // b west of a
            if (b.y > a.y) return new Vector2Int(a.CenterX, b.y); // b at +y of a
            return new Vector2Int(a.CenterX, a.y); // b at -y of a
        }
    }
}