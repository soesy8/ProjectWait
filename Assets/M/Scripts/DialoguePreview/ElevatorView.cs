// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorView.cs. Original is not modified.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Flow content stays here; spatial presentation owns world dialogue, the HUD and notebook.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ElevatorView : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Camera playerCamera;
        public ElevatorLesson lesson;
        public ElevatorSpatialUI spatial;
        public TMP_Text title, subtitle, thought;
        public RectTransform actionPanel, thoughtPanel, developerPanel, dialoguePanel, infoPanel;
        public TMP_Text dialogueText, infoText;
        public int InteractionRevision { get; private set; }
        public readonly List<Button> ChoiceButtons = new List<Button>();
        readonly List<Action> actions = new List<Action>();
        readonly List<string> labels = new List<string>();
        static readonly Color Ink=new Color(.28f,.19f,.14f), Paper=new Color(1,.965f,.86f), Teal=new Color(.16f,.48f,.44f), Mint=new Color(.79f,.89f,.76f);
        [SerializeField] TMP_Text speaker, progress, controls;
        [SerializeField] RectTransform mission;
        bool developer;
        int historyPage;
        string normalLine, normalSpeaker;
        Vector3 origin;
        float layoutAspect=-1, revealStarted;
        bool observing;
        CanvasGroup cinematicGroup;
        Quaternion anchorRotation=Quaternion.identity;
        public void Initialize(ElevatorLesson value) {lesson=value;PrepareCompactPresentation();Reanchor(playerCamera.transform.position);}
        public void Build()
        {
            for(int i=transform.childCount-1;i>=0;i--)DestroyImmediate(transform.GetChild(i).gameObject);
            var canvas=GetComponent<Canvas>();if(!canvas)canvas=gameObject.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=playerCamera;canvas.sortingOrder=20;
            if(!GetComponent<CanvasScaler>())gameObject.AddComponent<CanvasScaler>();
            if(!GetComponent<GraphicRaycaster>())gameObject.AddComponent<GraphicRaycaster>();
            if(!GetComponent<TrackedDeviceGraphicRaycaster>())gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            var root=(RectTransform)transform;root.sizeDelta=new Vector2(1280,720);
            mission=Card("Chapter",root,new Vector2(420,78),Vector2.zero,Paper);
            title=Label("StoryTitle",mission,new Vector2(376,26),new Vector2(0,21),15);title.color=Teal;title.text="멈춰도 함께, 안전하게";
            subtitle=Label("Objective",mission,new Vector2(376,38),new Vector2(0,-11),23);
            progress=Label("TeamProgress",root,new Vector2(400,36),Vector2.zero,18);progress.alignment=TextAlignmentOptions.MidlineRight;
            progress.color=Ink;
            controls=Label("Controls",root,new Vector2(850,22),Vector2.zero,13);controls.alignment=TextAlignmentOptions.Center;
            controls.text="마우스로 둘러보기 · 중앙을 맞추고 클릭 · R 다시 시작";
            dialoguePanel=Card("StoryCard",root,new Vector2(980,190),Vector2.zero,Paper);
            speaker=Label("Speaker",dialoguePanel,new Vector2(880,24),new Vector2(0,62),17);speaker.color=Teal;
            dialogueText=Label("Dialogue",dialoguePanel,new Vector2(880,64),new Vector2(0,19),25);
            actionPanel=Rect("Actions",dialoguePanel,new Vector2(900,58),new Vector2(0,-57));
            // Serialized aliases retain compatibility with the scene's existing lesson tools.
            thoughtPanel=dialoguePanel;thought=dialogueText;infoPanel=dialoguePanel;infoText=dialogueText;
            developerPanel=Card("DeveloperPanel",root,new Vector2(800,300),new Vector2(0,60),Paper);
            var devTitle=Label("DevTitle",developerPanel,new Vector2(740,36),new Vector2(0,112),20);devTitle.text="개발 도구 · F8 닫기";
            string[] names={"로비","공 받기","슛 재현","정지","통화","함께 대기","구조 안내"};
            for(int i=0;i<names.Length;i++){int n=i;MakeButton("Stage"+i,developerPanel,new Vector2(170,48),new Vector2(-285+(i%4)*190,48-(i/4)*64),names[i],()=>lesson.DeveloperJump(n));}
            MakeButton("NoResponse",developerPanel,new Vector2(460,48),new Vector2(0,-92),"비상통화 응답 / 무응답",()=>{lesson.simulateNoResponse=!lesson.simulateNoResponse;Refresh();});
            developerPanel.gameObject.SetActive(false);
            PrepareCompactPresentation();Reanchor(playerCamera.transform.position);
        }
        public void PrepareCompactPresentation()
        {
            if(!speaker||!mission) return; // The migration rebuilds the obsolete UI exactly once in Edit Mode.
            title.text="멈춰도 함께, 안전하게";
            developerPanel.Find("Stage2").GetComponentInChildren<TMP_Text>().text="짧은 도입 연출";
            developerPanel.Find("Stage5").GetComponentInChildren<TMP_Text>().text="친구에게 말 걸기";
            for(int i=0;i<7;i++){int n=i;Bind(developerPanel.Find("Stage"+i).GetComponent<Button>(),()=>lesson.DeveloperJump(n));}
            Bind(developerPanel.Find("NoResponse").GetComponent<Button>(),()=>{lesson.simulateNoResponse=!lesson.simulateNoResponse;Refresh();});
            ApplyCompactLayout();
        }
        static void Bind(Button b,Action a){b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>a());}
        public void Reanchor(Vector3 value){origin=value;anchorRotation=Quaternion.Euler(0,playerCamera.transform.eulerAngles.y,0);ApplyCompactLayout();}
        public void ApplyCompactLayout()
        {
            if(spatial){spatial.Layout();return;}
            if(!playerCamera||!mission)return;
            float aspect=playerCamera.aspect;
            var root=(RectTransform)transform;root.sizeDelta=new Vector2(720*aspect,720);
            bool wide=aspect>=1.6f;
            float width=wide?600:Mathf.Min(850,root.rect.width-60);
            float bodyHeight=Mathf.Clamp(dialogueText.GetPreferredValues(dialogueText.text,width-48,0).y+8,72,145);
            float height=bodyHeight+62+(actions.Count>0?82:0);
            float missionWidth=wide?420:360;
            mission.sizeDelta=new Vector2(missionWidth,78);
            mission.anchoredPosition=new Vector2(-root.rect.width*.5f+22+missionWidth*.5f,303);
            title.rectTransform.sizeDelta=new Vector2(missionWidth-44,26);
            subtitle.rectTransform.sizeDelta=new Vector2(missionWidth-44,38);
            progress.rectTransform.anchoredPosition=new Vector2(root.rect.width*.5f-232,313);
            progress.gameObject.SetActive(aspect>1.45f);
            dialoguePanel.sizeDelta=new Vector2(width,height);dialoguePanel.anchoredPosition=new Vector2(wide?root.rect.width*.5f-width*.5f-30:0,-328+height*.5f);
            speaker.rectTransform.sizeDelta=new Vector2(width-48,30);speaker.rectTransform.anchoredPosition=new Vector2(0,height*.5f-25);
            dialogueText.rectTransform.sizeDelta=new Vector2(width-48,bodyHeight);dialogueText.rectTransform.anchoredPosition=new Vector2(0,height*.5f-46-bodyHeight*.5f);
            actionPanel.anchoredPosition=new Vector2(0,-height*.5f+47);actionPanel.sizeDelta=new Vector2(width-48,72);
            for(int i=0;i<ChoiceButtons.Count;i++)
            {
                float w=ChoiceButtons.Count==1?Mathf.Min(width-48,790):(width-64)*.5f;
                var rect=(RectTransform)ChoiceButtons[i].transform;rect.sizeDelta=new Vector2(w,72);
                rect.anchoredPosition=new Vector2((i-(ChoiceButtons.Count-1)*.5f)*(w+16),0);
                ChoiceButtons[i].GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta=new Vector2(w-32,66);
            }
            controls.rectTransform.anchoredPosition=new Vector2(0,-348);
            controls.rectTransform.sizeDelta=new Vector2(root.rect.width-32,24);
            controls.gameObject.SetActive(!XRSettings.isDeviceActive);
            developerPanel.sizeDelta=new Vector2(Mathf.Min(800,root.rect.width-50),300);
            layoutAspect=aspect;PositionCanvas();
        }
        void PositionCanvas()
        {
            const float distance=1.25f;
            bool desktop=!XRSettings.isDeviceActive;
            var rotation=desktop?playerCamera.transform.rotation:anchorRotation;
            var anchor=desktop?playerCamera.transform.position:origin;
            transform.SetPositionAndRotation(anchor+rotation*Vector3.forward*distance,rotation);
            float height=2*distance*Mathf.Tan(playerCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            transform.localScale=Vector3.one*(height*.96f/720);
        }
        void LateUpdate()
        {
            if(!playerCamera||!speaker)return;
            if(spatial){spatial.Tick();return;}
            if(Mathf.Abs(playerCamera.aspect-layoutAspect)>.001f)ApplyCompactLayout();else PositionCanvas();
            if(lesson&&lesson.Flow!=null)
            {
                SetCinematicVisibility();
                if(!observing)dialogueText.maxVisibleCharacters=Mathf.Max(1,(int)((Time.unscaledTime-revealStarted)*48));
            }
        }
        public void ToggleDeveloper(){developer=!developer;developerPanel.gameObject.SetActive(developer);InteractionRevision++;}
        public void ShowThought(string value)
        {
            // A clicked object's card persists until another click or explicit close;
            // hovering elsewhere must not replace it with a second description panel.
            if(lesson&&lesson.Flow!=null&&lesson.Flow.HasObjectPreview)value="";
            if(spatial){spatial.ShowThought(value);return;}
            if(!speaker)return;
            observing=!string.IsNullOrEmpty(value);
            dialogueText.text=observing?value:normalLine;speaker.text=observing?"살펴보기":normalSpeaker;
            dialogueText.maxVisibleCharacters=int.MaxValue;
            if(!observing)revealStarted=Time.unscaledTime-100;
            ApplyCompactLayout();
        }
        public void InvokeChoice(int index)
        {
            if(lesson&&lesson.CinematicBlocking)return;
            if(index>=0&&index<ChoiceButtons.Count&&ChoiceButtons[index]&&ChoiceButtons[index].isActiveAndEnabled&&ChoiceButtons[index].interactable)ChoiceButtons[index].onClick.Invoke();
        }
        public void Refresh()
        {
            if(!lesson||lesson.Flow==null||!speaker)return;
            spatial?.Transitions?.Begin();
            var previousButtons=new List<Button>(ChoiceButtons);
            var previousLabels=new List<string>(labels);
            actions.Clear();labels.Clear();ChoiceButtons.Clear();observing=false;
            var f=lesson.Flow;string objective="",line="",who="친구";
            switch(f.Step)
            {
                case ElevatorStep.StartupFade:objective="시작 준비";who="안내";break;
                case ElevatorStep.GymDeparture:objective="친구와 체육관에서 나가기";who="친구";line=ElevatorOpeningSequence.FriendLine;break;
                case ElevatorStep.Entering:objective="친구와 함께 타기";who="안내";break;
                case ElevatorStep.Exiting:objective="구조요원을 따라 나가기";who="안내";break;
                case ElevatorStep.Lobby:
                    objective="엘리베이터 부르기";line=lesson.settings.lobbyDialogue;Add("1   엘리베이터 타기",lesson.Continue);break;
                case ElevatorStep.Boarding:
                    objective=f.BallReceived?"1층 버튼 누르기":f.DestinationSelected?"친구의 공 받기":"1층 누르기 · 공 받기\n순서는 자유예요";
                    line=f.BallReceived?ElevatorDialogue.ChooseFloor:f.DestinationSelected?lesson.settings.handoffDialogue:ElevatorDialogue.Boarding;
                    if(!f.DestinationSelected)Add("1   1층 누르기",lesson.SelectFloor);
                    if(!f.BallReceived)Add(f.DestinationSelected?"1   공 받기":"2   공 받기",lesson.Continue);
                    break;
                case ElevatorStep.Proposal:
                    objective="친구의 슛 이야기";line=ElevatorDialogue.ShotStory;break;
                case ElevatorStep.PosterPointing:
                    objective="친구에게 말 걸기";who="안내";line="친구가 뛰려고 해요.\n주변의 안전 안내를 살펴보세요.";break;
                case ElevatorStep.PosterWarning:
                    objective="친구에게 알려 주기";who="나";line="여기서는 뛰면 위험해.\n슛은 밖에서 하자.";break;
                case ElevatorStep.Travel:
                    objective="1층으로 내려가는 중";line="알았어. 밖에서 하자!";break;
                case ElevatorStep.Jump:
                    objective="친구가 슛 동작을 흉내 내요";line=ElevatorDialogue.ShotGesture;break;
                case ElevatorStep.Stopped:
                    objective="비상통화 버튼을\n눌러 주세요.";
                    if(f.Phase==InteractionPhase.Dialogue)line=ElevatorDialogue.Stopped;
                    else{who="도움 요청";line=ElevatorDialogue.HelpHint(f.HintLevel);}
                    break;
                case ElevatorStep.Connecting:
                    objective="비상통화 연결 중";who="안내";line="관리실에 연결 중입니다.\n잠시만 기다려 주세요.";break;
                case ElevatorStep.Unanswered:
                    objective="휴대폰으로\n119에 연락해 주세요.";who="안내";line="비상통화가 연결되지 않아요.\n휴대폰으로 119에 연락하세요.\n실제 전화가 아닌 체험 통화예요.";Add("1   119에 연락하기 · 체험",lesson.Phone);break;
                case ElevatorStep.CallQuestion:
                    objective="무슨 일이 생겼는지 알리기";who=ElevatorDialogue.Caller(f.UsedPhone);line=ElevatorDialogue.Greeting(f.UsedPhone);break;
                case ElevatorStep.CallResponse:
                    objective="멈춘 상황과 인원 알리기";who="나";line=lesson.settings.reportDialogue;break;
                case ElevatorStep.CallLiftQuestion:
                    objective="승강기 번호 확인하기";who=ElevatorDialogue.Caller(f.UsedPhone);line=ElevatorDialogue.LocationQuestion;break;
                case ElevatorStep.CallIdentityGuide:
                    objective="건물 이름과 번호 찾기";who=ElevatorDialogue.Caller(f.UsedPhone);line=ElevatorDialogue.IdentityGuide;Add("찾아볼게요",lesson.Continue);break;
                case ElevatorStep.CallSafetyAdvice:
                    objective="안전 안내 확인하기";Add("알겠어요",lesson.Continue);break;
                case ElevatorStep.Call:
                    who=ElevatorDialogue.Caller(f.UsedPhone);
                    if(f.SearchingIdentity){objective="건물 이름과 번호 찾기";who="위치 알리기";line=ElevatorDialogue.IdentitySearchHint(f.HintLevel);}
                    else if(f.ReviewingIdentity){objective="위치 알리기";line=lesson.settings.building+"\n"+lesson.settings.liftId;who="엘리베이터 안내판";Add("위치 알려 주기",lesson.Report);Add("다시 살펴보기",lesson.ReinspectIdentity);}
                    else{objective="안전 안내 확인하기";line=ElevatorDialogue.SafetyAdvice;Add("알겠어요",lesson.Advice);}break;
                case ElevatorStep.DoorPosterPointing:
                    objective="친구와 이야기해 보세요.";who="친구와 함께 기다리기";
                    line="친구가 문에 기대어 쉬려고 해요.\n어떻게 대답할지 생각해 보세요.";
                    break;
                case ElevatorStep.Waiting:
                    objective="친구와 함께 기다리기";
                    if(!f.Reassured){line=ElevatorDialogue.FriendWaiting;Add("1   "+lesson.settings.waitDialogue,lesson.Reassure);}
                    else{string[] replies={"고마워. 이제 좀 안심이 돼.","헤헤, 너랑 하니까 더 재밌었어.","좋아! 다음엔 패스 연습도 하자.","응, 나도 네 옆에 있을게."};line=replies[Mathf.Clamp(f.Encouragements,0,3)];if(f.Encouragements<3)Add(f.Encouragements==0?"1   오늘 농구 재밌었지?":f.Encouragements==1?"1   다음에 또 같이 하자.":"1   조금만 더 같이 기다리자.",lesson.Encourage);}break;
                case ElevatorStep.WaitingFade:
                    objective="친구와 함께 기다리기";who="친구";line="이제 여기서 기다리면 되겠지?";break;
                case ElevatorStep.Retry:
                    objective="기다릴 자리 다시 생각하기";who="잠깐!";line=f.DoorRetry?ElevatorDialogue.RetryDoor:ElevatorDialogue.RetryExit;Add("다시 살펴보기",lesson.Retry);break;
                case ElevatorStep.Rescue:
                    objective="구조요원 안내 기다리기";who="구조요원";line=f.ReadyToExit?ElevatorDialogue.RescueReady:ElevatorDialogue.RescueChecking;if(f.ReadyToExit)Add("1   구조요원 따라 나가기",lesson.Continue);break;
                case ElevatorStep.ReturnLobby:
                    objective="계단으로 1층 내려가기";who="구조요원";line=ElevatorDialogue.Stairs;Add("1   계단으로 내려가기",lesson.Continue);break;
                case ElevatorStep.Returning:objective="계단으로 내려가는 중";who="안내";line="계단으로 1층에 내려가요.";break;
                case ElevatorStep.GroundFloor:
                    objective="친구와 밖으로 나가기";line=ElevatorDialogue.GroundFloor;Add("1   친구와 밖으로 나가기",lesson.Continue);break;
                case ElevatorStep.GoingHome:objective="친구와 집으로";who="안내";break;
                case ElevatorStep.Complete:
                    objective="함께 안전하게 나왔어요";who="친구";line=ElevatorDialogue.Complete;Add("1   우리가 함께한 일",ShowHistory);Add("2   다시 해보기",lesson.Restart);break;
            }
            if(f.Step!=ElevatorStep.DoorPosterPointing&&ElevatorDoorSafetyCutscene.Describe(lesson,out var safetySpeaker,out var safetyLine))
            {who=safetySpeaker;line=safetyLine;objective=f.IsCallCutscene||f.AwaitingCallContinue?"현재 위치를 알리고 안내 듣기":"문에서 떨어져 안전하게 기다리기";}
            if(lesson.scenario){lesson.scenario.Describe(ref who,ref line);if(lesson.scenario.objectGuidedScene&&!f.AwaitingCallContinue&&f.Step!=ElevatorStep.Call&&f.Step!=ElevatorStep.DoorPosterPointing&&f.Step!=ElevatorStep.Retry&&f.Step!=ElevatorStep.Complete){actions.Clear();labels.Clear();}}
            if(lesson.presentation&&lesson.presentation.rescueTeam)lesson.presentation.rescueTeam.Describe(lesson,ref who,ref line);
            if(f.HasObjectPreview)
            {
                DescribeObjectPreview(f.PreviewClue.Value,out who,out line,out string actionLabel);
                objective=f.CanConfirmObject?"친구에게 할 말":"살펴보기";
                actions.Clear();labels.Clear();
                if(f.CanConfirmObject)
                {
                    if(f.CanChooseSafeWall)Add(ElevatorDialogue.SafeReply,lesson.ChooseSafeWall);
                    Add(ElevatorDialogue.UnsafeReply,lesson.ChooseUnsafeWall);
                }
                Add(f.CanConfirmObject?"조금 더 살펴보기":"닫기",lesson.CloseObjectPreview);
            }
            subtitle.text=objective;normalLine=line;normalSpeaker=who;speaker.text=who;dialogueText.text=line;
            progress.text=(f.HelpRequested?"<color=#297A70>● 도움 요청</color>":"○ 도움 요청")+"   "+(f.LocationReported?"<color=#297A70>● 위치 전달</color>":"○ 위치 전달")+"   "+(f.Reassured?"<color=#297A70>● 함께 대기</color>":"○ 함께 대기");
            if(!spatial){revealStarted=Time.unscaledTime;dialogueText.maxVisibleCharacters=Application.isPlaying?0:int.MaxValue;}
            int revision=f.Revision,run=lesson.RunVersion;
            for(int i=0;i<actions.Count;i++)
            {
                var action=actions[i];string name=f.Step==ElevatorStep.Call&&!f.ReviewingIdentity?"DialogueNext":"Choice"+i;
                Button b=i<previousButtons.Count&&i<previousLabels.Count&&previousLabels[i]==labels[i]&&previousButtons[i]&&previousButtons[i].name==name?previousButtons[i]:null;
                if(b){previousButtons[i]=null;b.onClick.RemoveAllListeners();}
                else{b=MakeButton(name,actionPanel,new Vector2(790,56),Vector2.zero,labels[i],()=>{});spatial?.Transitions?.Visibility((RectTransform)b.transform,true);}
                b.onClick.AddListener(()=>{if(!lesson.CinematicBlocking&&lesson.Flow.Revision==revision&&lesson.RunVersion==run){bool explicitAction=lesson.Flow.HasObjectPreview||lesson.Flow.ReviewingIdentity||lesson.Flow.AwaitingCallContinue||lesson.Flow.Step==ElevatorStep.DoorPosterPointing||lesson.Flow.Step==ElevatorStep.Retry;if(!explicitAction&&spatial&&spatial.TryReadDialogue())return;action();}});
                ChoiceButtons.Add(b);
            }
            foreach(var old in previousButtons)
            {
                if(!old)continue;
                if(spatial&&spatial.Transitions)spatial.Transitions.RetireChoice((RectTransform)old.transform);
                else{old.gameObject.SetActive(false);if(Application.isPlaying)Destroy(old.gameObject);else DestroyImmediate(old.gameObject);}
            }
            if(spatial)spatial.SetPanelVisible(actionPanel,actions.Count>0);else actionPanel.gameObject.SetActive(actions.Count>0);
            if(spatial)spatial.Present(who,line,objective);
            ApplyCompactLayout();SetCinematicVisibility();InteractionRevision++;
            if(lesson.scenario)lesson.scenario.RefreshWorldMaterials();
            spatial?.Transitions?.End();
        }
        public void SetCinematicVisibility()
        {
            if(spatial){spatial.SetCinematicVisibility();return;}
            if(!cinematicGroup)cinematicGroup=GetComponent<CanvasGroup>();
            if(!cinematicGroup)cinematicGroup=gameObject.AddComponent<CanvasGroup>();
            bool visible=!lesson.CinematicBlocking;cinematicGroup.alpha=visible?1:0;cinematicGroup.interactable=visible;cinematicGroup.blocksRaycasts=visible;
        }
        void Add(string label,Action callback){labels.Add(label);actions.Add(callback);}
        void DescribeObjectPreview(ElevatorClue clue,out string who,out string line,out string action)
        {
            var f=lesson.Flow;
            action="";
            who="살펴본 물체";
            line=ElevatorDialogue.DescribeObject(lesson,clue);
            switch(clue)
            {
                case ElevatorClue.Notice:
                    who="안전 포스터";
                    if(f.Step==ElevatorStep.PosterPointing)line="엘리베이터 안에서는\n뛰거나 공을 튀기지 마세요.";
                    break;
                case ElevatorClue.Door:
                    who="닫힌 출입문";
                    break;
                case ElevatorClue.Friend:
                    who="친구";
                    break;
                case ElevatorClue.Intercom:
                    who="비상통화 장치";
                    action="이 장치로 도움 요청하기";break;
                case ElevatorClue.Phone:
                    who="휴대폰";
                    action="휴대폰으로 119 모의 신고";break;
                case ElevatorClue.FloorButtons:case ElevatorClue.FrontFloorButtons:
                    who="층 버튼";break;
                case ElevatorClue.Display:who="층 표시기";break;
                case ElevatorClue.Identity:who="엘리베이터 안내판";break;
                case ElevatorClue.FrontIntercom:who="전면 비상벨";break;
                case ElevatorClue.FacilityGuide:who="층별 시설 안내";break;
                case ElevatorClue.Advertisement:who="농구 교실 안내";break;
            }
        }
        void ShowHistory()
        {
            if(spatial){spatial.ShowHistory();return;}
            string[] lines={"도움 요청 성공!  비상통화로 우리 상황을 알렸어요.","위치 전달 성공!  벽의 표시에서 건물명과 승강기 번호를 찾았어요.","함께 대기 성공!  놀란 친구를 안심시키고 안내를 기다렸어요."};
            ShowThought(lines[historyPage%3]);speaker.text="우리가 해낸 일 · "+(historyPage%3+1)+" / 3";historyPage++;
        }
        public void RestoreThoughtLayout(){historyPage=0;observing=false;if(spatial)spatial.Restore();}
        public void CollectInteractiveButtons(List<UnityEngine.UI.Button> result)
        {
            result.Clear();GetComponentsInChildren(true,result);
            if(spatial&&spatial.notebook)result.AddRange(spatial.notebook.GetComponentsInChildren<UnityEngine.UI.Button>(true));
        }
        RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return r;
        }
        RectTransform Card(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
        {
            var r=Rect(name,parent,size,pos);var g=r.gameObject.AddComponent<CorridorBubbleGraphic>();g.color=color;g.borderColor=Ink;g.cornerRadius=14;g.borderWidth=1.7f;g.extrusion=2;g.bevelWidth=1;g.shadowOffset=new Vector2(0,-4);g.raycastTarget=false;return r;
        }
        TMP_Text Label(string name,Transform parent,Vector2 size,Vector2 pos,float point)
        {
            var r=Rect(name,parent,size,pos);r.localPosition+=Vector3.back*.5f;var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=point;t.color=Ink;t.fontStyle=FontStyles.Normal;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;return t;
        }
        Button MakeButton(string name,Transform parent,Vector2 size,Vector2 pos,string text,Action callback)
        {
            var r=Card(name,parent,size,pos,Mint);r.localPosition+=Vector3.back*.5f;var graphic=r.GetComponent<CorridorBubbleGraphic>();graphic.borderColor=Teal;graphic.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=graphic;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.86f,.96f,.91f);colors.pressedColor=new Color(.65f,.83f,.76f);colors.selectedColor=Color.white;b.colors=colors;
            var t=Label("Label",r,size-new Vector2(32,8),Vector2.zero,21);t.text=text;t.alignment=TextAlignmentOptions.Center;
            if(spatial)spatial.StyleButton(b);
            b.onClick.AddListener(()=>callback());return b;
        }
    }
}
