Shader "ProjectWait/M/Outline Coverage"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline"}
  Pass
  {
   Name "ObjectCoverage" Cull Off ZWrite Off ZTest Always
   Blend One One BlendOp Max
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 positionCS:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(a.p.xyz);return o;}
   float2 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    // Keep the entire silhouette, including coplanar faces. Scene-depth clipping
    // here turns small depth differences into false internal triangle outlines.
    // Max blending keeps the nearest surface independent of triangle draw order.
    float own=unity_OrthoParams.w>.5?LinearDepthToEyeDepth(i.positionCS.z):LinearEyeDepth(i.positionCS.z,_ZBufferParams);
    return float2(1,rcp(max(own,.0001)));
   }
   ENDHLSL
  }
 }
}
