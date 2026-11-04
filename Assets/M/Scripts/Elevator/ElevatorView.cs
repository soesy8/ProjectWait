using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator
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
            controls.text="클릭으로 선택 · 우클릭을 누르고 둘러보기 · 숫자 1 / 2 선택 · R 처음부터";
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
            developerPanel.Find("Stage2").GetComponentInChildren<TMP_Text>().text="슛 이야기·포스터";
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
            if(spatial){spatial.ShowThought(value);return;}
            if(!speaker)return;
            observing=!string.IsNullOrEmpty(value);
            dialogueText.text=observing?value:normalLine;speaker.text=observing?"살펴보기 · 클릭 / 컨트롤러 선택":normalSpeaker;
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
                case ElevatorStep.Entering:objective="친구와 함께 탑승하기";line="친구와 함께 엘리베이터에 들어가요.";break;
                case ElevatorStep.Exiting:objective="안내를 따라 나가기";line="안내를 따라 천천히 밖으로 나가요.";break;
                case ElevatorStep.Lobby:
                    objective="3층 운동을 마치고, 1층으로";line=lesson.settings.lobbyDialogue;Add("1   친구와 1호기 타기",lesson.Continue);break;
                case ElevatorStep.Boarding:
                    if(!f.DestinationSelected){objective="낮은 측면 패널에서 1층 선택";who="나의 미션";line="목적지는 1층 주출입구예요.\n낮은 측면 패널에서\n1층 버튼을 선택해요.";Add("1   측면 패널에서 1층 누르기",lesson.SelectFloor);}
                    else{objective="공을 튀기지 않고 안고 있기";line=lesson.settings.handoffDialogue;Add("1   공 받아 안고 있기",lesson.Continue);}break;
                case ElevatorStep.Proposal:
                    objective="친구의 슛 이야기";line="아까 슛 멋있었지? 보여 줄게!";break;
                case ElevatorStep.PosterPointing:
                    objective="안전 포스터를 가리켜요";who="나";line="안에서는 뛰면 안 되는데…\n벽의 안전 포스터를 가리켜 알려 주자.";break;
                case ElevatorStep.PosterWarning:
                    objective="친구에게 뛰지 말라고 말해요";who="나";line="안에서 뛰면 안 돼. 하지 마!";break;
                case ElevatorStep.Travel:
                    objective="1층으로 내려가는 중";line="좋아, 슛은 밖에서 하자.\n공을 안고 천천히 내려가자.";break;
                case ElevatorStep.Jump:
                    objective="친구가 만류를 듣지 않고 슛을 흉내 내요";line="에이, 한 번만! 잘 봐.\n이렇게 뛰어서!";break;
                case ElevatorStep.Stopped:
                    objective="도움을 요청할 장치 찾기";
                    if(f.Phase==InteractionPhase.Dialogue)line="어? 왜 멈췄지?";
                    else{who="나의 미션";line=f.HintLevel==0?"도움을 요청할 장치를\n찾아 선택해 보세요.":"낮은 측면 비상벨을 찾아요.\n난간 위로 올라가지 않아요.";}
                    break;
                case ElevatorStep.Connecting:
                    objective="비상통화 연결 중";who="비상통화";line="관리실에 연결하고 있어요.\n응답을 기다려 주세요.";break;
                case ElevatorStep.Unanswered:
                    objective="다른 방법으로 도움 요청";who="나의 미션";line="응답이 없어요.\n휴대폰으로 119에 신고해요.\n이 체험은 모의 통화예요.";Add("1   휴대폰으로 119 모의 신고",lesson.Phone);break;
                case ElevatorStep.CallQuestion:
                    objective="무슨 일이 생겼는지 말하기";who=f.UsedPhone?"119 · 모의 통화":"관리실";line="연결됐습니다. 무슨 상황인가요?";break;
                case ElevatorStep.CallResponse:
                    objective="멈춘 상황과 인원 전달";who="나";line=lesson.settings.reportDialogue;break;
                case ElevatorStep.CallLiftQuestion:
                    objective="어느 승강기인지 확인하기";who=f.UsedPhone?"119 · 모의 통화":"관리실";line="어느 엘리베이터에 있나요?";break;
                case ElevatorStep.CallIdentityGuide:
                    objective="위치 표시가 있는 곳 듣기";who=f.UsedPhone?"119 · 모의 통화":"관리실";line="전면 비상벨 아래 식별판에\n건물명과 승강기 번호가 적혀 있어요.";break;
                case ElevatorStep.Call:
                    who=f.UsedPhone?"119 · 모의 통화":"관리실 · 통화 중";
                    if(!f.IdentityFound){objective="전면 비상벨 아래 식별판 확인";who="나의 미션";line="안내받은 식별판을 직접 찾아\n건물명과 승강기 번호를 확인해요.";}
                    else if(!f.LocationReported){objective="찾은 위치를 알려 주세요";line="찾았다!\n"+lesson.settings.building+"\n"+lesson.settings.liftId;who="나의 발견";Add("다음",lesson.Report);}
                    else{objective="상황을 보완하고 안내 듣기";line="위치를 확인했어요.\n문에서 떨어져 기다려 주세요.\n직원이 안전하게 안내할게요.";Add("다음",lesson.Advice);}break;
                case ElevatorStep.Waiting:
                    objective="친구와 함께 기다리기";
                    if(!f.Reassured){line="아직 조금 무서워…\n옆에서 같이 기다려 줄래?";Add("1   "+lesson.settings.waitDialogue,lesson.Reassure);}
                    else{string[] replies={"고마워. 네가 옆에 있으니까 마음이 놓여.","맞아, 아까 그 슛! 나도 깜짝 놀랐다니까.","다음에는 같이 패스 연습하자. 밖에서!","우리 둘이 잘 해냈네. 같이 기다려 줘서 고마워."};line=replies[Mathf.Clamp(f.Encouragements,0,3)];if(f.Encouragements<3)Add(f.Encouragements==0?"1   아까 그 슛, 정말 멋졌어!":f.Encouragements==1?"1   다음엔 밖에서 같이 연습하자.":"1   응, 우리 같이 기다리자.",lesson.Encourage);}break;
                case ElevatorStep.WaitingFade:
                    objective="문에서 떨어져 함께 기다리기";who="친구";line="도움을 요청했으니\n문을 건드리지 말고 기다리자.";break;
                case ElevatorStep.Retry:
                    objective=f.ProposalRetry?"Wait! 슛은 밖에서 해요":"문에서 한 걸음 떨어져요";who="잠깐, 안전하게";line=f.ProposalRetry?"안에서는 뛰거나 공을 던지지 않아요.\n잠깐 멈추고 다시 생각해요.":"문은 직접 열지 않아요.\n비상통화로 도움을 요청하고\n안내를 기다려요.";Add("1   알겠어, 다시 생각하기",lesson.Retry);break;
                case ElevatorStep.Rescue:
                    objective="구조원의 안내를 들어요";who="구조원";line=f.ReadyToExit?"바닥 높이와 나갈 길을 확인했어요.\n안내에 따라 천천히 나와 주세요.":"문 밖이 안전한지 확인할게요.\n안내할 때까지 안에서 기다려 주세요.";if(f.ReadyToExit)Add("1   안내에 따라 천천히 나가기",lesson.Continue);break;
                case ElevatorStep.ReturnLobby:
                    objective="직원과 계단으로 1층 이동";who="직원";line="승강기는 점검이 필요해요.\n계단으로 1층까지\n함께 내려가 드릴게요.";Add("1   직원의 안내를 받아 내려가기",lesson.Continue);break;
                case ElevatorStep.Returning:objective="직원과 함께 1층으로";who="이동";line="안내를 받아 계단으로 내려가요.";break;
                case ElevatorStep.GroundFloor:
                    objective="1층 주출입구로 귀가";line="1층에 도착했어!\n같이 기다려 줘서 고마워.\n이제 집에 가자.";Add("1   친구와 주출입구로 나가기",lesson.Continue);break;
                case ElevatorStep.GoingHome:objective="친구와 집으로";line="공은 안고, 천천히 나가요.";break;
                case ElevatorStep.Complete:
                    objective="고장난 엘리베이터에서 안전하게 대처했어요";who="친구";line="도움을 요청하고, 위치를 알리고,\n문을 건드리지 않고 함께 기다렸어요.";Add("1   우리가 해낸 일",ShowHistory);Add("2   한 번 더 해보기",lesson.Restart);break;
            }
            if(lesson.scenario){lesson.scenario.Describe(ref who,ref line);if(lesson.scenario.objectGuidedScene&&f.Step!=ElevatorStep.Call){actions.Clear();labels.Clear();}}
            if(lesson.presentation&&lesson.presentation.rescueTeam)lesson.presentation.rescueTeam.Describe(lesson,ref who,ref line);
            subtitle.text=objective;normalLine=line;normalSpeaker=who;speaker.text=who;dialogueText.text=line;
            progress.text=(f.HelpRequested?"<color=#297A70>● 도움 요청</color>":"○ 도움 요청")+"   "+(f.LocationReported?"<color=#297A70>● 위치 전달</color>":"○ 위치 전달")+"   "+(f.Reassured?"<color=#297A70>● 함께 대기</color>":"○ 함께 대기");
            if(!spatial){revealStarted=Time.unscaledTime;dialogueText.maxVisibleCharacters=Application.isPlaying?0:int.MaxValue;}
            int revision=f.Revision,run=lesson.RunVersion;
            for(int i=0;i<actions.Count;i++)
            {
                var action=actions[i];string name=f.Step==ElevatorStep.Call?"DialogueNext":"Choice"+i;
                Button b=i<previousButtons.Count&&i<previousLabels.Count&&previousLabels[i]==labels[i]&&previousButtons[i]&&previousButtons[i].name==name?previousButtons[i]:null;
                if(b){previousButtons[i]=null;b.onClick.RemoveAllListeners();}
                else{b=MakeButton(name,actionPanel,new Vector2(790,56),Vector2.zero,labels[i],()=>{});spatial?.Transitions?.Visibility((RectTransform)b.transform,true);}
                b.onClick.AddListener(()=>{if(!lesson.CinematicBlocking&&lesson.Flow.Revision==revision&&lesson.RunVersion==run){if(spatial&&spatial.TryReadDialogue())return;action();}});
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
