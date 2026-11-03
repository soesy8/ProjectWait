using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class CaptureAppliedSkybox
{
 const string Dir="Assets/M/CodexTemp/UIFadeSkybox_20261103~/";
 static void Save(Camera camera,string name)
 {
  var prior=camera.targetTexture;var active=RenderTexture.active;var render=new RenderTexture(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
  try
  {camera.targetTexture=render;camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(Dir+name+".png",image.EncodeToPNG());}
  finally{camera.targetTexture=prior;RenderTexture.active=active;Object.DestroyImmediate(render);Object.DestroyImmediate(image);}
 }
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required.");
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var scene=SceneManager.GetActiveScene();var go=new GameObject("Temporary skybox capture");var camera=go.AddComponent<Camera>();
  try
  {
   camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=65;camera.aspect=16f/9;camera.nearClipPlane=.03f;camera.farClipPlane=1000;camera.cullingMask=0;
   foreach(var pair in new[]{new[]{0f,0f},new[]{90f,0f},new[]{180f,0f},new[]{270f,0f}})
   {camera.transform.rotation=Quaternion.Euler(pair[1],pair[0],0);Save(camera,"Skybox_"+(int)pair[0]);}
   camera.cullingMask=~0;camera.transform.position=new Vector3(-1.8f,9.03f,-1.8f);camera.transform.rotation=Quaternion.Euler(-8,270,0);Save(camera,"Skybox_ThroughWindow");
   return new{success=true,material=AssetDatabase.GetAssetPath(RenderSettings.skybox),texture=AssetDatabase.GetAssetPath(RenderSettings.skybox.GetTexture("_MainTex")),shaderError=ShaderUtil.ShaderHasError(RenderSettings.skybox.shader),images=5};
  }
  finally{Object.DestroyImmediate(go);EditorSceneManager.OpenScene(scene.path,OpenSceneMode.Single);}
 }
}