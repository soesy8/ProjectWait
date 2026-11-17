using System.Collections.Generic;
using ProjectWait.M.Shared;
using ProjectWait.M.SceneTransition;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectWait.M.TitleScene
{
    [DefaultExecutionOrder(-100)]
    public sealed class TitlePauseMenu : MonoBehaviour
    {
        public TitlePlazaPlayer player;
        public Canvas canvas;
        public UnityEngine.UI.Button resumeButton,quitButton;
        public bool IsPaused {get;private set;}
        InputAction toggle,select;
        bool pendingClick;
        float priorTimeScale;
        bool priorAudioPause;
        UnityEngine.UI.Button hovered;
        PointerEventData pointer;
        BaseInputModule mutedModule;
        readonly List<RaycastResult> hits=new List<RaycastResult>();
        readonly Dictionary<Canvas,bool> hiddenCanvases=new Dictionary<Canvas,bool>();
        readonly FreshPressGate gate=new FreshPressGate();
        void OnEnable()
        {
            toggle=new InputAction("LobbyPause",InputActionType.Button);
            toggle.AddBinding("<Keyboard>/escape");toggle.AddBinding("<XRController>{LeftHand}/menuButton");toggle.AddBinding("<XRController>{RightHand}/secondaryButton");
            toggle.performed+=_=>Toggle();toggle.Enable();
            select=new InputAction("LobbyMenuSelect",InputActionType.Button,"<Mouse>/leftButton");
            select.AddBinding("<XRController>{RightHand}/triggerPressed");select.AddBinding("<XRController>{RightHand}/trigger");
            select.performed+=_=>pendingClick=true;select.Enable();
            resumeButton.onClick.AddListener(Resume);quitButton.onClick.AddListener(Quit);
            canvas.gameObject.SetActive(false);
        }
        public void RequireFreshClick(){gate.Arm(Time.unscaledTimeAsDouble);pendingClick=false;ClearTarget();}
        public void Toggle(){if(IsPaused)Resume();else Pause();}
        public void Pause()
        {
            if(IsPaused||StorybookSceneTransition.IsBusy)return;
            priorTimeScale=Time.timeScale;priorAudioPause=AudioListener.pause;IsPaused=true;
            Time.timeScale=0;AudioListener.pause=true;StoryMusic.SetMenuDucked(this,true);
            // The lobby uses its own tracked aim ray. Keep one click owner on PC
            // too, otherwise the existing UI module applies settings twice.
            mutedModule=EventSystem.current?EventSystem.current.currentInputModule:null;
            if(mutedModule)mutedModule.enabled=false;
            var camera=player.viewCamera;var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.sortingOrder=1000;
            var rect=(RectTransform)canvas.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.sizeDelta=new Vector2(1280,720);rect.SetPositionAndRotation(camera.transform.position+forward*1.4f,Quaternion.LookRotation(forward));rect.localScale=Vector3.one*.0016f;
            canvas.gameObject.SetActive(true);player.SetMenuOpen(true);RequireFreshClick();
            foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var other in root.GetComponentsInChildren<Canvas>(true))
            {
                if(other==canvas||other.transform.IsChildOf(canvas.transform))continue;
                hiddenCanvases[other]=other.enabled;other.enabled=false;
            }
        }
        public void Resume()
        {
            if(!IsPaused)return;
            IsPaused=false;Time.timeScale=priorTimeScale;AudioListener.pause=priorAudioPause;StoryMusic.SetMenuDucked(this,false);
            canvas.gameObject.SetActive(false);ClearTarget();player.SetMenuOpen(false);pendingClick=false;
            if(mutedModule)mutedModule.enabled=true;mutedModule=null;
            foreach(var item in hiddenCanvases)if(item.Key)item.Key.enabled=item.Value;hiddenCanvases.Clear();
        }
        void Update()
        {
            bool click=pendingClick;pendingClick=false;gate.Observe(Time.unscaledTimeAsDouble,select.IsPressed());
            if(!IsPaused||!EventSystem.current)return;
            if(pointer==null)pointer=new PointerEventData(EventSystem.current);
            pointer.Reset();pointer.button=PointerEventData.InputButton.Left;pointer.pointerId=-98;
            UnityEngine.UI.Button target=null;
            if(player.IsVR)
            {
                bool valid=player.TryGetAimRay(out var ray);Vector3 end=valid?ray.GetPoint(8):Vector3.zero;float nearest=8;
                if(valid)foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    if(!button.IsActive()||!button.IsInteractable())continue;
                    var rect=(RectTransform)button.transform;
                    if(!new Plane(rect.forward,rect.position).Raycast(ray,out float distance)||distance<0||distance>nearest)continue;
                    var point=ray.GetPoint(distance);if(!rect.rect.Contains((Vector2)rect.InverseTransformPoint(point)))continue;
                    nearest=distance;end=point;target=button;
                    pointer.pointerCurrentRaycast=new RaycastResult{gameObject=button.gameObject,worldPosition=point,distance=distance};
                }
                player.ShowMenuRay(ray,valid,end,target,click&&gate.Ready);
            }
            else if(Mouse.current!=null)
            {
                pointer.position=Mouse.current.position.ReadValue();hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
                foreach(var hit in hits)
                {
                    var button=hit.gameObject?hit.gameObject.GetComponentInParent<UnityEngine.UI.Button>():null;
                    if(!button||!button.IsActive()||!button.IsInteractable()||!button.transform.IsChildOf(canvas.transform))continue;
                    target=button;pointer.pointerCurrentRaycast=hit;break;
                }
            }
            if(hovered!=target){ClearTarget();hovered=target;if(hovered)ExecuteEvents.Execute(hovered.gameObject,pointer,ExecuteEvents.pointerEnterHandler);}
            if(!hovered||!click||!gate.Ready)return;
            var selected=hovered.gameObject;
            ExecuteEvents.Execute(selected,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(selected,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(selected,pointer,ExecuteEvents.pointerClickHandler);
        }
        void ClearTarget(){if(hovered&&pointer!=null)ExecuteEvents.Execute(hovered.gameObject,pointer,ExecuteEvents.pointerExitHandler);hovered=null;}
        public void Quit()
        {
            Resume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void OnDisable()
        {
            Resume();StoryMusic.SetMenuDucked(this,false);toggle?.Dispose();select?.Dispose();
            if(resumeButton)resumeButton.onClick.RemoveListener(Resume);if(quitButton)quitButton.onClick.RemoveListener(Quit);
        }
    }
}
