Shader "ProjectWait/M/Observation UI Focus Window"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha Clip",Float)=0
        _FocusOrigin("Centre-eye origin",Vector)=(0,0,0,1)
        _FocusForward("Centre-eye forward",Vector)=(0,0,1,0)
        _FocusRight("Centre-eye right",Vector)=(1,0,0,0)
        _FocusUp("Centre-eye up",Vector)=(0,1,0,0)
        _FocusRadius("Angular ellipse tangents",Vector)=(.22,.18,0,0)
        _FocusStrength("Focus amount",Range(0,1))=0
        _FocusOpacity("Centre background opacity",Range(0,1))=.12
    }
    SubShader
    {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST, _ClipRect;
            half4 _Color, _TextureSampleAdd;
            float4 _FocusOrigin, _FocusForward, _FocusRight, _FocusUp, _FocusRadius;
            float _FocusStrength, _FocusOpacity;
            struct A { float4 p:POSITION; half4 c:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; half4 c:COLOR; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; float3 world:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.p=TransformObjectToHClip(v.p.xyz); o.world=TransformObjectToWorld(v.p.xyz);
                o.c=v.c*_Color; o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.local=v.p.xy; return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 c=(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)+_TextureSampleAdd)*i.c;
                // A shared centre-eye cone uses world positions, so both eyes reveal
                // the same physical patch. No eye-tracking device is required.
                float3 delta=i.world-_FocusOrigin.xyz;
                float depth=dot(delta,_FocusForward.xyz);
                float2 ellipse=float2(dot(delta,_FocusRight.xyz),dot(delta,_FocusUp.xyz))/max(.001,depth)/max(float2(.001,.001),_FocusRadius.xy);
                float opacity=lerp(_FocusOpacity,1,smoothstep(.42,1,length(ellipse)));
                c.a*=lerp(1,opacity,saturate(_FocusStrength)*step(.001,depth));
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=step(_ClipRect.x,i.local.x)*step(i.local.x,_ClipRect.z)*step(i.local.y,_ClipRect.w)*step(_ClipRect.y,i.local.y);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDHLSL
        }
    }
}
