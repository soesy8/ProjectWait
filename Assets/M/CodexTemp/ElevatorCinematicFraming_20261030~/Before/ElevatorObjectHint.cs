using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace ProjectWait.M.Elevator
{
 public sealed class ElevatorObjectHint:MonoBehaviour
 {
  public ElevatorObservation observation;public TMP_Text label;public CanvasGroup group;
  public Vector3 worldOffset,objectPointOffset;
  [Min(.0001f)]public float worldPixelSize=.002f;
  [Min(.01f)]public float appearSeconds=.09f,disappearSeconds=.07f;
  bool wasVisible;Vector3 anchor;
  readonly Vector3[] corners=new Vector3[4];
  readonly List<Rect> reserved=new List<Rect>();
  static float Overlap(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
  bool Project(RectTransform rect,Camera camera,out Rect result)
  {
   rect.GetWorldCorners(corners);var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;
   foreach(var corner in corners){var p=camera.WorldToViewportPoint(corner);if(p.z<=camera.nearClipPlane){result=default;return false;}min=Vector2.Min(min,p);max=Vector2.Max(max,p);}result=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return true;
  }
  void Reserve(RectTransform rect,Camera camera,float padding=.025f)
  {if(!rect||!rect.gameObject.activeInHierarchy||!Project(rect,camera,out var box))return;reserved.Add(Rect.MinMaxRect(box.xMin-padding,box.yMin-padding,box.xMax+padding,box.yMax+padding));}
  void ReserveFace(Transform actor,Camera camera)
  {
   if(!actor||!actor.gameObject.activeInHierarchy)return;var collider=actor.GetComponent<Collider>();if(!collider)return;
   var b=collider.bounds;var face=new Bounds(b.center+Vector3.up*b.extents.y*.65f,new Vector3(b.size.x,b.size.y*.40f,b.size.z));
   var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;
   for(int i=0;i<8;i++){var p=camera.WorldToViewportPoint(face.center+Vector3.Scale(face.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(p.z<=camera.nearClipPlane)return;min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
   reserved.Add(Rect.MinMaxRect(min.x-.02f,min.y-.02f,max.x+.02f,max.y+.02f));
  }
  Vector3 ChooseAnchor(Camera camera,ElevatorView view)
  {
   var seed=observation.transform.position+objectPointOffset+worldOffset;reserved.Clear();
   if(view)
   {
    Reserve(view.dialoguePanel,camera,.045f);foreach(var b in view.ChoiceButtons)if(b&&b.IsActive())Reserve((RectTransform)b.transform,camera);
    ReserveFace(view.lesson.friend,camera);if(view.lesson.presentation&&view.lesson.presentation.rescuer)ReserveFace(view.lesson.presentation.rescuer.transform,camera);
   }
   var right=camera.transform.right;var up=Vector3.up;var forward=camera.transform.forward;
   var offsets=new[]{Vector3.zero,right*.8f,-right*.8f,up*.55f,-up*.55f,right*.8f+up*.55f,-right*.8f+up*.55f,right*.8f-up*.55f,-right*.8f-up*.55f,forward*.7f,-forward*.5f,right*1.35f,-right*1.35f,up,-up,forward*1.2f};
   var rect=(RectTransform)transform;var best=seed;float bestScore=float.MaxValue;
   foreach(var offset in offsets)
   {
    var p=seed+offset;var direction=p-camera.transform.position;if(direction.sqrMagnitude<.01f)continue;
    transform.SetPositionAndRotation(p,Quaternion.LookRotation(direction,Vector3.up));if(!Project(rect,camera,out var box))continue;
    float score=offset.sqrMagnitude*.002f;
    float area=box.width*box.height;score+=(area-Overlap(box,new Rect(.025f,.04f,.95f,.92f)))*20000;
    foreach(var avoid in reserved)score+=Overlap(box,avoid)*10000;
    if(score<bestScore){bestScore=score;best=p;}
   }
   return best;
  }
  public void Present(bool visible,string text,Camera camera,bool reposition,ElevatorView view=null)
  {
   if(!observation||!group||!camera)return;transform.localScale=Vector3.one*worldPixelSize;
   // Choose a separate position on appearance, then keep its physical size and
   // world anchor fixed as the player moves. Dialogue and its buttons stay visible.
   if(visible&&(!wasVisible||reposition))anchor=ChooseAnchor(camera,view);
   wasVisible=visible;group.alpha=Mathf.MoveTowards(group.alpha,visible?1:0,Time.unscaledDeltaTime/(visible?appearSeconds:disappearSeconds));group.blocksRaycasts=false;group.interactable=false;
   if(group.alpha<=0)return;if(visible)label.text=text;transform.position=anchor;
   var dir=anchor-camera.transform.position;if(dir.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(dir,Vector3.up);
  }
  void OnDisable(){wasVisible=false;if(group)group.alpha=0;}
 }
}
