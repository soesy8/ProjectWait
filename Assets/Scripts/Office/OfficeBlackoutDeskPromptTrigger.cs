using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Shows the desk prompt while the player is in TeacherDeskDropoff.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeBlackoutDeskPromptTrigger : MonoBehaviour
    {
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] Transform playerRoot;
        [SerializeField] UnityEvent<string> onPromptChanged = new UnityEvent<string>();
        [SerializeField] string availablePrompt = "책을 놓을 자리가 있네.";

        readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
        bool promptVisible;
        public bool PlayerInDeskArea => playerColliders.Count > 0;

        void OnEnable()
        {
            if (director) director.StepChanged += OnStepChanged;
            RefreshPrompt();
        }

        void OnDisable()
        {
            if (director) director.StepChanged -= OnStepChanged;
            playerColliders.Clear();
            SetPrompt(string.Empty);
        }

        void OnStepChanged(OfficeBlackoutFlow.Step _) => RefreshPrompt();

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

        public void HidePrompt()
        {
            playerColliders.Clear();
            SetPrompt(string.Empty);
        }

        void RefreshPrompt()
        {
            bool shouldShow = playerColliders.Count > 0 && director && director.Flow != null &&
                              director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand;
            SetPrompt(shouldShow ? availablePrompt : string.Empty);
        }

        void SetPrompt(string value)
        {
            bool visible = !string.IsNullOrWhiteSpace(value);
            if (promptVisible == visible && !visible) return;
            promptVisible = visible;
            onPromptChanged?.Invoke(visible ? value : string.Empty);
        }

        bool IsPlayer(Collider other)
        {
            if (playerRoot)
                return other.transform == playerRoot || other.transform.IsChildOf(playerRoot);

            Transform candidate = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            return candidate.CompareTag("Player") || candidate.GetComponentInParent<CharacterController>() != null;
        }

    }
}
