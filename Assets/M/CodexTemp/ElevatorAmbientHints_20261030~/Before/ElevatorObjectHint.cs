using TMPro;
using UnityEngine;
namespace ProjectWait.M.Elevator
{
 public sealed class ElevatorObjectHint:MonoBehaviour
 {
  public ElevatorObservation observation;
  public TMP_Text label;
  public CanvasGroup group;
  public Vector3 worldOffset,objectPointOffset;
  [Min(.0001f)] public float worldPixelSize=.002f;
  [Min(.01f)] public float appearSeconds=.09f,disappearSeconds=.07f;
  bool wasVisible;
  Vector3 anchor;
  public void Present(bool visible,string text,Camera camera,bool reposition)
  {
   if(!observation||!group||!camera)return;
   if(visible&&(!wasVisible||reposition))anchor=observation.transform.position+objectPointOffset+worldOffset;
   wasVisible=visible;
   // Physical size and a fixed anchor are independent of the viewer's distance.
   transform.localScale=Vector3.one*worldPixelSize;
   group.alpha=Mathf.MoveTowards(group.alpha,visible?1:0,Time.unscaledDeltaTime/(visible?appearSeconds:disappearSeconds));
   group.blocksRaycasts=false;group.interactable=false;
   if(group.alpha<=0)return;
   if(visible)label.text=text;
   transform.position=anchor;
   var dir=anchor-camera.transform.position;
   if(dir.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(dir,Vector3.up);
  }
  void OnDisable(){wasVisible=false;if(group)group.alpha=0;}
 }
}
