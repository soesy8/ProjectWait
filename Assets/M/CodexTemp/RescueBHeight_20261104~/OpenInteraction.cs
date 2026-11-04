using UnityEditor.SceneManagement;
using UnityEngine;
public static class OpenSmallerBInteraction
{
 public static object Run()
 {
  if(Application.isPlaying)throw new System.Exception("Edit mode required");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
   if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("Unsaved scene changes");
  var scene=EditorSceneManager.OpenScene("Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity",OpenSceneMode.Single);
  return new{scene=scene.path};
 }
}
