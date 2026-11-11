#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace ProjectWait.M.VRPreview
{
    // Queue full state events rather than writing InputState.Change in Update. This keeps
    // InputAction performed/canceled events and TrackedPoseDriver caches in sync in this project.
    [DefaultExecutionOrder(-10000)]
    public sealed class VRPreviewInput : MonoBehaviour
    {
        public enum ControlMode { Head, LeftHand, RightHand }
        public ControlMode Mode { get; private set; }
        public Keyboard KeyboardForTesting { get; set; }
        public bool IgnoreMouseForTesting { get; set; }
        public Transform LeftController { get; set; }
        public Transform RightController { get; set; }
        public XRSimulatedHMD Head { get; private set; }
        public XRSimulatedController Left { get; private set; }
        public XRSimulatedController Right { get; private set; }
        XRSimulatedHMDState headState;
        XRSimulatedControllerState leftState, rightState;
        Vector2 headAngles, leftAngles, rightAngles;

        void OnEnable()
        {
            InputSystem.RegisterLayout<XRSimulatedHMD>();
            InputSystem.RegisterLayout<XRSimulatedController>();
            Head = InputSystem.AddDevice<XRSimulatedHMD>("WaitPreviewHead");
            Left = InputSystem.AddDevice<XRSimulatedController>("WaitPreviewLeft");
            Right = InputSystem.AddDevice<XRSimulatedController>("WaitPreviewRight");
            InputSystem.SetDeviceUsage(Left, UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(Right, UnityEngine.InputSystem.CommonUsages.RightHand);
            ResetPoses(); InputSystem.onBeforeUpdate += QueueBeforeDynamicInput;
        }

        public void ResetPoses()
        {
            headState.Reset(); leftState.Reset(); rightState.Reset();
            headAngles = leftAngles = rightAngles = Vector2.zero;
            leftState.devicePosition = new Vector3(-.20f, -.10f, .35f);
            rightState.devicePosition = new Vector3(.20f, -.10f, .35f);
            Mode = ControlMode.Head;
        }

        public void SetControllerPose(bool left, Vector3 localPosition, Quaternion localRotation)
        {
            Vector3 angles = localRotation.eulerAngles;
            if (left)
            {
                leftState.devicePosition = localPosition; leftState.deviceRotation = localRotation;
                leftAngles = new Vector2(angles.y, angles.x > 180 ? angles.x - 360 : angles.x); Mode = ControlMode.LeftHand;
            }
            else
            {
                rightState.devicePosition = localPosition; rightState.deviceRotation = localRotation;
                rightAngles = new Vector2(angles.y, angles.x > 180 ? angles.x - 360 : angles.x); Mode = ControlMode.RightHand;
            }
        }

        void Update()
        {
            var keyboard = KeyboardForTesting ?? Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tabKey.wasPressedThisFrame) Mode = (ControlMode)(((int)Mode + 1) % 3);
            if (keyboard.leftBracketKey.wasPressedThisFrame) Mode = ControlMode.LeftHand;
            if (keyboard.rightBracketKey.wasPressedThisFrame) Mode = ControlMode.RightHand;
            if (keyboard.rKey.wasPressedThisFrame) ResetPoses();
            Vector2 mouse = IgnoreMouseForTesting || Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * .12f;
            Vector3 move = new Vector3((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            if (Mode == ControlMode.Head)
            {
                headAngles += new Vector2(mouse.x, -mouse.y); headAngles.y = Mathf.Clamp(headAngles.y, -75, 75);
                headState.centerEyeRotation = headState.deviceRotation = Quaternion.Euler(headAngles.y, headAngles.x, 0);
                headState.centerEyePosition += Quaternion.Euler(0, headAngles.x, 0) * move * (1.25f * Time.deltaTime);
                headState.devicePosition = headState.centerEyePosition;
                leftState.devicePosition = headState.centerEyePosition + headState.centerEyeRotation * new Vector3(-.20f, -.10f, .35f);
                rightState.devicePosition = headState.centerEyePosition + headState.centerEyeRotation * new Vector3(.20f, -.10f, .35f);
                leftState.deviceRotation = rightState.deviceRotation = headState.centerEyeRotation;
            }
            else if (Mode == ControlMode.LeftHand)
            {
                leftAngles += new Vector2(mouse.x, -mouse.y); leftAngles.y = Mathf.Clamp(leftAngles.y, -85, 85);
                leftState.deviceRotation = Quaternion.Euler(leftAngles.y, leftAngles.x, 0);
                leftState.devicePosition += Quaternion.Euler(0, headAngles.x, 0) * move * (.6f * Time.deltaTime);
            }
            else
            {
                rightAngles += new Vector2(mouse.x, -mouse.y); rightAngles.y = Mathf.Clamp(rightAngles.y, -85, 85);
                rightState.deviceRotation = Quaternion.Euler(rightAngles.y, rightAngles.x, 0);
                rightState.devicePosition += Quaternion.Euler(0, headAngles.x, 0) * move * (.6f * Time.deltaTime);
            }
            bool useLeft = Mode == ControlMode.LeftHand;
            leftState.buttons = rightState.buttons = 0;
            leftState.trigger = rightState.trigger = leftState.grip = rightState.grip = 0;
            if (useLeft)
            {
                leftState.trigger = keyboard.tKey.isPressed ? 1 : 0; leftState.grip = keyboard.gKey.isPressed ? 1 : 0;
                leftState.WithButton(ControllerButton.TriggerButton, keyboard.tKey.isPressed); leftState.WithButton(ControllerButton.GripButton, keyboard.gKey.isPressed);
            }
            else
            {
                rightState.trigger = keyboard.tKey.isPressed ? 1 : 0; rightState.grip = keyboard.gKey.isPressed ? 1 : 0;
                rightState.WithButton(ControllerButton.TriggerButton, keyboard.tKey.isPressed); rightState.WithButton(ControllerButton.GripButton, keyboard.gKey.isPressed);
            }
            leftState.primary2DAxis = new Vector2((keyboard.lKey.isPressed ? 1 : 0) - (keyboard.jKey.isPressed ? 1 : 0),
                (keyboard.iKey.isPressed ? 1 : 0) - (keyboard.kKey.isPressed ? 1 : 0));
        }

        void QueueBeforeDynamicInput()
        {
            // XR devices also update BeforeRender. Feeding buttons there misses the existing
            // XRI StateChange/WasPerformedThisFrame gate, so publish only before Dynamic input.
            if (InputState.currentUpdateType == InputUpdateType.Dynamic) Queue();
        }

        void Queue()
        {
            headState.isTracked = leftState.isTracked = rightState.isTracked = true;
            headState.trackingState = leftState.trackingState = rightState.trackingState = 3;
            InputSystem.QueueStateEvent(Head, headState);
            InputSystem.QueueStateEvent(Left, leftState);
            InputSystem.QueueStateEvent(Right, rightState);
        }

        void OnDisable()
        {
            InputSystem.onBeforeUpdate -= QueueBeforeDynamicInput;
            if (Head != null && Head.added) InputSystem.RemoveDevice(Head);
            if (Left != null && Left.added) InputSystem.RemoveDevice(Left);
            if (Right != null && Right.added) InputSystem.RemoveDevice(Right);
            Head = null; Left = Right = null;
        }
    }
}
#endif
