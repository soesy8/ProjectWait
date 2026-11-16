using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Accepts a click or XR trigger on the friend when its prompt is active.</summary>
    public sealed class OfficeBlackoutFriendInteraction : MonoBehaviour
    {
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] OfficeBlackoutPromptCard promptCard;
        [SerializeField] Transform friendRoot;
        [SerializeField] Camera playerCamera;
        [SerializeField, Min(1)] int requiredPromptIndex = 2;
        [SerializeField, Min(1)] int requiredTeacherPromptIndex = 3;
        [SerializeField, Min(1)] int requiredEscortCompletePromptIndex = 4;
        [SerializeField, Min(0.1f)] float interactionRayDistance = 8f;
        [SerializeField] LayerMask interactionLayers = ~0;
        [SerializeField] InputActionReference interactAction;
        [SerializeField] bool logDebug = true;

        InputAction fallbackAction;

        void OnEnable()
        {
            if (!friendRoot) friendRoot = transform;
            if (!playerCamera) playerCamera = Camera.main;

            Collider[] colliders = friendRoot ? friendRoot.GetComponentsInChildren<Collider>(true) : System.Array.Empty<Collider>();
            if (logDebug)
                Debug.Log($"[OfficeBlackoutFriendInteraction] Enabled on {name}. Friend root: {(friendRoot ? friendRoot.name : "missing")}; " +
                          $"colliders: {colliders.Length}; camera: {(playerCamera ? playerCamera.name : "missing")}; " +
                          $"director: {(director ? director.name : "missing")}; prompt card: {(promptCard ? promptCard.name : "missing")}; " +
                          $"required prompt: {requiredPromptIndex}.", this);

            if (colliders.Length == 0)
                Debug.LogWarning("[OfficeBlackoutFriendInteraction] Friend root has no collider. Aim interaction cannot hit the friend.", this);
            if (!director || !promptCard || !playerCamera || !friendRoot)
                Debug.LogWarning("[OfficeBlackoutFriendInteraction] One or more references are missing; friend interaction cannot work.", this);

            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed += OnInteract;
                interactAction.action.Enable();
                if (logDebug) Debug.Log($"[OfficeBlackoutFriendInteraction] Listening to input action '{interactAction.action.name}'.", this);
            }
            else
            {
                fallbackAction = new InputAction("OfficeBlackoutFriendInteract", InputActionType.Button);
                fallbackAction.AddBinding("<Mouse>/leftButton");
                fallbackAction.AddBinding("<XRController>/{TriggerButton}");
                fallbackAction.performed += OnInteract;
                fallbackAction.Enable();
                if (logDebug) Debug.Log("[OfficeBlackoutFriendInteraction] Listening to mouse left click and XR controller trigger.", this);
            }
        }

        void OnDisable()
        {
            if (interactAction != null && interactAction.action != null)
                interactAction.action.performed -= OnInteract;
            if (fallbackAction == null) return;
            fallbackAction.performed -= OnInteract;
            fallbackAction.Dispose();
            fallbackAction = null;
        }

        void OnInteract(InputAction.CallbackContext _)
        {
            if (logDebug) Debug.Log("[OfficeBlackoutFriendInteraction] Click/trigger input received.", this);

            if (!director || director.Flow == null || !promptCard || !playerCamera)
            {
                Debug.LogWarning("[OfficeBlackoutFriendInteraction] Input ignored: Director, Flow, PromptCard, or player camera is missing.", this);
                return;
            }

            if (director.Flow.CurrentStep == OfficeBlackoutFlow.Step.EscortToExit)
            {
                if (!director.CanFinishEscort)
                {
                    if (logDebug) Debug.Log("[OfficeBlackoutFriendInteraction] End-sequence click ignored: one or both characters are still moving.", this);
                    return;
                }

                if (!promptCard.IsShowingPresetPrompt(requiredEscortCompletePromptIndex))
                {
                    if (logDebug) Debug.Log($"[OfficeBlackoutFriendInteraction] End-sequence click ignored: preset prompt {requiredEscortCompletePromptIndex} is not visible.", this);
                    return;
                }

                Transform teacherRoot = director.TeacherTransform;
                bool hitFinishedCharacter =
                    (teacherRoot && HitTarget(teacherRoot, "teacher")) ||
                    (friendRoot && HitTarget(friendRoot, "friend"));
                if (hitFinishedCharacter)
                {
                    director.FadeToBlack();
                    Debug.Log("[OfficeBlackoutFriendInteraction] A finished character was clicked; fade to black started.", this);
                }
                return;
            }

            if (director.Flow.CurrentStep == OfficeBlackoutFlow.Step.TeacherArrives)
            {
                if (!promptCard.IsShowingPresetPrompt(requiredTeacherPromptIndex))
                {
                    if (logDebug) Debug.Log($"[OfficeBlackoutFriendInteraction] Teacher click ignored: preset prompt {requiredTeacherPromptIndex} is not visible.", this);
                    return;
                }

                Transform teacherRoot = director.TeacherTransform;
                if (!teacherRoot || !HitTarget(teacherRoot, "teacher")) return;

                bool escortStarted = director.BeginEscort();
                if (escortStarted)
                    Debug.Log("[OfficeBlackoutFriendInteraction] Teacher was clicked; escort started.", this);
                else if (logDebug)
                    Debug.LogWarning($"[OfficeBlackoutFriendInteraction] Teacher was hit, but the flow rejected BeginEscort at step {director.Flow.CurrentStep}.", this);
                return;
            }

            if (director.Flow.CurrentStep != OfficeBlackoutFlow.Step.KeepFriendInPlace)
            {
                if (logDebug) Debug.Log($"[OfficeBlackoutFriendInteraction] Input ignored: current flow step is {director.Flow.CurrentStep}.", this);
                return;
            }

            if (!promptCard.IsShowingPresetPrompt(requiredPromptIndex))
            {
                if (logDebug) Debug.Log($"[OfficeBlackoutFriendInteraction] Input ignored: preset prompt {requiredPromptIndex} is not currently visible.", this);
                return;
            }

            if (!HitTarget(friendRoot, "friend")) return;

            bool started = director.AskFriendToStay();
            if (started)
                Debug.Log("[OfficeBlackoutFriendInteraction] Friend was hit; onFriendAskedToStay flow started.", this);
            else if (logDebug)
                Debug.LogWarning($"[OfficeBlackoutFriendInteraction] Friend was hit, but the flow rejected AskFriendToStay at step {director.Flow.CurrentStep}.", this);
        }

        bool HitTarget(Transform targetRoot, string targetLabel)
        {
            Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!XRSettings.isDeviceActive && Mouse.current != null && Cursor.lockState != CursorLockMode.Locked)
                screenPoint = Mouse.current.position.ReadValue();

            Ray ray = playerCamera.ScreenPointToRay(screenPoint);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactionRayDistance, interactionLayers,
                QueryTriggerInteraction.Collide);
            if (hits.Length == 0)
            {
                if (logDebug)
                    Debug.Log($"[OfficeBlackoutFriendInteraction] Aim ray missed all colliders (distance {interactionRayDistance:0.0}).", this);
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == targetRoot || hitTransform.IsChildOf(targetRoot))
                {
                    if (logDebug)
                        Debug.Log($"[OfficeBlackoutFriendInteraction] Aim ray hit {targetLabel} '{hitTransform.name}' at {hit.distance:0.00}m.", this);
                    return true;
                }

                // Trigger volumes such as ExitDesk detect player presence but should not block aiming at the friend.
                if (hit.collider.isTrigger)
                {
                    if (logDebug)
                        Debug.Log($"[OfficeBlackoutFriendInteraction] Aim ray passed through trigger '{hitTransform.name}' at {hit.distance:0.00}m.", this);
                    continue;
                }

                if (logDebug)
                    Debug.Log($"[OfficeBlackoutFriendInteraction] Aim ray blocked by solid collider '{hitTransform.name}' at {hit.distance:0.00}m before reaching {targetLabel} '{targetRoot.name}'.", this);
                return false;
            }

            if (logDebug)
                Debug.Log($"[OfficeBlackoutFriendInteraction] Aim ray found no {targetLabel} collider after skipping trigger volumes; expected '{targetRoot.name}'.", this);
            return false;
        }
    }
}
