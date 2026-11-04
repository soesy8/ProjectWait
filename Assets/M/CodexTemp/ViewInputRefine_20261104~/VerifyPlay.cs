using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyViewInput
{
 const string Work="Assets/M/CodexTemp/ViewInputRefine_20261104~/";
 static readonly List<object> checks=new List<object>();
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPosterCutscene g;
 static Mouse mouse;static string scene;
 static void Check(bool pass,string name){checks.Add(new{pass,name});if(!pass)throw new Exception(name);}
 static async Task Until(Func<bool> ready,int milliseconds,string name)
 {
  float end=Time.realtimeSinceStartup+milliseconds/1000f;
  while(!ready()&&Time.realtimeSinceStartup<end)await Task.Delay(20);
  Check(ready(),name);
 }
 static async Task MoveMouse(Vector2 delta)
 {
  InputSystem.QueueStateEvent(mouse,new MouseState{delta=delta});await Task.Delay(80);
  InputSystem.QueueStateEvent(mouse,new MouseState());await Task.Delay(80);
 }
 static Vector2 AimAngles(Vector3 target)
 {var r=Quaternion.LookRotation(target-l.playerCamera.transform.position).eulerAngles;return new Vector2(r.y,Mathf.DeltaAngle(0,r.x));}
 static void Aim(Vector3 target){var a=AimAngles(target);c.SetLookAngles(a.x,a.y);}
 static void AtX(Vector3 target,float desired)
 {
  var a=AimAngles(target);float lo=a.x-65,hi=a.x+65;
  for(int i=0;i<28;i++)
  {
   float yaw=(lo+hi)*.5f;c.SetLookAngles(yaw,a.y);
   float x=l.playerCamera.WorldToViewportPoint(target).x;
   if(x>desired)lo=yaw;else hi=yaw;
  }
  g.Advance(0);
 }
 static async Task Shot(string name)
 {ScreenCapture.CaptureScreenshot(Work+scene+"_"+name+".png");await Task.Delay(120);}
 static void CheckCenterEdges(Vector3 target,string label)
 {
  foreach(float x in new[]{.31f,.69f,.29f,.71f})
  {
   AtX(target,x);var v=l.playerCamera.WorldToViewportPoint(target);bool inside=x>.3f&&x<.7f;
   Check(Mathf.Abs(v.x-x)<.002f&&g.GuideTargetCentered==inside&&g.arrow.gameObject.activeSelf!=inside,label+": arrow visibility at viewport x="+x);
  }
  // Test the vertical limits through actual camera projection as well.
  var cam=l.playerCamera;
  Check(g.IsInAttentionArea(cam.ViewportToWorldPoint(new Vector3(.5f,.31f,3)))&&g.IsInAttentionArea(cam.ViewportToWorldPoint(new Vector3(.5f,.69f,3)))&&!g.IsInAttentionArea(cam.ViewportToWorldPoint(new Vector3(.5f,.29f,3)))&&!g.IsInAttentionArea(cam.ViewportToWorldPoint(new Vector3(.5f,.71f,3))),label+": vertical central 40 percent boundaries");
  Check(!g.IsInAttentionArea(cam.transform.position-cam.transform.forward*3),label+": target behind the player never counts as centered");
 }
 public static async Task<object> Run()
 {
  checks.Clear();l=Object.FindFirstObjectByType<ElevatorLesson>();c=Object.FindFirstObjectByType<ElevatorDesktopControls>();g=l.posterCutscene;
  scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  string failure=null;var errors=new List<string>();
  bool lessonEnabled=l.enabled,guideEnabled=g.enabled;
  var source=InputSystem.settings;var temp=Object.Instantiate(source);temp.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  var real=Mouse.current;bool realEnabled=real!=null&&real.enabled;
  Application.LogCallback monitor=(message,stack,type)=>{if((type==LogType.Error||type==LogType.Exception)&&errors.Count<12)errors.Add(message);};
  Vector2 normalDelta=Vector2.zero,cinematicDelta=Vector2.zero,scriptOnly=Vector2.zero,withInput=Vector2.zero;
  try
  {
   Check(Application.isPlaying&&c.IsDesktop&&g,"Desktop scene initialized");
   InputSystem.settings=temp;if(realEnabled)InputSystem.DisableDevice(real);mouse=InputSystem.AddDevice<Mouse>("ViewInputVerificationMouse");Application.logMessageReceived+=monitor;
   int frame=Time.frameCount;await Task.Delay(250);Check(Time.frameCount>frame+1,"Actual player loop advances");
   l.DeveloperJump(1);c.SetLookAngles(140,13);await Task.Delay(150);var start=c.LookAngles;
   await MoveMouse(new Vector2(90,-40));normalDelta=c.LookAngles-start;
   Check(Vector2.Distance(normalDelta,new Vector2(90,40)*c.sensitivity)<.1f,"Exploration mouse movement uses the configured sensitivity");
   l.DeveloperJump(2);c.SetLookAngles(140,13);await Task.Delay(150);start=c.LookAngles;
   await MoveMouse(new Vector2(90,-40));cinematicDelta=c.LookAngles-start;
   Check(Vector2.Distance(cinematicDelta,normalDelta)<.1f,"The same mouse input has full strength during the letterboxed cinematic");

   l.enabled=false;l.Restart();l.Continue();c.SetLookAngles(23,9);l.Advance(3.8f);l.Advance(1.0f);l.Advance(1.5f);scriptOnly=c.LookAngles;
   Check(Mathf.Abs(scriptOnly.x-203)<.01f&&Mathf.Abs(scriptOnly.y-9)<.01f,"Boarding adds exactly 180 degrees and preserves existing pitch");
   l.Restart();l.Continue();c.SetLookAngles(23,9);l.Advance(3.8f);await Task.Delay(100);
   var before=c.LookAngles;await MoveMouse(new Vector2(120,-60));var first=c.LookAngles-before;
   l.Advance(.5f);before=c.LookAngles;await MoveMouse(new Vector2(-30,20));var second=c.LookAngles-before;
   l.Advance(2.0f);withInput=c.LookAngles;
   Check(Vector2.Distance(first,new Vector2(120,60)*c.sensitivity)<.1f&&Vector2.Distance(second,new Vector2(-30,-20)*c.sensitivity)<.1f,"Actual mouse deltas remain fully responsive during the scripted turn");
   Check(Vector2.Distance(withInput,scriptOnly+first+second)<.02f,"Scripted rotation and both player look offsets accumulate independently");
   var settled=c.LookAngles;for(int i=0;i<5;i++)l.Advance(0);
   Check(Vector2.Distance(settled,c.LookAngles)<.001f,"Completing the turn does not apply it again or recenter gaze");
   await MoveMouse(new Vector2((180-c.LookAngles.x)/c.sensitivity,c.LookAngles.y/c.sensitivity));
   Check(Mathf.Abs(c.LookAngles.x-180)<.1f&&Mathf.Abs(c.LookAngles.y)<.1f,"Player can undo their own offset to face forward after the cinematic turn");
   l.Restart();l.Continue();c.SetLookAngles(341,-21);l.Advance(l.settings.boardingSeconds+.1f);
   Check(Mathf.Abs(c.LookAngles.x-521)<.01f&&Mathf.Abs(c.LookAngles.y+21)<.01f,"A skipped final turn frame still adds fixed positive 180 degrees across yaw wrap");

   l.DeveloperJump(2);l.Advance(l.settings.shotStorySeconds+.1f);l.enabled=true;g.enabled=false;c.SetLookAngles(180,-5);g.Advance(0);l.presentation.space.overlay.Advance(10);
   Check(l.Flow.AwaitingPoster&&l.presentation.space.overlay.Amount>.9f,"Poster pointing remains in the letterboxed cutscene");
   CheckCenterEdges(g.TargetPosition,"Poster");
   AtX(g.TargetPosition,.60f);
   Check(g.GuideTargetCentered&&!g.arrow.gameObject.activeSelf&&g.worldCanvas.gameObject.activeSelf&&g.instructionPanel.gameObject.activeSelf,"Poster arrow hides inside the central area while its circle and pointing instruction remain");
   Check(!g.HitsTarget(l.playerCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out _),"Hiding the poster arrow does not bypass the exact pointing target");
   await Shot("PosterCentered_NoArrow");
   AtX(g.TargetPosition,.76f);Check(g.arrow.gameObject.activeSelf,"Poster arrow returns when the target leaves the central area");await Shot("PosterOutside_Arrow");
   Aim(g.TargetPosition);g.enabled=true;float began=Time.realtimeSinceStartup;
   await Until(()=>l.Flow.Step==ElevatorStep.PosterWarning,2400,"Continuous pointing still resumes the cutscene");
   Check(Time.realtimeSinceStartup-began>=1.40f,"The existing 1.5 second pointing requirement is retained");
   await Until(()=>l.Flow.Step==ElevatorStep.Jump,4000,"Player warning still leads into the friend's shot");
   l.enabled=false;l.Advance(0);g.enabled=false;
   Vector3 friend=l.friend.position+Vector3.up*1.05f;
   CheckCenterEdges(friend,"Friend");
   AtX(friend,.60f);
   Check(!g.arrow.gameObject.activeSelf&&!g.instructionPanel.gameObject.activeSelf,"Friend inside the central area proceeds without arrow or watch instruction");
   await Shot("FriendCentered_NoArrow");
   AtX(friend,.76f);Check(g.arrow.gameObject.activeSelf&&g.instructionPanel.gameObject.activeSelf,"Friend guidance returns outside the central area");await Shot("FriendOutside_Arrow");
   Aim(friend);l.enabled=true;g.enabled=true;
   await Until(()=>l.Flow.Step==ElevatorStep.Stopped,6500,"The shot and fault continue normally without a centered arrow");
   Check(l.Flow.JumpCount==1,"The new viewing rules preserve a single friend jump");
   Check(errors.Count==0,"No new runtime or rendering errors");
  }
  catch(Exception ex){failure=ex.ToString();}
  finally
  {
   Application.logMessageReceived-=monitor;if(l.pauseMenu.IsPaused)l.pauseMenu.Resume();
   l.enabled=lessonEnabled;g.enabled=guideEnabled;l.Restart();
   if(mouse!=null)InputSystem.RemoveDevice(mouse);InputSystem.settings=source;
   if(realEnabled)InputSystem.EnableDevice(real);if(real!=null)real.MakeCurrent();Object.Destroy(temp);
  }
  var report=new{scene,status=failure==null?"PASS":"FAIL",checks,normalDelta=new[]{normalDelta.x,normalDelta.y},cinematicDelta=new[]{cinematicDelta.x,cinematicDelta.y},scriptOnly=new[]{scriptOnly.x,scriptOnly.y},withInput=new[]{withInput.x,withInput.y},errors,failure};
  File.WriteAllText(Work+scene+"_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
