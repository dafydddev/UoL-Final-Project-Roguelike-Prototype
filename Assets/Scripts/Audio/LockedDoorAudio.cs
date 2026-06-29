using Doors;
using UnityEngine;

namespace Audio
{
    public class LockedDoorAudio : MonoBehaviour
    {
        // Clip played each time a locked door is unlocked.
        [SerializeField] private AudioClip unlockedClip;

        private void OnEnable()
        {
            LockedDoor.OnDoorOpened += PlayUnlockedSound;
        }

        private void OnDisable()
        {
            LockedDoor.OnDoorOpened -= PlayUnlockedSound;
        }

        private void PlayUnlockedSound()
        {
            if (!unlockedClip) return;
            UISoundManager.Instance?.PlayClip(unlockedClip);
        }
    }
}