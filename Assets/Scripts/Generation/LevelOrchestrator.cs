using Exits;
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

        // The level number currently loaded.
        private int CurrentLevel { get; set; }

        // Start the run at level 1.
        private void Start() => LoadLevel(1);

        // Advance to the level after the current one (called when an exit is reached).
        private void NextLevel() => LoadLevel(CurrentLevel + 1);
        
        private void Reload() => LoadLevel(CurrentLevel);

        // Loads the given level, deriving its tier and regenerating the facility.
        private void LoadLevel(int level)
        {
            CurrentLevel = level;

            // 1-10: 0, 11-20: 1, 21-30: 2, then held at maxTier.
            var tier = Mathf.Min((level - 1) / levelsPerTier, maxTier);

            mission.complexity = tier;
            facility.difficulty = tier;
            facility.Generate();
        }

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
    }
}