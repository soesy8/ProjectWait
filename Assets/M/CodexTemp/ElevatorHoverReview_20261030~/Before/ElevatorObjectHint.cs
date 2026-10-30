using TMPro;using UnityEngine;
namespace ProjectWait.M.Elevator
{
 public sealed class ElevatorObjectHint:MonoBehaviour
 {
  public ElevatorObservation observation;public TMP_Text label;public CanvasGroup group;public Vector3 worldOffset;public Vector3 objectPointOffset;
  bool wasVisible;Vector3 anchor;
  public void Present(bool visible,string text,Camera camera,bool reposition)
  {
   if(visible&&(!wasVisible||reposition)){anchor=observation.transform.position+objectPointOffset+worldOffset;transform.position=anchor;}
   wasVisible=visible;group.alpha=visible?1:0;group.blocksRaycasts=false;group.interactable=false;
   if(!visible)return;label.text=text;transform.position=anchor;transform.localScale=Vector3.one*(Mathf.Clamp(Vector3.Distance(anchor,camera.transform.position)*.34f,.38f,1.75f)/340f);var dir=anchor-camera.transform.position;if(dir.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(dir,Vector3.up);
  }
 }
}
