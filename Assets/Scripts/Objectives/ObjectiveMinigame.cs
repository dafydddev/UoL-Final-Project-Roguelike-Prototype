using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Objectives
{
    // A arrow minigame attached to an objective.
    // When the player steps onto it, a random arrow sequence appears; entering it correctly completes the objective.
    // Leaving the trigger cancels it, and a wrong press restarts the sequence.
    [RequireComponent(typeof(Objective))]
    public class ObjectiveMinigame : MonoBehaviour
    {
        [Header("Arrow sprites")] public Sprite arrowUp;
        public Sprite arrowDown;
        public Sprite arrowLeft;
        public Sprite arrowRight;

        [Header("Settings")] public int sequenceLength = 3;
        public float arrowSpacing = 0.6f; // gap between arrow icons
        public Vector3 displayOffset = new(0f, 1.5f, 0f); // where the icons float relative to the objective

        private readonly List<int> _sequence = new(); // the target sequence: 0 up, 1 down, 2 left, 3 right
        private readonly List<SpriteRenderer> _icons = new(); // on-screen arrow icons
        private int _index; // how many arrows the player has matched so far
        private bool _active; // minigame currently running
        private bool _done; // already completed (won't retrigger)

        private Objective _objective;
        private ObjectiveTracker _tracker;

        public static event Action OnMiniGameSuccess;
        public static event Action OnMiniGameFailure;

        private void Awake()
        {
            _objective = GetComponent<Objective>();
            _tracker = FindAnyObjectByType<ObjectiveTracker>();
        }

        // Start the minigame when the player enters (unless already done or running).
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_done || _active) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Begin();
        }

        // Cancel if the player walks away mid-sequence.
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_active) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Cancel();
        }

        private void Update()
        {
            if (!_active) return;

            // Read a direction press; -1 means nothing relevant this frame.
            var dir = PressedDirection();
            if (dir < 0) return;

            if (dir == _sequence[_index])
            {
                // Correct press: advance, finishing if that was the last arrow.
                _index++;
                if (_index >= _sequence.Count) Finish();
                else Refresh();
            }
            else
            {
                _index = 0; // wrong press restarts the sequence
                Refresh();
                OnMiniGameFailure?.Invoke();
            }
        }

        // Begin a fresh run with a new random sequence and its icons.
        private void Begin()
        {
            _active = true;
            _index = 0;
            _sequence.Clear();
            for (var i = 0; i < sequenceLength; i++) _sequence.Add(Random.Range(0, 4));
            BuildIcons();
        }

        // Complete the objective and tear down the minigame so it won't run again.
        private void Finish()
        {
            _active = false;
            _done = true;
            ClearIcons();
            if (_tracker)
            {
                _tracker.Complete(_objective.id);
            }
            OnMiniGameSuccess?.Invoke();
        }

        // Abort the current run without completing.
        private void Cancel()
        {
            _active = false;
            _index = 0;
            ClearIcons();
        }

        // Returns the direction pressed this frame (0-3) from keyboard or gamepad, or -1 if none.
        private static int PressedDirection()
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame) return 0;
                if (k.downArrowKey.wasPressedThisFrame) return 1;
                if (k.leftArrowKey.wasPressedThisFrame) return 2;
                if (k.rightArrowKey.wasPressedThisFrame) return 3;
            }

            var g = Gamepad.current;
            if (g != null)
            {
                if (g.dpad.up.wasPressedThisFrame) return 0;
                if (g.dpad.down.wasPressedThisFrame) return 1;
                if (g.dpad.left.wasPressedThisFrame) return 2;
                if (g.dpad.right.wasPressedThisFrame) return 3;
            }

            return -1;
        }

        // Creates the row of arrow icons for the current sequence, centred above the objective.
        private void BuildIcons()
        {
            ClearIcons();
            var startX = -(_sequence.Count - 1) * arrowSpacing / 2f; // centre the row
            for (var i = 0; i < _sequence.Count; i++)
            {
                var go = new GameObject($"Arrow_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = displayOffset + new Vector3(startX + i * arrowSpacing, 0f, 0f);
                var spriteRend = go.AddComponent<SpriteRenderer>();
                spriteRend.sprite = SpriteFor(_sequence[i]);
                spriteRend.sortingOrder = 100;
                _icons.Add(spriteRend);
            }

            Refresh();
        }

        // Dims arrows already entered and leaves the rest bright, showing progress.
        private void Refresh()
        {
            for (var i = 0; i < _icons.Count; i++)
            {
                _icons[i].color = i < _index ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
            }
        }

        // Destroys all arrow icons and clears the list.
        private void ClearIcons()
        {
            foreach (var ic in _icons.Where(ic => ic))
            {
                Destroy(ic.gameObject);
            }

            _icons.Clear();
        }

        // Maps a direction code to its arrow sprite.
        private Sprite SpriteFor(int dir) => dir switch
        {
            0 => arrowUp,
            1 => arrowDown,
            2 => arrowLeft,
            _ => arrowRight
        };
    }
}