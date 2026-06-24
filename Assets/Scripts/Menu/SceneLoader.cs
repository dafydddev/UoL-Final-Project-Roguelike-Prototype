using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menu
{
    // Loads scenes in response to UI button presses.
    public class SceneLoader : MonoBehaviour
    {
        // Load a scene by its name (must be added to Build Settings).
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}