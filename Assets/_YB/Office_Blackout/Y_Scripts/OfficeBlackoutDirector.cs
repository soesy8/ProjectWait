using System;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// Scene-facing coordinator. Wire its UnityEvents to dialogue, lighting,
    /// character and audio presentations in the Inspector.
    /// </summary>
    public sealed class OfficeBlackoutDirector : MonoBehaviour
    {
        [Serializable] public sealed class StepEvent : UnityEvent<OfficeBlackoutFlow.Step> { }

        [Header("State notifications")]
        [SerializeField] StepEvent onStepChanged = new StepEvent();
        [SerializeField] UnityEvent onErrandStarted = new UnityEvent();
        [SerializeField] UnityEvent onBlackoutStarted = new UnityEvent();
        [SerializeField] UnityEvent onFriendCalmed = new UnityEvent();
        [SerializeField] UnityEvent onFriendAskedToStay = new UnityEvent();
        [SerializeField] UnityEvent onTeacherAnnouncement = new UnityEvent();
        [SerializeField] UnityEvent onTeacherArrives = new UnityEvent();
        [SerializeField] UnityEvent onEscortStarted = new UnityEvent();
        [SerializeField] UnityEvent onExperienceCompleted = new UnityEvent();

        public OfficeBlackoutFlow Flow { get; private set; }
        public event Action<OfficeBlackoutFlow.Step> StepChanged;

        void Awake()
        {
            Flow = new OfficeBlackoutFlow();
            Flow.StepChanged += HandleStepChanged;
        }

        void Start() => BeginErrand();

        public bool BeginErrand() => Transition(Flow.BeginErrand, onErrandStarted);
        public bool PlaceDocuments() => Transition(Flow.PlaceDocuments, onBlackoutStarted);
        public bool AcknowledgeFriendPanic() => Flow.AcknowledgeFriendPanic();
        public bool CalmFriend() => Transition(Flow.CalmFriend, onFriendCalmed);
        public bool AskFriendToStay() => Transition(Flow.AskFriendToStay, onFriendAskedToStay);
        public bool AnnounceTeacherIsComing() => Transition(Flow.AnnounceTeacherIsComing, onTeacherAnnouncement);
        public bool TeacherArrives() => Transition(Flow.TeacherArrives, onTeacherArrives);
        public bool BeginEscort() => Transition(Flow.BeginEscort, onEscortStarted);
        public bool ReachExit() => Transition(Flow.ReachExit, onExperienceCompleted);

        public void RestartExperience()
        {
            Flow.Reset();
            BeginErrand();
        }

        bool Transition(Func<bool> action, UnityEvent presentation)
        {
            if (Flow == null || !action()) return false;
            presentation?.Invoke();
            return true;
        }

        void HandleStepChanged(OfficeBlackoutFlow.Step step)
        {
            onStepChanged?.Invoke(step);
            StepChanged?.Invoke(step);
        }

        void OnDestroy()
        {
            if (Flow != null) Flow.StepChanged -= HandleStepChanged;
        }
    }
}
