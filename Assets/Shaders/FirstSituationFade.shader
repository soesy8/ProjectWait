Shader "Y/Crosswalk/ImmersiveFade" // 헤드셋 추적을 유지한 채 배치 전환만 가리는 전체 시야 페이드입니다.
{ // 미리 배치한 카메라 자식 구체의 안쪽 면을 표시합니다.
    Properties // 위치 복원 암전과 상황별 주변광을 같은 표면에서 합성합니다.
    { // 두 효과 모두 기본값은 투명하여 평상시 시야를 유지합니다.
        _FadeColor ("전환막 색상", Color) = (0.04,0.05,0.07,0) // 기존 위치 복원 암전의 색과 알파입니다.
        _CueColor ("상황 강조 색상", Color) = (0,0,0,0) // 관찰과 위험 및 안전 완료 순간의 주변광입니다.
    } // 전환막 속성 선언을 마칩니다.
    SubShader // 기존 URP 렌더러에서 별도 카메라 변경 없이 사용합니다.
    { // 월드 물체와 UI 위에 전환 알파만 합성합니다.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent" } // 장면을 가릴 때 주변 시야까지 동일하게 전환합니다.
        Pass // 안쪽에서 보이는 구체를 한 번만 그립니다.
        { // 깊이와 카메라 추적 데이터는 수정하지 않습니다.
            Cull Front // 실제 카메라가 구체 안에 있으므로 안쪽 면만 표시합니다.
            ZTest Always // 가까운 컨트롤러나 물체가 불투명 전환막을 뚫고 보이지 않게 합니다.
            ZWrite Off // 장면과 UI의 깊이 버퍼를 보존합니다.
            Blend SrcAlpha OneMinusSrcAlpha // 부드러운 투명도 전환을 적용합니다.
            HLSLPROGRAM // 설치된 URP와 XR 스테레오 변환을 사용합니다.
            #pragma vertex Vert // 기존 구체 정점을 그대로 투영합니다.
            #pragma fragment Frag // 위치 복원 순간의 색상과 알파만 출력합니다.
            #pragma target 3.5 // 현재 프로젝트의 URP와 XR 화면 좌표 기능을 사용합니다.
            #pragma multi_compile_instancing // 양눈의 스테레오 인스턴싱을 지원합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 현재 프로젝트 URP의 좌표 변환을 사용합니다.
            CBUFFER_START(UnityPerMaterial) // 머티리얼 상수의 구조를 고정합니다.
                half4 _FadeColor; // UI 제어기에서 전달한 현재 색상과 알파입니다.
                half4 _CueColor; // 현재 상황의 화면 가장자리 색상과 강조 알파입니다.
            CBUFFER_END // 머티리얼 상수 선언을 마칩니다.
            struct Attributes // 기존 구체 메시의 정점과 눈 인덱스를 받습니다.
            { // 추가 메시나 투영 행렬을 만들지 않습니다.
                float4 positionOS : POSITION; // 카메라 자식 구체의 실제 정점입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 눈의 인스턴스를 받습니다.
            }; // 정점 입력을 마칩니다.
            struct Varyings // 클립 위치와 스테레오 인덱스를 전달합니다.
            { // 색상은 머티리얼 상수에서 직접 읽습니다.
                float4 positionCS : SV_POSITION; // 기존 카메라의 투영을 그대로 사용합니다.
                UNITY_VERTEX_OUTPUT_STEREO // 현재 눈의 출력 대상을 보존합니다.
            }; // 정점 출력을 마칩니다.
            Varyings Vert(Attributes input) // 자연스러운 머리 추적을 따라 전환막만 렌더링합니다.
            { // 실제 XR 카메라의 위치·회전·시야각을 제어하지 않습니다.
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 양눈의 현재 인스턴스를 준비합니다.
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); return output; // 저장된 구체를 기존 시야로 투영합니다.
            } // 정점 변환을 마칩니다.
            half4 Frag(Varyings input) : SV_Target // 기존 암전 위에 중앙을 비워 둔 상황별 주변광을 합성합니다.
            { // XR의 현재 눈마다 같은 화면 비율로 가장자리만 물들입니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 눈의 화면 좌표와 렌더링 상태를 선택합니다.
                float2 centered = GetNormalizedScreenSpaceUV(input.positionCS.xy) * 2.0 - 1.0; // 실제 렌더 크기에 맞는 화면 중심 기준 좌표입니다.
                half edge = smoothstep(0.32, 1.2, length(centered * float2(0.9, 1.0))); // 중앙의 배우와 도로는 비워 두고 주변으로 갈수록 강조합니다.
                half cueAlpha = saturate(_CueColor.a * edge) * (1.0h - _FadeColor.a); // 위치 복원 암전이 진해질수록 주변광을 가려 전환막을 보존합니다.
                half alpha = _FadeColor.a + cueAlpha; // 기존 암전과 주변광을 중복해 불투명해지지 않도록 합칩니다.
                half3 color = (_FadeColor.rgb * _FadeColor.a + _CueColor.rgb * cueAlpha) / max(alpha, 0.0001h); // 일반 알파 합성에 맞게 두 효과의 실제 색상을 계산합니다.
                return half4(color, alpha); // 기존 깊이와 시야 추적을 유지한 채 표시 효과만 출력합니다.
            } // 전환막과 상황별 주변광의 픽셀 처리를 마칩니다.
            ENDHLSL // 셰이더 프로그램을 마칩니다.
        } // 전환막 패스를 마칩니다.
    } // URP 전용 서브셰이더를 마칩니다.
} // 전체 시야 페이드를 마칩니다.
