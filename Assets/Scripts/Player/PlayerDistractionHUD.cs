using Items;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    // HUD icon for the player's throwable. Shows the icon while the player is carrying a
    // throwable and hides it otherwise, driven by ThrowableInventory's change event.
    [RequireComponent(typeof(Image))]
    public class PlayerDistractionHUD : MonoBehaviour
    {
        [SerializeField] private Sprite inventoryIconSprite;
        private Image _inventoryIcon;

        private void Awake()
        {
            // Start hidden, and apply the configured icon sprite if one was set.
            _inventoryIcon = GetComponent<Image>();
            _inventoryIcon.enabled = false;
            if (inventoryIconSprite) _inventoryIcon.sprite = inventoryIconSprite;
        }

        // Listen for inventory changes while active.
        private void OnEnable()
        {
            PlayerDistractionInventory.OnInventoryChanged += OnThrowableChanged;
        }

        private void OnDisable()
        {
            PlayerDistractionInventory.OnInventoryChanged -= OnThrowableChanged;
        }

        // Show or hide the icon based on whether the player is currently holding a throwable.
        private void OnThrowableChanged(bool hasThrowable)
        {
            if (!_inventoryIcon || !inventoryIconSprite) return;
            _inventoryIcon.enabled = hasThrowable;
        }
    }
}