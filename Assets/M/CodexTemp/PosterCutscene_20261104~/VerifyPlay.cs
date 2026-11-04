using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyPosterPlay
{
 const string Work="Assets/M/CodexTemp/PosterCutscene_20261104~/";
 static readonly List<object> checks=new List<object>();
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPosterCutscene g;
 static string scene;static Mouse mouse;static Keyboard keyboard;
 static void Check(bool pass,string name){checks.Add(new{pass,name});if(!pass)throw new Exception(name);}
 static async Task Until(Func<bool> ready,int milliseconds,string name)
 {
  var end=Time.realtimeSinceStartup+milliseconds/1000f;
  while(!ready()&&Time.realtimeSinceStartup<end)await Task.Delay(25);
  Check(ready(),name);
 }
 static void Aim(Vector3 point)
 {
  var rotation=Quaternion.LookRotation(point-l.playerCamera.transform.position).eulerAngles;
  c.SetLookAngles(rotation.y,Mathf.DeltaAngle(0,rotation.x));
 }
 static async Task Shot(string name)
 {
  ScreenCapture.CaptureScreenshot(Work+scene+"_"+name+".png");await Task.Delay(120);
 }
 static async Task Input(Vector2 delta,params Key[] keys)
 {
  InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(4,4),delta=delta}.WithButton(MouseButton.Left,true));
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));await Task.Delay(100);
  InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);
 }
 public static async Task<object> Run()
 {
  checks.Clear();l=Object.FindFirstObjectByType<ElevatorLesson>();c=Object.FindFirstObjectByType<ElevatorDesktopControls>();g=l.posterCutscene;
  scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  var errors=new List<string>();string failure=null;float measuredHold=0,pulseMin=9,pulseMax=0;
  Application.LogCallback monitor=(message,stack,type)=>{if((type==LogType.Error||type==LogType.Exception)&&errors.Count<10)errors.Add(message);};
  var sourceSettings=InputSystem.settings;var temp=Object.Instantiate(sourceSettings);temp.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  var realMouse=Mouse.current;var realKeyboard=Keyboard.current;bool rm=realMouse!=null&&realMouse.enabled,rk=realKeyboard!=null&&realKeyboard.enabled;
  try
  {
   Check(Application.isPlaying&&l.Flow!=null&&c.IsDesktop,"Real desktop Play session initialized");
   Application.logMessageReceived+=monitor;InputSystem.settings=temp;
   if(rm)InputSystem.DisableDevice(realMouse);if(rk)InputSystem.DisableDevice(realKeyboard);
   mouse=InputSystem.AddDevice<Mouse>("PosterVerificationMouse");keyboard=InputSystem.AddDevice<Keyboard>("PosterVerificationKeyboard");
   int frame=Time.frameCount;await Task.Delay(250);Check(Time.frameCount>frame+1,"Player loop advances between observations");
   l.DeveloperJump(1);l.SelectFloor();Aim(l.friend.position+Vector3.up*1.0f);await Task.Delay(150);
   var ball=l.CollectObservations().First(o=>o.clue==ElevatorClue.Ball&&o.isActiveAndEnabled);
   ball.Activate();await Task.Delay(550);
   Check(l.Flow.Step==ElevatorStep.Proposal&&l.Flow.IsCinematic,"Receiving the basketball immediately starts the cutscene");
   Check(l.Flow.PlayerHasBall&&l.view.ChoiceButtons.Count==0,"The ball is received and the old shot choices are removed");
   Check(l.presentation.space.overlay.Amount>.99f&&!c.centerLaser.enabled,"Story starts with full letterbox and no gameplay laser");
   await Shot("01_Story");
   await Until(()=>l.Flow.AwaitingPoster,7000,"Friend story reaches the poster pointing gate");
   await Task.Delay(150);
   Check(l.Flow.IsCinematic&&l.CinematicBlocking&&!c.movement.CanMove,"Pointing gate keeps letterbox and movement lock");
   Check(g.screenCanvas.renderMode==RenderMode.ScreenSpaceOverlay&&g.screenCanvas.gameObject.activeInHierarchy,"Directional arrow uses a visible screen space canvas");
   Check(g.worldCanvas.renderMode==RenderMode.WorldSpace&&g.worldCanvas.gameObject.activeInHierarchy,"Poster circle uses a visible world space canvas");
   Check(g.ringGraphic.canvasRenderer&&g.ringGraphic.canvasRenderer.GetMesh().vertexCount>100&&g.arrow.GetComponent<CanvasRenderer>()&&g.arrow.GetComponent<CanvasRenderer>().GetMesh().vertexCount>10,"Circle and arrow contain actual rendered UI mesh geometry");
   Check(c.centerLaser.enabled&&c.reticle.gameObject.activeInHierarchy,"Central PC laser remains available during the pointing gate");
   Check(g.holdSeconds==1.5f&&g.pulseSeconds==1.5f&&Mathf.Abs(g.pulseGrowth-.2f)<.0001f,"Hold duration and breathing cycle use the requested values");
   Check(Vector3.Dot(g.worldRing.forward,(g.TargetPosition-l.playerCamera.transform.position).normalized)>.999f,"World circle faces the player");
   c.SetLookAngles(0,0);await Task.Delay(100);
   Check(g.ArrowClamped,"Offscreen poster still has a direction arrow");
   Vector2 look=c.LookAngles;Vector3 feet=c.movement.FeetPosition;int run=l.RunVersion,revision=l.Flow.Revision,clicks=c.ClickCount;
   await Input(new Vector2(120,-50),Key.W,Key.D,Key.Digit1,Key.R,Key.F8);
   Check(Vector2.Distance(look,c.LookAngles)>.5f,"Actual mouse input freely adjusts the cinematic viewpoint");
   Check(l.RunVersion==run&&l.Flow.Revision==revision&&c.ClickCount==clicks,"Gameplay clicks and shortcuts cannot bypass the pointing gate");
   Check(Vector3.Distance(feet,c.movement.FeetPosition)<.001f,"Attempted WASD input cannot walk out of the cinematic");
   c.SetLookAngles(180,-5);
   float end=Time.realtimeSinceStartup+1.65f;
   while(Time.realtimeSinceStartup<end){pulseMin=Mathf.Min(pulseMin,g.PulseScale);pulseMax=Mathf.Max(pulseMax,g.PulseScale);await Task.Delay(20);}
   Check(pulseMin<1.003f&&pulseMax>1.197f&&pulseMax<=1.201f,"The circle grows by 20 percent and shrinks over a full cycle");
   Check(l.Flow.AwaitingPoster&&l.Flow.JumpCount==0&&g.HoldElapsed==0,"Waiting without pointing never auto-resumes the cutscene");
   await Shot("02_FindPoster");
   Aim(g.TargetPosition);await Until(()=>g.HoldElapsed>.32f,2000,"Center ray on the circle begins hold progress");
   Check(g.HitsTarget(l.playerCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out _),"The actual PC ray reaches the poster target without occlusion");
   await Shot("03_Pointing");
   c.SetLookAngles(180,-5);await Task.Delay(120);
   Check(g.HoldElapsed==0&&g.ringGraphic.progress==0,"Looking away resets continuous hold progress");
   Aim(g.TargetPosition);await Until(()=>g.HoldElapsed>.32f,2000,"A fresh hold starts after looking back");
   l.pauseMenu.Pause();float held=g.HoldElapsed,elapsed=l.Flow.Elapsed;await Task.Delay(600);
   Check(Mathf.Abs(g.HoldElapsed-held)<.001f&&Mathf.Abs(l.Flow.Elapsed-elapsed)<.001f,"Pause freezes both hold progress and story time");
   Check(!g.screenCanvas.gameObject.activeSelf&&!g.worldCanvas.gameObject.activeSelf,"Poster cues hide behind the pause menu");
   l.pauseMenu.Resume();c.SetLookAngles(180,-5);await Task.Delay(120);
   Check(g.HoldElapsed==0,"A new aim after pause starts from zero");
   Aim(g.TargetPosition);var retained=c.LookAngles;float start=Time.realtimeSinceStartup;
   await Until(()=>l.Flow.Step==ElevatorStep.PosterWarning,2300,"Holding the central ray resumes the cinematic automatically");
   measuredHold=Time.realtimeSinceStartup-start;
   Check(measuredHold>=1.40f&&measuredHold<1.85f,"Resume occurs after approximately 1.5 seconds of continuous aim");
   Check(l.Flow.FirstJudgment.Contains("뛰지 말라고")&&l.Flow.JumpCount==0,"Player warns against jumping before the friend acts");
   Check(!g.screenCanvas.gameObject.activeSelf&&!g.worldCanvas.gameObject.activeSelf,"Pointing cues retire when the warning begins");
   Check(l.presentation.space.overlay.Amount>.99f&&Vector2.Distance(retained,c.LookAngles)<.001f,"Letterbox and player gaze remain unchanged when the cutscene resumes");
   await Shot("04_PlayerWarning");
   await Until(()=>l.Flow.Step==ElevatorStep.Jump,4000,"The friend ignores the warning and begins the shot sequence");
   Check(l.Flow.JumpCount==1,"Only the friend performs one jump");
   await Task.Delay(50);Check(g.screenCanvas.gameObject.activeSelf&&!g.worldCanvas.gameObject.activeSelf,"Brief friend direction cue preserves free camera control");
   Aim(l.friend.position+Vector3.up*1.05f);
   await Until(()=>l.Flow.Step==ElevatorStep.Jump&&l.Flow.Elapsed>=l.settings.friendReplySeconds+.28f,5000,"Shot reaches its airborne moment");
   await Awaitable.EndOfFrameAsync();
   Check(l.friend.position.y-l.presentation.space.sportsCentre.friendInside.y>.15f,"Friend visibly leaves the floor while miming the shot");
   Check(Vector3.Distance(feet,c.movement.FeetPosition)<.001f,"The player's body remains on the floor throughout the shot");
   await Shot("05_FriendJump");
   await Until(()=>l.Flow.Step==ElevatorStep.Stopped,3000,"The shot is followed by the elevator fault");
   await Task.Delay(60);
   var effects=l.GetComponent<ElevatorCameraEffects>();
   Check(effects.LastShakeAmplitude>.1f,"The fault plays the existing mechanical shake feedback");
   Check(l.Flow.IsShotCutscene&&l.view.GetComponent<CanvasGroup>().alpha==0&&l.presentation.space.overlay.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.name=="CinematicSubtitle"&&t.text.Contains("왜 멈췄지")),"Fault reaction stays readable in the letterbox caption");
   await Shot("06_Fault");
   await Until(()=>l.Flow.CanObserve&&!l.CinematicBlocking,3000,"Fault reaction releases into the emergency investigation");
   Check(l.CanInteract(ElevatorClue.Intercom)&&!g.worldCanvas.gameObject.activeSelf&&!g.screenCanvas.gameObject.activeSelf,"Normal emergency interaction resumes with no leftover poster UI");
   l.Restart();await Task.Delay(120);
   Check(l.Flow.Step==ElevatorStep.Lobby&&g.HoldElapsed==0&&!g.worldCanvas.gameObject.activeSelf&&!g.screenCanvas.gameObject.activeSelf,"Restart clears the new cinematic gate and cues");
   Check(errors.Count==0,"No new runtime exceptions or rendering errors");
  }
  catch(Exception ex){failure=ex.ToString();}
  finally
  {
   Application.logMessageReceived-=monitor;
   if(l.pauseMenu.IsPaused)l.pauseMenu.Resume();l.Restart();
   if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
   InputSystem.settings=sourceSettings;
   if(rm)InputSystem.EnableDevice(realMouse);if(rk)InputSystem.EnableDevice(realKeyboard);
   if(realMouse!=null)realMouse.MakeCurrent();if(realKeyboard!=null)realKeyboard.MakeCurrent();Object.Destroy(temp);
  }
  var report=new{scene,status=failure==null?"PASS":"FAIL",checks,measuredHold,pulseMin,pulseMax,errors,failure};
  File.WriteAllText(Work+scene+"_PlayValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
