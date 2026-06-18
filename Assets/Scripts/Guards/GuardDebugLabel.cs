using Guards.Goap;
using UnityEngine;

namespace Guards
{
    // Floats a text label above each guard showing its current GOAP goal.
    // You can see what the AI is "thinking" at a glance while playing.
    [RequireComponent(typeof(GuardAgent))]
    public class GuardDebugLabel : MonoBehaviour
    {
        public Vector3 offset = new(0f, 0.8f, 0f); // label position relative to the guard
        public int fontSize = 32;
        public float characterSize = 0.08f;

        private GuardAgent _agent;
        private TextMesh _text;

        private void Awake()
        {
            _agent = GetComponent<GuardAgent>();
            // Create a child object to hold the floating text.
            var go = new GameObject("DebugLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;

            // Configure the TextMesh appearance.
            _text = go.AddComponent<TextMesh>();
            _text.anchor = TextAnchor.LowerCenter;
            _text.alignment = TextAlignment.Center;
            _text.fontSize = fontSize;
            _text.characterSize = characterSize;
            _text.color = Color.white;

            go.GetComponent<MeshRenderer>().sortingOrder = 1000; // draw over the facility
        }

        // Refresh the label each frame to track the guard's current goal and position.
        private void LateUpdate()
        {
            if (!_text) return;
            _text.text = _agent.CurrentGoal;
            _text.transform.localPosition = offset; // hold position as the guard moves
        }
    }
}