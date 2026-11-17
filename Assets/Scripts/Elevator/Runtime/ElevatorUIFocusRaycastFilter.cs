using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Only the optional full-card reader carries this filter; choice buttons never do.
    public sealed class ElevatorUIFocusRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        public ElevatorUIFocusWindow window;
        public bool Allows(Vector3 worldPoint)=>!window||window.OpacityAt(worldPoint)>.28f;
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
        {
            return !RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform,screenPoint,eventCamera,out var point)||Allows(point);
        }
    }
}
