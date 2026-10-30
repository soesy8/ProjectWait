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
  [Min(1)]public float idleWidth=6.5f,hoverWidth=10.4f,idleHalo=20,hoverHalo=32;
  public bool Visible{get;private set;}
  public bool Hovered{get;private set;}
  float flashUntil;
  public float Width=>(Hovered?hoverWidth:idleWidth)*(flashUntil>Time.time?1.12f:1);
  public float Halo=>Hovered?hoverHalo:idleHalo;
  public Color Colour=>flashUntil>Time.time?Color.Lerp(Hovered?hoverColor:idleColor,Color.white,.35f):Hovered?hoverColor:idleColor;
  public void SetPresentation(bool visible,bool hovered){Visible=visible;Hovered=visible&&hovered;}
  public void SetVisible(bool visible){SetPresentation(visible,visible);}
  public void Flash(){flashUntil=Time.time+.22f;}
  public void ClearFlash(){flashUntil=0;}
  void OnEnable(){Active.Add(this);}
  void OnDisable(){Active.Remove(this);Visible=Hovered=false;ClearFlash();}
 }
}
