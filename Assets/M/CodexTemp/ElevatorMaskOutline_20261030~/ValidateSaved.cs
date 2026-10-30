using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering.Universal;using UnityEditor;using UnityEditor.SceneManagement;using ProjectWait.M.Elevator;using ProjectWait.M.Editor;using Object=UnityEngine.Object;
public static class ValidateSavedMaskMO
{
 const string D="Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/";
 static List<object> checks=new List<object>();static void Check(bool ok,string name){checks.Add(new{name,passed=ok});if(!ok)throw new Exception(name);}
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode first");checks.Clear();var details=new List<object>();string error=null;
  try
  {
   var data=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ElevatorMaskOutlineSetup.RendererPath);Check(data&&data.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().Count(f=>f.isActive)==1,"One active outline feature in M renderer");var feature=data.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().Single();Check(feature.maskShader&&feature.distanceShader,"Both rendering shaders are serialized build dependencies");
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/M/Arts/Rendering/ElevatorScenePipeline.asset");Check(new SerializedObject(pipeline).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue==data,"Only M pipeline points to the dedicated M renderer");
   foreach(var shader in new[]{feature.maskShader,feature.distanceShader})Check(!ShaderUtil.ShaderHasError(shader)&&ShaderUtil.GetShaderMessages(shader).Length==0,"Clean shader compile: "+shader.name);
   foreach(var path in ElevatorHoverRefinementSetup.Scenes)
   {
    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var roots=scene.GetRootGameObjects();var transforms=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var components=roots.SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c).ToArray();var l=components.OfType<ElevatorLesson>().Single();var targets=components.OfType<ElevatorOutlineTarget>().ToArray();
    Check(targets.Length==16,path+": 16 saved targets");Check(l.CollectObservations().Where(o=>o.hasDescription).All(o=>o.outlineTarget&&o.outlineTarget.geometry.Length>0),path+": all descriptions have grouped mask geometry");
    Check(targets.SelectMany(t=>t.geometry).All(g=>g.sourceRenderer||(g.mesh&&g.anchor)),path+": mask source references resolve after reopening");Check(targets.SelectMany(t=>t.geometry).Where(g=>g.anchor).All(g=>g.anchor.GetComponentsInChildren<Renderer>(true).Length==0),path+": static mask anchors create no visible duplicate renderers");
    Check(!components.Any(c=>c.GetType().Name=="ElevatorShapeHighlight")&&!transforms.Any(t=>t.name.StartsWith("SkyGlow")||t.name=="ButtonPressFeedback"||t.name=="PointerOutline"||t.name=="ObservationOutline"),path+": legacy renderer objects and components absent");
    Check(components.OfType<ElevatorButtonFeedback>().All(f=>f.outlineTarget==f.GetComponent<ElevatorObservation>().outlineTarget&&(!f.gameObject.activeInHierarchy||f.outlineTarget)),path+": button feedback reuses its target mask");
    Check(transforms.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,path+": no missing scripts");
    var missing=new List<string>();foreach(var component in components){var so=new SerializedObject(component);var prop=so.GetIterator();while(prop.Next(true))if(prop.propertyType==SerializedPropertyType.ObjectReference&&prop.objectReferenceValue==null&&prop.objectReferenceInstanceIDValue!=0)missing.Add(component.name+"/"+component.GetType().Name+"/"+prop.propertyPath);}
    var outlineMissing=missing.Where(m=>m.Contains("/ElevatorOutlineTarget/")||m.Contains("/ElevatorObservation/")||m.Contains("/ElevatorButtonFeedback/")||m.Contains("/ElevatorSceneRendering/")).ToArray();Check(outlineMissing.Length==0,path+": no broken outline source or component references");
    var deps=AssetDatabase.GetDependencies(path,true);Check(!deps.Any(p=>p.Contains("CodexTemp")),path+": no temporary runtime dependencies");Check(!deps.Any(p=>p.Contains("SkyHighlight")||p.Contains("SkyGlow")||p.EndsWith("ElevatorShapeHighlight.cs")),path+": no legacy shader or script dependencies");
    Check(l.scenario.objectGuidedScene==path.Contains("ObjectInteraction"),path+": original/test scenario mode retained");details.Add(new{path,targets=targets.Length,parts=targets.Sum(t=>t.geometry.Length),missing});
   }
   foreach(var file in new[]{"Assets/M/Scripts/Elevator/ElevatorShapeHighlight.cs","Assets/M/Arts/Shaders/ElevatorSkyHighlight.shader","Assets/M/Arts/Shaders/ElevatorSkyHighlightMask.shader","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlowMask.mat","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlow_0.mat","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlow_1.mat","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlow_2.mat"})Check(!File.Exists(file)&&!File.Exists(file+".meta"),"Removed obsolete asset and meta: "+file);
  }catch(Exception e){error=e.ToString();}
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,passed=checks.Count(c=>(bool)c.GetType().GetProperty("passed").GetValue(c)),checks,scenes=details,error},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(D+"SavedSceneValidation.json",report);return report;
 }
}
