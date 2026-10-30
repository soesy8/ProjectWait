Shader "ProjectWait/M/Sky Highlight"
{
 Properties{_Color("Glow",Color)=(0.35,0.87,1,1) _Width("Width in pixels",Float)=3 _StencilRef("Object stencil",Float)=64}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+100"}
  Pass
  {
   Name "Silhouette" Tags{"LightMode"="SRPDefaultUnlit"}
   Cull Front ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
   Stencil { Ref [_StencilRef] ReadMask 255 Comp NotEqual Pass Keep }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial) half4 _Color;float _Width;CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A v)
   {
    V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.p=TransformObjectToHClip(v.p.xyz);
    float3 normalVS=TransformWorldToViewDir(TransformObjectToWorldNormal(v.n),true);
    float2 dir=normalVS.xy/max(length(normalVS.xy),.0001);
    o.p.xy+=dir*(_Width*2/_ScaledScreenParams.xy)*o.p.w;return o;
   }
   half4 Frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return _Color;}
   ENDHLSL
  }
 }
}
