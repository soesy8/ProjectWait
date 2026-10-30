Shader "ProjectWait/M/Outline Distance"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off ZTest Always
  HLSLINCLUDE
  #pragma target 3.5
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
  TEXTURE2D_X(_OutlineSource);TEXTURE2D_X(_OutlineOriginalMask);
  CBUFFER_START(UnityPerMaterial)
  float4 _OutlineFullSize,_OutlineSourceSize,_OutlineTint,_OutlineWidths;float _OutlineStep;
  CBUFFER_END
  struct A{uint vertexID:SV_VertexID;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=GetFullScreenTriangleVertexPosition(a.vertexID);o.uv=GetFullScreenTriangleTexCoord(a.vertexID);return o;}
  float4 Seed(V i):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 pixel=i.uv*_OutlineFullSize.xy;float2 first=floor(pixel-.5)+.5;float best=1e20;float4 result=0;
   [unroll]for(int y=0;y<2;y++)[unroll]for(int x=0;x<2;x++)
   {
    float2 p=first+float2(x,y);float2 mask=SAMPLE_TEXTURE2D_X_LOD(_OutlineSource,sampler_PointClamp,p*_OutlineFullSize.zw,0).rg;float distance=dot(p-pixel,p-pixel);
    if(mask.r>.5&&distance<best){best=distance;result=float4(p,mask.g,1);}
   }
   return result;
  }
  float4 Jump(V i):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 pixel=i.uv*_OutlineFullSize.xy;float best=1e20;float4 result=0;
   [unroll]for(int y=-1;y<=1;y++)[unroll]for(int x=-1;x<=1;x++)
   {
    float2 uv=i.uv+float2(x,y)*_OutlineStep*_OutlineSourceSize.zw;if(any(uv<0)||any(uv>1))continue;
    float4 seed=SAMPLE_TEXTURE2D_X_LOD(_OutlineSource,sampler_PointClamp,uv,0);float d=dot(seed.xy-pixel,seed.xy-pixel);
    if(seed.w>.5&&d<best){best=d;result=seed;}
   }
   return result;
  }
  float4 Composite(V i):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float original=SAMPLE_TEXTURE2D_X_LOD(_OutlineOriginalMask,sampler_PointClamp,i.uv,0).r;
   float4 seed=SAMPLE_TEXTURE2D_X_LOD(_OutlineSource,sampler_PointClamp,i.uv,0);if(seed.w<.5||original>.999)return 0;
   float scene=LinearEyeDepth(SampleSceneDepth(i.uv),_ZBufferParams);if(scene+max(_OutlineWidths.w,seed.z*.001)<seed.z)return 0;
   float distance=length(seed.xy-i.uv*_OutlineFullSize.xy);float core=1-smoothstep(_OutlineWidths.x-.75,_OutlineWidths.x+.75,distance);
   float halo=pow(saturate(1-distance/max(_OutlineWidths.y,1)),1.7)*.65;
   float alpha=max(core,halo)*(1-saturate(original))*_OutlineTint.a;return float4(_OutlineTint.rgb,alpha);
  }
  float4 Debug(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float m=SAMPLE_TEXTURE2D_X_LOD(_OutlineSource,sampler_PointClamp,i.uv,0).r;return float4(m,m,m,1);}
  ENDHLSL
  Pass {Name "Seed"
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Seed
   #pragma multi_compile_instancing
  ENDHLSL
  }
  Pass {Name "Dilation"
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Jump
   #pragma multi_compile_instancing
  ENDHLSL
  }
  Pass {Name "OutsideOnly" Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Composite
   #pragma multi_compile_instancing
  ENDHLSL
  }
  Pass {Name "DebugMask"
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Debug
   #pragma multi_compile_instancing
  ENDHLSL
  }
 }
}
