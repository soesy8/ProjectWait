using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using ProjectWait.M.SceneTransition;

namespace ProjectWait.M.TitleScene
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TitlePlazaPlayer : MonoBehaviour
    {
        public Camera viewCamera;
        public TMP_Text messageText;
        public Canvas desktopHints;
        public LineRenderer controllerRay;
        public Vector2 walkMin = new Vector2(-4.8f, -2.9f);
        public Vector2 walkMax = new Vector2(4.8f, .9f);
        public float eyeHeight = 1.35f;
        public float speed = 1.65f;
        public float lookSensitivity = .105f;
        public float interactionDistance = 3f;
        public bool IsVR { get; private set; }
        public int TickedFrames { get; private set; }
        public string HoveredExperience => hovered ? hovered.experienceTitle : "";
        public CharacterController Controller { get; private set; }
        public string LastMessage { get; private set; } = "";
        TitlePortal hovered;
        Vector3 safeHead;
        bool hasSafeHead, oldTrigger, skipMouse = true, started;
        bool transitionHudHidden, hintsBeforeTransition, rayBeforeTransition;
        float yaw, pitch, verticalSpeed, messageUntil, turnCooldown;
        readonly RaycastHit[] headHits = new RaycastHit[16];
        readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();

        void Awake() { Controller = GetComponent<CharacterController>(); }
        void OnEnable() { Application.onBeforeRender += ApplyTrackedHead; }
        void Start()
        {
            started = true;
            yaw = transform.eulerAngles.y;
            RefreshMode();
            if (!IsVR) SetMouseLock(true);
        }
        void RefreshMode()
        {
            bool vr = XRSettings.isDeviceActive && (XRSettings.loadedDeviceName ?? "").IndexOf("mock", System.StringComparison.OrdinalIgnoreCase) < 0;
            if (started && vr == IsVR && viewCamera.stereoTargetEye == (vr ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None)) return;
            IsVR = vr;
            viewCamera.stereoTargetEye = vr ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None;
            if (desktopHints) desktopHints.gameObject.SetActive(!vr);
            if (controllerRay) controllerRay.gameObject.SetActive(vr);
            if (!vr)
            {
                viewCamera.transform.localPosition = Vector3.up * eyeHeight;
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            else
            {
                SetMouseLock(false);
                SubsystemManager.GetSubsystems(inputSubsystems);
                foreach (var subsystem in inputSubsystems) if (subsystem.running) subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            }
            hasSafeHead = false;
        }
        void Update()
        {
            TickedFrames++;
            RefreshMode();
            SetTransitionHudHidden(StorybookSceneTransition.IsBusy);
            var keyboard = Keyboard.current;
            if (!IsVR && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetMouseLock(Cursor.lockState != CursorLockMode.Locked);
            if (!IsVR && StorybookSceneTransition.IsBusy && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetMouseLock(true);
            Vector2 input = Vector2.zero;
            if (IsVR)
            {
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out input);
                var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 turn);
                turnCooldown -= Time.unscaledDeltaTime;
                if (Mathf.Abs(turn.x) > .7f && turnCooldown <= 0)
                {
                    transform.RotateAround(viewCamera.transform.position, Vector3.up, Mathf.Sign(turn.x) * 30);
                    turnCooldown = .3f;
                }
                ApplyTrackedHead();
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (keyboard != null) input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    var delta = mouse.delta.ReadValue();
                    if (!skipMouse) AddLookDelta(delta);
                    skipMouse = false;
                }
            }
            if (StorybookSceneTransition.IsBusy)
            {
                if (hovered) { hovered.SetHovered(false); hovered = null; }
                // Gaze, tracked head and snap turns above stay live. Only walking/selecting stop.
                return;
            }
            Move(input, Time.deltaTime);
            UpdateInteraction();
            if (messageText && Time.unscaledTime > messageUntil && !string.IsNullOrEmpty(LastMessage))
            { LastMessage = ""; messageText.text = ""; }
        }
        public void AddLookDelta(Vector2 delta)
        {
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -55, 65);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (viewCamera) viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void Move(Vector2 input, float seconds)
        {
            if (StorybookSceneTransition.IsBusy || !Controller || !Controller.enabled || !viewCamera || seconds <= 0) return;
            input = Vector2.ClampMagnitude(input, 1);
            if (input.magnitude < .18f) input = Vector2.zero;
            var forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            verticalSpeed = Controller.isGrounded ? -1.5f : Mathf.Max(-12, verticalSpeed - 18 * seconds);
            Controller.Move(((forward * input.y + right * input.x) * speed + Vector3.up * verticalSpeed) * seconds);
            ConstrainToPlaza();
        }
        public void ConstrainToPlaza()
        {
            var point = IsVR ? viewCamera.transform.position : transform.position;
            var correction = new Vector3(Mathf.Clamp(point.x, walkMin.x, walkMax.x) - point.x, 0, Mathf.Clamp(point.z, walkMin.y, walkMax.y) - point.z);
            if (transform.position.y < -.15f) correction.y = -transform.position.y;
            if (correction.sqrMagnitude > .000001f)
            { Controller.enabled = false; transform.position += correction; Controller.enabled = true; }
        }
        void ApplyTrackedHead()
        {
            if (!IsVR || !viewCamera) return;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 p))
            { if (p.y < .4f) p.y += eyeHeight; viewCamera.transform.localPosition = p; }
            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion r)) viewCamera.transform.localRotation = r;
            var current = viewCamera.transform.position;
            if (hasSafeHead)
            {
                var delta = current - safeHead;
                float length = delta.magnitude;
                if (length > .0001f)
                {
                    int count = Physics.SphereCastNonAlloc(safeHead, .10f, delta / length, headHits, length, ~0, QueryTriggerInteraction.Ignore);
                    float allowed = length;
                    for (int i = 0; i < count; i++) if (headHits[i].collider && !headHits[i].collider.transform.IsChildOf(transform)) allowed = Mathf.Min(allowed, Mathf.Max(0, headHits[i].distance - .01f));
                    if (allowed < length) { Controller.enabled = false; transform.position += delta.normalized * (allowed - length); Controller.enabled = true; }
                }
            }
            ConstrainToPlaza();
            safeHead = viewCamera.transform.position;
            hasSafeHead = true;
            var local = transform.InverseTransformPoint(safeHead);
            Controller.center = new Vector3(local.x, Controller.height * .5f, local.z);
        }
        void UpdateInteraction()
        {
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            bool pressed = false;
            if (IsVR)
            {
                var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 p) && right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion r))
                    ray = new Ray(transform.TransformPoint(p), transform.TransformDirection(r * Vector3.forward));
                right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool trigger);
                pressed = trigger && !oldTrigger; oldTrigger = trigger;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Cursor.lockState != CursorLockMode.Locked) SetMouseLock(true); else pressed = true;
            }
            TitlePortal target = null;
            var end = ray.GetPoint(interactionDistance);
            if (Physics.Raycast(ray, out var hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
            { target = hit.collider.GetComponentInParent<TitlePortal>(); end = hit.point; }
            if (target != hovered) { if (hovered) hovered.SetHovered(false); hovered = target; if (hovered) hovered.SetHovered(true); }
            if (controllerRay && IsVR) { controllerRay.SetPosition(0, ray.origin); controllerRay.SetPosition(1, end); }
            if (pressed && hovered) hovered.Activate();
        }
        public void ShowMessage(string message)
        { LastMessage = message; messageUntil = Time.unscaledTime + 3; if (messageText) messageText.text = message; }
        void SetMouseLock(bool value) { Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !value; skipMouse = true; }
        void OnApplicationFocus(bool focus) { skipMouse = true; }
        void SetTransitionHudHidden(bool hidden)
        {
            if (hidden == transitionHudHidden) return;
            transitionHudHidden = hidden;
            if (hidden)
            {
                hintsBeforeTransition = desktopHints && desktopHints.enabled;
                rayBeforeTransition = controllerRay && controllerRay.enabled;
                if (desktopHints) desktopHints.enabled = false;
                if (controllerRay) controllerRay.enabled = false;
            }
            else
            {
                if (desktopHints) desktopHints.enabled = hintsBeforeTransition;
                if (controllerRay) controllerRay.enabled = rayBeforeTransition;
            }
        }
        void OnDisable()
        { Application.onBeforeRender -= ApplyTrackedHead; SetTransitionHudHidden(false); if (hovered) hovered.SetHovered(false); SetMouseLock(false); }
    }
}
