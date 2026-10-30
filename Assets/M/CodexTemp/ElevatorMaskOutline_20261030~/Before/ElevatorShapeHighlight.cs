using System.Collections.Generic;
using UnityEngine;
namespace ProjectWait.M.Elevator
{
 public sealed class ElevatorShapeHighlight:MonoBehaviour
 {
  public Renderer[] shells;
  public SkinnedMeshRenderer[] skinnedSources,skinnedShells;
  public Color idleColor=new Color(1f,.70f,.28f,1);
  [Range(.1f,3)] public float idleWidthScale=1.25f;
  [Range(1,4)] public float hoverWidthScale=2;
  public bool Visible{get;private set;}
  public bool Hovered{get;private set;}
  static readonly HashSet<int> Groups=new HashSet<int>();
  readonly Dictionary<Material,Material> owned=new Dictionary<Material,Material>();
  readonly Dictionary<Renderer,Material[]> originals=new Dictionary<Renderer,Material[]>();
  int group;
  bool initialized;
  void Prepare()
  {
   if(initialized||!Application.isPlaying||shells==null)return;
   for(int i=1;i<64;i++)if(Groups.Add(i)){group=i;break;}
   if(group==0)return;
   foreach(var r in shells)
   {
    if(!r)continue;var sources=r.sharedMaterials;originals[r]=sources;var materials=new Material[sources.Length];
    for(int i=0;i<sources.Length;i++)
    {
     var source=sources[i];if(!source)continue;
     if(!owned.TryGetValue(source,out var copy)){copy=new Material(source){name=source.name+" (object outline)",hideFlags=HideFlags.DontSave};copy.SetFloat("_StencilRef",group);owned.Add(source,copy);}materials[i]=copy;
    }
    r.sharedMaterials=materials;
   }
   initialized=true;
  }
  public void SetPresentation(bool visible,bool hovered)
  {
   if(!visible){SetVisible(false);return;}
   Prepare();if(!initialized)return;
   if(Visible&&Hovered==hovered)return;
   Visible=true;Hovered=hovered;
   // Each object draws its mask, then its shells. A background object's mask
   // cannot erase the hovered object's silhouette when many outlines are visible.
   int queue=3000+(group+(hovered?64:0))*4;
   foreach(var entry in owned)
   {
    bool mask=!entry.Key.HasProperty("_Width");float width=mask?0:entry.Key.GetFloat("_Width");
    entry.Value.renderQueue=queue+(mask?0:width>12?1:width>7?2:3);
    if(mask)continue;
    entry.Value.SetFloat("_Width",width*(hovered?hoverWidthScale:idleWidthScale));
    var sourceColor=entry.Key.GetColor("_Color");var color=hovered?sourceColor:idleColor;
    color.a=Mathf.Clamp01(sourceColor.a*2)*(hovered?1:idleColor.a);entry.Value.SetColor("_Color",color);
   }
   foreach(var r in shells)if(r)r.enabled=true;
  }
  public void SetVisible(bool value)
  {
   if(value){SetPresentation(true,true);return;}
   Visible=Hovered=false;if(shells!=null)foreach(var r in shells)if(r)r.enabled=false;
  }
  void LateUpdate()
  {
   if(!Visible||skinnedSources==null||skinnedShells==null)return;
   for(int i=0;i<skinnedShells.Length;i++)
   {var s=skinnedSources[i];var d=skinnedShells[i];if(!s||!d||!d.enabled||!s.sharedMesh)continue;for(int b=0;b<s.sharedMesh.blendShapeCount;b++)d.SetBlendShapeWeight(b,s.GetBlendShapeWeight(b));}
  }
  void OnDisable(){SetVisible(false);ReleaseMaterials();}
  void OnDestroy(){ReleaseMaterials();}
  void ReleaseMaterials()
  {
   foreach(var e in originals)if(e.Key)e.Key.sharedMaterials=e.Value;
   foreach(var m in owned.Values)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}
   if(group!=0)Groups.Remove(group);
   group=0;initialized=false;owned.Clear();originals.Clear();
  }
 }
}
