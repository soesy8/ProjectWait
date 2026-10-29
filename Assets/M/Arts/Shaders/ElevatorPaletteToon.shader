Shader "ProjectWait/M/Vertex Palette Toon"
{
 Properties { [MainColor] _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
  CBUFFER_END
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
   Name "ToonSurface" Tags {"LightMode"="UniversalForward"} Cull Off
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half4 Frag(V i, FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    if(i.style.z>1.5&&!IS_FRONT_VFACE(front,true,false))discard;
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 sc=ComputeScreenPos(TransformWorldToHClip(i.world));
    #else
    float4 sc=TransformWorldToShadowCoord(i.world);
    #endif
    Light light=GetMainLight(sc);half ndl=dot(normalize(i.normal),light.direction);half edge=max(i.style.w,fwidth(ndl));
    half middle=smoothstep(i.bands.x-edge,i.bands.x+edge,ndl);half lit=smoothstep(i.bands.y-edge,i.bands.y+edge,ndl);
    half3 tone=lerp(i.shade.rgb,i.shade.www,middle);tone=lerp(tone,half3(1,1,1),lit);
    tone*=lerp(half3(.76,.80,.85),half3(1,1,1),light.shadowAttenuation);
    return half4(i.color.rgb*lerp(tone,half3(1,1,1),i.style.y),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "StoryInk" Tags {"LightMode"="SRPDefaultUnlit"} Cull Front ZWrite On
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex InkVert
   #pragma fragment InkFrag
   #pragma multi_compile_instancing
   V InkVert(A i){V o=Vert(i);float d=max(.5,distance(GetCameraPositionWS(),o.world));float w=min(.035,i.style.x*d/(450*abs(UNITY_MATRIX_P._m11)));o.p=TransformWorldToHClip(o.world+normalize(o.normal)*w);return o;}
   half4 InkFrag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);clip(i.style.x-.001);return i.ink;}
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0 Cull Back
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex ShadowVert
   #pragma fragment DepthFrag
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection;float3 _LightPosition;
   V ShadowVert(A i)
   {
    V o=Vert(i);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 d=normalize(_LightPosition-o.world);
    #else
    float3 d=_LightDirection;
    #endif
    o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),d));
    #if UNITY_REVERSED_Z
    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
    #else
    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
    #endif
    return o;
   }
   half4 DepthFrag(V i):SV_Target{return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R Cull Back
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment DepthFrag
   #pragma multi_compile_instancing
   half4 DepthFrag(V i):SV_Target{return 0;}
   ENDHLSL
  }
 }
}
