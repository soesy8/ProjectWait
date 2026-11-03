using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectWait.M.Elevator
{
    [DisallowMultipleComponent]
    public sealed class ElevatorSmoothButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Button button;
        [SerializeField] RectTransform artwork,label;
        Vector2 labelRest;
        bool hovered,selected,pressed;
        float offset,scale=1;
        public void Configure(Button target,RectTransform surface,RectTransform text)
        {button=target;artwork=surface;label=text;hovered=selected=pressed=false;offset=0;scale=1;CaptureRest();}
        public void CaptureRest(){if(label)labelRest=label.anchoredPosition;Apply();}
        void OnEnable(){if(label)CaptureRest();}
        void Apply(){if(artwork){artwork.anchoredPosition=Vector2.up*offset;artwork.localScale=Vector3.one*scale;}if(label)label.anchoredPosition=labelRest+Vector2.up*offset;}
        public void OnPointerEnter(PointerEventData e){hovered=true;}
        public void OnPointerExit(PointerEventData e){hovered=pressed=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=pressed=false;}
        void LateUpdate()
        {
            bool active=button&&button.IsInteractable();if(!active)hovered=selected=pressed=false;
            float amount=1-Mathf.Exp(-Time.unscaledDeltaTime*32);
            offset=Mathf.Lerp(offset,active?pressed?-3:hovered||selected?1.5f:0:0,amount);
            scale=Mathf.Lerp(scale,active&&pressed?.985f:1,amount);Apply();
        }
        void OnDisable(){hovered=selected=pressed=false;offset=0;scale=1;Apply();}
    }
}