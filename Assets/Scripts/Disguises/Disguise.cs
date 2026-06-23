using UnityEngine;

namespace Disguises
{
    // A wearable disguise pickup. Walking into it cloaks the player for a short time:
    // Guards that aren't already chasing fail to recognise them. See PlayerDisguise / GuardVision.
    public class Disguise : MonoBehaviour
    {
        [Tooltip("How long the disguise lasts once picked up, in seconds.")]
        public float duration = 15f;
    }
}
