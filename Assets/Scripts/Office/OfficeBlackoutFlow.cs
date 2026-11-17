using System;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Progression rules for the teachers' office blackout experience.</summary>
    public sealed class OfficeBlackoutFlow
    {
        public enum Step
        {
            NotStarted,
            Errand,
            BlackoutReaction,
            CalmFriend,
            KeepFriendInPlace,
            WaitForTeacher,
            TeacherAnnouncement,
            TeacherArrives,
            EscortToExit,
            Complete
        }

        public Step CurrentStep { get; private set; } = Step.NotStarted;
        public bool DocumentsPlaced { get; private set; }
        public bool FriendCalmed { get; private set; }
        public bool FriendAskedToStay { get; private set; }
        public bool TeacherAnnounced { get; private set; }
        public event Action<Step> StepChanged;

        public bool BeginErrand()
        {
            if (CurrentStep != Step.NotStarted) return false;
            Enter(Step.Errand);
            return true;
        }

        /// <summary>Called only after the player confirms placing the papers at the desk.</summary>
        public bool PlaceDocuments()
        {
            if (CurrentStep != Step.Errand) return false;
            DocumentsPlaced = true;
            Enter(Step.BlackoutReaction);
            return true;
        }

        public bool AcknowledgeFriendPanic()
        {
            if (CurrentStep != Step.BlackoutReaction) return false;
            Enter(Step.CalmFriend);
            return true;
        }

        public bool CalmFriend()
        {
            if (CurrentStep != Step.CalmFriend) return false;
            FriendCalmed = true;
            Enter(Step.KeepFriendInPlace);
            return true;
        }

        public bool AskFriendToStay()
        {
            if (CurrentStep != Step.KeepFriendInPlace || !FriendCalmed) return false;
            FriendAskedToStay = true;
            Enter(Step.WaitForTeacher);
            return true;
        }

        public bool AnnounceTeacherIsComing()
        {
            if (CurrentStep != Step.WaitForTeacher || !FriendAskedToStay) return false;
            TeacherAnnounced = true;
            Enter(Step.TeacherAnnouncement);
            return true;
        }

        public bool TeacherArrives()
        {
            if (CurrentStep != Step.TeacherAnnouncement || !TeacherAnnounced) return false;
            Enter(Step.TeacherArrives);
            return true;
        }

        public bool BeginEscort()
        {
            if (CurrentStep != Step.TeacherArrives) return false;
            Enter(Step.EscortToExit);
            return true;
        }

        public bool ReachExit()
        {
            if (CurrentStep != Step.EscortToExit || !TeacherAnnounced) return false;
            Enter(Step.Complete);
            return true;
        }

        public void Reset()
        {
            DocumentsPlaced = false;
            FriendCalmed = false;
            FriendAskedToStay = false;
            TeacherAnnounced = false;
            Enter(Step.NotStarted);
        }

        void Enter(Step next)
        {
            if (CurrentStep == next) return;
            CurrentStep = next;
            StepChanged?.Invoke(next);
        }
    }
}
