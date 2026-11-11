Shader "ProjectWait/M/Title Outline"
{
 Properties { _OutlineColor("Warm ink",Color)=(.286,.2,.153,1) _OutlineWidth("Reference pixels",Range(0,5))=1.1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Cull Front ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _OutlineColor; float _OutlineWidth;
   CBUFFER_END
   struct A { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 position:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
   V Vert(A i)
   {
    V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 p=TransformObjectToWorld(i.vertex.xyz); float3 n=normalize(TransformObjectToWorldNormal(i.normal));
    float distanceToEye=max(.5,distance(GetCameraPositionWS(),p));
    float width=min(.025,_OutlineWidth*distanceToEye/(540*abs(UNITY_MATRIX_P._m11)));
    o.position=TransformWorldToHClip(p+n*width); return o;
   }
   half4 Frag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return _OutlineColor; }
   ENDHLSL
  }
 }
}
