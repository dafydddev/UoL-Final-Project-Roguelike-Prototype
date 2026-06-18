using System.Collections.Generic;
using System.Linq;
using Generation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Navigation
{
    // Owns the generated grid and the pathfinder, and translates between tile-space
    // (what A* works in) and world-space (what agents move in). The Tilemap is the
    // single source of truth for that conversion, matching every spawner in the project.
    public class FacilityNavigation : MonoBehaviour
    {
        public Tilemap tilemap;

        private AStarPathfinder _pathfinder;

        public bool Ready => _pathfinder != null;

        // Called once the layout exists. FacilityOrchestrator already has the grid.
        public void Build(TileType[,] grid)
        {
            Grid = grid;
            _pathfinder = new AStarPathfinder(grid);
        }

        private Vector2Int WorldToCell(Vector3 world)
        {
            var c = tilemap.WorldToCell(world);
            return new Vector2Int(c.x, c.y);
        }

        public Vector3 CellToWorld(Vector2Int cell) =>
            tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

        // World-space path, ready to feed to an agent. Null if unreachable.
        public List<Vector3> FindWorldPath(Vector3 from, Vector3 to)
        {
            var cells = _pathfinder?.FindPath(WorldToCell(from), WorldToCell(to));
            if (cells == null) return null;

            var path = new List<Vector3>(cells.Count);
            path.AddRange(cells.Select(CellToWorld));
            return path;
        }

        // Exposed for the gizmo drawer and for snapping spawn points onto floor.
        public bool IsWalkable(Vector2Int cell) => _pathfinder != null && _pathfinder.IsWalkable(cell);
        public TileType[,] Grid { get; private set; }
    }
}