using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ValidateSmallerB
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  const string work="Assets/M/CodexTemp/RescueBHeight_20261104~/";
  var original=JObject.Parse(File.ReadAllText(work+"Delivery/BHeightHead_Validation.json"));
  var manifest=JObject.Parse(File.ReadAllText("Assets/M/Arts/Models/Rescuers/Sources/Rescuer_B_Manifest.json"));
  var checks=new List<object>();
  Action<bool,string> check=(pass,text)=>{checks.Add(new{passed=pass,check=text});if(!pass)throw new Exception(text);};
  string hash;using(var sha=SHA256.Create())using(var file=File.OpenRead(@"E:\ProjectWait\Blender\구조요원\Wait_Rescuer_B_LiftSpecialist.blend"))hash=BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
  check(hash==(string)manifest["source_blender_sha256"],"Imported FBX was generated from the edited original Blender file");
  double expected=(double)original["new_height_m"];
  check(Math.Abs(expected/(double)original["before"]["size"][2]-.89)<.00001,"Source total height is exactly 89 percent");
  var before=(JArray)original["head_before"]["size"];
  foreach(var path in new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"})
  {
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();var team=lesson.presentation.rescueTeam;
   var actor=team.specialist;var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=skin.sharedMesh;
   check(Vector3.Distance(actor.localScale,Vector3.one)<.00001f,path+": actor transform stays at unit scale");
   check(Math.Abs(mesh.bounds.size.y-expected)<.00005,path+": saved mesh height matches edited original");
   check(Mathf.Abs(team.specialistPoseScale.y-(float)manifest["pose_scale"][1])<.00001f,path+": gesture targets match the new skeleton");
   int head=Array.FindIndex(skin.bones,b=>b.name=="head");var positions=mesh.vertices;var weights=mesh.boneWeights;
   var points=positions.Where((v,i)=>weights[i].boneIndex0==head&&weights[i].weight0>.999f).ToArray();
   check(points.Length>100,path+": head vertices remain bound to head bone");
   var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
   float[] expectedHead={(float)before[0]*.96f,(float)before[2]*.96f,(float)before[1]*.96f};
   check(Vector3.Distance(bounds.size,new Vector3(expectedHead[0],expectedHead[1],expectedHead[2]))<.00015f,path+": head is uniformly 96 percent on all axes");
   check(Mathf.Abs(actor.GetComponent<BoxCollider>().size.y-mesh.bounds.size.y)<.00001f,path+": selection collider fits the new height");
  }
  EditorSceneManager.OpenScene("Assets/M/Scene/Elevator_Safety_Prototype.unity",OpenSceneMode.Single);
  File.WriteAllText(work+"AppliedValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="PASS",checks},Newtonsoft.Json.Formatting.Indented));
  return new{status="PASS",height_m=expected,checks=checks.Count};
 }
}
