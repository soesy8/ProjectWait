using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ProjectWait.M.Elevator
{
    // Desktop uses a free pointer. Hold the right button to look; XR keeps tracked rays.
    public sealed class ElevatorDesktopControls : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Camera targetCamera;
        public Transform xrOrigin;
        public LineRenderer centerLaser;
        public Transform laserDot;
        public float eyeHeight=1.1064756f, sensitivity=.10f, laserRange=12;
        public bool IsDesktop { get; private set; }
        public bool MouseLookActive { get; private set; }
        public string HoverTargetName=>hovered?hovered.name:"";
        public Vector2 LookAngles=>new Vector2(yaw,pitch);
        public int ClickCount { get; private set; }
        readonly List<UnityEngine.UI.Button> buttons=new List<UnityEngine.UI.Button>();
        GameObject hovered;
        PointerEventData pointer;
        XRUIInputModule uiModule;
        Renderer dotRenderer;
        MaterialPropertyBlock tint;
        float yaw,pitch;
        bool initialized,skipDelta,wasFocused,calibrateXR;
        InputAction[] choices;
        InputAction restartAction,developerAction,clickAction,escapeAction;
        int pendingChoice=-1,pendingRevision,pendingRun,clickRevision,clickRun,viewRevision=-1;
        bool pendingRestart,pendingDeveloper,pendingClick,pendingEscape;
        static readonly Color Idle=new Color(1,.67f,.20f),Target=new Color(.25f,.90f,.72f);

        void OnEnable()
        {
            choices=new InputAction[4];
            for(int i=0;i<4;i++)
            {
                int n=i;choices[i]=new InputAction("ElevatorChoice"+(i+1),InputActionType.Button,"<Keyboard>/"+(i+1));
                choices[i].performed+=_=>{if(pendingChoice<0&&lesson&&lesson.Flow!=null){pendingChoice=n;pendingRevision=lesson.Flow.Revision;pendingRun=lesson.RunVersion;}};choices[i].Enable();
            }
            restartAction=new InputAction("RestartElevator",InputActionType.Button,"<Keyboard>/r");restartAction.performed+=_=>pendingRestart=true;restartAction.Enable();
            developerAction=new InputAction("ElevatorDeveloper",InputActionType.Button,"<Keyboard>/f8");developerAction.performed+=_=>pendingDeveloper=true;developerAction.Enable();
            clickAction=new InputAction("ElevatorCenterClick",InputActionType.Button,"<Mouse>/leftButton");
            clickAction.performed+=_=>{if(lesson&&lesson.Flow!=null){pendingClick=true;clickRevision=lesson.Flow.Revision;clickRun=lesson.RunVersion;}};clickAction.Enable();
            escapeAction=new InputAction("ElevatorCursorToggle",InputActionType.Button,"<Keyboard>/escape");escapeAction.performed+=_=>pendingEscape=true;escapeAction.Enable();
        }
        void Start()
        {
            uiModule=FindFirstObjectByType<XRUIInputModule>();
            dotRenderer=laserDot.GetComponent<Renderer>();tint=new MaterialPropertyBlock();
            wasFocused=Application.isFocused;RefreshMode();
        }
        void RefreshMode()
        {
            bool desktop=!XRSettings.isDeviceActive;if(initialized&&desktop==IsDesktop)return;
            initialized=true;IsDesktop=desktop;ClearHover();
            var pose=targetCamera.GetComponent<TrackedPoseDriver>();if(pose)pose.enabled=!desktop;
            if(uiModule)uiModule.enableMouseInput=!desktop;
            if(desktop)
            {
                targetCamera.transform.position=new Vector3(xrOrigin.position.x,eyeHeight,xrOrigin.position.z);
                SetScriptedHeading(lesson.presentation&&lesson.presentation.space?lesson.presentation.space.DefaultHeading:0);SetMouseLook(false);
            }
            else {SetMouseLook(false);calibrateXR=true;}
            RefreshLaserVisibility();
        }
        void Update()
        {
            RefreshMode();
            if(pendingDeveloper){pendingDeveloper=false;lesson.view.ToggleDeveloper();}
            if(pendingRestart){pendingRestart=false;pendingChoice=-1;pendingClick=false;lesson.Restart();}
            if(!IsDesktop)return;
            if(lesson.CinematicBlocking){pendingChoice=-1;pendingClick=false;ClearHover();SetMouseLook(false);return;}
            bool focused=Application.isFocused;if(focused!=wasFocused){wasFocused=focused;if(!focused)SetMouseLook(false);}
            if(pendingEscape){pendingEscape=false;SetMouseLook(false);}
            if(pendingChoice>=0)
            {
                int choice=pendingChoice;pendingChoice=-1;
                if(pendingRevision==lesson.Flow.Revision&&pendingRun==lesson.RunVersion)lesson.view.InvokeChoice(choice);
            }
            bool click=pendingClick;pendingClick=false;
            var mouse=Mouse.current;
            if(mouse!=null && mouse.rightButton.isPressed!=MouseLookActive)SetMouseLook(mouse.rightButton.isPressed);
            if(MouseLookActive&&mouse!=null)
            {
                var delta=skipDelta?Vector2.zero:mouse.delta.ReadValue();skipDelta=false;
                yaw+=delta.x*sensitivity;pitch=Mathf.Clamp(pitch-delta.y*sensitivity,-65,65);
                targetCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
            }
            if(mouse!=null)
            {
                UpdateCenterRay();
                if(click&&!MouseLookActive&&clickRevision==lesson.Flow.Revision&&clickRun==lesson.RunVersion)SelectCenterTarget();
            }
        }
        void LateUpdate()
        {
            if(!calibrateXR||IsDesktop)return;
            var device=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if(!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool tracked)||!tracked)return;
            // One initial height offset; physical head movement stays tracked afterward.
            xrOrigin.position+=Vector3.up*(eyeHeight-targetCamera.transform.position.y);
            lesson.view.Reanchor(targetCamera.transform.position);calibrateXR=false;
        }
        public void SetMouseLook(bool active)
        {
            MouseLookActive=active&&IsDesktop;Cursor.lockState=MouseLookActive?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!MouseLookActive;skipDelta=true;
            if(MouseLookActive&&Mouse.current!=null)Mouse.current.WarpCursorPosition(new Vector2(Screen.width*.5f,Screen.height*.5f));
            RefreshLaserVisibility();
        }
        public void ResetView()
        {
            if(!IsDesktop)return;SetScriptedHeading(lesson.presentation&&lesson.presentation.space?lesson.presentation.space.DefaultHeading:0);SetMouseLook(false);
        }
        public void SetScriptedHeading(float value){yaw=value;pitch=0;targetCamera.transform.rotation=Quaternion.Euler(0,yaw,0);}
        void RefreshLaserVisibility()
        {
            // A full laser across the image obscures small labels; a point is enough on desktop.
            if(centerLaser)centerLaser.enabled=false;
            if(laserDot)laserDot.gameObject.SetActive(initialized&&IsDesktop&&isActiveAndEnabled);
        }
        void UpdateCenterRay()
        {
            var screen=MouseLookActive?new Vector2(Screen.width*.5f,Screen.height*.5f):(Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero);
            var ray=targetCamera.ScreenPointToRay(screen);
            float distance=laserRange;GameObject target=null;
            if(Physics.Raycast(ray,out var hit,laserRange))
            {
                distance=hit.distance;var observation=hit.collider.GetComponentInParent<ElevatorObservation>();
                if(observation&&lesson.CanInteract(observation.clue))target=observation.gameObject;
            }
            if(viewRevision!=lesson.view.InteractionRevision)
            {
                viewRevision=lesson.view.InteractionRevision;lesson.view.CollectInteractiveButtons(buttons);
            }
            var system=EventSystem.current;
            if(system)
            {
                if(pointer==null)pointer=new PointerEventData(system);
                pointer.Reset();pointer.pointerId=-99;pointer.button=PointerEventData.InputButton.Left;pointer.position=screen;
                foreach(var button in buttons)
                {
                    if(!button||!button.IsActive()||!button.IsInteractable())continue;
                    var rect=(RectTransform)button.transform;var plane=new Plane(rect.forward,rect.position);
                    if(!plane.Raycast(ray,out float d)||d<targetCamera.nearClipPlane||d>distance)continue;
                    Vector3 point=ray.GetPoint(d);if(!rect.rect.Contains((Vector2)rect.InverseTransformPoint(point)))continue;
                    target=button.gameObject;distance=d;
                    pointer.pointerCurrentRaycast=new RaycastResult{gameObject=target,distance=d,worldPosition=point,worldNormal=-rect.forward};
                }
            }
            if(target!=hovered)
            {
                ClearHover();hovered=target;
                if(hovered)
                {
                    hovered.GetComponent<ElevatorObservation>()?.Hover(true);
                    if(pointer!=null)ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerEnterHandler);
                }
            }
            else if(hovered)hovered.GetComponent<ElevatorObservation>()?.Hover(true);
            var color=hovered?Target:Idle;
            centerLaser.SetPosition(0,targetCamera.transform.TransformPoint(new Vector3(.055f,-.045f,.15f)));
            centerLaser.SetPosition(1,ray.GetPoint(Mathf.Clamp(distance,.15f,laserRange)));
            tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);centerLaser.SetPropertyBlock(tint);
            laserDot.position=ray.GetPoint(Mathf.Clamp(distance-.006f,.12f,laserRange));laserDot.localScale=Vector3.one*Mathf.Clamp(distance*.004f,.008f,.03f);
            dotRenderer.SetPropertyBlock(tint);
        }
        public void SelectCenterTarget()
        {
            if(!IsDesktop||!hovered)return;
            ClickCount++;var observation=hovered.GetComponent<ElevatorObservation>();
            if(observation){observation.Activate();return;}
            if(pointer==null)return;
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerClickHandler);
        }
        void ClearHover()
        {
            if(!hovered)return;hovered.GetComponent<ElevatorObservation>()?.Hover(false);
            if(pointer!=null)ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerExitHandler);hovered=null;
        }
        void OnDisable()
        {
            ClearHover();if(choices!=null)foreach(var a in choices)a.Dispose();
            restartAction?.Dispose();developerAction?.Dispose();clickAction?.Dispose();escapeAction?.Dispose();
            pendingChoice=-1;pendingRestart=pendingDeveloper=pendingClick=pendingEscape=false;
            if(IsDesktop){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(centerLaser)centerLaser.enabled=false;if(laserDot)laserDot.gameObject.SetActive(false);
        }
    }
}

