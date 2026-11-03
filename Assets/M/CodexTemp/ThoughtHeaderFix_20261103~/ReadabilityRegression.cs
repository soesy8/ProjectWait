using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class VerifyDialogueReadability
{
 const string Dir="Assets/M/CodexTemp/ThoughtHeaderFix_20261103~/";
 static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPlayerMovement m;static ElevatorSpatialUI ui;
 static readonly List<object> checks=new List<object>();static readonly List<object> samples=new List<object>();
 static void Check(bool pass,string name){checks.Add(new{name,passed=pass});if(!pass)throw new Exception(name);}
 static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
 static void Settle(){l.presentation.space.overlay.Advance(10);ui.Tick();ui.Transitions.Advance(2);Canvas.ForceUpdateCanvases();}
 static void Finish(){for(int i=0;i<50&&ui.TryReadDialogue();i++){}ui.Tick();}
 static float Intersection(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
 static Rect RectOnScreen(RectTransform rect,Camera camera)
 {var corners=new Vector3[4];rect.GetWorldCorners(corners);var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;foreach(var p in corners){var v=RectTransformUtility.WorldToScreenPoint(camera,p);min=Vector2.Min(min,v);max=Vector2.Max(max,v);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
 static void Aim(Vector3 p){var angles=Quaternion.LookRotation(p-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(angles.y,Mathf.DeltaAngle(0,angles.x));}
 static void Place(Vector3 p){m.Controller.enabled=false;c.xrOrigin.position+=p-l.playerCamera.transform.position;l.playerCamera.transform.position=p;m.Controller.enabled=true;Physics.SyncTransforms();}
 static async Task Capture(string name)
 {await Task.Delay(80);ScreenCapture.CaptureScreenshot(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_"+name+".png");await Task.Delay(150);}
 static void InspectDialogue(string name)
 {
  Settle();var text=l.view.dialogueText;text.ForceMeshUpdate();
  Check(text.fontSize==35&&!text.enableAutoSizing,name+": stable 35pt body without auto-shrink");
  Check(text.alignment==TextAlignmentOptions.MidlineLeft&&text.lineSpacing>=9,name+": left alignment and readable leading");
  Check(l.view.dialoguePanel.rect.width==580&&l.view.dialoguePanel.rect.height>=230,name+": expanded panel and stable minimum height");
  Check(text.textInfo.lineCount<=3&&!text.isTextOverflowing,name+": current page stays within three readable lines");
  Check(text.GetPreferredValues(text.text,float.PositiveInfinity,float.PositiveInfinity).x<=text.rectTransform.rect.width+1,name+": explicit Korean word wrapping fits body width");
  foreach(var b in l.view.ChoiceButtons)
  {var t=b.GetComponentInChildren<TMP_Text>();t.ForceMeshUpdate();Check(!t.isTextOverflowing,name+": choice "+b.name+" fits");}
  if(l.Flow.IsCinematic||l.Flow.Step==ElevatorStep.Call)
  {Check(ui.DialogueFullyRevealed,name+": critical speech is immediately readable");Check(ui.DialoguePageCount==1||name.StartsWith("Long speech"),name+": automatic/critical speech fits on a single page");}
  samples.Add(new{name,text=text.text,page=ui.DialoguePage,pages=ui.DialoguePageCount,lines=text.textInfo.lineCount,panel=l.view.dialoguePanel.sizeDelta.ToString()});
 }
 public static async Task<object> Main()
 {
  checks.Clear();samples.Clear();l=Object.FindFirstObjectByType<ElevatorLesson>();
  if(!Application.isPlaying||!l||l.Flow==null)throw new Exception("Enter Play Mode first.");
  c=Object.FindFirstObjectByType<ElevatorDesktopControls>();m=c.movement;ui=l.view.spatial;
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled;var camera=l.playerCamera;float originalAspect=camera.aspect;var hudRect=(RectTransform)ui.hud.transform;
  string error=null;
  try
  {
   l.enabled=c.enabled=m.enabled=l.scenario.enabled=false;l.Restart();Settle();InspectDialogue("Lobby");
   Check(!ui.DialogueFullyRevealed,"Ordinary speech begins with a typewriter reveal");
   float started=ui.DialogueStartedAt;await Task.Delay(180);ui.Tick();int before=l.view.dialogueText.maxVisibleCharacters;
   l.Refresh();ui.Tick();Check(ui.DialogueStartedAt==started&&l.view.dialogueText.maxVisibleCharacters>=before,"Refreshing the same sentence never restarts typing");
   var reader=l.view.dialoguePanel.Find("DialogueReader").GetComponent<Button>();int rev=l.Flow.Revision;
   reader.onClick.Invoke();Check(ui.DialogueFullyRevealed&&l.Flow.Revision==rev,"First dialogue click reveals all text without progressing story");
   Check(!reader.IsInteractable()&&!reader.targetGraphic.raycastTarget,"Finished dialogue no longer intercepts object selection");
   l.Refresh();Check(ui.DialogueFullyRevealed&&ui.DialogueStartedAt==started,"Completed reveal survives identical refresh");
   if(l.view.ChoiceButtons.Count>0){l.view.InvokeChoice(0);Check(l.Flow.Step==ElevatorStep.Entering,"Second selection proceeds after sentence is revealed");}
   else{l.Continue();}
   InspectDialogue("Entering");Check(l.CinematicBlocking&&!ui.TryReadDialogue(),"Cinematic gameplay lock still prevents reading-click progression");
   l.DeveloperJump(1);Settle();InspectDialogue("Boarding destination");l.SelectFloor();InspectDialogue("Ball handoff");l.Continue();Settle();InspectDialogue("Proposal");
   l.ObserveProposal();InspectDialogue("Proposal inspection");
   l.UnsafeProposal();InspectDialogue("Jump");l.Advance(l.settings.friendReplySeconds+l.settings.jumpSeconds+l.settings.afterJumpSeconds+.1f);InspectDialogue("Stopped reaction");
   l.Advance(2);Settle();InspectDialogue("Stopped investigation");Finish();float hintStart=ui.DialogueStartedAt;
   l.Advance(l.settings.firstHintSeconds+1);Settle();InspectDialogue("Stopped hint");
   if(l.scenario.objectGuidedScene)Check(ui.DialogueStartedAt==hintStart&&ui.DialogueFullyRevealed,"Hint-only flow revision preserves already-read friend speech");
   l.Intercom();InspectDialogue("Connecting");l.Advance(l.settings.connectionSeconds+.1f);Settle();InspectDialogue("Manager connected");
   l.ReportSituation();InspectDialogue("Manager requests identity");float managerStart=ui.DialogueStartedAt;
   l.Advance(l.settings.secondHintSeconds+1);Settle();Check(ui.DialogueStartedAt==managerStart&&ui.DialogueFullyRevealed,"Search hint changes do not restart management speech");
   l.FindIdentity();InspectDialogue("Identity found");l.Report();InspectDialogue("Waiting instructions");
   l.Advice();Settle();InspectDialogue("Friend reassurance");l.Reassure();InspectDialogue("Waiting fade");l.Advance(4.9f);InspectDialogue("Rescuer checking");l.Advance(l.settings.rescueSubtitleSeconds+.1f);Settle();InspectDialogue("Rescuer ready");
   l.Continue();InspectDialogue("Exit");l.Advance(l.settings.exitSeconds+.1f);Settle();InspectDialogue("Stair advice");l.Continue();InspectDialogue("Stairs");l.Advance(3.6f);Settle();InspectDialogue("Ground floor");
   l.Continue();InspectDialogue("Going home");l.Advance(6.1f);Settle();InspectDialogue("Completion");
   l.DeveloperJump(3);l.simulateNoResponse=true;l.Advance(2);Settle();l.Intercom();l.Advance(l.settings.connectionSeconds+.1f);Settle();InspectDialogue("No answer");l.Phone();InspectDialogue("119 simulation");l.simulateNoResponse=false;
   l.DeveloperJump(2);l.UnsafeDoor();Settle();InspectDialogue("Safety retry");
   // Long content is paged, not made smaller or cut off. Existing choices wait for reading.
   l.DeveloperJump(4);Settle();string longText="문 밖으로 혼자 나오지 마세요. 안전을 확인할 때까지 안에서 기다려 주세요.\n관리실에서 직원에게 상황을 전했어요. 문을 직접 열려고 하지 마세요.\n직원이 도착하면 바닥 높이를 확인해요. 안내에 따라 천천히 이동해 주세요.";
   l.view.dialogueText.text=longText;ui.Present("관리실 · 통화 중",longText,"");InspectDialogue("Long speech first page");
   Check(ui.DialoguePageCount>1,"Long dialogue becomes multiple pages at full font size");int page=ui.DialoguePage;rev=l.Flow.Revision;
   l.view.InvokeChoice(0);Check(ui.DialoguePage==page+1&&l.Flow.Revision==rev,"Dialogue choice reads next page before executing its action");
   var current=l.view.dialogueText.text;l.view.dialogueText.text=longText;ui.Present("관리실 · 통화 중",longText,"");
   Check(ui.DialoguePage==page+1&&l.view.dialogueText.text==current,"Refreshing long content preserves current page");
   Finish();l.view.InvokeChoice(0);Check(l.Flow.SituationReported,"Action executes after final dialogue page is read");
   // Text measurement remains identical to the actual visual size.
   var pages=ElevatorDialogueText.Pages(l.view.dialogueText,"친구가 많이 놀랐구나. 내가 옆에 있어 줘야겠어.");
   Check(pages.All(p=>p.Split('\n').Length<=3)&&!pages.Any(p=>p.Contains("\n구예요")||p.Contains("\n어요")),"Korean wrapping avoids orphaned word endings");
   // Content collision placement changes only HUD, never the speaker or camera.
   l.Restart();Settle();Place(l.friend.position+new Vector3(0,1.1f,-2.5f));Aim(l.friend.position+new Vector3(.25f,1.15f,0));ui.Tick();Finish();
   l.view.ShowThought("농구를 마치고 함께 내려가는 친구예요.");ui.Tick();ui.Transitions.Advance(2);Canvas.ForceUpdateCanvases();
   var thought=RectOnScreen(l.view.thoughtPanel,null);var speech=RectOnScreen(l.view.dialoguePanel,camera);
   float overlap=Intersection(thought,speech);Check(overlap<1,"Observation bubble avoids projected dialogue panel");
   foreach(var b in l.view.ChoiceButtons)Check(Intersection(thought,RectOnScreen((RectTransform)b.transform,camera))<1,"Observation bubble avoids choice "+b.name);
   var bubble=l.view.thoughtPanel.Find("CurrentThoughtBubble");Check(!bubble.Find("Heading").gameObject.activeSelf&&!bubble.Find("ThoughtRibbon").gameObject.activeSelf,"Observation heading/ribbon remain removed");
   var p=c.transform.position;var angles=c.LookAngles;var anchor=l.view.transform.position;
   ui.Tick();Check(Vector3.Distance(p,c.transform.position)<.0001f&&Vector2.Distance(angles,c.LookAngles)<.001f&&Vector3.Distance(anchor,l.view.transform.position)<.0001f,"Readability layout never repositions player or speech anchor");
   Aim(l.friend.position+Vector3.up*1.25f);Invoke(c,"UpdateCenterRay");Check(c.HoverTargetName==l.friend.name,"Expanded dialogue remains clear of friend's face selection ray");
   await Capture("01_LobbyReadable");
   // Stable size at distance, with a cap instead of unbounded screen following.
   float initialScale=l.view.transform.localScale.x;Place(l.friend.position+new Vector3(0,1.1f,-6));ui.Tick();
   Check(l.view.transform.localScale.x>=initialScale&&l.view.transform.localScale.x<=ui.worldPixelSize*ui.maximumReadabilityScale+.00001f,"Distant dialogue scale is bounded and never shrinks text");
   Place(mapInside()+Vector3.up*1.1f);l.DeveloperJump(4);Settle();l.ReportSituation();Aim(l.presentation.space.sportsCentre.sideIntercom.position+Vector3.up*.35f);ui.Tick();l.view.ShowThought("식별판에서 건물명과 승강기 번호를 확인해요.");ui.Tick();await Capture("02_ManagerReadable");
   // Aspect reflow is exercised without changing scene assets or project settings.
   foreach(float aspect in new[]{16f/9,4f/3,9f/16})
   {camera.aspect=aspect;ui.Layout();ui.Tick();Check(l.view.dialogueText.fontSize==35&&!l.view.dialogueText.isTextOverflowing,"Aspect "+aspect.ToString("F2")+" preserves body font and fit");}
   camera.aspect=originalAspect;ui.Layout();
   var fill=l.view.dialoguePanel.Find("ReadingPaper").GetComponent<Image>();
   Check(fill.color.a==1&&!fill.sprite&&!fill.raycastTarget,"Flat opaque cream reading area retains frame but removes text-background shading");
   int paperIndex=fill.transform.GetSiblingIndex();for(int i=0;i<4;i++)ui.Layout();
   Check(fill.transform.GetSiblingIndex()<l.view.dialogueText.transform.GetSiblingIndex(),"Repeated reflow always keeps reading paper behind text");
  }
  catch(Exception e){error=e.ToString();}
  finally
  {camera.aspect=originalAspect;l.simulateNoResponse=false;if(l.IsPaused)l.pauseMenu.Resume();c.ClearTarget();l.Restart();Settle();l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;}
  var result=new{success=error==null,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,checks=checks.ToArray(),samples=samples.ToArray(),error,actualHMD=false};
  File.WriteAllText(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 static Vector3 mapInside()=>l.presentation.space.sportsCentre.inside;
}