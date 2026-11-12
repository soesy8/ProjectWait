Shader "ProjectWait/M/Story Transition Sky Bridge"
{
    Properties
    {
        [PerRendererData] _MainTex("UI texture", 2D) = "white" {}
        _SkyTex("Existing panoramic sky", 2D) = "white" {}
        _Tint("Sky tint", Color) = (.5,.5,.5,1)
        _Exposure("Sky exposure", Float) = 1
        _Rotation("Sky rotation", Float) = 0
        _StencilComp("Stencil comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil operation", Float) = 0
        _StencilWriteMask("Stencil write mask", Float) = 255
        _StencilReadMask("Stencil read mask", Float) = 255
        _ColorMask("Color mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_SkyTex); SAMPLER(sampler_SkyTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Exposure, _Rotation;
            CBUFFER_END
            struct A { float4 position : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 position : SV_POSITION; float3 world : TEXCOORD0; half alpha : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A input)
            {
                V output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.world = TransformObjectToWorld(input.position.xyz);
                output.position = TransformWorldToHClip(output.world);
                output.alpha = input.color.a;
                return output;
            }
            half4 Frag(V input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = normalize(input.world - GetCameraPositionWS());
                float angle = radians(_Rotation);
                direction.xz = float2(cos(angle)*direction.x - sin(angle)*direction.z, sin(angle)*direction.x + cos(angle)*direction.z);
                float2 uv = float2(.5 - atan2(direction.z, direction.x) / TWO_PI, 1 - acos(clamp(direction.y,-1,1)) / PI);
                half3 color = SAMPLE_TEXTURE2D(_SkyTex, sampler_SkyTex, uv).rgb;
                #if defined(UNITY_COLORSPACE_GAMMA)
                    color *= _Tint.rgb * 2;
                #else
                    color *= _Tint.rgb * 4.5947938;
                #endif
                return half4(color * _Exposure, input.alpha);
            }
            ENDHLSL
        }
    }
}
