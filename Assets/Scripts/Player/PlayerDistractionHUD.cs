using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Player
{
    // HUD icon for the player's throwable. Shows the icon while the player is carrying a
    // throwable and hides it otherwise, driven by ThrowableInventory's change event.
    [RequireComponent(typeof(Image))]
    public class PlayerDistractionHUD : MonoBehaviour
    {
        [SerializeField] private Sprite inventoryIconSprite;
        [SerializeField] private TMP_Text countLabel; // shows "x3" etc.
        private Image _inventoryIcon;

        private void Awake()
        {
            _inventoryIcon = GetComponent<Image>();
            _inventoryIcon.enabled = false;
            if (inventoryIconSprite) _inventoryIcon.sprite = inventoryIconSprite;
        }

        private void OnEnable() => PlayerDistractionInventory.OnInventoryChanged += OnCountChanged;
        private void OnDisable() => PlayerDistractionInventory.OnInventoryChanged -= OnCountChanged;

        // Show the icon while carrying at least one, and display how many.
        private void OnCountChanged(int count)
        {
            if (!_inventoryIcon || !inventoryIconSprite) return;
            _inventoryIcon.enabled = count > 0;
            if (countLabel) countLabel.text = count > 0 ? $"x {count}" : "";
        }
    }
}