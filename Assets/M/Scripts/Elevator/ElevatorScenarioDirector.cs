using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator
{
    [DefaultExecutionOrder(-200)]
    public sealed class ElevatorScenarioDirector : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public bool objectGuidedScene;
        public Material worldOverlay;
        public ElevatorObjectHint[] hints;
        readonly Dictionary<Material,Material> fontOverlays=new Dictionary<Material,Material>();
        ElevatorObservation[] observations;Canvas[] canvases;
        ElevatorDesktopControls desktop;
        public ElevatorObservation PrimaryTarget {get;private set;}
        [Min(0)] public float guidanceDelay=8;
        [Min(.1f)] public float guidanceCycleSeconds=1;
        public ElevatorClue? GuidanceTarget {get;private set;}
        public float GuidanceElapsed {get;private set;}
        public bool GuidanceActive=>GuidanceTarget.HasValue&&GuidanceElapsed>=guidanceDelay&&!lesson.CinematicBlocking;
        public float GuidanceBlend=>GuidanceActive?.5f-.5f*Mathf.Cos((GuidanceElapsed-guidanceDelay)*Mathf.PI*2/guidanceCycleSeconds):0;
        int guidanceRun=-1;
        void Awake(){lesson.scenario=this;observations=lesson.CollectObservations();desktop=FindFirstObjectByType<ElevatorDesktopControls>();}
        public bool Allows(ElevatorClue clue)=>lesson.CanInteract(clue);
        ElevatorClue? RequiredTarget()
        {
            var f=lesson.Flow;
            if(f.IsCinematic)return null;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:return ElevatorClue.LobbyCall;
                case ElevatorStep.Boarding:return f.DestinationSelected?ElevatorClue.Ball:ElevatorClue.FloorButtons;
                case ElevatorStep.Proposal:return objectGuidedScene?(ElevatorClue?)ElevatorClue.Notice:null;
                case ElevatorStep.Stopped:return ElevatorClue.Intercom;
                case ElevatorStep.Unanswered:return ElevatorClue.Phone;
                case ElevatorStep.Call:return f.SearchingIdentity?(ElevatorClue?)ElevatorClue.Identity:null;
                case ElevatorStep.Waiting:return ElevatorClue.Friend;
                case ElevatorStep.Retry:return !f.ProposalRetry&&objectGuidedScene?(ElevatorClue?)ElevatorClue.Intercom:null;
                case ElevatorStep.Rescue:case ElevatorStep.ReturnLobby:return objectGuidedScene?(ElevatorClue?)ElevatorClue.Rescuer:null;
                case ElevatorStep.GroundFloor:return ElevatorClue.GroundExit;
                default:return null;
            }
        }
        public void AdvanceGuidance(float seconds)
        {
            if(!lesson||lesson.Flow==null)return;
            var target=RequiredTarget();
            if(guidanceRun!=lesson.RunVersion||GuidanceTarget!=target)
            {guidanceRun=lesson.RunVersion;GuidanceTarget=target;GuidanceElapsed=0;}
            if(!target.HasValue||lesson.CinematicBlocking||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            GuidanceElapsed+=seconds;
        }
        public void NotifySelection(ElevatorClue clue)
        {
            if(clue==GuidanceTarget||GuidanceTarget==ElevatorClue.FloorButtons&&clue==ElevatorClue.FrontFloorButtons)
                GuidanceElapsed=0;
        }
        public bool IsGuidanceTarget(ElevatorClue clue)=>GuidanceActive&&GuidanceTarget==clue;
        public bool HandleObjectAction(ElevatorClue clue)
        {
            var f=lesson.Flow;
            // The emergency button requests a connection. Dialogue uses the Next UI.
            if(f.Step==ElevatorStep.Unanswered&&clue==ElevatorClue.Phone){lesson.Phone();return true;}
            if(f.Step==ElevatorStep.Retry&&clue==ElevatorClue.Intercom){lesson.Retry();return true;}
            if(clue==ElevatorClue.Rescuer&&(f.Step==ElevatorStep.ReturnLobby||f.Step==ElevatorStep.Rescue&&f.ReadyToExit)){lesson.Continue();return true;}
            return false;
        }
        public string Hint(ElevatorClue clue)
        {
            var f=lesson.Flow;
            switch(clue)
            {
                case ElevatorClue.LobbyCall:return "내려가는 엘리베이터 호출 버튼이에요.";
                case ElevatorClue.FloorButtons:return "손잡이 위의 낮은 측면 조작 패널이에요. 층과 문 버튼이 있어요.";
                case ElevatorClue.FrontFloorButtons:return "출입문 옆의 전면 층 버튼이에요. 측면 패널에서도 같은 층을 고를 수 있어요.";
                case ElevatorClue.Ball:return "농구를 마치고 챙겨 온 공이에요. 친구에게서 받아 안을 수 있어요.";
                case ElevatorClue.Friend:return f.Step==ElevatorStep.Waiting?"놀란 친구가 천천히 숨을 고르고 있어요. 이야기를 건네 볼까요?":f.Step==ElevatorStep.Proposal?"아까 성공한 슛 동작을 보여 주고 싶어 하는 친구예요.":"농구를 마치고 함께 내려가는 친구예요.";
                case ElevatorClue.Notice:return "안전 이용 포스터예요. 고장이 났을 때의 대처 방법도 적혀 있어요.";
                case ElevatorClue.Intercom:return f.Step==ElevatorStep.Call?"관리실과 통화 중이에요. 대화는 말풍선의 ‘다음’ 버튼으로 이어가요.":"손이 닿는 높이에 있는 비상벨과 스피커예요.";
                case ElevatorClue.FrontIntercom:return "높은 전면 비상벨이에요. 낮은 측면 패널에도 비상통화 장치가 있어요.";
                case ElevatorClue.Identity:return lesson.settings.building+" · "+lesson.settings.liftId+"\n승강기를 구분하는 식별 정보예요.";
                case ElevatorClue.Display:return "마지막 표시는 3층이에요. 표시만으로 정확한 정지 위치를 알 수는 없어요.";
                case ElevatorClue.FacilityGuide:return "층별 시설 안내예요. 1층 로비, 3층 농구장, 5층 체력단련실이 있어요.";
                case ElevatorClue.Advertisement:return "우리 동네 농구 교실 안내예요.";
                case ElevatorClue.Rescuer:return "승강장 높이와 나갈 경로를 확인하는 직원이에요.";
                case ElevatorClue.Phone:return "휴대폰이에요. 비상통화에 응답이 없을 때 119 모의 신고에 사용할 수 있어요.";
                case ElevatorClue.GroundExit:return "1층의 주출입구예요. 친구와 함께 집으로 가는 길이에요.";
                default:return "주변을 천천히 살펴봐요.";
            }
        }
        public void Describe(ref string who,ref string line)
        {
            if(!objectGuidedScene)return;var f=lesson.Flow;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:who="친구";line="1층으로 내려가자.\n아래쪽 호출 버튼을 눌러 줄래?";break;
                case ElevatorStep.Boarding:who="친구";line=f.DestinationSelected?"1층 눌렀구나!\n공을 선택해서 받아 줄래?":"1층으로 가자.\n낮은 측면 패널에서\n1층 버튼을 찾아봐.";break;
                case ElevatorStep.Stopped:who="친구";line=f.CanObserve?"어지러워… 어떻게 하지?\n낮은 비상벨로 도움을 요청하자.":"덜컹! 어? 멈췄어…";break;
                case ElevatorStep.Waiting:who="친구";line="아직 조금 무서워…\n옆에서 같이 기다려 줄래?";break;
                case ElevatorStep.Rescue:who="직원";line=f.ReadyToExit?"바닥 높이와 나갈 길을 확인했어요.\n저를 선택해 천천히 따라오세요.":"문 밖이 안전한지 확인할게요.\n안내할 때까지 안에서 기다려 주세요.";break;
                case ElevatorStep.ReturnLobby:who="직원";line="승강기는 점검이 필요해요.\n저를 선택하면 계단으로\n1층까지 함께 내려갈게요.";break;
                case ElevatorStep.GroundFloor:who="친구";line="이제 안전한 1층이야.\n주출입구를 선택해서 함께 나가자.";break;
                case ElevatorStep.Complete:who="친구";line="도움을 요청하고, 위치를 알리고,\n문을 건드리지 않고 함께 기다렸어. 고마워!";break;
            }
        }
        void LateUpdate()
        {
            if(!lesson||lesson.Flow==null)return;
            AdvanceGuidance(Time.unscaledDeltaTime);
            if(observations==null)observations=lesson.CollectObservations();
            PrimaryTarget=observations.FirstOrDefault(o=>o&&o.gameObject.activeInHierarchy&&o.IsHovered&&lesson.CanInteract(o.clue));
            lesson.view.ShowThought(PrimaryTarget?Hint(PrimaryTarget.clue):"");
            // Object descriptions share the screen thought bubble; retire the old world labels.
            if(hints!=null)foreach(var h in hints)
            {
                if(h){if(h.group){h.group.alpha=0;h.group.blocksRaycasts=h.group.interactable=false;}h.gameObject.SetActive(false);}
            }
            ApplyWorldMaterials();
        }
        void ApplyWorldMaterials()
        {
            if(canvases==null)canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==gameObject.scene).ToArray();
            foreach(var canvas in canvases)
            {
                if(canvas.renderMode!=RenderMode.WorldSpace)continue;
                foreach(var g in canvas.GetComponentsInChildren<Graphic>(true))
                {
                    if(g is TMP_Text t)
                    {
                        var source=t.fontSharedMaterial;if(!source||source.shader.name=="TextMeshPro/Distance Field Overlay")continue;
                        if(!fontOverlays.TryGetValue(source,out var m)){m=new Material(source);m.name="World readable text";m.shader=Shader.Find("TextMeshPro/Distance Field Overlay");m.renderQueue=4201;fontOverlays.Add(source,m);}t.fontSharedMaterial=m;
                    }
                    else if(!(g is TMP_SubMeshUI)&&worldOverlay)g.material=worldOverlay;
                }
            }
        }
        void OnDestroy(){foreach(var m in fontOverlays.Values)if(m)Destroy(m);}
    }
}
