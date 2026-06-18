using Camera;
using UnityEngine;

namespace Player
{
    // Spawns (or respawns) the player prefab at a given position and points the camera at it.
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;

        // The current player instance, or null if none has been spawned yet.
        private GameObject _player;

        // Spawns the player at the given position, replacing any existing instance.
        public void SpawnPlayer(Vector3 spawnPosition)
        {
            // Clear out the previous player first so we never leave duplicates behind.
            if (_player != null)
            {
                // Destroy is deferred and only valid in play mode; editor/generation needs the immediate version.
                if (Application.isPlaying) Destroy(_player);
                else DestroyImmediate(_player);
            }

            // Create the new player and have the camera follow it.
            _player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            cameraFollow.SetTarget(_player.transform);
        }
    }
}