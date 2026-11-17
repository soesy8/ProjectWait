Shader "ProjectWait/Office/Storybook Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Palette atlas", 2D)="white"{}
        [MainColor] _BaseColor("Color", Color)=(1,1,1,1)
        _ShadeColor("Shadow tint", Color)=(.74,.78,.76,1)
        _MidTone("Middle tone", Float)=.9
        _ShadowThreshold("Shadow boundary", Float)=.02
        _LightThreshold("Light boundary", Float)=.62
        _BandSoftness("Band softness", Float)=.012
        _OutlineColor("Brown ink", Color)=(.286,.2,.153,1)
        _OutlineWidth("Outline pixels", Float)=.6
        _PoweredSurface("Powered lamp or screen", Range(0,1))=0
        _Cull("Culling", Float)=2
        [HideInInspector] _DissolveEnabled("Scene fade",Float)=0
        [HideInInspector] _DissolveProgress("Progress",Float)=0
        [HideInInspector] _DissolveReveal("Reveal",Float)=0
        [HideInInspector] _DissolveOrigin("Origin",Vector)=(0,0,0,0)
        [HideInInspector] _DissolveRange("Range",Vector)=(0,18,0,0)
        [HideInInspector] _DissolveCellSize("Spacing",Float)=.36
        [HideInInspector] _DissolveBandWidth("Band",Float)=3
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        // Only this shader reads the scene-owned global. Shared scenes are unaffected.
        float _OfficeDarkness;
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor,_ShadeColor,_OutlineColor;
        float _MidTone,_ShadowThreshold,_LightThreshold,_BandSoftness,_OutlineWidth,_PoweredSurface,_Cull;
        float4 _DissolveOrigin,_DissolveRange;
        float _DissolveEnabled,_DissolveProgress,_DissolveReveal,_DissolveCellSize,_DissolveBandWidth;
        CBUFFER_END
        #include "TitleScene/LobbyDissolveCommon.hlsl"
        struct A
        {
            float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct V
        {
            float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        V Vert(A i)
        {
            V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.world=TransformObjectToWorld(i.p.xyz); o.p=TransformWorldToHClip(o.world);
            o.normal=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;
        }
        half4 Depth(V i):SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            clip(LobbyDissolveSDF(i.world,i.normal));return 0;
        }
        ENDHLSL
        Pass
        {
            Name "ToonSurface" Tags {"LightMode"="UniversalForward"} Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                clip(LobbyDissolveSDF(i.world,i.normal));
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 sc=ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                    float4 sc=TransformWorldToShadowCoord(i.world);
                #endif
                Light light=GetMainLight(sc);
                half n=dot(normalize(i.normal),light.direction),edge=max(_BandSoftness,fwidth(n));
                half3 tone=lerp(_ShadeColor.rgb,_MidTone.xxx,smoothstep(_ShadowThreshold-edge,_ShadowThreshold+edge,n));
                tone=lerp(tone,1,smoothstep(_LightThreshold-edge,_LightThreshold+edge,n));
                tone*=lerp(half3(.74,.81,.82),1,light.shadowAttenuation);
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                color*=lerp(tone,1,_PoweredSurface*(1-_OfficeDarkness));
                // Deliberate emergency fill: recognisable faces, not a black headset screen.
                color*=lerp(half3(1,1,1),half3(.16,.25,.32),_OfficeDarkness);
                color*=lerp(1,.2,_PoweredSurface*_OfficeDarkness);
                return half4(color,1);
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
            V InkVert(A i)
            {
                V o=Vert(i);float d=max(.5,distance(GetCameraPositionWS(),o.world));
                float w=min(.03,_OutlineWidth*d/(450*abs(UNITY_MATRIX_P._m11)));
                o.p=TransformWorldToHClip(o.world+normalize(o.normal)*w);return o;
            }
            half4 InkFrag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                clip(_OutlineWidth-.001);clip(LobbyDissolveSDF(i.world,i.normal));return _OutlineColor;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection,_LightPosition;
            V ShadowVert(A i)
            {
                V o=Vert(i);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction=normalize(_LightPosition-o.world);
                #else
                    float3 direction=_LightDirection;
                #endif
                o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),direction));
                #if UNITY_REVERSED_Z
                    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
                #else
                    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
                #endif
                return o;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
