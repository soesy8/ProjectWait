using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class CaptureSmallerBPreview
{
 public static async Task<object> Run()
 {
  var lesson=Object.FindFirstObjectByType<ElevatorLesson>();
  var controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();
  bool enabled=lesson.enabled,controlEnabled=controls.enabled;
  try
  {
   lesson.enabled=false;controls.enabled=false;
   lesson.DeveloperJump(6);controls.SetLookAngles(180,-7);
   for(int i=0;i<88;i++)lesson.Advance(.05f);
   lesson.presentation.space.overlay.Advance(10);
   if(!lesson.view.spatial.DialogueFullyRevealed)lesson.view.spatial.TryReadDialogue();
   await Task.Delay(250);
   const string path="Assets/M/CodexTemp/RescueBHeight_20261104~/Scene_B_Updated.png";
   ScreenCapture.CaptureScreenshot(path);await Task.Delay(250);
   return new{path,exists=File.Exists(path)};
  }
  finally{lesson.Restart();lesson.enabled=enabled;controls.enabled=controlEnabled;}
 }
}
