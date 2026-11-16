// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorPauseMenu.cs. Original is not modified.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    [DefaultExecutionOrder(-100)]
    public sealed class ElevatorPauseMenu : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public ElevatorDesktopControls controls;
        public Canvas canvas;
        public bool useSharedArtwork;
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
            Bind(titleButton,ReturnToLobby); // Serialized legacy field now owns the shared lobby action.
            if(canvas)canvas.gameObject.SetActive(false);
        }
        static void Bind(UnityEngine.UI.Button button,UnityEngine.Events.UnityAction action){if(!button)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
        public void Toggle(){if(IsPaused)Resume();else Pause();}
        public void Pause()
        {
            if(ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy)return;
            if(IsPaused||!lesson||lesson.Flow==null||!canvas)return;
            priorTimeScale=Time.timeScale;priorAudioPause=AudioListener.pause;IsPaused=true;
            ProjectWait.M.Shared.StoryMusic.SetMenuDucked(this,true);
            Time.timeScale=0;AudioListener.pause=true;
            foreach(var o in lesson.CollectObservations())o.ResetHover();
            controls?.ClearTarget();controls?.SetMouseLook(false);
            canvas.renderMode=RenderMode.WorldSpace;
            canvas.worldCamera=lesson.playerCamera;canvas.sortingOrder=1000;
            if(canvas.renderMode==RenderMode.WorldSpace)
            {
                var rect=(RectTransform)canvas.transform;
                // Centre anchors on child cards require a centred root pivot as well.
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1280,720);
                var camera=lesson.playerCamera.transform;
                var forward=Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
                rect.SetPositionAndRotation(camera.position+forward*1.4f,Quaternion.LookRotation(forward));rect.localScale=Vector3.one*.0016f;
            }
            canvas.gameObject.SetActive(true);lesson.view.SetCinematicVisibility();
            controls?.RequireFreshChoiceClick();
        }
        public void Resume()
        {
            if(!IsPaused)return;
            // Keep the four-action shared menu while retaining the old fallback:
            // continuing confirms a stable worn pose when userPresence is unavailable.
            if(controls&&!controls.IsDesktop&&controls.XRCalibrationPending)controls.RequestXRHeightCalibration();
            IsPaused=false;Time.timeScale=priorTimeScale;AudioListener.pause=priorAudioPause;
            ProjectWait.M.Shared.StoryMusic.SetMenuDucked(this,false);
            if(canvas)canvas.gameObject.SetActive(false);
            controls?.ClearTarget();controls?.SetMouseLook(true);lesson?.view.SetCinematicVisibility();
        }
        public void Restart(){lesson.RestartExperience();if(ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy)Resume();}
        public void ReturnToLobby(){lesson.ReturnToLobby();if(ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy)Resume();}
        public void CalibrateHeight()
        {
            if(controls&&!controls.IsDesktop)controls.RequestXRHeightCalibration();
        }
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
            if(IsPaused&&canvas)
            {Vector3 facing=canvas.transform.position-lesson.playerCamera.transform.position;if(facing.sqrMagnitude>.001f)canvas.transform.rotation=Quaternion.LookRotation(facing,Vector3.up);}
        }
        void OnDisable(){ProjectWait.M.Shared.StoryMusic.SetMenuDucked(this,false);toggle?.Dispose();if(IsPaused){Time.timeScale=priorTimeScale;AudioListener.pause=priorAudioPause;IsPaused=false;}if(canvas)canvas.gameObject.SetActive(false);}
    }
}
