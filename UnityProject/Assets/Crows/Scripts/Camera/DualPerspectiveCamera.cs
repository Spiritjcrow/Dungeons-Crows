using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonsCrows.CameraSystem
{
    public enum PerspectiveMode
    {
        FirstPerson,
        ThirdPerson
    }

    [RequireComponent(typeof(Camera))]
    public sealed class DualPerspectiveCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform firstPersonAnchor;

        [Header("Mode")]
        [SerializeField] private PerspectiveMode mode = PerspectiveMode.ThirdPerson;
        [SerializeField] private bool allowPlayerToggle = true;

        [Header("Third Person")]
        [SerializeField] private Vector3 thirdPersonShoulderOffset = new Vector3(0.65f, 1.9f, -4.8f);
        [SerializeField, Range(0.5f, 12f)] private float thirdPersonDistance = 4.8f;
        [SerializeField, Range(-70f, 80f)] private float thirdPersonPitch = 12f;

        [Header("First Person")]
        [SerializeField] private Vector3 firstPersonFallbackOffset = new Vector3(0f, 1.72f, 0.08f);

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float gamepadLookSpeed = 115f;
        [SerializeField] private float followSharpness = 18f;
        [SerializeField, Range(-85f, 85f)] private float minPitch = -72f;
        [SerializeField, Range(-85f, 85f)] private float maxPitch = 78f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.22f;
        [SerializeField] private float collisionPadding = 0.08f;

        private Camera _camera;
        private float _yaw;
        private float _pitch;

        public PerspectiveMode Mode => mode;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = false;
            _yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
            _pitch = mode == PerspectiveMode.ThirdPerson ? thirdPersonPitch : transform.eulerAngles.x;
        }

        private void Update()
        {
            if (allowPlayerToggle && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
                TogglePerspective();

            ReadLookInput();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (mode == PerspectiveMode.FirstPerson)
                UpdateFirstPerson();
            else
                UpdateThirdPerson();
        }

        public void SetTarget(Transform newTarget, Transform newFirstPersonAnchor = null)
        {
            target = newTarget;
            firstPersonAnchor = newFirstPersonAnchor;
            if (target != null) _yaw = target.eulerAngles.y;
        }

        public void TogglePerspective()
        {
            SetPerspective(mode == PerspectiveMode.FirstPerson
                ? PerspectiveMode.ThirdPerson
                : PerspectiveMode.FirstPerson);
        }

        public void SetPerspective(PerspectiveMode nextMode)
        {
            mode = nextMode;
            if (_camera != null) _camera.orthographic = false;
        }

        private void ReadLookInput()
        {
            Vector2 look = Vector2.zero;

            if (Mouse.current != null && Mouse.current.delta.IsActuated())
                look += Mouse.current.delta.ReadValue() * mouseSensitivity;

            if (Gamepad.current != null)
                look += Gamepad.current.rightStick.ReadValue() * (gamepadLookSpeed * Time.unscaledDeltaTime);

            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);
        }

        private void UpdateFirstPerson()
        {
            Transform anchor = firstPersonAnchor != null ? firstPersonAnchor : target;
            Vector3 desiredPosition = firstPersonAnchor != null
                ? anchor.position
                : target.TransformPoint(firstPersonFallbackOffset);

            Quaternion desiredRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }

        private void UpdateThirdPerson()
        {
            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * thirdPersonShoulderOffset.y;
            Vector3 lateral = orbit * Vector3.right * thirdPersonShoulderOffset.x;
            Vector3 backward = orbit * Vector3.back * thirdPersonDistance;
            Vector3 desiredPosition = pivot + lateral + backward;

            Vector3 ray = desiredPosition - pivot;
            float rayLength = ray.magnitude;
            if (rayLength > 0.001f &&
                Physics.SphereCast(
                    pivot,
                    collisionRadius,
                    ray.normalized,
                    out RaycastHit hit,
                    rayLength,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                desiredPosition = pivot + ray.normalized * Mathf.Max(
                    0.1f,
                    hit.distance - collisionPadding);
            }

            Quaternion desiredRotation = Quaternion.LookRotation(
                pivot - desiredPosition,
                Vector3.up);

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }
    }
}
