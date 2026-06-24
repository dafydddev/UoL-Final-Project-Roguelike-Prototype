using Keycards;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    // HUD for collected keycards. On each pickup it adds an Image under the parent,
    // showing the keycard sprite tinted to that key's colour.
    public class KeycardHUD : MonoBehaviour
    {
        [SerializeField] private Transform iconParent;
        [SerializeField] private Sprite keycardSprite;
        [SerializeField] private float keycardImageScale = 0.25f;

        private void OnEnable()
        {
            PlayerKeycardInventory.OnKeycardCollected += OnKeycardCollected;
            PlayerKeycardInventory.OnInventoryReset += Clear;
        }

        private void OnDisable()
        {
            PlayerKeycardInventory.OnKeycardCollected -= OnKeycardCollected;
            PlayerKeycardInventory.OnInventoryReset -= Clear;
        }

        // Add one icon for the collected card, tinted to its colour.
        private void OnKeycardCollected(string keyId)
        {
            var icon = new GameObject($"Keycard_{keyId}").AddComponent<Image>();
            icon.transform.localScale = Vector3.one * keycardImageScale;
            icon.transform.SetParent(iconParent, false);
            icon.sprite = keycardSprite;
            icon.color = KeyColour.For(keyId);
        }

        private void Clear()
        {
            for (var i = iconParent.childCount - 1; i >= 0; i--)
            {
                Destroy(iconParent.GetChild(i).gameObject);
            }
        }
    }
}