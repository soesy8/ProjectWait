using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WaitForSecondsRealtime = ProjectWait.YB.OfficeBlackout.OfficeStoryWait;

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

        [Header("Scene objects")]
        [SerializeField] Transform missionDocuments;
        [SerializeField] OfficeBlackoutCinematicOverlay cinematicOverlay;
        [SerializeField] OfficeBlackoutWaypointMover friendWaypointMover;
        [SerializeField] OfficeBlackoutWaypointMover teacherWaypointMover;
        [SerializeField] OfficeBlackoutPromptCard promptCard;
        [SerializeField] Transform teacherEntry;
        [SerializeField] GameObject completionNotebook;

        [Header("Automatic progression")]
        [SerializeField, Min(0f)] float teacherAnnouncementDelayAfterDialogue = 5f;

        public OfficeBlackoutFlow Flow { get; private set; }
        public Transform TeacherTransform => teacherWaypointMover ? teacherWaypointMover.transform : null;
        public event Action<OfficeBlackoutFlow.Step> StepChanged;
        Coroutine teacherAnnouncementRoutine;
        Coroutine teacherArrivalWatchRoutine;
        Coroutine escortArrivalWatchRoutine;

        void Awake()
        {
            Flow = new OfficeBlackoutFlow();
            Flow.StepChanged += HandleStepChanged;
            if (completionNotebook) completionNotebook.SetActive(false);
        }

        IEnumerator Start() { while (OfficeStoryTime.Frozen) yield return null; BeginErrand(); }

        public bool BeginErrand() => Transition(Flow.BeginErrand, onErrandStarted);
        public bool PlaceDocuments() => Transition(Flow.PlaceDocuments, onBlackoutStarted);
        public void ActivateMissionDocuments()
        {
            if (missionDocuments) missionDocuments.gameObject.SetActive(true);
        }
        public void PlayDialogue(int dialogueIndex)
        {
            if (cinematicOverlay) cinematicOverlay.PlayDialogue(dialogueIndex);
            else Debug.LogWarning("OfficeBlackoutDirector has no cinematic overlay assigned.", this);
        }
        public void StartFriendRoute() => friendWaypointMover?.StartRoute();
        public void ContinueFriendRoute() => friendWaypointMover?.ContinueRoute();
        public void ContinueTeacherRoute() => teacherWaypointMover?.ContinueRoute();
        public void MoveFriendToNextWaypoint() => friendWaypointMover?.MoveToNextWaypoint();
        public void StopFriendMovement() => friendWaypointMover?.StopMovement();
        public bool PlayerReachedExitDesk()
        {
            if (Flow == null || !Flow.AcknowledgeFriendPanic()) return false;
            return CalmFriend();
        }
        public bool AcknowledgeFriendPanic() => Flow.AcknowledgeFriendPanic();
        public bool CalmFriend() => Transition(Flow.CalmFriend, onFriendCalmed);
        public bool AskFriendToStay() => Transition(Flow.AskFriendToStay, onFriendAskedToStay);
        public void HandleDialogueCompleted(int dialogueIndex)
        {
            if (Flow == null) return;

            // Dialogue 3 is played by onFriendAskedToStay. Start the five-second
            // countdown after that complete caption/letterbox sequence has ended.
            if (dialogueIndex == 3 && Flow.CurrentStep == OfficeBlackoutFlow.Step.WaitForTeacher)
            {
                if (teacherAnnouncementRoutine != null)
                    StopCoroutine(teacherAnnouncementRoutine);
                teacherAnnouncementRoutine = StartCoroutine(AnnounceTeacherAfterDelay());
                return;
            }

            // Dialogue 4 is the teacher's announcement. Reveal the teacher only
            // after that entire letterbox sequence has completed.
            if (dialogueIndex == 4 && Flow.CurrentStep == OfficeBlackoutFlow.Step.TeacherAnnouncement)
                StartTeacherRoute();

            // Dialogue 5 is the teacher-arrival event's final presentation.
            // Show Element 3 only after its captions and letterbox have ended.
            if (dialogueIndex == 5 && Flow.CurrentStep == OfficeBlackoutFlow.Step.TeacherArrives)
                promptCard?.ShowPresetPrompt(3);
        }

        void StartTeacherRoute()
        {
            if (!teacherWaypointMover || !teacherEntry)
            {
                Debug.LogWarning("[OfficeBlackout] Cannot start teacher arrival: teacher mover or TeacherEntry is not assigned.", this);
                return;
            }

            if (teacherArrivalWatchRoutine != null)
            {
                StopCoroutine(teacherArrivalWatchRoutine);
                teacherArrivalWatchRoutine = null;
            }

            Transform teacherTransform = teacherWaypointMover.transform;
            teacherWaypointMover.StopMovement();
            teacherTransform.SetPositionAndRotation(teacherEntry.position, teacherEntry.rotation);
            teacherWaypointMover.gameObject.SetActive(true);
            teacherWaypointMover.StartRoute();
            Debug.Log($"[OfficeBlackout] Teacher activated at {teacherEntry.name}; waypoint route started.", this);
            teacherArrivalWatchRoutine = StartCoroutine(WaitForTeacherAtComePoint());
        }

        IEnumerator WaitForTeacherAtComePoint()
        {
            while (OfficeStoryTime.Frozen || teacherWaypointMover && teacherWaypointMover.IsMoving)
                yield return null;

            teacherArrivalWatchRoutine = null;
            // The configured teacher route pauses after waypoint index 1 (TeacherCome).
            if (teacherWaypointMover && teacherWaypointMover.CurrentWaypointIndex >= 2 &&
                Flow != null && Flow.CurrentStep == OfficeBlackoutFlow.Step.TeacherAnnouncement)
            {
                Debug.Log("[OfficeBlackout] Teacher reached TeacherCome; onTeacherArrives is starting.", this);
                TeacherArrives();
            }
        }

        IEnumerator AnnounceTeacherAfterDelay()
        {
            yield return new WaitForSecondsRealtime(teacherAnnouncementDelayAfterDialogue);
            teacherAnnouncementRoutine = null;

            if (Flow != null && Flow.CurrentStep == OfficeBlackoutFlow.Step.WaitForTeacher)
            {
                Debug.Log("[OfficeBlackout] Five seconds after dialogue 3; announcing that the teacher is coming.", this);
                AnnounceTeacherIsComing();
            }
        }

        public bool AnnounceTeacherIsComing() => Transition(Flow.AnnounceTeacherIsComing, onTeacherAnnouncement);
        public bool TeacherArrives() => Transition(Flow.TeacherArrives, onTeacherArrives);
        public bool BeginEscort()
        {
            if (!Transition(Flow.BeginEscort, onEscortStarted)) return false;
            promptCard?.HidePrompt();

            if (escortArrivalWatchRoutine != null)
                StopCoroutine(escortArrivalWatchRoutine);
            escortArrivalWatchRoutine = StartCoroutine(WaitForEscortRoutes());
            var travel = GetComponent<OfficeEscortSequence>(); if (travel) travel.Begin();
            return true;
        }

        IEnumerator WaitForEscortRoutes()
        {
            while (OfficeStoryTime.Frozen || friendWaypointMover && teacherWaypointMover &&
                   (!friendWaypointMover.IsRouteComplete || !teacherWaypointMover.IsRouteComplete))
                yield return null;

            escortArrivalWatchRoutine = null;
            if (Flow != null && Flow.CurrentStep == OfficeBlackoutFlow.Step.EscortToExit &&
                friendWaypointMover && teacherWaypointMover &&
                friendWaypointMover.IsRouteComplete && teacherWaypointMover.IsRouteComplete)
            {
                Debug.Log("[OfficeBlackout] Friend and teacher completed their escort routes; showing prompt Element 4.", this);
                promptCard?.ShowPresetPrompt(4);
            }
        }

        public bool CanFinishEscort =>
            Flow != null && Flow.CurrentStep == OfficeBlackoutFlow.Step.EscortToExit &&
            friendWaypointMover && teacherWaypointMover &&
            friendWaypointMover.IsRouteComplete && teacherWaypointMover.IsRouteComplete;

        public void FadeToBlack()
        {
            promptCard?.HidePrompt();
            if (cinematicOverlay) cinematicOverlay.FadeToBlack();
            else Debug.LogWarning("OfficeBlackoutDirector has no cinematic overlay assigned.", this);
        }

        public void ActivateCompletionNotebook()
        {
            ReachExit();
            var integration = OfficeSceneIntegration.Current;
            if (integration && integration.presentation) integration.presentation.ShowCompletion();
            else if (completionNotebook) completionNotebook.SetActive(true);
            else Debug.LogWarning("OfficeBlackoutDirector has no completion notebook assigned.", this);
            if (cinematicOverlay) cinematicOverlay.FadeFromBlack();
        }
        public bool ReachExit() => Transition(Flow.ReachExit, onExperienceCompleted);

        public void RestartExperience()
        {
            if (teacherAnnouncementRoutine != null)
            {
                StopCoroutine(teacherAnnouncementRoutine);
                teacherAnnouncementRoutine = null;
            }
            if (teacherArrivalWatchRoutine != null)
            {
                StopCoroutine(teacherArrivalWatchRoutine);
                teacherArrivalWatchRoutine = null;
            }
            if (escortArrivalWatchRoutine != null)
            {
                StopCoroutine(escortArrivalWatchRoutine);
                escortArrivalWatchRoutine = null;
            }
            if (teacherWaypointMover)
            {
                teacherWaypointMover.StopMovement();
                teacherWaypointMover.gameObject.SetActive(false);
            }
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
            if (teacherAnnouncementRoutine != null)
                StopCoroutine(teacherAnnouncementRoutine);
            if (teacherArrivalWatchRoutine != null)
                StopCoroutine(teacherArrivalWatchRoutine);
            if (escortArrivalWatchRoutine != null)
                StopCoroutine(escortArrivalWatchRoutine);
            if (Flow != null) Flow.StepChanged -= HandleStepChanged;
        }
    }
}
