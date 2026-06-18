using System;
using Objectives;
using Player;
using UnityEngine;

namespace Exits
{
    // The level exit.
    // Fires the Reached event when the player enters it, but only once the primary objective is complete.
    [RequireComponent(typeof(BoxCollider2D))]
    public class Exit : MonoBehaviour
    {
        // Raised when the player successfully reaches the exit.
        public static event Action Reached;

        private ObjectiveTracker _tracker;

        // Cache the objective tracker so we can check completion on contact.
        private void Awake() => _tracker = FindAnyObjectByType<ObjectiveTracker>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only the player can trigger the exit.
            if (other.GetComponent<PlayerController>() == null) return;

            // The exit stays inert until the primary objective is done.
            if (_tracker == null || !_tracker.PrimaryComplete) return;

            Reached?.Invoke();
        }
    }
}