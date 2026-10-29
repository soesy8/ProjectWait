Shader "ProjectWait/M/Cinematic Overlay"
{
 Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} }
 SubShader
 {
  Tags {"Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 p:POSITION;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 p:SV_POSITION;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.color=i.color;return o;}
   half4 Frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return i.color;}
   ENDHLSL
  }
 }
}
