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
        [SerializeField] OfficeBlackoutCinematicOverlay cinematicOverlay;
        [SerializeField] OfficeBlackoutWaypointMover friendWaypointMover;
        [SerializeField] OfficeBlackoutWaypointMover teacherWaypointMover;
        [SerializeField] OfficeBlackoutPromptCard promptCard;
        [SerializeField] OfficeWrongTurn wrongTurn;
        [SerializeField] Transform teacherEntry;
        [SerializeField] Transform exitDoor;
        Quaternion closedDoorRotation;
        [SerializeField] GameObject completionNotebook;

        [Header("Automatic progression")]
        [SerializeField, Min(0f)] float teacherAnnouncementDelayAfterDialogue = 1.5f;

        public OfficeBlackoutFlow Flow { get; private set; }
        public Transform TeacherTransform => teacherWaypointMover ? teacherWaypointMover.transform : null;
        public event Action<OfficeBlackoutFlow.Step> StepChanged;
        public bool ActionReady { get; private set; }
        public enum ActionPhase { Inspect, Choose, Retry, Consequence }
        public ActionPhase Phase { get; private set; }
        public bool Choosing => ActionReady && Phase == ActionPhase.Choose;
        public bool Failed => ActionReady && Phase == ActionPhase.Retry;
        Coroutine consequence;
        public bool ConsequencePlaying => consequence != null || Phase == ActionPhase.Consequence;
        public string OtherReply => Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend ?
            "혼자 먼저 나가 봐." : "문 밖으로 가서 찾아보자.";
        public string Instruction => Failed ? (Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend ?
            "친구가 책상에 부딪혔어. 옆에서 같이 기다리자." : "앞이 잘 안 보여. 여기서 같이 기다리는 게 좋겠어.") :
            Choosing ? "친구에게 뭐라고 말하면 좋을까?" : Flow.CurrentStep switch
            {
                OfficeBlackoutFlow.Step.Errand => "책상에 빈자리가 있네. 책은 저기에 놓자.",
                OfficeBlackoutFlow.Step.CalmFriend => "친구 목소리가 떨려. 옆에서 안심시켜 주자.",
                OfficeBlackoutFlow.Step.TeacherArrives => "선생님이 길을 비춰 주시네. 친구랑 같이 따라가자.",
                _ => ""
            };
        public string ActionLabel => !ActionReady || Flow == null ? "" : Flow.CurrentStep switch
        {
            OfficeBlackoutFlow.Step.Errand => "책 놓기",
            OfficeBlackoutFlow.Step.CalmFriend => "괜찮아. 내가 옆에 있어.",
            OfficeBlackoutFlow.Step.KeepFriendInPlace => "여기서 함께 선생님을 기다리자.",
            OfficeBlackoutFlow.Step.TeacherArrives => "선생님과 함께 이동하기",
            _ => ""
        };
        public bool SelectWorldTarget()
        {
            if (!ActionReady || Phase != ActionPhase.Inspect || OfficeSceneIntegration.GameplayBlocked) return false;
            if (Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend)
            {
                SetActionPhase(ActionPhase.Choose);
                return true;
            }
            return Flow.CurrentStep switch
            {
                OfficeBlackoutFlow.Step.Errand => PlaceDocuments(),
                OfficeBlackoutFlow.Step.TeacherArrives => BeginEscort(),
                _ => false
            };
        }
        public bool Answer(bool safe)
        {
            if (!Choosing || OfficeSceneIntegration.GameplayBlocked) return false;
            if (!safe)
            {
                if (Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend)
                    consequence = StartCoroutine(ShowWrongTurn());
                else SetActionPhase(ActionPhase.Retry);
                return true;
            }
            return Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend ? CalmFriend() : AskFriendToStay();
        }
        public bool RetryAnswer()
        {
            if (!Failed || OfficeSceneIntegration.GameplayBlocked) return false;
            if (wrongTurn.HasCheckpoint) consequence = StartCoroutine(RewindWrongTurn());
            else SetActionPhase(ActionPhase.Choose);
            return true;
        }
        IEnumerator ShowWrongTurn()
        {
            Phase = ActionPhase.Consequence;
            promptCard.HidePrompt();
            yield return wrongTurn.Play();
            consequence = null;
            SetActionPhase(ActionPhase.Retry);
        }
        IEnumerator RewindWrongTurn()
        {
            Phase = ActionPhase.Consequence;
            promptCard.HidePrompt();
            yield return wrongTurn.Rewind(() => SetActionPhase(ActionPhase.Choose));
            consequence = null;
            OfficeSceneIntegration.Current?.RequireFreshClick();
        }
        void CancelConsequence()
        {
            if (consequence != null) StopCoroutine(consequence);
            consequence = null;
            if (wrongTurn) wrongTurn.Cancel();
        }
        void SetActionPhase(ActionPhase phase)
        {
            Phase = phase;
            promptCard?.ShowPrompt(Failed ? "다시하기" : ActionLabel);
            OfficeSceneIntegration.Current?.RequireFreshClick();
        }
        Coroutine teacherAnnouncementRoutine;
        Coroutine teacherArrivalWatchRoutine;

        void Awake()
        {
            Flow = new OfficeBlackoutFlow();
            if (exitDoor) closedDoorRotation = exitDoor.localRotation;
            Flow.StepChanged += HandleStepChanged;
            if (completionNotebook) completionNotebook.SetActive(false);
        }

        IEnumerator Start() { while (OfficeStoryTime.Frozen) yield return null; BeginErrand(); }

        public bool BeginErrand() => Transition(Flow.BeginErrand, onErrandStarted);
        public bool PlaceDocuments() => Transition(Flow.PlaceDocuments, onBlackoutStarted);
        public bool Inspecting(OfficeBlackoutFlow.Step step) =>
            ActionReady && Phase == ActionPhase.Inspect && Flow.CurrentStep == step && !OfficeSceneIntegration.GameplayBlocked;
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
                SetActionPhase(Flow.CurrentStep == OfficeBlackoutFlow.Step.KeepFriendInPlace ? ActionPhase.Choose : ActionPhase.Inspect);
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

            teacherArrivalWatchRoutine = StartCoroutine(EnterFromDoor());
        }

        IEnumerator EnterFromDoor()
        {
            if (exitDoor)
            {
                var open = closedDoorRotation * Quaternion.Euler(0, -88, 0);
                for (float t = 0; t < 1;)
                {
                    t = Mathf.Min(1, t + OfficeStoryTime.Delta / .65f);
                    exitDoor.localRotation = Quaternion.Slerp(closedDoorRotation, open, Mathf.SmoothStep(0, 1, t));
                    yield return null;
                }
            }
            Transform teacherTransform = teacherWaypointMover.transform;
            teacherWaypointMover.StopMovement();
            teacherTransform.SetPositionAndRotation(teacherEntry.position, teacherEntry.rotation);
            teacherWaypointMover.gameObject.SetActive(true);
            teacherWaypointMover.StartRoute();
            Debug.Log($"[OfficeBlackout] Teacher activated at {teacherEntry.name}; waypoint route started.", this);
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
            CancelConsequence();
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
            if (exitDoor) exitDoor.localRotation = closedDoorRotation;
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
            CancelConsequence();
            if (teacherAnnouncementRoutine != null)
                StopCoroutine(teacherAnnouncementRoutine);
            if (teacherArrivalWatchRoutine != null)
                StopCoroutine(teacherArrivalWatchRoutine);
            if (Flow != null) Flow.StepChanged -= HandleStepChanged;
        }
    }
}
