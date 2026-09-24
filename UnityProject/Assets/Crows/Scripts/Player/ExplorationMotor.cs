using DungeonsCrows.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonsCrows.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ExplorationMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform viewReference;
        [SerializeField] private DualPerspectiveCamera perspectiveCamera;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.8f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float rotationSharpness = 14f;

        [Header("Vertical")]
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float jumpHeight = 1.15f;

        private CharacterController _controller;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (viewReference == null && Camera.main != null)
                viewReference = Camera.main.transform;
        }

        private void Update()
        {
            Vector2 input = ReadMovement();
            bool sprint = ReadSprint();
            bool jump = ReadJump();

            Transform basis = viewReference != null ? viewReference : transform;
            Vector3 forward = Vector3.ProjectOnPlane(basis.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(basis.right, Vector3.up).normalized;
            Vector3 desiredDirection = forward * input.y + right * input.x;
            if (desiredDirection.sqrMagnitude > 1f)
                desiredDirection.Normalize();

            float targetSpeed = sprint ? sprintSpeed : walkSpeed;
            Vector3 desiredVelocity = desiredDirection * targetSpeed;
            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity,
                desiredVelocity,
                acceleration * Time.deltaTime);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                if (jump)
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 displacement =
                (_planarVelocity + Vector3.up * _verticalVelocity) *
                Time.deltaTime;
            _controller.Move(displacement);

            UpdateFacing(desiredDirection, basis);
        }

        public void SetViewReference(Transform reference)
        {
            viewReference = reference;
        }

        public void SetPerspectiveCamera(DualPerspectiveCamera cameraRig)
        {
            perspectiveCamera = cameraRig;
        }

        private void UpdateFacing(Vector3 desiredDirection, Transform basis)
        {
            Vector3 facing = desiredDirection;

            if (perspectiveCamera != null &&
                perspectiveCamera.Mode == PerspectiveMode.FirstPerson)
            {
                facing = Vector3.ProjectOnPlane(basis.forward, Vector3.up);
            }

            if (facing.sqrMagnitude < 0.001f) return;

            Quaternion desiredRotation =
                Quaternion.LookRotation(facing.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }

        private static Vector2 ReadMovement()
        {
            Vector2 value = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) value.y += 1f;
                if (Keyboard.current.sKey.isPressed) value.y -= 1f;
                if (Keyboard.current.dKey.isPressed) value.x += 1f;
                if (Keyboard.current.aKey.isPressed) value.x -= 1f;
            }

            if (Gamepad.current != null)
                value += Gamepad.current.leftStick.ReadValue();

            return Vector2.ClampMagnitude(value, 1f);
        }

        private static bool ReadSprint()
        {
            bool keyboard =
                Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed);
            bool gamepad =
                Gamepad.current != null &&
                Gamepad.current.leftStickButton.isPressed;
            return keyboard || gamepad;
        }

        private static bool ReadJump()
        {
            bool keyboard =
                Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame;
            bool gamepad =
                Gamepad.current != null &&
                Gamepad.current.buttonSouth.wasPressedThisFrame;
            return keyboard || gamepad;
        }
    }
}
