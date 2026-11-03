using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyFadeSkybox
{
 const string Dir="Assets/M/CodexTemp/ThoughtHeaderFix_20261103~/";
 static readonly List<object> checks=new List<object>();
 static void Check(bool pass,string name){checks.Add(new{name,passed=pass});if(!pass)throw new Exception(name);}
 static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
 static async Task Shot(string name){ScreenCapture.CaptureScreenshot(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_"+name+".png");await Task.Delay(100);}
 public static async Task<object> Main()
 {
  checks.Clear();var l=Object.FindFirstObjectByType<ElevatorLesson>();
  if(!Application.isPlaying||!l||l.Flow==null)throw new Exception("Enter Play Mode first.");
  var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var m=c.movement;var ui=l.view.spatial;var t=ui.Transitions;
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled,ve=l.view.enabled;string error=null;
  try
  {
   l.enabled=c.enabled=m.enabled=l.scenario.enabled=l.view.enabled=false;l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);
   var pos=l.friend.position+new Vector3(0,1.1f,-2.5f);m.Controller.enabled=false;c.xrOrigin.position+=pos-l.playerCamera.transform.position;l.playerCamera.transform.position=pos;m.Controller.enabled=true;
   var a=Quaternion.LookRotation(l.friend.position+new Vector3(.3f,1.2f,0)-pos).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));ui.Tick();
   ui.ShowThought("친구와 함께 안전하게 내려가요.");ui.Tick();var panel=l.view.thoughtPanel;var group=panel.GetComponent<CanvasGroup>();var p=panel.localPosition;var size=panel.sizeDelta;
   Check(panel.gameObject.activeSelf&&group.alpha<1,"Observation appears with opacity, not a position/scale animation");
   t.Advance(.025f);Check(group.alpha>0&&group.alpha<1,"Opacity reveal passes through Lerp intermediate values");
   Check(Vector3.Distance(p,panel.localPosition)<.001f&&Vector2.Distance(size,panel.sizeDelta)<.001f,"Fade does not move or resize observation panel");t.Advance(2);
   var anchor=panel.anchorMin;var pivot=panel.pivot;
   for(int i=0;i<12;i++){c.SetLookAngles(c.LookAngles.x+11,c.LookAngles.y);ui.Tick();}
   Check(panel.anchorMin==anchor&&panel.pivot==pivot&&Vector3.Distance(p,panel.localPosition)<.01f,"Looking around never relocates a visible observation bubble");
   c.xrOrigin.position+=Vector3.right*.25f;l.playerCamera.transform.position+=Vector3.right*.25f;ui.Tick();
   Check(panel.anchorMin==anchor&&Vector3.Distance(p,panel.localPosition)<.01f,"Walking does not make observation UI chase empty screen space");
   var dialoguePos=l.view.dialoguePanel.anchoredPosition;var dialogueRot=l.view.transform.rotation;var dialogueScale=l.view.transform.localScale;
   for(int i=0;i<6;i++){c.SetLookAngles(c.LookAngles.x-13,c.LookAngles.y);ui.Tick();}
   Check(Vector2.Distance(dialoguePos,l.view.dialoguePanel.anchoredPosition)<.01f,"Friend card does not shift as gaze changes");
   Check(Quaternion.Angle(dialogueRot,l.view.transform.rotation)<.01f&&Vector3.Distance(dialogueScale,l.view.transform.localScale)<.00001f,"World dialogue has no per-frame turn or zoom response");
   ui.ShowThought("비상통화로 도움을 요청하고 안내를 기다려요.");ui.Tick();Check(panel.anchorMin==anchor&&Vector3.Distance(p,panel.localPosition)<.01f,"Different object descriptions reuse the same stable HUD position");
   Check(group.alpha<1,"Content replacement fades at the same position");t.Advance(2);
   ui.ShowThought("");Check(!group.interactable&&!group.blocksRaycasts,"Dismissal blocks input immediately");t.Advance(.025f);Check(group.alpha>0&&group.alpha<1,"Dismissal uses intermediate opacity");t.Advance(2);Check(!panel.gameObject.activeSelf,"Fully faded description panel deactivates");
   var values=new[]{"안전 포스터를 살펴봐요.","친구 주머니의 휴대폰이에요.","전면 패널의 비상벨이에요.","식별판에서 승강기 번호를 확인해요."};
   foreach(var value in values)
   {
    // Exercise both mission title and observation, including maliciously reactivated legacy UI.
    ui.ShowThought("");ui.Layout();t.Advance(.02f);
    var legacy=l.view.thoughtPanel.Find("ThoughtHeading");if(legacy){legacy.gameObject.SetActive(true);legacy.GetComponent<TMP_Text>().text="살펴보니";}
    var legacyRibbon=l.view.thoughtPanel.Find("ThoughtRibbon");if(legacyRibbon)legacyRibbon.gameObject.SetActive(true);
    ui.ShowThought(value);ui.Tick();t.Advance(.025f);
    var bubble=panel.Find("CurrentThoughtBubble");
    Check(bubble&&!bubble.Find("Heading").gameObject.activeSelf&&!bubble.Find("ThoughtRibbon").gameObject.activeSelf,value+": current observation title and ribbon stay hidden");
    Check(!legacy||!legacy.gameObject.activeSelf,value+": legacy heading is forcibly retired");
    Check(!legacyRibbon||!legacyRibbon.gameObject.activeSelf,value+": legacy decorative panel stays hidden");
    Check(!panel.GetComponentsInChildren<TMP_Text>(true).Any(x=>x.gameObject.activeInHierarchy&&x.text.Contains("살펴보니")),value+": no live or ghost '살펴보니' text remains");
    t.Advance(2);
   }
   Check(l.scenario.hints.All(h=>!h||!h.gameObject.activeSelf&&(!h.group||h.group.alpha==0)),"Legacy world-description panels are disabled in the saved scene");
   // A generic layout change is applied once, never continued as a visible slide by Advance.
   t.Begin();panel.localPosition+=new Vector3(35,-12,0);panel.sizeDelta+=new Vector2(20,10);panel.localScale*=1.02f;t.End();
   var once=panel.localPosition;var onceSize=panel.sizeDelta;var onceScale=panel.localScale;
   t.Advance(.02f);t.Advance(.05f);t.Advance(.2f);
   Check(Vector3.Distance(once,panel.localPosition)<.001f&&Vector2.Distance(onceSize,panel.sizeDelta)<.001f&&Vector3.Distance(onceScale,panel.localScale)<.00001f,"Transition driver never interpolates geometry");
   // Button feedback remains visually fixed while native ColorTint still eases color.
   l.DeveloperJump(4);l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);var button=l.view.ChoiceButtons[0];var art=button.transform.Find("StorybookArtwork") as RectTransform;
   var artPos=art.anchoredPosition;var artScale=art.localScale;var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
   ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);await Task.Delay(80);
   Check(art.anchoredPosition==artPos&&art.localScale==artScale,"Hover and press no longer lift or zoom button artwork");
   Check(button.transition==Selectable.Transition.ColorTint&&button.colors.fadeDuration>0,"Button still gives smoothly fading color feedback");ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
   // Typing and stale callback protection are not replaced by UI fades.
   l.ReportSituation();int revision=l.Flow.Revision;Check(!button.enabled&&!button.interactable,"Outgoing choice is immediately inert");button.onClick.Invoke();Check(l.Flow.Revision==revision,"Outgoing choice has no stale callback");
   l.Refresh();Check(ui.DialogueFullyRevealed,"Manager instructions remain fully revealed despite fade-only presentation");
   l.pauseMenu.Pause();ui.Tick();Check(l.view.GetComponent<CanvasGroup>().alpha==0,"Pause still hides gameplay UI immediately");l.pauseMenu.Resume();
   var sky=RenderSettings.skybox;
   Check(sky&&AssetDatabase.GetAssetPath(sky)=="Assets/M/Arts/Materials/Elevator/StorybookCitySkybox.mat","Scene uses the formal M panoramic skybox material");
   Check(sky.shader.name=="Skybox/Panoramic"&&!ShaderUtil.ShaderHasError(sky.shader),"Panoramic skybox shader compiles");
   Check(AssetDatabase.GetAssetPath(sky.GetTexture("_MainTex"))=="Assets/M/Arts/Textures/Skybox_Storybook_City_1024.png","Skybox references the actual existing panorama, not stale preview GUID");
   Check(l.playerCamera.clearFlags==CameraClearFlags.Skybox,"Gameplay camera draws the panorama background");
   Check(RenderSettings.ambientMode==UnityEngine.Rendering.AmbientMode.Trilight,"Authored toon environment lighting is preserved");
   l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);ui.ShowThought("친구와 함께 안전하게 내려가요.");ui.Tick();t.Advance(.03f);await Shot("01_OpacityFade");t.Advance(2);await Shot("02_StableDescription");
   c.SetLookAngles(90,-15);ui.Tick();await Shot("03_PanoramaInScene");
  }
  catch(Exception e){error=e.ToString();}
  finally{if(l.IsPaused)l.pauseMenu.Resume();c.ClearTarget();l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;l.view.enabled=ve;}
  var result=new{success=error==null,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,checks=checks.ToArray(),error,actualHMD=false};
  File.WriteAllText(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_FadeSkyboxVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}