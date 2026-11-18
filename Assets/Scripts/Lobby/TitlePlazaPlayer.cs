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
        public TitlePauseMenu pauseMenu;
        public TMP_Text messageText;
        public Canvas desktopHints;
        public LineRenderer controllerRay;
        public GameObject leftControllerVisual,rightControllerVisual;
        public Vector2 walkMin = new Vector2(-4.8f, -2.9f);
        public Vector2 walkMax = new Vector2(4.8f, .9f);
        public float eyeHeight = 1.35f;
        public float speed = 1.65f;
        public float lookSensitivity = .105f;
        public float interactionDistance = 8f;
        public bool IsVR { get; private set; }
        public int TickedFrames { get; private set; }
        public string HoveredExperience => hovered ? hovered.experienceTitle : "";
        public bool InteractionBlocked => StorybookSceneTransition.IsBusy||pauseMenu&&pauseMenu.IsPaused||menuClickBlocked;
        public CharacterController Controller { get; private set; }
        public float FeetOffset { get; private set; }
        public float FloorHeight => transform.position.y + FeetOffset;
        public string LastMessage { get; private set; } = "";
        TitlePortal hovered;
        Vector3 safeHead;
        bool hasSafeHead, oldTrigger, skipMouse = true, modeConfigured;
        bool transitionHudHidden, hintsBeforeTransition, rayBeforeTransition;
        float yaw, pitch, verticalSpeed, messageUntil, turnCooldown;
        float spawnYaw;
        bool alignFirstTrackedHeading = true;
        readonly RaycastHit[] headHits = new RaycastHit[16];
        readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
        InputAction aimPose, selectTrigger;
        bool hadValidAim;
        bool offeredHintThisVisit;
        static int guidedVisits;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGuidedVisits() => guidedVisits = 0;
        bool menuClickBlocked;
        float menuInputReadyAt;
        TitleLobbyControllerVisuals controllerVisuals;
        LobbyControllerRay sharedRay;
        readonly ProjectWait.M.Elevator.DialoguePreview.ElevatorXRHeightCalibration heightCalibration=new ProjectWait.M.Elevator.DialoguePreview.ElevatorXRHeightCalibration();
        float trackedHeightOffset;
        int heightFrame=-1;
        public void RequestHeightCalibration(){heightCalibration.Request(true);}
        public const string HeightPrompt="눈높이를 맞추려면 트리거를 누른 뒤\n잠시 가만히 있어 주세요.";
        void TrackingOriginChanged(XRInputSubsystem subsystem){heightCalibration.Request();}

        void Awake() { Controller = GetComponent<CharacterController>(); spawnYaw = transform.eulerAngles.y; }
        void OnEnable()
        {
            Application.onBeforeRender += ApplyTrackedHead;
            // OpenXR's pointer pose is /input/aim/pose, not /input/grip/pose.
            aimPose = new InputAction("LobbyRightAim", InputActionType.Value, "<XRController>{RightHand}/pointer", expectedControlType: "Pose");
            selectTrigger = new InputAction("LobbySelect", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            selectTrigger.AddBinding("<XRController>{RightHand}/trigger");
            aimPose.Enable(); selectTrigger.Enable();
            hadValidAim = oldTrigger = false;
            modeConfigured = false;
        }
        void Start()
        {
            yaw = transform.eulerAngles.y;
            RefreshMode();
            if (!IsVR) SetMouseLock(true);
        }
        void RefreshMode()
        {
            eyeHeight=ProjectWait.M.Shared.ExperiencePreferences.EyeHeight;lookSensitivity=ProjectWait.M.Shared.ExperiencePreferences.LookSensitivity;
            bool vr = XRSettings.isDeviceActive && (XRSettings.loadedDeviceName ?? "").IndexOf("mock", System.StringComparison.OrdinalIgnoreCase) < 0;
            if (modeConfigured && vr == IsVR) return;
            modeConfigured = true;
            IsVR = vr;
            hadValidAim = oldTrigger = false;
            // URP owns stereo rendering. Its legacy camera property is not a reliable mode latch.
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
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
                if (controllerVisuals == null) controllerVisuals = new TitleLobbyControllerVisuals(transform,controllerRay,leftControllerVisual,rightControllerVisual,aimPose);
                if(sharedRay==null&&controllerRay&&ProjectWait.M.Shared.StoryMusic.Current)
                {var style=ProjectWait.M.Shared.StoryMusic.Current.GetComponent<ProjectWait.M.Shared.SharedControllerRays>();if(style)sharedRay=new LobbyControllerRay(transform,controllerRay,style);}
                SetMouseLock(false);
                SubsystemManager.GetSubsystems(inputSubsystems);
                foreach (var subsystem in inputSubsystems) if (subsystem.running) {subsystem.trackingOriginUpdated-=TrackingOriginChanged;subsystem.trackingOriginUpdated+=TrackingOriginChanged;subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);}
            }
            hasSafeHead = false;
            controllerVisuals?.Update(vr && !transitionHudHidden,trackedHeightOffset);
        }
        void Update()
        {
            TickedFrames++;
            RefreshMode();
            SetTransitionHudHidden(StorybookSceneTransition.IsBusy);
            var keyboard = Keyboard.current;
            if(pauseMenu&&pauseMenu.IsPaused){if(IsVR)ApplyTrackedHead();return;}
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
                hadValidAim = false;
                oldTrigger = selectTrigger != null && selectTrigger.IsPressed();
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
        public void ApplyTransitionHeadPose(SceneTransitionHeadPose pose, Vector3 footprint)
        {
            if (!viewCamera) return;
            float floor = FloorHeight;
            bool enabledController = Controller && Controller.enabled;
            if (enabledController) Controller.enabled = false;
            if (IsVR)
            {
                FaceLobbyForward();
                alignFirstTrackedHeading = false;
            }
            else
            {
                yaw = spawnYaw; pitch = 0;
                transform.rotation = Quaternion.Euler(0, yaw, 0);
                viewCamera.transform.localRotation = Quaternion.identity;
            }
            if (pose.IsValid)
            {
                // Carry over the calibrated height and footprint, not the departing gaze.
                eyeHeight = pose.EyeHeight;
                transform.position += new Vector3(footprint.x, floor + eyeHeight, footprint.z) - viewCamera.transform.position;
                if (IsVR) heightCalibration.AcceptTransferredHeight();
            }
            FeetOffset = floor - transform.position.y;
            if (Controller) Controller.center = new Vector3(0, Controller.height * .5f + FeetOffset, 0);
            verticalSpeed = 0; hasSafeHead = false; skipMouse = true;
            if (enabledController) Controller.enabled = true;
        }
        void FaceLobbyForward()
        {
            // Turn around the tracked eye so room-scale displacement, pitch and roll stay physical.
            var eye = viewCamera.transform;
            transform.RotateAround(eye.position, Vector3.up, Mathf.DeltaAngle(eye.eulerAngles.y, spawnYaw));
        }
        public void Move(Vector2 input, float seconds)
        {
            if (StorybookSceneTransition.IsBusy || pauseMenu&&pauseMenu.IsPaused || !Controller || !Controller.enabled || !viewCamera || seconds <= 0) return;
            input = Vector2.ClampMagnitude(input, 1);
            if (input.magnitude < .18f) input = Vector2.zero;
            var forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            verticalSpeed = Controller.isGrounded ? -1.5f : Mathf.Max(-12, verticalSpeed - 18 * seconds);
            Controller.Move(((forward * input.y + right * input.x) * ProjectWait.M.Shared.ExperiencePreferences.MoveSpeed + Vector3.up * verticalSpeed) * seconds);
            ConstrainToPlaza();
        }
        public void ConstrainToPlaza()
        {
            var point = IsVR ? viewCamera.transform.position : transform.position;
            var correction = new Vector3(Mathf.Clamp(point.x, walkMin.x, walkMax.x) - point.x, 0, Mathf.Clamp(point.z, walkMin.y, walkMax.y) - point.z);
            if (FloorHeight < -.15f) correction.y = -FloorHeight;
            if (correction.sqrMagnitude > .000001f)
            { Controller.enabled = false; transform.position += correction; Controller.enabled = true; }
        }
        void ApplyTrackedHead()
        {
            if (!IsVR || !viewCamera) return;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 p))
            {
                if (p.y < .4f) p.y += eyeHeight;
                if(heightFrame!=Time.frameCount)
                {
                    heightFrame=Time.frameCount;bool known=head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence,out bool present);
                    bool tracked=head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool value)&&value;
                    if(heightCalibration.Observe(p.y,tracked,known?(bool?)present:null,Time.unscaledDeltaTime))
                        trackedHeightOffset=FloorHeight+eyeHeight-transform.position.y-p.y;
                }
                p.y+=trackedHeightOffset;viewCamera.transform.localPosition = p;
            }
            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion r))
            {
                viewCamera.transform.localRotation = r;
                if (alignFirstTrackedHeading && !StorybookSceneTransition.IsBusy &&
                    head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool valid) && valid)
                {
                    FaceLobbyForward();
                    alignFirstTrackedHeading = false;
                    hasSafeHead = false;
                }
            }
            controllerVisuals?.Update(!transitionHudHidden,trackedHeightOffset);
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
            Controller.center = new Vector3(local.x, Controller.height * .5f + FeetOffset, local.z);
        }
        void UpdateInteraction()
        {
            if(menuClickBlocked)
            {
                bool held=IsVR?selectTrigger!=null&&selectTrigger.IsPressed():Mouse.current!=null&&Mouse.current.leftButton.isPressed;
                if(Time.unscaledTime<menuInputReadyAt||held)return;
                menuClickBlocked=false;
            }
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            bool pressed = false;
            if (IsVR)
            {
                bool trigger = selectTrigger != null && selectTrigger.IsPressed();
                bool valid = TryGetAimRay(out ray);
                if(heightCalibration.Pending)
                {
                    if(LastMessage!=HeightPrompt)ShowMessage(HeightPrompt);
                    if(valid&&trigger&&!oldTrigger)heightCalibration.Request(true);
                    oldTrigger=trigger;hadValidAim=false;sharedRay?.Hide();if(controllerRay)controllerRay.enabled=false;
                    if(hovered){hovered.SetHovered(false);hovered=null;}return;
                }
                if(LastMessage==HeightPrompt){LastMessage="";if(messageText)messageText.text="";}
                if (controllerRay) controllerRay.enabled = valid && !transitionHudHidden;
                if (!valid)
                {
                    sharedRay?.Hide();
                    controllerVisuals?.ShowPointer(default, false, false);
                    if (hovered) hovered.SetHovered(false);
                    hovered = null; hadValidAim = false; oldTrigger = trigger;
                    return; // Never fall back to the head/grip or keep a stale hover.
                }
                pressed = hadValidAim && trigger && !oldTrigger;
                hadValidAim = true; oldTrigger = trigger;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Cursor.lockState != CursorLockMode.Locked) SetMouseLock(true); else pressed = true;
            }
            if (!offeredHintThisVisit && guidedVisits < 2)
            {
                offeredHintThisVisit = true;
                ++guidedVisits;
                ProjectWait.M.Shared.LookAroundHint.Show(viewCamera);
            }
            var target = TraceInteraction(ray, out var end);
            if (target != hovered) { if (hovered) hovered.SetHovered(false); hovered = target; if (hovered) hovered.SetHovered(true); }
            if(IsVR)sharedRay?.Set(ray,true,end,target,pressed);
            if (IsVR) controllerVisuals?.ShowPointer(end, false, false);
            if (pressed && hovered) hovered.Activate();
        }
        public TitlePortal TraceInteraction(Ray ray, out Vector3 end)
        {
            end = ray.GetPoint(interactionDistance);
            if (!Physics.Raycast(ray, out var hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore)) return null;
            end = hit.point;
            return hit.collider.GetComponentInParent<TitlePortal>();
        }
        public bool TryGetAimRay(out Ray ray)
        {
            ray = default;
            if(!TitleControllerPose.TryRead(aimPose,out var p,out var q))return false;
            ray=new Ray(transform.TransformPoint(p+Vector3.up*trackedHeightOffset),transform.TransformDirection(q*Vector3.forward));return true;
        }
        public void ShowMessage(string message)
        { LastMessage = message; messageUntil = Time.unscaledTime + 3; if (messageText) messageText.text = message; }
        public void SetMenuOpen(bool open)
        {
            if(hovered)hovered.SetHovered(false);hovered=null;
            oldTrigger=true;hadValidAim=false;SetMouseLock(!open&&!IsVR);sharedRay?.Hide();
            if(!open){menuClickBlocked=true;menuInputReadyAt=Time.unscaledTime+.3f;}
        }
        public void ShowMenuRay(Ray ray,bool valid,Vector3 end,bool target,bool pressed)
        {
            if(controllerRay)controllerRay.enabled=valid;
            sharedRay?.Set(ray,valid,end,target,pressed);
            controllerVisuals?.ShowPointer(end,false,false);
        }
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
                sharedRay?.Hide();
                controllerVisuals?.Update(false);
            }
            else
            {
                if (desktopHints) desktopHints.enabled = hintsBeforeTransition;
                if (controllerRay) controllerRay.enabled = rayBeforeTransition;
            }
        }
        void OnDisable()
        {
            Application.onBeforeRender -= ApplyTrackedHead;
            foreach(var subsystem in inputSubsystems)if(subsystem!=null)subsystem.trackingOriginUpdated-=TrackingOriginChanged;
            aimPose?.Dispose(); selectTrigger?.Dispose(); aimPose = selectTrigger = null;
            controllerVisuals?.Dispose(); controllerVisuals = null; modeConfigured = false;
            sharedRay?.Dispose();sharedRay=null;
            hadValidAim = oldTrigger = false;
            SetTransitionHudHidden(false); if (controllerRay) controllerRay.enabled = false;
            if (hovered) hovered.SetHovered(false); hovered = null; SetMouseLock(false);
        }
    }
}
