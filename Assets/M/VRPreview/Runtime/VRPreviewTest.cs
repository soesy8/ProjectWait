using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Unity.XR.CoreUtils;
using ProjectWait.M.Elevator.DialoguePreview;
#endif

namespace ProjectWait.M.VRPreview
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(30000)]
    [AddComponentMenu("Wait/Temporary VR Preview")]
    public sealed class VRPreviewTest : MonoBehaviour
    {

        public bool showHelp = true;
        public bool showBothEyes = true;
        [Header("Temporary world reading layout")]
        [Range(.9f, 1.6f)] public float worldReadingDistance = 1.35f;
        [Range(.0012f, .002f)] public float worldReadingScale = .0014f;
        public bool showLetterbox;
        [Range(.08f, .2f)] public float letterboxHeight = .12f;
        [Range(20, 36)] public float captionFontSize = 26;
        [Range(24, 40)] public float dialogueFontSize = 32;
        [Range(24, 36)] public float choiceFontSize = 30;
        public bool Running { get; private set; }
        public string Status { get; private set; } = "Editor-only preview. Press Play.";

#if UNITY_EDITOR
        XRLoader loader;
        XRDisplaySubsystem display;
        GameObject session;
        ElevatorDesktopControls controls;
        VRPreviewInput simulator;
        bool controlsEnabled, cursorVisible, captured;
        CursorLockMode cursorLock;
        Transform cameraOffset;
        Vector3 offsetPosition, cameraPosition;
        Quaternion offsetRotation, cameraRotation, rigRotation;
        Vector3 offsetScale;
        TrackedPoseDriver poseDriver;
        bool poseEnabled;
        readonly List<DriverState> drivers = new List<DriverState>();

        readonly List<InputAction> temporaryPoseActions = new List<InputAction>();
        readonly Dictionary<GameObject, bool> controllerStates = new Dictionary<GameObject, bool>();
        Coroutine boot;
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        struct DriverState
        {
            public Transform transform;
            public Vector3 position;
            public Quaternion rotation;
            public bool enabled;
            public TrackedPoseDriver driver;
            public InputActionProperty positionInput, rotationInput, trackingStateInput;
        }

        void OnEnable()
        {
            if (Application.isPlaying) boot = StartCoroutine(BeginAfterStart());
        }

        IEnumerator BeginAfterStart()
        {
            // XR manual initialization requires the scene's Start/graphics initialization to finish.
            yield return null;
            boot = null;
            try { Begin(); }
            catch (Exception ex)
            {
                End(); Status = "Preview failed: " + ex.Message;
                Debug.LogError("[Wait VR Preview] " + ex, this);
            }
        }

        static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);

        static void RefreshControls(ElevatorDesktopControls value)
        {
            if (value) typeof(ElevatorDesktopControls).GetMethod("RefreshMode", Hidden).Invoke(value, null);
        }

        void Begin()
        {
            var displays = new List<XRDisplaySubsystem>(); SubsystemManager.GetSubsystems(displays);
            if (displays.Any(d => d.running)) throw new InvalidOperationException("An XR display is already running. Stop the physical headset session first.");

            var loaderType = FindType("Unity.XR.MockHMD.MockHMDLoader");
            if (loaderType == null) throw new InvalidOperationException("Mock HMD package is not installed. The ordinary PC scene is unchanged.");
            controls = FindFirstObjectByType<ElevatorDesktopControls>();
            if (!controls || !controls.targetCamera) throw new InvalidOperationException("The main lesson camera is missing.");
            var origin = controls.xrOrigin.GetComponent<XROrigin>();
            var modality = FindFirstObjectByType<XRInputModalityManager>();
            if (!origin || !modality || !modality.leftController || !modality.rightController)
                throw new InvalidOperationException("The existing XR Origin/controller references are missing.");

            cursorLock = Cursor.lockState; cursorVisible = Cursor.visible;

            controlsEnabled = controls.enabled;
            cameraPosition = controls.targetCamera.transform.localPosition;
            cameraRotation = controls.targetCamera.transform.localRotation;
            rigRotation = controls.xrOrigin.localRotation;
            poseDriver = controls.targetCamera.GetComponent<TrackedPoseDriver>();
            poseEnabled = poseDriver && poseDriver.enabled;
            cameraOffset = origin.CameraFloorOffsetObject.transform;
            offsetPosition = cameraOffset.localPosition; offsetRotation = cameraOffset.localRotation; offsetScale = cameraOffset.localScale;
            drivers.Clear(); controllerStates.Clear();
            foreach (var driver in controls.xrOrigin.GetComponentsInChildren<TrackedPoseDriver>(true))
                drivers.Add(new DriverState { transform = driver.transform, position = driver.transform.localPosition,
                    rotation = driver.transform.localRotation, enabled = driver.enabled, driver = driver,
                    positionInput = driver.positionInput, rotationInput = driver.rotationInput, trackingStateInput = driver.trackingStateInput });
            foreach (var go in new[] { modality.leftController, modality.rightController, modality.leftHand, modality.rightHand }.Where(g => g))
                controllerStates[go] = go.activeSelf;
            captured = true;

            // An independent, non-persistent loader: no project XR provider list is modified.
            loader = (XRLoader)ScriptableObject.CreateInstance(loaderType); loader.hideFlags = HideFlags.HideAndDontSave;
            if (!loader.Initialize()) throw new InvalidOperationException("Mock HMD initialization failed.");
            var api = FindType("Unity.XR.MockHMD.MockHMD");
            var renderMethod = api.GetMethod("SetRenderMode");
            renderMethod.Invoke(null, new[] { Enum.Parse(renderMethod.GetParameters()[0].ParameterType, "SinglePassInstanced") });
            api.GetMethod("SetEyeResolution").Invoke(null, new object[] { 1200, 1200 });
            if (!loader.Start()) throw new InvalidOperationException("Mock HMD did not start.");
            display = loader.GetLoadedSubsystem<XRDisplaySubsystem>();
            if (display == null || !display.running) throw new InvalidOperationException("No running Mock HMD display was created.");

            float heading = controls.targetCamera.transform.eulerAngles.y;
            RefreshControls(controls);
            // The production component reads legacy XR.InputDevices for wear/height. Its source is untouched;
            // only this session substitutes Input System tracking and an explicitly simulated worn state.
            controls.enabled = false;
            controls.xrOrigin.rotation = Quaternion.Euler(0, heading, 0);
            controls.targetCamera.transform.localRotation = Quaternion.identity;
            if (poseDriver) poseDriver.enabled = true;

            session = new GameObject("Temporary XR session"); session.transform.SetParent(transform, false); session.SetActive(false);
            simulator = session.AddComponent<VRPreviewInput>();
            session.AddComponent<VRPreviewFraming>().Initialize(this, controls.targetCamera, display);
            simulator.LeftController = modality.leftController.transform;
            simulator.RightController = modality.rightController.transform;
            BindSimulatedPose(poseDriver, "<XRSimulatedHMD>", "centerEyePosition", "centerEyeRotation");
            BindSimulatedPose(modality.leftController.GetComponent<TrackedPoseDriver>(), "<XRSimulatedController>{LeftHand}", "devicePosition", "deviceRotation");
            BindSimulatedPose(modality.rightController.GetComponent<TrackedPoseDriver>(), "<XRSimulatedController>{RightHand}", "devicePosition", "deviceRotation");
            session.SetActive(true);
            Running = true; Status = "Mock HMD + queued simulated controllers | Both eyes | simulated wear";
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void BindSimulatedPose(TrackedPoseDriver driver, string device, string position, string rotation)
        {
            if (!driver) throw new InvalidOperationException("A tracked pose driver is missing.");
            driver.enabled = false;
            var p = new InputAction("Wait preview position", InputActionType.PassThrough, device + "/" + position);
            var r = new InputAction("Wait preview rotation", InputActionType.PassThrough, device + "/" + rotation);
            var t = new InputAction("Wait preview tracking", InputActionType.Value, device + "/trackingState");
            temporaryPoseActions.Add(p); temporaryPoseActions.Add(r); temporaryPoseActions.Add(t);
            driver.positionInput = new InputActionProperty(p); driver.rotationInput = new InputActionProperty(r);
            driver.trackingStateInput = new InputActionProperty(t); driver.enabled = true;
        }

        void LateUpdate()
        {
            if (!Running || !controls) return;
            var hmd = InputSystem.devices.OfType<XRSimulatedHMD>().FirstOrDefault();
            if (hmd != null) controls.ObserveXRHeight(hmd.isTracked.isPressed, true, Time.unscaledDeltaTime);
            if (display != null && display.running)
                display.SetPreferredMirrorBlitMode(showBothEyes ? XRMirrorViewBlitMode.SideBySide : XRMirrorViewBlitMode.LeftEye);
        }

        void OnDisable()
        {
            if (boot != null) { StopCoroutine(boot); boot = null; }
            End();
        }

        void End()
        {
            Running = false;
            if (session) { session.SetActive(false); DestroyImmediate(session); session = null; }

            simulator = null;
            if (loader) { loader.Stop(); loader.Deinitialize(); DestroyImmediate(loader); loader = null; }
            display = null;
            if (captured)
            {
                if (cameraOffset) { cameraOffset.localPosition = offsetPosition; cameraOffset.localRotation = offsetRotation; cameraOffset.localScale = offsetScale; }
                foreach (var state in drivers)
                    if (state.driver)
                    {
                        state.driver.enabled = false;
                        state.driver.positionInput = state.positionInput; state.driver.rotationInput = state.rotationInput;
                        state.driver.trackingStateInput = state.trackingStateInput;
                        state.transform.localPosition = state.position; state.transform.localRotation = state.rotation;
                        state.driver.enabled = state.enabled;
                    }
                foreach (var pair in controllerStates) if (pair.Key) pair.Key.SetActive(pair.Value);
                if (controls)
                {
                    controls.xrOrigin.localRotation = rigRotation;
                    controls.targetCamera.transform.localPosition = cameraPosition;
                    controls.targetCamera.transform.localRotation = cameraRotation;
                    if (poseDriver) poseDriver.enabled = poseEnabled;
                    controls.enabled = controlsEnabled; RefreshControls(controls);
                    // A same-aspect XR -> PC switch otherwise keeps the world-space HUD.
                    if (controls.lesson && controls.lesson.Flow != null && controls.lesson.view && controls.lesson.view.spatial)
                        controls.lesson.view.spatial.Layout();
                }
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.lockState = cursorLock; Cursor.visible = cursorVisible;
                captured = false;
            }
            drivers.Clear(); controllerStates.Clear();
            foreach (var action in temporaryPoseActions) action.Dispose();
            temporaryPoseActions.Clear();
            Status = "Preview stopped. Ordinary PC controls restored.";
        }

        void OnGUI()
        {
            if (!showHelp || !Application.isPlaying) return;
            GUI.Box(new Rect(12, 12, 650, 98), "TEMPORARY VR PREVIEW - EDITOR ONLY");
            GUI.Label(new Rect(24, 35, 550, 23), Running ? "Mouse: look | WASD: move | Tab: device mode | [ / ]: choose hand" : Status);
            GUI.Label(new Rect(24, 57, 550, 23), "T: trigger | G: grip | Esc: menu | R: reset | IJKL: left stick | Mode: " + (simulator ? simulator.Mode.ToString() : "off"));
            GUI.Label(new Rect(24, 79, 620, 23), "Dialogue follows story stages | Esc: pause and reread");
        }
#endif
    }
}
