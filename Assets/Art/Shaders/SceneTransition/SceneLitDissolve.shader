Shader "ProjectWait/M/Scene Lit Dissolve"
{
    Properties
    {
        _WorkflowMode("Workflow", Float)=1
        [MainTexture] _BaseMap("Albedo",2D)="white"{}
        [MainColor] _BaseColor("Color",Color)=(1,1,1,1)
        _Cutoff("Alpha cutoff",Range(0,1))=.5
        _Smoothness("Smoothness",Range(0,1))=.5
        _SmoothnessTextureChannel("Smoothness channel",Float)=0
        _Metallic("Metallic",Range(0,1))=0
        _MetallicGlossMap("Metallic map",2D)="white"{}
        _SpecColor("Specular",Color)=(.2,.2,.2,1)
        _SpecGlossMap("Specular map",2D)="white"{}
        [ToggleOff] _SpecularHighlights("Specular highlights",Float)=1
        [ToggleOff] _EnvironmentReflections("Environment reflections",Float)=1
        _BumpScale("Normal scale",Float)=1
        _BumpMap("Normal map",2D)="bump"{}
        _Parallax("Parallax",Range(.005,.08))=.005
        _ParallaxMap("Height map",2D)="black"{}
        _OcclusionStrength("Occlusion strength",Range(0,1))=1
        _OcclusionMap("Occlusion map",2D)="white"{}
        [HDR] _EmissionColor("Emission",Color)=(0,0,0,1)
        _EmissionMap("Emission map",2D)="white"{}
        _DetailMask("Detail mask",2D)="white"{}
        _DetailAlbedoMapScale("Detail albedo scale",Float)=1
        _DetailAlbedoMap("Detail albedo",2D)="linearGrey"{}
        _DetailNormalMapScale("Detail normal scale",Float)=1
        _DetailNormalMap("Detail normal",2D)="bump"{}
        [HideInInspector] _ClearCoatMask("Clear coat mask",Float)=0
        [HideInInspector] _ClearCoatSmoothness("Clear coat smoothness",Float)=0
        _Surface("Surface",Float)=0
        _Blend("Blend",Float)=0
        _Cull("Culling",Float)=2
        [ToggleUI] _AlphaClip("Alpha clip",Float)=0
        [HideInInspector] _SrcBlend("Source blend",Float)=1
        [HideInInspector] _DstBlend("Destination blend",Float)=0
        [HideInInspector] _SrcBlendAlpha("Source alpha blend",Float)=1
        [HideInInspector] _DstBlendAlpha("Destination alpha blend",Float)=0
        [HideInInspector] _ZWrite("Z write",Float)=1
        [HideInInspector] _BlendModePreserveSpecular("Preserve specular",Float)=1
        [HideInInspector] _AlphaToMask("Alpha to mask",Float)=0
        [ToggleUI] _ReceiveShadows("Receive shadows",Float)=1
        _QueueOffset("Queue offset",Float)=0
        [HideInInspector] _MainTex("Legacy texture",2D)="white"{}
        [HideInInspector] _Color("Legacy color",Color)=(1,1,1,1)
        [HideInInspector] _DissolveEnabled("Scene fade",Float)=0
        [HideInInspector] _DissolveProgress("Progress",Float)=0
        [HideInInspector] _DissolveReveal("Reveal",Float)=0
        [HideInInspector] _DissolveOrigin("Origin",Vector)=(0,0,0,0)
        [HideInInspector] _DissolveRange("Range",Vector)=(.5,18,0,0)
        [HideInInspector] _DissolveCellSize("Spacing",Float)=.36
        [HideInInspector] _DissolveBandWidth("Band",Float)=3
        [HideInInspector][NoScaleOffset] unity_Lightmaps("Lightmaps",2DArray)=""{}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("Directional lightmaps",2DArray)=""{}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("Shadow masks",2DArray)=""{}
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" "RenderType"="Opaque" "IgnoreProjector"="True"}
        LOD 300
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite] Cull [_Cull] AlphaToMask [_AlphaToMask]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment SceneLitFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "SceneLitFade.hlsl"
            void SceneLitFragment(Varyings input, out half4 color : SV_Target0
                #ifdef _WRITE_RENDERING_LAYERS
                    , out uint layers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(LobbyDissolveSDF(input.positionWS,input.normalWS));
                LitPassFragment(input,color
                    #ifdef _WRITE_RENDERING_LAYERS
                        , layers
                    #endif
                );
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SceneShadowVertex
            #pragma fragment SceneAuxFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "SceneLitFade.hlsl"
            #include "SceneDissolveAuxiliary.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SceneDepthVertex
            #pragma fragment SceneAuxFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "SceneLitFade.hlsl"
            #include "SceneDissolveAuxiliary.hlsl"
            ENDHLSL
        }
    }
}
