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
        public ElevatorStorybookUI storybook;
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
        [Min(.1f)] public float maximumReadabilityScale=1.45f;

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
        CanvasGroup worldGroup, hudGroup;
        float layoutAspect=-1;
        int layoutWidth,layoutHeight;
        Vector2 layoutHudSize;
        string dialogueSource, dialogueSpeaker;
        string[] dialoguePages = { "" };
        int dialoguePage, dialogueRun = -1;
        bool revealComplete;
        TMP_Text readingStatus;
        UnityEngine.UI.Button dialogueReader;
        readonly Vector3[] projectionCorners = new Vector3[4];
        readonly System.Collections.Generic.List<Rect> readingObstacles = new System.Collections.Generic.List<Rect>();
        bool thoughtPlacementReady,friendPlacementReady;
        Vector2 thoughtAnchor;
        float friendCardShift;
        bool worldPlacementReady;
        Quaternion dialogueRotation;
        Vector3 dialogueScale;
        ElevatorStep dialoguePlacedStep=(ElevatorStep)(-1);
        int dialoguePlacedRun=-1;
        public int DialoguePage => dialoguePage;
        public int DialoguePageCount => dialoguePages.Length;
        public bool DialogueFullyRevealed => revealComplete;
        public float DialogueStartedAt => revealStarted;
        ElevatorUITransitions transitions;
        public ElevatorUITransitions Transitions
        {
            get
            {
                if(!Application.isPlaying)return null;
                if(!transitions){transitions=GetComponent<ElevatorUITransitions>();if(!transitions)transitions=gameObject.AddComponent<ElevatorUITransitions>();transitions.Initialize(view);}
                return transitions;
            }
        }

        void OnEnable(){Application.onBeforeRender+=PositionHud;}
        void OnDisable(){Application.onBeforeRender-=PositionHud;}

        public void Present(string who, string line, string objective)
        {
            Transitions?.Begin();
            var f = view.lesson.Flow;
            complete = f.Step == ElevatorStep.Complete;
            screenOnly = !view.lesson.scenario && (who == "나의 미션" || who == "나의 발견");
            observed = "";
            baseThought = screenOnly ? line : "";
            if(f.Step == ElevatorStep.Call && f.SituationReported && !f.IdentityFound)
            {
                // The manager's speech remains in the world; search help belongs to the HUD.
                view.dialogueText.text = "식별판에서 건물명과\n승강기 번호를 알려 주세요.";
                baseThought = f.HintLevel > 0 ? "전면 비상벨 아래의 식별판에서 건물명과 관리번호를 찾아보자." : "벽의 식별판을 찾아 선택해 보자.";
            }
            speaker.text = who;
            SetPanelVisible(view.dialoguePanel,!screenOnly);
            view.actionPanel.SetParent(complete ? rightPage : view.transform, false);
            view.actionPanel.localScale = Vector3.one;
            if(complete)
            {
                view.dialogueText.text = "고장 났을 때도 함께 안전하게 대처했어!";
                SetPanelVisible(view.dialoguePanel,true);
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
            string source=view.dialogueText.text;
            bool changed=dialogueRun!=view.lesson.RunVersion||dialogueSource!=source||dialogueSpeaker!=who;
            if(changed)
            {
                worldPlacementReady=friendPlacementReady=false;
                Transitions?.FadeContent(view.dialoguePanel);
                Transitions?.FadeContent(speakerTab);
                dialogueRun=view.lesson.RunVersion;dialogueSource=source;dialogueSpeaker=who;
                dialoguePages=ElevatorDialogueText.Pages(view.dialogueText,source);dialoguePage=0;
                revealStarted=Time.time;revealComplete=!Application.isPlaying||f.IsCinematic||f.Step==ElevatorStep.Call||f.Step==ElevatorStep.Unanswered||f.Step==ElevatorStep.Retry;
            }
            view.dialogueText.text=dialoguePages[dialoguePage];
            UpdateDialogueReveal();
            UpdateThought();
            Layout();
            Transitions?.End();
        }

        public bool TryReadDialogue()
        {
            if(!view.lesson||view.lesson.CinematicBlocking||screenOnly||complete||!view.dialoguePanel.gameObject.activeInHierarchy)return false;
            if(!revealComplete){revealComplete=true;UpdateDialogueReveal();return true;}
            if(dialoguePage+1>=dialoguePages.Length)return false;
            Transitions?.Begin();
            dialoguePage++;view.dialogueText.text=dialoguePages[dialoguePage];
            revealStarted=Time.time;revealComplete=false;UpdateDialogueReveal();Layout();Transitions?.End();return true;
        }

        void UpdateDialogueReveal()
        {
            if(!view.dialogueText)return;
            int count=revealComplete?int.MaxValue:Mathf.Max(1,(int)((Time.time-revealStarted)*42));
            if(count>=view.dialogueText.text.Length)revealComplete=true;
            view.dialogueText.maxVisibleCharacters=revealComplete?int.MaxValue:count;
            if(dialogueReader)
            {
                bool canRead=!view.lesson.CinematicBlocking&&!complete&&!screenOnly&&(!revealComplete||dialoguePage+1<dialoguePages.Length);
                dialogueReader.interactable=canRead;dialogueReader.targetGraphic.raycastTarget=canRead;
            }
            if(readingStatus)
            {
                string action=!revealComplete?"선택하면 전체 표시":dialoguePage+1<dialoguePages.Length?"선택하면 다음 문장":"";
                readingStatus.text=(dialoguePages.Length>1?$"{dialoguePage+1} / {dialoguePages.Length}  ":"")+action;
                readingStatus.gameObject.SetActive(!complete&&!screenOnly&&!view.lesson.Flow.IsCinematic&&!string.IsNullOrEmpty(readingStatus.text));
            }
        }

        void PrepareDialogueReader()
        {
            if(!dialogueReader)
            {
                var r=(RectTransform)new GameObject("DialogueReader",typeof(RectTransform)).transform;r.SetParent(view.dialoguePanel,false);
                r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=Color.clear;image.raycastTarget=true;
                dialogueReader=r.gameObject.AddComponent<UnityEngine.UI.Button>();dialogueReader.targetGraphic=image;dialogueReader.transition=UnityEngine.UI.Selectable.Transition.None;
                dialogueReader.onClick.AddListener(()=>TryReadDialogue());
            }
            if(!readingStatus)
            {
                var r=(RectTransform)new GameObject("ReadingStatus",typeof(RectTransform)).transform;r.SetParent(view.dialoguePanel,false);
                r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
                readingStatus=r.gameObject.AddComponent<TextMeshProUGUI>();readingStatus.font=view.font;readingStatus.color=Ink;readingStatus.alignment=TextAlignmentOptions.MidlineRight;
            }
            ElevatorTypography.Apply(readingStatus,ElevatorTypography.HintSize);
            readingStatus.rectTransform.sizeDelta=new Vector2(view.dialoguePanel.rect.width-100,34);
            readingStatus.rectTransform.anchoredPosition=new Vector2(0,-view.dialoguePanel.rect.height*.5f+25);
            UpdateDialogueReveal();
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
            label.color = Paper; ElevatorTypography.Apply(label,ElevatorTypography.ChoiceSize);
            if(storybook)storybook.SkinButton(button);
        }

        public void ShowThought(string value)
        {
            value = value ?? "";
            if (observed == value){ElevatorThoughtHeaderPolicy.Hide(view.thoughtPanel);return;}
            Transitions?.Begin();
            observed = value;
            if(!string.IsNullOrEmpty(value)||!string.IsNullOrEmpty(baseThought))Transitions?.FadeContent(view.thoughtPanel);
            UpdateThought();
            LayoutHud();
            Transitions?.End();
        }

        void UpdateThought()
        {
            var text = string.IsNullOrEmpty(observed) ? baseThought : observed;
            view.thought.text = text;
            view.thought.maxVisibleCharacters = int.MaxValue;
            thoughtHeading.text = "";
            thoughtHeading.gameObject.SetActive(false);
            ElevatorThoughtHeaderPolicy.Hide(view.thoughtPanel);
            SetPanelVisible(view.thoughtPanel,!complete && !string.IsNullOrEmpty(text));
        }

        public void SetPanelVisible(RectTransform panel,bool visible)
        {
            if(Transitions)Transitions.Visibility(panel,visible);else panel.gameObject.SetActive(visible);
            if(panel==view.dialoguePanel&&speakerTab)
            {if(Transitions)Transitions.Visibility(speakerTab,visible);else speakerTab.gameObject.SetActive(visible);}
        }

        public void Restore()
        {
            historyPage = 0; placedRun = -1; observed = baseThought = "";dialogueRun=-1;dialogueSource=null;
            thoughtPlacementReady=friendPlacementReady=worldPlacementReady=false;
            if(storybook)storybook.HighlightHistory(-1);
            if(notebook)notebook.gameObject.SetActive(false);
            UpdateThought();
        }

        public void ShowHistory()
        {
            Transitions?.Begin();
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
            if(storybook)storybook.HighlightHistory(page);
            Transitions?.End();
        }

        public void Layout()
        {
            if(!view || !hud || !view.playerCamera)return;
            Transitions?.Begin();
            var root = (RectTransform)view.transform;
            root.sizeDelta = new Vector2(660,620);
            root.localScale = Vector3.one * worldPixelSize;
            const float width = ElevatorDialogueText.PanelWidth;
            view.dialogueText.fontSize=ElevatorDialogueText.BodySize;view.dialogueText.lineSpacing=9;view.dialogueText.alignment=TextAlignmentOptions.MidlineLeft;
            float body = Mathf.Max(102, view.dialogueText.GetPreferredValues(view.dialogueText.text,ElevatorDialogueText.TextWidth,0).y+12);
            float height = Mathf.Max(230,body+100);
            view.dialoguePanel.sizeDelta = new Vector2(width,height);
            view.dialoguePanel.anchoredPosition = Vector2.zero;
            view.dialogueText.rectTransform.sizeDelta = new Vector2(width-56,body);
            view.dialogueText.rectTransform.anchoredPosition = new Vector2(0,-12);
            ElevatorTypography.Apply(speaker,ElevatorTypography.SpeakerSize);
            speaker.textWrappingMode=TextWrappingModes.NoWrap;
            speakerTab.sizeDelta = new Vector2(Mathf.Clamp(speaker.GetPreferredValues(speaker.text,float.PositiveInfinity,float.PositiveInfinity).x+56,132,Mathf.Max(360,width-48)),54);
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
                ElevatorTypography.Apply(label,complete?ElevatorTypography.NotebookButtonSize:button.name=="DialogueNext"?ElevatorTypography.NextSize:ElevatorTypography.ChoiceSize);
                if(complete)label.text = i==0 ? "우리가 해낸 일" : "다시 해보기";
                float padding=buttonWidth<300?38:storybook?64:44;
                float minimumHeight=complete?82:button.name=="DialogueNext"?84:104;
                float buttonHeight=Mathf.Max(minimumHeight,label.GetPreferredValues(label.text,buttonWidth-padding,0).y+34);
                r.sizeDelta=new Vector2(buttonWidth,buttonHeight);
                r.anchoredPosition=complete?new Vector2((i-.5f)*227,0):new Vector2(0,-buttonY-buttonHeight*.5f);
                r.localPosition=new Vector3(r.localPosition.x,r.localPosition.y,-1);
                label.rectTransform.sizeDelta=r.sizeDelta-new Vector2(44,20);
                buttonY+=buttonHeight+18;
            }
            if(!complete)view.actionPanel.sizeDelta=new Vector2(width,Mathf.Max(0,buttonY-18));
            view.actionPanel.anchoredPosition=complete?new Vector2(0,-216):new Vector2(0,-height*.5f-40);
            if(storybook)storybook.LayoutWorld();
            PrepareDialogueReader();
            LayoutHud();
            PositionWorld();
            layoutAspect=view.playerCamera.aspect;layoutWidth=Screen.width;layoutHeight=Screen.height;
            Transitions?.End();
        }

        void LayoutHud()
        {
            bool descriptionsOnly = view.lesson.scenario;
            hud.gameObject.SetActive(true);
            var camera = view.playerCamera;
            bool xr = XRSettings.isDeviceActive;
            hud.renderMode = xr ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            hud.worldCamera = camera;
            hud.planeDistance = .4f;
            var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 1;
            var hudRect = (RectTransform)hud.transform;
            float width = xr ? 720*camera.aspect : hudRect.rect.width;
            if(xr)
            {
                var hr = (RectTransform)hud.transform; hr.sizeDelta=new Vector2(width,720);
                PositionHud();
            }
            mission.anchorMin=mission.anchorMax=mission.pivot=new Vector2(0,1);
            mission.gameObject.SetActive(!complete&&!descriptionsOnly);
            ElevatorTypography.Apply(view.title,ElevatorTypography.HeadingSize);
            ElevatorTypography.Apply(view.subtitle,ElevatorTypography.ObjectiveSize);
            float missionWidth=Mathf.Min(430,width-48);
            float objectiveHeight=Mathf.Max(38,view.subtitle.GetPreferredValues(view.subtitle.text,missionWidth-40,0).y+8);
            mission.sizeDelta=new Vector2(missionWidth,objectiveHeight+58);mission.anchoredPosition=new Vector2(24,-22);
            view.title.rectTransform.sizeDelta=new Vector2(mission.rect.width-40,30);view.title.rectTransform.anchoredPosition=new Vector2(0,mission.rect.height*.5f-24);
            view.subtitle.rectTransform.sizeDelta=new Vector2(mission.rect.width-40,objectiveHeight);view.subtitle.rectTransform.anchoredPosition=new Vector2(0,-19);
            progress.rectTransform.anchorMin=progress.rectTransform.anchorMax=progress.rectTransform.pivot=new Vector2(1,1);
            ElevatorTypography.Apply(progress,ElevatorTypography.HintSize);
            float progressWidth=progress.GetPreferredValues(progress.text,float.PositiveInfinity,float.PositiveInfinity).x+20;
            progress.rectTransform.anchoredPosition=new Vector2(-24,-24);progress.rectTransform.sizeDelta=new Vector2(progressWidth,38);
            progress.gameObject.SetActive(!complete&&!descriptionsOnly&&width>missionWidth+progressWidth+80);
            if(progressBackdrop){progressBackdrop.anchorMin=progressBackdrop.anchorMax=progressBackdrop.pivot=Vector2.one;progressBackdrop.anchoredPosition=new Vector2(-14,-16);progressBackdrop.sizeDelta=new Vector2(progressWidth+20,54);progressBackdrop.gameObject.SetActive(progress.gameObject.activeSelf);}
            controls.rectTransform.anchorMin=controls.rectTransform.anchorMax=new Vector2(.5f,0);
            ElevatorTypography.Apply(controls,ElevatorTypography.ControlsSize);
            controls.text=width<850?"WASD 이동 · 마우스 둘러보기\n중앙 조준 후 클릭 · Esc 일시정지":"WASD 이동  ·  마우스로 둘러보기  ·  중앙 조준 후 클릭  ·  Esc 일시정지";
            float controlsWidth=Mathf.Max(120,width-64);
            float controlsHeight=Mathf.Max(32,controls.GetPreferredValues(controls.text,controlsWidth,0).y+6);
            controls.rectTransform.anchoredPosition=new Vector2(0,12+controlsHeight*.5f);controls.rectTransform.sizeDelta=new Vector2(controlsWidth,controlsHeight);
            controls.gameObject.SetActive(!xr&&!complete&&!descriptionsOnly);
            if(controlsBackdrop){controlsBackdrop.anchorMin=controlsBackdrop.anchorMax=new Vector2(.5f,0);controlsBackdrop.pivot=new Vector2(.5f,0);controlsBackdrop.anchoredPosition=new Vector2(0,6);controlsBackdrop.sizeDelta=new Vector2(Mathf.Min(controlsWidth+20,controls.GetPreferredValues().x+32),controlsHeight+12);controlsBackdrop.gameObject.SetActive(controls.gameObject.activeSelf);}
            float thoughtWidth=Mathf.Min(380,width-48);
            float bodyWidth=thoughtWidth*.72f;
            float bodyHeight=Mathf.Max(56,view.thought.GetPreferredValues(view.thought.text,bodyWidth,0).y);
            float thoughtHeight=Mathf.Max(200,(bodyHeight+58)/.70f);
            if(layoutWidth!=Screen.width||layoutHeight!=Screen.height||Mathf.Abs(layoutAspect-camera.aspect)>.001f)thoughtPlacementReady=false;
            var placement=thoughtPlacementReady?thoughtAnchor:new Vector2(1,.55f);
            view.thoughtPanel.anchorMin=view.thoughtPanel.anchorMax=placement;
            view.thoughtPanel.pivot=new Vector2(placement.x,.5f);
            view.thoughtPanel.sizeDelta=new Vector2(thoughtWidth,thoughtHeight);
            view.thoughtPanel.anchoredPosition=new Vector2(placement.x>0?-24:24,0);
            var cloud=view.thoughtPanel.GetComponent<CorridorBubbleGraphic>();
            if(cloud&&!cloud.thoughtCloud){cloud.thoughtCloud=true;cloud.speechTail=false;cloud.SetVerticesDirty();}
            thoughtHeading.alignment=TextAlignmentOptions.Center;
            thoughtHeading.rectTransform.sizeDelta=new Vector2(bodyWidth,24);thoughtHeading.rectTransform.anchoredPosition=new Vector2(0,bodyHeight*.5f+12);
            view.thought.rectTransform.sizeDelta=new Vector2(bodyWidth,bodyHeight);view.thought.rectTransform.anchoredPosition=new Vector2(0,-16);
            if(storybook)storybook.LayoutThought(!string.IsNullOrEmpty(observed));
            ElevatorThoughtHeaderPolicy.Hide(view.thoughtPanel);
            PlaceThoughtOutsideDialogue();
            layoutHudSize=hudRect.rect.size;
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
            if(!worldPlacementReady||dialoguePlacedStep!=step||dialoguePlacedRun!=lesson.RunVersion)
            {
                var facing=anchor-view.playerCamera.transform.position;
                if(facing.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(facing,Vector3.up);
                dialogueRotation=rotation;
                float distance=Vector3.Distance(anchor,view.playerCamera.transform.position);
                dialogueScale=Vector3.one*(worldPixelSize*Mathf.Clamp(distance/2f,1,maximumReadabilityScale));
                worldPlacementReady=true;dialoguePlacedStep=step;dialoguePlacedRun=lesson.RunVersion;
            }
            // Keep the speaker anchor, but do not turn/resize cards as the player looks around.
            transform.SetPositionAndRotation(anchor,dialogueRotation);
            transform.localScale=dialogueScale;
            if(!complete&&speaker.text=="친구")
            {
                if(!friendPlacementReady)
                {
                    var face=transform.InverseTransformPoint(lesson.friend.position+Vector3.up*1.04f);
                    float radius=.31f/transform.lossyScale.x;
                    friendCardShift=Mathf.Max(0,face.x+radius+view.dialoguePanel.rect.width*.5f+20);
                    friendPlacementReady=true;
                }
                float shift=friendCardShift;
                // Move the card, not its world speaker/player anchor, clear of the face silhouette.
                view.dialoguePanel.anchoredPosition=new Vector2(shift,0);
                var actions=view.actionPanel.anchoredPosition;actions.x=shift;view.actionPanel.anchoredPosition=actions;
                var tab=speakerTab.anchoredPosition;
                tab.x=-view.dialoguePanel.rect.width*.5f+88+speakerTab.rect.width*.5f+shift;
                speakerTab.anchoredPosition=tab;
            }
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
            worldGroup.alpha=transition||paused||f.IsShotCutscene?0:1;worldGroup.interactable=!view.lesson.CinematicBlocking;worldGroup.blocksRaycasts=worldGroup.interactable;
            hudGroup.alpha=f.IsCinematic||paused?0:1;hudGroup.interactable=false;hudGroup.blocksRaycasts=false;
            if(notebook)notebook.gameObject.SetActive(complete&&!paused);
        }

        public void Tick()
        {
            Transitions?.Begin();
            // CanvasScaler may resize after the screen/camera change was observed.
            // Reflow against the actual canvas once its new size has been applied.
            if(Mathf.Abs(layoutAspect-view.playerCamera.aspect)>.001f||layoutWidth!=Screen.width||layoutHeight!=Screen.height||layoutHudSize!=((RectTransform)hud.transform).rect.size)Layout();
            else PositionWorld();
            PositionHud();SetCinematicVisibility();
            ElevatorThoughtHeaderPolicy.Hide(view.thoughtPanel);
            if(view.lesson.Flow!=null&&!view.lesson.IsPaused)UpdateDialogueReveal();
            Transitions?.End();
            Transitions?.Advance(view.lesson.IsPaused?0:Time.unscaledDeltaTime);
        }

        bool ScreenRect(RectTransform rect,out Rect bounds)
        {
            rect.GetWorldCorners(projectionCorners);Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            foreach(var corner in projectionCorners)
            {
                var p=view.playerCamera.WorldToScreenPoint(corner);
                if(p.z<=view.playerCamera.nearClipPlane){bounds=default;return false;}
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return true;
        }
        static float Overlap(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
        void PlaceThoughtOutsideDialogue()
        {
            if(!hud||!view.thoughtPanel.gameObject.activeInHierarchy||thoughtPlacementReady)return;
            readingObstacles.Clear();
            if(view.dialoguePanel.gameObject.activeInHierarchy&&ScreenRect(view.dialoguePanel,out var dialogue))readingObstacles.Add(dialogue);
            if(view.actionPanel.gameObject.activeInHierarchy)foreach(var button in view.ChoiceButtons)
                if(button&&button.IsActive()&&ScreenRect((RectTransform)button.transform,out var action))readingObstacles.Add(action);
            var actor=view.lesson.friend;
            if(actor&&actor.gameObject.activeInHierarchy)
            {
                var face=view.playerCamera.WorldToScreenPoint(actor.position+Vector3.up*1.04f);
                var edge=view.playerCamera.WorldToScreenPoint(actor.position+Vector3.up*1.04f+view.playerCamera.transform.right*.34f);
                if(face.z>0){float radius=Mathf.Abs(edge.x-face.x);readingObstacles.Add(new Rect(face.x-radius,face.y-radius,2*radius,2*radius));}
            }
            var map=view.lesson.presentation&&view.lesson.presentation.space?view.lesson.presentation.space.sportsCentre:null;
            if(map&&map.sideIntercom)
            {
                var p=view.playerCamera.WorldToScreenPoint(map.sideIntercom.position);
                if(p.z>0)readingObstacles.Add(new Rect(p.x-28,p.y-28,56,56));
            }
            var panel=view.thoughtPanel;
            var previousPlacement=panel.anchorMin;
            var candidates=new[]{new Vector2(1,.55f),new Vector2(0,.55f),new Vector2(1,.24f),new Vector2(0,.24f),new Vector2(1,.78f),new Vector2(0,.78f)};
            float best=float.MaxValue;Vector2 selected=candidates[0];
            var hudCamera=hud.renderMode==RenderMode.ScreenSpaceOverlay?null:view.playerCamera;
            foreach(var candidate in candidates)
            {
                panel.anchorMin=panel.anchorMax=candidate;panel.pivot=new Vector2(candidate.x,.5f);panel.anchoredPosition=new Vector2(candidate.x>0?-24:24,0);
                panel.GetWorldCorners(projectionCorners);Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
                foreach(var corner in projectionCorners){var p=RectTransformUtility.WorldToScreenPoint(hudCamera,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
                var rect=Rect.MinMaxRect(min.x,min.y,max.x,max.y);float area=rect.width*rect.height;
                float score=(area-Overlap(rect,new Rect(12,12,Screen.width-24,Screen.height-24)))*4;
                foreach(var avoid in readingObstacles)score+=Overlap(rect,Rect.MinMaxRect(avoid.xMin-16,avoid.yMin-16,avoid.xMax+16,avoid.yMax+16));
                score+=Vector2.Distance(candidate,new Vector2(1,.55f))*40;
                // Keep the chosen side until another position is meaningfully clearer.
                if(candidate!=previousPlacement)score+=area*.04f;
                if(score<best){best=score;selected=candidate;}
            }
            panel.anchorMin=panel.anchorMax=selected;panel.pivot=new Vector2(selected.x,.5f);panel.anchoredPosition=new Vector2(selected.x>0?-24:24,0);
            thoughtAnchor=selected;thoughtPlacementReady=true;
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
