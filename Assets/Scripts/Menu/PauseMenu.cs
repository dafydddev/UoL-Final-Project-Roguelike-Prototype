using UnityEngine;
using UnityEngine.InputSystem;

namespace Menu
{
    // Toggles a pause panel via an InputSystem action, freezing the game while open.
    // Buttons on the panel call Resume / SceneLoader, like the other menus.
    public class PauseMenu : MonoBehaviour
    {
        // The Pause action from the InputSystem_Actions asset (same asset RebindMenu uses).
        [SerializeField] private InputActionReference pauseAction;
        // The panel shown while paused; hidden when playing.
        [SerializeField] private GameObject pausePanel;

        private bool _paused;

        private void Start() => SetPaused(false); // start unpaused whatever the editor state

        private void OnEnable()
        {
            // Listen for the toggle and enable the action so it fires.
            if (pauseAction == null) return;
            pauseAction.action.performed += OnPause;
            pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            if (pauseAction != null)
            {
                pauseAction.action.performed -= OnPause;
                pauseAction.action.Disable();
            }
            // Never leave time frozen if this object is torn down (e.g. scene change).
            Time.timeScale = 1f;
        }

        // Fired when the Pause action is performed.
        private void OnPause(InputAction.CallbackContext _) => SetPaused(!_paused);

        // Hooked to the panel's Resume button.
        public void Resume() => SetPaused(false);

        // Shows/hides the panel and freezes or resumes game time.
        private void SetPaused(bool paused)
        {
            _paused = paused;
            if (pausePanel) pausePanel.SetActive(paused);
            // Time.timeScale 0 halts physics/animation; UI and InputSystem still run.
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}