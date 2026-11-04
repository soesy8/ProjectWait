using UnityEditor.SceneManagement;
using UnityEngine;
public static class RetryPosterApply
{
 public static object Run()
 {
  if(Application.isPlaying)throw new System.Exception("Edit mode required");
  // The failed first application only made these unsaved, task-owned edits.
  // The scene was verified clean before applying; reopen its saved version.
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/M/Scene/Elevator_Safety_Prototype.unity")throw new System.Exception("Unexpected scene");
  EditorSceneManager.OpenScene(scene.path,OpenSceneMode.Single);
  return ProjectWait.M.Editor.ElevatorPosterCutsceneSetup.ApplyBoth();
 }
}
