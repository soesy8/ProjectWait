using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class FinishSportsScene
{
 const string Dir="Assets/M/CodexTemp/UnitySportsCentreRebuild~/";
 const string Prefab="Assets/M/Prefabs/SportsCentre3F/SportsCentreMap.prefab";
 static int Colliders(Transform root)
 {
  int count=0;
  foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
  {
   string n=filter.transform.parent.name;
   if(!(n.Contains("통창")||n.Contains("비워둔 구조")||n.Contains("농구 연습장")||n.Contains("탈의"))||filter.GetComponent<MeshCollider>()||!filter.sharedMesh)continue;
   filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;count++;
  }
  return count;
 }
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode before finishing.");
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/M/Scene/Elevator_Safety_Prototype.unity")throw new Exception("Wrong scene.");
  var contents=PrefabUtility.LoadPrefabContents(Prefab);int prefabColliders;
  try{prefabColliders=Colliders(contents.transform);PrefabUtility.SaveAsPrefabAsset(contents,Prefab);}finally{PrefabUtility.UnloadPrefabContents(contents);}
  var lesson=Object.FindFirstObjectByType<ElevatorLesson>();int sceneColliders=Colliders(lesson.GetComponent<ElevatorSportsCentreMap>().geometry);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  scene=EditorSceneManager.OpenScene(scene.path,OpenSceneMode.Single);Physics.SyncTransforms();
  lesson=Object.FindFirstObjectByType<ElevatorLesson>();var map=lesson.GetComponent<ElevatorSportsCentreMap>();
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var checks=new List<object>();
  Action<string,bool> check=(name,passed)=>checks.Add(new{name,passed});
  check("Saved M scene reopened",scene.path=="Assets/M/Scene/Elevator_Safety_Prototype.unity"&&!scene.isDirty);
  check("No missing scripts",all.All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0));
  check("No missing active materials or unsupported shaders",all.Where(t=>t.gameObject.activeInHierarchy).SelectMany(t=>t.GetComponents<Renderer>()).All(r=>r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported)));
  check("Native map and panels assigned",map.geometry&&map.sideIntercom&&map.frontIntercom&&map.identity&&map.facilityGuide&&map.autoDoorLeft&&map.autoDoorRight);
  check("Main doors bound to imported model",lesson.presentation.leftDoor.IsChildOf(map.geometry)&&lesson.presentation.rightDoor.IsChildOf(map.geometry));
  check("Story identity correct",lesson.settings.building=="한빛 생활체육센터"&&lesson.settings.liftId.Contains("HB-EL-01"));
  check("Start pose is 3F",Mathf.Abs(lesson.presentation.space.playerRig.position.y-7.92f)<.001f&&Mathf.Abs(map.friendInside.z-4.12f)<.001f);
  check("UI readable backdrops persisted",lesson.view.spatial.controlsBackdrop&&lesson.view.spatial.progressBackdrop);
  var dependencies=AssetDatabase.GetDependencies(scene.path,true);var temp=dependencies.Where(p=>p.Contains("/CodexTemp/")).ToArray();check("No temporary runtime asset dependency",temp.Length==0);
  check("Legacy lobby hidden",!lesson.transform.Find("BasketballLobby").gameObject.activeSelf);
  var floors=new List<object>();
  foreach(var test in new[]{new Vector3(0,.7f,0),new Vector3(0,4.6f,0),new Vector3(0,8.6f,0),new Vector3(-4,8.6f,-14)})
  {
   bool hit=Physics.Raycast(test,Vector3.down,out var info,1.2f);float expected=test.y<1?0:test.y<5?3.96f:7.92f;
   bool passed=hit&&Mathf.Abs(info.point.y-expected)<.12f;floors.Add(new{origin=test.ToString(),hit,height=hit?info.point.y:-999,expected,passed});check("Walkable floor at "+test,passed);
  }
  check("3F window collision",Physics.Raycast(new Vector3(-5,9.5f,0),Vector3.left,out var window,3)&&window.point.x<-6);
  foreach(string name in new[]{"3F_Side_Emergency_1.28m","3F_LiftIdentity_HB-EL-01"})
  {
   var target=lesson.transform.Find("SportsCentre_Interactions/"+name);var origin=map.inside+Vector3.up*1.1064756f;
   bool hit=Physics.Raycast(origin,(target.position-origin).normalized,out var result,10);check("Collision changes preserve ray to "+name,hit&&result.collider.transform==target);
  }
  var manifest=JsonUtility.FromJson<ProjectWait.M.Editor.ElevatorSportsCentreSetup.Manifest>(File.ReadAllText("Assets/M/Arts/Models/SportsCentre3F/SportsCentreManifest.json"));
  check("Active floor 3 and unused lift 5",manifest.objects.Any(p=>p.name=="3F · Lift 1 · 층수"&&p.body=="3")&&manifest.objects.Any(p=>p.name=="3F · Lift 2 · 층수"&&p.body=="5"));
  check("Directory has no current-floor highlight",!manifest.objects.Any(p=>p.name=="3F · 현재층 안내 강조"||p.name=="3F · 현위치 표기"));
  bool success=checks.All(c=>(bool)c.GetType().GetProperty("passed").GetValue(c));
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success,passed=checks.Count(c=>(bool)c.GetType().GetProperty("passed").GetValue(c)),checks,prefabColliders,sceneColliders,floors,tempDependencies=temp,scene=scene.path,dependencyCount=dependencies.Length,completedUtc=DateTime.UtcNow.ToString("o")},Newtonsoft.Json.Formatting.Indented);
  File.WriteAllText(Dir+"SceneValidation.json",report);return report;
 }
}
