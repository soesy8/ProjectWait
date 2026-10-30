Shader "ProjectWait/M/Sky Highlight Mask"
{
 Properties { _StencilRef("Object stencil",Float)=64 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+90" }
  Pass
  {
   Name "VisibleSilhouetteMask" Tags { "LightMode"="SRPDefaultUnlit" }
   // Cover the full projected object, including back faces and small facial pieces.
   // The glow itself still depth-tests against the scene.
   Cull Off ZWrite Off ZTest Always ColorMask 0
   Stencil { Ref [_StencilRef] ReadMask 255 WriteMask 255 Comp Always Pass Replace }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 p:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 p:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
   V Vert(A v)
   {
    V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.p=TransformObjectToHClip(v.p.xyz); return o;
   }
   half4 Frag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return 0; }
   ENDHLSL
  }
 }
}
