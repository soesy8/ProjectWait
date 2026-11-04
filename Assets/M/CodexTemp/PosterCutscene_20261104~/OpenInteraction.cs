using UnityEditor.SceneManagement;
using UnityEngine;
public static class OpenPosterInteraction
{
 public static object Run()
 {
  if(Application.isPlaying)throw new System.Exception("Edit mode required");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("Unsaved changes");
  return EditorSceneManager.OpenScene("Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity",OpenSceneMode.Single).path;
 }
}
