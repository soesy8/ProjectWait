Shader "ProjectWait/M/Title Atlas Toon"
{
 Properties
 {
  [MainTexture] _BaseMap("Shared colour atlas",2D)="white"{}
  _ShadeColor("Shadow tint",Color)=(.741,.784,.765,1)
  _MidTone("Middle band",Range(0,1))=.9
  _ShadowThreshold("Shadow threshold",Float)=.02
  _LightThreshold("Light threshold",Float)=.62
  _BandSoftness("Band edge",Float)=.012
  _OutlineColor("Warm brown ink",Color)=(.286,.2,.153,1)
  _OutlineWidth("Reference ink pixels",Float)=1.15
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial)
   half4 _ShadeColor,_OutlineColor;
   float4 _BaseMap_ST;
   half _MidTone,_ShadowThreshold,_LightThreshold,_BandSoftness,_OutlineWidth;
  CBUFFER_END
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
  struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;half4 style:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half2 style:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A i)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   o.world=TransformObjectToWorld(i.p.xyz);o.normal=TransformObjectToWorldNormal(i.n);
   if(i.style.r>.5)
   {
    float d=max(.5,distance(GetCameraPositionWS(),o.world));
    o.world+=normalize(o.normal)*min(.025,_OutlineWidth*d/(540*abs(UNITY_MATRIX_P._m11)));
   }
   o.p=TransformWorldToHClip(o.world);o.uv=i.uv;o.style=i.style.rg;return o;
  }
  ENDHLSL
  Pass
  {
   Name "AtlasSurfaceAndInk" Tags {"LightMode"="UniversalForward"}
   Cull Off ZWrite On
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half4 Frag(V i,FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    bool facing=IS_FRONT_VFACE(front,true,false);
    if(i.style.x>.5){if(facing)discard;return _OutlineColor;}
    if(!facing)discard;
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 sc=ComputeScreenPos(TransformWorldToHClip(i.world));
    #else
    float4 sc=TransformWorldToShadowCoord(i.world);
    #endif
    Light light=GetMainLight(sc);half ndl=dot(normalize(i.normal),light.direction);half edge=max(_BandSoftness,fwidth(ndl));
    half mid=smoothstep(_ShadowThreshold-edge,_ShadowThreshold+edge,ndl);half lit=smoothstep(_LightThreshold-edge,_LightThreshold+edge,ndl);
    half3 tone=lerp(_ShadeColor.rgb,_MidTone.xxx,mid);tone=lerp(tone,half3(1,1,1),lit);
    tone*=lerp(half3(.76,.80,.85),half3(1,1,1),light.shadowAttenuation);
    return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*lerp(tone,half3(1,1,1),i.style.y),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} Cull Back ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex ShadowVert
   #pragma fragment DepthFrag
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection,_LightPosition;
   V ShadowVert(A i)
   {
    V o=Vert(i);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDir=normalize(_LightPosition-o.world);
    #else
    float3 lightDir=_LightDirection;
    #endif
    o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),lightDir));
    #if UNITY_REVERSED_Z
    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
    #else
    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
    #endif
    return o;
   }
   half4 DepthFrag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);clip(.5-i.style.x);return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} Cull Back ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment DepthFrag
   #pragma multi_compile_instancing
   half4 DepthFrag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);clip(.5-i.style.x);return 0;}
   ENDHLSL
  }
 }
}
