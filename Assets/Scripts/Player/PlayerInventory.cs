using System.Collections.Generic;
using Keycards;
using UnityEngine;

namespace Player
{
    // Tracks which keycards the player has collected, so locked doors can check access.
    [RequireComponent(typeof(Collider2D))]
    public class PlayerInventory : MonoBehaviour
    {
        // The set of key ids the player currently holds.
        private readonly HashSet<string> _keys = new();

        // A locked door opens when this returns true for its edge.keyRoomId.
        public bool HasKey(string keyId) => keyId != null && _keys.Contains(keyId);

        // Picks up any keycard the player walks into.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Ignore colliders that aren't keycards.
            var card = other.GetComponent<Keycard>();
            if (card == null) return;
            // Store the key id and remove the pickup from the world.
            _keys.Add(card.keyId);
            Destroy(card.gameObject);
        }
    }
}