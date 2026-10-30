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
        public ElevatorFlow Flow { get; private set; }
        public string Thought { get; private set; } = "";
        public int Floor { get; private set; } = 2;
        public int RunVersion { get; private set; }
        public bool CinematicBlocking => presentation&&presentation.space&&presentation.space.overlay&&presentation.space.overlay.Blocking;
        Vector3 friendHome;
        float floorClock;
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
        void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float delta)
        {
            Flow.NoResponse = simulateNoResponse;
            Flow.Tick(delta);
            bool outside = Flow.Step == ElevatorStep.Lobby || Flow.Step == ElevatorStep.Complete;
            cabin.SetActive(presentation&&presentation.space||!outside); friend.gameObject.SetActive(true);
            if (Flow.IsMoving)
            {
                floorClock += delta;
                if (floorClock >= 4) { floorClock %= 4; Floor = Floor >= 9 ? 2 : Floor + 1; }
            }
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
            cabinLight.intensity = Flow.Step == ElevatorStep.Stopped && Flow.Elapsed < .5f ? 1.15f : 1.6f;
            floorLabel.text = outside ? "01" : Flow.IsMoving ? "↑  " + Floor.ToString("00") : "—  " + Floor.ToString("00") + "  정지";
            if (presentation) presentation.Apply(this);
            if (renderedRevision != Flow.Revision)
            {
                renderedRevision = Flow.Revision; Thought = "";activeClue=null; ClearHover(); view.Refresh();
            }
        }
        public void Refresh() { view.Refresh(); }
        public void Continue() { Flow.Continue(); Sync(); }
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
            RunVersion++; floorClock = 0; Floor = 2; renderedRevision = -1;
            Thought = ""; signal.Stop(); motor.Stop(); previousMoving = Flow.IsMoving;
            friend.localPosition = friendHome; ClearHover(); Advance(0);
        }
        void ClearHover()
        {
            foreach (var target in GetComponentsInChildren<ElevatorObservation>(true)) target.ResetHover();
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
                case ElevatorClue.Door: Thought = moving ? "지금은 문이 닫혀 있어." : "층 사이에 멈춘 걸까? 문은 닫혀 있어."; break;
                case ElevatorClue.Display: Thought = moving ? "층 숫자가 바뀌고 있어." : "숫자가 멈췄어. 지금 층은 정확히 알 수 없겠어."; break;
                case ElevatorClue.Intercom: Thought = moving ? "벨 표시와 스피커가 있네." : Flow.HelpRequested ? "도움을 요청했어. 안내에 따라 기다리자." : "도움을 요청할 수 있는 비상통화 장치야."; break;
                case ElevatorClue.Notice: Thought = "문에서 떨어져 비상통화로 도움을 요청해요."; break;
                case ElevatorClue.FloorButtons: Thought = "층 버튼과 문 열림·닫힘 버튼이야. 멈췄을 때는 비상통화로 도움을 요청하자."; break;
                case ElevatorClue.LobbyCall: Thought = "엘리베이터를 불러 친구와 함께 타요."; break;
                case ElevatorClue.Advertisement: Thought = "주말에 열리는 우리 동네 농구 교실! 다음에는 밖에서 친구와 패스 연습을 해 보자."; break;
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
            if(clue==ElevatorClue.LobbyCall&&Flow.Step==ElevatorStep.Lobby)Continue();
            else if (clue == ElevatorClue.Intercom && Flow.Step == ElevatorStep.Stopped) Intercom();
            else if (clue == ElevatorClue.Identity && Flow.Step == ElevatorStep.Call) FindIdentity();
            else if ((clue == ElevatorClue.Ball || clue == ElevatorClue.Friend) && Flow.Step == ElevatorStep.Boarding) Continue();
            else if (clue == ElevatorClue.Friend && Flow.Step == ElevatorStep.Waiting) { if(Flow.Reassured)Encourage();else Reassure(); }
            else if (clue == ElevatorClue.Door && Flow.Step == ElevatorStep.Stopped) UnsafeDoor();
        }
        public bool CanInteract(ElevatorClue clue)
        {
            if(Flow==null||CinematicBlocking)return false;
            if(Flow.Step==ElevatorStep.Lobby)return clue==ElevatorClue.LobbyCall;
            if(Flow.Step==ElevatorStep.Boarding)return clue==ElevatorClue.Ball||clue==ElevatorClue.Friend;
            if(Flow.Step==ElevatorStep.Waiting)return clue==ElevatorClue.Friend;
            if(Flow.Step==ElevatorStep.Call)return Flow.SituationReported&&!Flow.LocationReported&&(clue==ElevatorClue.Identity||clue==ElevatorClue.Display);
            return Flow.Step==ElevatorStep.Stopped&&Flow.Phase==InteractionPhase.Investigation;
        }
        void OnDisable()
        {
            if (motor) motor.Stop(); if (signal) signal.Stop();
            if (friend) friend.localPosition = friendHome;
            ClearHover();
        }
        public object Snapshot() => new { step = Flow.Step.ToString(), phase = Flow.Phase.ToString(), Flow.PlayerHasBall, Flow.HelpRequested, Flow.LocationReported, Flow.UsedPhone, Flow.Reassured, Flow.ReadyToExit, Flow.HintLevel, Flow.JumpCount, Floor, motorPlaying = motor.isPlaying, friendHeight = friend.localPosition.y - friendHome.y, history = Flow.History.ToArray() };
    }
}
