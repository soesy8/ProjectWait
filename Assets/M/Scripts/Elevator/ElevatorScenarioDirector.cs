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
        public bool LockView => objectGuidedScene && lesson && lesson.Flow!=null && lesson.Flow.IsCinematic && !lesson.IsPaused;
        readonly Dictionary<Material,Material> fontOverlays=new Dictionary<Material,Material>();
        ElevatorObservation[] observations;Canvas[] canvases;
        ElevatorDesktopControls desktop;
        public ElevatorObservation PrimaryTarget {get;private set;}
        int lastRevision=-1,lastRun=-1;
        void Awake(){lesson.scenario=this;observations=lesson.CollectObservations();desktop=FindFirstObjectByType<ElevatorDesktopControls>();HideHud();}
        void HideHud(){if(lesson.view.spatial&&lesson.view.spatial.hud){lesson.view.spatial.hud.renderMode=RenderMode.WorldSpace;lesson.view.spatial.hud.gameObject.SetActive(false);}}
        public bool Allows(ElevatorClue clue)=>lesson.CanInteract(clue);
        public bool HandleObjectAction(ElevatorClue clue)
        {
            var f=lesson.Flow;
            if(f.Step==ElevatorStep.Proposal){if(clue==ElevatorClue.Friend)lesson.UnsafeProposal();else if(clue==ElevatorClue.Notice)lesson.SafeProposal();return true;}
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
                case ElevatorStep.Lobby:who="친구";line="운동도 마쳤으니 1층으로 내려가자.\n아래쪽 호출 버튼으로 엘리베이터를 불러 줄래?";break;
                case ElevatorStep.Boarding:who="친구";line=f.DestinationSelected?"1층 눌렀구나! 내 공을 받아 줄래?\n공을 선택하면 건네줄게.":"우리는 1층으로 가자.\n손잡이 위의 낮은 패널에서 1을 찾아봐.";break;
                case ElevatorStep.Proposal:who="친구";line="아까 슛 동작, 내가 보여 줄까?\n나를 선택해 부탁하거나 포스터를 살펴보고 밖에서 하자고 말해 줘.";break;
                case ElevatorStep.Stopped:who="친구";line=f.CanObserve?"어지러워… 뭐부터 해야 하지?\n손이 닿는 비상벨로 도움을 요청하자.":"덜컹! 어? 멈췄어… 머리가 빙빙 돌아.";break;
                case ElevatorStep.Waiting:who="친구";line="아직 가슴이 두근거리고 조금 무서워…\n내 옆에서 같이 기다려 줄래?";break;
                case ElevatorStep.Rescue:who="직원";line=f.ReadyToExit?"바닥 높이와 나갈 경로를 확인했어요.\n저를 선택하면 안내를 따라 천천히 나와요.":"잘 기다렸어요. 문 밖이 안전한지 확인할게요.\n제가 안내할 때까지 기다려 주세요.";break;
                case ElevatorStep.ReturnLobby:who="직원";line="승강기는 점검이 필요해요.\n저를 선택하면 함께 계단으로 1층에 내려갈게요.";break;
                case ElevatorStep.GroundFloor:who="친구";line="이제 안전한 1층이야.\n주출입구를 선택해서 함께 나가자.";break;
                case ElevatorStep.Complete:who="친구";line="도움을 요청하고, 위치를 알리고,\n문을 건드리지 않고 함께 기다렸어. 고마워!";break;
            }
        }
        void LateUpdate()
        {
            if(!lesson||lesson.Flow==null)return;HideHud();
            if(observations==null)observations=lesson.CollectObservations();
            PrimaryTarget=observations.FirstOrDefault(o=>o&&o.gameObject.activeInHierarchy&&o.IsHovered&&lesson.CanInteract(o.clue));
            bool reposition=lastRun!=lesson.RunVersion||lastRevision!=lesson.Flow.Revision;
            if(hints!=null)foreach(var h in hints)
            {
                if(!h||!h.observation)continue;var o=h.observation;
                bool show=!lesson.CinematicBlocking&&!lesson.IsPaused&&o.gameObject.activeInHierarchy&&lesson.CanInteract(o.clue)&&o.IsHovered;
                string text=Hint(o.clue);
                h.Present(show,text,lesson.playerCamera,reposition,lesson.view);
            }
            lastRun=lesson.RunVersion;lastRevision=lesson.Flow.Revision;
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
