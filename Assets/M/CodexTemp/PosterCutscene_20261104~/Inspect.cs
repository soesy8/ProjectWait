using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectPosterCutscene
{
 public static object Run()
 {
  var lesson=Object.FindFirstObjectByType<ElevatorLesson>();
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  return new{scene=scene.path,scene.isDirty,playing=Application.isPlaying,
   posters=lesson.CollectObservations().Where(o=>o.clue==ElevatorClue.Notice).Select(o=>new{name=o.name,position=o.transform.position.ToString("F4"),rotation=o.transform.eulerAngles.ToString(),colliders=o.GetComponents<Collider>().Select(c=>new{type=c.GetType().Name,center=c.bounds.center.ToString("F4"),size=c.bounds.size.ToString("F4")})}),
   camera=lesson.playerCamera.transform.position.ToString(),
   settings=UnityEditor.AssetDatabase.GetAssetPath(lesson.settings),font=UnityEditor.AssetDatabase.GetAssetPath(lesson.view.font),
   canvases=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).Select(c=>new{c.name,mode=c.renderMode.ToString(),c.sortingOrder})};
 }
}
