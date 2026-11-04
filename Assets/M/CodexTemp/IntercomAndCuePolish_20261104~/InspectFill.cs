using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectFilledCircle
{
 public static async Task<object> Run()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var g=l.posterCutscene;
  try
  {
   l.DeveloperJump(2);l.Advance(l.settings.shotStorySeconds+.1f);
   var r=Quaternion.LookRotation(g.TargetPosition-l.playerCamera.transform.position).eulerAngles;c.SetLookAngles(r.y,Mathf.DeltaAngle(0,r.x));
   float end=Time.realtimeSinceStartup+3;
   while(l.Flow.AwaitingPoster&&Time.realtimeSinceStartup<end)await Task.Delay(10);
   return new{step=l.Flow.Step.ToString(),g.RingFading,g.RingAlpha,active=g.worldCanvas.gameObject.activeSelf,progress=g.ringGraphic.progress};
  }
  finally{l.Restart();}
 }
}
