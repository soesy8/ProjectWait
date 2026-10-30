using System;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using ProjectWait.M.Editor;
public static class ApplyA2
{
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.isDirty){if(!ElevatorHoverRefinementSetup.Scenes.Contains(scene.path))throw new Exception("Unexpected dirty scene: "+scene.path);EditorSceneManager.SaveScene(scene,"Assets/M/CodexTemp/ElevatorAmbientHints_20261030~/BeforeLiveScene_StrongerOutline.unity",true);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Failed to preserve current scene.");}
  ElevatorHoverRefinementSetup.ApplyBoth();return "Applied to both M scenes; existing live edits preserved.";
 }
}