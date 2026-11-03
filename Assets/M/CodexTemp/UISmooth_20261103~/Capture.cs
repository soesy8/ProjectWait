using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class CaptureSmoothUI
{
 const string Dir="Assets/M/CodexTemp/UISmooth_20261103~/";
 static async Task Shot(string name){ScreenCapture.CaptureScreenshot(Dir+name+".png");await Task.Delay(120);}
 public static async Task<object> Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var m=c.movement;var ui=l.view.spatial;var t=ui.Transitions;
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled,ve=l.view.enabled;
  var frames=new List<object>();
  try
  {
   l.enabled=c.enabled=m.enabled=l.scenario.enabled=l.view.enabled=false;l.Restart();l.presentation.space.overlay.Advance(10);
   var pos=l.friend.position+new Vector3(0,1.1f,-2.5f);m.Controller.enabled=false;c.xrOrigin.position+=pos-l.playerCamera.transform.position;l.playerCamera.transform.position=pos;m.Controller.enabled=true;
   var a=Quaternion.LookRotation(l.friend.position+new Vector3(.3f,1.2f,0)-pos).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));
   ui.Tick();while(ui.TryReadDialogue()){}t.Advance(2);await Task.Delay(40);
   l.view.ShowThought("친구와 함께 안전하게 내려가요.");ui.Tick();
   foreach(var pair in new[]{new[]{.03f,1f},new[]{.07f,2f},new[]{.4f,3f}})
   {
    t.Advance(pair[0]);frames.Add(new{stage="appearance",opacity=l.view.thoughtPanel.GetComponent<CanvasGroup>().alpha,position=l.view.thoughtPanel.localPosition.ToString()});
    await Shot("Smooth_0"+(int)pair[1]+"_Appear");
   }
   l.view.ShowThought("엘리베이터가 멈추면 비상벨로 도움을 요청해요.\n문은 직접 열지 않고 안내를 기다려요.");ui.Tick();
   foreach(var pair in new[]{new[]{.03f,4f},new[]{.06f,5f},new[]{.4f,6f}})
   {
    t.Advance(pair[0]);frames.Add(new{stage="replacement",opacity=l.view.thoughtPanel.GetComponent<CanvasGroup>().alpha,position=l.view.thoughtPanel.localPosition.ToString()});
    await Shot("Smooth_0"+(int)pair[1]+"_Change");
   }
   return new{success=true,frames,activeChoiceRoots=l.view.actionPanel.childCount,currentChoices=l.view.ChoiceButtons.Count};
  }
  finally{l.Restart();l.presentation.space.overlay.Advance(10);ui.Tick();t.Advance(2);l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;l.view.enabled=ve;}
 }
}