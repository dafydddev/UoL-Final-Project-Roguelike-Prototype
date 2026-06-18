using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace Objectives
{
    // HUD readout of the current objectives. Subscribes to the tracker and redraws the list
    // whenever objectives change, showing a checkbox per objective and greening completed ones.
    public class ObjectiveHUD : MonoBehaviour
    {
        public ObjectiveTracker tracker;
        public TMP_Text text;

        // Subscribe and draw the initial state when enabled.
        private void OnEnable()
        {
            if (tracker != null) tracker.Changed += Redraw;
            Redraw();
        }

        // Unsubscribe when disabled to avoid dangling event handlers.
        private void OnDisable()
        {
            if (tracker != null) tracker.Changed -= Redraw;
        }

        // Rebuilds the objective text, primaries first, with completed lines ticked and coloured green.
        private void Redraw()
        {
            if (!text || !tracker) return;

            var sb = new StringBuilder();
            foreach (var o in tracker.Objectives.OrderByDescending(e => e.isPrimary))
            {
                var objTag = o.isPrimary ? "Primary" : "Secondary";
                var line = $"[{(o.complete ? "x" : " ")}] {objTag}: {o.name}";
                sb.AppendLine(o.complete ? $"<color=#66cc66>{line}</color>" : line);
            }
            text.text = sb.ToString();
        }
    }
}