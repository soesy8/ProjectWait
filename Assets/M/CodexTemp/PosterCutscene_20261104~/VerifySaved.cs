using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifySavedPoster
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene changes");
  var checks=new List<object>();Action<bool,string> check=(pass,name)=>{checks.Add(new{pass,name});if(!pass)throw new Exception(name);};
  var paths=new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
  foreach(var path in paths)
  {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();var g=l.posterCutscene;
   check(g&&g.lesson==l&&g.posterAnchor&&g.posterSurface,path+": saved pointing controller and poster references");
   check(g.screenCanvas.renderMode==RenderMode.ScreenSpaceOverlay&&g.worldCanvas.renderMode==RenderMode.WorldSpace,path+": distinct screen and world canvas modes");
   check(!g.screenCanvas.gameObject.activeSelf&&!g.worldCanvas.gameObject.activeSelf,path+": cues are initially hidden");
   check(g.holdSeconds==1.5f&&g.pulseSeconds==1.5f&&Mathf.Abs(g.pulseGrowth-.2f)<.00001f,path+": exact requested hold and pulse settings");
   check(!g.ringGraphic.raycastTarget&&!g.arrow.GetComponent<UnityEngine.UI.Graphic>().raycastTarget,path+": guidance graphics do not block selection rays");
   check(g.instruction.font==l.view.font&&g.instruction.fontSize==22,path+": Korean guidance uses the existing readable font");
   check(l.presentation.space.overlay&&l.presentation.rescueTeam&&l.presentation.rescueTeam.specialist,path+": existing cinematic overlay and rescue team retained");
   check(Mathf.Abs(l.presentation.rescueTeam.specialist.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh.bounds.size.y-1.4784758681f)<.0001f,path+": latest B source proportions retained");
   check(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,path+": no missing scripts");
   check(!AssetDatabase.GetDependencies(path,true).Any(p=>p.Contains("CodexTemp")),path+": scene has no temporary runtime dependency");
   foreach(bool unanswered in new[]{false,true})
   {
    var f=new ElevatorFlow(l.settings){NoResponse=unanswered};f.Continue();f.Tick(l.settings.boardingSeconds+.1f);f.SelectDestination();f.Continue();
    check(f.Step==ElevatorStep.Proposal&&f.IsCinematic,path+": receiving ball starts story (unanswered="+unanswered+")");
    f.Tick(l.settings.shotStorySeconds+.1f);f.Tick(1000);f.Continue();
    check(f.AwaitingPoster&&f.JumpCount==0,path+": poster gate has no timeout or Continue bypass");
    f.PointAtSafetyPoster();f.Tick(l.settings.posterWarningSeconds+.1f);
    check(f.Step==ElevatorStep.Jump&&f.JumpCount==1&&f.FirstJudgment.Contains("뛰지 말라고"),path+": warning precedes the friend's one jump");
    f.Tick(l.settings.friendReplySeconds+l.settings.jumpSeconds+l.settings.afterJumpSeconds+.1f);f.Tick(2);f.RequestIntercom();f.Tick(l.settings.connectionSeconds+.1f);
    check(f.Step==(unanswered?ElevatorStep.Unanswered:ElevatorStep.Call),path+": emergency call routing still works");
    if(unanswered)f.RequestPhone();f.ReportSituation();f.FindIdentity();f.ReportLocation();f.FollowAdvice();f.Reassure();f.Tick(5);
    check(f.Step==ElevatorStep.Rescue&&f.Reassured&&f.LocationReported,path+": safe response still reaches the rescue team");
    f.Tick(l.settings.rescueSubtitleSeconds+.1f);f.Continue();f.Tick(l.settings.exitSeconds+.1f);f.Continue();f.Tick(4);f.Continue();f.Tick(7);
    check(f.Step==ElevatorStep.Complete,path+": full scenario still completes after the new opening");
   }
  }
  EditorSceneManager.OpenScene(paths[0],OpenSceneMode.Single);
  var report=new{status="PASS",checks};File.WriteAllText("Assets/M/CodexTemp/PosterCutscene_20261104~/SavedValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  return new{status="PASS",checks=checks.Count};
 }
}
