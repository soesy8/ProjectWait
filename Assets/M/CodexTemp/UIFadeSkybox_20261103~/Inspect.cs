using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectFadeSkybox
{
 public static object Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var scene=SceneManager.GetActiveScene();
  if(!l||!scene.path.StartsWith("Assets/M/Scene/"))throw new Exception("M scene required.");
  var result=new{
   scene=scene.path,dirty=scene.isDirty,playing=Application.isPlaying,skybox=AssetDatabase.GetAssetPath(RenderSettings.skybox),ambient=RenderSettings.ambientMode.ToString(),
   cameras=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).Select(c=>new{name=c.name,main=c==l.playerCamera,active=c.gameObject.activeInHierarchy,clear=c.clearFlags.ToString(),skybox=c.GetComponent<Skybox>()?AssetDatabase.GetAssetPath(c.GetComponent<Skybox>().material):null}).ToArray(),
   skyMeshes=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&(r.name.IndexOf("sky",StringComparison.OrdinalIgnoreCase)>=0||r.sharedMaterials.Any(m=>m&&m.name=="LobbySky"))).Select(r=>new{name=r.name,parent=r.transform.parent?r.transform.parent.name:"",active=r.gameObject.activeInHierarchy,enabled=r.enabled,materials=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray(),bounds=r.bounds.ToString()}).ToArray(),
   headers=l.view.spatial.hud.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.name.Contains("Heading")||t.text.Contains("살펴보니")).Select(t=>new{name=t.name,parent=t.transform.parent.name,active=t.gameObject.activeSelf,text=t.text}).ToArray(),
   hintObjects=Object.FindObjectsByType<ElevatorObjectHint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(h=>new{h.name,alpha=h.group?h.group.alpha:-1,heading=h.transform.Find("Heading")?h.transform.Find("Heading").GetComponent<TMPro.TMP_Text>()?.text:null}).ToArray()
  };
  File.WriteAllText("Assets/M/CodexTemp/UIFadeSkybox_20261103~/Inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}