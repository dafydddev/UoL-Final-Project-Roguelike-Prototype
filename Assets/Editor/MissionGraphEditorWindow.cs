#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Generation;
using Rooms;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Editor
{
    // Editor tool (Tools -> Mission Graph Editor) for previewing generation without entering play mode.
    // Generates a mission + room graph from the chosen settings and draws both as interactive diagrams.
    public class MissionGraphEditorWindow : EditorWindow
    {
        // Generation settings, mirroring the runtime generators.
        private int _complexity = 1;
        private MissionType _forcedType = MissionType.Assassination;
        private bool _randomType = true;
        private int _seed = 42;
        private bool _randomSeed = true;
        private int _difficulty = 1;
        private float _lockChance = 0.5f;

        // The most recently generated graphs.
        private MissionGraph _missionGraph;
        private RoomGraph _roomGraph;

        // Which graph the window is currently showing.
        private enum Tab
        {
            Mission,
            Room
        }

        private Tab _tab = Tab.Mission;
        private Vector2 _scroll; // scroll position of the graph canvas
        private string _selectedNodeId; // id of the node shown in the inspector, or null

        // Computed screen positions for each node, per graph.
        private readonly Dictionary<string, Vector2> _missionPositions = new();
        private readonly Dictionary<string, Vector2> _roomPositions = new();

        // Formatting Styles

        private const int FontSize = 13;
        private const int FontSizeSmall = 10;

        // Node and layout dimensions.
        private const float NodeW = 140f;
        private const float NodeH = 50f;
        private const float LevelSpacingX = 180f; // horizontal gap between dependency levels
        private const float LevelSpacingY = 70f; // vertical gap between nodes on the same level
        private const float CanvasOffsetX = 20f;
        private const float CanvasOffsetY = 20f;

        // Node/edge colours by type.
        private static readonly Color ColEntry = new(0.29f, 0.62f, 1.00f);
        private static readonly Color ColPrereq = new(0.65f, 0.55f, 0.98f);
        private static readonly Color ColPrimary = new(0.97f, 0.44f, 0.44f);
        private static readonly Color ColSecondary = new(0.20f, 0.83f, 0.60f);
        private static readonly Color ColEntrance = new(0.29f, 0.62f, 1.00f);
        private static readonly Color ColExit = new(0.97f, 0.44f, 0.44f);
        private static readonly Color ColObjective = new(0.97f, 0.44f, 0.44f);
        private static readonly Color ColKeycard = new(0.98f, 0.75f, 0.26f);
        private static readonly Color ColGuardPost = new(0.98f, 0.60f, 0.20f);
        private static readonly Color ColCorridor = new(0.42f, 0.45f, 0.50f);
        private static readonly Color ColEdge = new(0.28f, 0.35f, 0.45f);

        private static readonly Color
            ColEdgeLocked = new(0.98f, 0.75f, 0.26f); // locked edges drawn dashed in this colour

        private static readonly Color ColBackground = new(0.08f, 0.09f, 0.11f);

        // Registers the window under the Tools menu.
        [MenuItem("Tools/Mission Graph Editor")]
        public static void Open() => GetWindow<MissionGraphEditorWindow>("Mission Graph Editor");

        // Main draw loop: settings on top, then (once generated) metadata, tabs, graph, and inspector.
        private void OnGUI()
        {
            DrawSettings();
            if (_missionGraph == null)
            {
                EditorGUILayout.HelpBox("Configure settings and press Generate.", MessageType.Info);
                return;
            }

            DrawMeta();
            DrawTabs();
            DrawGraph();
            DrawInspector();
        }

        // Draws the settings panel and, on Generate, builds both graphs and lays them out.
        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            _complexity = EditorGUILayout.IntSlider("Complexity", _complexity, 0, 2);
            _difficulty = EditorGUILayout.IntSlider("Difficulty", _difficulty, 0, 2);
            _lockChance = EditorGUILayout.Slider("Lock Chance", _lockChance, 0f, 1f);
            _randomType = EditorGUILayout.Toggle("Random Type", _randomType);
            if (!_randomType)
            {
                _forcedType = (MissionType)EditorGUILayout.EnumPopup("Mission Type", _forcedType);
            }

            _randomSeed = EditorGUILayout.Toggle("Random Seed", _randomSeed);
            if (!_randomSeed)
            {
                _seed = EditorGUILayout.IntField("Seed", _seed);
            }

            if (GUILayout.Button("Generate"))
            {
                // Run the (editor copy of the) mission generator, then expand into a room graph.
                var gen = new MissionGeneratorRuntime
                {
                    complexity = _complexity,
                    forcedType = _forcedType,
                    randomType = _randomType,
                    seed = _seed,
                    randomSeed = _randomSeed
                };

                _missionGraph = gen.Generate();
                _roomGraph = RoomGraphGenerator.Generate(_missionGraph, _difficulty, _lockChance);
                _seed = _missionGraph.seed; // reflect the actually-used seed back into the field

                // Compute node positions for both diagrams.
                LayoutGraph(
                    _missionGraph.nodes.Select(n => n.id),
                    _missionGraph.nodes.SelectMany(n => n.dependencies.Select(d => (d, n.id))),
                    _missionPositions);

                LayoutGraph(
                    _roomGraph.rooms.Select(r => r.id),
                    _roomGraph.edges.Select(e => (e.fromId, e.toId)),
                    _roomPositions);

                _selectedNodeId = null;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        // Draws a one-line summary bar of the generated mission/room graph.
        private void DrawMeta()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Facility: {_missionGraph.facility}", GUILayout.Width(220));
            EditorGUILayout.LabelField($"Type: {_missionGraph.type}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Seed: {_missionGraph.seed}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Nodes: {_missionGraph.nodes.Count}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Rooms: {_roomGraph.rooms.Count}");
            EditorGUILayout.LabelField($"Exits: {_roomGraph.rooms.Count(r => r.role == RoomRole.Exit)}");
            EditorGUILayout.EndHorizontal();
        }

        // Mission/Room toggle buttons.
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tab == Tab.Mission, "Mission Graph", EditorStyles.toolbarButton)) _tab = Tab.Mission;
            if (GUILayout.Toggle(_tab == Tab.Room, "Room Graph", EditorStyles.toolbarButton)) _tab = Tab.Room;
            EditorGUILayout.EndHorizontal();
        }

        // Draws the scrollable graph canvas for whichever tab is active.
        private void DrawGraph()
        {
            var canvasRect = GUILayoutUtility.GetRect(position.width, position.height - 220f);
            EditorGUI.DrawRect(canvasRect, ColBackground);

            _scroll = GUI.BeginScrollView(canvasRect, _scroll, ComputeContentRect());

            if (_tab == Tab.Mission) DrawMissionGraph();
            else DrawRoomGraph();

            GUI.EndScrollView();
        }

        // Draws the mission graph: dependency arrows first, then coloured nodes on top.
        private void DrawMissionGraph()
        {
            foreach (var node in _missionGraph.nodes)
            {
                if (!_missionPositions.TryGetValue(node.id, out var toPos)) continue;
                foreach (var dep in node.dependencies)
                {
                    if (!_missionPositions.TryGetValue(dep, out var fromPos)) continue;
                    DrawArrow(fromPos, toPos, ColEdge, false);
                }
            }

            foreach (var node in _missionGraph.nodes)
            {
                if (!_missionPositions.TryGetValue(node.id, out var pos)) continue;
                var col = node.nodeType switch
                {
                    NodeType.Entry => ColEntry,
                    NodeType.Prerequisite => ColPrereq,
                    NodeType.Primary => ColPrimary,
                    NodeType.Secondary => ColSecondary,
                    _ => Color.grey
                };
                DrawNode(pos, node.id, node.text, node.label, col);
            }
        }

        // Draws the room graph: connection arrows (dashed if locked), then coloured room nodes.
        private void DrawRoomGraph()
        {
            foreach (var edge in _roomGraph.edges)
            {
                if (!_roomPositions.TryGetValue(edge.fromId, out var fromPos)) continue;
                if (!_roomPositions.TryGetValue(edge.toId, out var toPos)) continue;
                DrawArrow(fromPos, toPos, edge.locked ? ColEdgeLocked : ColEdge, edge.locked);
            }

            foreach (var room in _roomGraph.rooms)
            {
                if (!_roomPositions.TryGetValue(room.id, out var pos)) continue;
                var col = room.role switch
                {
                    RoomRole.Entrance => ColEntrance,
                    RoomRole.Exit => ColExit,
                    RoomRole.ObjectiveRoom => ColObjective,
                    RoomRole.KeycardRoom => ColKeycard,
                    RoomRole.GuardPost => ColGuardPost,
                    RoomRole.Corridor => ColCorridor,
                    _ => Color.grey
                };
                // Show the underlying mission text where there is one, else just the role.
                var label = room.missionNodeId != null
                    ? _missionGraph.nodes.Find(n => n.id == room.missionNodeId)?.text ?? room.role.ToString()
                    : room.role.ToString();
                DrawNode(pos, room.id, label, room.role.ToString(), col);
            }
        }

        // Draws details for the currently selected mission node or room.
        private void DrawInspector()
        {
            if (_selectedNodeId == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);

            if (_tab == Tab.Mission)
            {
                var node = _missionGraph.nodes.Find(n => n.id == _selectedNodeId);
                if (node != null)
                {
                    EditorGUILayout.LabelField("Text", node.text);
                    EditorGUILayout.LabelField("Label", node.label);
                    EditorGUILayout.LabelField("Type", node.nodeType.ToString());
                    EditorGUILayout.LabelField("ID", node.id);
                    if (node.dependencies.Count > 0)
                    {
                        EditorGUILayout.LabelField("Dependencies", string.Join(", ", node.dependencies));
                    }
                }
            }
            else
            {
                var room = _roomGraph.rooms.Find(r => r.id == _selectedNodeId);
                if (room != null)
                {
                    EditorGUILayout.LabelField("Role", room.role.ToString());
                    EditorGUILayout.LabelField("ID", room.id);
                    EditorGUILayout.LabelField("Mission Node ID", room.missionNodeId ?? "—");
                    // List any locked edges leading into this room.
                    var locked = _roomGraph.edges.FindAll(e => e.toId == room.id && e.locked);
                    foreach (var e in locked)
                    {
                        EditorGUILayout.LabelField("Locked from", e.fromId);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // Assigns each node an (x, y) position using a layered layout:
        // a topological pass puts each node at a "level" = its longest dependency depth, then nodes are stacked per level.
        private static void LayoutGraph(
            IEnumerable<string> ids,
            IEnumerable<(string from, string to)> edges,
            Dictionary<string, Vector2> positions)
        {
            positions.Clear();
            var idList = ids.ToList();
            var levels = new Dictionary<string, int>();
            var inDegree = new Dictionary<string, int>();
            var adj = new Dictionary<string, List<string>>();

            // Initialise adjacency and in-degree for every node.
            foreach (var id in idList)
            {
                inDegree[id] = 0;
                adj[id] = new List<string>();
            }

            // Build the edge lists and in-degree counts.
            foreach (var (from, to) in edges)
            {
                if (!adj.ContainsKey(from)) adj[from] = new List<string>();
                inDegree.TryAdd(to, 0);
                adj[from].Add(to);
                inDegree[to]++;
            }

            // Recording each node's longest-path level.
            var queue = new Queue<string>();
            foreach (var id in idList)
                if (inDegree.TryGetValue(id, out var deg) && deg == 0)
                {
                    queue.Enqueue(id);
                    levels[id] = 0;
                }

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!adj.TryGetValue(id, out var neighbours)) continue;
                foreach (var nb in neighbours)
                {
                    var lvl = (levels.GetValueOrDefault(id, 0)) + 1;
                    if (!levels.TryGetValue(nb, out var cur) || lvl > cur) levels[nb] = lvl; // keep the deepest
                    if (--inDegree[nb] == 0) queue.Enqueue(nb);
                }
            }

            // Any node not reached (e.g. in a cycle) defaults to level 0.
            foreach (var id in idList) levels.TryAdd(id, 0);

            // Group nodes by level.
            var byLevel = new Dictionary<int, List<string>>();
            foreach (var id in idList)
            {
                var l = levels[id];
                if (!byLevel.ContainsKey(l)) byLevel[l] = new List<string>();
                byLevel[l].Add(id);
            }

            // Place each level in its own column, stacking its nodes vertically.
            foreach (var (level, group) in byLevel)
            {
                var x = CanvasOffsetX + level * LevelSpacingX + NodeW * 0.5f;
                for (var i = 0; i < group.Count; i++)
                    positions[group[i]] = new Vector2(x, CanvasOffsetY + i * LevelSpacingY + NodeH * 0.5f);
            }
        }

        // Computes the scroll-view content size needed to fit all nodes of the active tab.
        private Rect ComputeContentRect()
        {
            var positions = _tab == Tab.Mission ? _missionPositions : _roomPositions;
            var maxX = 0f;
            var maxY = 0f;
            foreach (var p in positions.Values)
            {
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
            }

            return new Rect(0, 0, maxX + NodeW, maxY + NodeH);
        }

        // Draws a single node box (title + subtitle) and handles click-to-select/deselect.
        private void DrawNode(Vector2 pos, string id, string text, string subtitle, Color col)
        {
            var rect = new Rect(pos.x - NodeW * 0.5f, pos.y - NodeH * 0.5f, NodeW, NodeH);
            var isSelected = _selectedNodeId == id;
            var bg = col * 0.25f; // darkened fill
            bg.a = 1f;

            EditorGUI.DrawRect(rect, bg);
            DrawBorder(rect, isSelected ? Color.white : col, isSelected ? 2f : 1f); // highlight when selected

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperCenter, fontSize = FontSize, wordWrap = true,
                normal = { textColor = Color.white }
            };
            var subStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.LowerCenter, fontSize = FontSizeSmall,
                normal = { textColor = new Color(0.6f, 0.6f, 0.7f) }
            };

            GUI.Label(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height * 0.6f), text, labelStyle);
            GUI.Label(new Rect(rect.x + 4, rect.y + rect.height * 0.55f, rect.width - 8, rect.height * 0.4f), subtitle,
                subStyle);

            // Toggle selection if this node was clicked.
            if (Event.current.type != EventType.MouseDown || !rect.Contains(Event.current.mousePosition)) return;
            _selectedNodeId = isSelected ? null : id;
            Event.current.Use();
            Repaint();
        }

        // Finds where a ray from a node's centre exits its box, so arrows touch the edge not the middle.
        private static Vector2 RectEdgeIntersect(Vector2 centre, Vector2 dir, float w, float h)
        {
            var hw = w * 0.5f;
            var hh = h * 0.5f;
            var tx = dir.x != 0 ? hw / Mathf.Abs(dir.x) : float.MaxValue;
            var ty = dir.y != 0 ? hh / Mathf.Abs(dir.y) : float.MaxValue;
            return centre + dir * Mathf.Min(tx, ty);
        }

        // Draws an arrow between two node boxes, optionally dashed (for locked edges), with a head.
        private static void DrawArrow(Vector2 from, Vector2 to, Color col, bool dashed)
        {
            var dir = (to - from).normalized;
            var start = RectEdgeIntersect(from, dir, NodeW, NodeH); // exit point on the source box
            var end = RectEdgeIntersect(to, -dir, NodeW, NodeH); // entry point on the target box
            var old = Handles.color;
            Handles.color = col;

            if (dashed)
            {
                // Walk the line in fixed-length segments, drawing every other one.
                const float dashLen = 6f;
                var total = Vector2.Distance(start, end);
                var drawn = 0f;
                var on = true;
                while (drawn < total)
                {
                    var seg = Mathf.Min(dashLen, total - drawn);
                    var s = Vector2.Lerp(start, end, drawn / total);
                    var e = Vector2.Lerp(start, end, (drawn + seg) / total);
                    if (on) Handles.DrawLine(s, e);
                    drawn += seg;
                    on = !on;
                }
            }
            else
            {
                Handles.DrawLine(start, end);
            }

            // Draw the two-pronged arrowhead at the end.
            if ((end - start).sqrMagnitude > 0.01f)
            {
                var perp = new Vector2(-dir.y, dir.x) * 5f;
                Handles.DrawLine(end, end - dir * 10f + perp);
                Handles.DrawLine(end, end - dir * 10f - perp);
            }

            Handles.color = old;
        }

        // Draws a t-thick rectangle outline by filling its four edges.
        private static void DrawBorder(Rect r, Color col, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), col);
            EditorGUI.DrawRect(new Rect(r.x, r.y + r.height - t, r.width, t), col);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), col);
            EditorGUI.DrawRect(new Rect(r.x + r.width - t, r.y, t, r.height), col);
        }
    }

    // A standalone copy of the mission generator used by the editor window. Mirrors MissionGenerator's logic.
    public class MissionGeneratorRuntime
    {
        public int complexity = 1;
        public MissionType forcedType = MissionType.Assassination;
        public bool randomType = true;
        public int seed;
        public bool randomSeed = true;

        private System.Random _rng;

        // Builds a mission graph; see MissionGenerator.Generate for the full explanation.
        public MissionGraph Generate()
        {
            // Resolve and seed the RNG for repeatable previews.
            var resolvedSeed = randomSeed ? Random.Range(0, int.MaxValue) : seed;
            _rng = new System.Random(resolvedSeed);

            // Pick the mission type and pull its content.
            var type = randomType ? (MissionType)_rng.Next(0, 3) : forcedType;
            var (prereqSets, secondaries, terminalText, terminalLabel) = MissionGeneratorData.Data[type];

            // Choose facility, prerequisite chain, and how many secondaries to add.
            var facility = Pick(MissionGeneratorData.Facilities);
            var prereqSet = Pick(prereqSets);
            var numSecondaries = complexity switch
            {
                0 => _rng.Next(0, 2),
                1 => _rng.Next(1, 3),
                _ => _rng.Next(2, 4)
            };

            var graph = new MissionGraph { type = type, facility = facility, seed = resolvedSeed };

            // Entry node.
            var entry = MakeNode("entry", "Infiltrate facility", "Mission start", NodeType.Entry);
            graph.nodes.Add(entry);

            // Chain the prerequisites, each depending on the previous.
            var prevIds = new List<string> { entry.id };
            foreach (var d in prereqSet)
            {
                var node = MakeNode($"prereq_{graph.nodes.Count}", d.text, d.label, NodeType.Prerequisite);
                node.dependencies.AddRange(prevIds);
                graph.nodes.Add(node);
                prevIds = new List<string> { node.id };
            }

            // Primary objective depends on the last prerequisite.
            var terminal = MakeNode("primary", terminalText, terminalLabel, NodeType.Primary);
            terminal.dependencies.AddRange(prevIds);
            graph.nodes.Add(terminal);

            // Secondaries branch off the entry or any prerequisite.
            var branchCandidates = graph.nodes
                .FindAll(n => n.nodeType is NodeType.Entry or NodeType.Prerequisite)
                .ConvertAll(n => n.id);

            // Add the chosen number of unique secondaries.
            var pool = new List<NodeData>(secondaries);
            for (var i = 0; i < numSecondaries && pool.Count > 0; i++)
            {
                var idx = _rng.Next(pool.Count);
                var d = pool[idx];
                pool.RemoveAt(idx);
                var sec = MakeNode($"secondary_{i}", d.text, d.label, NodeType.Secondary);
                sec.dependencies.Add(branchCandidates[_rng.Next(branchCandidates.Count)]);
                graph.nodes.Add(sec);
            }

            return graph;
        }

        // Helper to construct a node.
        private static MissionNode MakeNode(string id, string text, string label, NodeType type) => new()
            { id = id, text = text, label = label, nodeType = type };

        // Picks a random array element using the seeded RNG.
        private T Pick<T>(T[] arr) => arr[_rng.Next(arr.Length)];
    }
}
#endif