using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class CaptureDialogueReadability
{
 static void Save(Camera camera,string name)
 {
  var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes("Assets/M/CodexTemp/DialogueReadability_20261103~/"+name+".png",image.EncodeToPNG());}
  finally{camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
 }
 public static async Task<object> Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var m=c.movement;var ui=l.view.spatial;
  bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled;
  try
  {
   l.enabled=c.enabled=m.enabled=l.scenario.enabled=false;l.Restart();l.presentation.space.overlay.Advance(10);
   for(int i=0;i<2;i++)
   {
    if(i==1){l.DeveloperJump(4);l.ReportSituation();l.presentation.space.overlay.Advance(10);}
    var anchor=i==0?l.friend.position+Vector3.up*1.1f:l.presentation.space.sportsCentre.sideIntercom.position;
    var pos=i==0?l.friend.position+new Vector3(0,1.1f,-2.5f):l.presentation.space.sportsCentre.inside+Vector3.up*1.1f;
    m.Controller.enabled=false;c.xrOrigin.position+=pos-l.playerCamera.transform.position;l.playerCamera.transform.position=pos;m.Controller.enabled=true;
    var a=Quaternion.LookRotation(anchor+Vector3.up*.25f-pos).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));
    while(ui.TryReadDialogue()){}ui.Tick();l.view.ShowThought(i==0?"농구를 마치고 함께 내려가는 친구예요.":"식별판에서 건물명과 승강기 번호를 확인해요.");ui.Tick();Canvas.ForceUpdateCanvases();await Task.Delay(100);
    Save(l.playerCamera,i==0?"LobbySmall":"ManagerSmall");
   }
   return new{success=true};
  }
  finally{l.Restart();l.presentation.space.overlay.Advance(10);l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;}
 }
}