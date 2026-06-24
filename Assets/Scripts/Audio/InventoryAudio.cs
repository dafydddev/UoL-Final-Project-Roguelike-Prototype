using Items;
using Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audio
{
    public class InventoryAudio : MonoBehaviour
    {
        // Clip played each time an item (e.g. distraction or keycard) is collected and dropped.
        [SerializeField] private AudioClip inventoryClip;

        private void OnEnable()
        {
            PlayerDistractionInventory.OnInventoryChanged += PlayInventorySound;
        }

        private void OnDisable()
        {
            PlayerDistractionInventory.OnInventoryChanged -= PlayInventorySound;
        }

        private void PlayInventorySound(bool _)
        {
            if (!inventoryClip) return;
            UISoundManager.Instance.PlayClip(inventoryClip);
        }
    }
}