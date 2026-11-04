using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ValidateSavedRescue
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode first");
  var checks=new List<object>();
  Action<bool,string> check=(ok,name)=>{checks.Add(new{passed=ok,check=name});if(!ok)throw new Exception(name);};
  string[] paths={"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
  foreach(var path in paths)
  {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();var team=lesson.presentation.rescueTeam;
   check(team&&lesson.presentation.rescuer==team.gameObject,path+": saved team reference");
   check(!team.gameObject.activeSelf,path+": crew hidden until arrival");
   check(team.firefighter&&team.specialist&&team.firefighter!=team.specialist,path+": two distinct actors");
   check(lesson.CollectObservations().Count(o=>o.clue==ElevatorClue.Rescuer)==2,path+": both observations saved");
   check(scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,path+": no missing scripts");
   foreach(var actor in new[]{team.firefighter,team.specialist})
   {
    var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);check(skins.Length==1,actor.name+": one skin renderer");
    var skin=skins[0];check(skin.bones.Length==18&&skin.bones.All(b=>b),actor.name+": all 18 bones saved");
    check(skin.sharedMaterials.Length==1&&skin.sharedMaterial&&skin.sharedMaterial.shader.isSupported,actor.name+": palette material available");
    check(!ShaderUtil.ShaderHasError(skin.sharedMaterial.shader),actor.name+": shader compiles");
    check(AssetDatabase.GetAssetPath(skin.sharedMesh).StartsWith("Assets/M/"),actor.name+": M-owned mesh asset");
    check(skin.sharedMesh.colors.Length==skin.sharedMesh.vertexCount,actor.name+": vertex palette saved");
    var observation=actor.GetComponent<ElevatorObservation>();check(observation.lesson==lesson&&observation.outlineTarget.geometry.Length==1&&observation.outlineTarget.geometry[0].sourceRenderer==skin,actor.name+": selection and outline references saved");
    string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject);
    check(AssetDatabase.GetDependencies(prefab,true).All(p=>!p.Contains("/CodexTemp/")),actor.name+": no runtime dependency on temporary tools");
   }
  }
  var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/M/Arts/Fonts/ElevatorKoreanFallback.asset");if(font)AssetDatabase.SaveAssetIfDirty(font);
  EditorSceneManager.OpenScene(paths[0],OpenSceneMode.Single);
  var result=new{status="PASS",checks=checks.ToArray(),physical_headset_tested=false};
  File.WriteAllText("Assets/M/CodexTemp/RescueIntegration_20261104~/SavedValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  return new{status="PASS",checks=checks.Count};
 }
}
