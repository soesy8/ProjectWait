using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyMaskScene
{
 const string D="Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/";
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPlayerMovement move;static List<object> checks;static string mode;
 static Vector2 Centre=>new Vector2(Screen.width*.5f,Screen.height*.5f);
 static void Check(bool ok,string name){checks.Add(new{name,passed=ok});if(!ok)throw new Exception(name);}
 static ElevatorObservation Obs(ElevatorClue clue)=>l.CollectObservations().First(o=>o.gameObject.activeInHierarchy&&o.clue==clue);
 static async Task Mouse(Vector2 p,bool down=false){var state=new MouseState{position=p};if(down)state=state.WithButton(MouseButton.Left,true);InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,state);await Task.Delay(90);}
 static async Task Idle(){foreach(var o in l.CollectObservations())o.ResetHover();c.SetLookAngles(180,65);await Mouse(Centre);await Task.Delay(150);}
 static void Stage(int n){l.DeveloperJump(n);if(n==3)l.Advance(2);l.presentation.space.overlay.Advance(2);}
 static Rect Box(RectTransform r){var corners=new Vector3[4];r.GetWorldCorners(corners);var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;foreach(var w in corners){var p=l.playerCamera.WorldToViewportPoint(w);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
 static float Area(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
 static async Task Aim(ElevatorObservation o){var b=o.GetComponent<Collider>().bounds;var p=b.center;if(o.clue==ElevatorClue.Friend)p+=Vector3.up*b.extents.y*.55f;var e=Quaternion.LookRotation(p-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(e.y,Mathf.DeltaAngle(0,e.x));await Mouse(Centre);await Task.Delay(160);}
 static async Task Shot(string name){await Awaitable.EndOfFrameAsync();ScreenCapture.CaptureScreenshot(D+mode+"_"+name+".png");await Task.Delay(120);}
 static async Task Hover(ElevatorClue clue,string label)
 {
  var o=Obs(clue);await Aim(o);Check(c.HoverTargetName==o.name,label+": actual ray reaches the object ("+c.HoverTargetName+")");
  Check(o.outlineTarget.Visible&&o.outlineTarget.Hovered,label+": hover changes this object to the hover state");
  var color=o.outlineTarget.Colour;Check(color.b>.95f&&color.r<.5f&&Mathf.Abs(o.outlineTarget.Width-10.4f)<.01f,label+": cyan hover uses a 10.4 pixel exterior band");
  var hint=l.scenario.hints.First(h=>h.observation==o);Check(hint.group.alpha>.99f,label+": description appears on hover");
  Check(l.view.dialoguePanel.gameObject.activeInHierarchy&&l.view.dialoguePanel.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha>.99f),label+": dialogue remains fully visible");
  var hb=Box((RectTransform)hint.transform);Check(Area(hb,Box(l.view.dialoguePanel))<.0001f,label+": description does not overlap dialogue");
  foreach(var button in l.view.ChoiceButtons.Where(b=>b&&b.IsActive()))Check(Area(hb,Box((RectTransform)button.transform))<.0001f,label+": description avoids button "+button.name);
  var fc=l.friend.GetComponent<Collider>().bounds;var face=new Bounds(fc.center+Vector3.up*fc.extents.y*.65f,new Vector3(fc.size.x,fc.size.y*.4f,fc.size.z));var faceMin=Vector2.one*float.MaxValue;var faceMax=Vector2.one*float.MinValue;bool faceInFront=true;for(int i=0;i<8;i++){var p=l.playerCamera.WorldToViewportPoint(face.center+Vector3.Scale(face.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(p.z<=l.playerCamera.nearClipPlane)faceInFront=false;faceMin=Vector2.Min(faceMin,p);faceMax=Vector2.Max(faceMax,p);}if(faceInFront)Check(Area(hb,Rect.MinMaxRect(faceMin.x,faceMin.y,faceMax.x,faceMax.y))<.0001f,label+": explanation does not cover the friend's face");
  Check(Area(hb,new Rect(0,0,1,1))>=hb.width*hb.height*.98f,label+": description remains within the view");await Shot(label);
 }
 static async Task Next(){var b=l.view.ChoiceButtons.Single();var e=Quaternion.LookRotation(b.transform.position-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(e.y,Mathf.DeltaAngle(0,e.x));await Mouse(Centre);Check(c.HoverTargetName==b.name&&b.name=="DialogueNext","Separate Next control stays reachable");await Mouse(Centre,true);await Mouse(Centre);}
 static void Warp(Vector3 feet,float yaw){move.Controller.enabled=false;move.transform.position=feet;move.Controller.enabled=true;move.CalibrateFeet(feet.y);move.ResetVerticalMotion();c.SetLookAngles(yaw,0);Physics.SyncTransforms();}
 static void Walk(int n){for(int i=0;i<n;i++)move.Move(Vector2.up,.02f,false);}
 public static async Task<string> Main()
 {
  for(int i=0;i<100;i++){l=Object.FindFirstObjectByType<ElevatorLesson>();if(Application.isPlaying&&l&&l.Flow!=null)break;await Task.Delay(100);}
  c=Object.FindFirstObjectByType<ElevatorDesktopControls>();move=Object.FindFirstObjectByType<ElevatorPlayerMovement>();mode=l.scenario.objectGuidedScene?"Guided":"Original";checks=new List<object>();string error=null;
  var old=InputSystem.settings;var temp=Object.Instantiate(old);temp.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temp.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temp;
  var hardware=UnityEngine.InputSystem.Mouse.current;bool enabled=hardware!=null&&hardware.enabled;if(enabled)InputSystem.DisableDevice(hardware);var mouse=InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("AmbientOutlineQAMouse");mouse.MakeCurrent();
  try
  {
   Stage(0);await Task.Delay(160);await Idle();var objects=l.CollectObservations().Where(o=>o.gameObject.activeInHierarchy&&o.hasDescription&&l.CanInteract(o.clue)).ToArray();
   Check(objects.Length>=10,"Many description objects share the same idle affordance, without choosing one answer");
   Check(objects.All(o=>o.outlineTarget&&o.outlineTarget.Visible&&!o.outlineTarget.Hovered),"Description objects remain highlighted without hover");Check(l.scenario.hints.All(h=>h.group.alpha==0),"Descriptions themselves stay hidden until hover");Check(!Obs(ElevatorClue.LobbyCallUp).outlineTarget||!Obs(ElevatorClue.LobbyCallUp).outlineTarget.Visible,"Unavailable upward call is not highlighted");
   foreach(var o in objects){var col=o.outlineTarget.Colour;Check(col.r>.9f&&col.b<.5f&&Mathf.Abs(o.outlineTarget.Width-6.5f)<.01f,"Shared warm idle silhouette: "+o.clue);}
   Check(ElevatorSilhouetteOutlineFeature.LastFrame>=Time.frameCount-2&&ElevatorSilhouetteOutlineFeature.LastTargetCount>0,"Real Render Graph draws an on-screen object mask");
   Check(objects.All(o=>o.outlineTarget.geometry.Length>0),"Each inspectable object has one grouped geometry list");
   Check(!l.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("SkyGlow")||t.name=="ButtonPressFeedback"),"No extruded hulls or old pulse meshes remain");
   var actor=Obs(ElevatorClue.Friend).outlineTarget;Check(actor.geometry.Any(g=>g.sourceRenderer is SkinnedMeshRenderer)&&actor.geometry.All(g=>!g.mesh),"Friend mask follows real animated skinned renderers without copied meshes");

   await Hover(ElevatorClue.Friend,"01_LobbyFriend");Check(ElevatorSilhouetteOutlineFeature.LastTargetCount>2,"Multiple visible objects keep independent masks");float perfStart=Time.realtimeSinceStartup;int perfFrame=Time.frameCount;await Task.Delay(1700);Check(Time.frameCount>perfFrame,"Full-scene mask rendering continues across frames");File.WriteAllText(D+mode+"Performance.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{fps=(Time.frameCount-perfFrame)/(Time.realtimeSinceStartup-perfStart),targets=ElevatorSilhouetteOutlineFeature.LastTargetCount,width=Screen.width,height=Screen.height}));var fh=l.scenario.hints.First(h=>h.observation==Obs(ElevatorClue.Friend));var anchor=fh.transform.position;var size=fh.transform.lossyScale;move.Move(new Vector2(-.3f,0),.15f,false);await Task.Delay(30);Check(Vector3.Distance(fh.transform.position,anchor)<.001f&&Vector3.Distance(fh.transform.lossyScale,size)<.000001f,"Description keeps a fixed world position and size after placement");
   await Hover(ElevatorClue.LobbyCall,"02_LobbyCall");var fb=Obs(ElevatorClue.LobbyCall).feedback;var band=fb.outlineTarget;int renderersBefore=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;fb.Press();Check(fb.IsAnimating&&band.Width>10.4f&&band.Colour.r>.5f,"Button press briefly brightens and thickens the same mask outline");Check(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length==renderersBefore,"Button feedback creates no extra hull renderer");await Task.Delay(550);Check(!fb.IsAnimating&&Mathf.Abs(band.Width-10.4f)<.01f,"Button feedback returns to normal hover presentation");Check(!Obs(ElevatorClue.Friend).outlineTarget.Hovered&&Obs(ElevatorClue.Friend).outlineTarget.Visible,"Previous hover returns to the warm idle outline");await Idle();Check(l.scenario.hints.All(h=>h.group.alpha==0),"Leaving hover hides the descriptions, without hiding idle outlines");
   Warp(new Vector3(-.6f,7.92f,-4.35f),180);await Task.Delay(450);Walk(130);Check(move.FeetPosition.z< -7.9f&&move.FeetPosition.z> -8.4f,"Rear wall now stops the capsule two metres farther behind the door: "+move.FeetPosition.ToString("F3"));
   Warp(new Vector3(-4.5f,7.92f,-4.35f),180);Walk(90);Check(move.FeetPosition.z> -5.3f&&move.FeetPosition.z< -4.7f,"Real basketball-side wall still blocks passage");
   Stage(2);await Task.Delay(180);await Hover(ElevatorClue.Friend,"03_ProposalFriend");
   Stage(3);await Task.Delay(180);await Hover(ElevatorClue.Friend,"04_CabinFriend");await Hover(ElevatorClue.Intercom,"05_Emergency");await Hover(ElevatorClue.Notice,"06_Poster");await Hover(ElevatorClue.FacilityGuide,"07_Directory");
   Stage(4);await Task.Delay(180);await Hover(ElevatorClue.Intercom,"08_OperatorDescription");await Next();Check(l.Flow.SituationReported,"Next dialogue progression is unaffected");await Hover(ElevatorClue.Identity,"09_Identity");await Mouse(Centre,true);await Mouse(Centre);Check(l.Flow.IdentityFound,"Identity is still inspected directly");await Next();Check(l.Flow.LocationReported,"Next still reports the location");await Next();Check(l.Flow.Step==ElevatorStep.Waiting,"Conversation proceeds to the reassurance stage");
   await Hover(ElevatorClue.Friend,"10_WaitingFriend");await Idle();l.Restart();l.presentation.space.overlay.Advance(2);
  }
  catch(Exception e){error=e.ToString();ScreenCapture.CaptureScreenshot(D+mode+"_Failure.png");}
  finally{InputSystem.RemoveDevice(mouse);if(enabled)InputSystem.EnableDevice(hardware);hardware?.MakeCurrent();InputSystem.settings=old;Object.Destroy(temp);}
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,mode,passed=checks.Count(x=>(bool)x.GetType().GetProperty("passed").GetValue(x)),checks,error},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(D+mode+"Verification.json",report);return report;
 }
}