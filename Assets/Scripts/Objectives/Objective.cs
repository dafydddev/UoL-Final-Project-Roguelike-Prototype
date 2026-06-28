using UnityEngine;

namespace Objectives
{
    // Marks a world object as an objective with a given id.
    // Hides itself once the tracker reports that objective complete.
    public class Objective : MonoBehaviour
    {
        public string id;

        private ObjectiveTracker _tracker;

        // Find the tracker and listen for objective changes.
        private void OnEnable()
        {
            _tracker = FindAnyObjectByType<ObjectiveTracker>();
            if (_tracker != null) _tracker.OnObjectiveChanged += OnOnObjectiveChanged;
        }

        // Stop listening when disabled.
        private void OnDisable()
        {
            if (_tracker != null) _tracker.OnObjectiveChanged -= OnOnObjectiveChanged;
        }

        // Deactivate this object once its objective has been completed.
        private void OnOnObjectiveChanged()
        {
            if (_tracker.IsComplete(id)) gameObject.SetActive(false);
        }
    }
}