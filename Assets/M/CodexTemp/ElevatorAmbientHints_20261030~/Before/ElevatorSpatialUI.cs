using TMPro;
using UnityEngine;
using UnityEngine.XR;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator
{
    // World dialogue never follows the player's gaze. Only the non-interactive HUD does.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ElevatorSpatialUI : MonoBehaviour
    {
        public ElevatorView view;
        public Canvas hud;
        public RectTransform mission, speakerTab;
        public TMP_Text speaker, progress, controls, thoughtHeading;
        public RectTransform progressBackdrop;
        public RectTransform controlsBackdrop;
        public Transform intercomAnchor, rescuerAnchor;
        public Transform notebook;
        public RectTransform leftPage, rightPage;
        public TMP_Text notebookCaption, notebookDetail;
        public TMP_Text[] completionMarks;
        public RectTransform[] completionRows;
        [Header("World placement in metres, relative to the scene's forward direction")]
        public Vector3 friendOffset = new Vector3(1.12f, 1.53f, -.18f);
        public Vector3 intercomOffset = new Vector3(-1.14f, .72f, -.80f);
        [Min(.0001f)] public float worldPixelSize = .00215f;
        [Min(.1f)] public float notebookDistance = 1.0f;
        public Vector3 notebookOffset = new Vector3(0, -.22f, 0);
        public Vector3 notebookTilt = new Vector3(12, 0, 0);
        [Min(.1f)] public float notebookScale = 1.12f;
        public Vector3 rescuerOffset = new Vector3(1.40f, 1.67f, -2.05f);

        static readonly Color Ink = new Color(.28f,.19f,.14f);
        static readonly Color Paper = new Color(1,.965f,.86f);
        static readonly Color Teal = new Color(.16f,.48f,.44f);
        static readonly Color Mint = new Color(.79f,.89f,.76f);
        string baseThought = "", observed = "";
        float revealStarted;
        int historyPage;
        bool screenOnly, complete;
        ElevatorStep placedStep = (ElevatorStep)(-1);
        int placedRun = -1;
        CanvasGroup worldGroup, hudGroup,descriptionDialogueGroup;
        float layoutAspect=-1;
        int layoutWidth,layoutHeight;

        void OnEnable(){Application.onBeforeRender+=PositionHud;}
        void OnDisable(){Application.onBeforeRender-=PositionHud;}

        public void Present(string who, string line, string objective)
        {
            var f = view.lesson.Flow;
            complete = f.Step == ElevatorStep.Complete;
            screenOnly = !view.lesson.scenario && (who == "나의 미션" || who == "나의 발견");
            observed = "";
            baseThought = screenOnly ? line : "";
            if(f.Step == ElevatorStep.Call && f.SituationReported && !f.IdentityFound)
            {
                // The manager's speech remains in the world; search help belongs to the HUD.
                view.dialogueText.text = "건물명과 승강기 번호를\n알려 주세요.";
                baseThought = f.HintLevel > 0 ? "전면 비상벨 아래의 식별판에서 건물명과 관리번호를 찾아보자." : "벽의 식별판을 찾아 선택해 보자.";
            }
            speaker.text = who;
            view.dialoguePanel.gameObject.SetActive(!screenOnly);
            view.actionPanel.SetParent(complete ? rightPage : view.transform, false);
            view.actionPanel.localScale = Vector3.one;
            if(complete)
            {
                view.dialogueText.text = "고장 났을 때도 함께 안전하게 대처했어!";
                view.dialoguePanel.gameObject.SetActive(true);
                bool[] done = {f.HelpRequested, f.LocationReported, f.Reassured};
                for(int i=0;i<completionMarks.Length;i++)
                {
                    completionMarks[i].text = done[i] ? "완료" : "돌아보기";
                    completionMarks[i].color = done[i] ? Teal : Ink;
                }
                notebookCaption.text = "함께 안전하게 기다렸어요.";
                notebookDetail.text = "도움을 요청하고, 위치를 알리고,\n친구와 함께 안내를 기다렸어요.";
            }
            notebook.gameObject.SetActive(complete);
            mission.gameObject.SetActive(!complete);
            progress.gameObject.SetActive(!complete);
            revealStarted = Time.time;
            UpdateThought();
            Layout();
        }

        public void StyleButton(UnityEngine.UI.Button button)
        {
            var g = button.GetComponent<CorridorBubbleGraphic>();
            g.color = Teal; g.borderColor = Paper; g.borderWidth = 2.5f;
            g.cornerRadius = 15; g.extrusion = 8; g.bevelWidth = 4;
            g.shadowOffset = new Vector2(4,-10); g.shadowOpacity = .16f;
            var label = button.GetComponentInChildren<TMP_Text>();
            label.text = label.text.TrimStart('1','2','3','4',' ');
            label.text = label.text.Replace("요. ","요.\n");
            label.color = Paper; label.fontSize = 28;
            label.fontStyle = FontStyles.Normal;
        }

        public void ShowThought(string value)
        {
            observed = value ?? "";
            UpdateThought();
            LayoutHud();
        }

        void UpdateThought()
        {
            var text = string.IsNullOrEmpty(observed) ? baseThought : observed;
            view.thought.text = text;
            view.thought.maxVisibleCharacters = int.MaxValue;
            thoughtHeading.text = string.IsNullOrEmpty(observed) ? "나의 생각 · 힌트" : "살펴보니…";
            view.thoughtPanel.gameObject.SetActive(!complete && !string.IsNullOrEmpty(text));
        }

        public void Restore()
        {
            historyPage = 0; placedRun = -1; observed = baseThought = "";
            if(notebook)notebook.gameObject.SetActive(false);
            UpdateThought();
        }

        public void ShowHistory()
        {
            string[] titles = {"01  도움을 요청했어요", "02  위치를 전달했어요", "03  함께 기다렸어요"};
            string[] details = {
                "비상통화로 멈춘 상황과\n함께 있는 사람을 알렸어요.",
                "벽의 표시를 찾아 건물명과\n승강기 번호를 알려 줬어요.",
                "놀란 친구와 천천히 숨을 고르고\n구조원의 안내를 기다렸어요."
            };
            int page = historyPage++ % 3;
            notebookCaption.text = titles[page];
            notebookDetail.text = details[page];
            for(int i=0;i<completionRows.Length;i++)completionRows[i].GetComponent<CorridorBubbleGraphic>().color = i==page ? Mint : Paper;
        }

        public void Layout()
        {
            if(!view || !hud || !view.playerCamera)return;
            var root = (RectTransform)view.transform;
            root.sizeDelta = new Vector2(580,460);
            root.localScale = Vector3.one * worldPixelSize;
            const float width = 510;
            float body = Mathf.Max(64, view.dialogueText.GetPreferredValues(view.dialogueText.text,width-56,0).y+8);
            float height = body + 68;
            view.dialoguePanel.sizeDelta = new Vector2(width,height);
            view.dialoguePanel.anchoredPosition = Vector2.zero;
            view.dialogueText.rectTransform.sizeDelta = new Vector2(width-56,body);
            view.dialogueText.rectTransform.anchoredPosition = new Vector2(0,-12);
            speakerTab.sizeDelta = new Vector2(Mathf.Clamp(speaker.GetPreferredValues().x+44,116,300),46);
            speakerTab.anchoredPosition = new Vector2(-width*.5f+speakerTab.rect.width*.5f+24,height*.5f+5);
            speaker.rectTransform.sizeDelta = speakerTab.sizeDelta-new Vector2(22,4);
            speaker.rectTransform.anchoredPosition = Vector2.zero;
            view.actionPanel.sizeDelta = complete ? new Vector2(438,72) : new Vector2(width,100*view.ChoiceButtons.Count);
            float buttonY=0;
            for(int i=0;i<view.ChoiceButtons.Count;i++)
            {
                var button = view.ChoiceButtons[i];
                if(!button)continue;
                float buttonWidth = complete ? 211 : button.name=="DialogueNext"?190:width;
                var r = (RectTransform)button.transform;
                var label = button.GetComponentInChildren<TMP_Text>();
                label.fontSize = complete ? 21 : 28;
                label.textWrappingMode=TextWrappingModes.Normal;
                if(complete)label.text = i==0 ? "우리가 해낸 일" : "다시 해보기";
                float buttonHeight=complete?70:button.name=="DialogueNext"?72:Mathf.Max(88,label.GetPreferredValues(label.text,buttonWidth-44,0).y+28);
                r.sizeDelta=new Vector2(buttonWidth,buttonHeight);
                r.anchoredPosition=complete?new Vector2((i-.5f)*227,0):new Vector2(0,-buttonY-buttonHeight*.5f);
                r.localPosition=new Vector3(r.localPosition.x,r.localPosition.y,-1);
                label.rectTransform.sizeDelta=r.sizeDelta-new Vector2(44,20);
                buttonY+=buttonHeight+18;
            }
            view.actionPanel.anchoredPosition=complete?new Vector2(0,-216):new Vector2(0,-height*.5f-40);
            LayoutHud();
            PositionWorld();
            layoutAspect=view.playerCamera.aspect;layoutWidth=Screen.width;layoutHeight=Screen.height;
        }

        void LayoutHud()
        {
            if(view.lesson.scenario){hud.renderMode=RenderMode.WorldSpace;hud.gameObject.SetActive(false);return;}
            var camera = view.playerCamera;
            bool xr = XRSettings.isDeviceActive;
            hud.renderMode = xr ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            hud.worldCamera = camera;
            hud.planeDistance = .4f;
            var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 1;
            float width = 720*camera.aspect;
            if(xr)
            {
                var hr = (RectTransform)hud.transform; hr.sizeDelta=new Vector2(width,720);
                PositionHud();
            }
            mission.anchorMin=mission.anchorMax=mission.pivot=new Vector2(0,1);
            mission.sizeDelta=new Vector2(Mathf.Min(370,width-48),72);mission.anchoredPosition=new Vector2(24,-22);
            view.title.rectTransform.sizeDelta=new Vector2(mission.rect.width-32,20);view.title.rectTransform.anchoredPosition=new Vector2(0,20);
            view.subtitle.rectTransform.sizeDelta=new Vector2(mission.rect.width-32,36);view.subtitle.rectTransform.anchoredPosition=new Vector2(0,-10);
            progress.rectTransform.anchorMin=progress.rectTransform.anchorMax=progress.rectTransform.pivot=new Vector2(1,1);
            progress.rectTransform.anchoredPosition=new Vector2(-24,-24);progress.rectTransform.sizeDelta=new Vector2(530,32);
            progress.fontSize=16;progress.gameObject.SetActive(!complete&&camera.aspect>1.5f);
            if(progressBackdrop){progressBackdrop.anchorMin=progressBackdrop.anchorMax=progressBackdrop.pivot=Vector2.one;progressBackdrop.anchoredPosition=new Vector2(-14,-16);progressBackdrop.sizeDelta=new Vector2(550,48);progressBackdrop.gameObject.SetActive(progress.gameObject.activeSelf);}
            controls.rectTransform.anchorMin=controls.rectTransform.anchorMax=new Vector2(.5f,0);
            controls.rectTransform.anchoredPosition=new Vector2(0,12);controls.rectTransform.sizeDelta=new Vector2(width-40,22);
            controls.gameObject.SetActive(!xr&&!complete);
            if(controlsBackdrop){controlsBackdrop.anchorMin=controlsBackdrop.anchorMax=new Vector2(.5f,0);controlsBackdrop.pivot=new Vector2(.5f,0);controlsBackdrop.anchoredPosition=new Vector2(0,6);controlsBackdrop.sizeDelta=new Vector2(Mathf.Min(800,width-40),32);controlsBackdrop.gameObject.SetActive(controls.gameObject.activeSelf);}
            controls.text="WASD 이동  ·  마우스로 둘러보기  ·  중앙 조준 후 클릭  ·  Esc 일시정지";
            float thoughtWidth=Mathf.Min(480,width-48);
            float thoughtHeight=Mathf.Max(112,view.thought.GetPreferredValues(view.thought.text,thoughtWidth-54,0).y+56);
            view.thoughtPanel.anchorMin=view.thoughtPanel.anchorMax=view.thoughtPanel.pivot=Vector2.zero;
            view.thoughtPanel.sizeDelta=new Vector2(thoughtWidth,thoughtHeight);
            view.thoughtPanel.anchoredPosition=new Vector2(28,52);
            thoughtHeading.rectTransform.sizeDelta=new Vector2(thoughtWidth-52,24);thoughtHeading.rectTransform.anchoredPosition=new Vector2(0,thoughtHeight*.5f-23);
            view.thought.rectTransform.sizeDelta=new Vector2(thoughtWidth-52,thoughtHeight-50);view.thought.rectTransform.anchoredPosition=new Vector2(0,-16);
        }

        void PositionWorld()
        {
            var lesson=view.lesson;
            var f=lesson.Flow;
            var step=f!=null?f.Step:ElevatorStep.Lobby;
            bool outside=step==ElevatorStep.Lobby||step==ElevatorStep.Complete||step==ElevatorStep.Entering;
            var rotation=Quaternion.Euler(0,outside?180:0,0);
            Vector3 anchor=lesson.friend.position+rotation*friendOffset;
            if(step==ElevatorStep.Call||step==ElevatorStep.Connecting||step==ElevatorStep.Unanswered)
                anchor=(intercomAnchor?intercomAnchor.position:lesson.presentation.intercomButton.position)+intercomOffset;
            if(step==ElevatorStep.Retry)anchor=new Vector3(.25f,1.60f,1.90f);
            if(step==ElevatorStep.Rescue)anchor=(rescuerAnchor?rescuerAnchor.position:new Vector3(-.85f,0,4.1f))+rescuerOffset;
            if(complete)anchor=lesson.friend.position+rotation*new Vector3(.95f,1.65f,-.08f);
            var map=lesson.presentation.space?lesson.presentation.space.sportsCentre:null;
            if(map){rotation=map.DialogueRotation;anchor=map.DialogueAnchor();}
            var facing=anchor-view.playerCamera.transform.position;
            if(facing.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(facing,Vector3.up);
            transform.SetPositionAndRotation(anchor,rotation);
            if(placedStep!=step||placedRun!=lesson.RunVersion)
            {
                // Place the book once when the stage changes; subsequent gaze changes do not move it.
                var camera=view.playerCamera.transform;
                var bookRotation=Quaternion.Euler(0,camera.eulerAngles.y,0);
                notebook.SetPositionAndRotation(camera.position+bookRotation*(Vector3.forward*notebookDistance+notebookOffset),bookRotation*Quaternion.Euler(notebookTilt));
                notebook.localScale=Vector3.one*notebookScale;
                placedStep=step;placedRun=lesson.RunVersion;
            }
            if(complete&&notebook)
            {
                var direction=notebook.position-view.playerCamera.transform.position;
                if(direction.sqrMagnitude>.001f)notebook.rotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Euler(notebookTilt);
            }
        }

        public void SetObjectHintVisibility(bool visible)
        {
            if(!descriptionDialogueGroup)descriptionDialogueGroup=view.dialoguePanel.GetComponent<CanvasGroup>();
            if(!descriptionDialogueGroup)descriptionDialogueGroup=view.dialoguePanel.gameObject.AddComponent<CanvasGroup>();
            // Keep action buttons usable while showing one explanation at a time.
            descriptionDialogueGroup.alpha=visible&&!complete?0:1;
        }
        public void SetCinematicVisibility()
        {
            if(!view.lesson||view.lesson.Flow==null)return;
            if(!worldGroup)worldGroup=GetComponent<CanvasGroup>();
            if(!worldGroup)worldGroup=gameObject.AddComponent<CanvasGroup>();
            if(!hudGroup)hudGroup=hud.GetComponent<CanvasGroup>();
            if(!hudGroup)hudGroup=hud.gameObject.AddComponent<CanvasGroup>();
            // Dialogue remains spatial during story beats; transitions use the existing comfort fade.
            var f=view.lesson.Flow;
            bool transition=f.Step==ElevatorStep.WaitingFade||f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.Returning||f.Step==ElevatorStep.GoingHome;
            bool paused=view.lesson.IsPaused;
            worldGroup.alpha=transition||paused?0:1;worldGroup.interactable=!view.lesson.CinematicBlocking;worldGroup.blocksRaycasts=worldGroup.interactable;
            hudGroup.alpha=f.IsCinematic||paused?0:1;hudGroup.interactable=false;hudGroup.blocksRaycasts=false;
            if(notebook)notebook.gameObject.SetActive(complete&&!paused);
        }

        public void Tick()
        {
            if(Mathf.Abs(layoutAspect-view.playerCamera.aspect)>.001f||layoutWidth!=Screen.width||layoutHeight!=Screen.height)Layout();
            else PositionWorld();
            PositionHud();SetCinematicVisibility();
            if(view.lesson.Flow!=null)view.dialogueText.maxVisibleCharacters=Mathf.Max(1,(int)((Time.time-revealStarted)*42));
        }
        void PositionHud()
        {
            if(!XRSettings.isDeviceActive||!hud||!view||!view.playerCamera)return;
            var camera=view.playerCamera;
            const float distance=1;
            hud.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*distance,camera.transform.rotation);
            hud.transform.localScale=Vector3.one*(2*distance*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)/720);
        }
    }
}
