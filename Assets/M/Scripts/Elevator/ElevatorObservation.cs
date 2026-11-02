using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorClue { Friend, Door, Display, Intercom, Notice, Identity, Ball, FloorButtons, Advertisement, LobbyCall, FrontIntercom, FacilityGuide, GroundExit, FrontFloorButtons, LobbyCallUp, Phone, Rescuer }

    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorObservation : MonoBehaviour, IXRSelectFilter
    {
        public const float InteractionRange = 2f;
        public ElevatorLesson lesson;
        public ElevatorClue clue;
        public ElevatorOutlineTarget outlineTarget;
        public ElevatorButtonFeedback feedback;
        public bool hasDescription;
        public bool IsHovered => hovered;
        public bool IsInInteractionRange => DistanceToPlayer <= InteractionRange;
        public bool CanActivate => isActiveAndEnabled && lesson && lesson.CanInteract(clue) && IsInInteractionRange;
        public bool canProcess => isActiveAndEnabled;
        // Measure from the player's eyes to the closest interaction surface, in world metres.
        public float DistanceToPlayer
        {
            get
            {
                if (!lesson || !lesson.playerCamera) return float.PositiveInfinity;
                var position = lesson.playerCamera.transform.position;
                float squared = float.PositiveInfinity;
                if (interaction)
                    foreach (var collider in interaction.colliders)
                        if (collider && collider.enabled && collider.gameObject.activeInHierarchy)
                            squared = Mathf.Min(squared, (collider.ClosestPoint(position) - position).sqrMagnitude);
                return float.IsPositiveInfinity(squared) ? Vector3.Distance(position, transform.position) : Mathf.Sqrt(squared);
            }
        }
        bool hovered;
        int runVersion;
        XRSimpleInteractable interaction;
        void Awake()
        {
            interaction = GetComponent<XRSimpleInteractable>();
            interaction.selectFilters.Add(this);
            interaction.hoverEntered.AddListener(_ => Hover(true));
            interaction.hoverExited.AddListener(_ => Hover(false));
            interaction.selectEntered.AddListener(_ => Activate());
        }
        public void Hover(bool value)
        {
            if (value && (!hovered || runVersion != lesson.RunVersion)) { lesson.Inspect(clue); runVersion = lesson.RunVersion; }
            hovered = value;
            if(!value)lesson.ClearThought(clue);
        }
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => CanActivate;
        public void Activate() { if(!CanActivate)return; if(feedback&&(clue!=ElevatorClue.Intercom||lesson.Flow.Step==ElevatorStep.Stopped))feedback.Press(); lesson.Activate(clue); }
        public void ResetHover() { hovered=false;if(outlineTarget)outlineTarget.SetVisible(false); }
        void LateUpdate()
        {
            if(outlineTarget)outlineTarget.SetPresentation(hasDescription&&lesson&&lesson.Flow!=null&&lesson.CanInteract(clue),hovered,IsInInteractionRange);
        }
        void OnDisable() { ResetHover(); }
        void OnDestroy() { if(interaction)interaction.selectFilters.Remove(this); }
    }
}
