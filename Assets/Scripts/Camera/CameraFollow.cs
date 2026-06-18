using UnityEngine;

namespace Camera
{
    public class CameraFollow : MonoBehaviour
    {
        // The object the camera follows.
        [SerializeField] private Transform target;

        // How quickly the camera catches up to the target.
        [SerializeField] private float speed = 10f;

        // How far back the camera sits from the target.
        [SerializeField] private float zOffset = 10f;

        private void Update()
        {
            // Do nothing if there's no target to follow.
            if (!target) return;
            // Smoothly move toward the target, kept zOffset units back.
            transform.position = Vector3.Lerp(transform.position, target.position + Vector3.back * zOffset,
                speed * Time.deltaTime);
        }

        public void SetTarget(Transform targetTransform)
        {
            // Set which object the camera should follow.
            target = targetTransform;
        }
    }
}