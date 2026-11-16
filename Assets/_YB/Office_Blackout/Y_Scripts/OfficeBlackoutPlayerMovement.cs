using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// Desktop WASD and XR left-stick locomotion for the office blackout scene.
    /// Add to the same GameObject as the XR Origin and CharacterController.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class OfficeBlackoutPlayerMovement : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] Camera playerCamera;
        [SerializeField] Transform playerStart;
        [SerializeField] bool placeAtPlayerStartOnStart = true;

        [Header("Desktop debug look")]
        [SerializeField] bool enableDesktopMouseLook = true;
        [SerializeField] Transform desktopPitchPivot;
        [SerializeField, Min(0.01f)] float desktopLookSensitivity = 0.12f;
        [SerializeField, Range(30f, 89f)] float desktopPitchLimit = 80f;

        [Header("Movement input")]
        [Tooltip("Optional XRI Left Locomotion / Move action. Keyboard WASD is always available on desktop.")]
        [SerializeField] InputActionReference xrMoveAction;
        [SerializeField, Min(0f)] float desktopSpeed = 2.2f;
        [SerializeField, Min(0f)] float xrSpeed = 1.65f;
        [SerializeField] float gravity = -15f;

        [Header("Allowed story steps")]
        [SerializeField] OfficeBlackoutFlow.Step[] movementSteps =
        {
            OfficeBlackoutFlow.Step.Errand,
            OfficeBlackoutFlow.Step.BlackoutReaction,
            OfficeBlackoutFlow.Step.EscortToExit
        };

        CharacterController characterController;
        InputAction keyboardMove;
        InputAction fallbackXRMove;
        InputAction activeXRMove;
        bool enabledXRActionHere;
        float verticalSpeed;
        float desktopPitch;
        CursorLockMode previousCursorLockMode;
        bool previousCursorVisible;
        bool cursorStateCaptured;

        public Vector2 LastInput { get; private set; }
        public bool CanMove => IsMovementStepAllowed();
        public bool IsMovementManuallyEnabled => manualMovementEnabled;

        void Reset()
        {
            characterController = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            characterController = GetComponent<CharacterController>();
            if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
            if (!desktopPitchPivot && playerCamera && playerCamera.transform.parent)
                desktopPitchPivot = playerCamera.transform.parent;

            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            cursorStateCaptured = true;

            keyboardMove = new InputAction("OfficeBlackoutDesktopMove", InputActionType.Value);
            keyboardMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            keyboardMove.Enable();

            if (xrMoveAction != null && xrMoveAction.action != null)
            {
                activeXRMove = xrMoveAction.action;
                enabledXRActionHere = !activeXRMove.enabled;
                if (enabledXRActionHere) activeXRMove.Enable();
            }
            else
            {
                fallbackXRMove = new InputAction("OfficeBlackoutXRMove", InputActionType.Value);
                fallbackXRMove.AddBinding("<XRController>{LeftHand}/primary2DAxis", processors: "stickDeadzone(min=0.18,max=0.95)");
                fallbackXRMove.Enable();
                activeXRMove = fallbackXRMove;
            }
        }

        void Start()
        {
            if (placeAtPlayerStartOnStart && playerStart)
                PlaceAt(playerStart);

            if (enableDesktopMouseLook && !XRSettings.isDeviceActive)
                LockDesktopCursor();
        }

        void Update()
        {
            if (!characterController || !characterController.enabled || !playerCamera)
            {
                LastInput = Vector2.zero;
                verticalSpeed = 0f;
                return;
            }

            UpdateControllerCenter();

            bool usingXR = XRSettings.isDeviceActive;
            if (!usingXR && enableDesktopMouseLook)
                UpdateDesktopLook();

            Vector2 input = usingXR
                ? activeXRMove != null ? activeXRMove.ReadValue<Vector2>() : Vector2.zero
                : keyboardMove.ReadValue<Vector2>();

            LastInput = CanMove ? Vector2.ClampMagnitude(input, 1f) : Vector2.zero;

            if (characterController.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            else
                verticalSpeed = Mathf.Max(-20f, verticalSpeed + gravity * Time.deltaTime);

            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float speed = usingXR ? xrSpeed : desktopSpeed;
            Vector3 horizontal = (forward * LastInput.y + right * LastInput.x) * speed;

            characterController.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
            UpdateControllerCenter();
        }

        public void PlaceAtPlayerStart()
        {
            if (playerStart) PlaceAt(playerStart);
        }

        public void SetMovementEnabled(bool enabled)
        {
            // Useful for temporary authored sequences; story-step gating still applies.
            if (!enabled) LastInput = Vector2.zero;
            manualMovementEnabled = enabled;
        }

        bool manualMovementEnabled = true;

        bool IsMovementStepAllowed()
        {
            if (!manualMovementEnabled) return false;
            if (!director || director.Flow == null) return false;
            foreach (var step in movementSteps)
                if (director.Flow.CurrentStep == step) return true;
            return false;
        }

        void PlaceAt(Transform marker)
        {
            bool wasEnabled = characterController && characterController.enabled;
            if (wasEnabled) characterController.enabled = false;
            transform.SetPositionAndRotation(marker.position, marker.rotation);
            verticalSpeed = 0f;
            if (wasEnabled) characterController.enabled = true;
            UpdateControllerCenter();
        }

        void UpdateControllerCenter()
        {
            if (!characterController || !playerCamera) return;
            Vector3 headLocal = transform.InverseTransformPoint(playerCamera.transform.position);
            characterController.center = new Vector3(headLocal.x, characterController.height * 0.5f, headLocal.z);
        }

        void UpdateDesktopLook()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    LockDesktopCursor();
                return;
            }

            if (Mouse.current == null) return;

            Vector2 look = Mouse.current.delta.ReadValue() * desktopLookSensitivity;
            if (Mathf.Abs(look.x) > 0.001f)
                transform.Rotate(Vector3.up, look.x, Space.World);

            if (desktopPitchPivot && Mathf.Abs(look.y) > 0.001f)
            {
                desktopPitch = Mathf.Clamp(desktopPitch - look.y, -desktopPitchLimit, desktopPitchLimit);
                desktopPitchPivot.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);
            }
        }

        static void LockDesktopCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            keyboardMove?.Dispose();
            keyboardMove = null;
            if (fallbackXRMove != null)
            {
                fallbackXRMove.Dispose();
                fallbackXRMove = null;
            }
            else if (enabledXRActionHere && activeXRMove != null)
            {
                activeXRMove.Disable();
            }
            activeXRMove = null;
            enabledXRActionHere = false;
            verticalSpeed = 0f;
            LastInput = Vector2.zero;
            if (cursorStateCaptured)
            {
                Cursor.lockState = previousCursorLockMode;
                Cursor.visible = previousCursorVisible;
                cursorStateCaptured = false;
            }
        }
    }
}
