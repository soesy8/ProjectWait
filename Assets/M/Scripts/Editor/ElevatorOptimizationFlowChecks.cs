using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ProjectWait.M.Elevator;
public static class ElevatorOptimizationFlowChecks
{
 public static object Run()
 {
  var checks=new List<string>();Action<bool,string> check=(pass,label)=>{if(!pass)throw new Exception(label);checks.Add(label);};
  var settings=ScriptableObject.CreateInstance<ElevatorSettings>();
  try
  {
   foreach(bool phone in new[]{false,true})
   {
    var f=new ElevatorFlow(settings){NoResponse=phone};
    f.DeveloperJump(2);f.Tick(settings.shotStorySeconds+.01f);check(f.AwaitingPoster,"First poster prompt still waits for input");
    f.PointAtSafetyPoster();f.Tick(settings.posterWarningSeconds+.01f);check(f.Step==ElevatorStep.Jump&&f.JumpCount==1,"First warning retains shot sequence");
    f.Tick(settings.friendReplySeconds+settings.jumpSeconds+settings.afterJumpSeconds+.01f);f.Tick(2);f.RequestIntercom();f.Tick(settings.connectionSeconds+.01f);
    if(phone){check(f.Step==ElevatorStep.Unanswered,"Unanswered intercom fallback retained");f.RequestPhone();}
    f.Tick(settings.callQuestionSeconds+.01f);f.Tick(settings.callReplySeconds+.01f);f.Tick(settings.callQuestionSeconds+.01f);f.Tick(settings.callLocationGuideSeconds+.01f);
    check(f.SearchingIdentity&&!f.IsCinematic,"Finding identity restores object interaction");
    f.FindIdentity();check(f.Step==ElevatorStep.CallLocationResponse&&f.IdentityFound&&f.LocationReported&&f.IsCinematic,"Selecting identity resumes location cutscene");
    int rev=f.Revision;f.FindIdentity();f.ReportLocation();f.FollowAdvice();check(f.Revision==rev,"Duplicate/old dialogue actions cannot skip cutscene");
    f.Tick(settings.locationReportSeconds+.01f);check(f.Step==ElevatorStep.CallSafetyAdvice,"Location precedes waiting advice");
    f.Tick(settings.safetyAdviceSeconds+.01f);check(f.Step==ElevatorStep.DoorLean,"Friend approaches door after advice");
    f.Tick(settings.doorLeanSeconds+.01f);check(f.Step==ElevatorStep.DoorPosterPointing&&f.AwaitingPoster,"Second poster pointing is required");
    f.Tick(120);f.Continue();f.Reassure();check(f.Step==ElevatorStep.DoorPosterPointing&&!f.DoorWarningGiven&&!f.ReadyToExit,"No timeout or bypass before player points again");
    f.PointAtSafetyPoster();check(f.Step==ElevatorStep.DoorPosterWarning&&f.DoorWarningGiven&&f.JumpCount==1,"Second poster warns about doors without replaying jump");
    f.Tick(settings.doorWarningSeconds+.01f);check(f.Step==ElevatorStep.SafeWallMove,"Friend moves only after warning");
    f.Tick(settings.safeWallMoveSeconds+.01f);check(f.Step==ElevatorStep.SafeWallWaiting&&f.SafeWallReached&&f.Reassured,"Safe wall arrival records cooperative waiting");
    f.Tick(settings.safeWallSettleSeconds+.01f);check(f.Step==ElevatorStep.WaitingFade,"Safe wait leads into time transition");
    f.Tick(4.81f);check(f.Step==ElevatorStep.Rescue&&!f.ReadyToExit,"Rescue follows safe waiting");
    f.Tick(settings.rescueSubtitleSeconds+.01f);f.Continue();f.Tick(settings.exitSeconds+.01f);f.Continue();f.Tick(3.51f);f.Continue();f.Tick(6.01f);
    check(f.Step==ElevatorStep.Complete,"Both call routes still complete the rescue");
    check(f.History.Count(x=>x.Contains("다시 가리켜"))==1,"Door warning recorded once");
    f.Reset();check(!f.DoorWarningGiven&&!f.SafeWallReached&&!f.LocationReported&&f.Step==ElevatorStep.Lobby,"Restart clears added story state");
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(settings);}
  var report=new{status="PASS",checks};File.WriteAllText("Assets/M/Documentation/Optimization_20261105/FlowValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{status="PASS",checks=checks.Count};
 }
}
