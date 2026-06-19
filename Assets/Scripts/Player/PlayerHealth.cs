using System;
using System.Collections;
using Guards.GOAP;
using UnityEngine;

namespace Player
{
    // The player's lives and damage handling. Takes a hit when a guard catches the player,
    // with a brief invulnerability window after each hit, and announces life changes / death.
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int fullLives = 3;
        [SerializeField] private float invulnerableSeconds = 1f; // i-frame duration after a hit
        private int _currentLives;

        public static event Action<int> OnLivesChanged; // current lives, for the HUD
        public static event Action OnDied;

        private bool _hurtable = true; // false during the invulnerability window
        private bool _dead; // latched at 0 lives so death is handled exactly once

        private void OnEnable() => GuardAgent.OnPlayerCaught += TakeHit;
        private void OnDisable() => GuardAgent.OnPlayerCaught -= TakeHit;

        // Start at full lives and publish the initial value.
        private void Awake()
        {
            _currentLives = fullLives;
            OnLivesChanged?.Invoke(_currentLives);
        }

        // Called when a guard catches the player.
        // The "was this a valid hit?" decision already happened upstream, this only owns i-frames and lives.
        private void TakeHit()
        {
            // Already dead or mid-i-frames: ignore. The dead latch matters because OnDied
            // triggers a full level reload — it must fire once, not once per later catch.
            if (_dead || !_hurtable) return;

            _currentLives--;
            OnLivesChanged?.Invoke(_currentLives);

            if (_currentLives <= 0)
            {
                _dead = true;
                OnDied?.Invoke();
                return;
            }

            StartCoroutine(Invulnerability());
        }

        // Disables damage for invulnerableSeconds, then re-enables it.
        private IEnumerator Invulnerability()
        {
            _hurtable = false;
            yield return new WaitForSeconds(invulnerableSeconds);
            _hurtable = true;
        }
    }
}