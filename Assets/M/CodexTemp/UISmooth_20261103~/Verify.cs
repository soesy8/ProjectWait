using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class VerifySmoothUI
{
 const string Dir="Assets/M/CodexTemp/UISmooth_20261103~/";
 static readonly List<object> checks=new List<object>();
 static void Check(bool pass,string name){checks.Add(new{name,passed=pass});if(!pass)throw new Exception(name);}
 static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
 public static async Task<object> Main()
 {
  checks.Clear();var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var m=c.movement;var ui=l.view.spatial;var t=ui.Transitions;
  if(!Application.isPlaying||l.Flow==null)throw new Exception("Play Mode required.");
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled,ue=ui.enabled;string error=null;
  try
  {
   l.enabled=c.enabled=m.enabled=l.scenario.enabled=false;ui.enabled=false;l.view.enabled=false;
   l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);
   var camera=l.playerCamera.transform;var player=c.xrOrigin;var position=player.position;var rotation=player.rotation;var look=c.LookAngles;
   Check(t.layoutSeconds>=.15f&&t.layoutSeconds<=.3f,"Panel motion uses a short comfortable duration");
   Check(t.textSeconds>0&&t.textSeconds<.25f,"Text crossfade has a short bounded duration");
   // Fade in and out; all hit filtering changes on request, not at animation completion.
   ui.ShowThought("안전 포스터를 살펴봐요.");var panel=l.view.thoughtPanel;var group=panel.GetComponent<CanvasGroup>();
   Check(panel.gameObject.activeSelf&&group.alpha<.01f,"Observation appearance starts transparent, not as a pop");
   t.Advance(.025f);Check(group.alpha>0&&group.alpha<1,"Observation fade passes through intermediate opacity");
   t.Advance(2);Check(group.alpha>.999f,"Observation fade converges to fully readable opacity");
   ui.ShowThought("");Check(panel.gameObject.activeSelf&&!group.blocksRaycasts&&!group.interactable,"Disappearing panel stays visible briefly but stops accepting input immediately");
   t.Advance(.025f);Check(group.alpha>0&&group.alpha<1,"Observation dismissal fades out smoothly");t.Advance(2);Check(!panel.gameObject.activeSelf,"Fully faded observation panel deactivates");
   // Arbitrary rect movement/resizing/scale share one transition state, even under reentrant layout.
   ui.ShowThought("짧은 관찰 설명이에요.");t.Advance(2);var oldCenter=panel.localPosition;var oldSize=panel.sizeDelta;var oldScale=panel.localScale;
   t.Begin();t.Begin();panel.localPosition+=new Vector3(-120,60,0);panel.sizeDelta+=new Vector2(30,50);panel.localScale*=1.04f;var destination=panel.localPosition;var size=panel.sizeDelta;var scale=panel.localScale;t.End();t.End();
   Check(Vector3.Distance(panel.localPosition,oldCenter)<.01f&&Vector2.Distance(panel.sizeDelta,oldSize)<.01f,"New layout keeps the previous displayed position and size at transition start");
   t.Advance(.025f);Check(Vector3.Distance(panel.localPosition,oldCenter)>.1f&&Vector3.Distance(panel.localPosition,destination)>.1f,"Panel moves through intermediate positions");
   Check(panel.sizeDelta.y>oldSize.y&&panel.sizeDelta.y<size.y,"Panel height interpolates rather than jumping");
   Check(panel.localScale.x>oldScale.x&&panel.localScale.x<scale.x,"Panel scale interpolates with position and size");
   t.Advance(2);Check(Vector3.Distance(panel.localPosition,destination)<.03f&&Vector2.Distance(panel.sizeDelta,size)<.03f,"Panel settles exactly at target geometry");
   // Anchor/pivot changes preserve the center; they do not teleport from one screen side.
   var center=panel.TransformPoint(panel.rect.center);t.Begin();panel.anchorMin=panel.anchorMax=new Vector2(0,.3f);panel.pivot=new Vector2(0,.5f);panel.anchoredPosition=new Vector2(24,0);var targetCenter=panel.TransformPoint(panel.rect.center);t.End();
   Check(Vector3.Distance(center,panel.TransformPoint(panel.rect.center))<.05f,"Changing observation anchors preserves displayed center on the first frame");
   t.Advance(.025f);Check(Vector3.Distance(center,panel.TransformPoint(panel.rect.center))>1&&Vector3.Distance(targetCenter,panel.TransformPoint(panel.rect.center))>1,"Observation anchor transition slides instead of teleporting");t.Advance(2);
   // Crossfades retain old displayed content briefly without delaying the story state.
   l.DeveloperJump(4);l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);var previous=l.view.dialogueText.text;
   position=player.position;rotation=player.rotation;look=c.LookAngles;
   l.ReportSituation();var textGroup=l.view.dialogueText.GetComponent<CanvasGroup>();var ghost=l.view.dialogueText.transform.parent.Find("TransitionText");
   Check(previous!=l.view.dialogueText.text&&ghost&&ghost.GetComponent<TMP_Text>().text==previous,"Changed dialogue retains an outgoing non-interactive text layer");
   Check(textGroup.alpha<.01f&&!ghost.GetComponent<TMP_Text>().raycastTarget,"New dialogue begins transparent and old text cannot intercept selection");
   t.Advance(.025f);Check(textGroup.alpha==0&&ghost.GetComponent<CanvasGroup>().alpha>0&&ghost.GetComponent<CanvasGroup>().alpha<1,"Outgoing sentence fades before new glyphs appear, preventing double text");
   t.Advance(.075f);Check(textGroup.alpha>0&&textGroup.alpha<1&&ghost.GetComponent<CanvasGroup>().alpha==0,"Incoming sentence fades through intermediate opacity without double text");
   var incoming=l.view.dialogueText.text;var alpha=textGroup.alpha;l.Refresh();Check(l.view.dialogueText.text==incoming&&textGroup.alpha>=alpha-.001f,"Identical refresh does not restart text fading");
   t.Advance(2);await Task.Delay(30);Check(textGroup.alpha==1&&!l.view.dialogueText.transform.parent.Find("TransitionText"),"Finished text transition removes its outgoing layer");
   // Changes arriving mid-fade replace the transient layer instead of building up stale text.
   l.FindIdentity();t.Advance(.03f);l.Report();t.Advance(.03f);l.Advice();t.Advance(2);await Task.Delay(30);
   Check(l.view.GetComponentsInChildren<TMP_Text>(true).All(x=>x.name!="TransitionText"),"Rapid dialogue updates leave no old text layers");
   // World speaker anchor and camera controls are not animated/recentered.
   Check(Vector3.Distance(player.position,position)<.001f&&Quaternion.Angle(player.rotation,rotation)<.01f&&Vector2.Distance(c.LookAngles,look)<.01f,"UI transitions never move or turn the player");
   ui.Tick();Check(Vector3.Distance(l.view.transform.position,l.presentation.space.sportsCentre.DialogueAnchor())<.001f,"Speech canvas retains its spatial speaker anchor");
   // Button lifecycle: same label survives refresh; replaced choices fade but are already inert.
   l.DeveloperJump(4);l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);var button=l.view.ChoiceButtons[0];var callbackCount=l.Flow.Revision;
   l.Refresh();Check(l.view.ChoiceButtons[0]==button,"Unchanged choices reuse their button instead of rebuilding/popping");
   var feedback=button.GetComponent<ElevatorSmoothButtonFeedback>();Check(feedback&&!button.GetComponent<StorybookButtonFeedback>(),"M-local smooth feedback replaces abrupt shared button feedback");
   var artwork=button.transform.Find("StorybookArtwork") as RectTransform;var rest=artwork.anchoredPosition;
   feedback.OnPointerEnter(new PointerEventData(EventSystem.current));Invoke(feedback,"LateUpdate");
   Check(artwork.anchoredPosition.y>rest.y&&artwork.anchoredPosition.y<rest.y+1.5f,"Pointer hover lifts button surface through intermediate positions");
   feedback.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});Invoke(feedback,"LateUpdate");
   Check(artwork.localScale.x>.985f&&artwork.localScale.x<1,"Pointer press scales button surface smoothly");
   l.ReportSituation();Check(button&&!button.enabled&&!button.interactable&&button.name.StartsWith("Retiring_"),"Removed choice becomes inert before its fade completes");
   Check(button.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"Retired button graphics cannot block desktop or XR rays");
   int revision=l.Flow.Revision;button.onClick.Invoke();Check(l.Flow.Revision==revision,"Retired button has no stale callback");
   var fade=button.GetComponent<CanvasGroup>();t.Advance(.025f);Check(fade.alpha>0&&fade.alpha<1,"Removed choice fades out smoothly");t.Advance(2);await Task.Delay(30);Check(!button,"Retired choice is cleaned up after its fade");
   // A pause/comfort fade has absolute priority; no smooth transition re-enables gameplay.
   l.DeveloperJump(2);l.UnsafeProposal();ui.Tick();Check(l.CinematicBlocking&&!ui.TryReadDialogue(),"Cinematic interaction lock takes priority over transitions");
   l.pauseMenu.Pause();ui.Tick();Check(l.view.GetComponent<CanvasGroup>().alpha==0,"Pause hides gameplay UI immediately");l.pauseMenu.Resume();
   Check(l.view.dialogueText.fontSize==35&&ui.storybook.thoughtBubblePrefab,"Readability typography and current storybook bubble are preserved");
   l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);await Task.Delay(30);
   Check(!l.view.GetComponentsInChildren<Transform>(true).Any(x=>x.name.StartsWith("Retiring_")||x.name=="TransitionText"),"Restart and convergence leave no transient UI objects");
  }
  catch(Exception e){error=e.ToString();}
  finally
  {if(l.IsPaused)l.pauseMenu.Resume();l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;ui.enabled=ue;l.view.enabled=true;}
  var result=new{success=error==null,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,checks=checks.ToArray(),error,actualHMD=false};File.WriteAllText(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_SmoothVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}