using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectInteraction
{
 public static string Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var cam=l.playerCamera;var data=cam.GetUniversalAdditionalCameraData();var rp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  var so=new SerializedObject(rp);var renderers=so.FindProperty("m_RendererDataList");var rd=Enumerable.Range(0,renderers.arraySize).Select(i=>renderers.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData).Where(x=>x).ToArray();
  var rig=l.presentation.space.playerRig;
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=SceneManager.GetActiveScene().path,playing=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,
   pipeline=new{name=rp.name,path=AssetDatabase.GetAssetPath(rp),rp.shadowDistance,rp.shadowCascadeCount,renderers=rd.Select(r=>new{name=r.name,features=r.rendererFeatures.Select(f=>new{name=f.name,type=f.GetType().FullName,active=f.isActive,settings=EditorJsonUtility.ToJson(f)}).ToArray()}).ToArray()},
   camera=new{cam.fieldOfView,cam.nearClipPlane,cam.farClipPlane,data.renderShadows,data.renderPostProcessing,position=cam.transform.position.ToString(),children=cam.GetComponentsInChildren<Transform>(true).Select(t=>new{name=t.name,active=t.gameObject.activeSelf,components=t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray()}).ToArray()},
   lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(x=>new{x.name,type=x.type.ToString(),x.intensity,shadows=x.shadows.ToString(),x.shadowStrength,position=x.transform.position.ToString()}).ToArray(),
   controller=EditorJsonUtility.ToJson(rig.GetComponent<CharacterController>()),
   locomotion=rig.GetComponentsInChildren<Behaviour>(true).Where(b=>b.GetType().FullName.Contains("Locomotion")||b.GetType().Name.Contains("Vignette")).Select(b=>new{b.name,type=b.GetType().FullName,b.enabled,active=b.gameObject.activeInHierarchy}).ToArray(),
   canvases=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>new{c.name,mode=c.renderMode.ToString(),c.sortingOrder,c.planeDistance,materials=c.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g=>g.material?g.material.shader.name:null).Distinct().ToArray()}).ToArray(),
   volumes=Object.FindObjectsByType<Volume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(v=>new{v.name,v.isGlobal,v.weight,profile=v.sharedProfile?EditorJsonUtility.ToJson(v.sharedProfile):null}).ToArray()},Newtonsoft.Json.Formatting.Indented);
  Directory.CreateDirectory("Assets/M/CodexTemp/ElevatorInteraction_20261030~");File.WriteAllText("Assets/M/CodexTemp/ElevatorInteraction_20261030~/Before.json",report);return report;
 }
}
