using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyIntercomPolish
{
 const string Work="Assets/M/CodexTemp/IntercomAndCuePolish_20261104~/";
 static readonly List<object> checks=new List<object>();
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPosterCutscene g;static string scene;
 static void Check(bool pass,string name){checks.Add(new{pass,name});if(!pass)throw new Exception(name);}
 static async Task Until(Func<bool> ready,int milliseconds,string name)
 {
  float end=Time.realtimeSinceStartup+milliseconds/1000f;
  while(!ready()&&Time.realtimeSinceStartup<end)await Task.Delay(15);
  Check(ready(),name);
 }
 static Vector2 Angles(Vector3 target){var r=Quaternion.LookRotation(target-l.playerCamera.transform.position).eulerAngles;return new Vector2(r.y,Mathf.DeltaAngle(0,r.x));}
 static void Aim(Vector3 target){var a=Angles(target);c.SetLookAngles(a.x,a.y);}
 static void AtDistance(Vector3 target,float distance)
 {
  var a=Angles(target);float lo=0,hi=60;
  for(int i=0;i<26;i++)
  {
   float offset=(lo+hi)*.5f;c.SetLookAngles(a.x-offset,a.y);
   var v=l.playerCamera.WorldToViewportPoint(target);float d=new Vector2((v.x-.5f)*2,(v.y-.5f)*2).magnitude;
   if(d<distance)lo=offset;else hi=offset;
  }
 }
 static async Task Shot(string name){ScreenCapture.CaptureScreenshot(Work+scene+"_"+name+".png");await Task.Delay(100);}
 static string Caption()=>l.presentation.space.overlay.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="CinematicSubtitle").text;
 static async Task CheckArrow(Vector3 target,string label)
 {
  AtDistance(target,.8f);await Task.Delay(600);
  Check(g.ArrowAlpha>.99f&&g.ArrowTargetAlpha==1,label+": full arrow beyond 60 percent center distance");
  AtDistance(target,.45f);await Task.Delay(600);
  Check(Mathf.Abs(g.GuideDistance-.45f)<.004f&&Mathf.Abs(g.ArrowTargetAlpha-.5f)<.012f&&Mathf.Abs(g.ArrowAlpha-.5f)<.02f,label+": half opacity at 45 percent center distance");
  await Awaitable.EndOfFrameAsync();
  Check(Mathf.Abs(g.arrow.GetComponent<CanvasRenderer>().GetInheritedAlpha()-.5f)<.025f,label+": actual renderer inherits interpolated opacity");
  await Shot(label+"_ArrowHalf");
  AtDistance(target,.2f);await Task.Delay(50);
  Check(g.ArrowAlpha>0&&g.ArrowAlpha<.5f,label+": alpha transitions gradually instead of toggling off");
  await Task.Delay(600);Check(g.ArrowAlpha==0&&!g.arrow.gameObject.activeSelf,label+": arrow disappears within 30 percent center distance");
 }
 public static async Task<object> Run()
 {
  checks.Clear();l=Object.FindFirstObjectByType<ElevatorLesson>();c=Object.FindFirstObjectByType<ElevatorDesktopControls>();g=l.posterCutscene;scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  var errors=new List<string>();string failure=null;float storyDuration=0,warningDuration=0,ringDuration=0;
  bool le=l.enabled,noResponse=l.simulateNoResponse;
  var settings=InputSystem.settings;var temp=Object.Instantiate(settings);temp.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  var real=Mouse.current;bool re=real!=null&&real.enabled;Mouse mouse=null;
  Application.LogCallback monitor=(message,stack,type)=>{if((type==LogType.Error||type==LogType.Exception)&&errors.Count<12)errors.Add(message);};
  try
  {
   Check(Application.isPlaying&&c.IsDesktop,"Real desktop Play session");Application.logMessageReceived+=monitor;
   InputSystem.settings=temp;if(re)InputSystem.DisableDevice(real);mouse=InputSystem.AddDevice<Mouse>("IntercomCueVerificationMouse");l.simulateNoResponse=false;
   int frame=Time.frameCount;await Task.Delay(250);Check(Time.frameCount>frame+1,"Player loop is advancing");
   Check(l.settings.shotStorySeconds==1.5f&&l.settings.posterWarningSeconds==1.5f&&l.settings.friendReplySeconds==1.5f,"All three pre-fault dialogue beats use 1.5 seconds");
   l.DeveloperJump(2);c.SetLookAngles(180,-5);float start=Time.realtimeSinceStartup;
   await Until(()=>l.Flow.AwaitingPoster,2100,"Short friend dialogue reaches the pointing gate");storyDuration=Time.realtimeSinceStartup-start;
   Check(storyDuration>1.40f&&storyDuration<1.85f,"Friend story lasts approximately 1.5 seconds");
   await CheckArrow(g.TargetPosition,"Poster");
   Check(l.Flow.AwaitingPoster&&!l.Flow.IdentityFound,"Distance-based cue opacity does not advance the story");
   Aim(g.TargetPosition);await Until(()=>l.Flow.Step==ElevatorStep.PosterWarning,2300,"The completed 1.5 second hold resumes the cutscene");
   float filled=Time.realtimeSinceStartup;
   Check(g.RingFading&&g.RingAlpha>.85f&&g.worldCanvas.gameObject.activeSelf&&g.ringGraphic.progress==1,"Completed circle remains visible and full at the start of its fade (alpha="+g.RingAlpha+", progress="+g.ringGraphic.progress+", fading="+g.RingFading+")");
   await Until(()=>g.RingAlpha<.60f&&g.RingAlpha>.35f,750,"Circle reaches partial opacity during its fade");
   Check(l.Flow.Step==ElevatorStep.PosterWarning,"Circle fades while the player's next dialogue is already playing");
   await Shot("CircleHalfFade");
   await Until(()=>!g.RingFading&&!g.worldCanvas.gameObject.activeSelf,750,"Circle disappears smoothly after one second");ringDuration=Time.realtimeSinceStartup-filled;
   Check(ringDuration>.90f&&ringDuration<1.20f,"Circle fade duration is approximately one second");
   await Until(()=>l.Flow.Step==ElevatorStep.Jump,1000,"The shortened player warning reaches the friend reply");warningDuration=Time.realtimeSinceStartup-filled;
   Check(warningDuration>1.35f&&warningDuration<1.8f,"Player warning lasts approximately 1.5 seconds");
   l.enabled=false;l.Advance(0);await CheckArrow(l.friend.position+Vector3.up*1.05f,"Friend");
   Aim(l.friend.position+Vector3.up*1.05f);l.enabled=true;
   await Until(()=>l.Flow.Step==ElevatorStep.Jump&&l.Flow.Elapsed>=1.75f,2500,"Short friend reply still leads into the airborne shot");
   await Awaitable.EndOfFrameAsync();Check(l.friend.position.y-l.presentation.space.sportsCentre.friendInside.y>.12f,"Friend still visibly jumps");
   await Until(()=>l.Flow.Step==ElevatorStep.Stopped,2200,"The shot leads to the fault");
   await Until(()=>l.Flow.CanObserve&&!l.CinematicBlocking,3000,"Emergency investigation unlocks after the fault reaction");
   var intercom=l.CollectObservations().First(o=>o.clue==ElevatorClue.Intercom&&o.isActiveAndEnabled);
   var identity=l.CollectObservations().First(o=>o.clue==ElevatorClue.Identity&&o.isActiveAndEnabled);
   intercom.Activate();Check(l.Flow.Step==ElevatorStep.Connecting,"Activating the emergency intercom starts connection");
   var gaze=c.LookAngles;
   await Until(()=>l.Flow.Step==ElevatorStep.CallQuestion,4000,"Manager connects and asks what happened");await Task.Delay(35);
   Check(Caption().Contains("무슨 상황")&&l.Flow.IsCallCutscene&&l.CinematicBlocking,"Manager's first question is a letterboxed cinematic caption");
   identity.Activate();l.Flow.FindIdentity();Check(!l.Flow.IdentityFound&&!identity.CanActivate&&l.view.ChoiceButtons.Count==0,"The clue cannot be found or skipped during the call cinematic");
   await Shot("Call_01_Question");
   await Until(()=>l.Flow.Step==ElevatorStep.CallResponse,2200,"Player answers the manager automatically");await Task.Delay(35);
   Check(l.Flow.SituationReported&&l.Flow.HelpRequested&&Caption().Contains("고장 나서 움직이지 않아요"),"Player explicitly reports the fault and inability to move");
   await Shot("Call_02_PlayerResponse");
   await Until(()=>l.Flow.Step==ElevatorStep.CallLiftQuestion,2600,"Manager asks which elevator");await Task.Delay(35);
   Check(Caption().Contains("어느 엘리베이터"),"The specific-elevator question is presented before clue guidance");
   await Shot("Call_03_WhichLift");
   await Until(()=>l.Flow.Step==ElevatorStep.CallIdentityGuide,2200,"Manager explains where the location is written");await Task.Delay(35);
   Check(Caption().Contains("전면 비상벨 아래 식별판")&&Caption().Contains("승강기 번호"),"Manager names the exact panel and the information on it");
   identity.Activate();l.Flow.FindIdentity();Check(!l.Flow.IdentityFound&&l.CinematicBlocking,"Location instructions still block premature clue completion");
   await Shot("Call_04_LocationGuide");
   await Until(()=>l.Flow.SearchingIdentity&&!l.CinematicBlocking,3500,"Call cinematic ends and direct clue investigation unlocks");
   Check(!l.Flow.IdentityFound&&l.Flow.CanObserve&&c.movement.CanMove&&identity.CanActivate,"The player must find the clue after regaining control");
   Check(Vector2.Distance(c.LookAngles,gaze)<.001f,"The entire call preserves the player's gaze");
   Check(l.presentation.space.overlay.Amount<.016f&&l.view.ChoiceButtons.Count==0,"Letterbox leaves and no Next button can substitute for finding the clue");
   var other=l.CollectObservations().First(o=>o.clue==ElevatorClue.Display&&o.isActiveAndEnabled);other.Activate();Check(!l.Flow.IdentityFound,"Inspecting an unrelated object does not solve the clue");
   await Shot("Call_05_FindClue");
   Aim(identity.GetComponent<Collider>().bounds.center);await Task.Delay(150);
   Check(c.HoverTargetName==identity.name,"The actual central PC ray can target the identity plate");
   c.SelectCenterTarget();await Task.Delay(80);Check(l.Flow.IdentityFound&&!l.Flow.LocationReported,"Selecting the identity plate reveals its location before reporting it");
   await Shot("Call_06_ClueFound");
   for(int i=0;i<4&&!l.Flow.LocationReported;i++){l.view.InvokeChoice(0);await Task.Delay(100);}
   Check(l.Flow.LocationReported,"Location reporting remains available after the clue is found");
   for(int i=0;i<4&&l.Flow.Step==ElevatorStep.Call;i++){l.view.InvokeChoice(0);await Task.Delay(100);}
   Check(l.Flow.Step==ElevatorStep.Waiting,"The revised call still leads into reassuring and waiting with the friend");
   Check(errors.Count==0,"No new runtime exceptions or rendering errors");
  }
  catch(Exception ex){failure=ex.ToString();}
  finally
  {
   Application.logMessageReceived-=monitor;if(l.pauseMenu.IsPaused)l.pauseMenu.Resume();l.enabled=le;l.simulateNoResponse=noResponse;l.Restart();
   if(mouse!=null)InputSystem.RemoveDevice(mouse);InputSystem.settings=settings;if(re)InputSystem.EnableDevice(real);if(real!=null)real.MakeCurrent();Object.Destroy(temp);
  }
  var report=new{scene,status=failure==null?"PASS":"FAIL",checks,storyDuration,warningDuration,ringDuration,errors,failure};
  File.WriteAllText(Work+scene+"_PlayValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
