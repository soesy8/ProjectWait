using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator
{
    [DefaultExecutionOrder(-100)]
    public sealed class ElevatorPauseMenu : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public ElevatorDesktopControls controls;
        public Canvas canvas;
        public UnityEngine.UI.Button resumeButton,restartButton,titleButton,quitButton;
        public bool IsPaused {get;private set;}
        public bool QuitRequested {get;private set;}
        InputAction toggle;
        float priorTimeScale=1;
        bool priorAudioPause;
        public IEnumerable<UnityEngine.UI.Button> Buttons=>new[]{resumeButton,restartButton,titleButton,quitButton};
        void OnEnable()
        {
            toggle=new InputAction("ElevatorPause",InputActionType.Button);
            toggle.AddBinding("<Keyboard>/escape");toggle.AddBinding("<XRController>{LeftHand}/menuButton");toggle.AddBinding("<XRController>{RightHand}/secondaryButton");
            toggle.performed+=_=>Toggle();toggle.Enable();
            Bind(resumeButton,Resume);Bind(restartButton,Restart);Bind(quitButton,QuitGame);
            if(titleButton){titleButton.onClick.RemoveAllListeners();titleButton.interactable=false;}
            if(canvas)canvas.gameObject.SetActive(false);
        }
        static void Bind(UnityEngine.UI.Button button,UnityEngine.Events.UnityAction action){if(!button)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
        public void Toggle(){if(IsPaused)Resume();else Pause();}
        public void Pause()
        {
            if(IsPaused||!lesson||lesson.Flow==null||!canvas)return;
            priorTimeScale=Time.timeScale;priorAudioPause=AudioListener.pause;IsPaused=true;
            Time.timeScale=0;AudioListener.pause=true;
            foreach(var o in lesson.GetComponentsInChildren<ElevatorObservation>(true))o.ResetHover();
            controls?.ClearTarget();controls?.SetMouseLook(false);
            canvas.renderMode=XRSettings.isDeviceActive?RenderMode.WorldSpace:RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera=lesson.playerCamera;canvas.sortingOrder=1000;
            if(XRSettings.isDeviceActive)
            {
                var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(1280,720);
                var camera=lesson.playerCamera.transform;
                rect.SetPositionAndRotation(camera.position+camera.forward*1.3f,camera.rotation);rect.localScale=Vector3.one*.0018f;
            }
            canvas.gameObject.SetActive(true);lesson.view.SetCinematicVisibility();
        }
        public void Resume()
        {
            if(!IsPaused)return;
            IsPaused=false;Time.timeScale=priorTimeScale;AudioListener.pause=priorAudioPause;
            if(canvas)canvas.gameObject.SetActive(false);
            controls?.ClearTarget();controls?.SetMouseLook(true);lesson?.view.SetCinematicVisibility();
        }
        public void Restart(){Resume();lesson.Restart();}
        public void QuitGame()
        {
            QuitRequested=true;Resume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void LateUpdate()
        {
            if(IsPaused&&XRSettings.isDeviceActive&&canvas)
            {Vector3 facing=canvas.transform.position-lesson.playerCamera.transform.position;if(facing.sqrMagnitude>.001f)canvas.transform.rotation=Quaternion.LookRotation(facing,Vector3.up);}
        }
        void OnDisable(){toggle?.Dispose();if(IsPaused){Time.timeScale=priorTimeScale;AudioListener.pause=priorAudioPause;IsPaused=false;}if(canvas)canvas.gameObject.SetActive(false);}
    }
}
