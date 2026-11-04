using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class DebugPosterVisual
{
 static object Graphic(ElevatorPosterCueGraphic g)
 {
  var m=g.canvasRenderer.GetMesh();
  return new{g.name,active=g.isActiveAndEnabled,renderer=g.canvasRenderer!=null,g.canvasRenderer.cull,g.canvasRenderer.absoluteDepth,g.canvasRenderer.materialCount,alpha=g.canvasRenderer.GetInheritedAlpha(),rect=g.rectTransform.rect.ToString(),scale=g.transform.lossyScale.ToString(),vertices=m?m.vertexCount:0,triangles=m?m.triangles.Length:0,material=g.materialForRendering.name,shader=g.materialForRendering.shader.name};
 }
 public static async Task<object> Run()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();
  l.DeveloperJump(2);l.Advance(l.settings.shotStorySeconds+.1f);c.SetLookAngles(180,-5);await Task.Delay(200);
  var g=l.posterCutscene;g.ringGraphic.SetVerticesDirty();g.arrow.GetComponent<ElevatorPosterCueGraphic>().SetVerticesDirty();Canvas.ForceUpdateCanvases();
  await Task.Delay(150);
  var result=new{arrow=Graphic(g.arrow.GetComponent<ElevatorPosterCueGraphic>()),ring=Graphic(g.ringGraphic),renderMode=g.worldCanvas.renderMode.ToString(),camera=l.playerCamera.transform.position.ToString(),target=g.TargetPosition.ToString(),anchor=g.arrow.anchoredPosition.ToString(),pixelRect=l.playerCamera.pixelRect.ToString()};
  ScreenCapture.CaptureScreenshot("Assets/M/CodexTemp/PosterCutscene_20261104~/DebugGraphic.png");return result;
 }
}
