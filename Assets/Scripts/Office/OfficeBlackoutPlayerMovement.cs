using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using ProjectWait.M.Shared;

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
        [SerializeField, Range(30f, 89f)] float desktopPitchLimit = 80f;

        [Header("Movement input")]
        [Tooltip("Optional XRI Left Locomotion / Move action. Keyboard WASD is always available on desktop.")]
        [SerializeField] InputActionReference xrMoveAction;
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
        UnityEngine.InputSystem.XR.TrackedPoseDriver poseDriver;
        bool originalPoseEnabled, lastXR;
        Unity.XR.CoreUtils.XROrigin origin;
        bool snapHeld;
        float xrStableSeconds, previousXRHeight;
        public bool SpawnReady { get; private set; }

        public Vector2 LastInput { get; private set; }
        public bool CanMove => IsMovementStepAllowed();
        public bool IsMovementManuallyEnabled => manualMovementEnabled;
        public float FloorHeight => transform.position.y;
        public Vector3 FeetPosition => new Vector3(playerCamera.transform.position.x, FloorHeight, playerCamera.transform.position.z);

        void Reset()
        {
            characterController = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            characterController = GetComponent<CharacterController>();
            if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
            origin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
            poseDriver = playerCamera ? playerCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>() : null;
            originalPoseEnabled = poseDriver && poseDriver.enabled;
            lastXR = XRSettings.isDeviceActive;
            if (poseDriver && !lastXR) poseDriver.enabled = false;
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
            if (!XRSettings.isDeviceActive && origin && origin.CameraFloorOffsetObject)
            {
                // B2 saved a negative desktop eye offset below its old camera floor.
                // Normalize only the untracked desktop camera before shared calibration.
                playerCamera.transform.localPosition = Vector3.zero;
                var offset = origin.CameraFloorOffsetObject.transform.localPosition;
                offset.y = ExperiencePreferences.EyeHeight; origin.CameraFloorOffsetObject.transform.localPosition = offset;
            }
            SpawnReady = !XRSettings.isDeviceActive;
        }

        void Update()
        {
            bool xr = XRSettings.isDeviceActive;
            if (xr != lastXR) { lastXR = xr; SpawnReady = !xr; xrStableSeconds = 0; if (poseDriver) poseDriver.enabled = xr && originalPoseEnabled; SyncCursor(); }
            if (xr && !SpawnReady) UpdateXRCalibration();
            if (!xr && enableDesktopMouseLook && !OfficeStoryTime.Frozen &&
                !(OfficeSceneIntegration.Current && (OfficeSceneIntegration.Current.IsCompleted || OfficeSceneIntegration.Current.EscortMoving)))
                UpdateDesktopLook();
            if (OfficeSceneIntegration.GameplayBlocked) { LastInput = Vector2.zero; return; }
            if (!characterController || !characterController.enabled || !playerCamera)
            {
                LastInput = Vector2.zero;
                verticalSpeed = 0f;
                return;
            }

            UpdateControllerCenter();

            bool usingXR = XRSettings.isDeviceActive;

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
            float speed = ExperiencePreferences.MoveSpeed;
            Vector3 horizontal = (forward * LastInput.y + right * LastInput.x) * speed;

            characterController.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
            UpdateControllerCenter();
            if (usingXR) UpdateSnapTurn();
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
            if (OfficeSceneIntegration.GameplayBlocked) return false;
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
            characterController.height = Mathf.Clamp(headLocal.y + .08f, .65f, 2.4f);
            characterController.center = new Vector3(headLocal.x, characterController.height * 0.5f, headLocal.z);
        }

        void UpdateDesktopLook()
        {
            // A free cursor selects readable cards; RMB-drag looks around on desktop.
            if (Mouse.current == null || !Mouse.current.rightButton.isPressed) return;

            Vector2 look = Mouse.current.delta.ReadValue() * ExperiencePreferences.LookSensitivity;
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SyncCursor()
        {
            bool ui = OfficeSceneIntegration.Current && (OfficeSceneIntegration.Current.IsPaused || OfficeSceneIntegration.Current.IsCompleted);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !XRSettings.isDeviceActive;
        }
        public void RequestHeightCalibration()
        {
            if (!XRSettings.isDeviceActive || !origin || !origin.CameraFloorOffsetObject) return;
            var offset = origin.CameraFloorOffsetObject.transform;
            offset.position += Vector3.up * (ExperiencePreferences.EyeHeight - (playerCamera.transform.position.y - transform.position.y));
            SpawnReady = true; UpdateControllerCenter();
        }
        void UpdateXRCalibration()
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!device.isValid) device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) || !tracked ||
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out bool worn) && !worn)
            { xrStableSeconds = 0; return; }
            float height = playerCamera.transform.position.y - transform.position.y;
            if (height < .2f || Mathf.Abs(height - previousXRHeight) > .025f) xrStableSeconds = 0;
            else xrStableSeconds += Mathf.Min(Time.unscaledDeltaTime, .1f);
            previousXRHeight = height;
            if (xrStableSeconds >= .4f) RequestHeightCalibration();
        }
        public void MoveGuided(Vector3 delta)
        { if (!OfficeStoryTime.Frozen && characterController && characterController.enabled) characterController.Move(delta); }
        public void RelocateFeet(Vector3 point)
        {
            bool enabled = characterController.enabled; characterController.enabled = false;
            // Retain the tracked head offset and orientation. Only the origin's feet move.
            Vector3 horizontalHead = Vector3.ProjectOnPlane(playerCamera.transform.position - transform.position, Vector3.up);
            transform.position = point - horizontalHead; verticalSpeed = 0;
            characterController.enabled = enabled; UpdateControllerCenter();
        }
        public void ApplyTransitionHeadPose(ProjectWait.M.SceneTransition.SceneTransitionHeadPose pose, Vector3 footprint)
        {
            bool active = characterController.enabled; characterController.enabled = false;
            pose.Apply(transform, playerCamera.transform, FloorHeight, XRSettings.isDeviceActive, footprint);
            if (!XRSettings.isDeviceActive)
            {
                transform.rotation = Quaternion.Euler(0, pose.EyeRotation.eulerAngles.y, 0);
                desktopPitch = Mathf.DeltaAngle(0, pose.EyeRotation.eulerAngles.x);
                if (desktopPitchPivot) desktopPitchPivot.localRotation = Quaternion.Euler(desktopPitch, 0, 0);
                playerCamera.transform.localRotation = Quaternion.identity;
            }
            characterController.enabled = active; UpdateControllerCenter();
        }
        void UpdateSnapTurn()
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 axis);
            if (Mathf.Abs(axis.x) < .3f) snapHeld = false;
            if (!snapHeld && Mathf.Abs(axis.x) > .7f)
            { snapHeld = true; transform.RotateAround(playerCamera.transform.position, Vector3.up, Mathf.Sign(axis.x) * 30); }
        }

        void OnDisable()
        {
            if (poseDriver) poseDriver.enabled = originalPoseEnabled;
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
