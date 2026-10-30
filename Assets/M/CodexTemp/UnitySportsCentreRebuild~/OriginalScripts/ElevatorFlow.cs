using System.Collections.Generic;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorStep { Lobby, Boarding, Proposal, Jump, Stopped, Connecting, Call, Unanswered, Waiting, Retry, Rescue, Complete, Entering, Exiting }
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
        public int Encouragements { get; private set; }
        public bool Reassured { get; private set; }
        public bool UsedPhone { get; private set; }
        public bool ReadyToExit { get; private set; }
        public int HintLevel { get; private set; }
        public int Revision { get; private set; }
        public int JumpCount { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsMoving => Step == ElevatorStep.Boarding || Step == ElevatorStep.Proposal || Step == ElevatorStep.Jump;
        public bool IsCinematic => Step==ElevatorStep.Entering || Step==ElevatorStep.Exiting || Step==ElevatorStep.Proposal || Step==ElevatorStep.Jump || Step==ElevatorStep.Connecting || (Step==ElevatorStep.Stopped&&Phase==InteractionPhase.Dialogue) || (Step==ElevatorStep.Rescue&&!ReadyToExit);
        public bool CanObserve => Phase == InteractionPhase.Investigation || Step == ElevatorStep.Boarding || Step == ElevatorStep.Waiting || (Step == ElevatorStep.Call && SituationReported && !LocationReported);
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
            Encouragements = 0;
            JumpCount = 0; retryReturn = ElevatorStep.Stopped; Enter(ElevatorStep.Lobby);
        }
        public void Continue()
        {
            switch (Step)
            {
                case ElevatorStep.Lobby:
                    History.Add("농구를 마치고 친구와 엘리베이터에 탑승"); Enter(ElevatorStep.Entering); break;
                case ElevatorStep.Boarding:
                    History.Add("친구의 농구공을 잠깐 받아 줌"); Enter(ElevatorStep.Proposal); break;
                case ElevatorStep.Proposal:
                    History.Add("친구가 슛 자세를 재현함 · 플레이어는 뛰지 않음");
                    JumpCount++; Enter(ElevatorStep.Jump); break;
                case ElevatorStep.Stopped:
                    if (Phase == InteractionPhase.Dialogue) Enter(ElevatorStep.Stopped, InteractionPhase.Investigation);
                    break;
                case ElevatorStep.Rescue:
                    if (ReadyToExit)
                    {
                        History.Add("구조원의 안내를 확인하고 함께 나옴"); Enter(ElevatorStep.Exiting);
                    }
                    break;
            }
        }
        public void RequestIntercom()
        {
            if (Step != ElevatorStep.Stopped || !CanObserve) return;
            History.Add("대처: 비상통화 장치를 찾아 도움 요청"); Enter(ElevatorStep.Connecting);
        }
        public void RequestPhone()
        {
            if (Step != ElevatorStep.Unanswered) return;
            UsedPhone = true; History.Add("대처: 휴대폰 119 모의 신고"); Enter(ElevatorStep.Call, InteractionPhase.Choice);
        }
        public void ReportLocation()
        {
            if (Step != ElevatorStep.Call || !SituationReported || !IdentityFound || LocationReported) return;
            LocationReported = true; History.Add("위치 표시를 확인하고 건물명과 승강기 번호 전달"); Revision++;
        }
        public void ReportSituation()
        {
            if (Step != ElevatorStep.Call || SituationReported) return;
            SituationReported = HelpRequested = true;
            History.Add("엘리베이터가 멈췄어요. 친구랑 둘이 있어요."); Elapsed = 0; Revision++;
        }
        public void FindIdentity()
        {
            if (Step != ElevatorStep.Call || !SituationReported || IdentityFound) return;
            IdentityFound = true; History.Add("벽의 위치 표시를 직접 찾아 확인함"); Revision++;
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
            Reassured = true; History.Add("친구에게: 문은 건드리지 말고 같이 기다리자.");
            Enter(ElevatorStep.Waiting);
        }
        public void UnsafeAction(string action)
        {
            if (!(Step == ElevatorStep.Stopped && CanObserve) && Step != ElevatorStep.Unanswered && Step != ElevatorStep.Waiting && Step != ElevatorStep.Call) return;
            retryReturn = Step; History.Add("첫 시도 기록: " + action + " → Wait! 중단 후 재도전"); Enter(ElevatorStep.Retry, InteractionPhase.Choice);
        }
        public void Retry()
        {
            if (Step != ElevatorStep.Retry) return;
            Enter(retryReturn, retryReturn == ElevatorStep.Stopped ? InteractionPhase.Investigation :
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
                    if(Elapsed>=settings.exitSeconds)Enter(ElevatorStep.Complete);
                    break;
                case ElevatorStep.Proposal:
                    if (Elapsed >= .9f) Continue();
                    break;
                case ElevatorStep.Stopped:
                    if (Phase == InteractionPhase.Dialogue && Elapsed >= 1.8f) Continue();
                    break;
                case ElevatorStep.Jump:
                    if (Elapsed >= settings.friendReplySeconds + settings.jumpSeconds + settings.afterJumpSeconds)
                    {
                        History.Add("잠시 뒤 엘리베이터 운행이 멈춤"); Enter(ElevatorStep.Stopped);
                    }
                    break;
                case ElevatorStep.Connecting:
                    if (Elapsed >= settings.connectionSeconds)
                    {
                        History.Add(NoResponse ? "비상통화: 응답 없음" : "비상통화: 관리실 연결");
                        Enter(NoResponse ? ElevatorStep.Unanswered : ElevatorStep.Call, InteractionPhase.Choice);
                    }
                    break;
                case ElevatorStep.Waiting:
                    if (HelpRequested && LocationReported && Reassured && Elapsed >= settings.rescueWaitSeconds) Enter(ElevatorStep.Rescue);
                    break;
                case ElevatorStep.Rescue:
                    if (!ReadyToExit && Elapsed >= settings.rescueSubtitleSeconds) { ReadyToExit = true; Revision++; }
                    break;
            }
            if ((Step == ElevatorStep.Stopped && CanObserve) || Step == ElevatorStep.Unanswered || (Step == ElevatorStep.Call && SituationReported && !IdentityFound))
            {
                int hint = Elapsed >= settings.secondHintSeconds ? 2 : Elapsed >= settings.firstHintSeconds ? 1 : 0;
                if (hint != HintLevel) { HintLevel = hint; Revision++; }
            }
        }
        public void DeveloperJump(int stage)
        {
            Reset(); if (stage <= 0) return;
            History.Add("개발용 단계 이동: 앞 단계 생략");
            if (stage == 1) { Enter(ElevatorStep.Boarding); return; }
            if (stage == 2) { JumpCount = 1; Enter(ElevatorStep.Jump); return; }
            if (stage == 3) { Enter(ElevatorStep.Stopped); return; }
            if (stage == 4) { Enter(ElevatorStep.Call, InteractionPhase.Choice); return; }
            SituationReported = IdentityFound = LocationReported = HelpRequested = true;
            if (stage == 5) { Enter(ElevatorStep.Waiting, InteractionPhase.Choice); return; }
            Reassured = true; Enter(ElevatorStep.Rescue);
        }
    }
}
