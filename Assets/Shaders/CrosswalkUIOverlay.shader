Shader "Y/Crosswalk/UIOverlay" // 기존 월드 UI의 둥근 이미지와 글자가 환경 깊이에 가려지지 않게 합니다.
{ // 실제 Canvas와 스프라이트를 그대로 사용합니다.
    Properties // 기존 Unity UI의 이미지·마스크·색상 입력을 받습니다.
    { // Inspector에서 재질 값을 편집할 수 있습니다.
        [PerRendererData] _MainTex ("이미지", 2D) = "white" {} // 각 Image와 RawImage의 실제 텍스처입니다.
        _Color ("색상", Color) = (1,1,1,1) // 기존 파스텔 Image 색상을 그대로 곱합니다.
        _StencilComp ("마스크 비교", Float) = 8 // 기존 UI Mask의 스텐실 설정입니다.
        _Stencil ("마스크 번호", Float) = 0 // Canvas에서 전달할 마스크 번호입니다.
        _StencilOp ("마스크 연산", Float) = 0 // 기존 이미지 마스크 연산입니다.
        _StencilWriteMask ("마스크 기록", Float) = 255 // 스텐실 기록 범위입니다.
        _StencilReadMask ("마스크 읽기", Float) = 255 // 스텐실 비교 범위입니다.
        _ColorMask ("색상 기록", Float) = 15 // RGBA 색상 채널을 표시합니다.
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("투명 영역 자르기", Float) = 0 // UI Mask에서 사용하는 알파 클립입니다.
    } // 디자인에 쓰인 스프라이트는 변경하지 않습니다.
    SubShader // 현재 프로젝트의 URP와 스테레오 UI를 지원합니다.
    { // 모든 패널은 실제 Hierarchy의 Canvas 정렬을 따릅니다.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" } // 환경 뒤에 가려지지 않는 UI 렌더링 순서입니다.
        Pass // 기존 Image 메시를 한 번 그립니다.
        { // 입력 판정과 깊이 버퍼는 변경하지 않습니다.
            Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] } // 기존 UI 마스크를 유지합니다.
            Cull Off // 월드 공간 양면 UI를 유지합니다.
            ZWrite Off // 다른 UI나 장면의 깊이를 덮어쓰지 않습니다.
            ZTest Always // 캐릭터·신호등·배경이 텍스트와 배경을 잘라내지 않게 합니다.
            Blend SrcAlpha OneMinusSrcAlpha // 이미지의 부드러운 가장자리와 그림자 알파를 보존합니다.
            ColorMask [_ColorMask] // 기존 Canvas 색상 마스크를 적용합니다.
            HLSLPROGRAM // Unity 6 URP의 셰이더 변환을 사용합니다.
            #pragma vertex Vert // 실제 UI 정점의 위치를 변환합니다.
            #pragma fragment Frag // 원래 이미지 색상과 투명도를 출력합니다.
            #pragma multi_compile_instancing // VR 양눈의 인스턴스 정보를 사용합니다.
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT // RectMask2D의 클립 범위를 지원합니다.
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP // UI Mask의 투명 영역 제거를 지원합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 설치된 URP의 좌표 변환과 XR 매크로를 사용합니다.
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); // 현재 UI의 스프라이트 또는 RenderTexture입니다.
            CBUFFER_START(UnityPerMaterial) // 재질 상수의 레이아웃을 고정합니다.
                float4 _MainTex_ST, _ClipRect; half4 _Color, _TextureSampleAdd; // 원래 텍스처 좌표·클립 범위·색상입니다.
            CBUFFER_END // 재질 상수 선언을 마칩니다.
            struct Attributes // 기존 Canvas 메시의 정점 입력입니다.
            { // 런타임에 메시나 UI를 만들지 않습니다.
                float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; // 실제 RectTransform 메시의 위치·색·UV입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 VR 눈 인덱스를 받습니다.
            }; // 정점 입력 구조입니다.
            struct Varyings // 픽셀 단계에 전달할 기존 UI 정보입니다.
            { // UI의 클립 좌표는 로컬 정점 위치를 사용합니다.
                float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 local : TEXCOORD1; // 화면 위치와 이미지 좌표입니다.
                UNITY_VERTEX_OUTPUT_STEREO // 양눈 투영 정보를 유지합니다.
            }; // 픽셀 입력 구조입니다.
            Varyings Vert(Attributes input) // UI 위치를 현재 눈의 화면 좌표로 변환합니다.
            { // 카메라의 추적이나 투영값을 수정하지 않습니다.
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 실제 스테레오 눈 정보를 초기화합니다.
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); output.local = input.positionOS.xy; // 실제 UI 정점을 투영하고 마스크 기준을 보존합니다.
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw; output.color = input.color * _Color; return output; // 스프라이트 색과 CanvasGroup 알파를 전달합니다.
            } // 패널의 위치와 크기는 RectTransform이 결정합니다.
            half4 Frag(Varyings input) : SV_Target // 원래 파스텔 이미지를 표시합니다.
            { // 투명 픽셀은 배경을 그대로 보이게 합니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); half4 color = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd) * input.color; // 원래 그림자·모서리·색상을 보존합니다.
                #ifdef UNITY_UI_CLIP_RECT // RectMask2D가 있는 경우만 사각형 클립을 적용합니다.
                    float2 inside = step(_ClipRect.xy, input.local) * step(input.local, _ClipRect.zw); color.a *= inside.x * inside.y; // 실제 마스크 안의 이미지 알파만 남깁니다.
                #endif // 클립이 없는 패널은 원래 알파를 유지합니다.
                #ifdef UNITY_UI_ALPHACLIP // 기존 UI Mask가 요청한 투명 픽셀 제거입니다.
                    clip(color.a - 0.001h); // 투명한 가장자리를 스텐실에 기록하지 않습니다.
                #endif // 기본 둥근 이미지는 부드러운 알파를 그대로 사용합니다.
                return color; // 실제 Image와 CanvasGroup의 합성 색상을 출력합니다.
            } // 임시 패널이나 텍스트를 생성하지 않습니다.
            ENDHLSL // UI 셰이더를 마칩니다.
        } // 기존 디자인 자산을 재사용합니다.
    } // 씬에 지정한 UI 재질에만 적용합니다.
} // 다른 월드 오브젝트의 머티리얼은 변경하지 않습니다.
