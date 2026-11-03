using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class CapturePolishReview
{
 static void Save(Camera camera,string name)
 {
  var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes("Assets/M/CodexTemp/InteractionPolish_20261103~/"+name+".png",image.EncodeToPNG());}
  finally{camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
 }
 public static async Task<object> Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var m=c.movement;
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled;
  try
  {
   l.enabled=c.enabled=m.enabled=l.scenario.enabled=false;l.DeveloperJump(4);l.presentation.space.overlay.Advance(10);
   m.Controller.enabled=false;var pos=l.friend.position+new Vector3(0,.85f,-1.5f);c.xrOrigin.position+=pos-l.playerCamera.transform.position;l.playerCamera.transform.position=pos;m.Controller.enabled=true;
   var a=Quaternion.LookRotation(l.friend.position+new Vector3(.05f,.69f,0)-pos).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));
   l.view.ShowThought("");await Task.Delay(150);Save(l.playerCamera,"PocketReviewSmall");
   l.ReportSituation();l.view.spatial.ShowThought("친구 주머니의 휴대폰으로 도움을 요청할 수 있어요.");
   var map=l.presentation.space.sportsCentre;pos=map.inside+new Vector3(0,1.1f,0);m.Controller.enabled=false;c.xrOrigin.position+=pos-l.playerCamera.transform.position;l.playerCamera.transform.position=pos;m.Controller.enabled=true;
   a=Quaternion.LookRotation(map.sideIntercom.position+Vector3.up*.25f-pos).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));l.view.spatial.Tick();await Task.Delay(200);Save(l.playerCamera,"IntercomAndDescriptionReviewSmall");
   return new{success=true};
  }
  finally{l.Restart();l.presentation.space.overlay.Advance(10);l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;}
 }
}