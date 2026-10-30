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
public static class ValidateSavedHV
{
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var checks=new List<object>();var scenes=new List<object>();Action<bool,string> check=(ok,name)=>{checks.Add(new{name,passed=ok});if(!ok)throw new Exception(name);};string error=null;
  try
  {
   for(int i=0;i<2;i++)
   {
    var path=ElevatorHoverRefinementSetup.Scenes[i];var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();var prefix=i==0?"Original: ":"Guided: ";
    check(!scene.isDirty,prefix+"reopened saved scene is clean");check(l&&l.scenario&&l.pauseMenu&&l.view.spatial,prefix+"essential scene references are present");check(l.scenario.objectGuidedScene==(i==1),prefix+"distinct scene mode is preserved");
    check(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),prefix+"no missing script components");
    check(!AssetDatabase.GetDependencies(path,true).Any(p=>p.StartsWith("Assets/M/CodexTemp/")),prefix+"runtime has no temporary-file dependencies");
    var pause=(RectTransform)l.pauseMenu.canvas.transform;check(pause.pivot==new Vector2(.5f,.5f)&&pause.anchorMin==new Vector2(.5f,.5f)&&pause.anchorMax==new Vector2(.5f,.5f),prefix+"pause root uses a centred pivot and anchors");check(pause.sizeDelta==new Vector2(1280,720)&&Mathf.Abs(pause.localScale.x-.0016f)<.000001f,prefix+"pause has the saved physical scale and dimensions");check(!l.pauseMenu.canvas.gameObject.activeSelf,prefix+"pause menu is initially hidden");
    check(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).All(c=>c.renderMode==RenderMode.WorldSpace),prefix+"all scene canvases remain world space");check(!l.view.spatial.hud.gameObject.activeSelf,prefix+"screen HUD stays disabled");
    check(l.scenario.hints.Length>=14&&l.scenario.hints.All(h=>h&&h.observation&&h.label&&h.group),prefix+"descriptions reference multiple real objects");check(l.scenario.hints.All(h=>Mathf.Abs(h.worldPixelSize-.002f)<.000001f&&h.transform.localScale==Vector3.one*.002f&&h.group.alpha==0&&!h.group.blocksRaycasts),prefix+"fixed physical-size descriptions start hidden and do not intercept rays");
    var walls=l.transform.Find("Scenario_TransparentBoundaries");check(walls.GetComponentsInChildren<Collider>().All(c=>c.enabled&&!c.isTrigger)&&walls.GetComponentsInChildren<Renderer>().Length==0,prefix+"all boundaries are solid and invisible");check(Mathf.Abs(walls.Find("3F_TerraceDoor").position.z+6.4f)<.001f,prefix+"basketball sliding-door barrier is behind the threshold");check(walls.Find("3F_DownStairEntrance")&&walls.Find("1F_StairEntrance"),prefix+"descending and ground-floor ascending stair blockers are saved");
    var props=l.transform.Find("Furniture_CompoundColliders");check(props&&props.GetComponentsInChildren<CapsuleCollider>().Length==4,prefix+"four planter bodies have collision");check(props.GetComponentsInChildren<BoxCollider>().Length>=16,prefix+"chair seats, backs and arms have collision");
    var native=l.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SportsCentre3F/Meshes/")).ToArray();var entrances=native.Where(f=>f.transform.parent.name.EndsWith("08 · 뒤쪽 자동문 / 진입부")).ToArray();
    check(entrances.Length>=16&&entrances.All(f=>f.GetComponent<MeshCollider>()&&f.GetComponent<MeshCollider>().enabled&&!f.GetComponent<MeshCollider>().isTrigger&&f.GetComponent<MeshCollider>().sharedMesh==f.sharedMesh),prefix+"both floors' fixed entrance walls, frames and glass use their actual collision mesh");
    var court=native.Where(f=>f.transform.parent.name.Contains("농구 연습장")||f.transform.parent.name.Contains("탈의")).ToArray();check(court.Length>=15&&court.All(f=>f.GetComponent<MeshCollider>()&&f.GetComponent<MeshCollider>().enabled&&f.GetComponent<MeshCollider>().sharedMesh),prefix+"existing court and connecting-corridor collision is intact");
    var face=l.GetComponent<ElevatorCharacterPerformance>();check(face&&face.normalFaceMesh&&face.panicFaceMesh&&face.panicFace,prefix+"normal and panic expressions retain references");check(face.normalFaceMesh.triangles.Length==407493&&face.panicFaceMesh.triangles.Length==378420,prefix+"original eyes and mouth remain removed from the panic base mesh");check(face.panicFace.GetComponentsInChildren<LineRenderer>(true).Length==3,prefix+"panic expression retains only two black spiral eyes and a wave mouth");
    check(l.settings.waitDialogue.Contains("도움")&&!l.settings.waitDialogue.Contains("만져"),prefix+"reassurance dialogue matches the calmer waiting story");
    scenes.Add(new{path,guid=AssetDatabase.AssetPathToGUID(path),hints=l.scenario.hints.Length,boundaries=walls.GetComponentsInChildren<Collider>().Length,props=props.GetComponentsInChildren<Collider>().Length,fixedEntranceMeshes=entrances.Length});
   }
   check(AssetDatabase.AssetPathToGUID(ElevatorHoverRefinementSetup.Scenes[0])!=AssetDatabase.AssetPathToGUID(ElevatorHoverRefinementSetup.Scenes[1]),"Original and test scene GUIDs remain distinct");
  }
  catch(Exception e){error=e.ToString();}
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,passed=checks.Count(x=>(bool)x.GetType().GetProperty("passed").GetValue(x)),checks,scenes,error},Newtonsoft.Json.Formatting.Indented);File.WriteAllText("Assets/M/CodexTemp/ElevatorHoverReview_20261030~/SavedSceneValidation.json",report);return report;
 }
}