// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorSpatialUI.cs. Original is not modified.
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // VR dialogue keeps its placement pose; PC captions retain their existing following behaviour.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ElevatorSpatialUI : MonoBehaviour
    {
        public ElevatorView view;
        public ElevatorStorybookUI storybook;
        public ElevatorUIFocusWindow focusWindow;
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
        [Range(1.2f,1.6f)] public float readingDistance=1.4f;
        [Min(.1f)] public float notebookDistance = 1.0f;
        public Vector3 notebookOffset = new Vector3(0, -.22f, 0);
        public Vector3 notebookTilt = new Vector3(12, 0, 0);
        [Min(.1f)] public float notebookScale = 1.12f;
        public Vector3 rescuerOffset = new Vector3(1.40f, 1.67f, -2.05f);
        [Min(.1f)] public float maximumReadabilityScale=1.45f;
        [Header("Non-blocking dialogue")]
        public bool compactDesktopDialogue=true;
        [Range(.6f,1)] public float desktopCaptionScale=.82f;

        static readonly Color Ink = new Color(.28f,.19f,.14f);
        static readonly Color Paper = new Color(1,.965f,.86f);
        static readonly Color Teal = new Color(.16f,.48f,.44f);
        static readonly Color Mint = new Color(.79f,.89f,.76f);
        string baseThought = "", observed = "";
        string statusHeading="",statusAction="";
        float revealStarted;
        int historyPage;
        bool screenOnly, complete;
        ElevatorStep placedStep = (ElevatorStep)(-1);
        int placedRun = -1;
        CanvasGroup worldGroup, hudGroup;
        const float HudFadeSeconds=.5f, StatusFadeSeconds=1f;
        float hudOpacity=1,hudFadeStart=1,hudFadeGoal=1,hudFadeElapsed=HudFadeSeconds;
        int hudVisibilityRun=-1;
        CanvasGroup statusGroup;
        float statusOpacity,statusFadeStart,statusFadeGoal,statusFadeElapsed=StatusFadeSeconds;
        bool statusWanted;
        int statusVisibilityRun=-1;
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
        bool thoughtPlacementReady;
        Vector2 thoughtAnchor;
        bool layoutXR, stereoReady;
        Rect hudReadingArea;
        int subtitleLayoutVersion=-1;
        public bool StatusFits { get; private set; }
        public bool DialogueDeferredForSubtitle { get; private set; }
        ElevatorCinematicOverlay Overlay => view&&view.lesson&&view.lesson.presentation&&view.lesson.presentation.space?view.lesson.presentation.space.overlay:null;
        StereoUISafeArea.LayoutStamp stereoLayout;
        readonly System.Collections.Generic.List<UnityEngine.UI.Graphic> readingGraphics = new System.Collections.Generic.List<UnityEngine.UI.Graphic>();
        readonly System.Collections.Generic.List<Vector3> readingGeometryCorners = new System.Collections.Generic.List<Vector3>();
        Bounds readingBounds;
        readonly ElevatorSpeechAnchor speechAnchor = new ElevatorSpeechAnchor();
        ElevatorPlayerSpeechPanel playerSpeech;
        public bool IsPlayerSpeaking=>!complete&&speaker&&speaker.text=="나"&&view&&view.dialogueText&&!string.IsNullOrEmpty(view.dialogueText.text);
        public ElevatorCinematicOverlay CinematicOverlay=>Overlay;
        public Transform SpeechSource => speechAnchor.Source;
        public Vector3 SpeechSourcePoint => speechAnchor.SourcePoint;
        public bool StereoPlacementFits { get; private set; }
        Quaternion dialogueRotation;
        Vector3 dialogueScale;
        Vector3 dialoguePosition;
        float columnHalfHeight,columnHalfWidth;
        public float PlacedReadingDistance {get;private set;}
        readonly System.Collections.Generic.List<ElevatorUIPlacement.Region> protectedRegions = new System.Collections.Generic.List<ElevatorUIPlacement.Region>();
        public Rect PlacementViewportRect { get; private set; }
        public float PlacementWeightedOverlap { get; private set; }
        public Rect HudViewportRect { get; private set; }
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
            // A reading card stays available while the player selects the next object.
            screenOnly = false;
            observed = "";
            baseThought = "";
            if(f.SearchingIdentity&&!f.HasObjectPreview&&f.Step!=ElevatorStep.CallIdentityGuide)
            {
                // The call cinematic has finished; the player now finds the clue.
                view.dialogueText.text = ElevatorDialogue.IdentitySearchHint(f.HintLevel);
            }
            speaker.text = who;
            view.actionPanel.SetParent(complete ? rightPage : view.transform, false);
            view.actionPanel.localScale = Vector3.one;
            if(complete)
            {
                view.dialogueText.text = ElevatorDialogue.Complete;
                SetPanelVisible(view.dialoguePanel,false);
                bool[] done = {f.HelpRequested, f.LocationReported, f.SafeWallReached, f.ReadyToExit};
                for(int i=0;i<completionMarks.Length;i++)
                {
                    completionMarks[i].text = done[i] ? "완료" : "돌아보기";
                    completionMarks[i].color = done[i] ? Teal : Ink;
                }
                notebookCaption.text = "안에서는 뛰지 않아요.";
                notebookDetail.text = "";
            }
            notebook.gameObject.SetActive(complete);
            progress.gameObject.SetActive(!complete);
            string source=view.dialogueText.text;
            bool changed=dialogueRun!=view.lesson.RunVersion||dialogueSource!=source||dialogueSpeaker!=who;
            if(changed)
            {
                Transitions?.FadeContent(view.dialoguePanel);
                Transitions?.FadeContent(speakerTab);
                dialogueRun=view.lesson.RunVersion;dialogueSource=source;dialogueSpeaker=who;
                // Show the whole short line immediately; reading never consumes a
                // world-selection click or requires a separate page/close action.
                dialoguePages=new[]{source};dialoguePage=0;
                revealStarted=Time.time;revealComplete=true;
            }
            view.dialogueText.text=ElevatorSpeechAnchor.VisibleLine(view.lesson,who,dialoguePages[dialoguePage]);
            SetPanelVisible(view.dialoguePanel,!complete&&!screenOnly&&!string.IsNullOrEmpty(view.dialogueText.text));
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
                dialogueReader.interactable=false;dialogueReader.targetGraphic.raycastTarget=false;
            }
            if(readingStatus)
            {
                string action=!revealComplete?"눌러서 모두 보기":dialoguePage+1<dialoguePages.Length?"눌러서 다음 글 보기":"";
                if(statusPage!=dialoguePage||statusPageCount!=dialoguePages.Length||statusRevealed!=revealComplete)
                {
                    readingStatus.text=(dialoguePages.Length>1?$"{dialoguePage+1} / {dialoguePages.Length}  ":"")+action;
                    statusPage=dialoguePage;statusPageCount=dialoguePages.Length;statusRevealed=revealComplete;
                }
                readingStatus.gameObject.SetActive(!complete&&!screenOnly&&!view.lesson.Flow.IsCinematic&&!string.IsNullOrEmpty(readingStatus.text));
            }
        }

        int statusPage=-1,statusPageCount=-1;
        bool statusRevealed;

        void PrepareDialogueReader()
        {
            // Preserve any existing reader object, but remove its input surface.
            // Actual choice buttons live outside the dialogue panel and stay active.
            foreach(var graphic in view.dialoguePanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            var reader=view.dialoguePanel.Find("DialogueReader");
            if(reader){dialogueReader=reader.GetComponent<UnityEngine.UI.Button>();if(dialogueReader)dialogueReader.interactable=false;}
            var status=view.dialoguePanel.Find("ReadingStatus");
            if(status){readingStatus=status.GetComponent<TMP_Text>();status.gameObject.SetActive(false);}
            UpdateDialogueReveal();
        }

        public void StyleButton(UnityEngine.UI.Button button)
        {
            var g = button.GetComponent<CorridorBubbleGraphic>();
            g.color = Teal; g.borderColor = Paper; g.borderWidth = 2.5f;
            g.cornerRadius = 15; g.extrusion = 8; g.bevelWidth = 4;
            g.shadowOffset = new Vector2(4,-10); g.shadowOpacity = .16f;
            var label = button.GetComponentInChildren<TMP_Text>();
            label.text = ElevatorDialogue.WithoutShortcut(label.text);
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
            thoughtPlacementReady=false;
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
            thoughtPlacementReady=false;
            speechAnchor.Reset();
            if(focusWindow)focusWindow.ResetWindow();
            if(storybook)storybook.HighlightHistory(-1);
            if(notebook)notebook.gameObject.SetActive(false);
            UpdateThought();
        }

        public void ShowHistory()
        {
            Transitions?.Begin();
            string[] titles = {"01  도움을 요청했어요", "02  위치를 전달했어요", "03  문에서 떨어져 기다렸어요", "04  안내에 따라 이동했어요"};
            string[] details = {
                "엘리베이터가 멈췄다고 알리고,\n친구와 둘이 있다고 말했어요.",
                "식별판에서 건물 이름과\n승강기 번호를 찾아 알려 줬어요.",
                "문에서 떨어진 옆 벽에서\n친구와 함께 기다렸어요.",
                "안전한지 확인한 뒤,\n구조요원을 따라 나왔어요."
            };
            int page = historyPage++ % 4;
            notebookCaption.text = titles[page];
            notebookDetail.text = details[page];
            for(int i=0;i<completionRows.Length;i++)completionRows[i].GetComponent<CorridorBubbleGraphic>().color = i==page ? Mint : Paper;
            if(storybook)storybook.HighlightHistory(page);
            Transitions?.End();
        }

        public void Layout() => Layout(XRSettings.isDeviceActive);

        // Explicit mode also permits recorded-frustum layout checks without starting an XR provider.
        public void Layout(bool xr)
        {
            if(!view || !hud || !view.playerCamera)return;
            Transitions?.Begin();
            var root = (RectTransform)view.transform;
            root.sizeDelta = new Vector2(660,620);
            root.localScale = Vector3.one * (xr?VRUIReadingLayout.Distance/VRUIReadingLayout.PointsPerRadian*StereoUISafeArea.VisualScale:worldPixelSize);
            const float width = ElevatorDialogueText.PanelWidth;
            view.dialogueText.fontSize=view.lesson.Flow!=null&&view.lesson.Flow.ReviewingIdentity?34:ElevatorDialogueText.BodySize;view.dialogueText.lineSpacing=9;view.dialogueText.alignment=TextAlignmentOptions.MidlineLeft;
            float body = Mathf.Max(102, view.dialogueText.GetPreferredValues(view.dialogueText.text,ElevatorDialogueText.TextWidth,0).y+12);
            float height = Mathf.Max(210,body+80);
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
                ElevatorTypography.Apply(label,complete?24:button.name=="DialogueNext"?ElevatorTypography.NextSize:ElevatorTypography.ChoiceSize);
                if(complete)label.text = i==0 ? "함께한 일 보기" : "다시 해보기";
                float padding=buttonWidth<300?38:storybook?64:44;
                float minimumHeight=complete?82:76;
                // Storybook artwork insets the final label by 26 pixels vertically.
                // Measure enough padding for that final rectangle, not the pre-skin size.
                float buttonHeight=Mathf.Max(minimumHeight,label.GetPreferredValues(label.text,buttonWidth-padding,0).y+32);
                r.sizeDelta=new Vector2(buttonWidth,buttonHeight);
                r.anchoredPosition=complete?new Vector2((i-.5f)*227,0):new Vector2(0,-buttonY-buttonHeight*.5f);
                r.localPosition=new Vector3(r.localPosition.x,r.localPosition.y,-1);
                label.rectTransform.sizeDelta=r.sizeDelta-new Vector2(44,20);
                buttonY+=buttonHeight+12;
            }
            if(!complete)view.actionPanel.sizeDelta=new Vector2(width,Mathf.Max(0,buttonY-12));
            view.actionPanel.anchoredPosition=complete?new Vector2(0,-216):new Vector2(0,-height*.5f-24);
            if(storybook)storybook.LayoutWorld(xr);
            PrepareDialogueReader();
            // Rotate the whole reading column around its centre, not around the body
            // card, so billboarding never swings the buttons around the player.
            float top=height*.5f+48;
            float bottom=-height*.5f-(view.ChoiceButtons.Count>0?24+view.actionPanel.rect.height:34)-16;
            float centre=(top+bottom)*.5f;
            columnHalfHeight=(top-bottom)*.5f*worldPixelSize;
            columnHalfWidth=(width*.5f+24)*worldPixelSize;
            if(!complete)
            {
                view.dialoguePanel.anchoredPosition=new Vector2(0,-centre);
                var actions=view.actionPanel.anchoredPosition;actions.y-=centre;view.actionPanel.anchoredPosition=actions;
            }
            if(focusWindow)focusWindow.Refresh();
            if(xr)MeasureReadingBounds();
            LayoutHud(xr);
            PositionWorld();
            layoutXR=xr;
            layoutAspect=view.playerCamera.aspect;layoutWidth=Screen.width;layoutHeight=Screen.height;
            Transitions?.End();
        }

        void LayoutHud() => LayoutHud(XRSettings.isDeviceActive);

        public void LayoutHud(bool xr)
        {
            bool descriptionsOnly = view.lesson.scenario;
            hud.gameObject.SetActive(true);
            var camera = view.playerCamera;
            if(xr)thoughtPlacementReady=false;
            hud.enabled=!xr||stereoReady;
            hud.renderMode = xr ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            hud.worldCamera = camera;
            hud.planeDistance = .4f;
            var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 1;
            var hudRect = (RectTransform)hud.transform;
            float width = hudRect.rect.width;
            if(xr)
            {
                PositionHud();
                width=hudRect.rect.width;
            }
            float visual=xr?StereoUISafeArea.VisualScale:1;
            mission.localScale=progress.rectTransform.localScale=controls.rectTransform.localScale=Vector3.one*visual;
            var thoughtParent=xr?view.transform:hud.transform;
            if(view.thoughtPanel.parent!=thoughtParent)
            {view.thoughtPanel.SetParent(thoughtParent,false);view.thoughtPanel.localPosition=Vector3.zero;view.thoughtPanel.localRotation=Quaternion.identity;}
            view.thoughtPanel.localScale=Vector3.one*(xr?1:visual);
            if(progressBackdrop)progressBackdrop.localScale=Vector3.one*visual;
            if(controlsBackdrop)controlsBackdrop.localScale=Vector3.one*visual;
            bool status=DescribeStatus(out var heading,out var action);
            bool show=status&&!complete&&!LookingAtDialogue();
            SetStatusVisibility(show);
            // Keep the outgoing words until their own one-second fade finishes.
            if(status||statusOpacity<=0){statusHeading=heading;statusAction=action;}
            view.title.text=statusHeading;view.subtitle.text=statusAction;
            ElevatorTypography.Apply(view.title,ElevatorTypography.HeadingSize);
            ElevatorTypography.Apply(view.subtitle,ElevatorTypography.ObjectiveSize);
            float missionWidth=Mathf.Min(xr?VRUIReadingLayout.StatusWidth:430,width-48);
            float objectiveHeight=Mathf.Max(38,view.subtitle.GetPreferredValues(view.subtitle.text,missionWidth-40,0).y+8);
            float headingHeight=xr?Mathf.Max(30,view.title.GetPreferredValues(view.title.text,missionWidth-40,0).y+4):30;
            mission.sizeDelta=new Vector2(missionWidth,objectiveHeight+(xr?headingHeight+30:58));
            // Keep the screen-space status fixed even when targets, text or gaze change.
            mission.anchorMin=mission.anchorMax=mission.pivot=new Vector2(1,0);
            mission.anchoredPosition=new Vector2(-24,24);
            var hudSize=new Vector2(Mathf.Max(1,width),Mathf.Max(1,hudRect.rect.height));
            var size=mission.sizeDelta*visual/hudSize;
            var lowerRight=new Vector2(1,0)+mission.anchoredPosition/hudSize;
            HudViewportRect=new Rect(lowerRight-new Vector2(size.x,0),size);
            view.title.rectTransform.sizeDelta=new Vector2(mission.rect.width-40,headingHeight);view.title.rectTransform.anchoredPosition=new Vector2(0,mission.rect.height*.5f-(xr?14+headingHeight*.5f:24));
            view.subtitle.rectTransform.sizeDelta=new Vector2(mission.rect.width-40,objectiveHeight);view.subtitle.rectTransform.anchoredPosition=new Vector2(0,xr?-mission.rect.height*.5f+14+objectiveHeight*.5f:-19);
            progress.rectTransform.anchorMin=progress.rectTransform.anchorMax=progress.rectTransform.pivot=new Vector2(1,1);
            ElevatorTypography.Apply(progress,ElevatorTypography.HintSize);
            float progressWidth=progress.GetPreferredValues(progress.text,float.PositiveInfinity,float.PositiveInfinity).x+20;
            progress.rectTransform.anchoredPosition=new Vector2(-24,-24);progress.rectTransform.sizeDelta=new Vector2(progressWidth,38);
            progress.gameObject.SetActive(!complete&&!descriptionsOnly&&width>missionWidth+progressWidth+80);
            if(progressBackdrop){progressBackdrop.anchorMin=progressBackdrop.anchorMax=progressBackdrop.pivot=Vector2.one;progressBackdrop.anchoredPosition=new Vector2(-14,-16);progressBackdrop.sizeDelta=new Vector2(progressWidth+20,54);progressBackdrop.gameObject.SetActive(progress.gameObject.activeSelf);}
            controls.rectTransform.anchorMin=controls.rectTransform.anchorMax=new Vector2(.5f,0);
            ElevatorTypography.Apply(controls,ElevatorTypography.ControlsSize);
            controls.text=xr?"왼손 스틱으로 이동 · 메뉴 버튼으로 잠시 멈춤":width<850?"WASD 이동 · 마우스로 둘러보기\n가운데에 맞추고 클릭 · Esc 메뉴":"WASD 이동  ·  마우스로 둘러보기  ·  가운데에 맞추고 클릭  ·  Esc 메뉴";
            float controlsWidth=Mathf.Max(120,xr?Mathf.Min(520,width-64):width-64);
            float controlsHeight=Mathf.Max(32,controls.GetPreferredValues(controls.text,controlsWidth,0).y+6);
            controls.rectTransform.anchoredPosition=new Vector2(0,12+controlsHeight*.5f);controls.rectTransform.sizeDelta=new Vector2(controlsWidth,controlsHeight);
            controls.gameObject.SetActive(ShowInitialControls());
            if(controlsBackdrop){controlsBackdrop.anchorMin=controlsBackdrop.anchorMax=new Vector2(.5f,0);controlsBackdrop.pivot=new Vector2(.5f,0);controlsBackdrop.anchoredPosition=new Vector2(0,6);controlsBackdrop.sizeDelta=new Vector2(Mathf.Min(controlsWidth+20,controls.GetPreferredValues().x+32),controlsHeight+12);controlsBackdrop.gameObject.SetActive(controls.gameObject.activeSelf);}
            float thoughtWidth=Mathf.Min(xr?VRUIReadingLayout.StatusWidth:380,width-48);
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
            if(xr)PositionHud();
            PlaceThoughtOutsideDialogue(xr);
            layoutHudSize=hudRect.rect.size;
        }

        void PositionWorld()
        {
            if(IsPlayerSpeaking){EnsurePlayerSpeech().Refresh(XRSettings.isDeviceActive);return;}
            if(playerSpeech)playerSpeech.Hide();
            var lesson=view.lesson;
            var f=lesson.Flow;
            var step=f!=null?f.Step:ElevatorStep.Lobby;
            if(XRSettings.isDeviceActive)
            {
                stereoReady=StereoUISafeArea.TryCapture(view.playerCamera,out var frame);
                if(!stereoReady)return;
                PlaceStereoDialogue(frame,step);
            }
            else
            {
                var anchor=speechAnchor.Get(lesson,speaker.text,readingBounds,worldPixelSize);
                dialoguePosition=anchor.position;dialogueRotation=anchor.rotation;dialogueScale=Vector3.one*worldPixelSize;
                PlacedReadingDistance=Vector3.Distance(view.playerCamera.transform.position,dialoguePosition);
            }
            transform.SetPositionAndRotation(dialoguePosition,dialogueRotation);
            transform.localScale=dialogueScale;
            if(storybook)storybook.PointSpeechTail(speechAnchor.SourcePoint);
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

        ElevatorPlayerSpeechPanel EnsurePlayerSpeech()
        {
            if(playerSpeech)return playerSpeech;
            var go=new GameObject("Player speech screen panel",typeof(RectTransform));go.transform.SetParent(view.lesson.transform,false);
            playerSpeech=go.AddComponent<ElevatorPlayerSpeechPanel>();playerSpeech.Initialize(this);return playerSpeech;
        }

        public void SetCinematicVisibility() => SetCinematicVisibility(XRSettings.isDeviceActive);

        public void SetCinematicVisibility(bool vr)
        {
            if(!view.lesson||view.lesson.Flow==null)return;
            if(!worldGroup)worldGroup=GetComponent<CanvasGroup>();
            if(!worldGroup)worldGroup=gameObject.AddComponent<CanvasGroup>();
            if(!hudGroup)hudGroup=hud.GetComponent<CanvasGroup>();
            if(!hudGroup)hudGroup=hud.gameObject.AddComponent<CanvasGroup>();
            // Dialogue remains spatial during story beats; transitions use the existing comfort fade.
            var f=view.lesson.Flow;
            bool paused=view.lesson.IsPaused;
            bool invalidStereo=vr&&!stereoReady;
            bool covered=ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy||!view.lesson.SpawnReady||f.Step==ElevatorStep.StartupFade||Overlay&&Overlay.FadeAlpha>.01f;
            bool content=complete||!string.IsNullOrEmpty(view.dialogueText.text)||view.ChoiceButtons.Count>0;
            // All spoken lines, including cutscenes, use the source-anchored bubble.
            // The letterbox and full-screen comfort fade remain separate owners.
            DialogueDeferredForSubtitle=false;
            worldGroup.alpha=invalidStereo||covered||paused||!content?0:1;worldGroup.interactable=!invalidStereo&&!view.lesson.CinematicBlocking;worldGroup.blocksRaycasts=worldGroup.interactable;
            if(IsPlayerSpeaking)
            {
                worldGroup.alpha=0;worldGroup.interactable=worldGroup.blocksRaycasts=false;
                EnsurePlayerSpeech().Refresh(vr);
            }
            else if(playerSpeech)playerSpeech.Hide();
            float goal=f.IsCinematic?0:1;
            if(hudVisibilityRun!=view.lesson.RunVersion||!Application.isPlaying)
            {
                hudVisibilityRun=view.lesson.RunVersion;
                hudOpacity=hudFadeStart=hudFadeGoal=goal;hudFadeElapsed=HudFadeSeconds;
            }
            else if(hudFadeGoal!=goal)
            {
                hudFadeStart=hudOpacity;hudFadeGoal=goal;hudFadeElapsed=0;
            }
            hudGroup.alpha=paused?0:hudOpacity;hudGroup.interactable=false;hudGroup.blocksRaycasts=false;
            if(statusGroup)statusGroup.alpha=paused?0:statusOpacity;
            if(notebook)notebook.gameObject.SetActive(complete&&!paused);
        }

        bool ShowInitialControls()=>!complete&&!view.lesson.IsPaused&&view.lesson.Flow.Step==ElevatorStep.Lobby&&view.lesson.Flow.Elapsed<12;

        void SetStatusVisibility(bool visible)
        {
            if(!statusGroup)statusGroup=mission.GetComponent<CanvasGroup>();
            if(!statusGroup)statusGroup=mission.gameObject.AddComponent<CanvasGroup>();
            statusGroup.interactable=false;statusGroup.blocksRaycasts=false;
            // Other HUD hints still fade during cinematics. The goal's visibility
            // is independently owned by actual dialogue gaze, not that parent fade.
            // Canvas disabling for scene transitions and explicit pause still apply.
            statusGroup.ignoreParentGroups=true;
            if(statusVisibilityRun!=view.lesson.RunVersion)
            {
                statusVisibilityRun=view.lesson.RunVersion;
                statusOpacity=statusFadeStart=statusFadeGoal=0;statusWanted=false;statusFadeElapsed=StatusFadeSeconds;
            }
            if(statusWanted!=visible)
            {
                statusWanted=visible;statusFadeStart=statusOpacity;statusFadeGoal=visible?1:0;statusFadeElapsed=0;
            }
            if(visible||statusOpacity>0)mission.gameObject.SetActive(true);
            else mission.gameObject.SetActive(false);
            statusGroup.alpha=statusOpacity;
        }

        void AdvanceStatusVisibility(float seconds)
        {
            if(!statusGroup)return;
            statusFadeElapsed=Mathf.Min(StatusFadeSeconds,statusFadeElapsed+seconds);
            statusOpacity=Mathf.Lerp(statusFadeStart,statusFadeGoal,Mathf.SmoothStep(0,1,statusFadeElapsed/StatusFadeSeconds));
            statusGroup.alpha=statusOpacity;
            if(!statusWanted&&statusFadeElapsed>=StatusFadeSeconds)mission.gameObject.SetActive(false);
        }

        public void AdvanceHudVisibility(float seconds)
        {
            SetCinematicVisibility();
            if(view.lesson.IsPaused||seconds<0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            hudFadeElapsed=Mathf.Min(HudFadeSeconds,hudFadeElapsed+seconds);
            float t=Mathf.SmoothStep(0,1,hudFadeElapsed/HudFadeSeconds);
            hudOpacity=Mathf.Lerp(hudFadeStart,hudFadeGoal,t);
            hudGroup.alpha=hudOpacity;
            AdvanceStatusVisibility(seconds);
        }

        public void Tick()
        {
            Transitions?.Begin();
            RefreshSpeechClock();
            bool xr=XRSettings.isDeviceActive;
            bool projectionChanged=false;
            int subtitleVersion=Overlay?Overlay.SubtitleLayoutVersion:0;
            bool subtitleChanged=xr&&subtitleVersion!=subtitleLayoutVersion;
            subtitleLayoutVersion=subtitleVersion;
            if(xr)
            {
                stereoReady=StereoUISafeArea.TryCapture(view.playerCamera,out var frame);
                if(stereoReady)projectionChanged=stereoLayout.Update(view.playerCamera,frame);
                else stereoLayout.Reset();
            }
            if(projectionChanged||subtitleChanged||layoutXR!=xr)thoughtPlacementReady=false;
            // CanvasScaler may resize after the screen/camera change was observed.
            // Reflow against the actual canvas once its new size has been applied.
            if(projectionChanged||subtitleChanged||layoutXR!=xr||!xr&&(Mathf.Abs(layoutAspect-view.playerCamera.aspect)>.001f||layoutWidth!=Screen.width||layoutHeight!=Screen.height||layoutHudSize!=((RectTransform)hud.transform).rect.size))Layout();
            else PositionWorld();
            bool wantsStatus=DescribeStatus(out _,out _)&&!complete&&!LookingAtDialogue();
            if(statusWanted!=wantsStatus||statusVisibilityRun!=view.lesson.RunVersion||controls.gameObject.activeSelf!=ShowInitialControls())LayoutHud();
            PositionHud();AdvanceHudVisibility(Time.unscaledDeltaTime);
            ElevatorThoughtHeaderPolicy.Hide(view.thoughtPanel);
            if(view.lesson.Flow!=null&&!view.lesson.IsPaused)UpdateDialogueReveal();
            Transitions?.End();
            Transitions?.Advance(view.lesson.IsPaused?0:Time.unscaledDeltaTime);
            if(focusWindow)focusWindow.Advance(view.lesson.IsPaused?0:Time.unscaledDeltaTime);
        }

        bool LookingAtDialogue()
        {
            if(IsPlayerSpeaking)return playerSpeech&&playerSpeech.LookingAtPanel();
            if(screenOnly||!view.dialoguePanel.gameObject.activeInHierarchy||string.IsNullOrEmpty(view.dialogueText.text))return false;
            if(worldGroup&&worldGroup.alpha<=.01f)return false;
            var canvas=GetComponent<Canvas>();if(canvas&&!canvas.enabled)return false;
            var panel=view.dialoguePanel;var group=panel.GetComponent<CanvasGroup>();
            if(group&&group.alpha<=.01f)return false;
            // Head-forward is the viewing criterion, not "somewhere in either eye".
            // Speaker ribbons, choices, hand rays and other panels never hide the goal.
            var camera=view.playerCamera;var eye=camera.transform;
            var ray=new Ray(eye.position,eye.forward);
            return new Plane(panel.forward,panel.position).Raycast(ray,out var distance)&&
                distance>=camera.nearClipPlane&&distance<=camera.farClipPlane&&
                panel.rect.Contains((Vector2)panel.InverseTransformPoint(ray.GetPoint(distance)));
        }

        bool DescribeStatus(out string heading,out string action)
        {
            var f=view.lesson.Flow;heading="";action="";
            if(f==null)return false;
            if(f.ReviewingIdentity){heading="건물 이름과 번호를 찾았어요";action="‘위치 알려 주기’를 선택해\n통화로 알려 주세요.";return true;}
            if(f.AwaitingCallContinue){heading=f.UsedPhone?"119의 안내예요":"관리실의 안내예요";action=f.Step==ElevatorStep.CallIdentityGuide?"안내를 보면서\n문 옆의 식별판을 선택하세요.":"문에서 떨어져\n안전하게 기다려 주세요.";return true;}
            switch(f.Step)
            {
                case ElevatorStep.PosterPointing:
                    heading="친구가 뛰려고 해요";action="주변의 안전 안내를\n살펴보세요.";return true;
                case ElevatorStep.Connecting:
                    heading="비상통화 연결 중";action="관리실에 연결하고 있어요.\n잠시만 기다려 주세요.";return true;
                case ElevatorStep.Stopped:
                    heading="엘리베이터가 멈췄어요";action=ElevatorDialogue.HelpHint(f.HintLevel);return f.CanObserve;
                case ElevatorStep.Unanswered:
                    heading="비상통화에 응답이 없어요";action="휴대폰으로\n119에 연락해 주세요.";return true;
                case ElevatorStep.Call:
                    heading="현재 위치를 알려 주세요";action=ElevatorDialogue.IdentitySearchHint(f.HintLevel);return f.SearchingIdentity;
                case ElevatorStep.DoorLean:
                    heading="친구가 잠깐 쉬려고 해요";action="친구의 이야기를\n잠시 들어 주세요.";return true;
                case ElevatorStep.DoorPosterPointing:
                    heading="친구가 잠깐 쉬려고 해요";
                    action=f.CanConfirmObject?"친구에게 할 말을\n골라 주세요.":f.HintLevel>0?"친구와 이야기해 보세요.\n주변 안전 안내도 살펴볼 수 있어요.":"친구와 이야기해 보세요.";return true;
                case ElevatorStep.DoorPosterWarning:case ElevatorStep.SafeWallMove:
                    heading="친구가 문에서 떨어지고 있어요";action="옆 벽으로 이동 중이에요.\n잠시만 기다려 주세요.";return true;
                case ElevatorStep.SafeWallWaiting:case ElevatorStep.WaitingFade:
                    heading="문에서 떨어져 함께 기다려요";action="구조요원이 올 때까지\n여기서 함께 기다려요.";return true;
                case ElevatorStep.Rescue:
                    heading=f.ReadyToExit?"안전 확인이 끝났어요":"구조대가 도착했어요";
                    action=f.ReadyToExit?"구조요원의 안내를 따라\n함께 나가세요.":"밖으로 나가도 안전한지\n확인할 때까지 기다리세요.";return true;
                case ElevatorStep.Exiting:
                    heading="구조요원과 함께 나가고 있어요";action="안내를 따라\n천천히 이동하세요.";return true;
                default:return false;
            }
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
        void PlaceThoughtOutsideDialogue(bool xr)
        {
            if(!hud||!view.thoughtPanel.gameObject.activeInHierarchy||thoughtPlacementReady)return;
            if(xr){PlaceStereoThought();return;}
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
            VRUIReadingLayout.SetPlane(hud.transform,camera.transform);
            if(!StereoUISafeArea.TryCapture(camera,out var frame))
            {hud.enabled=false;return;}
            LayoutVRHud(frame);
        }

        public bool LayoutVRHud(StereoUISafeArea.Frame frame)
        {
            if(!hud||!view||!view.playerCamera)return false;
            VRUIReadingLayout.SetPlane(hud.transform,view.playerCamera.transform);
            var root=(RectTransform)hud.transform;
            if(!VRUIReadingLayout.Area(frame,root,out hudReadingArea,out _)){hud.enabled=false;StatusFits=false;return false;}
            stereoReady=true;
            hud.enabled=true;
            var statusArea=hudReadingArea;
            if(Overlay&&Overlay.TryGetSubtitleLocalRect(root,out var subtitle))statusArea=VRUIReadingLayout.Above(statusArea,subtitle);
            if(playerSpeech&&playerSpeech.TryGetExclusion(root,out var playerRegion))statusArea=VRUIReadingLayout.Above(statusArea,playerRegion);
            mission.localScale=Vector3.one*StereoUISafeArea.VisualScale;
            StatusFits=VRUIReadingLayout.Place(mission,root,statusArea,VRUIReadingLayout.StatusYaw,VRUIReadingLayout.StatusPitch,out _)&&
                VRUIReadingLayout.Project(frame,mission,out var projected,true);
            if(!StatusFits)VRUIReadingLayout.Place(mission,root,hudReadingArea,0,0,out _);
            if(VRUIReadingLayout.Project(frame,mission,out projected))HudViewportRect=projected;
            if(progress)VRUIReadingLayout.Place(progress.rectTransform,root,hudReadingArea,-16,20,out _);
            if(progressBackdrop)VRUIReadingLayout.Place(progressBackdrop,root,hudReadingArea,-16,20,out _);
            var controlsArea=hudReadingArea;
            if(Overlay&&Overlay.TryGetSubtitleLocalRect(root,out subtitle))controlsArea=VRUIReadingLayout.Above(controlsArea,subtitle);
            if(playerSpeech&&playerSpeech.TryGetExclusion(root,out playerRegion))controlsArea=VRUIReadingLayout.Above(controlsArea,playerRegion);
            if(controls)VRUIReadingLayout.Place(controls.rectTransform,root,controlsArea,0,-20,out _);
            if(controlsBackdrop)VRUIReadingLayout.Place(controlsBackdrop,root,controlsArea,0,-20,out _);
            return StatusFits;
        }

        bool StereoRectVisible(RectTransform rect,StereoUISafeArea.Frame frame)
        {
            if(!rect)return false;
            rect.GetWorldCorners(projectionCorners);
            foreach(var point in projectionCorners)if(!frame.Contains(point,0,0))return false;
            return true;
        }

        void MeasureReadingBounds()
        {
            readingGeometryCorners.Clear();
            bool found=false;
            MeasureReadingPanel(view.dialoguePanel,ref found);
            MeasureReadingPanel(speakerTab,ref found);
            if(view.ChoiceButtons.Count>0)MeasureReadingPanel(view.actionPanel,ref found);
            if(!found)readingBounds=new Bounds(Vector3.zero,new Vector3(columnHalfWidth*2/worldPixelSize,columnHalfHeight*2/worldPixelSize,0));
            // Include border extrusion/shadows as well as the actual name, portrait and choice rectangles.
            readingBounds.Expand(new Vector3(24,24,0));
        }

        void MeasureReadingPanel(RectTransform panel,ref bool found)
        {
            if(!panel)return;
            readingGraphics.Clear();panel.GetComponentsInChildren(false,readingGraphics);
            foreach(var graphic in readingGraphics)
            {
                if(!graphic.enabled||graphic.name=="TransitionText"||graphic.name.StartsWith("Retiring_"))continue;
                graphic.rectTransform.GetWorldCorners(projectionCorners);
                foreach(var point in projectionCorners)
                {
                    var local=transform.InverseTransformPoint(point);
                    readingGeometryCorners.Add(local);
                    if(found)readingBounds.Encapsulate(local);else{readingBounds=new Bounds(local,Vector3.zero);found=true;}
                }
            }
        }

        void PlaceStereoDialogue(StereoUISafeArea.Frame frame,ElevatorStep step)
        {
            float scale=VRUIReadingLayout.Distance/VRUIReadingLayout.PointsPerRadian*StereoUISafeArea.VisualScale;
            var anchor=speechAnchor.Get(view.lesson,speaker.text,readingBounds,scale);
            dialoguePosition=anchor.position;dialogueRotation=anchor.rotation;dialogueScale=Vector3.one*scale;
            var half=(Vector2)readingBounds.extents*scale;
            var centre=anchor.position+anchor.rotation*(readingBounds.center*scale);
            // Visibility is a diagnostic only; it cannot move a scene-anchored UI towards the head.
            StereoPlacementFits=frame.ProjectQuad(centre,anchor.rotation,half,out var rect);
            StereoPlacementFits=ProjectStereoReading(frame,centre,anchor.rotation,scale,StereoUISafeArea.Margin,out rect)&&StereoPlacementFits;
            PlacementViewportRect=rect;PlacementWeightedOverlap=0;
            PlacedReadingDistance=Vector3.Distance(view.playerCamera.transform.position,centre);
        }

        void RefreshSpeechClock()
        {
            if(dialogueSource==null||view.lesson.IsPaused)return;
            string line=ElevatorSpeechAnchor.VisibleLine(view.lesson,dialogueSpeaker,dialogueSource);
            if(view.dialogueText.text==line)return;
            view.dialogueText.text=line;view.dialogueText.maxVisibleCharacters=int.MaxValue;
            SetPanelVisible(view.dialoguePanel,!complete&&!screenOnly&&!string.IsNullOrEmpty(line));
        }

        void PlaceStereoThought()
        {
            var panel=view.thoughtPanel;
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;
            float unit=VRUIReadingLayout.Distance/VRUIReadingLayout.PointsPerRadian*StereoUISafeArea.VisualScale;
            panel.localPosition=new Vector3(-.95f,.12f,.30f)/unit;panel.localRotation=Quaternion.identity;
            thoughtPlacementReady=true;
        }

        bool ProjectStereoReading(StereoUISafeArea.Frame frame,Vector3 center,Quaternion rotation,float scale,float lower,out Rect bounds)
        {
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            foreach(var local in readingGeometryCorners)
            {
                var world=center+rotation*((local-readingBounds.center)*scale);
                if(!frame.Contains(world,StereoUISafeArea.Margin,lower)||!VRUIReadingLayout.Comfortable(view.playerCamera.transform,world)){bounds=default;return false;}
                for(int eye=0;eye<2;eye++)
                {
                    if(!frame.Project(world,eye,out var p)){bounds=default;return false;}
                    min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
            }
            bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return readingGeometryCorners.Count>0;
        }

        void AddStereoGuideRegion(StereoUISafeArea.Frame frame)
        {
            var guide=view.lesson.GetComponentInChildren<ElevatorTutorialPreview>(true);
            if(guide&&guide.IsVisible&&guide.InstructionPanel&&ProjectStereoRect(guide.InstructionPanel,frame,out var rect))
                protectedRegions.Add(new ElevatorUIPlacement.Region(rect,120,"Guide"));
        }

        bool ProjectStereoRect(RectTransform rect,StereoUISafeArea.Frame frame,out Rect bounds)
        {
            rect.GetWorldCorners(projectionCorners);
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            foreach(var point in projectionCorners)for(int eye=0;eye<2;eye++)
            {
                if(!frame.Project(point,eye,out var p)){bounds=default;return false;}
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return true;
        }
    }
}
