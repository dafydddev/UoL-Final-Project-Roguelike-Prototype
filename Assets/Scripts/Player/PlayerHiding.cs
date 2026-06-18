using UnityEngine;

namespace Player
{
    // Tracks whether the player is hidden, supporting overlapping cover zones via a
    // counter (so leaving one patch while still inside another keeps the player hidden).
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerHiding : MonoBehaviour
    {
        [SerializeField] private Sprite hiddenSprite;
        [SerializeField] private Sprite visibleSprite;
        private SpriteRenderer _renderer;
        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        // How many cover zones the player is currently inside.
        private int _covers;

        // Hidden as long as the player is inside at least one cover zone.
        public bool IsHidden => _covers > 0;

        // Called when entering a cover zone. (Argument is unused; kept for caller convenience.)
        public void Enter(Object _)
        {
            _renderer.sprite = hiddenSprite;
            _covers++;
        }

        // Called when leaving a cover zone, clamped so the count never goes negative.
        public void Exit(Object _)
        {
            _renderer.sprite = visibleSprite;
            _covers = Mathf.Max(0, _covers - 1);
        }
    }
}