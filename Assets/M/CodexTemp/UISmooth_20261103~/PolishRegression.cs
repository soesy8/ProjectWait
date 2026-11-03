using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class VerifyInteractionPolish
{
 const string Dir="Assets/M/CodexTemp/UISmooth_20261103~/";
 static readonly List<object> checks=new List<object>();
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPlayerMovement m;static ElevatorScenarioDirector s;
 static ElevatorSportsCentreMap map;static string scene;
 static void Check(bool pass,string name){checks.Add(new{name,passed=pass});if(!pass)throw new Exception(name);}
 static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
 static float Angle(Vector2 a,Vector2 b)=>Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(a.x,b.x)),Mathf.Abs(a.y-b.y));
 static void Settle(){l.presentation.space.overlay.Advance(10);s.AdvanceGuidance(0);}
 static void PlaceCamera(Vector3 position){m.Controller.enabled=false;c.xrOrigin.position+=position-l.playerCamera.transform.position;l.playerCamera.transform.position=position;m.Controller.enabled=true;m.ResetVerticalMotion();Physics.SyncTransforms();}
 static void Aim(Vector3 point){var a=Quaternion.LookRotation(point-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));Physics.SyncTransforms();}
 static async Task Shot(string name){ScreenCapture.CaptureScreenshot(Dir+scene+"_"+name+".png");await Task.Delay(120);}
 static void UpdateOutline(ElevatorObservation o)=>Invoke(o,"LateUpdate");
 static ElevatorObservation Observation(ElevatorClue clue)=>l.CollectObservations().First(o=>o.clue==clue&&o.gameObject.activeInHierarchy);
 static async Task Stable(string name,Action action)
 {var p=m.transform.position;var a=c.LookAngles;action();await Task.Delay(100);Check(Vector3.Distance(p,m.transform.position)<.0001f&&Angle(a,c.LookAngles)<.001f,name+" does not restore interior recentering or forced gaze");}
 public static async Task<object> Main()
 {
  checks.Clear();l=Object.FindFirstObjectByType<ElevatorLesson>();
  if(!Application.isPlaying||!l||l.Flow==null)throw new Exception("Enter Play Mode first.");
  c=Object.FindFirstObjectByType<ElevatorDesktopControls>();m=c.movement;s=l.scenario;map=l.presentation.space.sportsCentre;scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=s.enabled;var inputSettings=InputSystem.settings;var temp=Object.Instantiate(inputSettings);bool background=Application.runInBackground;
  var realMouse=Mouse.current;bool mouseEnabled=realMouse!=null&&realMouse.enabled;Mouse mouse=null;string error=null;
  try
  {
   l.enabled=false;c.enabled=false;m.enabled=false;s.enabled=false;
   l.Restart();Settle();
   l.Continue();l.Advance(3.3f);c.SetLookAngles(25,18);l.Advance(.2f);
   Check(l.Flow.Step==ElevatorStep.Entering&&Angle(c.LookAngles,new Vector2(25,18))>.1f&&Mathf.Abs(Mathf.DeltaAngle(c.LookAngles.x,180))>1,"Initial boarding restores a progressive front-facing turn");
   l.Advance(1.2f);Check(Angle(c.LookAngles,new Vector2(180,0))<.01f,"Initial boarding turn finishes facing the elevator front");
   l.Advance(l.settings.boardingSeconds);Settle();
   Check(l.Flow.Step==ElevatorStep.Boarding&&Vector3.Distance(m.FeetPosition,map.inside)<.02f,"Initial boarding still ends inside the cabin");
   c.SetLookAngles(65,-11);await Stable("Floor button",()=>l.SelectFloor());
   await Stable("Ball selection",()=>l.Continue());
   await Stable("Inspect choice",()=>l.ObserveProposal());
   l.DeveloperJump(4);Settle();c.SetLookAngles(112,15);await Stable("Report situation",()=>l.ReportSituation());await Stable("Identity selection",()=>l.FindIdentity());await Stable("Location report",()=>l.Report());
   // Pocket geometry and follow behaviour.
   l.Restart();Settle();await Task.Delay(80);
   var phone=Observation(ElevatorClue.Phone);var ball=Observation(ElevatorClue.Ball);var friend=Observation(ElevatorClue.Friend);
   Check(phone.transform.IsChildOf(l.friend)&&phone.transform.parent.name=="pelvis","Phone is attached to friend's pelvis, not the cabin wall");
   var local=l.friend.InverseTransformPoint(phone.transform.position);
   Check(local.x>.1f&&local.x<.3f&&local.y>.3f&&local.y<.7f&&local.z<0,"Phone sits in the viewer-right front pocket");
   Check(!friend.outlineTarget.geometry.Any(g=>g.sourceRenderer&&g.sourceRenderer.transform.IsChildOf(phone.transform)),"Phone does not become part of friend's highlight mask");
   Check(phone.outlineTarget.geometry.All(g=>g.sourceRenderer&&g.sourceRenderer.transform.IsChildOf(phone.transform)),"Phone keeps an independent highlight and select target");
   PlaceCamera(l.friend.position+new Vector3(0,.75f,-1.25f));Aim(l.friend.position+new Vector3(.08f,.55f,0));
   await Task.Delay(150);await Shot("01_PocketCloseup");
   var phonePoint=phone.GetComponent<BoxCollider>().bounds.center;Aim(phonePoint);Physics.SyncTransforms();Invoke(c,"UpdateCenterRay");
   Check(c.HoverTargetName==phone.name,"Visible pocket phone can be selected without selecting friend");
   var oldPhone=phone.transform.position;var offset=l.friend.InverseTransformPoint(phone.transform.position);
   l.friend.position+=new Vector3(.6f,0,.3f);l.friend.Rotate(Vector3.up,40,Space.World);Physics.SyncTransforms();
   Check(Vector3.Distance(phone.transform.position,l.friend.TransformPoint(offset))<.001f&&Vector3.Distance(phone.transform.position,oldPhone)>.2f,"Pocket phone follows friend movement and turning");
   l.Restart();Settle();c.ClearTarget();
   // Only active, correct story objects receive delayed guidance.
   s.AdvanceGuidance(0);Check(s.GuidanceTarget==ElevatorClue.LobbyCall&&!s.GuidanceActive,"Guidance starts with the lobby call button and no immediate answer reveal");
   s.AdvanceGuidance(7.99f);Check(!s.GuidanceActive,"Guidance does not start before eight seconds");
   l.Activate(ElevatorClue.Notice);Check(s.GuidanceElapsed>=7.99f,"Wrong object selection does not restart guidance timer");
   Observation(ElevatorClue.Notice).Hover(true);Check(s.GuidanceElapsed>=7.99f,"Hovering alone does not restart guidance timer");Observation(ElevatorClue.Notice).Hover(false);
   s.AdvanceGuidance(.01f);Check(s.GuidanceActive,"Guidance starts at eight seconds without a correct selection");
   var call=Observation(ElevatorClue.LobbyCall);s.AdvanceGuidance(.25f);UpdateOutline(call);Check(s.GuidanceBlend>.49f&&s.GuidanceBlend<.51f,"One-second pulse reaches a smooth intermediate colour at quarter cycle");
   s.AdvanceGuidance(.25f);UpdateOutline(call);Check(s.GuidanceBlend>.999f&&Vector4.Distance(call.outlineTarget.Colour,new Color(.32f,.84f,1,1))<.01f,"Pulse smoothly reaches blue at half cycle");
   s.AdvanceGuidance(.5f);UpdateOutline(call);Check(s.GuidanceBlend<.001f&&Vector4.Distance(call.outlineTarget.Colour,new Color(1,.7f,.28f,1))<.01f,"Pulse returns to orange at a full second");
   Check(!s.IsGuidanceTarget(ElevatorClue.Notice)&&!s.IsGuidanceTarget(ElevatorClue.Friend),"Unrelated objects keep their normal colours");
   call.Hover(true);await Task.Delay(400);UpdateOutline(call);Check(Vector4.Distance(call.outlineTarget.Colour,new Color(.32f,.84f,1,1))<.015f,"Real hover still takes priority over guidance pulsing");call.Hover(false);
   float timer=s.GuidanceElapsed;l.pauseMenu.Pause();s.AdvanceGuidance(20);Check(s.GuidanceElapsed==timer,"Guidance timer freezes in pause menu");l.pauseMenu.Resume();
   l.Activate(ElevatorClue.LobbyCall);s.AdvanceGuidance(0);Check(!s.GuidanceActive&&s.GuidanceTarget==null,"Correct call selection immediately stops hint while boarding");
   l.DeveloperJump(1);Settle();Check(s.GuidanceTarget==ElevatorClue.FloorButtons&&s.GuidanceElapsed==0,"New task resets eight-second timer to floor button");
   s.AdvanceGuidance(8.5f);l.Activate(ElevatorClue.FrontFloorButtons);s.AdvanceGuidance(0);Check(s.GuidanceTarget==ElevatorClue.Ball&&s.GuidanceElapsed==0,"Valid alternate floor panel advances hint to the basketball");
   l.Continue();s.AdvanceGuidance(0);Check(s.GuidanceTarget==(s.objectGuidedScene?(ElevatorClue?)ElevatorClue.Notice:null),"Proposal hint follows the current scene's interaction style");
   l.DeveloperJump(3);l.Advance(2);Settle();Check(s.GuidanceTarget==ElevatorClue.Intercom,"Stopped investigation hints the reachable emergency bell");
   s.AdvanceGuidance(8.5f);UpdateOutline(Observation(ElevatorClue.Intercom));await Shot("02_IntercomGuidance");
   l.Intercom();s.AdvanceGuidance(0);float wait=s.GuidanceElapsed;s.AdvanceGuidance(20);Check(!s.GuidanceActive&&s.GuidanceElapsed==wait,"Cinematic beats cannot accumulate hint pulses");
   l.Advance(l.settings.connectionSeconds+.1f);Settle();l.ReportSituation();s.AdvanceGuidance(0);Check(s.GuidanceTarget==ElevatorClue.Identity,"Management request changes guidance to the identity plate");
   l.FindIdentity();s.AdvanceGuidance(0);Check(s.GuidanceTarget==null,"Correct identity selection stops object hint during dialogue-only actions");
   l.DeveloperJump(3);l.simulateNoResponse=true;l.Advance(2);Settle();l.Intercom();l.Advance(l.settings.connectionSeconds+.1f);Settle();
   Check(s.GuidanceTarget==ElevatorClue.Phone,"No-response branch hints the pocket phone");l.simulateNoResponse=false;
   // Management dialogue belongs to the reachable intercom, not to a fixed cabin-centre point.
   l.DeveloperJump(4);Settle();l.ReportSituation();l.view.spatial.Tick();
   Check(Vector3.Distance(map.DialogueAnchor(),map.sideIntercom.position)<.8f,"Manager dialogue is spatially anchored beside the emergency bell");
   Check(Vector3.Distance(l.view.transform.position,map.DialogueAnchor())<.001f,"World dialogue canvas actually uses intercom placement");
   var anchor=l.view.transform.position;PlaceCamera(map.inside+new Vector3(.1f,1.1f,0));Aim(map.sideIntercom.position+Vector3.up*.2f);l.view.spatial.Tick();
   Check(Vector3.Distance(anchor,l.view.transform.position)<.001f,"Manager speech stays at the bell while player looks or walks");
   await Task.Delay(100);await Shot("03_ManagerAtBell");
   // Hide observation heading and its decorative panel, without touching mission thought title.
   l.view.spatial.ShowThought("친구 주머니에 휴대폰이 살짝 보여요.\n필요할 때 선택할 수 있어요.");
   var bubble=l.view.thoughtPanel.Find("CurrentThoughtBubble");
   Check(bubble&&!bubble.Find("Heading").gameObject.activeSelf&&!bubble.Find("ThoughtRibbon").gameObject.activeSelf,"Observed screen bubble has no heading text or heading panel");
   var text=bubble.Find("Thought").GetComponent<TMP_Text>();text.ForceMeshUpdate();Check(bubble.Find("Artwork").gameObject.activeSelf&&text.gameObject.activeSelf&&!text.isTextOverflowing,"Observed bubble and readable description remain");
   await Shot("04_DescriptionOnly");l.view.spatial.ShowThought("");Check(bubble.Find("Heading").gameObject.activeSelf&&bubble.Find("ThoughtRibbon").gameObject.activeSelf,"Non-observation mission thoughts retain their original title");
   // Full-size receiving motion with alpha fade, and no persistent miniature ball.
   l.DeveloperJump(1);Settle();l.SelectFloor();PlaceCamera(map.inside+Vector3.up*1.1f);Aim(l.friend.position+Vector3.up*.8f);
   l.Continue();Check(l.presentation.playerBall.gameObject.activeSelf&&l.presentation.BallVisibility>.99f,"Basketball receiving starts at full visibility");
   Check(Vector3.Distance(l.presentation.playerBall.lossyScale,l.presentation.friendBall.lossyScale)<.001f,"Receiving ball keeps the original world size instead of shrinking");
   var start=l.presentation.playerBall.position;await Shot("05_CatchStart");await Task.Delay(450);l.Advance(0);
   Check(l.presentation.BallVisibility>0&&l.presentation.BallVisibility<1&&Vector3.Distance(start,l.presentation.playerBall.position)>.1f,"Ball moves toward hands while opacity fades smoothly");
   var block=new MaterialPropertyBlock();l.presentation.playerBall.GetComponent<Renderer>().GetPropertyBlock(block);Check(Mathf.Abs(block.GetFloat("_Visibility")-l.presentation.BallVisibility)<.001f,"Ball fade drives actual shader visibility");
   await Shot("06_CatchFade");await Task.Delay(700);l.Advance(0);
   Check(!l.presentation.playerBall.gameObject.activeSelf&&l.presentation.BallVisibility==0,"Received ball naturally disappears without a tiny held model");
   l.Refresh();l.Advance(0);Check(!l.presentation.playerBall.gameObject.activeSelf,"Dialogue refresh does not make the hidden ball reappear");
   Check(!ShaderUtil.ShaderHasError(l.presentation.ballFadeMaterial.shader),"URP ball-fade shader compiles");await Shot("07_CatchFinished");
   l.Restart();Settle();Check(!l.presentation.playerBall.gameObject.activeSelf&&l.presentation.friendBall.gameObject.activeSelf,"Restart restores friend ball and clears transfer fade");
   // Real mouse deltas prove the letterbox sensitivity ratio, without changing physical head tracking.
   temp.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temp.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temp;Application.runInBackground=true;
   if(mouseEnabled)InputSystem.DisableDevice(realMouse);mouse=InputSystem.AddDevice<Mouse>("PolishQAMouse");mouse.MakeCurrent();c.enabled=true;
   await Task.Delay(100);c.SetLookAngles(0,0);InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(4,4),delta=new Vector2(100,0)});await Task.Delay(90);InputSystem.QueueStateEvent(mouse,new MouseState());await Task.Delay(60);float normal=c.LookAngles.x;
   Check(c.LookSensitivityMultiplier==1&&normal>1,"No-letterbox gameplay retains normal mouse sensitivity");
   l.DeveloperJump(2);l.UnsafeProposal();Settle();await Task.Delay(100);c.SetLookAngles(0,0);InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(4,4),delta=new Vector2(100,0)});await Task.Delay(90);InputSystem.QueueStateEvent(mouse,new MouseState());await Task.Delay(60);float slow=c.LookAngles.x;
   Check(c.LookSensitivityMultiplier==.1f&&Mathf.Abs(slow/normal-.1f)<.01f,"Visible letterbox reduces real look response by exactly ninety percent");
   Check(l.CinematicBlocking&&!m.CanMove,"Letterbox scenes retain gameplay input lock");
   l.Advance(l.settings.friendReplySeconds+l.settings.jumpSeconds+l.settings.afterJumpSeconds+.1f);l.Advance(2);l.presentation.space.overlay.Advance(.1f);Check(c.LookSensitivityMultiplier==.1f,"Sensitivity reduction persists while letterbox slides away");Settle();Check(c.LookSensitivityMultiplier==1,"Normal response returns once letterbox fully disappears");
  }
  catch(Exception e){error=e.ToString();}
  finally
  {
   if(l.IsPaused)l.pauseMenu.Resume();l.simulateNoResponse=false;c.ClearTarget();l.Restart();Settle();
   c.enabled=ce;m.enabled=me;s.enabled=se;l.enabled=le;
   if(mouse!=null)InputSystem.RemoveDevice(mouse);if(mouseEnabled)InputSystem.EnableDevice(realMouse);realMouse?.MakeCurrent();InputSystem.settings=inputSettings;Object.Destroy(temp);Application.runInBackground=background;
  }
  var result=new{success=error==null,scene,checks=checks.ToArray(),error,actualHMD=false};File.WriteAllText(Dir+scene+"_Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}