using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace ProjectWait.M.Elevator
{
 public sealed class ElevatorOptimizedOutlineFeature:ScriptableRendererFeature
 {
  public Shader maskShader,distanceShader;
  Material maskMaterial,distanceMaterial;OutlinePass pass;
  public static int LastFrame{get;private set;}public static int LastTargetCount{get;private set;}
  public static bool LastMaskFadeEnabled{get;private set;}public static float LastMaskFadeProgress{get;private set;}
#if UNITY_EDITOR
  public static ElevatorOutlineTarget DebugTarget;public static bool DebugMask;
#endif
  public override void Create()
  {
   CoreUtils.Destroy(maskMaterial);CoreUtils.Destroy(distanceMaterial);maskMaterial=distanceMaterial=null;pass=null;
   if(!maskShader)maskShader=Shader.Find("ProjectWait/M/Scene Outline Coverage");if(!distanceShader)distanceShader=Shader.Find("ProjectWait/M/Outline Distance");
   if(!maskShader||!distanceShader)return;maskMaterial=CoreUtils.CreateEngineMaterial(maskShader);distanceMaterial=CoreUtils.CreateEngineMaterial(distanceShader);pass=new OutlinePass(maskMaterial,distanceMaterial);
  }
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
  {
   if(pass==null||renderingData.cameraData.cameraType!=CameraType.Game||renderingData.cameraData.renderType!=CameraRenderType.Base)return;
   foreach(var target in ElevatorOutlineTarget.Active)if(target&&target.Visible&&target.geometry!=null&&target.geometry.Length>0){renderer.EnqueuePass(pass);break;}
  }
  protected override void Dispose(bool disposing){CoreUtils.Destroy(maskMaterial);CoreUtils.Destroy(distanceMaterial);maskMaterial=distanceMaterial=null;pass=null;}
  sealed class OutlinePass:ScriptableRenderPass
  {
   readonly List<ElevatorOutlineTarget> targets=new List<ElevatorOutlineTarget>();
   readonly Material maskMaterial,distanceMaterial;readonly List<MaterialPropertyBlock> blocks=new List<MaterialPropertyBlock>();int blockIndex;
   // Preserve the authored pixel widths at 2 metres, 1080p and the scene's 68-degree FOV.
   // Convert them to world units so their proportion to the object stays constant.
   const float WorldUnitsPerWidthUnit=4f*.6745085f/1080f;
   // Printed surfaces sit slightly in front of their authored frames.
   const float DepthTolerance=.06f;
   static readonly int SourceID=Shader.PropertyToID("_OutlineSource"),MaskID=Shader.PropertyToID("_OutlineOriginalMask"),SizeID=Shader.PropertyToID("_OutlineFullSize"),SourceSizeID=Shader.PropertyToID("_OutlineSourceSize"),StepID=Shader.PropertyToID("_OutlineStep"),ColourID=Shader.PropertyToID("_OutlineTint"),WidthsID=Shader.PropertyToID("_OutlineWidths");
   public OutlinePass(Material mask,Material distance){maskMaterial=mask;distanceMaterial=distance;renderPassEvent=RenderPassEvent.BeforeRenderingTransparents;ConfigureInput(ScriptableRenderPassInput.Depth);}
   MaterialPropertyBlock Block(){if(blockIndex==blocks.Count)blocks.Add(new MaterialPropertyBlock());var b=blocks[blockIndex++];b.Clear();return b;}
   sealed class MaskData{public ElevatorOutlineGeometry[] geometry;public Material material;public Rect rect;}
   sealed class BlitData{public TextureHandle source,mask;public Material material;public MaterialPropertyBlock properties;public int shaderPass;public Rect rect;}
   static Rect Pixels(Rect normalized,int width,int height){float x0=Mathf.Floor(normalized.xMin*width),y0=Mathf.Floor(normalized.yMin*height);return new Rect(x0,y0,Mathf.Ceil(normalized.xMax*width)-x0,Mathf.Ceil(normalized.yMax*height)-y0);}
   static bool Region(ElevatorOutlineTarget target,Camera camera,float projectionScale,int width,int height,out Rect rect,out float radius)
   {
    var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;bool any=false,near=false;float nearestDepth=float.MaxValue;
    foreach(var g in target.geometry)
    {
     if(g==null||!g.IsValid)continue;var b=g.WorldBounds;
     int eyes=camera.stereoEnabled?2:1;
     for(int eye=0;eye<eyes;eye++)for(int i=0;i<8;i++)
     {
      var world=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
      var p=camera.WorldToViewportPoint(world,eyes==1?Camera.MonoOrStereoscopicEye.Mono:eye==0?Camera.MonoOrStereoscopicEye.Left:Camera.MonoOrStereoscopicEye.Right);
      nearestDepth=Mathf.Min(nearestDepth,p.z);
      if(p.z<=camera.nearClipPlane){near=true;continue;}any=true;min=Vector2.Min(min,p);max=Vector2.Max(max,p);
     }
    }
    float scale=projectionScale/(camera.orthographic?1:Mathf.Max(nearestDepth,camera.nearClipPlane));
    // The closest bounds corner gives a conservative search radius for every silhouette depth.
    radius=Mathf.Min(Mathf.Max(width,height),Mathf.Max(target.Width,target.Halo)*scale);float padding=radius+6;
    if(!any){rect=default;return false;}if(near){rect=new Rect(0,0,1,1);return true;}
    float px=padding/width,py=padding/height;rect=Rect.MinMaxRect(Mathf.Clamp01(min.x-px),Mathf.Clamp01(min.y-py),Mathf.Clamp01(max.x+px),Mathf.Clamp01(max.y+py));return rect.width>0&&rect.height>0;
   }
   void Blit(RenderGraph graph,string name,TextureHandle source,TextureHandle mask,TextureHandle depth,TextureHandle destination,Material material,int shaderPass,Rect rect,Vector4 fullSize,Vector4 sourceSize,float step,Color colour,Vector4 widths,bool blend)
   {
    using(var builder=graph.AddRasterRenderPass<BlitData>(name,out var data))
    {
     data.source=source;data.mask=mask;data.material=material;data.shaderPass=shaderPass;data.rect=rect;data.properties=Block();
     data.properties.SetVector(SizeID,fullSize);data.properties.SetVector(SourceSizeID,sourceSize);data.properties.SetFloat(StepID,step);data.properties.SetColor(ColourID,colour);data.properties.SetVector(WidthsID,widths);
     builder.UseTexture(source,AccessFlags.Read);if(mask.IsValid())builder.UseTexture(mask,AccessFlags.Read);if(depth.IsValid())builder.UseTexture(depth,AccessFlags.Read);
     builder.SetRenderAttachment(destination,0,blend?AccessFlags.ReadWrite:AccessFlags.Write);
     builder.SetRenderFunc(static(BlitData d,RasterGraphContext context)=>
     {
      d.properties.SetTexture(SourceID,d.source);if(d.mask.IsValid())d.properties.SetTexture(MaskID,d.mask);
      context.cmd.EnableScissorRect(d.rect);context.cmd.DrawProcedural(Matrix4x4.identity,d.material,d.shaderPass,MeshTopology.Triangles,3,1,d.properties);context.cmd.DisableScissorRect();
     });
    }
   }
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
   {
    var cameraData=frameData.Get<UniversalCameraData>();if(cameraData.cameraType!=CameraType.Game||cameraData.renderType!=CameraRenderType.Base)return;
    var resources=frameData.Get<UniversalResourceData>();if(!resources.cameraDepthTexture.IsValid())return;
    // Match the surface's world mask instead of switching every outline on at transition end.
    // Keep the normal selection policy (target.Visible) separate from this rendering concern.
    var transition=ProjectWait.M.SceneTransition.StorybookSceneTransition.Current;
    var fade=transition?(transition.ArrivingFade?transition.ArrivingFade:transition.ActiveLobbyFade):null;
    bool dissolving=fade&&fade.useSceneMaterials&&fade.State!=ProjectWait.M.TitleScene.LobbyDissolveTest.FadeState.Visible;
    LastMaskFadeEnabled=dissolving;LastMaskFadeProgress=dissolving?(fade.State==ProjectWait.M.TitleScene.LobbyDissolveTest.FadeState.Hidden?1:fade.Progress):0;
    maskMaterial.SetFloat("_DissolveEnabled",dissolving?1:0);
    if(dissolving)
    {
     maskMaterial.SetFloat("_DissolveProgress",fade.State==ProjectWait.M.TitleScene.LobbyDissolveTest.FadeState.Hidden?1:fade.Progress);
     maskMaterial.SetFloat("_DissolveReveal",fade.State==ProjectWait.M.TitleScene.LobbyDissolveTest.FadeState.FadingIn?1:0);
     maskMaterial.SetVector("_DissolveOrigin",fade.TransitionOrigin);maskMaterial.SetVector("_DissolveRange",new Vector4(fade.ActiveDistanceRange.x,fade.ActiveDistanceRange.y,0,0));
     maskMaterial.SetFloat("_DissolveCellSize",fade.ActualDropletSpacing);maskMaterial.SetFloat("_DissolveBandWidth",fade.boundaryWidth);
    }
    blockIndex=0;targets.Clear();foreach(var t in ElevatorOutlineTarget.Active)if(t&&t.Visible&&t.geometry!=null&&t.geometry.Length>0)targets.Add(t);targets.Sort((a,b)=>a.Hovered.CompareTo(b.Hovered));
    var descriptor=resources.activeColorTexture.GetDescriptor(graph);int w=descriptor.width,h=descriptor.height;var fullSize=new Vector4(w,h,1f/w,1f/h);
    float widthScale=.5f*h*WorldUnitsPerWidthUnit;var camera=cameraData.camera;float projection=Mathf.Abs(camera.projectionMatrix.m11);
    if(camera.stereoEnabled)projection=Mathf.Max(Mathf.Abs(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).m11),Mathf.Abs(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right).m11));
    int sw=Mathf.Max(1,(w+1)/2),sh=Mathf.Max(1,(h+1)/2);var seedSize=new Vector4(sw,sh,1f/sw,1f/sh);
    // Coverage stays full resolution. Distance propagation uses half-size targets.
    var maskDesc=descriptor;maskDesc.name="Elevator coverage";maskDesc.depthBufferBits=DepthBits.None;maskDesc.colorFormat=GraphicsFormat.R16G16_SFloat;maskDesc.msaaSamples=MSAASamples.None;maskDesc.bindTextureMS=false;maskDesc.clearBuffer=true;maskDesc.clearColor=Color.clear;maskDesc.filterMode=FilterMode.Point;
    var seedDesc=maskDesc;seedDesc.name="Elevator nearest silhouette";seedDesc.width=sw;seedDesc.height=sh;seedDesc.colorFormat=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)?GraphicsFormat.R32G32B32A32_SFloat:GraphicsFormat.R16G16B16A16_SFloat;
    LastFrame=Time.frameCount;LastTargetCount=0;
    // Each interactable unions its parts in its own mask; neighbours keep independent silhouettes.
    foreach(var target in targets)
    {
#if UNITY_EDITOR
     if(DebugTarget&&target!=DebugTarget)continue;
#endif
     if(!Region(target,camera,widthScale*projection,w,h,out var region,out float radius))continue;
     var mask=graph.CreateTexture(maskDesc);var rect=Pixels(region,w,h);var halfRect=Pixels(region,sw,sh);
     using(var builder=graph.AddRasterRenderPass<MaskData>("Elevator mask / "+target.name,out var data))
     {
      data.geometry=target.geometry;data.material=maskMaterial;data.rect=rect;builder.SetRenderAttachment(mask,0,AccessFlags.ReadWrite);
      builder.SetRenderFunc(static(MaskData d,RasterGraphContext context)=>
      {
       context.cmd.EnableScissorRect(d.rect);
       foreach(var g in d.geometry)
       {
        if(g==null||!g.IsValid)continue;
        if(g.sourceRenderer){var skin=g.sourceRenderer as SkinnedMeshRenderer;var filter=g.sourceRenderer.GetComponent<MeshFilter>();var mesh=skin?skin.sharedMesh:filter?filter.sharedMesh:null;if(!mesh)continue;for(int i=0;i<mesh.subMeshCount;i++)context.cmd.DrawRenderer(g.sourceRenderer,d.material,i,0);}
        else for(int i=0;i<g.mesh.subMeshCount;i++)context.cmd.DrawMesh(g.mesh,g.anchor.localToWorldMatrix,d.material,i,0);
       }
       context.cmd.DisableScissorRect();
      });
     }
     LastTargetCount++;
#if UNITY_EDITOR
     if(DebugMask){Blit(graph,"Elevator debug mask",mask,TextureHandle.nullHandle,TextureHandle.nullHandle,resources.activeColorTexture,distanceMaterial,3,new Rect(0,0,w,h),fullSize,fullSize,0,Color.white,Vector4.zero,false);continue;}
#endif
     var nearest=graph.CreateTexture(seedDesc);Blit(graph,"Elevator seed / "+target.name,mask,TextureHandle.nullHandle,TextureHandle.nullHandle,nearest,distanceMaterial,0,halfRect,fullSize,fullSize,0,Color.white,Vector4.zero,false);
     int start=Mathf.Max(1,Mathf.NextPowerOfTwo(Mathf.Max(1,Mathf.CeilToInt(radius*.5f)+1))/2);
     for(int jump=start;jump>=1;jump/=2){var next=graph.CreateTexture(seedDesc);Blit(graph,"Elevator dilation "+jump,nearest,TextureHandle.nullHandle,TextureHandle.nullHandle,next,distanceMaterial,1,halfRect,fullSize,seedSize,jump,Color.white,Vector4.zero,false);nearest=next;}
     var refined=graph.CreateTexture(seedDesc);Blit(graph,"Elevator dilation refine",nearest,TextureHandle.nullHandle,TextureHandle.nullHandle,refined,distanceMaterial,1,halfRect,fullSize,seedSize,1,Color.white,Vector4.zero,false);
     Blit(graph,"Elevator outside-only composite / "+target.name,refined,mask,resources.cameraDepthTexture,resources.activeColorTexture,distanceMaterial,2,rect,fullSize,seedSize,0,target.Colour,new Vector4(target.Width,target.Halo,widthScale,DepthTolerance),true);
    }
   }
  }
 }
}
