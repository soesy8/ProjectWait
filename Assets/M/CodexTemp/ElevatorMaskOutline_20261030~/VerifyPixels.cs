using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class VerifyPixelsMO
{
 const string D="Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/";
 static List<object> checks=new List<object>();static void Check(bool ok,string name){checks.Add(new{name,passed=ok});if(!ok)throw new Exception(name);}
 static async Task<Texture2D> Capture(string name){await Task.Delay(220);await Awaitable.EndOfFrameAsync();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(D+name+".png",t.EncodeToPNG());return t;}
 public static async Task<string> Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var camera=l.playerCamera;var controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();var movement=Object.FindFirstObjectByType<ElevatorPlayerMovement>();
  var pos=camera.transform.position;var rot=camera.transform.rotation;var flags=camera.clearFlags;var bg=camera.backgroundColor;var layers=camera.cullingMask;float far=camera.farClipPlane,near=camera.nearClipPlane;bool ce=controls.enabled,me=movement.enabled;
  var additional=camera.GetUniversalAdditionalCameraData();bool post=additional.renderPostProcessing;var aa=additional.antialiasing;
  var canvases=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==l.gameObject.scene&&c.gameObject.activeSelf).ToArray();
  var root=new GameObject("MaskOutline_PixelFixture");var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",Color.black);var items=new List<Renderer>();var created=new List<Object>();string error=null;checks.Clear();int inside=0,outside=0,coloured=0,occluded=0;float fps=0;
  try
  {
   l.Restart();l.presentation.space.overlay.Advance(2);await Task.Delay(180);controls.enabled=false;movement.enabled=false;foreach(var c in canvases)c.gameObject.SetActive(false);
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;camera.nearClipPlane=.1f;camera.farClipPlane=20;additional.renderPostProcessing=false;additional.antialiasing=AntialiasingMode.None;camera.transform.SetPositionAndRotation(new Vector3(1000,1000,1000),Quaternion.identity);
   for(int i=0;i<3;i++){var part=GameObject.CreatePrimitive(i==2?PrimitiveType.Sphere:PrimitiveType.Cube);part.name="Mask compound part "+i;part.transform.SetParent(root.transform);part.layer=31;part.transform.position=new Vector3(1000+(i==0?-.38f:i==1?.38f:0),1000+(i==2?.70f:0),1005);part.transform.localScale=i==2?new Vector3(.9f,.9f,.9f):new Vector3(.9f,1.1f,.8f);part.GetComponent<Renderer>().sharedMaterial=mat;items.Add(part.GetComponent<Renderer>());}
   var target=root.AddComponent<ElevatorOutlineTarget>();target.geometry=items.Select(r=>new ElevatorOutlineGeometry{sourceRenderer=r}).ToArray();target.SetPresentation(true,true);ElevatorSilhouetteOutlineFeature.DebugTarget=target;ElevatorSilhouetteOutlineFeature.DebugMask=true;
   var mask=await Capture("Pixel_01_CombinedWhiteMask");created.Add(mask);var maskPixels=mask.GetPixels32();Check(maskPixels.Count(p=>p.r>240)>2000,"Several separate meshes form one white coverage mask");
   ElevatorSilhouetteOutlineFeature.DebugMask=false;var outline=await Capture("Pixel_02_OutsideOutline");created.Add(outline);var image=outline.GetPixels32();Check(image.Length==maskPixels.Length,"Mask and final output share screen dimensions");
   for(int y=2;y<mask.height-2;y++)for(int x=2;x<mask.width-2;x++)
   {
    int i=y*mask.width+x;bool cyan=image[i].b>35&&image[i].b>image[i].r*1.25f;
    if(cyan){coloured++;if(maskPixels[i].r>240)inside++;else outside++;}
   }
   Check(coloured>1000,"A strong outline is rendered around the grouped silhouette");Check(inside<=Mathf.Max(5,coloured/500),"At least 99.8 percent of outline pixels lie outside the original object mask; no internal surface lines");
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Opaque occlusion fixture";wall.layer=31;wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(1000,1000,1003);wall.transform.localScale=new Vector3(6,6,.2f);wall.GetComponent<Renderer>().sharedMaterial=mat;
   var hidden=await Capture("Pixel_03_FullyOccluded");created.Add(hidden);occluded=hidden.GetPixels32().Count(p=>p.b>35&&p.b>p.r*1.25f);Check(occluded==0,"A fully occluded target has no outline through the wall");
   wall.transform.position=new Vector3(998.5f,1000,1003);wall.transform.localScale=new Vector3(3,6,.2f);var partial=await Capture("Pixel_04_PartiallyOccluded");created.Add(partial);var pp=partial.GetPixels32();int wallBlue=0,visibleBlue=0;for(int y=0;y<partial.height;y++)for(int x=0;x<partial.width;x++){var p=pp[y*partial.width+x];if(p.b<=35||p.b<=p.r*1.25f)continue;if(x<partial.width/2-10)wallBlue++;else visibleBlue++;}Check(wallBlue==0&&visibleBlue>200,"Partial occlusion keeps the visible contour without drawing on the foreground wall");
   Check(ElevatorSilhouetteOutlineFeature.LastTargetCount==1,"Validation executed the new Render Graph feature with one grouped target");
   int start=Time.frameCount;await Task.Delay(1500);fps=(Time.frameCount-start)/1.5f;
  }
  catch(Exception e){error=e.ToString();}
  finally
  {
   ElevatorSilhouetteOutlineFeature.DebugMask=false;ElevatorSilhouetteOutlineFeature.DebugTarget=null;Object.Destroy(root);Object.Destroy(mat);foreach(var item in created)if(item)Object.Destroy(item);
   camera.transform.SetPositionAndRotation(pos,rot);camera.clearFlags=flags;camera.backgroundColor=bg;camera.cullingMask=layers;camera.nearClipPlane=near;camera.farClipPlane=far;additional.renderPostProcessing=post;additional.antialiasing=aa;foreach(var c in canvases)if(c)c.gameObject.SetActive(true);controls.enabled=ce;movement.enabled=me;
  }
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,checks,coloured,inside,outside,occluded,fixtureFPS=fps,error},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(D+"PixelVerification.json",report);return report;
 }
}