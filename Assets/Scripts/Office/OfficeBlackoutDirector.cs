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
        [SerializeField, Min(0f)] float teacherAnnouncementDelayAfterDialogue = 1.5f;

        public OfficeBlackoutFlow Flow { get; private set; }
        public Transform TeacherTransform => teacherWaypointMover ? teacherWaypointMover.transform : null;
        public event Action<OfficeBlackoutFlow.Step> StepChanged;
        public bool ActionReady { get; private set; }
        public string ActionLabel => !ActionReady || Flow == null ? "" : Flow.CurrentStep switch
        {
            OfficeBlackoutFlow.Step.Errand => "서류 놓기",
            OfficeBlackoutFlow.Step.CalmFriend => "괜찮아. 내가 옆에 있어.",
            OfficeBlackoutFlow.Step.KeepFriendInPlace => "여기서 함께 선생님을 기다리자.",
            OfficeBlackoutFlow.Step.TeacherArrives => "선생님과 함께 이동하기",
            _ => ""
        };
        public bool PerformAction()
        {
            if (!ActionReady || OfficeSceneIntegration.GameplayBlocked) return false;
            return Flow.CurrentStep switch
            {
                OfficeBlackoutFlow.Step.Errand => PlaceDocuments(),
                OfficeBlackoutFlow.Step.CalmFriend => CalmFriend(),
                OfficeBlackoutFlow.Step.KeepFriendInPlace => AskFriendToStay(),
                OfficeBlackoutFlow.Step.TeacherArrives => BeginEscort(),
                _ => false
            };
        }
        Coroutine teacherAnnouncementRoutine;
        Coroutine teacherArrivalWatchRoutine;

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
            // Presence in a trigger is never the learner's reassuring response.
            return false;
        }
        public bool AcknowledgeFriendPanic() => Flow.AcknowledgeFriendPanic();
        public bool CalmFriend() => Transition(Flow.CalmFriend, onFriendCalmed);
        public bool AskFriendToStay() => Transition(Flow.AskFriendToStay, onFriendAskedToStay);
        public void HandleDialogueCompleted(int dialogueIndex)
        {
            if (Flow == null) return;
            if (dialogueIndex == 1 && Flow.CurrentStep == OfficeBlackoutFlow.Step.BlackoutReaction)
                AcknowledgeFriendPanic();
            if (dialogueIndex == 7 && Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand ||
                dialogueIndex == 1 && Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend ||
                dialogueIndex == 2 && Flow.CurrentStep == OfficeBlackoutFlow.Step.KeepFriendInPlace ||
                dialogueIndex == 5 && Flow.CurrentStep == OfficeBlackoutFlow.Step.TeacherArrives)
            {
                ActionReady = true;
                promptCard?.ShowPrompt(ActionLabel);
                OfficeSceneIntegration.Current?.RequireFreshClick();
            }
            if (dialogueIndex == 6 && CanFinishEscort) ActivateCompletionNotebook();

            // A short, pausable wait begins after the learner has read the reply.
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
                AnnounceTeacherIsComing();
            }
        }

        public bool AnnounceTeacherIsComing() => Transition(Flow.AnnounceTeacherIsComing, onTeacherAnnouncement);
        public bool TeacherArrives() => Transition(Flow.TeacherArrives, onTeacherArrives);
        public bool BeginEscort()
        {
            if (!Transition(Flow.BeginEscort, onEscortStarted)) return false;
            promptCard?.HidePrompt();

            var travel = GetComponent<OfficeEscortSequence>(); if (travel) travel.Begin();
            return true;
        }

        public bool CanFinishEscort =>
            Flow != null && Flow.CurrentStep == OfficeBlackoutFlow.Step.EscortToExit &&
            GetComponent<OfficeEscortSequence>() is var travel && travel && travel.WasRelocated &&
            !OfficeSceneIntegration.Current.EscortMoving;

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
            ActionReady = false;
            promptCard?.HidePrompt();
            onStepChanged?.Invoke(step);
            StepChanged?.Invoke(step);
        }

        void OnDestroy()
        {
            if (teacherAnnouncementRoutine != null)
                StopCoroutine(teacherAnnouncementRoutine);
            if (teacherArrivalWatchRoutine != null)
                StopCoroutine(teacherArrivalWatchRoutine);
            if (Flow != null) Flow.StepChanged -= HandleStepChanged;
        }
    }
}
