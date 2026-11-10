using System;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectWait.M.Elevator
{
 [Serializable] public sealed class ElevatorOutlineGeometry
 {
  public Renderer sourceRenderer;
  public Mesh mesh;
  public Transform anchor;
  public bool IsValid=>sourceRenderer?sourceRenderer.enabled&&sourceRenderer.gameObject.activeInHierarchy:mesh&&anchor&&anchor.gameObject.activeInHierarchy;
  public Bounds WorldBounds
  {
   get
   {
    if(sourceRenderer)return sourceRenderer.bounds;var local=mesh.bounds;var result=new Bounds(anchor.TransformPoint(local.center),Vector3.zero);
    for(int i=0;i<8;i++)result.Encapsulate(anchor.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));return result;
   }
  }
 }
 public sealed class ElevatorOutlineTarget:MonoBehaviour
 {
  public static readonly HashSet<ElevatorOutlineTarget> Active=new HashSet<ElevatorOutlineTarget>();
  public ElevatorOutlineGeometry[] geometry=Array.Empty<ElevatorOutlineGeometry>();
  public Color idleColor=new Color(1,.70f,.28f,1),hoverColor=new Color(.32f,.84f,1,1);
  [Min(0),Tooltip("주황색과 파란색 사이의 전환 시간(초). 0이면 즉시 전환합니다.")]public float colorTransitionDuration=.25f;
  [Min(1)]public float idleWidth=6.5f,hoverWidth=10.4f,idleHalo=20,hoverHalo=32;
  public bool Visible{get;private set;}
  public bool Hovered{get;private set;}
  public bool InRange{get;private set;}
  float flashUntil,rangeBlend;
  public float Width=>(Hovered?hoverWidth:idleWidth)*(flashUntil>Time.time?1.12f:1);
  public float Halo=>Hovered?hoverHalo:idleHalo;
  public Color Colour
  {
   get{var colour=Color.Lerp(idleColor,hoverColor,rangeBlend);return flashUntil>Time.time?Color.Lerp(colour,Color.white,.35f):colour;}
  }
  public void SetPresentation(bool visible,bool hovered,bool inRange=false)
  {
   bool wasVisible=Visible;Visible=visible;Hovered=visible&&hovered;InRange=visible&&inRange;
   if(Visible&&!wasVisible)rangeBlend=InRange?1:0;
  }
  public void SetVisible(bool visible){SetPresentation(visible,visible,visible);}
  public void Flash(){flashUntil=Time.time+.22f;}
  public void ClearFlash(){flashUntil=0;}
  void Update(){rangeBlend=Mathf.MoveTowards(rangeBlend,InRange?1:0,colorTransitionDuration>0?Time.unscaledDeltaTime/colorTransitionDuration:1);}
  void OnEnable(){rangeBlend=0;Active.Add(this);}
  void OnDisable(){Active.Remove(this);Visible=Hovered=InRange=false;rangeBlend=0;ClearFlash();}
 }
}
