using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyInteraction
{
 const string Dir="Assets/M/CodexTemp/ElevatorInteraction_20261030~/";
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPlayerMovement m;static ElevatorPauseMenu p;
 static readonly List<object> checks=new List<object>();static int passed;
 static void Check(bool value,string name){checks.Add(new{name,passed=value});if(!value)throw new Exception(name);passed++;}
 static void Teleport(Vector3 feet,float yaw=0){m.Controller.enabled=false;m.transform.position=feet-Vector3.up*m.FeetOffset;m.Controller.enabled=true;m.ResetVerticalMotion();c.SetLookAngles(yaw,0);Physics.SyncTransforms();}
 static async Task Key(Key key,int hold=130){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));await Task.Delay(hold);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());await Task.Delay(150);}
 static async Task Mouse(Vector2 position,bool down=false,Vector2 delta=default){var state=new MouseState{position=position,delta=delta};if(down)state=state.WithButton(MouseButton.Left,true);InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,state);await Task.Delay(120);}
 static async Task Aim(Vector3 target){var rotation=Quaternion.LookRotation(target-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(rotation.y,Mathf.DeltaAngle(0,rotation.x));await Mouse(new Vector2(4,4));await Task.Delay(120);}
 static async Task ClickCentre(){var pos=new Vector2(4,4);await Mouse(pos,true);await Mouse(pos);}
 static async Task MenuClick(UnityEngine.UI.Button b)
 {
  var screen=RectTransformUtility.WorldToScreenPoint(null,b.transform.position);await Mouse(screen);Check(c.HoverTargetName==b.name,"Pause pointer reaches "+b.name);await Mouse(screen,true);await Mouse(screen);
 }
 static async Task Shot(string name){await Task.Delay(180);ScreenCapture.CaptureScreenshot(Dir+name+".png");await Task.Delay(220);}
 public static async Task<string> Main()
 {
  for(int i=0;i<100;i++){l=Object.FindFirstObjectByType<ElevatorLesson>();if(Application.isPlaying&&l&&l.Flow!=null)break;await Task.Delay(100);}
  if(!Application.isPlaying||!l||l.Flow==null)return "{\"success\":false,\"error\":\"Play Mode not initialized\"}";
  c=Object.FindFirstObjectByType<ElevatorDesktopControls>();m=c.movement;p=l.pauseMenu;checks.Clear();passed=0;
  var original=InputSystem.settings;var temp=Object.Instantiate(original);bool background=Application.runInBackground;temp.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temp.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temp;Application.runInBackground=true;
  var originalMouse=UnityEngine.InputSystem.Mouse.current;var originalKeyboard=Keyboard.current;bool mouseEnabled=originalMouse!=null&&originalMouse.enabled,keyEnabled=originalKeyboard!=null&&originalKeyboard.enabled;
  if(mouseEnabled)InputSystem.DisableDevice(originalMouse);if(keyEnabled)InputSystem.DisableDevice(originalKeyboard);
  var mouse=InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("InteractionQAMouse");var keyboard=InputSystem.AddDevice<Keyboard>("InteractionQAKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();InputDevice left=null,right=null;GameObject occluder=null;Material blockMaterial=null;string error=null;int frame=Time.frameCount;
  try
  {
   l.Restart();await Task.Delay(350);l.presentation.space.overlay.Advance(2);
   Check(m&&m.Controller.enabled&&m.CanMove,"Movement controller active");
   Check(l.view.spatial.hud.renderMode==RenderMode.ScreenSpaceOverlay,"PC HUD is screen overlay");
   var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Check(pipeline&&pipeline.shadowDistance>=55&&AssetDatabase.GetAssetPath(pipeline).StartsWith("Assets/M/"),"Scene-local 60m shadow pipeline active");
   var start=m.FeetPosition;await Key(UnityEngine.InputSystem.Key.W,420);Check(m.FeetPosition.z-start.z>.5f,"W moves forward with actual keyboard events");var before=m.FeetPosition;await Key(UnityEngine.InputSystem.Key.D,300);Check(m.FeetPosition.x-before.x>.3f,"D strafes with actual keyboard events");
   c.SetLookAngles(0,0);await Mouse(new Vector2(3,3),false,new Vector2(180,-45));Check(c.LookAngles.x>10&&c.LookAngles.y>2,"Mouse delta rotates yaw and pitch without a held button");
   var target=l.transform.Find("SportsCentre_Interactions/3F_Lobby_Call");await Aim(target.position);Check(c.HoverTargetName==target.name,"Off-centre cursor still selects the centre-ray target");Check(c.centerLaser.enabled&&c.reticle.gameObject.activeInHierarchy,"Central beam and reticle visible");
   var cr=c.reticle.GetComponent<CanvasRenderer>();var rp=RectTransformUtility.WorldToScreenPoint(null,c.reticle.transform.position);Check(cr&&cr.GetMesh()&&cr.GetMesh().vertexCount>0&&!cr.cull&&Mathf.Abs(rp.x-Screen.width*.5f)<1&&Mathf.Abs(rp.y-Screen.height*.5f)<1,"Reticle has visible rendered geometry at screen centre");var vp=l.playerCamera.WorldToViewportPoint(c.laserDot.position);Check(Mathf.Abs(vp.x-.5f)<.003f&&Mathf.Abs(vp.y-.5f)<.003f,"Ray endpoint is centred");
   var glow=target.GetComponent<ElevatorObservation>().shapeHighlight;Check(glow&&glow.Visible&&glow.shells.Length>=3&&glow.shells.All(r=>r.enabled),"Actual call-button meshes glow");
   Check(glow.shells.All(r=>r.sharedMaterial.shader.name.StartsWith("ProjectWait/M/Sky Highlight")),"Glow uses the silhouette shader");await Shot("01_CentreRayAndGlow");
   await Key(UnityEngine.InputSystem.Key.Escape);Check(p.IsPaused&&Time.timeScale==0&&AudioListener.pause,"Escape pauses game time and audio");Check(p.Buttons.Count()==4&&!p.titleButton.interactable,"Four menu buttons with title placeholder disabled");
   Check(!glow.Visible&&!c.centerLaser.enabled&&!c.reticle.gameObject.activeInHierarchy,"Pause clears glow, beam and reticle");
   float elapsed=l.Flow.Elapsed;var position=m.transform.position;var angles=c.LookAngles;await Key(UnityEngine.InputSystem.Key.W,350);await Mouse(new Vector2(15,15),false,new Vector2(180,90));l.Advance(2);Check(Mathf.Abs(l.Flow.Elapsed-elapsed)<.0001f&&Vector3.Distance(position,m.transform.position)<.001f&&Vector2.Distance(angles,c.LookAngles)<.001f,"Pause freezes scenario, walking and mouse-look");await Shot("02_PauseMenu");
   await MenuClick(p.resumeButton);Check(!p.IsPaused&&Time.timeScale==1&&!AudioListener.pause&&c.MouseLookActive,"Resume restores gameplay and centre mouse-look");
   Teleport(new Vector3(-5.8f,7.95f,0),270);for(int i=0;i<90;i++)m.Move(Vector2.up,.02f,false);Check(m.FeetPosition.x>-6.85f,"Window collision blocks walking through the wall");
   var canvas=l.view.transform;await Task.Delay(120);Check(Vector3.Dot(canvas.forward,(canvas.position-l.playerCamera.transform.position).normalized)>.999f,"World dialogue faces the player after walking");
   Teleport(new Vector3(-.1f,7.95f,-1.8f));await Task.Delay(100);Check(Vector3.Dot(canvas.forward,(canvas.position-l.playerCamera.transform.position).normalized)>.999f,"World dialogue also faces the player from the other side");
   occluder=GameObject.CreatePrimitive(PrimitiveType.Cube);occluder.name="Temporary_HUD_Occlusion_Proof";occluder.GetComponent<Collider>().enabled=false;occluder.transform.SetPositionAndRotation(l.playerCamera.transform.position+l.playerCamera.transform.forward*.18f,l.playerCamera.transform.rotation);occluder.transform.localScale=new Vector3(5,5,.04f);blockMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));blockMaterial.color=new Color(.17f,.1f,.21f);occluder.GetComponent<Renderer>().sharedMaterial=blockMaterial;
   Check(l.view.spatial.hud.GetComponentsInChildren<TMPro.TMP_Text>().All(t=>t.fontSharedMaterial.shader.name.Contains("Overlay")),"HUD text materials draw above geometry");await Shot("03_HUDInFrontOfOccluder");Object.DestroyImmediate(occluder);occluder=null;Object.DestroyImmediate(blockMaterial);blockMaterial=null;
   l.DeveloperJump(1);l.presentation.space.overlay.Advance(2);l.SelectFloor();await Task.Delay(150);await Aim(l.friend.position+Vector3.up*1.02f);Check(c.HoverTargetName==l.friend.name,"Centre ray reaches friend silhouette");var friendGlow=l.friend.GetComponent<ElevatorObservation>().shapeHighlight;Check(friendGlow&&friendGlow.Visible&&friendGlow.skinnedShells.Length>0,"Skinned friend receives matching silhouette glow");await Shot("04_CharacterSilhouette");
   l.Restart();await Task.Delay(120);Teleport(new Vector3(-1.8f,7.95f,-1.8f));
   InputSystem.RegisterLayout("{\"name\":\"InteractionQAStick\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"primary2DAxis\",\"layout\":\"Stick\",\"format\":\"VEC2\",\"offset\":64}]}");
   left=InputSystem.AddDevice("InteractionQAStick");InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);right=InputSystem.AddDevice("InteractionQAStick");InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
   InputSystem.QueueDeltaStateEvent(left.GetChildControl<StickControl>("primary2DAxis"),new Vector2(.9f,0));InputSystem.QueueDeltaStateEvent(right.GetChildControl<StickControl>("primary2DAxis"),new Vector2(-1,0));await Task.Delay(180);
   var action=(InputAction)typeof(ElevatorPlayerMovement).GetField("leftStick",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);var stick=action.ReadValue<Vector2>();Check(stick.x>.5f&&action.activeControl.device==left,"VR action resolves only the left-hand stick");before=m.FeetPosition;for(int i=0;i<40;i++)m.Move(stick,.02f,true);Check(m.FeetPosition.x-before.x>.5f,"VR movement path moves the capsule with left-stick input");InputSystem.RemoveDevice(left);left=null;InputSystem.RemoveDevice(right);right=null;
   l.Restart();await Task.Delay(150);Teleport(new Vector3(-.6f,7.95f,-4.2f),180);await Task.Delay(650);Check(l.presentation.space.sportsCentre.thirdFloorAutoDoorLeft.localPosition.x<-.7f,"3F automatic door opens for an approaching player");
   l.DeveloperJump(3);l.Advance(2);l.presentation.space.overlay.Advance(2);await Key(UnityEngine.InputSystem.Key.Escape);await MenuClick(p.restartButton);Check(!p.IsPaused&&l.Flow.Step==ElevatorStep.Lobby&&l.Flow.History.Count==0&&l.Floor==3,"Pause restart resets scenario and floor");Check(Vector3.Distance(m.FeetPosition,l.presentation.space.sportsCentre.lobby)<.1f,"Restart returns the moving player to the 3F start");
   await Key(UnityEngine.InputSystem.Key.Escape);await Key(UnityEngine.InputSystem.Key.Escape);Check(!p.IsPaused&&Time.timeScale==1,"Escape also resumes the game");
   int count=Time.frameCount;await Task.Delay(250);Check(Time.frameCount>count&&count>frame,"Actual player frames advance");
  }
  catch(Exception e){error=e.ToString();ScreenCapture.CaptureScreenshot(Dir+"Failure.png");}
  finally
  {
   if(p.IsPaused)p.Resume();if(occluder)Object.DestroyImmediate(occluder);if(blockMaterial)Object.DestroyImmediate(blockMaterial);if(left!=null)InputSystem.RemoveDevice(left);if(right!=null)InputSystem.RemoveDevice(right);
   InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);if(mouseEnabled)InputSystem.EnableDevice(originalMouse);if(keyEnabled)InputSystem.EnableDevice(originalKeyboard);originalMouse?.MakeCurrent();originalKeyboard?.MakeCurrent();InputSystem.settings=original;Object.Destroy(temp);Application.runInBackground=background;
  }
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,passed,checks,error,frameStart=frame,frameEnd=Time.frameCount,actualHMD=false,mode="PC Play Mode with actual virtual mouse/keyboard events; virtual left/right XR stick action and capsule movement; no physical HMD",state=l.Snapshot()},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Dir+"InteractionVerification.json",report);return report;
 }
}
