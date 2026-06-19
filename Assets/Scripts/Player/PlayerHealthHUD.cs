using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class PlayerHealthHUD : MonoBehaviour
    {
        [SerializeField] private Sprite lifeIconSprite;
        private Image[] _lifeIcons;

        private void Awake()
        {
            // The life-slot icons placed as children in the HUD, one per max life.
            _lifeIcons = GetComponentsInChildren<Image>();
            foreach (var icon in _lifeIcons) icon.sprite = lifeIconSprite;
        }

        private void OnEnable()
        {
            PlayerHealth.OnLivesChanged += SetLifeIcons;
        }

        private void OnDisable()
        {
            PlayerHealth.OnLivesChanged -= SetLifeIcons;
        }

        private void SetLifeIcons(int livesRemaining)
        {
            for (var i = 0; i < _lifeIcons.Length; i++)
            {
                _lifeIcons[i].enabled = i < livesRemaining;
            }
        }
    }
}