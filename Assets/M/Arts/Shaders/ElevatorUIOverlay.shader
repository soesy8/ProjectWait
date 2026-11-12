Shader "ProjectWait/M/UI Overlay" // M의 UI 재질이 참조하는 같은 셰이더 이름을 사용합니다.
{ // 원본 종이 그림과 버튼 텍스처를 월드 UI에 표시합니다.
    Properties // Unity UI의 이미지·색상·마스크 속성을 받습니다.
    { // M에서 저장한 UIOverlay 재질의 속성 이름을 유지합니다.
        [PerRendererData] _MainTex("이미지", 2D) = "white" {} // 각 UI Image가 지정한 M 그림을 읽습니다.
        _Color("표시 색상", Color) = (1,1,1,1) // 버튼의 현재 전환 색상을 이미지에 곱합니다.
        _StencilComp("마스크 비교", Float) = 8 // 기존 UI 마스크의 비교 방식을 받습니다.
        _Stencil("마스크 번호", Float) = 0 // 부모 UI 마스크의 번호입니다.
        _StencilOp("마스크 연산", Float) = 0 // UI 마스크가 요청한 연산을 사용합니다.
        _StencilWriteMask("마스크 쓰기 범위", Float) = 255 // 마스크에 기록할 비트를 지정합니다.
        _StencilReadMask("마스크 읽기 범위", Float) = 255 // 비교에 사용할 마스크 비트를 지정합니다.
        _ColorMask("색상 기록 범위", Float) = 15 // 원본 재질의 RGBA 출력 범위를 유지합니다.
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("투명 영역 자르기", Float) = 0 // Unity Mask가 요청하면 투명 픽셀을 제거합니다.
    } // 종이 모양은 셰이더가 다시 그리지 않고 원본 스프라이트를 사용합니다.
    SubShader // 원본 M UI와 같은 URP 오버레이 표시를 적용합니다.
    { // 월드 물체에 글과 버튼이 가려지지 않는 렌더링 설정입니다.
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" } // M 재질과 스프라이트 아틀라스의 표시 순서를 유지합니다.
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] } // 부모 UI의 기존 마스크를 적용합니다.
        Cull Off // 고개를 돌려 뒤쪽에서 보더라도 월드 UI의 면이 사라지지 않게 합니다.
        ZWrite Off // 반투명 UI가 장면의 깊이값을 덮어쓰지 않게 합니다.
        ZTest Always // M의 읽기 카드와 같은 가림 방지 표시를 사용합니다.
        Blend SrcAlpha OneMinusSrcAlpha // 그림 가장자리와 CanvasGroup의 투명도를 합성합니다.
        ColorMask [_ColorMask] // UI 마스크가 지정한 색상 채널만 출력합니다.
        Pass // 원본 이미지에 색상과 마스크를 적용하는 한 번의 표시입니다.
        { // UI 정점과 픽셀 처리를 정의합니다.
            HLSLPROGRAM // 현재 프로젝트의 URP 셰이더 언어를 사용합니다.
            #pragma vertex Vert // 씬 RectTransform의 실제 위치를 화면으로 변환합니다.
            #pragma fragment Frag // M의 스프라이트 색상과 투명도를 출력합니다.
            #pragma multi_compile_instancing // 기존 XR 렌더링의 인스턴싱을 지원합니다.
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT // RectMask2D의 사각형 마스크를 지원합니다.
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP // UI 스텐실 마스크의 투명 영역 제거를 지원합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 설치된 URP의 좌표 변환과 XR 매크로를 사용합니다.
            TEXTURE2D(_MainTex); // UI Image가 전달한 원본 그림 텍스처입니다.
            SAMPLER(sampler_MainTex); // 원본 스프라이트의 필터 설정을 사용합니다.
            float4 _MainTex_ST, _ClipRect; // 텍스처 좌표 변환과 부모 마스크의 범위입니다.
            half4 _Color, _TextureSampleAdd; // 재질 색상과 Unity UI가 전달하는 텍스처 보정값입니다.
            struct Attributes // Canvas가 전달하는 원래 UI 메시입니다.
            { // 이미지의 위치와 색상 및 아틀라스 좌표를 읽습니다.
                float4 position : POSITION; // RectTransform의 로컬 정점 위치입니다.
                half4 color : COLOR; // 버튼과 CanvasGroup이 계산한 표시 색상입니다.
                float2 uv : TEXCOORD0; // 원본 M 그림을 읽을 스프라이트 좌표입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 XR 인스턴스를 전달받습니다.
            }; // UI 입력 정점의 구성입니다.
            struct Varyings // 픽셀에 전달할 이미지와 마스크 정보입니다.
            { // XR 눈별 좌표와 UI 로컬 위치를 함께 유지합니다.
                float4 position : SV_POSITION; // 현재 눈의 화면에 투영한 정점 위치입니다.
                half4 color : COLOR; // 재질과 버튼 색상을 합친 값입니다.
                float2 uv : TEXCOORD0; // 변환된 원본 스프라이트 좌표입니다.
                float2 local : TEXCOORD1; // RectMask2D가 사용할 원래 로컬 위치입니다.
                UNITY_VERTEX_OUTPUT_STEREO // 현재 XR 눈의 렌더링 정보를 보존합니다.
            }; // UI 픽셀 입력의 구성입니다.
            Varyings Vert(Attributes input) // 기존 UI 메시를 현재 카메라에 투영합니다.
            { // 화면 배치와 원본 그림 좌표를 함께 전달합니다.
                Varyings output; // 현재 UI 정점의 출력값을 준비합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 현재 인스턴스의 좌표 변환을 선택합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 해당 XR 눈에 맞는 출력을 설정합니다.
                output.position = TransformObjectToHClip(input.position.xyz); // 실제 월드 Canvas 위치를 화면으로 변환합니다.
                output.color = input.color * _Color; // UI 상태의 색상을 재질 색상과 결합합니다.
                output.uv = TRANSFORM_TEX(input.uv, _MainTex); // 원본 이미지의 텍스처 좌표를 적용합니다.
                output.local = input.position.xy; // 원래 UI 좌표를 마스크 판정에 전달합니다.
                return output; // 준비한 이미지 정점 정보를 픽셀 처리로 넘깁니다.
            } // 카메라나 원본 RectTransform은 변경하지 않습니다.
            half4 Frag(Varyings input) : SV_Target // 원본 M 그림을 UI 상태에 맞춰 표시합니다.
            { // 종이 무늬와 갈색 테두리는 스프라이트에 저장된 모양을 사용합니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 XR 눈의 픽셀 상태를 적용합니다.
                half4 color = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd) * input.color; // 원본 그림과 Unity UI 색상 보정을 결합합니다.
                #ifdef UNITY_UI_CLIP_RECT // 부모 RectMask2D가 활성화된 경우에만 범위를 자릅니다.
                color.a *= step(_ClipRect.x, input.local.x) * step(input.local.x, _ClipRect.z) * step(_ClipRect.y, input.local.y) * step(input.local.y, _ClipRect.w); // 마스크 밖의 UI 픽셀을 투명하게 만듭니다.
                #endif // 부모 사각형 마스크 적용을 마칩니다.
                #ifdef UNITY_UI_ALPHACLIP // 스텐실 마스크가 요청한 투명 영역만 제거합니다.
                clip(color.a - .001); // 그림의 투명한 가장자리가 마스크에 남지 않게 합니다.
                #endif // 투명 영역 제거를 마칩니다.
                return color; // 원본 종이 UI와 버튼의 최종 색상을 출력합니다.
            } // M과 같은 이미지 합성 처리를 마칩니다.
            ENDHLSL // UI 셰이더 프로그램의 끝입니다.
        } // 이미지 표시 패스의 끝입니다.
    } // URP 표시 설정의 끝입니다.
} // 원본 M 스프라이트를 사용하는 UI 셰이더입니다.
