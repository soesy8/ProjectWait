Shader "Y/Crosswalk/ObservationCharacterSilhouette" // 친구 전체를 한 덩어리로 보고 화면에 드러난 실루엣만 강조합니다.
{ // 관찰 컴포넌트가 전달하는 색상과 두께를 그대로 받습니다.
    Properties // 친구용 강조 재질에만 사용합니다.
    { // 씬에서 친구마다 지정한 관찰 색과 조준 색을 적용합니다.
        [HDR] _OutlineColor("윤곽선 색상", Color) = (0.16, 0.53, 0.72, 0.88) // 표시가 조준 상태에 따라 같은 외곽에서 색만 바뀝니다.
        _OutlineWidth("윤곽선 두께(m)", Float) = 0.009 // 가까이에서 보아도 얼굴이나 옷을 덮지 않는 두께입니다.
    } // 색과 두께는 다른 관찰 표시와 같은 프로퍼티를 사용합니다.
    SubShader // 원본 캐릭터와 배경이 깊이를 기록한 뒤 강조를 표시합니다.
    { // 기존 소품의 광채 셰이더에는 영향을 주지 않습니다.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" } // 불투명 캐릭터 뒤에 실루엣 패스를 실행합니다.
        HLSLINCLUDE // 스텐실 마스크와 윤곽이 동일한 스킨 및 XR 좌표를 사용합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 기존 URP의 월드 변환과 양안 투영을 사용합니다.
            CBUFFER_START(UnityPerMaterial) // 두 패스의 재질 상수 배치를 일치시킵니다.
                half4 _OutlineColor; // 관찰 컴포넌트에서 적용한 현재 색과 투명도입니다.
                float _OutlineWidth; // 외곽을 확장할 월드 공간 길이입니다.
            CBUFFER_END // 재질 값의 정의를 마칩니다.
            struct Attributes // 기존 스킨 렌더러가 포즈를 적용한 정점을 입력받습니다.
            { // 별도 메시나 관절 애니메이션 없이 같은 형태를 따라갑니다.
                float4 positionOS : POSITION; // 포즈가 반영된 메시 정점입니다.
                float3 normalOS : NORMAL; // 확장된 표면이 바깥을 향하도록 사용하는 법선입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 양안 렌더링에서 현재 눈을 구분합니다.
            }; // 정점 입력의 끝입니다.
            struct Varyings // 픽셀 단계에는 투영 좌표와 눈 정보만 전달합니다.
            { // 색상은 현재 재질에서 직접 읽습니다.
                float4 positionCS : SV_POSITION; // 현재 눈으로 투영한 실루엣 위치입니다.
                UNITY_VERTEX_OUTPUT_STEREO // VR에서는 눈마다 독립적인 대상에 기록합니다.
            }; // 보간 데이터의 끝입니다.
            Varyings ProjectSilhouette(Attributes input, float width) // 원본과 확장된 정점의 투영 방식을 일치시킵니다.
            { // 마스크와 선이 양안에서 같은 위치에 정렬되도록 합니다.
                Varyings output = (Varyings)0; // 플랫폼별 스테레오 출력을 초기화합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 현재 정점의 인스턴스와 눈을 설정합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 픽셀 단계에서도 같은 눈을 사용하도록 지정합니다.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz); // 현재 포즈의 정점을 월드 공간에 놓습니다.
                positionWS += TransformObjectToWorldNormal(input.normalOS) * width; // 마스크는 원래 크기를 유지하고 선만 바깥으로 넓힙니다.
                output.positionCS = TransformWorldToHClip(positionWS); // 현재 눈의 화면 좌표로 변환합니다.
                return output; // 해당 패스가 그릴 실루엣 좌표를 전달합니다.
            } // 공통 투영을 마칩니다.
            Varyings MaskVertex(Attributes input) { return ProjectSilhouette(input, 0.0); } // 확장하지 않은 몸 전체의 화면 영역을 기록합니다.
            Varyings OutlineVertex(Attributes input) { return ProjectSilhouette(input, _OutlineWidth); } // 몸 바깥으로 얇게 확장된 표면을 사용합니다.
            half4 MaskFragment() : SV_Target { return 0; } // 색을 그리지 않고 몸의 화면 영역만 스텐실에 남깁니다.
            half4 OutlineFragment() : SV_Target { return _OutlineColor; } // 조명이나 광채를 더하지 않은 단색 선을 그립니다.
        ENDHLSL // 두 패스에서 공유하는 투영 계산의 끝입니다.
        Pass // 옷과 머리 및 피부의 모든 조각을 하나의 화면 마스크로 기록합니다.
        { // 깊이 비교로 내부 메시 일부가 빠지지 않게 원래 크기의 전체 투영을 기록합니다.
            Name "CharacterSilhouetteMask" // 확장 패스가 몸 안쪽을 걸러 내는 데 쓰는 마스크입니다.
            Tags { "LightMode"="SRPDefaultUnlit" } // 기존 URP 전방 렌더링의 첫 표시 패스입니다.
            Cull Off // 얇은 눈이나 머리 부분도 양쪽에서 같은 마스크를 기록합니다.
            ZTest Always // 화면에서 원래 크기의 몸이 차지하는 모든 부분을 포함합니다.
            ZWrite Off // 원래 캐릭터와 주변 물체의 깊이를 유지합니다.
            ColorMask 0 // 캐릭터 표면이나 배경의 색상은 건드리지 않습니다.
            Stencil { Ref 1 WriteMask 1 Comp Always Pass Replace } // 다른 스텐실 비트는 보존하면서 몸 안쪽을 표시합니다.
            HLSLPROGRAM // 화면 마스크만 기록하는 셰이더 진입부입니다.
            #pragma target 3.5 // 기존 XR 인스턴스 표시 기능을 사용합니다.
            #pragma vertex MaskVertex // 포즈를 적용한 원본 크기를 사용합니다.
            #pragma fragment MaskFragment // 이 패스에서 색이 나타나지 않게 합니다.
            #pragma multi_compile_instancing // 각 눈의 마스크를 해당 눈에 기록합니다.
            ENDHLSL // 실루엣 마스크 프로그램의 끝입니다.
        } // 원본 실루엣을 기록한 뒤 외곽선을 그립니다.
        Pass // 원래 몸의 투영 밖에서만 얇은 선이 보이도록 제한합니다.
        { // 확장된 옷 주름과 얼굴 부품이 몸 내부에서 비치는 것을 막습니다.
            Name "CharacterSilhouetteOutline" // 몸 바깥에 실제 색을 그리는 패스입니다.
            Tags { "LightMode"="UniversalForward" } // 마스크 다음의 기존 URP 전방 표시 패스입니다.
            Cull Front // 바깥으로 확장한 표면의 뒷면을 사용해 윤곽선 모양을 만듭니다.
            ZTest LEqual // 친구보다 앞에 있는 실제 물체 뒤에는 선을 그리지 않습니다.
            ZWrite Off // 강조가 다른 물체의 가림을 변경하지 않습니다.
            Stencil { Ref 1 ReadMask 1 Comp NotEqual } // 원래 몸이 기록된 픽셀을 빼서 피부와 옷 내부에는 선을 표시하지 않습니다.
            Blend SrcAlpha OneMinusSrcAlpha // 기존 관찰 등장과 퇴장의 알파를 그대로 적용합니다.
            HLSLPROGRAM // 바깥 윤곽에만 현재 관찰 색을 출력합니다.
            #pragma target 3.5 // 마스크와 같은 XR 셰이더 기능을 사용합니다.
            #pragma vertex OutlineVertex // 원본 메시에서 얇게 확장한 형태를 투영합니다.
            #pragma fragment OutlineFragment // 빛 번짐 없이 설정된 색을 출력합니다.
            #pragma multi_compile_instancing // VR 양안에 같은 윤곽 표시를 제공합니다.
            ENDHLSL // 외곽선 프로그램의 끝입니다.
        } // 친구의 실루엣 강조를 마칩니다.
    } // URP 전방 렌더링에서만 사용합니다.
} // 친구 관찰용 외곽선 셰이더의 끝입니다.
