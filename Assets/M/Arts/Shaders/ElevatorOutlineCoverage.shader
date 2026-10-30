Shader "ProjectWait/M/Outline Coverage"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline"}
  Pass
  {
   Name "ObjectCoverage" Cull Off ZWrite Off ZTest Always
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   struct A{float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 positionCS:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(a.p.xyz);return o;}
   float2 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
    float own=LinearEyeDepth(i.positionCS.z,_ZBufferParams);float scene=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
    clip(scene+.025-own);return float2(1,own);
   }
   ENDHLSL
  }
 }
}
