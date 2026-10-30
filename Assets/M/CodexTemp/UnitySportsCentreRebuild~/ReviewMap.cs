using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ReviewSportsMap
{
 static void Shot(Camera main,string name,Vector3 position,Vector3 target,float lens)
 {
  var go=new GameObject("Temporary_Map_Review_Camera");var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;go.AddComponent<UniversalAdditionalCameraData>();
  var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);rt.Create();var prior=RenderTexture.active;
  try
  {
   cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));cam.fieldOfView=2*Mathf.Atan(36/(2*lens)/1.6f)*Mathf.Rad2Deg;cam.aspect=1.6f;cam.targetTexture=rt;
   var request=new RenderPipeline.StandardRequest{destination=rt};if(RenderPipeline.SupportsRenderRequest(cam,request))RenderPipeline.SubmitRenderRequest(cam,request);else cam.Render();
   RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes("Assets/M/CodexTemp/UnitySportsCentreRebuild~/"+name+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
  }
  finally{RenderTexture.active=prior;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 }
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Run in Edit Mode.");var l=Object.FindFirstObjectByType<ElevatorLesson>();
  Shot(l.playerCamera,"10_Court",new Vector3(5.8f,9.92f,-10.4f),new Vector3(-.5f,9.92f,-20),23);
  Shot(l.playerCamera,"11_Building",new Vector3(39,30,-47),new Vector3(0,6,-7),40);
  Shot(l.playerCamera,"12_ThirdFloorWindow",new Vector3(-6.08f,9.57f,1.2f),new Vector3(-23,5.2f,-8),21);
  EditorSceneManager.SaveScene(l.gameObject.scene);AssetDatabase.SaveAssets();return "Three Unity map overview renders saved; scene saved.";
 }
}
