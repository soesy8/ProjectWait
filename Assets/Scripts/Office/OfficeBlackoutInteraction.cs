using ProjectWait.M.Elevator;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Linq;

namespace ProjectWait.YB.OfficeBlackout
{
    [RequireComponent(typeof(Collider), typeof(ElevatorOutlineTarget))]
    public sealed class OfficeBlackoutInteraction : MonoBehaviour
    {
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] Transform book, deskSocket;
        [SerializeField, Min(.1f)] float interactionRayDistance = 8f;
        [SerializeField] InputActionReference interactAction;
        [SerializeField] Key keyboardKey = Key.E;

        InputAction fallbackAction;
        ElevatorOutlineTarget outline;
        Collider zone;
        readonly RaycastHit[] blockers = new RaycastHit[16];

        void Awake()
        {
            zone = GetComponent<Collider>();
            outline = GetComponent<ElevatorOutlineTarget>();
        }

        void OnEnable()
        {
            director.StepChanged += OnStep;
            var action = interactAction ? interactAction.action : null;
            if (action == null)
            {
                action = fallbackAction = new InputAction("OfficePlaceBook", InputActionType.Button);
                action.AddBinding("<XRController>{RightHand}/{TriggerButton}");
                action.AddBinding("<Mouse>/leftButton");
                action.AddBinding("<Keyboard>/" + keyboardKey.ToString().ToLowerInvariant());
            }
            action.performed += OnInteract;
            action.Enable();
        }

        void OnStep(OfficeBlackoutFlow.Step step)
        {
            if (step != OfficeBlackoutFlow.Step.BlackoutReaction) return;
            book.SetParent(deskSocket, false);
            book.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            outline.SetPresentation(false, false);
        }

        void LateUpdate()
        {
            var scene = OfficeSceneIntegration.Current;
            if (!scene || director.Flow == null) return;
            if (!director.Flow.DocumentsPlaced)
            {
                bool xr = XRSettings.isDeviceActive;
                Transform grip = xr ? scene.rightRay : scene.viewer.transform;
                if (book.parent != grip)
                {
                    book.SetParent(grip, false);
                    book.SetLocalPositionAndRotation(xr ? new Vector3(.035f, -.025f, .055f) : new Vector3(.32f, -.27f, .73f),
                        Quaternion.Euler(xr ? -12 : -22, -14, 12));
                }
            }
            bool visible = director.Inspecting(OfficeBlackoutFlow.Step.Errand);
            bool aimed = visible && scene.TryAimRay(null, out var ray) && HitDropoffZone(ray);
            outline.SetPresentation(visible, aimed, aimed);
        }

        void OnInteract(InputAction.CallbackContext context)
        {
            var scene = OfficeSceneIntegration.Current;
            if (!scene || !scene.ClickReady || !director.Inspecting(OfficeBlackoutFlow.Step.Errand)) return;
            if (XRSettings.isDeviceActive && !context.control.device.usages.Contains(UnityEngine.InputSystem.CommonUsages.RightHand)) return;
            if (scene.TryAimRay(context.control, out var ray) && !scene.HitUI(ray) && HitDropoffZone(ray) && director.SelectWorldTarget())
                scene.RequireFreshClick();
        }

        bool HitDropoffZone(Ray ray)
        {
            if (!zone.Raycast(ray, out var target, interactionRayDistance)) return false;
            int count = Physics.RaycastNonAlloc(ray, blockers, target.distance, ~0, QueryTriggerInteraction.Ignore);
            if (count == blockers.Length) return false;
            for (int i = 0; i < count; i++)
                if (!blockers[i].transform.IsChildOf(transform) && !blockers[i].transform.GetComponentInParent<CharacterController>())
                    return false;
            return true;
        }

        void OnDisable()
        {
            director.StepChanged -= OnStep;
            outline.SetPresentation(false, false);
            if (interactAction && interactAction.action != null) interactAction.action.performed -= OnInteract;
            fallbackAction?.Dispose(); fallbackAction = null;
        }
    }
}
