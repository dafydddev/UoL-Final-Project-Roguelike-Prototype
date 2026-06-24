using UnityEngine;

namespace Menu
{
    // Toggles between two UI panels, showing one and hiding the other.
    public class MenuSwitcher : MonoBehaviour
    {
        public GameObject menuA;
        public GameObject menuB;

        // Show the first menu and hide the second.
        public void ShowA()
        {
            menuA.SetActive(true);
            menuB.SetActive(false);
        }

        // Show the second menu and hide the first.
        public void ShowB()
        {
            menuA.SetActive(false);
            menuB.SetActive(true);
        }
    }
}