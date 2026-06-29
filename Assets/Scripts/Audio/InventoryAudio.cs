using Player;
using UnityEngine;

namespace Audio
{
    public class InventoryAudio : MonoBehaviour
    {
        // Clip played each time an item (e.g. distraction or keycard) is collected and dropped.
        [SerializeField] private AudioClip distractionClip;
        [SerializeField] private AudioClip keycardClip;

        private void OnEnable()
        {
            PlayerDistractionInventory.OnInventoryChanged += PlayDistractionSound;
            PlayerKeycardInventory.OnKeycardCollected += PlayKeycardSound;
        }

        private void OnDisable()
        {
            PlayerDistractionInventory.OnInventoryChanged -= PlayDistractionSound;
            PlayerKeycardInventory.OnKeycardCollected -= PlayKeycardSound;
        }
        
        private void PlayDistractionSound(int _) => PlayDistractionSound();
        private void PlayKeycardSound(string _) => PlayKeycardSound();

        private void PlayDistractionSound()
        {
            if (!distractionClip) return;
            UISoundManager.Instance?.PlayClip(distractionClip);
        }
        
        private void PlayKeycardSound()
        {
            if (!keycardClip) return;
            UISoundManager.Instance?.PlayClip(keycardClip);
        }
    }
}