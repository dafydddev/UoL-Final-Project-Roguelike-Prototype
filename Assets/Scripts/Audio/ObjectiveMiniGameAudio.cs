using Objectives;
using UnityEngine;

namespace Audio
{
    public class ObjectiveMiniGameAudio : MonoBehaviour
    {
        // Clip played each time an item (e.g. distraction or keycard) is collected and dropped.
        [SerializeField] private AudioClip miniGameSuccessClip;
        [SerializeField] private AudioClip miniGameFailureClip;
  
        private void OnEnable()
        {
            ObjectiveMinigame.OnMiniGameSuccess += PlayMiniGameSuccessSound;
            ObjectiveMinigame.OnMiniGameFailure += PlayMiniGameFailureSound;
        }

        private void OnDisable()
        {
            ObjectiveMinigame.OnMiniGameSuccess -= PlayMiniGameSuccessSound;
            ObjectiveMinigame.OnMiniGameFailure -= PlayMiniGameFailureSound;
        }

        private void PlayMiniGameSuccessSound()
        {
            if (!miniGameSuccessClip) return;
            UISoundManager.Instance?.PlayClip(miniGameSuccessClip);
        }
        
        private void PlayMiniGameFailureSound()
        {
            if (!miniGameFailureClip) return;
            UISoundManager.Instance?.PlayClip(miniGameFailureClip);
        }
    }
}