using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using ProjectWait.M.Editor;
using Object=UnityEngine.Object;
public static class MigrateMO
{
 const string D="Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/";
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(current.isDirty){if(!current.path.StartsWith("Assets/M/"))throw new Exception("Unexpected dirty scene.");EditorSceneManager.SaveScene(current,D+"BeforeLiveScene.unity",true);EditorSceneManager.SaveScene(current);}
  string rendererPath="Assets/M/Arts/Rendering/ElevatorOutlineRenderer.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/M/Arts/Rendering/ElevatorScenePipeline.asset");var so=new SerializedObject(pipeline);var renderers=so.FindProperty("m_RendererDataList");var original=renderers.GetArrayElementAtIndex(0).objectReferenceValue;
  if(!AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath)){if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),rendererPath))throw new Exception("Renderer copy failed.");}
  var renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);var feature=renderer.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().FirstOrDefault();
  if(!feature){feature=ScriptableObject.CreateInstance<ElevatorSilhouetteOutlineFeature>();feature.name="Elevator mask silhouette";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
  feature.maskShader=Shader.Find("ProjectWait/M/Outline Coverage");feature.distanceShader=Shader.Find("ProjectWait/M/Outline Distance");feature.SetActive(true);feature.Create();renderer.SetDirty();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
  renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
  var reports=new List<object>();
  foreach(var path in ElevatorHoverRefinementSetup.Scenes)
  {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();int removed=0,count=0,geometry=0;
   foreach(var o in lesson.CollectObservations())
   {
    var old=o.shapeHighlight;if(!old)continue;var target=o.GetComponent<ElevatorOutlineTarget>();if(!target)target=o.gameObject.AddComponent<ElevatorOutlineTarget>();
    var inputs=new List<ElevatorOutlineGeometry>();var anchors=old.shells.Where(r=>r).Select(r=>r.transform.parent).Distinct().ToArray();
    bool actor=o.clue==ElevatorClue.Friend||o.clue==ElevatorClue.Ball||o.clue==ElevatorClue.Rescuer||o.clue==ElevatorClue.Phone;
    foreach(var anchor in anchors)
    {
     if(actor)
     {
      var source=anchor.parent.GetComponent<Renderer>();if(!source)throw new Exception("Actor source missing: "+anchor.name+" / "+o.name);
      if(!inputs.Any(g=>g.sourceRenderer==source))inputs.Add(new ElevatorOutlineGeometry{sourceRenderer=source});Object.DestroyImmediate(anchor.gameObject);removed+=4;
     }
     else
     {
      var mesh=anchor.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault();if(!mesh)throw new Exception("Static mask geometry missing.");inputs.Add(new ElevatorOutlineGeometry{mesh=mesh.sharedMesh,anchor=anchor});
      foreach(var child in anchor.Cast<Transform>().ToArray()){Object.DestroyImmediate(child.gameObject);removed++;}anchor.name="OutlineMaskAnchor_"+inputs.Count;
     }
    }
    target.geometry=inputs.ToArray();o.outlineTarget=target;geometry+=inputs.Count;count++;target.SetVisible(false);
    if(o.feedback)o.feedback.outlineTarget=target;
    var pulse=o.transform.Find("ButtonPressFeedback");if(pulse)Object.DestroyImmediate(pulse.gameObject);
    o.shapeHighlight=null;Object.DestroyImmediate(old);
    if(o.outline){if(o.outline.gameObject!=o.gameObject)Object.DestroyImmediate(o.outline.gameObject);else Object.DestroyImmediate(o.outline);o.outline=null;}
    EditorUtility.SetDirty(o);EditorUtility.SetDirty(target);if(o.feedback)EditorUtility.SetDirty(o.feedback);
   }
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed.");reports.Add(new{path,targets=count,groupedParts=geometry,removedMeshRenderers=removed});
  }
  AssetDatabase.SaveAssets();var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{renderer=rendererPath,scenes=reports},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(D+"Migration.json",report);return report;
 }
}