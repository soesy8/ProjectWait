using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using ProjectWait.M.Editor;
using Object=UnityEngine.Object;
public static class ValidateA2
{
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var checks=new List<object>();var records=new List<object>();string error=null;Action<bool,string> check=(ok,name)=>{checks.Add(new{name,passed=ok});if(!ok)throw new Exception(name);};
  try
  {
   foreach(var shaderName in new[]{"ProjectWait/M/Sky Highlight","ProjectWait/M/Sky Highlight Mask"}){var shader=Shader.Find(shaderName);check(shader&&!ShaderUtil.ShaderHasError(shader),"Shader compiles: "+shaderName);var mat=new Material(shader);check(mat.HasProperty("_StencilRef"),"Independent silhouette reference: "+shaderName);Object.DestroyImmediate(mat);}
   for(int i=0;i<2;i++)
   {
    var path=ElevatorHoverRefinementSetup.Scenes[i];var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();string p=i==0?"Original: ":"Guided: ";check(!scene.isDirty,p+"saved scene reopens cleanly");check(l.scenario.objectGuidedScene==(i==1),p+"scene mode is preserved");
    check(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),p+"no missing scripts");check(!AssetDatabase.GetDependencies(path,true).Any(x=>x.StartsWith("Assets/M/CodexTemp/")),p+"no runtime temporary dependencies");
    check(l.scenario.hints.Length==15&&l.scenario.hints.All(h=>h&&h.observation&&h.observation.hasDescription&&h.observation.shapeHighlight),p+"all fifteen described objects have idle-outline metadata");
    check(l.scenario.hints.All(h=>h.worldPixelSize==.002f&&h.group.alpha==0),p+"descriptions preserve fixed size and begin hidden");check(l.scenario.hints.All(h=>h.observation.shapeHighlight.idleColor.r>.9f&&h.observation.shapeHighlight.idleColor.b<.5f),p+"shared warm idle colour is saved");check(l.scenario.hints.All(h=>h.observation.shapeHighlight.idleWidthScale==1.25f&&h.observation.shapeHighlight.hoverWidthScale==2),p+"stronger idle and hover widths are saved");
    check(l.view.dialoguePanel.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha==1),p+"dialogue is not saved hidden");var wall=l.transform.Find("Scenario_TransparentBoundaries/3F_TerraceDoor");check(wall&&Mathf.Abs(wall.position.z+8.4f)<.001f&&wall.GetComponent<BoxCollider>().enabled,p+"rear barrier is saved two metres farther back");
    check(l.transform.Find("Scenario_TransparentBoundaries/3F_DownStairEntrance")&&l.transform.Find("Scenario_TransparentBoundaries/1F_StairEntrance"),p+"stair blockers remain");check(l.transform.Find("Furniture_CompoundColliders").GetComponentsInChildren<Collider>().Length==22,p+"furniture collisions remain");
    var entrances=l.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SportsCentre3F/Meshes/")&&f.transform.parent.name.EndsWith("08 · 뒤쪽 자동문 / 진입부")).ToArray();check(entrances.Length==17&&entrances.All(f=>f.GetComponent<MeshCollider>()&&f.GetComponent<MeshCollider>().enabled),p+"fixed entrance walls and glass retain collision");
    var face=l.GetComponent<ElevatorCharacterPerformance>();check(face.panicFaceMesh.triangles.Length==378420&&face.normalFaceMesh.triangles.Length==407493,p+"character face meshes are preserved");records.Add(new{path,guid=AssetDatabase.AssetPathToGUID(path),descriptions=l.scenario.hints.Length,barrierZ=wall.position.z});
   }
  }
  catch(Exception e){error=e.ToString();}
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,passed=checks.Count(x=>(bool)x.GetType().GetProperty("passed").GetValue(x)),checks,scenes=records,error},Newtonsoft.Json.Formatting.Indented);File.WriteAllText("Assets/M/CodexTemp/ElevatorAmbientHints_20261030~/SavedValidation.json",report);return report;
 }
}