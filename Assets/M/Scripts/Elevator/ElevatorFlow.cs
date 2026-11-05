using System.Collections.Generic;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorStep { Lobby, Boarding, Proposal, Jump, Stopped, Connecting, Call, Unanswered, Waiting, Retry, Rescue, Complete, Entering, Exiting, Travel, ReturnLobby, Returning, GroundFloor, GoingHome, WaitingFade, PosterPointing, PosterWarning, CallQuestion, CallResponse, CallLiftQuestion, CallIdentityGuide, CallLocationResponse, CallSafetyAdvice, DoorLean, DoorPosterPointing, DoorPosterWarning, SafeWallMove, SafeWallWaiting }
    public enum InteractionPhase { Dialogue, Investigation, Choice }

    // PC, XR and verification all use this state owner. The player's body never jumps.
    public sealed class ElevatorFlow
    {
        public ElevatorStep Step { get; private set; }
        public InteractionPhase Phase { get; private set; }
        public readonly List<string> History = new List<string>();
        public bool NoResponse { get; set; }
        public bool HelpRequested { get; private set; }
        public bool LocationReported { get; private set; }
        public bool SituationReported { get; private set; }
        public bool IdentityFound { get; private set; }
        public bool DoorWarningGiven { get; private set; }
        public bool SafeWallReached { get; private set; }
        public int Encouragements { get; private set; }
        public bool Reassured { get; private set; }
        public bool UsedPhone { get; private set; }
        public bool ReadyToExit { get; private set; }
        public int HintLevel { get; private set; }
        public int Revision { get; private set; }
        public int JumpCount { get; private set; }
        public bool DestinationSelected { get; private set; }
        public string FirstJudgment { get; private set; } = "";
        public bool ProposalRetry => Step == ElevatorStep.Retry && retryReturn == ElevatorStep.Proposal;
        public float Elapsed { get; private set; }
        public bool AwaitingPoster => Step == ElevatorStep.PosterPointing || Step == ElevatorStep.DoorPosterPointing;
        public bool SearchingIdentity => Step == ElevatorStep.Call && Phase == InteractionPhase.Investigation && SituationReported && !IdentityFound;
        public bool IsCallCutscene => Step == ElevatorStep.Connecting || Step == ElevatorStep.CallQuestion || Step == ElevatorStep.CallResponse || Step == ElevatorStep.CallLiftQuestion || Step == ElevatorStep.CallIdentityGuide || Step == ElevatorStep.CallLocationResponse || Step == ElevatorStep.CallSafetyAdvice;
        public bool IsDoorSafetyCutscene => Step == ElevatorStep.DoorLean || Step == ElevatorStep.DoorPosterPointing || Step == ElevatorStep.DoorPosterWarning || Step == ElevatorStep.SafeWallMove || Step == ElevatorStep.SafeWallWaiting;
        public bool IsShotCutscene => Step == ElevatorStep.Proposal || Step == ElevatorStep.PosterPointing || Step == ElevatorStep.PosterWarning || Step == ElevatorStep.Jump || Step == ElevatorStep.Stopped && Phase == InteractionPhase.Dialogue;
        public bool IsMoving => Step == ElevatorStep.Travel || Step == ElevatorStep.Jump;
        public bool IsCinematic => IsShotCutscene || IsCallCutscene || IsDoorSafetyCutscene || Step==ElevatorStep.WaitingFade || Step==ElevatorStep.Entering || Step==ElevatorStep.Exiting || Step==ElevatorStep.Travel || Step==ElevatorStep.Returning || Step==ElevatorStep.GoingHome || (Step==ElevatorStep.Stopped&&Phase==InteractionPhase.Dialogue) || (Step==ElevatorStep.Rescue&&!ReadyToExit);
        public bool CanObserve => Phase == InteractionPhase.Investigation || Step == ElevatorStep.Boarding || Step == ElevatorStep.Waiting || SearchingIdentity;
        public bool PlayerHasBall => Step != ElevatorStep.Lobby && Step != ElevatorStep.Boarding && Step != ElevatorStep.Complete && Step != ElevatorStep.Entering;
        readonly ElevatorSettings settings;
        ElevatorStep retryReturn;

        public ElevatorFlow(ElevatorSettings value) { settings = value; Reset(); }
        void Enter(ElevatorStep step, InteractionPhase phase = InteractionPhase.Dialogue)
        {
            Step = step; Phase = phase; Elapsed = 0; HintLevel = 0; Revision++;
        }
        public void Reset()
        {
            History.Clear(); HelpRequested = LocationReported = SituationReported = IdentityFound = Reassured = UsedPhone = ReadyToExit = false;
            DoorWarningGiven = SafeWallReached = false;
            Encouragements = 0;
            JumpCount = 0; DestinationSelected = false; FirstJudgment = ""; retryReturn = ElevatorStep.Stopped; Enter(ElevatorStep.Lobby);
        }
        public void Continue()
        {
            switch (Step)
            {
                case ElevatorStep.Lobby:
                    History.Add("한빛 생활체육센터 3층에서 농구를 마치고 1층으로 귀가 준비"); Enter(ElevatorStep.Entering); break;
                case ElevatorStep.Boarding:
                    if (!DestinationSelected) return;
                    History.Add("친구의 농구공을 받아 안고 슛 이야기를 들음"); Enter(ElevatorStep.Proposal); break;
                case ElevatorStep.Stopped:
                    if (Phase == InteractionPhase.Dialogue) Enter(ElevatorStep.Stopped, InteractionPhase.Investigation);
                    break;
                case ElevatorStep.Rescue:
                    if (ReadyToExit)
                    {
                        History.Add("구조원의 안내를 확인하고 함께 나옴"); Enter(ElevatorStep.Exiting);
                    }
                    break;
                case ElevatorStep.ReturnLobby:
                    History.Add("정지한 승강기를 다시 이용하지 않고 직원 안내로 계단을 이용함"); Enter(ElevatorStep.Returning); break;
                case ElevatorStep.GroundFloor:
                    History.Add("1층 로비의 주출입구로 귀가"); Enter(ElevatorStep.GoingHome); break;
            }
        }
        public void SelectDestination(string panel = "낮은 측면")
        {
            if (Step != ElevatorStep.Boarding || DestinationSelected) return;
            DestinationSelected = true; History.Add(panel+" 패널에서 목적지 1층을 선택함"); Revision++;
        }
        public void PointAtSafetyPoster()
        {
            if (!AwaitingPoster) return;
            if (Step == ElevatorStep.DoorPosterPointing)
            {
                DoorWarningGiven = true;
                History.Add("안전 포스터를 다시 가리켜 친구에게 문에 기대면 안 된다고 알려 줌");
                Enter(ElevatorStep.DoorPosterWarning); return;
            }
            FirstJudgment = "안전 포스터를 가리키며 뛰지 말라고 말함";
            History.Add("안전 포스터를 직접 가리켜 친구에게 안에서는 뛰지 말자고 말함");
            Enter(ElevatorStep.PosterWarning);
        }
        public void RequestIntercom()
        {
            if (Step != ElevatorStep.Stopped || !CanObserve) return;
            History.Add("대처: 비상통화 장치를 찾아 도움 요청"); Enter(ElevatorStep.Connecting);
        }
        public void RequestPhone()
        {
            if (Step != ElevatorStep.Unanswered) return;
            UsedPhone = true; History.Add("대처: 휴대폰 119 모의 신고"); Enter(ElevatorStep.CallQuestion);
        }
        public void ReportLocation()
        {
            if (Step != ElevatorStep.Call || !SituationReported || !IdentityFound || LocationReported) return;
            LocationReported = true; History.Add("위치 표시를 확인하고 건물명과 승강기 번호 전달"); Revision++;
        }
        public void ReportSituation()
        {
            if (Step != ElevatorStep.CallQuestion || SituationReported) return;
            SituationReported = HelpRequested = true;
            History.Add("엘리베이터가 고장 나서 움직이지 않아요. 친구랑 둘이 있어요."); Enter(ElevatorStep.CallResponse);
        }
        public void FindIdentity()
        {
            if (!SearchingIdentity) return;
            IdentityFound = LocationReported = true;
            History.Add("벽의 위치 표시를 직접 찾아 건물명과 승강기 번호를 전달함");
            Enter(ElevatorStep.CallLocationResponse);
        }
        public void Encourage()
        {
            if (Step != ElevatorStep.Waiting || !Reassured || Encouragements >= 3) return;
            Encouragements++; Revision++;
        }
        public void FollowAdvice()
        {
            if (Step != ElevatorStep.Call || !LocationReported) return;
            HelpRequested = true; History.Add("대처: 안내를 듣고 문에서 떨어져 대기"); Enter(ElevatorStep.Waiting, InteractionPhase.Choice);
        }
        public void Reassure()
        {
            if (Step != ElevatorStep.Waiting || Reassured) return;
            Reassured = true; History.Add("친구에게: 도움을 요청했으니 여기서 천천히 숨을 고르며 함께 기다리자.");
            Enter(ElevatorStep.WaitingFade);
        }
        public void UnsafeAction(string action)
        {
            if (!(Step == ElevatorStep.Stopped && CanObserve) && Step != ElevatorStep.Proposal && Step != ElevatorStep.Unanswered && Step != ElevatorStep.Waiting && Step != ElevatorStep.Call) return;
            retryReturn = Step; History.Add("첫 시도 기록: " + action + " → Wait! 중단 후 재도전"); Enter(ElevatorStep.Retry, InteractionPhase.Choice);
        }
        public void Retry()
        {
            if (Step != ElevatorStep.Retry) return;
            Enter(retryReturn, retryReturn == ElevatorStep.Stopped || retryReturn == ElevatorStep.Call && SituationReported && !IdentityFound ? InteractionPhase.Investigation :
                retryReturn == ElevatorStep.Waiting && Reassured ? InteractionPhase.Dialogue : InteractionPhase.Choice);
        }
        public void Tick(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Elapsed += seconds;
            switch (Step)
            {
                case ElevatorStep.Entering:
                    if(Elapsed>=settings.boardingSeconds)Enter(ElevatorStep.Boarding);
                    break;
                case ElevatorStep.Exiting:
                    if(Elapsed>=settings.exitSeconds)Enter(ElevatorStep.ReturnLobby, InteractionPhase.Choice);
                    break;
                case ElevatorStep.Returning:
                    if (Elapsed >= 3.5f) Enter(ElevatorStep.GroundFloor, InteractionPhase.Choice);
                    break;
                case ElevatorStep.GoingHome:
                    if (Elapsed >= 6) Enter(ElevatorStep.Complete);
                    break;
                case ElevatorStep.Stopped:
                    if (Phase == InteractionPhase.Dialogue && Elapsed >= 1.8f) Continue();
                    break;
                case ElevatorStep.Jump:
                    if (Elapsed >= settings.friendReplySeconds + settings.jumpSeconds + settings.afterJumpSeconds) StopForFault();
                    break;
                case ElevatorStep.Proposal:
                    if (Elapsed >= settings.shotStorySeconds) Enter(ElevatorStep.PosterPointing, InteractionPhase.Investigation);
                    break;
                case ElevatorStep.PosterWarning:
                    if (Elapsed >= settings.posterWarningSeconds)
                    {
                        JumpCount++; History.Add("친구가 만류를 듣지 않고 슛을 흉내 내며 점프함 · 체험자는 뛰지 않음");
                        Enter(ElevatorStep.Jump);
                    }
                    break;
                case ElevatorStep.WaitingFade:
                    if (Elapsed >= 4.8f) Enter(ElevatorStep.Rescue);
                    break;
                case ElevatorStep.Travel:
                    if (Elapsed >= 2.8f)
                    {
                        StopForFault();
                    }
                    break;
                case ElevatorStep.Connecting:
                    if (Elapsed >= settings.connectionSeconds)
                    {
                        History.Add(NoResponse ? "비상통화: 응답 없음" : "비상통화: 관리실 연결");
                        Enter(NoResponse ? ElevatorStep.Unanswered : ElevatorStep.CallQuestion);
                    }
                    break;
                case ElevatorStep.CallQuestion:
                    if (Elapsed >= settings.callQuestionSeconds) ReportSituation();
                    break;
                case ElevatorStep.CallResponse:
                    if (Elapsed >= settings.callReplySeconds) Enter(ElevatorStep.CallLiftQuestion);
                    break;
                case ElevatorStep.CallLiftQuestion:
                    if (Elapsed >= settings.callQuestionSeconds) Enter(ElevatorStep.CallIdentityGuide);
                    break;
                case ElevatorStep.CallIdentityGuide:
                    if (Elapsed >= settings.callLocationGuideSeconds)
                    {
                        History.Add("전면 비상벨 아래 식별판에 위치가 적혀 있다는 안내를 듣고 직접 찾기 시작함");
                        Enter(ElevatorStep.Call, InteractionPhase.Investigation);
                    }
                    break;
                case ElevatorStep.CallLocationResponse:
                    if (Elapsed >= settings.locationReportSeconds) Enter(ElevatorStep.CallSafetyAdvice);
                    break;
                case ElevatorStep.CallSafetyAdvice:
                    if (Elapsed >= settings.safetyAdviceSeconds) Enter(ElevatorStep.DoorLean);
                    break;
                case ElevatorStep.DoorLean:
                    if (Elapsed >= settings.doorLeanSeconds) Enter(ElevatorStep.DoorPosterPointing, InteractionPhase.Investigation);
                    break;
                case ElevatorStep.DoorPosterWarning:
                    if (Elapsed >= settings.doorWarningSeconds && DoorWarningGiven) Enter(ElevatorStep.SafeWallMove);
                    break;
                case ElevatorStep.SafeWallMove:
                    if (Elapsed >= settings.safeWallMoveSeconds)
                    {
                        SafeWallReached = Reassured = true;
                        History.Add("친구가 문에서 떨어진 옆 벽으로 이동하여 안전하게 함께 기다림");
                        Enter(ElevatorStep.SafeWallWaiting);
                    }
                    break;
                case ElevatorStep.SafeWallWaiting:
                    if (Elapsed >= settings.safeWallSettleSeconds) Enter(ElevatorStep.WaitingFade);
                    break;
                case ElevatorStep.Waiting:
                    if (HelpRequested && LocationReported && Reassured && Elapsed >= settings.rescueWaitSeconds) Enter(ElevatorStep.Rescue);
                    break;
                case ElevatorStep.Rescue:
                    if (!ReadyToExit && Elapsed >= settings.rescueSubtitleSeconds) { ReadyToExit = true; Revision++; }
                    break;
            }
            if ((Step == ElevatorStep.Stopped && CanObserve) || Step == ElevatorStep.Unanswered || SearchingIdentity)
            {
                int hint = Elapsed >= settings.secondHintSeconds ? 2 : Elapsed >= settings.firstHintSeconds ? 1 : 0;
                if (hint != HintLevel) { HintLevel = hint; Revision++; }
            }
        }
        void StopForFault()
        {
            History.Add("엘리베이터가 덜컹하며 운행 정지 · 고장 대처 체험으로 진행");
            Enter(ElevatorStep.Stopped);
        }
        public void DeveloperJump(int stage)
        {
            Reset(); if (stage <= 0) return;
            History.Add("개발용 단계 이동: 앞 단계 생략");
            if (stage == 1) { Enter(ElevatorStep.Boarding); return; }
            if (stage == 2) { DestinationSelected = true; Enter(ElevatorStep.Proposal); return; }
            if (stage == 3) { Enter(ElevatorStep.Stopped); return; }
            if (stage == 4) { Enter(ElevatorStep.CallQuestion); return; }
            SituationReported = IdentityFound = LocationReported = HelpRequested = true;
            if (stage == 5) { Enter(ElevatorStep.Waiting, InteractionPhase.Choice); return; }
            Reassured = true; Enter(ElevatorStep.Rescue);
        }
    }
}
