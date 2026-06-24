using System;
using Disguises;
using UnityEngine;

namespace Player
{
    // Grants the player a temporary disguise.
    // While disguised, guards that aren't already chasing fail to recognise the player (see GuardVision).
    // Picked up by walking into a Disguise.
    [RequireComponent(typeof(Collider2D))]
    public class PlayerDisguise : MonoBehaviour
    {
        // Fires only when the disguised state actually flips, mirroring PlayerHiding.OnHiddenChanged.
        public static event Action<bool> OnDisguisedChanged;

        private float _expiry; // time at which the current disguise wears off
        private bool _disguised; // current disguised state

        // Picks up any disguise the player walks into, starting (or refreshing) the timer.
        private void OnTriggerEnter2D(Collider2D other)
        {
            var disguise = other.GetComponent<Disguise>();
            if (disguise == null) return;
            // Apply the disguise and remove the pickup from the world.
            _expiry = Time.time + disguise.duration;
            SetDisguised(true);
            Destroy(disguise.gameObject);
        }

        // Drop the disguise once its timer runs out.
        private void Update()
        {
            if (_disguised && Time.time >= _expiry) SetDisguised(false);
        }

        // Updates the flag and broadcasts only on an actual change.
        private void SetDisguised(bool value)
        {
            if (_disguised == value) return;
            _disguised = value;
            OnDisguisedChanged?.Invoke(value);
        }
    }
}