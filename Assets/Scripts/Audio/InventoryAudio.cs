using Player;
using UnityEngine;

namespace Audio
{
    public class InventoryAudio : MonoBehaviour
    {
        // Clip played each time an item (e.g. distraction or keycard) is collected and dropped.
        [SerializeField] private AudioClip inventoryClip;

        private void OnEnable()
        {
            PlayerDistractionInventory.OnInventoryChanged += PlayInventorySound;
            PlayerKeycardInventory.OnKeycardCollected += PlayInventorySound;
        }

        private void OnDisable()
        {
            PlayerDistractionInventory.OnInventoryChanged -= PlayInventorySound;
            PlayerKeycardInventory.OnKeycardCollected -= PlayInventorySound;
        }
        
        private void PlayInventorySound(bool _) => PlayInventorySound();
        private void PlayInventorySound(string _) => PlayInventorySound();
        
        private void PlayInventorySound()
        {
            if (!inventoryClip) return;
            UISoundManager.Instance?.PlayClip(inventoryClip);
        }
    }
}