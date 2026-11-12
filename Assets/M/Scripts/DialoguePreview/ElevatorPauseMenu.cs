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
        public UnityEngine.UI.Button resumeButton,restartButton,titleButton,quitButton;
        public bool IsPaused {get;private set;}
        public bool QuitRequested {get;private set;}
        InputAction toggle;
        float priorTimeScale=1;
        bool priorAudioPause;
        TMP_Text heightStatus;
        public IEnumerable<UnityEngine.UI.Button> Buttons=>new[]{resumeButton,restartButton,titleButton,quitButton};
        void OnEnable()
        {
            toggle=new InputAction("ElevatorPause",InputActionType.Button);
            toggle.AddBinding("<Keyboard>/escape");toggle.AddBinding("<XRController>{LeftHand}/menuButton");toggle.AddBinding("<XRController>{RightHand}/secondaryButton");
            toggle.performed+=_=>Toggle();toggle.Enable();
            Bind(resumeButton,Resume);Bind(restartButton,Restart);Bind(quitButton,QuitGame);
            Bind(titleButton,CalibrateHeight);UpdateCalibrationButton();
            if(canvas)canvas.gameObject.SetActive(false);
        }
        static void Bind(UnityEngine.UI.Button button,UnityEngine.Events.UnityAction action){if(!button)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
        public void Toggle(){if(IsPaused)Resume();else Pause();}
        public void Pause()
        {
            if(ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy)return;
            if(IsPaused||!lesson||lesson.Flow==null||!canvas)return;
            priorTimeScale=Time.timeScale;priorAudioPause=AudioListener.pause;IsPaused=true;
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
                rect.SetPositionAndRotation(camera.position+camera.forward*1.4f,camera.rotation);rect.localScale=Vector3.one*.0016f;
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
        public void CalibrateHeight()
        {
            if(controls&&!controls.IsDesktop)controls.RequestXRHeightCalibration();
        }
        void UpdateCalibrationButton()
        {
            if(!titleButton)return;
            bool vr=controls&&!controls.IsDesktop;
            titleButton.gameObject.SetActive(vr);
            titleButton.interactable=vr;
            var label=titleButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            if(label)label.text="눈높이 맞추기";
            var card=resumeButton?(RectTransform)resumeButton.transform.parent:null;
            if(!card)return;
            card.sizeDelta=new Vector2(550,vr?560:440);
            ((RectTransform)resumeButton.transform).anchoredPosition=new Vector2(0,vr?125:80);
            ((RectTransform)restartButton.transform).anchoredPosition=new Vector2(0,vr?32:-13);
            ((RectTransform)quitButton.transform).anchoredPosition=new Vector2(0,vr?-154:-106);
            var heading=card.Find("Heading") as RectTransform;
            if(heading)heading.anchoredPosition=new Vector2(0,vr?220:170);
            if(!heightStatus)
            {
                var rect=(RectTransform)new GameObject("HeightStatus",typeof(RectTransform)).transform;
                rect.SetParent(card,false);
                rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
                heightStatus=rect.gameObject.AddComponent<TextMeshProUGUI>();
                heightStatus.font=label.font;heightStatus.fontSharedMaterial=label.fontSharedMaterial;
                heightStatus.color=label.color;heightStatus.alignment=TextAlignmentOptions.Center;
                heightStatus.raycastTarget=false;
            }
            ElevatorTypography.Apply(heightStatus,22);
            heightStatus.rectTransform.sizeDelta=new Vector2(450,64);
            heightStatus.rectTransform.anchoredPosition=new Vector2(0,-234);
            heightStatus.gameObject.SetActive(vr);
            heightStatus.text=vr?controls.XRHeightStatus:"";
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
            UpdateCalibrationButton();
            if(IsPaused&&canvas)
            {Vector3 facing=canvas.transform.position-lesson.playerCamera.transform.position;if(facing.sqrMagnitude>.001f)canvas.transform.rotation=Quaternion.LookRotation(facing,Vector3.up);}
        }
        void OnDisable(){toggle?.Dispose();if(IsPaused){Time.timeScale=priorTimeScale;AudioListener.pause=priorAudioPause;IsPaused=false;}if(canvas)canvas.gameObject.SetActive(false);}
    }
}
