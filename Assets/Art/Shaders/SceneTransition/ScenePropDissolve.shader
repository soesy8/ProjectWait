Shader "ProjectWait/M/Scene Prop Dissolve"
{
 Properties
 {
  [MainColor] _BaseColor("Color",Color)=(1,1,1,1)
  [MainTexture] _BaseMap("Texture",2D)="white"{}
  _Unlit("Unlit",Float)=1
  _Cull("Culling",Float)=2
  _Cutoff("Alpha cutoff",Range(0,1))=.5
  _Surface("Surface",Float)=0
  _Blend("Blend",Float)=0
  _AlphaClip("Alpha clip",Float)=0
  [HideInInspector] _SrcBlend("Source blend",Float)=1
  [HideInInspector] _DstBlend("Destination blend",Float)=0
  [HideInInspector] _SrcBlendAlpha("Source alpha blend",Float)=1
  [HideInInspector] _DstBlendAlpha("Destination alpha blend",Float)=0
  [HideInInspector] _ZWrite("Depth write",Float)=1
  [HideInInspector] _AlphaToMask("Alpha to mask",Float)=0
  [HideInInspector] _DissolveEnabled("Scene fade",Float)=0
  [HideInInspector] _DissolveProgress("Progress",Float)=0
  [HideInInspector] _DissolveReveal("Reveal",Float)=0
  [HideInInspector] _DissolveOrigin("Origin",Vector)=(0,0,0,0)
  [HideInInspector] _DissolveRange("Range",Vector)=(.5,13,0,0)
  [HideInInspector] _DissolveCellSize("Spacing",Float)=.36
  [HideInInspector] _DissolveBandWidth("Band",Float)=3
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
  CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor; float4 _BaseMap_ST; half _Unlit, _Cutoff, _Surface;
   float4 _DissolveOrigin,_DissolveRange;
   float _DissolveEnabled,_DissolveProgress,_DissolveReveal,_DissolveCellSize,_DissolveBandWidth;
  CBUFFER_END
  #include "../TitleScene/LobbyDissolveCommon.hlsl"
  struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float fog:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.p.xyz);o.normal=TransformObjectToWorldNormal(i.n);o.p=TransformWorldToHClip(o.world);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);return o;}
  ENDHLSL
  Pass
  {
   Name "SceneProp" Tags {"LightMode"="UniversalForward"} Cull [_Cull] ZWrite [_ZWrite]
   Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
   AlphaToMask [_AlphaToMask]
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
   #pragma shader_feature_local_fragment _ALPHATEST_ON
   #pragma shader_feature_local_fragment _ALPHAMODULATE_ON
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);clip(LobbyDissolveSDF(i.world,i.normal));
    half4 sample=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
    half alpha=AlphaDiscard(sample.a,_Cutoff);
    half3 color=AlphaModulate(sample.rgb,alpha);
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
     float4 shadow=ComputeScreenPos(TransformWorldToHClip(i.world));
    #else
     float4 shadow=TransformWorldToShadowCoord(i.world);
    #endif
    Light light=GetMainLight(shadow);half ndl=dot(normalize(i.normal),light.direction);
    half band=ndl<.02?.76:(ndl<.62?.90:1);
    half3 tone=band*lerp(half3(.76,.80,.85),half3(1,1,1),light.shadowAttenuation);
    return half4(MixFog(color*lerp(tone,half3(1,1,1),_Unlit),i.fog),OutputAlpha(alpha,IsSurfaceTypeTransparent(_Surface)));
   }
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} Cull [_Cull] ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Depth
   #pragma multi_compile_instancing
   #pragma shader_feature_local_fragment _ALPHATEST_ON
   half4 Depth(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);clip(LobbyDissolveSDF(i.world,i.normal));Alpha(SampleAlbedoAlpha(i.uv,TEXTURE2D_ARGS(_BaseMap,sampler_BaseMap)).a,_BaseColor,_Cutoff);return i.p.z;}
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} Cull [_Cull] ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex SceneShadowVertex
   #pragma fragment SceneAuxFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #pragma shader_feature_local _ALPHATEST_ON
   #include "SceneDissolveAuxiliary.hlsl"
   ENDHLSL
  }
 }
}
