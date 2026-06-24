using Player;
using UnityEngine;

namespace Audio
{
    // Plays player-related sound effects in response to PlayerHealth events.
    public class PlayerAudio : MonoBehaviour
    {
        // Clip played each time the player takes a hit (loses a life).
        [SerializeField] private AudioClip playerHitClip;
        // Clip played once when the player runs out of lives and dies.
        [SerializeField] private AudioClip playerDeathClip;

        private void OnEnable()
        {
            PlayerHealth.OnLivesChanged += PlayHitSound;
            PlayerHealth.OnDied += PlayDeathSound;
        }
        
        private void OnDisable()
        {
            PlayerHealth.OnLivesChanged -= PlayHitSound;
            PlayerHealth.OnDied -= PlayDeathSound;
        }

        private void PlayHitSound(int _)
        {
            if (!playerHitClip) return;
            UISoundManager.Instance.PlayClip(playerHitClip);
        }

        private void PlayDeathSound()
        {
            if (!playerDeathClip) return;
            UISoundManager.Instance.PlayClip(playerDeathClip);
        }
    }
}