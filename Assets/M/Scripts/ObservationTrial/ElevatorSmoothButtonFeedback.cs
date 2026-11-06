// Isolated test snapshot of Assets/M/Scripts/Elevator/ElevatorSmoothButtonFeedback.cs. Original is not modified.
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.Elevator.ObservationTrial
{
    [DisallowMultipleComponent]
    // Button ColorTint owns hover/press fading; the paper and text never shift or zoom.
    public sealed class ElevatorSmoothButtonFeedback : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] RectTransform artwork,label;
        public void Configure(Button target,RectTransform surface,RectTransform text)
        {button=target;artwork=surface;label=text;CaptureRest();}
        public void CaptureRest()
        {
            if(artwork){artwork.anchoredPosition=Vector2.zero;artwork.localScale=Vector3.one;}
        }
        void OnEnable(){CaptureRest();}
    }
}