using UnityEngine;

namespace Audio
{
    /// Centralised, always-active AudioSource for UI sound effects.
    [RequireComponent(typeof(AudioSource))]
    public class UISoundManager : MonoBehaviour
    {
        // Global access point. Set once in Awake and torn down with the instance.
        public static UISoundManager Instance { get; private set; }

        // The single 2D AudioSource used for all PlayOneShot calls.
        private AudioSource _source;

        private void Awake()
        {
            // Singleton guard: if another instance already claimed the slot
            // destroy this one and bail before overwriting the existing Instance.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Survive scene loads (MainMenu -> gameplay) so UI audio is shared across scenes.
            DontDestroyOnLoad(gameObject);

            _source = GetComponent<AudioSource>();
        }

        private void OnDestroy()
        {
            // Clear the static reference only if we're the live instance
            if (Instance == this) Instance = null;
        }

        public void PlayClip(AudioClip clip)
        {
            // Guard against a missing source or a null clip reference rather
            if (!_source || !clip) return;
            _source.PlayOneShot(clip);
        }
    }
}