using UnityEngine;
using UnityEngine.InputSystem;
using ProjectWait.M.Elevator;

namespace ProjectWait.YB.OfficeBlackout
{
    // World targets and action cards use exactly the same state-checked command.
    public sealed class OfficeBlackoutFriendInteraction : MonoBehaviour
    {
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] ElevatorOutlineTarget friendOutline, teacherOutline;
        [SerializeField, Min(.1f)] float interactionRayDistance = 5f;
        InputAction select;
        readonly RaycastHit[] hits = new RaycastHit[32];

        void OnEnable()
        {
            select = new InputAction("OfficeCharacterSelect", InputActionType.Button);
            select.AddBinding("<Mouse>/leftButton");
            select.AddBinding("<Keyboard>/e");
            select.AddBinding("<XRController>/{TriggerButton}");
            select.performed += OnSelect;
            select.Enable();
        }
        ElevatorOutlineTarget CurrentTarget => director.Inspecting(OfficeBlackoutFlow.Step.TeacherArrives) ? teacherOutline :
            director.Inspecting(OfficeBlackoutFlow.Step.CalmFriend) ? friendOutline : null;

        void LateUpdate()
        {
            var scene = OfficeSceneIntegration.Current;
            var target = CurrentTarget;
            bool aimed = target && scene && (scene.TryAimRay(null, out var ray) && Hits(ray, target.transform) ||
                UnityEngine.XR.XRSettings.isDeviceActive && scene.TryHandRay(false, out ray) && Hits(ray, target.transform));
            friendOutline.SetPresentation(target == friendOutline, aimed && target == friendOutline, aimed && target == friendOutline);
            teacherOutline.SetPresentation(target == teacherOutline, aimed && target == teacherOutline, aimed && target == teacherOutline);
        }

        void OnSelect(InputAction.CallbackContext context)
        {
            var integration = OfficeSceneIntegration.Current;
            if (!integration || !integration.ClickReady || OfficeSceneIntegration.GameplayBlocked ||
                !director || !director.ActionReady || director.Phase != OfficeBlackoutDirector.ActionPhase.Inspect) return;
            var target = CurrentTarget;
            if (!target) return;
            if (!integration.TryAimRay(context.control, out var ray) || integration.HitUI(ray)) return;
            if (Hits(ray, target.transform) && director.SelectWorldTarget()) integration.RequireFreshClick();
        }

        bool Hits(Ray ray, Transform target)
        {
            int count = Physics.RaycastNonAlloc(ray, hits, interactionRayDistance, ~0, QueryTriggerInteraction.Collide);
            if (count == hits.Length) return false;
            float closestTarget = float.PositiveInfinity, closestObstacle = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.GetComponentInParent<CharacterController>()) continue;
                if (hit.transform == target || hit.transform.IsChildOf(target))
                    closestTarget = Mathf.Min(closestTarget, hit.distance);
                else if (!hit.collider.isTrigger) closestObstacle = Mathf.Min(closestObstacle, hit.distance);
            }
            return closestTarget < closestObstacle;
        }
        void OnDisable()
        {
            select?.Dispose(); select = null;
            if (friendOutline) friendOutline.SetPresentation(false, false);
            if (teacherOutline) teacherOutline.SetPresentation(false, false);
        }
    }
}
