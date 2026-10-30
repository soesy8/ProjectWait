using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ProjectWait.M.Elevator
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
        public bool IsPaused => pauseMenu&&pauseMenu.IsPaused;
        public ElevatorFlow Flow { get; private set; }
        public string Thought { get; private set; } = "";
        public int Floor { get; private set; } = 3;
        public int RunVersion { get; private set; }
        public bool IsReframing => presentation&&presentation.space&&presentation.space.sportsCentre&&presentation.space.sportsCentre.IsFraming;
        public bool CinematicBlocking => IsPaused||IsReframing||presentation&&presentation.space&&presentation.space.overlay&&presentation.space.overlay.Blocking;
        Vector3 friendHome;
        int renderedRevision = -1;
        bool previousMoving;
        GameObject cabin;
        ElevatorClue? activeClue;

        void Awake()
        {
            friendHome = presentation&&presentation.space?presentation.space.friendInside:friend.localPosition;
            cabin = transform.Find("Cabin").gameObject;
            Flow = new ElevatorFlow(settings) { NoResponse = simulateNoResponse };
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
            floorLabel.text = Floor.ToString();
            if (presentation) presentation.Apply(this);
            if (renderedRevision != Flow.Revision)
            {
                renderedRevision = Flow.Revision; Thought = "";activeClue=null; ClearHover(); view.Refresh();
            }
        }
        public void Refresh() { view.Refresh(); }
        public void Continue() { Flow.Continue(); Sync(); }
        public void SelectFloor() { Flow.SelectDestination(); Sync(); }
        public void SafeProposal() { Flow.ChooseSafe(); Sync(); }
        public void UnsafeProposal() { Flow.UnsafeProposal(); Sync(); }
        public void ObserveProposal() { Flow.ObserveProposal(); Sync(); }
        public void Intercom() { Flow.RequestIntercom(); Sync(); }
        public void Phone() { Flow.RequestPhone(); Sync(); }
        public void Report() { Flow.ReportLocation(); Sync(); }
        public void ReportSituation() { Flow.ReportSituation(); Sync(); }
        public void FindIdentity() { Flow.FindIdentity(); Sync(); }
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
            bool moving = Flow.IsMoving;
            switch (clue)
            {
                case ElevatorClue.Friend: Thought = Flow.Step==ElevatorStep.Boarding ? "친구의 공을 선택해서 받아 주세요." : Flow.Step==ElevatorStep.Waiting ? (Flow.Reassured?"친구에게 한마디 건네 볼까요?":"친구를 선택해 안심시켜 주세요.") : "친구가 놀랐어. 함께 차분히 도움을 요청하자."; break;
                case ElevatorClue.Ball: Thought = "공을 받아 주면 친구가 슛 이야기를 들려줘요."; break;
                case ElevatorClue.Door: Thought = moving ? "지금은 문이 닫혀 있어." : "문은 닫혀 있어. 직접 열지 않고 안내를 기다리자."; break;
                case ElevatorClue.Display: Thought = "마지막 표시는 3층이야. 표시만으로 정확한 정지 위치를 알 수는 없어."; break;
                case ElevatorClue.Intercom: Thought = moving ? "벨 표시와 스피커가 있네." : Flow.HelpRequested ? "도움을 요청했어. 안내에 따라 기다리자." : "도움을 요청할 수 있는 비상통화 장치야."; break;
                case ElevatorClue.Notice: Thought = "문에서 떨어져 비상통화로 도움을 요청해요."; break;
                case ElevatorClue.FloorButtons: Thought = Flow.Step==ElevatorStep.Boarding&&!Flow.DestinationSelected ? "손잡이 위의 낮은 측면 패널이야. 목적지인 1층을 선택하자." : "층 버튼과 문 열림·닫힘 버튼이야. 멈췄을 때는 비상통화로 도움을 요청하자."; break;
                case ElevatorClue.FrontFloorButtons: Thought = "전면 패널의 1층 버튼이야. 낮은 측면 패널에서도 누를 수 있어."; break;
                case ElevatorClue.FrontIntercom: Thought = "전면 비상벨은 손이 닿기 어려워. 올라서지 말고 낮은 측면 패널의 벨을 찾아보자."; break;
                case ElevatorClue.FacilityGuide: Thought = "1F 로비·주출입구 / 2F 준비 중\n3F 농구·탈의·보관함 / 4F 운동교실\n5F 체력단련실 / 6–12F 문화·운영"; break;
                case ElevatorClue.GroundExit: Thought = "1층 주출입구야. 친구와 함께 집에 가자."; break;
                case ElevatorClue.LobbyCall: Thought = "엘리베이터를 불러 친구와 함께 타요."; break;
                case ElevatorClue.Advertisement: Thought = "주말에 열리는 우리 동네 농구 교실! 다음에는 밖에서 친구와 패스 연습을 해 보자."; break;
                case ElevatorClue.Phone: Thought="비상통화에 응답이 없으면 휴대폰으로 위치와 상황을 알려요. 이 체험은 모의 통화예요.";break;
                case ElevatorClue.Rescuer: Thought="직원이 승강장 바닥과 경로를 확인했어요. 안내를 따라 천천히 이동해요.";break;
                case ElevatorClue.Identity: Thought = settings.building + " · " + settings.liftId + "\n선택해서 위치를 확인해요."; break;
            }
            view.ShowThought(Thought);
        }
        public void ClearThought(ElevatorClue clue)
        {
            if(activeClue!=clue)return;activeClue=null;Thought="";view.ShowThought("");
        }
        public void Activate(ElevatorClue clue)
        {
            if (!CanInteract(clue)) return;
            Inspect(clue);
            if(scenario&&scenario.objectGuidedScene&&scenario.HandleObjectAction(clue))return;
            if(clue==ElevatorClue.LobbyCall&&Flow.Step==ElevatorStep.Lobby)Continue();
            else if (clue == ElevatorClue.GroundExit && Flow.Step == ElevatorStep.GroundFloor) Continue();
            else if (clue == ElevatorClue.FloorButtons && Flow.Step == ElevatorStep.Boarding) SelectFloor();
            else if (clue == ElevatorClue.FrontFloorButtons && Flow.Step == ElevatorStep.Boarding) {Flow.SelectDestination("전면");Sync();}
            else if (clue == ElevatorClue.Intercom && Flow.Step == ElevatorStep.Stopped) Intercom();
            else if (clue == ElevatorClue.Identity && Flow.Step == ElevatorStep.Call) FindIdentity();
            else if (clue == ElevatorClue.Phone && Flow.Step == ElevatorStep.Unanswered) Phone();
            else if ((clue == ElevatorClue.Ball || clue == ElevatorClue.Friend) && Flow.Step == ElevatorStep.Boarding && Flow.DestinationSelected) Continue();
            else if (clue == ElevatorClue.Friend && Flow.Step == ElevatorStep.Waiting) { if(Flow.Reassured)Encourage();else Reassure(); }
            // Selecting a door inspects it; it does not mean trying to force it open.
        }
        // Availability for observation is deliberately broader than story progression.
        // Each action below is separately guarded by its flow state.
        public bool CanInteract(ElevatorClue clue)
        {
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
