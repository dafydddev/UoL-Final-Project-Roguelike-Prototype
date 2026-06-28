using System;
using System.Collections.Generic;
using Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    // Lets the player pick up multiple distractions by walking into them,
    // then throw them one at a time at their current position on a left-click.
    public class PlayerDistractionInventory : MonoBehaviour
    {
        // Raised whenever the carried count changes, carrying the new total so the HUD can update.
        public static event Action<int> OnInventoryChanged;

        // The distractions currently being carried (newest last).
        private readonly List<DistractionItem> _carried = new();

        private void Awake() => OnInventoryChanged?.Invoke(0);

        // Picks up a distraction the player walks into
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only react to not-yet-thrown distractions, not other colliders.
            var distraction = other.GetComponent<DistractionItem>();
            if (distraction == null || distraction.Dropped) return;
            // Carry it and hide it from the world while held.
            _carried.Add(distraction);
            distraction.gameObject.SetActive(false);
            OnInventoryChanged?.Invoke(_carried.Count);
        }

        private void Update()
        {
            // Throw on left-click, but only if we're actually carrying something.
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (_carried.Count == 0) return;
            // Pop the most recently picked-up item and drop it at the player's position.
            var last = _carried.Count - 1;
            var item = _carried[last];
            _carried.RemoveAt(last);
            item.Drop(transform.position);
            OnInventoryChanged?.Invoke(_carried.Count);
        }
    }
}