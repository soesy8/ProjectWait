using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ValidateSavedFadeSkybox
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required.");
  string original=SceneManager.GetActiveScene().path;int validated=0;
  foreach(var name in new[]{"Elevator_Safety_Prototype","Elevator_Safety_ObjectInteraction_Test"})
  {
   EditorSceneManager.OpenScene("Assets/M/Scene/"+name+".unity",OpenSceneMode.Single);
   var l=Object.FindFirstObjectByType<ElevatorLesson>();var ui=l.view.spatial;var sky=RenderSettings.skybox;
   if(!ui||!ui.storybook||!ui.storybook.thoughtBubblePrefab)throw new Exception("UI reference missing.");
   if(!sky||AssetDatabase.GetAssetPath(sky)!="Assets/M/Arts/Materials/Elevator/StorybookCitySkybox.mat")throw new Exception("Saved skybox missing.");
   if(AssetDatabase.GetAssetPath(sky.GetTexture("_MainTex"))!="Assets/M/Arts/Textures/Skybox_Storybook_City_1024.png")throw new Exception("Wrong panorama reference.");
   if(l.playerCamera.clearFlags!=CameraClearFlags.Skybox)throw new Exception("Saved main camera does not draw skybox.");
   foreach(Transform child in l.view.thoughtPanel)if((child.name=="ThoughtHeading"||child.name=="ThoughtRibbon")&&child.gameObject.activeSelf)throw new Exception("Legacy header remains active in saved scene.");
   if(ShaderUtil.ShaderHasError(sky.shader))throw new Exception("Skybox shader error.");validated++;
  }
  EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
  return new{success=true,validated,compilationFailed=EditorUtility.scriptCompilationFailed,restoredScene=original};
 }
}