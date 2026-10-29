#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Object=UnityEngine.Object;
namespace ProjectWait.M.Editor
{
 public static class ElevatorPaletteOptimizer
 {
  const string PathRoot="Assets/M/Arts/Models/Elevator/Optimized/";
  public static Material Palette
  {
   get
   {
    const string path="Assets/M/Arts/Materials/Elevator/UnifiedPalette.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
    m=new Material(Shader.Find("ProjectWait/M/Vertex Palette Toon")){name="UnifiedPalette",enableInstancing=true};AssetDatabase.CreateAsset(m,path);return m;
   }
  }
  static Color Linear(Color c)=>QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;
  static float F(Material m,string p,float fallback)=>m.HasProperty(p)?m.GetFloat(p):fallback;
  static Color C(Material m,string p,Color fallback)=>Linear(m.HasProperty(p)?m.GetColor(p):fallback);
  public static Mesh Bake(Mesh source,Material[] materials,string name,bool save=true)
  {
   Directory.CreateDirectory(PathRoot);
   if(materials.Any(m=>!m||!m.shader.name.Contains("Storybook Toon")))throw new InvalidOperationException("Only constant-color storybook materials may be baked: "+name);
   var originalVertex=new List<int>();var materialForVertex=new List<int>();var lookup=new Dictionary<long,int>();var indices=new List<int>();
   for(int s=0;s<source.subMeshCount;s++)foreach(int vertex in source.GetTriangles(s))
   {
    long key=((long)vertex<<32)|(uint)s;
    if(!lookup.TryGetValue(key,out int mapped)){mapped=originalVertex.Count;lookup.Add(key,mapped);originalVertex.Add(vertex);materialForVertex.Add(s);}
    indices.Add(mapped);
   }
   var positions=source.vertices;var normals=source.normals;var tangents=source.tangents;
   var result=new Mesh{name=name,indexFormat=originalVertex.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
   result.vertices=originalVertex.Select(v=>positions[v]).ToArray();if(normals.Length>0)result.normals=originalVertex.Select(v=>normals[v]).ToArray();if(tangents.Length>0)result.tangents=originalVertex.Select(v=>tangents[v]).ToArray();
   foreach(int channel in new[]{0,5,6,7}){var uv=new List<Vector4>();source.GetUVs(channel,uv);if(uv.Count>0)result.SetUVs(channel,originalVertex.Select(v=>uv[v]).ToList());}
   result.bindposes=source.bindposes;
   using(var counts=source.GetBonesPerVertex())using(var weights=source.GetAllBoneWeights())
   {
    if(counts.Length>0)
    {
     var offsets=new int[counts.Length];for(int v=1;v<counts.Length;v++)offsets[v]=offsets[v-1]+counts[v-1];var expandedWeights=new List<BoneWeight1>();var expandedCounts=new byte[originalVertex.Count];
     for(int v=0;v<originalVertex.Count;v++){int sourceVertex=originalVertex[v];expandedCounts[v]=counts[sourceVertex];for(int n=0;n<counts[sourceVertex];n++)expandedWeights.Add(weights[offsets[sourceVertex]+n]);}
     using(var newCounts=new Unity.Collections.NativeArray<byte>(expandedCounts,Unity.Collections.Allocator.Temp))using(var newWeights=new Unity.Collections.NativeArray<BoneWeight1>(expandedWeights.ToArray(),Unity.Collections.Allocator.Temp))result.SetBoneWeights(newCounts,newWeights);
    }
   }
   for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
   {
    var dv=new Vector3[source.vertexCount];var dn=new Vector3[source.vertexCount];var dt=new Vector3[source.vertexCount];source.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
    result.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),originalVertex.Select(v=>dv[v]).ToArray(),originalVertex.Select(v=>dn[v]).ToArray(),originalVertex.Select(v=>dt[v]).ToArray());
   }
   var colors=new Color[result.vertexCount];var style=new Vector4[result.vertexCount];var shade=new Vector4[result.vertexCount];var ink=new Vector4[result.vertexCount];var bands=new Vector2[result.vertexCount];
   for(int v=0;v<result.vertexCount;v++)
   {
    var m=materials[materialForVertex[v]];var sh=C(m,"_ShadeColor",new Color32(189,200,195,255));
    colors[v]=C(m,"_BaseColor",Color.white);style[v]=new Vector4(F(m,"_OutlineWidth",0),F(m,"_EmissionStrength",0),F(m,"_Cull",2),F(m,"_BandSoftness",.012f));
    shade[v]=new Vector4(sh.r,sh.g,sh.b,F(m,"_MidTone",.9f));ink[v]=C(m,"_OutlineColor",new Color32(73,51,39,255));bands[v]=new Vector2(F(m,"_ShadowThreshold",.02f),F(m,"_LightThreshold",.62f));
   }
   result.colors=colors;result.SetUVs(1,style.ToList());result.SetUVs(2,shade.ToList());result.SetUVs(3,ink.ToList());result.SetUVs(4,bands.ToList());result.SetTriangles(indices,0,false);result.bounds=source.bounds;
   if(!save)return result;
   var path=PathRoot+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(old){EditorUtility.CopySerialized(result,old);old.vertices=result.vertices;old.UploadMeshData(false);Object.DestroyImmediate(result);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);return old;}
   AssetDatabase.CreateAsset(result,path);return result;
  }
  public static object OptimizeCharacters(Transform root)
  {
   var reports=new List<object>();var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
   foreach(var r in renderers.Where(r=>r.sharedMaterials.Length>1))
   {
    var source=r.sharedMesh;int slots=r.sharedMaterials.Length;var baked=Bake(source,r.sharedMaterials,"CharacterPalette"+Array.IndexOf(renderers,r));
    r.sharedMesh=baked;r.sharedMaterials=new[]{Palette};
    reports.Add(new{r.name,before=slots,after=1,verticesBefore=source.vertexCount,verticesAfter=baked.vertexCount,blendShapes=baked.blendShapeCount,bones=baked.bindposes.Length});
   }
   return reports;
  }
  public static void CombineStatic(Transform root,string name)
  {
   var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&r.sharedMaterials.All(m=>m&&m.shader.name.Contains("Storybook Toon"))).ToArray();
   if(renderers.Length==0)return;
   var instances=new List<CombineInstance>();var temporary=new List<Mesh>();int i=0;
   foreach(var r in renderers)
   {
    var mesh=Bake(r.GetComponent<MeshFilter>().sharedMesh,r.sharedMaterials,name+"Part"+i++,false);temporary.Add(mesh);
    instances.Add(new CombineInstance{mesh=mesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix});
   }
   var combined=new Mesh{name=name,indexFormat=IndexFormat.UInt32};combined.CombineMeshes(instances.ToArray(),true,true,false);
   string path=PathRoot+name+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(previous){EditorUtility.CopySerialized(combined,previous);previous.vertices=combined.vertices;previous.UploadMeshData(false);Object.DestroyImmediate(combined);combined=previous;EditorUtility.SetDirty(combined);AssetDatabase.SaveAssetIfDirty(combined);}else AssetDatabase.CreateAsset(combined,path);
   var filter=root.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=combined;var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette;renderer.shadowCastingMode=ShadowCastingMode.Off;
   foreach(var r in renderers){var go=r.gameObject;Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);if(go.transform.childCount==0&&go.GetComponents<Component>().Length==1)Object.DestroyImmediate(go);}
   foreach(var mesh in temporary)Object.DestroyImmediate(mesh);
  }
  public static void OptimizeSingle(Renderer renderer,string name)
  {
   var filter=renderer.GetComponent<MeshFilter>();if(!filter)return;
   filter.sharedMesh=Bake(filter.sharedMesh,renderer.sharedMaterials,name);renderer.sharedMaterials=new[]{Palette};
  }
 }
}
#endif
