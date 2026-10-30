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
        public bool Allows(ElevatorClue clue)
        {
            var f=lesson.Flow;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:return clue==ElevatorClue.LobbyCall;
                case ElevatorStep.Boarding:return !f.DestinationSelected?(clue==ElevatorClue.FloorButtons||clue==ElevatorClue.FrontFloorButtons):(clue==ElevatorClue.Friend||clue==ElevatorClue.Ball);
                case ElevatorStep.Proposal:return clue==ElevatorClue.Friend||clue==ElevatorClue.Notice;
                case ElevatorStep.Stopped:return f.CanObserve&&clue!=ElevatorClue.LobbyCall&&clue!=ElevatorClue.LobbyCallUp&&clue!=ElevatorClue.GroundExit&&clue!=ElevatorClue.Rescuer&&clue!=ElevatorClue.Phone;
                case ElevatorStep.Call:return !f.SituationReported||f.IdentityFound?clue==ElevatorClue.Intercom:clue==ElevatorClue.Identity||clue==ElevatorClue.Display;
                case ElevatorStep.Unanswered:return clue==ElevatorClue.Phone;
                case ElevatorStep.Waiting:return clue==ElevatorClue.Friend;
                case ElevatorStep.Retry:return clue==ElevatorClue.Intercom;
                case ElevatorStep.Rescue:return f.ReadyToExit&&clue==ElevatorClue.Rescuer;
                case ElevatorStep.ReturnLobby:return clue==ElevatorClue.Rescuer;
                case ElevatorStep.GroundFloor:return clue==ElevatorClue.GroundExit;
                default:return false;
            }
        }
        public bool HandleObjectAction(ElevatorClue clue)
        {
            var f=lesson.Flow;
            if(f.Step==ElevatorStep.Proposal){if(clue==ElevatorClue.Friend)lesson.UnsafeProposal();else if(clue==ElevatorClue.Notice)lesson.SafeProposal();return true;}
            if(f.Step==ElevatorStep.Call&&clue==ElevatorClue.Intercom){if(!f.SituationReported)lesson.ReportSituation();else if(!f.LocationReported)lesson.Report();else lesson.Advice();return true;}
            if(f.Step==ElevatorStep.Unanswered&&clue==ElevatorClue.Phone){lesson.Phone();return true;}
            if(f.Step==ElevatorStep.Retry&&clue==ElevatorClue.Intercom){lesson.Retry();return true;}
            if(clue==ElevatorClue.Rescuer&&(f.Step==ElevatorStep.ReturnLobby||f.Step==ElevatorStep.Rescue&&f.ReadyToExit)){lesson.Continue();return true;}
            return false;
        }
        ElevatorClue TargetClue()
        {
            var f=lesson.Flow;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:return ElevatorClue.LobbyCall;
                case ElevatorStep.Boarding:return f.DestinationSelected?ElevatorClue.Ball:ElevatorClue.FloorButtons;
                case ElevatorStep.Proposal:return ElevatorClue.Friend;
                case ElevatorStep.Stopped:return ElevatorClue.Intercom;
                case ElevatorStep.Call:return f.SituationReported&&!f.IdentityFound?ElevatorClue.Identity:ElevatorClue.Intercom;
                case ElevatorStep.Waiting:return ElevatorClue.Friend;
                case ElevatorStep.Unanswered:return ElevatorClue.Phone;
                case ElevatorStep.Retry:return ElevatorClue.Intercom;
                case ElevatorStep.Rescue:case ElevatorStep.ReturnLobby:return ElevatorClue.Rescuer;
                case ElevatorStep.GroundFloor:return ElevatorClue.GroundExit;
                default:return ElevatorClue.LobbyCallUp;
            }
        }
        public bool IsGuidedTarget(ElevatorObservation o)
        {
            if(!objectGuidedScene||lesson.Flow==null)return false;
            var clue=TargetClue();
            if(o.clue==clue)return true;
            return lesson.Flow.Step==ElevatorStep.Proposal&&o.clue==ElevatorClue.Notice;
        }
        public string Hint(ElevatorClue clue)
        {
            var f=lesson.Flow;
            switch(clue)
            {
                case ElevatorClue.LobbyCall:return "아래쪽 호출 버튼으로 1호기를 불러요.";
                case ElevatorClue.FloorButtons:case ElevatorClue.FrontFloorButtons:return "목적지인 1층을 선택해요.";
                case ElevatorClue.Ball:return "친구의 공을 선택해서 받아 안아요.";
                case ElevatorClue.Friend:return f.Step==ElevatorStep.Proposal?"친구에게 슛 동작을 보여 달라고 해요.":f.Step==ElevatorStep.Waiting?"문에서 떨어져 함께 기다리자고 말해요.":"친구의 공을 받아 안아요.";
                case ElevatorClue.Notice:return f.Step==ElevatorStep.Proposal?"슛은 밖에서 하자고 이야기해요.":"고장 났을 때의 대처 방법을 살펴봐요.";
                case ElevatorClue.Intercom:return f.Step==ElevatorStep.Call?(!f.SituationReported?"엘리베이터가 멈췄고 둘이 있다고 알려요.":!f.LocationReported?"확인한 건물·호기·관리번호를 알려요.":"안내를 확인하고 문에서 떨어져요."):"손이 닿는 비상통화로 도움을 요청해요.";
                case ElevatorClue.Identity:return "식별판에서 건물·호기·관리번호를 확인해요.";
                case ElevatorClue.Rescuer:return f.Step==ElevatorStep.ReturnLobby?"직원의 안내를 받아 계단으로 내려가요.":"안전 확인을 마친 직원의 안내를 따라요.";
                case ElevatorClue.Phone:return "응답이 없으면 휴대폰으로 119에 알려요. (모의 통화)";
                case ElevatorClue.GroundExit:return "친구와 함께 1층 출입구로 나가요.";
                default:return "주변을 천천히 살펴봐요.";
            }
        }
        public void Describe(ref string who,ref string line)
        {
            if(!objectGuidedScene)return;var f=lesson.Flow;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:who="친구";line="운동도 마쳤으니 1층으로 내려가자.\n빛나는 아래쪽 호출 버튼으로 엘리베이터를 불러 줄래?";break;
                case ElevatorStep.Boarding:who="친구";line=f.DestinationSelected?"1층 눌렀구나! 내 공을 받아 줄래?\n빛나는 공을 선택하면 건네줄게.":"우리는 1층으로 가자.\n손잡이 위의 낮은 패널에서 빛나는 1을 찾아봐.";break;
                case ElevatorStep.Proposal:who="친구";line="아까 슛 동작, 내가 보여 줄까?\n나를 선택해 부탁하거나 포스터를 살펴보고 밖에서 하자고 말해 줘.";break;
                case ElevatorStep.Stopped:who="친구";line=f.CanObserve?"어지러워… 뭐부터 해야 하지?\n손이 닿는 비상벨로 도움을 요청하자.":"덜컹! 어? 멈췄어… 머리가 빙빙 돌아.";break;
                case ElevatorStep.Call:who=f.UsedPhone?"119 · 모의 통화":"관리실";line=!f.SituationReported?"어떤 상황인가요?\n빛나는 통화 장치를 선택해 멈춘 상황과 인원을 알려 주세요.":!f.IdentityFound?"잘 알려 줬어요. 앞쪽 식별판에서\n건물과 승강기 번호를 확인해 줄래요?":!f.LocationReported?"식별판을 확인했군요. 통화 장치를 선택해\n건물명·호기·관리번호를 알려 주세요.":"위치를 확인했어요. 다친 사람은 없군요.\n통화 장치로 안내를 확인하고 문에서 떨어져 기다려 주세요.";break;
                case ElevatorStep.Waiting:who="친구";line="문을 만져 볼까…? 아직 조금 무서워.\n나를 선택해서 함께 기다리자고 말해 줄래?";break;
                case ElevatorStep.Rescue:who="직원";line=f.ReadyToExit?"바닥 높이와 나갈 경로를 확인했어요.\n저를 선택하면 안내를 따라 천천히 나와요.":"잘 기다렸어요. 문 밖이 안전한지 확인할게요.\n제가 안내할 때까지 기다려 주세요.";break;
                case ElevatorStep.ReturnLobby:who="직원";line="승강기는 점검이 필요해요.\n저를 선택하면 함께 계단으로 1층에 내려갈게요.";break;
                case ElevatorStep.GroundFloor:who="친구";line="이제 안전한 1층이야.\n빛나는 주출입구를 선택해서 함께 나가자.";break;
                case ElevatorStep.Complete:who="친구";line="도움을 요청하고, 위치를 알리고,\n문을 건드리지 않고 함께 기다렸어. 고마워!";break;
            }
        }
        void LateUpdate()
        {
            if(!lesson||lesson.Flow==null)return;HideHud();
            if(observations==null)observations=lesson.CollectObservations();
            PrimaryTarget=objectGuidedScene?observations.FirstOrDefault(o=>o&&o.gameObject.activeInHierarchy&&o.clue==TargetClue()):null;
            bool reposition=lastRun!=lesson.RunVersion||lastRevision!=lesson.Flow.Revision;
            if(hints!=null)foreach(var h in hints)
            {
                if(!h||!h.observation)continue;var o=h.observation;
                bool show=!lesson.CinematicBlocking&&!lesson.IsPaused&&o.gameObject.activeInHierarchy&&lesson.CanInteract(o.clue)&&(o.IsHovered||IsGuidedTarget(o));
                string text=o.IsHovered&&!string.IsNullOrEmpty(lesson.Thought)?lesson.Thought:Hint(o.clue);
                h.Present(show,text,lesson.playerCamera,reposition);
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
