using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectWait.M.Elevator
{
    // The same physical-looking response is used by mouse and tracked-device pointers.
    [DisallowMultipleComponent]
    public sealed class StorybookButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] RectTransform artwork, label;
        Vector2 artworkRest, labelRest;
        bool hovered, selected, pressed;
        public void Configure(UnityEngine.UI.Button target, RectTransform surface, RectTransform text)
        {
            ResetState(); button = target; artwork = surface; label = text; CaptureRest();
        }
        void OnEnable() { CaptureRest(); }
        public void CaptureRest()
        {
            if (artwork) { artworkRest = Vector2.zero; artwork.anchoredPosition = artworkRest; }
            if (label) labelRest = label.anchoredPosition;
            Refresh();
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = true; Refresh(); } }
        public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
        public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { selected = pressed = false; Refresh(); }
        void Refresh()
        {
            bool active = button && button.IsInteractable();
            float offset = active ? pressed ? -3 : hovered || selected ? 1.5f : 0 : 0;
            if (artwork) { artwork.anchoredPosition = artworkRest + Vector2.up * offset; artwork.localScale = Vector3.one * (active && pressed ? .985f : 1); }
            if (label) label.anchoredPosition = labelRest + Vector2.up * offset;
        }
        void ResetState() { hovered = selected = pressed = false; Refresh(); }
        void LateUpdate() { if (button && !button.IsInteractable() && (hovered || selected || pressed)) ResetState(); }
        void OnDisable() { ResetState(); }
    }
}
