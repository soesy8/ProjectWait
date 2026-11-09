// Isolated test snapshot of Assets/M/Scripts/Elevator/ElevatorLesson.cs. Original is not modified.
using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ProjectWait.M.Elevator.ObservationTrial
{
    public sealed class ElevatorLesson : MonoBehaviour
    {
        public ElevatorSettings settings;
        public ElevatorView view;
        public Transform friend;
        public Camera playerCamera;
        public TMP_Text floorLabel;
        public Light cabinLight;
        public AudioSource motor, signal;
        public AudioClip motorClip, stopClip;
        public bool simulateNoResponse;
        public ElevatorStoryPresentation presentation;
        public ElevatorPauseMenu pauseMenu;
        public ElevatorScenarioDirector scenario;
        public ElevatorPosterCutscene posterCutscene;
        public ElevatorDoorSafetyCutscene doorSafetyCutscene;
        public bool IsPaused => pauseMenu&&pauseMenu.IsPaused;
        public ElevatorFlow Flow { get; private set; }
        public string Thought { get; private set; } = "";
        public int Floor { get; private set; } = 3;
        public int RunVersion { get; private set; }
        public bool CinematicBlocking => IsPaused||Flow!=null&&Flow.IsCinematic||presentation&&presentation.space&&presentation.space.overlay&&presentation.space.overlay.Blocking;
        Vector3 friendHome;
        int renderedRevision = -1;
        bool previousMoving;
        GameObject cabin;
        ElevatorClue? activeClue;

        void Awake()
        {
            friendHome = presentation&&presentation.space?presentation.space.friendInside:friend.localPosition;
            cabin = transform.Find("Cabin").gameObject;
            Flow = new ElevatorFlow(settings, presentation && presentation.space && presentation.space.sportsCentre) { NoResponse = simulateNoResponse };
            motor.clip = motorClip; motor.loop = true; motor.volume = .08f;
            signal.clip = stopClip; signal.volume = .18f;
            view.Initialize(this); ResetPresentation();
        }
        void OnEnable()
        {
            // A script reload retains the scene but not this managed state owner.
            if (Application.isPlaying && Flow == null) Awake();
        }
        void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float delta)
        {
            if(IsPaused||Flow==null)return;
            Flow.NoResponse = simulateNoResponse;
            var previousStep=Flow.Step;var previousPhase=Flow.Phase;
            int previousRevision=Flow.Revision,previousHint=Flow.HintLevel;
            Flow.Tick(delta);
            bool outside = Flow.Step == ElevatorStep.Lobby || Flow.Step == ElevatorStep.Complete;
            cabin.SetActive(presentation&&presentation.space||!outside); friend.gameObject.SetActive(true);
            Floor = Flow.Step==ElevatorStep.GroundFloor||Flow.Step==ElevatorStep.GoingHome||Flow.Step==ElevatorStep.Complete ? 1 : 3;
            float height = 0;
            if (Flow.Step == ElevatorStep.Jump)
            {
                float t = (Flow.Elapsed - settings.friendReplySeconds) / settings.jumpSeconds;
                if (t > 0 && t < 1) height = Mathf.Sin(t * Mathf.PI) * .24f;
            }
            friend.localPosition = friendHome + Vector3.up * height;
            if (previousMoving && !Flow.IsMoving && Flow.Step == ElevatorStep.Stopped) signal.Play();
            previousMoving = Flow.IsMoving;
            if (Flow.IsMoving && !motor.isPlaying) motor.Play();
            if (!Flow.IsMoving && motor.isPlaying) motor.Stop();
            cabinLight.intensity = 1.6f;
            string floorText=Floor==1?"1":"3";
            if(floorLabel.text!=floorText)floorLabel.text=floorText;
            if (presentation) presentation.Apply(this);
            if (renderedRevision != Flow.Revision)
            {
                bool hintOnly=renderedRevision==previousRevision&&previousStep==Flow.Step&&previousPhase==Flow.Phase&&previousHint!=Flow.HintLevel;
                renderedRevision = Flow.Revision;
                if(!hintOnly){Thought="";activeClue=null;ClearHover();}
                view.Refresh();
            }
        }
        public void Refresh() { view.Refresh(); }
        public void Continue() { Flow.Continue(); Sync(); }
        public void SelectFloor() { Flow.SelectDestination(); Sync(); }
        public void Intercom() { Flow.RequestIntercom(); Sync(); }
        public void Phone() { Flow.RequestPhone(); Sync(); }
        public void Report() { Flow.ReportLocation(); Sync(); }
        public void ReportSituation() { Flow.ReportSituation(); Sync(); }
        public void FindIdentity() { Flow.FindIdentity(); Sync(); }
        public void ReinspectIdentity() { Flow.ReinspectIdentity(); Sync(); }
        public void ReobserveDoor() { Flow.ReobserveDoor(); Sync(); }
        public void ChooseSafeWall() { if (!CinematicBlocking) { Flow.ChooseDoorAction(true); Sync(); } }
        public void ChooseUnsafeWall() { if (!CinematicBlocking) { Flow.ChooseDoorAction(false); Sync(); } }
        public void ConfirmObject() { if (!CinematicBlocking) { Flow.ConfirmObject(); Sync(); } }
        public void CloseObjectPreview() { if (!CinematicBlocking) { Flow.CloseObjectPreview(); Sync(); } }
        public void Encourage() { Flow.Encourage(); Sync(); }
        public void Advice() { Flow.FollowAdvice(); Sync(); }
        public void Reassure() { Flow.Reassure(); Sync(); }
        public void UnsafeDoor() { Flow.UnsafeAction("문을 억지로 열려고 함"); Sync(); }
        public void UnsafeExit() { Flow.UnsafeAction("스스로 빠져나가려고 함"); Sync(); }
        public void Retry() { Flow.Retry(); Sync(); }
        void Sync() { Advance(0); }
        public void Restart() { Flow.Reset(); FindFirstObjectByType<ElevatorDesktopControls>()?.ResetView(); ResetPresentation(); }
        public void DeveloperJump(int stage) { Flow.DeveloperJump(stage); ResetPresentation(); }
        void ResetPresentation()
        {
            view.RestoreThoughtLayout();
            RunVersion++; Floor = 3; renderedRevision = -1;
            Thought = ""; signal.Stop(); motor.Stop(); previousMoving = Flow.IsMoving;
            foreach(var feedback in GetComponentsInChildren<ElevatorButtonFeedback>(true))feedback.ResetFeedback();
            friend.localPosition = friendHome; ClearHover(); Advance(0);
        }
        public ElevatorObservation[] CollectObservations() => GetComponentsInChildren<ElevatorObservation>(true).Concat(presentation&&presentation.rescuer?presentation.rescuer.GetComponentsInChildren<ElevatorObservation>(true):Array.Empty<ElevatorObservation>()).Distinct().ToArray();
        void ClearHover()
        {
            foreach (var target in CollectObservations()) target.ResetHover();
        }
        public void Inspect(ElevatorClue clue)
        {
            if (!CanInteract(clue)) return;
            activeClue=clue;
            Thought=ElevatorDialogue.DescribeObject(this,clue);
            // Hover identifies a target; detailed text opens only on a deliberate click.
        }
        public void ClearThought(ElevatorClue clue)
        {
            if(activeClue!=clue)return;activeClue=null;Thought="";view.ShowThought("");
        }
        public void Activate(ElevatorClue clue)
        {
            if (!CanInteract(clue) || !CollectObservations().Any(o => o && o.clue == clue && o.CanActivate)) return;
            if(scenario)scenario.NotifySelection(clue);
            Inspect(clue);
            if (clue == ElevatorClue.Intercom && Flow.Step == ElevatorStep.Stopped) { Intercom(); return; }
            if (clue == ElevatorClue.Phone && Flow.Step == ElevatorStep.Unanswered) { Phone(); return; }
            if (clue == ElevatorClue.Identity && Flow.SearchingIdentity) { FindIdentity(); return; }
            if (Flow.SupportsObjectPreview) { Flow.PreviewObject(clue); Sync(); return; }
            if(scenario&&scenario.objectGuidedScene&&scenario.HandleObjectAction(clue))return;
            if(clue==ElevatorClue.Notice&&Flow.AwaitingPoster){Flow.PointAtSafetyPoster();Sync();}
            else if(clue==ElevatorClue.LobbyCall&&Flow.Step==ElevatorStep.Lobby)Continue();
            else if (clue == ElevatorClue.GroundExit && Flow.Step == ElevatorStep.GroundFloor) Continue();
            else if (clue == ElevatorClue.FloorButtons && Flow.Step == ElevatorStep.Boarding) SelectFloor();
            else if (clue == ElevatorClue.FrontFloorButtons && Flow.Step == ElevatorStep.Boarding) {Flow.SelectDestination("전면");Sync();}
            else if (clue == ElevatorClue.Intercom && Flow.Step == ElevatorStep.Stopped) Intercom();
            else if (clue == ElevatorClue.Identity && Flow.Step == ElevatorStep.Call) FindIdentity();
            else if (clue == ElevatorClue.Phone && Flow.Step == ElevatorStep.Unanswered) Phone();
            else if (clue == ElevatorClue.Rescuer && ((Flow.Step == ElevatorStep.Rescue && Flow.ReadyToExit) || Flow.Step == ElevatorStep.ReturnLobby)) Continue();
            else if ((clue == ElevatorClue.Ball || clue == ElevatorClue.Friend) && Flow.Step == ElevatorStep.Boarding && Flow.DestinationSelected) Continue();
            else if (clue == ElevatorClue.Friend && Flow.Step == ElevatorStep.Waiting) { if(Flow.Reassured)Encourage();else Reassure(); }
            // Selecting a door inspects it; it does not mean trying to force it open.
        }
        // Availability for observation is deliberately broader than story progression.
        // Each action below is separately guarded by its flow state.
        public bool CanInteract(ElevatorClue clue)
        {
            if (Flow != null && (Flow.ReviewingIdentity || Flow.AwaitingCallContinue || Flow.Step == ElevatorStep.Retry)) return false;
            return clue!=ElevatorClue.LobbyCallUp&&Flow!=null&&!IsPaused&&!CinematicBlocking&&!Flow.IsCinematic&&Flow.Step!=ElevatorStep.Complete;
        }
        void OnDisable()
        {
            if (motor) motor.Stop(); if (signal) signal.Stop();
            if (friend) friend.localPosition = friendHome;
            ClearHover();
        }
        public object Snapshot() => new { step = Flow.Step.ToString(), phase = Flow.Phase.ToString(), Flow.PlayerHasBall, Flow.DestinationSelected, Flow.FirstJudgment, Flow.HelpRequested, Flow.LocationReported, Flow.UsedPhone, Flow.Reassured, Flow.ReadyToExit, Flow.HintLevel, Flow.JumpCount, Floor, motorPlaying = motor.isPlaying, friendHeight = friend.localPosition.y - friendHome.y, history = Flow.History.ToArray() };
    }
}
