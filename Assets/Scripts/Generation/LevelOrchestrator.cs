using System.Collections;
using Exits;
using Menu;
using Player;
using UnityEngine;

namespace Generation
{
    // Drives level progression: loads the first level on start.
    // Advances to the next each time the player reaches an exit and scales difficulty/complexity by tier.
    public class LevelOrchestrator : MonoBehaviour
    {
        public FacilityOrchestrator facility;
        public MissionGenerator mission;

        [Tooltip("How many levels share one difficulty/complexity tier.")]
        public int levelsPerTier = 10;

        [Tooltip("Highest tier difficulty and complexity reach.")]
        public int maxTier = 2;

        [Tooltip("Iris wipe played around each level load.")]
        public IrisTransition iris;

        // The level number currently loaded.
        private int CurrentLevel { get; set; }

        // Start the run at level 1. The screen begins covered (see IrisTransition.Awake),
        // so the first level builds behind black and only needs to open: 1.2 -> 0.
        private void Start() => StartCoroutine(FirstLoad());

        // Advance to the level after the current one (called when an exit is reached).
        private void NextLevel() => StartCoroutine(Transition(CurrentLevel + 1));

        // Reloading causes the player to go back to the start
        private void Reload() => StartCoroutine(Transition(1));

        // Subscribe to / unsubscribe from the exit-reached event for level advancement.
        private void OnEnable()
        {
            Exit.Reached += NextLevel;
            PlayerHealth.OnDied += Reload;
        }

        private void OnDisable()
        {
            Exit.Reached -= NextLevel;
            PlayerHealth.OnDied -= Reload;
        }

        // First load: build level 1 behind the already-black screen, then open. 1.2 -> 0.
        private IEnumerator FirstLoad()
        {
            Build(1);
            yield return iris.Open();
        }

        // Every later load (next level or death-reset): close to black, rebuild
        // behind it, then open. 0 -> 1.2 -> 0, so the rebuild is never seen.
        private IEnumerator Transition(int level)
        {
            yield return iris.Close();
            Build(level);
            yield return iris.Open();
        }

        // Builds the given level, deriving its tier and regenerating the facility.
        private void Build(int level)
        {
            CurrentLevel = level;

            // 1-10: 0, 11-20: 1, 21-30: 2, then held at maxTier.
            var tier = Mathf.Min((level - 1) / levelsPerTier, maxTier);

            mission.complexity = tier;
            facility.difficulty = tier;
            facility.Generate();
        }
    }
}