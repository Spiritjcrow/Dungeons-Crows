using UnityEngine;

namespace DungeonsCrows.CameraSystem
{
    /// <summary>
    /// High-angle exploration camera with deliberately restrained motion.
    /// Keeps the board-game readability of an isometric crawler while still
    /// allowing Unity to render a fully 3D world.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RetroGothicCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Range(35f, 70f)] private float pitch = 52f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private float distance = 12f;
        [SerializeField] private float height = 10f;
        [SerializeField] private float followSharpness = 10f;
        [SerializeField] private bool orthographicExploration = true;
        [SerializeField] private float orthographicSize = 7.5f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ApplyProjection();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            Vector3 desired = target.position + offset + Vector3.up * height;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        public void SetExplorationProjection(bool useOrthographic)
        {
            orthographicExploration = useOrthographic;
            ApplyProjection();
        }

        private void ApplyProjection()
        {
            if (_camera == null) return;
            _camera.orthographic = orthographicExploration;
            _camera.orthographicSize = orthographicSize;
        }
    }
}
