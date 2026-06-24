using UnityEngine;

namespace Player
{
    // Single owner of the player's sprite.
    // PlayerHiding and PlayerDisguise track state and fire events.
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerSprite : MonoBehaviour
    {
        [SerializeField] private Sprite hiddenSprite;
        [SerializeField] private Sprite disguisedSprite;
        private SpriteRenderer _renderer;
        private Sprite _baseSprite; // normal appearance, captured at startup
        private bool _hidden;
        private bool _disguised;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseSprite = _renderer.sprite;
        }

        private void OnEnable()
        {
            PlayerHiding.OnHiddenChanged += SetHidden;
            PlayerDisguise.OnDisguisedChanged += SetDisguised;
        }

        private void OnDisable()
        {
            PlayerHiding.OnHiddenChanged -= SetHidden;
            PlayerDisguise.OnDisguisedChanged -= SetDisguised;
        }

        private void SetHidden(bool value) { _hidden = value; Refresh(); }
        private void SetDisguised(bool value) { _disguised = value; Refresh(); }

        // Hidden takes visual priority over disguise; disguise over the normal sprite.
        private void Refresh()
        {
            if (_hidden && hiddenSprite) _renderer.sprite = hiddenSprite;
            else if (_disguised && disguisedSprite) _renderer.sprite = disguisedSprite;
            else _renderer.sprite = _baseSprite;
        }
    }
}