Shader "ProjectWait/M/Scene Outline Coverage"
{
    Properties
    {
        [HideInInspector] _DissolveEnabled("Scene fade",Float)=0
        [HideInInspector] _DissolveProgress("Progress",Float)=0
        [HideInInspector] _DissolveReveal("Reveal",Float)=0
        [HideInInspector] _DissolveOrigin("Origin",Vector)=(0,0,0,0)
        [HideInInspector] _DissolveRange("Range",Vector)=(.5,18,0,0)
        [HideInInspector] _DissolveCellSize("Spacing",Float)=.36
        [HideInInspector] _DissolveBandWidth("Band",Float)=3
    }
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
            CBUFFER_START(UnityPerMaterial)
                float4 _DissolveOrigin,_DissolveRange;
                float _DissolveEnabled,_DissolveProgress,_DissolveReveal,_DissolveCellSize,_DissolveBandWidth;
            CBUFFER_END
            #include "../TitleScene/LobbyDissolveCommon.hlsl"
            struct A {float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A input)
            {
                V output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.world=TransformObjectToWorld(input.p.xyz);output.normal=TransformObjectToWorldNormal(input.n);
                output.positionCS=TransformWorldToHClip(output.world);return output;
            }
            float2 Frag(V input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normal=dot(input.normal,input.normal)>.001?normalize(input.normal):normalize(cross(ddx(input.world),ddy(input.world)));
                clip(LobbyDissolveSDF(input.world,normal));
                float depth=unity_OrthoParams.w>.5?LinearDepthToEyeDepth(input.positionCS.z):LinearEyeDepth(input.positionCS.z,_ZBufferParams);
                return float2(1,rcp(max(depth,.0001)));
            }
            ENDHLSL
        }
    }
}
