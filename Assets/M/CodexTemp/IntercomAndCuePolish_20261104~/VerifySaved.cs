using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifySavedIntercom
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved changes");
  var checks=new List<object>();Action<bool,string> check=(pass,name)=>{checks.Add(new{pass,name});if(!pass)throw new Exception(name);};
  var paths=new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
  foreach(var path in paths)
  {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();var g=l.posterCutscene;
   check(g&&g.arrowGroup&&g.ringGroup&&g.instructionGroup,path+": saved opacity groups and guide references");
   check(g.arrowGroup.alpha==0&&g.ringGroup.alpha==0&&!g.screenCanvas.gameObject.activeSelf&&!g.worldCanvas.gameObject.activeSelf,path+": cues saved hidden");
   check(!g.arrowGroup.blocksRaycasts&&!g.ringGroup.blocksRaycasts&&!g.instructionGroup.blocksRaycasts,path+": fading cues do not block input");
   check(l.settings.shotStorySeconds==1.5f&&l.settings.posterWarningSeconds==1.5f&&l.settings.friendReplySeconds==1.5f,path+": shortened pre-fault dialogue saved");
   check(l.settings.callQuestionSeconds==1.5f&&l.settings.callReplySeconds==2&&l.settings.callLocationGuideSeconds==2.5f,path+": call timing saved");
   check(l.settings.reportDialogue.Contains("고장 나서 움직이지 않아요"),path+": requested player response saved");
   check(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,path+": no missing scripts");
   check(!AssetDatabase.GetDependencies(path,true).Any(p=>p.Contains("CodexTemp")),path+": no temporary runtime dependencies");
   foreach(bool unanswered in new[]{false,true})
   {
    var f=new ElevatorFlow(l.settings){NoResponse=unanswered};f.DeveloperJump(3);f.Tick(2);f.RequestIntercom();f.Tick(l.settings.connectionSeconds+.1f);
    if(unanswered){check(f.Step==ElevatorStep.Unanswered,path+": unanswered intercom branch retained");f.RequestPhone();}
    check(f.Step==ElevatorStep.CallQuestion&&f.IsCinematic,path+": connection opens situation question (phone="+unanswered+")");
    f.FindIdentity();check(!f.IdentityFound,path+": identity cannot be found during the first question");
    f.Tick(l.settings.callQuestionSeconds+.1f);check(f.Step==ElevatorStep.CallResponse&&f.SituationReported&&f.HelpRequested,path+": player reports fault and occupancy");
    f.Tick(l.settings.callReplySeconds+.1f);check(f.Step==ElevatorStep.CallLiftQuestion,path+": caller asks which lift after hearing the situation");
    f.Tick(l.settings.callQuestionSeconds+.1f);f.FindIdentity();check(f.Step==ElevatorStep.CallIdentityGuide&&f.IsCinematic&&!f.IdentityFound,path+": exact plate instructions remain cinematic");
    f.Tick(l.settings.callLocationGuideSeconds+.1f);f.Tick(100);f.ReportLocation();
    check(f.SearchingIdentity&&!f.IsCinematic&&!f.IdentityFound&&!f.LocationReported,path+": clue requires investigation and has no auto-completion");
    f.FindIdentity();f.ReportLocation();f.FollowAdvice();f.Reassure();f.Tick(5);
    check(f.Step==ElevatorStep.Rescue&&f.LocationReported&&f.Reassured,path+": found clue still leads to rescue");
    f.Tick(l.settings.rescueSubtitleSeconds+.1f);f.Continue();f.Tick(l.settings.exitSeconds+.1f);f.Continue();f.Tick(4);f.Continue();f.Tick(7);
    check(f.Step==ElevatorStep.Complete,path+": complete scenario remains reachable");
   }
  }
  EditorSceneManager.OpenScene(paths[0],OpenSceneMode.Single);
  File.WriteAllText("Assets/M/CodexTemp/IntercomAndCuePolish_20261104~/SavedValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="PASS",checks},Newtonsoft.Json.Formatting.Indented));
  return new{status="PASS",checks=checks.Count};
 }
}
