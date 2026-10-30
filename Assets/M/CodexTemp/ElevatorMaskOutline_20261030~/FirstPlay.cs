using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class FirstPlayMO
{
 public static async Task<string> Main()
 {
  await Task.Delay(600);var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();l.Restart();l.presentation.space.overlay.Advance(2);await Task.Delay(250);
  var f=l.CollectObservations().First(o=>o.clue==ElevatorClue.Friend&&o.gameObject.activeInHierarchy);var p=f.GetComponent<Collider>().bounds.center+Vector3.up*.4f;var angle=Quaternion.LookRotation(p-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(angle.y,Mathf.DeltaAngle(0,angle.x));await Task.Delay(300);
  await Awaitable.EndOfFrameAsync();ScreenCapture.CaptureScreenshot("Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/FirstMaskOutline.png");await Task.Delay(150);
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{frame=Time.frameCount,featureFrame=ElevatorSilhouetteOutlineFeature.LastFrame,drawn=ElevatorSilhouetteOutlineFeature.LastTargetCount,targets=ElevatorOutlineTarget.Active.Select(t=>new{t.name,t.Visible,t.Hovered,geometry=t.geometry.Length}).ToArray()},Newtonsoft.Json.Formatting.Indented);File.WriteAllText("Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/FirstPlay.json",report);return report;
 }
}