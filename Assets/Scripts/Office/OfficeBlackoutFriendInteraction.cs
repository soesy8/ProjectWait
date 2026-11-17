using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectWait.YB.OfficeBlackout
{
    // World targets and action cards use exactly the same state-checked command.
    public sealed class OfficeBlackoutFriendInteraction : MonoBehaviour
    {
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] Transform friendRoot;
        [SerializeField] Camera playerCamera;
        [SerializeField, Min(.1f)] float interactionRayDistance = 5f;
        InputAction select;

        void OnEnable()
        {
            select = new InputAction("OfficeCharacterSelect", InputActionType.Button);
            select.AddBinding("<Mouse>/leftButton");
            select.AddBinding("<XRController>/{TriggerButton}");
            select.performed += OnSelect;
            select.Enable();
        }
        void OnSelect(InputAction.CallbackContext context)
        {
            var integration = OfficeSceneIntegration.Current;
            if (!integration || !integration.ClickReady || OfficeSceneIntegration.GameplayBlocked ||
                !director || !director.ActionReady) return;
            Transform target = director.Flow.CurrentStep == OfficeBlackoutFlow.Step.TeacherArrives ?
                director.TeacherTransform : director.Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend ||
                director.Flow.CurrentStep == OfficeBlackoutFlow.Step.KeepFriendInPlace ? friendRoot : null;
            if (!target) return;
            var hits = Physics.RaycastAll(integration.AimRay(context.control), interactionRayDistance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<CharacterController>()) continue;
                if (hit.transform == target || hit.transform.IsChildOf(target))
                {
                    if (director.PerformAction()) integration.RequireFreshClick();
                    return;
                }
                if (!hit.collider.isTrigger) return;
            }
        }
        void OnDisable() { select?.Dispose(); select = null; }
    }
}
