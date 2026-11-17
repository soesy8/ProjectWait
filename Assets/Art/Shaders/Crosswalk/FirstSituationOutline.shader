Shader "Y/Crosswalk/ObservationOutline" // 원본 물체 위에 실루엣만 표시하는 첫 번째 상황 전용 셰이더입니다.
{ // 외곽선 속성은 대상별 MaterialPropertyBlock으로 전달합니다.
    Properties // Inspector에서 머티리얼의 기본값도 편집할 수 있습니다.
    { // 원본 모델의 색을 유지하며 강조용 표면에만 파란빛을 표시합니다.
        [HDR] _OutlineColor ("발광 테두리 색상", Color) = (0.015, 0.1, 1.75, 1) // 진한 파란 중심선과 주변 광채에 같은 색을 사용합니다.
        _OutlineWidth ("테두리 두께", Float) = 0.048 // 기존 기본값의 네 배 두께로 실루엣을 확장합니다.
        _GlowWidth ("빛 번짐 폭 배율", Range(1.1, 5)) = 2.6 // 중심선보다 넓은 표면으로 주변 광채를 만듭니다.
        _GlowIntensity ("빛 번짐 밝기", Range(0, 4)) = 1.8 // 주변 배경에 더할 파란빛의 세기를 정합니다.
        _Primitive ("단순 도형의 모서리 연결", Float) = 0 // 각진 소품은 위치 방향으로 확장해 모서리 틈을 줄입니다.
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("테두리 면 표시", Float) = 1 // 일반 물체는 앞면을 제거하고 횡단보도의 얇은 윤곽 띠만 양면 표시를 선택합니다.
    } // 원본 깊이 버퍼 뒤에서 확대된 뒷면만 그립니다.
    SubShader // 현재 프로젝트의 Universal Render Pipeline에만 사용합니다.
    { // 불투명 원본 렌더링이 끝난 뒤 외곽선을 표시합니다.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" } // 물체의 실제 표면 깊이가 내부 윤곽을 가리게 합니다.
        HLSLINCLUDE // 두 패스가 같은 원본 스킨 좌표와 XR 변환을 공유합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 설치된 URP의 좌표 변환 API를 사용합니다.
            CBUFFER_START(UnityPerMaterial) // 머티리얼 상수의 레이아웃을 고정합니다.
                half4 _OutlineColor; // 대상별 윤곽 색과 알파입니다.
                float _OutlineWidth; // 월드 공간 확장 거리입니다.
                float _GlowWidth; // 중심선에 대한 광채의 확장 배율입니다.
                half _GlowIntensity; // 주변에 더하는 파란빛의 밝기입니다.
                float _Primitive; // 정점 중복이 있는 큐브 모서리의 확장 방식을 선택합니다.
            CBUFFER_END // 머티리얼 상수 선언을 마칩니다.
            struct Attributes // 기존 메시와 스키닝 결과를 입력으로 받습니다.
            { // XR 눈 인덱스와 메시 정점 정보를 전달합니다.
                float4 positionOS : POSITION; // 기존 스킨 메시의 현재 정점 위치입니다.
                float3 normalOS : NORMAL; // 기존 모델의 부드러운 표면 법선입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 양눈의 인스턴스 식별자를 보관합니다.
            }; // 메시 입력 선언을 마칩니다.
            struct Varyings // 클립 좌표와 스테레오 정보를 픽셀 단계에 전달합니다.
            { // 색상은 머티리얼 상수에서 직접 읽습니다.
                float4 positionCS : SV_POSITION; // 확대된 윤곽 정점의 화면 좌표입니다.
                float3 positionWS : TEXCOORD0; // 각 눈의 카메라를 향하는 방향을 구할 광채 표면 위치입니다.
                half3 expansionWS : TEXCOORD1; // 광채 바깥 가장자리의 투명도를 구할 확장 방향입니다.
                UNITY_VERTEX_OUTPUT_STEREO // 양눈의 올바른 렌더 타깃을 선택합니다.
            }; // 보간 구조를 마칩니다.
            Varyings BuildOutline(Attributes input, float widthScale) // 선과 광채를 같은 원본에서 서로 다른 두께로 확장합니다.
            { // 실제 XR 카메라의 투영행렬은 변경하지 않습니다.
                Varyings output = (Varyings)0; // 현재 정점의 출력 성분을 모두 초기화합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 현재 눈의 인스턴스를 선택합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 스테레오 출력 인덱스를 전달합니다.
                float3 axisX = TransformObjectToWorldDir(float3(1,0,0)); // 가로축 방향에서 비균일 크기 배율을 제외합니다.
                float3 axisY = TransformObjectToWorldDir(float3(0,1,0)); // 얇은 보도와 휴대전화의 높이 배율이 윤곽을 없애지 않게 합니다.
                float3 axisZ = TransformObjectToWorldDir(float3(0,0,1)); // 깊이축도 회전 방향만 사용합니다.
                float3 primitiveDirection = normalize(axisX * input.positionOS.x + axisY * input.positionOS.y + axisZ * input.positionOS.z + float3(0.000001,0.000001,0.000001)); // 각진 소품의 모서리를 크기와 무관한 같은 월드 두께로 확장합니다.
                float3 outlineDirection = normalize(lerp(TransformObjectToWorldNormal(input.normalOS), primitiveDirection, _Primitive)); // 모델 종류에 맞는 확장 방향을 일정한 월드 길이로 사용합니다.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz); // 스킨 또는 소품의 실제 월드 위치를 구합니다.
                positionWS += outlineDirection * (_OutlineWidth * widthScale); // 중심선과 주변 광채를 대상별 두께에 맞게 확장합니다.
                output.positionCS = TransformWorldToHClip(positionWS); // 양눈의 기존 시야에 같은 외곽선을 투영합니다.
                output.positionWS = positionWS; // 실제 확장 표면의 위치로 양눈의 시선 방향을 구합니다.
                output.expansionWS = outlineDirection; // 광채의 가장자리 투명도를 구할 방향을 전달합니다.
                return output; // 화면 안쪽은 원본 깊이로 가리고 실루엣 가장자리만 남깁니다.
            } // 정점 변환을 마칩니다.
            Varyings Vert(Attributes input) { return BuildOutline(input, 1.0); } // 진한 중심선에는 저장된 대상별 두께를 그대로 적용합니다.
            Varyings VertGlow(Attributes input) { return BuildOutline(input, _GlowWidth); } // 중심선보다 넓은 표면으로 파란 광채를 만듭니다.
            half4 Frag(Varyings input) : SV_Target // 선명한 파란 중심선과 기존 페이드 알파를 출력합니다.
            { // 원본 머티리얼의 텍스처나 색을 참조하지 않습니다.
                return _OutlineColor; // 조준 전환은 대상별 색상의 알파로 적용합니다.
            } // 윤곽 픽셀 출력을 마칩니다.
            half4 FragGlow(Varyings input) : SV_Target // 대상 주변의 배경에 부드러운 파란빛을 더합니다.
            { // 후처리 설정에 의존하지 않고 외곽선 주변에만 광채를 표시합니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 눈의 카메라 위치로 광채를 계산합니다.
                half facing = abs(dot(normalize(input.expansionWS), GetWorldSpaceNormalizeViewDir(input.positionWS))); // 광채 표면이 시선과 평행해지는 바깥 가장자리를 찾습니다.
                half softness = smoothstep(0.0h, 0.65h, facing); // 바깥 가장자리로 갈수록 빛을 부드럽게 투명하게 만듭니다.
                return half4(_OutlineColor.rgb * _GlowIntensity, _OutlineColor.a * softness * 0.5h); // 기존 조준 페이드에 맞춰 파란 광채도 함께 나타나고 사라지게 합니다.
            } // 광채 픽셀 출력을 마칩니다.
        ENDHLSL // 두 패스의 공통 좌표와 색상 계산을 마칩니다.
        Pass // 넓은 파란 광채를 중심선 뒤에 합성합니다.
        { // 원본 표면 깊이를 존중하면서 주변 배경에 빛을 더합니다.
            Name "ObservationGlow" // 클릭 가능한 대상의 빛 번짐 패스입니다.
            Tags { "LightMode"="SRPDefaultUnlit" } // URP의 추가 표시 패스로 광채를 함께 그립니다.
            Cull [_Cull] // 기존 대상의 실루엣 면 설정을 유지합니다.
            ZWrite Off // 광채가 장면의 깊이 정보를 바꾸지 않게 합니다.
            ZTest LEqual // 다른 물체 뒤에 가린 광채는 표시하지 않습니다.
            Blend SrcAlpha One // 배경에 파란빛을 더하여 발광하는 모습을 만듭니다.
            HLSLPROGRAM // 광채 표면을 그리는 진입점을 지정합니다.
            #pragma target 3.5 // 프로젝트의 URP와 XR 인스턴싱을 지원합니다.
            #pragma vertex VertGlow // 중심선보다 넓은 정점 표면을 사용합니다.
            #pragma fragment FragGlow // 가장자리에서 사라지는 광채를 출력합니다.
            #pragma multi_compile_instancing // 스테레오 인스턴싱 렌더링을 지원합니다.
            ENDHLSL // 광채 프로그램을 마칩니다.
        } // 빛 번짐 패스를 마칩니다.
        Pass // 두껍고 진한 파란 중심선을 표시합니다.
        { // 광채 위에서도 클릭 가능한 대상의 형태를 또렷하게 유지합니다.
            Name "ObservationOutline" // 기존 실루엣 외곽선의 표시 패스입니다.
            Tags { "LightMode"="UniversalForward" } // 같은 대상의 중심선을 URP 전방 표시 패스에서 그립니다.
            Cull [_Cull] // 기존 실루엣과 바닥 윤곽 띠의 면 설정을 유지합니다.
            ZWrite Off // 원본 모델과 다른 UI의 깊이를 변경하지 않습니다.
            ZTest LEqual // 원본 표면과 장애물 뒤의 윤곽을 가립니다.
            Blend SrcAlpha OneMinusSrcAlpha // 기존 조준 진입과 이탈의 알파 페이드를 유지합니다.
            HLSLPROGRAM // 진한 중심선을 그리는 진입점을 지정합니다.
            #pragma target 3.5 // 광채 패스와 같은 URP 및 XR 기능을 사용합니다.
            #pragma vertex Vert // 대상별로 저장한 두꺼운 윤곽 표면을 사용합니다.
            #pragma fragment Frag // 조명에 어두워지지 않는 파란 중심선을 출력합니다.
            #pragma multi_compile_instancing // 기존 XR 양눈 렌더링을 지원합니다.
            ENDHLSL // 중심선 프로그램을 마칩니다.
        } // 외곽선 패스를 마칩니다.
    } // URP 전용 서브셰이더를 마칩니다.
} // 첫 번째 상황의 실루엣 표현을 마칩니다.
