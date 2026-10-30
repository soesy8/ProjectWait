using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ProjectWait.M.Elevator
{
    public sealed class ElevatorDesktopControls : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Camera targetCamera;
        public Transform xrOrigin;
        public LineRenderer centerLaser;
        public Transform laserDot;
        public ElevatorReticleGraphic reticle;
        public ElevatorPlayerMovement movement;
        public float eyeHeight=1.1064756f,sensitivity=.10f,laserRange=12;
        public bool IsDesktop {get;private set;}
        public bool MouseLookActive {get;private set;}
        public string HoverTargetName=>hovered?hovered.name:"";
        public Vector2 LookAngles=>new Vector2(yaw,pitch);
        public int ClickCount {get;private set;}
        readonly List<UnityEngine.UI.Button> buttons=new List<UnityEngine.UI.Button>();
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        GameObject hovered;
        PointerEventData pointer;
        XRUIInputModule uiModule;
        Renderer dotRenderer;
        MaterialPropertyBlock tint;
        float yaw,pitch;
        bool initialized,skipDelta,wasFocused,calibrateXR;
        InputAction[] choices;
        InputAction restartAction,developerAction,clickAction;
        int pendingChoice=-1,pendingRevision,pendingRun,clickRevision,clickRun,viewRevision=-1;
        bool pendingRestart,pendingDeveloper,pendingClick;
        static readonly Color Idle=new Color(.85f,.94f,1),Target=new Color(.35f,.86f,1);
        void OnEnable()
        {
            tint=new MaterialPropertyBlock();if(laserDot)dotRenderer=laserDot.GetComponent<Renderer>();
            uiModule=FindFirstObjectByType<XRUIInputModule>();
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
        }
        void Start(){wasFocused=Application.isFocused;RefreshMode();}
        void RefreshMode()
        {
            bool desktop=!XRSettings.isDeviceActive;if(initialized&&desktop==IsDesktop)return;
            initialized=true;IsDesktop=desktop;ClearTarget();
            var pose=targetCamera.GetComponent<TrackedPoseDriver>();if(pose)pose.enabled=!desktop;
            if(uiModule)uiModule.enableMouseInput=false;
            if(desktop)
            {
                targetCamera.transform.position=new Vector3(xrOrigin.position.x,xrOrigin.position.y+eyeHeight,xrOrigin.position.z);
                if(movement)movement.CalibrateFeet(xrOrigin.position.y);
                SetScriptedHeading(lesson.presentation&&lesson.presentation.space?lesson.presentation.space.DefaultHeading:0);
                SetMouseLook(true);
            }
            else{SetMouseLook(false);calibrateXR=true;}
            RefreshLaserVisibility();
        }
        void Update()
        {
            if(!lesson||lesson.Flow==null)return;
            RefreshMode();bool click=pendingClick;pendingClick=false;
            if(lesson.IsPaused)
            {
                pendingChoice=-1;pendingRestart=pendingDeveloper=false;SetMouseLook(false);RefreshLaserVisibility();
                if(IsDesktop){UpdateMenuPointer();if(click)SelectCenterTarget();}return;
            }
            if(pendingDeveloper){pendingDeveloper=false;lesson.view.ToggleDeveloper();}
            if(pendingRestart){pendingRestart=false;pendingChoice=-1;click=false;lesson.Restart();}
            if(!IsDesktop)return;
            if(!MouseLookActive)SetMouseLook(true);
            bool focused=Application.isFocused;
            if(focused!=wasFocused){wasFocused=focused;SetMouseLook(true);skipDelta=true;}
            bool scripted=lesson.Flow.IsCinematic||lesson.scenario&&lesson.scenario.LockView||lesson.Flow.Step==ElevatorStep.Entering||lesson.Flow.Step==ElevatorStep.Exiting||lesson.Flow.Step==ElevatorStep.Returning||lesson.Flow.Step==ElevatorStep.GoingHome;
            var mouse=Mouse.current;
            if(!scripted&&mouse!=null)
            {
                Vector2 delta=skipDelta?Vector2.zero:mouse.delta.ReadValue();skipDelta=false;
                SetLookAngles(yaw+delta.x*sensitivity,pitch-delta.y*sensitivity);
            }
            if(lesson.CinematicBlocking){pendingChoice=-1;ClearTarget();RefreshLaserVisibility();return;}
            if(pendingChoice>=0)
            {
                int n=pendingChoice;pendingChoice=-1;
                if(pendingRevision==lesson.Flow.Revision&&pendingRun==lesson.RunVersion)lesson.view.InvokeChoice(n);
            }
            UpdateCenterRay();
            if(click&&clickRevision==lesson.Flow.Revision&&clickRun==lesson.RunVersion)SelectCenterTarget();
            RefreshLaserVisibility();
        }
        void LateUpdate()
        {
            if(!calibrateXR||IsDesktop)return;
            var d=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if(!d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool tracked)||!tracked)return;
            float floor=lesson.presentation.space.sportsCentre?lesson.presentation.space.sportsCentre.CurrentFloorHeight:0;
            var c=xrOrigin.GetComponent<CharacterController>();bool enabled=c&&c.enabled;if(enabled)c.enabled=false;
            xrOrigin.position+=Vector3.up*(floor+eyeHeight-targetCamera.transform.position.y);
            if(movement)movement.CalibrateFeet(floor);if(enabled)c.enabled=true;
            lesson.view.Reanchor(targetCamera.transform.position);calibrateXR=false;
        }
        public void SetMouseLook(bool active)
        {
            bool value=active&&IsDesktop&&!(lesson&&lesson.IsPaused);
            if(value!=MouseLookActive)skipDelta=true;
            MouseLookActive=value;
            Cursor.lockState=value&&Application.isFocused?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!value;RefreshLaserVisibility();
        }
        public void ResetView(){if(!IsDesktop)return;SetScriptedHeading(lesson.presentation.space.DefaultHeading);SetMouseLook(true);}
        public void SetScriptedHeading(float value){SetLookAngles(value,0);skipDelta=true;}
        public void SetLookAngles(float horizontal,float vertical){yaw=horizontal;pitch=Mathf.Clamp(vertical,-80,80);targetCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);}
        void RefreshLaserVisibility()
        {
            bool visible=initialized&&IsDesktop&&isActiveAndEnabled&&lesson&&!lesson.IsPaused&&!lesson.CinematicBlocking;
            if(centerLaser)centerLaser.enabled=visible;if(laserDot)laserDot.gameObject.SetActive(visible);if(reticle)reticle.gameObject.SetActive(visible);
        }
        void SetTarget(GameObject target)
        {
            if(hovered==target){if(hovered)hovered.GetComponent<ElevatorObservation>()?.Hover(true);return;}
            ClearTarget();hovered=target;
            if(hovered){hovered.GetComponent<ElevatorObservation>()?.Hover(true);if(pointer!=null)ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerEnterHandler);}
        }
        void PreparePointer(Vector2 position)
        {
            if(EventSystem.current==null)return;
            if(pointer==null)pointer=new PointerEventData(EventSystem.current);
            pointer.Reset();pointer.pointerId=-99;pointer.button=PointerEventData.InputButton.Left;pointer.position=position;
        }
        void UpdateMenuPointer()
        {
            if(Mouse.current==null||!EventSystem.current)return;
            PreparePointer(Mouse.current.position.ReadValue());uiHits.Clear();EventSystem.current.RaycastAll(pointer,uiHits);
            GameObject target=null;
            foreach(var hit in uiHits)
            {
                var b=hit.gameObject.GetComponentInParent<UnityEngine.UI.Button>();
                if(!b||!b.IsActive()||!b.IsInteractable()||!b.transform.IsChildOf(lesson.pauseMenu.canvas.transform))continue;
                target=b.gameObject;pointer.pointerCurrentRaycast=hit;break;
            }
            SetTarget(target);
        }
        void UpdateCenterRay()
        {
            var screen=new Vector2(Screen.width*.5f,Screen.height*.5f);var ray=targetCamera.ViewportPointToRay(new Vector3(.5f,.5f));
            float distance=laserRange;GameObject target=null;
            if(Physics.Raycast(ray,out var hit,laserRange,~0,QueryTriggerInteraction.Ignore))
            {
                distance=hit.distance;var o=hit.collider.GetComponentInParent<ElevatorObservation>();if(o&&lesson.CanInteract(o.clue))target=o.gameObject;
            }
            if(viewRevision!=lesson.view.InteractionRevision){viewRevision=lesson.view.InteractionRevision;lesson.view.CollectInteractiveButtons(buttons);}
            PreparePointer(screen);float uiDistance=laserRange;
            foreach(var button in buttons)
            {
                if(!button||!button.IsActive()||!button.IsInteractable())continue;
                var rect=(RectTransform)button.transform;var plane=new Plane(rect.forward,rect.position);
                if(!plane.Raycast(ray,out float d)||d<targetCamera.nearClipPlane||d>uiDistance)continue;
                var point=ray.GetPoint(d);if(!rect.rect.Contains((Vector2)rect.InverseTransformPoint(point)))continue;
                target=button.gameObject;distance=d;uiDistance=d;
                if(pointer!=null)pointer.pointerCurrentRaycast=new RaycastResult{gameObject=target,distance=d,worldPosition=point,worldNormal=-rect.forward};
            }
            SetTarget(target);var color=hovered?Target:Idle;
            if(centerLaser){centerLaser.SetPosition(0,targetCamera.transform.TransformPoint(new Vector3(0,-.022f,.13f)));centerLaser.SetPosition(1,ray.GetPoint(Mathf.Clamp(distance,.15f,laserRange)));tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);centerLaser.SetPropertyBlock(tint);}
            if(laserDot){laserDot.position=ray.GetPoint(Mathf.Clamp(distance-.008f,.12f,laserRange));laserDot.localScale=Vector3.one*Mathf.Clamp(distance*.003f,.007f,.02f);if(dotRenderer)dotRenderer.SetPropertyBlock(tint);}
            if(reticle)reticle.SetFocused(hovered);
        }
        public void SelectCenterTarget()
        {
            if(!IsDesktop||!hovered)return;ClickCount++;
            var observation=hovered.GetComponent<ElevatorObservation>();if(observation&&!lesson.IsPaused){observation.Activate();return;}
            if(pointer==null)return;
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerClickHandler);
        }
        public void ClearTarget()
        {
            if(!hovered)return;hovered.GetComponent<ElevatorObservation>()?.Hover(false);
            if(pointer!=null)ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerExitHandler);hovered=null;
            if(reticle)reticle.SetFocused(false);
        }
        void OnDisable()
        {
            ClearTarget();if(choices!=null)foreach(var a in choices)a.Dispose();restartAction?.Dispose();developerAction?.Dispose();clickAction?.Dispose();
            pendingChoice=-1;pendingRestart=pendingDeveloper=pendingClick=false;
            if(IsDesktop){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(centerLaser)centerLaser.enabled=false;if(laserDot)laserDot.gameObject.SetActive(false);if(reticle)reticle.gameObject.SetActive(false);
        }
    }
}
