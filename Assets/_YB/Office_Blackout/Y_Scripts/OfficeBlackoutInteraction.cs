using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// Put this component on DropoffZone. The player reaches TeacherDeskDropoff
    /// as a movement waypoint, then clicks/presses interact inside DropoffZone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeBlackoutInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] OfficeBlackoutDirector director;
        [Tooltip("Optional player root. If empty, Player tag or CharacterController is used.")]
        [SerializeField] Transform playerRoot;

        [Header("Input")]
        [Tooltip("Optional action override. Default: left mouse click and XR controller primary button.")]
        [SerializeField] InputActionReference interactAction;
        [SerializeField] Key keyboardKey = Key.E;

        [Header("Prompt")]
        [SerializeField] UnityEvent<string> onPromptChanged = new UnityEvent<string>();
        [SerializeField] string availablePrompt = "클릭하여 선생님 책상에 서류 놓기";

        readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
        InputAction fallbackAction;
        bool playerInZone => playerColliders.Count > 0;

        void Reset()
        {
            var trigger = GetComponent<Collider>();
            if (trigger) trigger.isTrigger = true;
        }

        void OnEnable()
        {
            var trigger = GetComponent<Collider>();
            if (trigger && !trigger.isTrigger)
                Debug.LogWarning($"{nameof(OfficeBlackoutInteraction)} expects an Is Trigger collider on {name}.", this);

            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed += OnInteract;
                interactAction.action.Enable();
            }
            else
            {
                fallbackAction = new InputAction("OfficeBlackoutInteract", InputActionType.Button);
                fallbackAction.AddBinding("<Mouse>/leftButton");
                fallbackAction.AddBinding("<XRController>/primaryButton");
                // E remains available as a keyboard fallback for editor checks.
                fallbackAction.AddBinding("<Keyboard>/" + keyboardKey.ToString().ToLowerInvariant());
                fallbackAction.performed += OnInteract;
                fallbackAction.Enable();
            }
        }

        void OnDisable()
        {
            if (interactAction != null && interactAction.action != null)
                interactAction.action.performed -= OnInteract;
            if (fallbackAction != null)
            {
                fallbackAction.performed -= OnInteract;
                fallbackAction.Dispose();
                fallbackAction = null;
            }
            playerColliders.Clear();
            SetPrompt(string.Empty);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            playerColliders.Add(other);
            RefreshPrompt();
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            playerColliders.Remove(other);
            RefreshPrompt();
        }

        void OnInteract(InputAction.CallbackContext _)
        {
            if (!playerInZone || !director || director.Flow == null) return;
            if (director.Flow.CurrentStep != OfficeBlackoutFlow.Step.Errand) return;

            if (director.PlaceDocuments())
                SetPrompt(string.Empty);
        }

        bool IsPlayer(Collider other)
        {
            if (playerRoot)
                return other.transform == playerRoot || other.transform.IsChildOf(playerRoot);

            Transform candidate = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            if (candidate == transform || candidate.IsChildOf(transform)) return false;
            return candidate.CompareTag("Player") || candidate.GetComponentInParent<CharacterController>() != null;
        }

        void RefreshPrompt()
        {
            bool available = playerInZone && director && director.Flow != null &&
                             director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand;
            SetPrompt(available ? availablePrompt : string.Empty);
        }

        void SetPrompt(string value) => onPromptChanged?.Invoke(value);
    }
}
