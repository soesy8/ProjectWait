using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class VerifySportsCentre
{
 const string Dir="Assets/M/CodexTemp/UnitySportsCentreRebuild~/";
 static ElevatorLesson l;static ElevatorDesktopControls controls;static ElevatorSportsCentreMap map;
 static readonly List<object> checks=new List<object>();
 static int passed;
 static void Check(bool ok,string name){checks.Add(new{name,passed=ok});if(!ok)throw new Exception(name);passed++;}
 static void ClearOverlay(){l.presentation.space.overlay.Advance(2);}
 static void Advance(float seconds){l.Advance(seconds);ClearOverlay();}
 static async Task Choice(int i){ClearOverlay();l.view.InvokeChoice(i);await Task.Delay(80);}
 static async Task Aim(Vector3 target)
 {
  controls.SetMouseLook(false);var rotation=Quaternion.LookRotation(target-l.playerCamera.transform.position);
  controls.SetScriptedHeading(rotation.eulerAngles.y);l.playerCamera.transform.rotation=rotation;
  InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f)});await Task.Delay(220);
 }
 static async Task Click()
 {
  var p=new Vector2(Screen.width*.5f,Screen.height*.5f);
  InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left,true));await Task.Delay(130);
  InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});await Task.Delay(150);
 }
 static async Task Physical(string name)
 {
  var t=l.transform.Find("SportsCentre_Interactions/"+name);await Aim(t.position);
  if(controls.HoverTargetName!=name)
  {
   var ray=l.playerCamera.ScreenPointToRay(new Vector2(Screen.width*.5f,Screen.height*.5f));
   File.WriteAllText(Dir+"RayDiagnostics.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{expected=name,hover=controls.HoverTargetName,thought=l.Thought,blocking=l.CinematicBlocking,desktop=controls.IsDesktop,mouse=Mouse.current.position.ReadValue().ToString(),width=Screen.width,height=Screen.height,state=l.Snapshot(),hits=Physics.RaycastAll(ray,12).OrderBy(h=>h.distance).Take(12).Select(h=>new{name=h.collider.name,distance=h.distance,clue=h.collider.GetComponentInParent<ElevatorObservation>()?.clue.ToString()}).ToArray()},Newtonsoft.Json.Formatting.Indented));
  }
  Check(controls.HoverTargetName==name,"Mouse ray reaches "+name);await Click();
 }
 static async Task Capture(string name)
 {
  await Task.Delay(900);ScreenCapture.CaptureScreenshot(Dir+name+".png");await Task.Delay(300);
 }
 static void FlowChecks()
 {
  foreach(bool unsafeFirst in new[]{false,true})
  {
   var f=new ElevatorFlow(l.settings);f.Continue();f.Tick(7);f.Continue();Check(f.Step==ElevatorStep.Boarding,"Ball handoff requires destination");f.SelectDestination();f.Continue();
   f.Tick(120);Check(f.Step==ElevatorStep.Proposal,"Judgment has no timeout");
   if(unsafeFirst){f.UnsafeProposal();Check(f.ProposalRetry&&f.JumpCount==0,"Unsafe shot is paused before any jump");f.Retry();}
   f.ChooseSafe();f.Tick(3);f.Tick(2);Check(f.Step==ElevatorStep.Stopped&&f.JumpCount==0,"Independent equipment stop after "+(unsafeFirst?"revised":"safe")+" judgment");
   Check(f.FirstJudgment==(unsafeFirst?"여기서 보여 줘":"밖에서 다시 해 보자"),"First judgment preserved");
   f.RequestIntercom();f.Tick(4);Check(f.Step==ElevatorStep.Call&&!f.IdentityFound,"Help connects before identity lookup");f.ReportLocation();Check(!f.LocationReported,"Cannot fabricate location before reading it");f.ReportSituation();f.FindIdentity();f.ReportLocation();f.FollowAdvice();f.Reassure();f.Tick(13);Check(f.Step==ElevatorStep.Rescue&&f.Encouragements==0,"Rescue does not require optional chats");
  }
  var fallback=new ElevatorFlow(l.settings){NoResponse=true};fallback.DeveloperJump(3);fallback.Tick(2);fallback.RequestIntercom();fallback.Tick(4);Check(fallback.Step==ElevatorStep.Unanswered,"Unanswered intercom branch");fallback.RequestPhone();fallback.ReportSituation();fallback.FindIdentity();fallback.ReportLocation();fallback.FollowAdvice();fallback.Reassure();fallback.Tick(13);Check(fallback.Step==ElevatorStep.Rescue&&fallback.UsedPhone,"Simulated 119 branch reaches rescue");
 }
 public static async Task<string> Main()
 {
  checks.Clear();passed=0;l=Object.FindFirstObjectByType<ElevatorLesson>();controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();map=l.GetComponent<ElevatorSportsCentreMap>();
  if(!Application.isPlaying||l.Flow==null)return "{\"success\":false,\"passed\":0,\"error\":\"Play Mode has not finished initializing\"}";
  var original=InputSystem.settings;var temporary=Object.Instantiate(original);bool background=Application.runInBackground;
  temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  InputSystem.settings=temporary;Application.runInBackground=true;
  var physicalMouse=Mouse.current;bool mouseWasEnabled=physicalMouse!=null&&physicalMouse.enabled;
  if(mouseWasEnabled)InputSystem.DisableDevice(physicalMouse);
  var testMouse=InputSystem.AddDevice<Mouse>("SportsCentre_QA_Mouse");testMouse.MakeCurrent();
  string error=null;int start=Time.frameCount;object completed=null;
  try
  {
   Check(Application.isPlaying,"Actual Play Mode");Check(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Exactly one EventSystem");
   Check(map&&map.geometry&&map.sideIntercom&&map.frontIntercom&&map.identity,"Scene and map references assigned");
   Check(Mathf.Abs(map.sideIntercom.position.y-7.92f-1.28f)<.01f&&Mathf.Abs(map.frontIntercom.position.y-7.92f-2.15f)<.01f,"Distinct low side / high front emergency heights");
   Check(map.sideIntercom.position.z>4.3f,"Side controls placed behind the entrance");
   Check(map.geometry.Find("1F")&&map.geometry.Find("2F")&&map.geometry.Find("3F"),"Three authored floors present");
   FlowChecks();l.Restart();await Task.Delay(400);ClearOverlay();
   Check(l.Floor==3&&Mathf.Abs(l.playerCamera.transform.position.y-7.92f-controls.eyeHeight)<.01f,"Player starts at actual 3F height");
   await Capture("01_Lobby");
   var vp=l.playerCamera.WorldToViewportPoint(l.view.dialoguePanel.position);Check(vp.x>.15f&&vp.x<.85f&&vp.y>.15f&&vp.y<.85f,"Initial dialogue is within the view");
   await Physical("3F_Lobby_Call");Check(l.Flow.Step==ElevatorStep.Entering,"Physical call button starts boarding");
   await Task.Delay(6600);ClearOverlay();Check(l.Flow.Step==ElevatorStep.Boarding,"Real-time boarding completes");Check(map.DoorOpen<.01f,"Cabin doors close after boarding");
   await Physical("3F_Side_Floor01");Check(l.Flow.DestinationSelected,"Physical low side 1F button works");
   Check(map.firstFloorRings.All(r=>r.sharedMaterial==map.selectedButton),"Both 1F panel rings reflect selection");
   await Capture("02_SidePanel_1F");await Choice(0);Check(l.Flow.Step==ElevatorStep.Proposal,"Ball handoff leads to judgment");
   controls.SetScriptedHeading(180);await Capture("03_Judgment");
   Check(l.view.ChoiceButtons.Count==3,"Three judgment choices present");
   var colors=l.view.ChoiceButtons.Select(b=>b.GetComponent<DwtD.TestScene.CorridorBubbleGraphic>().color).Distinct().Count();Check(colors==1,"Choices do not reveal safety through different colors");
   await Choice(2);Check(l.Flow.Phase==InteractionPhase.Investigation,"Optional look-around remains available");
   await Choice(1);Check(l.Flow.ProposalRetry&&l.Flow.JumpCount==0,"Unsafe UI choice pauses before motion");await Capture("04_WaitFeedback");
   await Choice(0);await Choice(0);Check(l.Flow.Step==ElevatorStep.Travel,"Reconsidered choice resumes travel");
   await Task.Delay(4900);ClearOverlay();Check(l.Flow.Step==ElevatorStep.Stopped&&l.Flow.CanObserve,"Real-time independent stop reaches investigation");
   Check(l.Floor==3&&l.cabinLight.intensity==1.6f&&l.Flow.JumpCount==0,"Last display 3, stable lighting and no jump");
   await Physical("3F_Front_Emergency_2.15m");Check(l.Flow.Step==ElevatorStep.Stopped&&l.Thought.Contains("손이 닿기"),"High front bell directs attention to reachable side bell");
   await Physical("3F_FacilityDirectory_NoHighlight");Check(l.Thought.Contains("2F 준비 중"),"Facility directory can be inspected optionally");
   await Capture("05_PosterAndDirectory");
   await Physical("3F_Side_Emergency_1.28m");Check(l.Flow.Step==ElevatorStep.Connecting&&!l.Flow.IdentityFound,"Low side bell requests help before identifying the lift");
   Advance(3);Check(l.Flow.Step==ElevatorStep.Call,"Intercom connects");await Choice(0);Check(l.Flow.SituationReported&&l.Flow.HelpRequested&&!l.Flow.IdentityFound,"Situation accepted before identity");
   await Physical("3F_LiftIdentity_HB-EL-01");Check(l.Flow.IdentityFound,"Physical identity plate is readable and selectable");
   await Capture("06_Identity");await Choice(0);Check(l.Flow.LocationReported,"Building, car and HB-EL-01 reported");await Choice(0);Check(l.Flow.Step==ElevatorStep.Waiting,"Location report leads to wait");
   await Choice(0);Check(l.Flow.Reassured,"Friend is reassured");Advance(13);Check(l.Flow.Step==ElevatorStep.Rescue&&!l.Flow.ReadyToExit,"Rescue first checks the landing");
   int revision=l.Flow.Revision;await Choice(0);Check(l.Flow.Revision==revision,"Exit is blocked before the safety check");Advance(4);Check(l.Flow.ReadyToExit&&map.DoorOpen>.99f,"Doors open after landing check");
   controls.SetScriptedHeading(180);await Capture("07_RescueAt3F");await Choice(0);Advance(5);Check(l.Flow.Step==ElevatorStep.ReturnLobby,"Cabin exit remains at 3F");
   await Choice(0);Check(l.Flow.Step==ElevatorStep.Returning,"Escorted stair transition is explicit");Advance(1.8f);Check(map.OnGround&&Mathf.Abs(l.playerCamera.transform.position.y-controls.eyeHeight)<.01f,"Floor transition moves the actual rig to 1F");Advance(2);await Capture("08_GroundFloor");
   await Choice(0);await Task.Delay(6300);Check(l.Flow.Step==ElevatorStep.Complete&&l.Floor==1,"Journey completes outside the 1F main entrance");
   Check(map.autoDoorLeft.localPosition.x<-1.2f&&map.autoDoorRight.localPosition.x>1.2f,"Ground automatic doors open for exit");
   Check(l.view.spatial.notebook.gameObject.activeInHierarchy,"Completion notebook visible");await Capture("09_Complete");completed=l.Snapshot();
   l.Restart();ClearOverlay();Check(l.Flow.Step==ElevatorStep.Lobby&&!l.Flow.DestinationSelected&&l.Flow.History.Count==0&&l.Floor==3,"Restart resets story and returns to 3F");
   Check(Mathf.Abs(spaceY()-7.92f)<.01f&&map.autoDoorLeft.localPosition.sqrMagnitude<.001f,"Restart restores elevation and closes the ground door");
   int now=Time.frameCount;await Task.Delay(500);Check(Time.frameCount>now&&now>start,"Player frames advance throughout the test");
  }
  catch(Exception e){error=e.ToString();ScreenCapture.CaptureScreenshot(Dir+"Failure.png");}
  finally{InputSystem.RemoveDevice(testMouse);if(mouseWasEnabled)InputSystem.EnableDevice(physicalMouse);if(physicalMouse!=null)physicalMouse.MakeCurrent();InputSystem.settings=original;Object.Destroy(temporary);Application.runInBackground=background;controls.SetMouseLook(false);}
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,passed,checks,error,completed,state=l.Snapshot(),frameStart=start,frameEnd=Time.frameCount,mode="Unity Editor PC Play Mode; actual mouse Input System events, UI callbacks and real-time transitions; selected waits compressed through Advance; no physical HMD"},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Dir+"RuntimeVerification.json",report);return report;
 }
 static float spaceY()=>l.presentation.space.playerRig.position.y;
}
