// Isolated test snapshot of Assets/M/Scripts/Elevator/ElevatorScenarioDirector.cs. Original is not modified.
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator.ObservationTrial
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
        readonly List<Graphic> graphics=new List<Graphic>();
        ElevatorObservation previousThoughtTarget;
        int thoughtRevision=-1,thoughtRun=-1;
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
        void Start()
        {
            RefreshWorldMaterials(true);
            if(hints!=null)foreach(var h in hints)
                if(h){if(h.group){h.group.alpha=0;h.group.blocksRaycasts=h.group.interactable=false;}h.gameObject.SetActive(false);}
        }
        public bool Allows(ElevatorClue clue)=>lesson.CanInteract(clue);
        // Stable discoverability cues, not a list of correct answers. Secondary
        // inspections remain available and get the same orange cue when pointed at.
        public bool IsPersistentSelectable(ElevatorClue clue)
        {
            var f=lesson.Flow;if(f==null)return false;
            if(f.SupportsObjectPreview)
                return clue==ElevatorClue.Friend||clue==ElevatorClue.Notice||clue==ElevatorClue.Intercom||
                    clue==(f.Step==ElevatorStep.Unanswered?ElevatorClue.Phone:ElevatorClue.Identity);
            switch(f.Step)
            {
                case ElevatorStep.Lobby:return clue==ElevatorClue.LobbyCall;
                case ElevatorStep.Boarding:return clue==ElevatorClue.FloorButtons||clue==ElevatorClue.FrontFloorButtons||
                    f.DestinationSelected&&(clue==ElevatorClue.Ball||clue==ElevatorClue.Friend);
                case ElevatorStep.Call:return f.SearchingIdentity&&clue==ElevatorClue.Identity;
                case ElevatorStep.Rescue:return f.ReadyToExit&&clue==ElevatorClue.Rescuer;
                case ElevatorStep.ReturnLobby:return clue==ElevatorClue.Rescuer;
                case ElevatorStep.GroundFloor:return clue==ElevatorClue.GroundExit;
                default:return false;
            }
        }
        ElevatorClue? RequiredTarget()
        {
            var f=lesson.Flow;
            if(f.IsCinematic||f.AwaitingCallContinue||f.CanConfirmObject)return null;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:return ElevatorClue.LobbyCall;
                case ElevatorStep.Boarding:return f.DestinationSelected?ElevatorClue.Ball:ElevatorClue.FloorButtons;
                case ElevatorStep.DoorPosterPointing:return ElevatorClue.Friend;
                case ElevatorStep.Stopped:return ElevatorClue.Intercom;
                case ElevatorStep.Unanswered:return ElevatorClue.Phone;
                case ElevatorStep.Call:return f.SearchingIdentity?(ElevatorClue?)ElevatorClue.Identity:null;
                case ElevatorStep.Waiting:return ElevatorClue.Friend;
                case ElevatorStep.Retry:return null;
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
            // Preview/confirmation checkpoints are handled before this method.
            if(f.Step==ElevatorStep.Unanswered&&clue==ElevatorClue.Phone){lesson.Phone();return true;}
            if(clue==ElevatorClue.Rescuer&&(f.Step==ElevatorStep.ReturnLobby||f.Step==ElevatorStep.Rescue&&f.ReadyToExit)){lesson.Continue();return true;}
            return false;
        }
        public string Hint(ElevatorClue clue)
        {
            return ElevatorDialogue.DescribeObject(lesson,clue);
        }
        public void Describe(ref string who,ref string line)
        {
            if(!objectGuidedScene)return;var f=lesson.Flow;
            switch(f.Step)
            {
                case ElevatorStep.Lobby:who="친구";line=ElevatorDialogue.CallLift;break;
                case ElevatorStep.Boarding:who="친구";line=f.DestinationSelected?ElevatorDialogue.Handoff:ElevatorDialogue.ChooseFloor;break;
                case ElevatorStep.Stopped:
                    who=f.CanObserve?"도움 요청":"친구";
                    line=!f.CanObserve?ElevatorDialogue.Stopped:f.HintLevel==0?"비상통화 버튼으로\n밖에 도움을 요청하세요.":"옆쪽 조작판에서\n벨 표시가 있는 버튼을 누르세요.";break;
                case ElevatorStep.Waiting:who="친구";if(!f.Reassured)line=ElevatorDialogue.FriendWaiting;break;
                case ElevatorStep.Rescue:who="구조요원";line=f.ReadyToExit?ElevatorDialogue.RescueReady:ElevatorDialogue.RescueChecking;break;
                case ElevatorStep.ReturnLobby:who="구조요원";line=ElevatorDialogue.Stairs;break;
                case ElevatorStep.GroundFloor:who="친구";line=ElevatorDialogue.GroundFloor;break;
                case ElevatorStep.Complete:who="친구";line=ElevatorDialogue.Complete;break;
            }
        }
        void LateUpdate()
        {
            if(!lesson||lesson.Flow==null)return;
            AdvanceGuidance(Time.unscaledDeltaTime);
            if(observations==null)observations=lesson.CollectObservations();
            PrimaryTarget=null;
            foreach(var observation in observations)
                if(observation&&observation.gameObject.activeInHierarchy&&observation.IsHovered&&lesson.CanInteract(observation.clue))
                {PrimaryTarget=observation;break;}
            if(previousThoughtTarget!=PrimaryTarget||thoughtRevision!=lesson.Flow.Revision||thoughtRun!=lesson.RunVersion)
            {
                previousThoughtTarget=PrimaryTarget;thoughtRevision=lesson.Flow.Revision;thoughtRun=lesson.RunVersion;
                // Do not stack a hover description on the current task/dialogue card.
                lesson.view.ShowThought("");
            }
        }
        // Run when content is created or its presentation mode changes, rather than every frame.
        public void RefreshWorldMaterials(bool rescan=false)
        {
            if(rescan||canvases==null)canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==gameObject.scene).ToArray();
            foreach(var canvas in canvases)
            {
                if(!canvas||canvas.renderMode!=RenderMode.WorldSpace)continue;
                graphics.Clear();canvas.GetComponentsInChildren(true,graphics);
                foreach(var g in graphics)
                {
                    if(g is TMP_Text t)
                    {
                        var source=t.fontSharedMaterial;if(!source||source.shader.name=="TextMeshPro/Distance Field Overlay")continue;
                        if(!fontOverlays.TryGetValue(source,out var m)){m=new Material(source);m.name="World readable text";m.shader=Shader.Find("TextMeshPro/Distance Field Overlay");m.renderQueue=4201;fontOverlays.Add(source,m);}t.fontSharedMaterial=m;
                    }
                    else if(!(g is TMP_SubMeshUI)&&worldOverlay&&g.material!=worldOverlay&&
                        !ElevatorUIFocusWindow.IsFocusMaterial(g.material))g.material=worldOverlay;
                }
            }
        }
        void OnDestroy(){foreach(var m in fontOverlays.Values)if(m)Destroy(m);}
    }
}
