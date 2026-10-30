using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorClue { Friend, Door, Display, Intercom, Notice, Identity, Ball, FloorButtons, Advertisement, LobbyCall, FrontIntercom, FacilityGuide, GroundExit, FrontFloorButtons, LobbyCallUp, Phone, Rescuer }

    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorObservation : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public ElevatorClue clue;
        public LineRenderer outline;
        public ElevatorShapeHighlight shapeHighlight;
        public ElevatorButtonFeedback feedback;
        public bool IsHovered => hovered;
        bool hovered;
        int runVersion;
        XRSimpleInteractable interaction;
        void Awake()
        {
            interaction = GetComponent<XRSimpleInteractable>();
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
        public void Activate() { if(!lesson.CanInteract(clue))return; if(feedback)feedback.Press(); lesson.Activate(clue); }
        public void ResetHover() { hovered = false; if (outline) outline.enabled = false; if(shapeHighlight)shapeHighlight.SetVisible(false); }
        void LateUpdate()
        {
            if(outline)outline.enabled=false;
            if(shapeHighlight)shapeHighlight.SetVisible(lesson&&lesson.Flow!=null&&lesson.CanInteract(clue)&&(hovered||lesson.scenario&&lesson.scenario.objectGuidedScene&&lesson.scenario.IsGuidedTarget(this)));
        }
        void OnDisable() { ResetHover(); }
    }
}
