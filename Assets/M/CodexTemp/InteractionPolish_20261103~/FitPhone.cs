using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class FitPocketPhone
{
 public static async Task<object> Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var m=c.movement;
  l.enabled=false;c.enabled=false;m.enabled=false;l.scenario.enabled=false;l.Restart();l.presentation.space.overlay.Advance(10);
  var p=l.CollectObservations().First(o=>o.clue==ElevatorClue.Phone);p.outlineTarget.SetVisible(false);
  p.transform.position=l.friend.TransformPoint(new Vector3(.13f,.58f,-.155f));p.transform.rotation=l.friend.rotation*Quaternion.Euler(0,-90,0)*Quaternion.Euler(0,0,5);
  var body=p.transform.Find("PhoneBody");body.localPosition=new Vector3(0,.045f,0);body.localScale=new Vector3(.025f,.060f,.10f);
  var screen=p.transform.Find("PhoneScreen");screen.localPosition=new Vector3(-.016f,.055f,0);screen.localScale=new Vector3(.006f,.022f,.075f);
  p.GetComponent<BoxCollider>().center=new Vector3(-.10f,.045f,0);p.GetComponent<BoxCollider>().size=new Vector3(.10f,.08f,.11f);
  l.presentation.friendBall.gameObject.SetActive(false);
  m.Controller.enabled=false;c.xrOrigin.position+=l.friend.position+new Vector3(0,.78f,-1.25f)-l.playerCamera.transform.position;l.playerCamera.transform.position=l.friend.position+new Vector3(0,.78f,-1.25f);m.Controller.enabled=true;
  var a=Quaternion.LookRotation(l.friend.position+new Vector3(.08f,.60f,0)-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(a.y,Mathf.DeltaAngle(0,a.x));
  await Task.Delay(150);ScreenCapture.CaptureScreenshot("Assets/M/CodexTemp/InteractionPolish_20261103~/PocketFit.png");await Task.Delay(200);
  var camera=l.playerCamera;var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();System.IO.File.WriteAllBytes("Assets/M/CodexTemp/InteractionPolish_20261103~/PocketFitSmall.png",image.EncodeToPNG());}
  finally{camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
  var ray=new Ray(l.playerCamera.transform.position,(p.GetComponent<BoxCollider>().bounds.center-l.playerCamera.transform.position).normalized);
  return new{phone=l.friend.InverseTransformPoint(p.transform.position).ToString(),hits=Physics.RaycastAll(ray,5).OrderBy(h=>h.distance).Select(h=>new{h.collider.name,h.distance}).ToArray()};
 }
}