using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorClue { Friend, Door, Display, Intercom, Notice, Identity, Ball, FloorButtons, Advertisement, LobbyCall, FrontIntercom, FacilityGuide, GroundExit, FrontFloorButtons, LobbyCallUp, Phone, Rescuer }

    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorObservation : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        public ElevatorLesson lesson;
        public ElevatorClue clue;
        public ElevatorOutlineTarget outlineTarget;
        public ElevatorButtonFeedback feedback;
        public bool hasDescription;
        public bool IsHovered => hovered;
        public bool CanActivate => isActiveAndEnabled && lesson && lesson.CanInteract(clue);
        public float HoverBlend => hoverBlend;
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
        float hoverBlend;
        ElevatorOutlineTarget cachedOutline;
        Color idleColor, hoverColor;
        float idleWidth, hoverWidth, idleHalo, hoverHalo;
        int runVersion;
        XRSimpleInteractable interaction;
        void Awake()
        {
            interaction = GetComponent<XRSimpleInteractable>();
            interaction.selectFilters.Add(this);
            interaction.hoverFilters.Add(this);
            interaction.hoverEntered.AddListener(_ => Hover(true));
            interaction.hoverExited.AddListener(_ => Hover(false));
            interaction.selectEntered.AddListener(_ => Activate());
            ExcludeNestedOutlineGeometry();
        }
        // A held prop has its own observation, even while parented to the friend.
        public void ExcludeNestedOutlineGeometry()
        {
            if (!outlineTarget) return;
            outlineTarget.geometry = System.Array.FindAll(outlineTarget.geometry, geometry =>
            {
                if (geometry == null) return false;
                var owner = geometry.sourceRenderer ? geometry.sourceRenderer.GetComponentInParent<ElevatorObservation>() : null;
                return !owner || owner == this || !owner.transform.IsChildOf(transform);
            });
        }
        public void Hover(bool value)
        {
            if(value&&!CanActivate)return;
            if (value && (!hovered || runVersion != lesson.RunVersion)) { lesson.Inspect(clue); runVersion = lesson.RunVersion; }
            hovered = value;
            if(!value)lesson.ClearThought(clue);
        }
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => CanActivate;
        bool IXRHoverFilter.Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => CanActivate;
        public void Activate() { if(!CanActivate)return; if(feedback&&(clue!=ElevatorClue.Intercom||lesson.Flow.Step==ElevatorStep.Stopped))feedback.Press(); lesson.Activate(clue); }
        public void ResetHover()
        {
            hovered=false;hoverBlend=0;
            if(cachedOutline)
            {
                cachedOutline.idleColor=idleColor;cachedOutline.hoverColor=hoverColor;
                cachedOutline.idleWidth=idleWidth;cachedOutline.hoverWidth=hoverWidth;
                cachedOutline.idleHalo=idleHalo;cachedOutline.hoverHalo=hoverHalo;
                cachedOutline.SetVisible(false);cachedOutline=null;
            }
            if(outlineTarget)outlineTarget.SetVisible(false);
        }
        void LateUpdate()
        {
            if(!outlineTarget)return;
            if(cachedOutline!=outlineTarget)
            {
                cachedOutline=outlineTarget;
                idleColor=outlineTarget.idleColor;hoverColor=outlineTarget.hoverColor;
                idleWidth=outlineTarget.idleWidth;hoverWidth=outlineTarget.hoverWidth;
                idleHalo=outlineTarget.idleHalo;hoverHalo=outlineTarget.hoverHalo;
            }
            bool visible=hasDescription&&lesson&&lesson.Flow!=null&&lesson.CanInteract(clue);
            float duration=outlineTarget.colorTransitionDuration;
            float amount=duration>0?1-Mathf.Exp(-5*Time.unscaledDeltaTime/duration):1;
            hoverBlend=visible?Mathf.Lerp(hoverBlend,hovered?1:0,amount):0;
            if(Mathf.Abs(hoverBlend-(hovered?1:0))<.001f)hoverBlend=hovered&&visible?1:0;
            // Keep the shared renderer unchanged: M owns hover interpolation, not proximity.
            float colourBlend=hoverBlend;
            if(visible&&lesson.scenario&&lesson.scenario.IsGuidanceTarget(clue))
                colourBlend=Mathf.Lerp(lesson.scenario.GuidanceBlend,1,hoverBlend);
            outlineTarget.idleColor=outlineTarget.hoverColor=Color.Lerp(idleColor,hoverColor,colourBlend);
            outlineTarget.idleWidth=outlineTarget.hoverWidth=Mathf.Lerp(idleWidth,hoverWidth,hoverBlend);
            outlineTarget.idleHalo=outlineTarget.hoverHalo=Mathf.Lerp(idleHalo,hoverHalo,hoverBlend);
            outlineTarget.SetPresentation(visible,hovered,false);
        }
        void OnDisable() { ResetHover(); }
        void OnDestroy() { if(interaction){interaction.selectFilters.Remove(this);interaction.hoverFilters.Remove(this);} }
    }
}
