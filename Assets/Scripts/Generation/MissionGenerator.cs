using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    // The kind of mission objective the player is given.
    public enum MissionType
    {
        Assassination,
        Theft,
        Sabotage
    }

    // The role a node plays in the mission dependency graph.
    public enum NodeType
    {
        Entry, // the starting "infiltrate" node
        Prerequisite, // a step that must be done before the primary objective
        Primary, // the main objective
        Secondary // an optional side objective
    }

    // A single piece of mission text: the objective and its short HUD label.
    [System.Serializable]
    public struct NodeData
    {
        public string text;
        public string label;

        public NodeData(string text, string label)
        {
            this.text = text;
            this.label = label;
        }
    }

    // One objective in the generated mission, plus the nodes it depends on.
    [System.Serializable]
    public class MissionNode
    {
        public string id;
        public string text;
        public string label;
        public NodeType nodeType;
        public List<string> dependencies = new(); // ids of nodes that must complete first
    }

    // A complete generated mission: its type, facility, objective nodes, and the seed used.
    [System.Serializable]
    public class MissionGraph
    {
        public MissionType type;
        public string facility;
        public List<MissionNode> nodes = new();
        public int seed;
    }

    // Static content pool the generator draws from: facility names and, per mission type,
    // the candidate prerequisite chains, optional secondaries, and the final objective text.
    public static class MissionGeneratorData
    {
        // Facility names picked at random for flavour.
        public static readonly string[] Facilities =
        {
            "Secure Research Complex", "Military Installation", "Corporate Headquarters",
            "Embassy Annex", "Industrial Compound", "Offshore Platform", "Government Archive"
        };

        // Per mission type: alternative prerequisite chains, optional secondary objectives,
        // and the text/label for the terminal (primary) objective.
        public static readonly Dictionary<MissionType, (NodeData[][] prereqSets, NodeData[] secondaries,
            string terminalText, string terminalLabel)> Data = new()
        {
            [MissionType.Assassination] = (
                // prereqSets: one of these chains is chosen; each entry becomes a prerequisite step.
                prereqSets: new[]
                {
                    new[] { new NodeData("Locate target", "Identify location") },
                    new[]
                    {
                        new NodeData("Disable cameras", "Remove surveillance"),
                        new NodeData("Acquire access card", "Enter restricted area")
                    },
                    new[] { new NodeData("Obtain keycard", "Access target wing") },
                    new[]
                    {
                        new NodeData("Intercept schedule", "Find patrol window"),
                        new NodeData("Disable alarm", "Prevent alert")
                    }
                },
                // secondaries: optional objectives, a random subset is added.
                secondaries: new[]
                {
                    new NodeData("Steal documents", "Optional: intelligence"),
                    new NodeData("Plant evidence", "Optional: misdirection"),
                    new NodeData("Photograph facility", "Optional: reconnaissance"),
                    new NodeData("Sabotage generator", "Optional: power outage"),
                    new NodeData("Copy hard drive", "Optional: data extraction")
                },
                terminalText: "Eliminate target", terminalLabel: "Primary objective"
            ),
            [MissionType.Theft] = (
                prereqSets: new[]
                {
                    new[] { new NodeData("Locate asset", "Find storage room") },
                    new[]
                    {
                        new NodeData("Crack safe code", "Bypass security"),
                        new NodeData("Acquire keycard", "Access vault")
                    },
                    new[] { new NodeData("Disable weight sensor", "Bypass trap") },
                    new[]
                    {
                        new NodeData("Clone access badge", "Impersonate staff"),
                        new NodeData("Disable motion sensors", "Clear detection")
                    }
                },
                secondaries: new[]
                {
                    new NodeData("Photograph blueprints", "Optional: intelligence"),
                    new NodeData("Plant tracker", "Optional: surveillance"),
                    new NodeData("Swap decoy", "Optional: delay discovery"),
                    new NodeData("Steal credentials", "Optional: future access"),
                    new NodeData("Copy encryption key", "Optional: data access")
                },
                terminalText: "Extract asset", terminalLabel: "Primary objective"
            ),
            [MissionType.Sabotage] = (
                prereqSets: new[]
                {
                    new[] { new NodeData("Locate control room", "Find system access") },
                    new[]
                    {
                        new NodeData("Obtain access codes", "Bypass lock"),
                        new NodeData("Disable fire suppression", "Prevent auto-repair")
                    },
                    new[] { new NodeData("Cut comms relay", "Prevent reinforcements") },
                    new[]
                    {
                        new NodeData("Acquire explosives", "Collect charges"),
                        new NodeData("Map blast radius", "Ensure safe exit")
                    }
                },
                secondaries: new[]
                {
                    new NodeData("Download schematics", "Optional: intelligence"),
                    new NodeData("Destroy backups", "Optional: no recovery"),
                    new NodeData("Eliminate engineer", "Optional: delay repair"),
                    new NodeData("Disable backup power", "Optional: extend outage"),
                    new NodeData("Steal prototype", "Optional: extra objective")
                },
                terminalText: "Destroy target system", terminalLabel: "Primary objective"
            )
        };
    }

    // Procedurally builds a MissionGraph from the content pool, scaling the number of
    // optional objectives by the complexity setting and using a seeded RNG for repeatability.
    public class MissionGenerator : MonoBehaviour
    {
        [Header("Generation Settings")] [Range(0, 2)]
        public int complexity = 1; // higher = more secondary objectives

        public MissionType forcedType = MissionType.Assassination; // used when randomType is off
        public bool randomType = true;
        public int seed; // used when randomSeed is off
        public bool randomSeed = true;

        private System.Random _rng;

        // Generates a fresh mission graph.
        public MissionGraph Generate()
        {
            // Resolve and seed the RNG so a given seed always produces the same mission.
            var resolvedSeed = randomSeed ? Random.Range(0, int.MaxValue) : seed;
            _rng = new System.Random(resolvedSeed);

            // Pick the mission type and grab its content set.
            var type = randomType ? (MissionType)_rng.Next(0, 3) : forcedType;
            var (prereqSets, secondaries, terminalText, terminalLabel) = MissionGeneratorData.Data[type];

            // Choose a facility and one of the prerequisite chains.
            var facility = Pick(MissionGeneratorData.Facilities);
            var prereqSet = Pick(prereqSets);

            // Decide how many optional objectives to add, based on complexity.
            var numSecondaries = complexity switch
            {
                0 => _rng.Next(0, 2),
                1 => _rng.Next(1, 3),
                _ => _rng.Next(2, 4)
            };

            var graph = new MissionGraph { type = type, facility = facility, seed = resolvedSeed };

            // Start with the entry node.
            var entry = MakeNode("entry", "Infiltrate facility", "Mission start", NodeType.Entry);
            graph.nodes.Add(entry);

            // Chain the prerequisites in order, each depending on the previous step.
            var prevIds = new List<string> { entry.id };
            foreach (var d in prereqSet)
            {
                var node = MakeNode($"prereq_{graph.nodes.Count}", d.text, d.label, NodeType.Prerequisite);
                node.dependencies.AddRange(prevIds);
                graph.nodes.Add(node);
                prevIds = new List<string> { node.id };
            }

            // The primary objective depends on the last prerequisite.
            var terminal = MakeNode("primary", terminalText, terminalLabel, NodeType.Primary);
            terminal.dependencies.AddRange(prevIds);
            graph.nodes.Add(terminal);

            // Secondaries can branch off the entry or any prerequisite node.
            var branchCandidates = graph.nodes
                .FindAll(n => n.nodeType is NodeType.Entry or NodeType.Prerequisite)
                .ConvertAll(n => n.id);

            // Add the chosen number of secondaries, drawing without repeats from the pool.
            var pool = new List<NodeData>(secondaries);
            for (var i = 0; i < numSecondaries && pool.Count > 0; i++)
            {
                var idx = _rng.Next(pool.Count);
                var d = pool[idx];
                pool.RemoveAt(idx);
                var sec = MakeNode($"secondary_{i}", d.text, d.label, NodeType.Secondary);
                sec.dependencies.Add(
                    branchCandidates[_rng.Next(branchCandidates.Count)]); // hang it off a random earlier node
                graph.nodes.Add(sec);
            }

            return graph;
        }

        // Helper to build a node with the given fields.
        private static MissionNode MakeNode(string id, string text, string label, NodeType type) => new()
            { id = id, text = text, label = label, nodeType = type };

        // Picks a random element from an array using the seeded RNG.
        private T Pick<T>(T[] arr) => arr[_rng.Next(arr.Length)];
    }
}