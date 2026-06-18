using Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    // Lets the player pick up a distraction by walking into it, then drop it at their current position on a left-click.
    public class ThrowController : MonoBehaviour
    {
        // The distraction currently being carried, or null if hands are empty.
        private Distraction _carried;

        // Picks up a distraction the player walks into.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Can only carry one at a time.
            if (_carried != null) return;
            // Only react to dropped-and-available distractions, not other colliders.
            var distraction = other.GetComponent<Distraction>();
            if (distraction == null || distraction.Dropped) return;
            // Carry it and hide it from the world while held.
            _carried = distraction;
            distraction.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Throw on left-click, but only if we're actually carrying something.
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (!_carried) return;
            // Drop it at the player's position and free our hands.
            _carried.Drop(transform.position);
            _carried = null;
        }
    }
}