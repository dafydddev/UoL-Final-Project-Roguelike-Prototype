using UnityEngine;

namespace Navigation
{
    // Editor gizmo helper: visualises the navigation grid and a test path between two
    // transforms when this object is selected. Purely a debugging aid — draws nothing at runtime.
    [RequireComponent(typeof(FacilityNavigation))]
    public class NavigationDebugDrawer : MonoBehaviour
    {
        public Transform from; // start point of the test path
        public Transform to; // end point of the test path
        public bool drawWalkable = true; // whether to draw a marker on every walkable cell

        private FacilityNavigation _nav;

        private void Awake() => _nav = GetComponent<FacilityNavigation>();

        // Called by the editor to draw gizmos while this object is selected.
        private void OnDrawGizmosSelected()
        {
            // Awake doesn't run in edit mode, so grab the reference lazily here too.
            if (_nav == null)
            {
                _nav = GetComponent<FacilityNavigation>();
            }

            // Nothing to draw until the grid has been built.
            if (!_nav.Ready || _nav.Grid == null)
            {
                return;
            }

            // Draw a small cube on each walkable cell.
            if (drawWalkable)
            {
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
                var grid = _nav.Grid;
                for (var x = 0; x < grid.GetLength(0); x++)
                {
                    for (var y = 0; y < grid.GetLength(1); y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!_nav.IsWalkable(cell))
                        {
                            continue;
                        }

                        Gizmos.DrawCube(_nav.CellToWorld(cell), Vector3.one * 0.3f);
                    }
                }
            }

            // The path preview needs both endpoints assigned.
            if (from == null || to == null) return;

            // Ask the pathfinder for a route; bail if there isn't one.
            var path = _nav.FindWorldPath(from.position, to.position);

            if (path == null) return;

            // Draw the path as yellow line segments between consecutive waypoints.
            Gizmos.color = Color.yellow;
            for (var i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }

            // Mark each waypoint with a green sphere.
            Gizmos.color = Color.green;
            foreach (var p in path)
            {
                Gizmos.DrawSphere(p, 0.12f);
            }
        }
    }
}