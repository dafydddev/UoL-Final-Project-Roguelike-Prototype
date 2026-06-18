using UnityEngine;

namespace Guards
{
    // Gives each guard mild individuality: randomised tuning at spawn,
    // plus a slow sway so movement isn't a perfectly straight line.
    [RequireComponent(typeof(GuardMovement))]
    public class GuardPersonality : MonoBehaviour
    {
        // Per-guard random spread applied to speed and vision, as a ± fraction.
        [Header("Variation (±fraction)")]
        public float speedVariation = 0.15f;
        public float visionRangeVariation = 0.15f;
        public float visionAngleVariation = 0.1f;

        [Header("Sway")]
        [Tooltip("How far the guard drifts sideways from a straight line, in world units.")]
        public float swayAmount = 0.12f;
        [Tooltip("How quickly the sway oscillates.")]
        public float swayFrequency = 1.5f;

        public float SwayOffset { get; private set; } // sampled each frame, read by movement

        private float _swayPhase; // current point in the sway cycle
        private float _swaySpeed; // this guard's own sway rate

        private void Start()
        {
            // Randomise move speed slightly so guards aren't identical.
            var move = GetComponent<GuardMovement>();
            move.moveSpeed *= RandFactor(speedVariation);

            // Randomise vision range/angle if this guard has a vision component.
            var vision = GetComponent<GuardVision>();
            if (vision != null)
            {
                vision.viewRange *= RandFactor(visionRangeVariation);
                vision.viewAngle *= RandFactor(visionAngleVariation);
            }

            // Give each guard a distinct sway phase and rhythm so they don't wobble in unison.
            _swayPhase = Random.Range(0f, Mathf.PI * 2f);   // distinct starting phase
            _swaySpeed = swayFrequency * RandFactor(0.3f);  // distinct rhythm
            move.SetPersonality(this);
        }

        private void Update()
        {
            // Advance the sway cycle and sample a sideways offset for movement to read this frame.
            _swayPhase += _swaySpeed * Time.deltaTime;
            SwayOffset = Mathf.Sin(_swayPhase) * swayAmount;
        }

        // Returns a random multiplier in [1 - variation, 1 + variation].
        private static float RandFactor(float variation) => 1f + Random.Range(-variation, variation);
    }
}