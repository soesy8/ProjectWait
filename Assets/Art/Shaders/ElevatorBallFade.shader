Shader "ProjectWait/M/Ball Handoff Fade"
{
 Properties
 {
  [MainColor] _BaseColor("Tint",Color)=(1,1,1,1)
   _Visibility("Visibility",Range(0,1))=1
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
  Tags {"RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
    half _Visibility;
    float4 _DissolveOrigin,_DissolveRange;
    float _DissolveEnabled,_DissolveProgress,_DissolveReveal,_DissolveCellSize,_DissolveBandWidth;
   CBUFFER_END
   #include "TitleScene/LobbyDissolveCommon.hlsl"
  struct A {float4 p:POSITION;float3 n:NORMAL;float4 color:COLOR;float4 style:TEXCOORD1;float4 shade:TEXCOORD2;float4 ink:TEXCOORD3;float2 bands:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 color:TEXCOORD2;float4 style:TEXCOORD3;half4 shade:TEXCOORD4;half4 ink:TEXCOORD5;float2 bands:TEXCOORD6;UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A i)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);
   o.color=i.color*_BaseColor;o.style=i.style;o.shade=i.shade;o.ink=i.ink;o.bands=i.bands;return o;
  }
  ENDHLSL
  Pass
  {
   Name "ToonSurface" Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half4 Frag(V i):SV_Target
   {
     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
     clip(LobbyDissolveSDF(i.world,i.normal));
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 sc=ComputeScreenPos(TransformWorldToHClip(i.world));
    #else
    float4 sc=TransformWorldToShadowCoord(i.world);
    #endif
    Light light=GetMainLight(sc);half ndl=dot(normalize(i.normal),light.direction);half edge=max(i.style.w,fwidth(ndl));
    half middle=smoothstep(i.bands.x-edge,i.bands.x+edge,ndl);half lit=smoothstep(i.bands.y-edge,i.bands.y+edge,ndl);
    half3 tone=lerp(i.shade.rgb,i.shade.www,middle);tone=lerp(tone,half3(1,1,1),lit);
    tone*=lerp(half3(.76,.80,.85),half3(1,1,1),light.shadowAttenuation);
    return half4(i.color.rgb*lerp(tone,half3(1,1,1),i.style.y),_Visibility);
   }
   ENDHLSL
  }
  Pass
  {
   Name "StoryInk" Tags {"LightMode"="SRPDefaultUnlit"}
   Cull Front ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex InkVert
   #pragma fragment InkFrag
   #pragma multi_compile_instancing
   V InkVert(A i)
   {
    V o=Vert(i);float d=max(.5,distance(GetCameraPositionWS(),o.world));
    float w=min(.035,i.style.x*d/(450*abs(UNITY_MATRIX_P._m11)));
    o.p=TransformWorldToHClip(o.world+normalize(o.normal)*w);return o;
   }
   half4 InkFrag(V i):SV_Target
   {
     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);clip(LobbyDissolveSDF(i.world,i.normal));clip(i.style.x-.001);
    return half4(i.ink.rgb,i.ink.a*_Visibility);
   }
   ENDHLSL
  }
 }
}
