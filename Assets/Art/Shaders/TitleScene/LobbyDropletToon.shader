Shader "ProjectWait/M/Lobby Cached Droplet Toon"
{
    Properties
    {
        _ShadeColor("Shadow tint",Color)=(.741,.784,.765,1)
        _MidTone("Middle band",Range(0,1))=.9
        _ShadowThreshold("Shadow threshold",Float)=.02
        _LightThreshold("Light threshold",Float)=.62
        _BandSoftness("Band edge",Float)=.012
        _OutlineColor("Warm brown ink",Color)=(.286,.2,.153,1)
        _OutlineWidth("Reference ink pixels",Float)=1.15
        [HideInInspector] _DissolveProgress("Progress",Float)=0
        [HideInInspector] _DissolveReveal("Reveal",Float)=0
        [HideInInspector] _DissolveOrigin("Origin",Vector)=(0,0,0,0)
        [HideInInspector] _DissolveRange("Range",Vector)=(.5,13,0,0)
        [HideInInspector] _DissolveBandWidth("Band",Float)=3
        [HideInInspector] _DropletMaximumRadius("Maximum radius",Float)=.225
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _ShadeColor,_OutlineColor;
            half _MidTone,_ShadowThreshold,_LightThreshold,_BandSoftness,_OutlineWidth;
            float4 _DissolveOrigin,_DissolveRange;
            float _DissolveProgress,_DissolveReveal,_DissolveBandWidth,_DropletMaximumRadius;
        CBUFFER_END
        #include "LobbyFadeCommon.hlsl"
        struct A
        {
            float4 center:POSITION; float3 radial:NORMAL; half4 color:COLOR; float2 style:TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct V
        {
            float4 position:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1;
            half4 color:COLOR; half ink:TEXCOORD2; float alive:TEXCOORD3;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        V Vert(A input)
        {
            V o=(V)0; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            float3 normal; float radius;
            float keep=LobbyKeepAt(input.center.xyz);
            radius=_DropletMaximumRadius*(1-DropletSmooth((keep-.72)/.28))*DropletSmooth(keep/.72);
            normal=input.radial;
            o.world=input.center.xyz+normal*radius; o.alive=radius-.004;
            o.normal=normal; o.ink=input.style.x; o.color=input.color;
            if(o.ink>.5)
            {
                float d=max(.5,distance(GetCameraPositionWS(),o.world));
                o.world+=normal*min(min(.025,radius*.16),_OutlineWidth*d/(540*abs(UNITY_MATRIX_P._m11)));
            }
            o.position=TransformWorldToHClip(o.world); return o;
        }
        ENDHLSL
        Pass
        {
            Name "CachedDropsAndInk" Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); clip(i.alive);
                if(i.ink>.5) return _OutlineColor;
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 sc=ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                float4 sc=TransformWorldToShadowCoord(i.world);
                #endif
                Light light=GetMainLight(sc);
                half ndl=dot(normalize(i.normal),light.direction),edge=max(_BandSoftness,fwidth(ndl));
                half mid=smoothstep(_ShadowThreshold-edge,_ShadowThreshold+edge,ndl);
                half lit=smoothstep(_LightThreshold-edge,_LightThreshold+edge,ndl);
                half3 tone=lerp(_ShadeColor.rgb,_MidTone.xxx,mid); tone=lerp(tone,half3(1,1,1),lit);
                tone*=lerp(half3(.76,.80,.85),half3(1,1,1),light.shadowAttenuation);
                return half4(i.color.rgb*tone,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" }
            Cull Back ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection,_LightPosition;
            V ShadowVert(A input)
            {
                V o=Vert(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 direction=normalize(_LightPosition-o.world);
                #else
                float3 direction=_LightDirection;
                #endif
                o.position=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),direction));
                #if UNITY_REVERSED_Z
                o.position.z=min(o.position.z,UNITY_NEAR_CLIP_VALUE*o.position.w);
                #else
                o.position.z=max(o.position.z,UNITY_NEAR_CLIP_VALUE*o.position.w);
                #endif
                return o;
            }
            half4 DepthFrag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); clip(i.alive); clip(.5-i.ink); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" }
            Cull Back ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half4 DepthFrag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); clip(i.alive); clip(.5-i.ink); return 0; }
            ENDHLSL
        }
    }
}
