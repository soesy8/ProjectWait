Shader "Y/Crosswalk/CleanCartoon" // 기존 모델의 큰 색면에 세 단계 명암과 얇은 윤곽선을 표현합니다.
{ // 재질의 기본 색상은 제작된 머티리얼에서 공급합니다.
    Properties // 별도의 인스펙터 조절 항목을 추가하지 않는 내부 재질 속성입니다.
    { // 기존 모델 팔레트와 추가할 명암·윤곽선의 내부 기본값을 직렬화합니다.
        [HideInInspector] _Cull("원본 면 표시", Float) = 2 // 얼굴 재질의 원본 양면 표시를 보존하고 나머지 재질은 기존 뒷면 숨김을 유지합니다.
        [HideInInspector] _BaseColor("원본 선형 색상", Vector) = (1, 1, 1, 1) // Blender 노드의 선형 색상이 Color 속성의 감마 변환으로 다시 바뀌지 않게 합니다.
        [HideInInspector] _ShadeStrength("그늘 밝기", Float) = 0.82 // 부드러운 점토형 그라데이션 대신 일정한 그늘 밝기를 사용합니다.
        [HideInInspector] _ShadeThreshold("원본 명암 경계", Float) = 0.22 // Blender 원본 색상 단계의 실제 경계값을 전달합니다.
        [HideInInspector] _MidTone("중간 명암의 밝기 비율", Range(0, 1)) = 0.5 // 기존 그늘과 원래 색상의 중간 밝기를 사용해 세 색면을 구분합니다.
        [HideInInspector] _LightThreshold("밝은 면의 광원 경계", Range(-1, 1)) = 0.65 // 광원을 충분히 향하는 면에서 기존 원래 색상을 표시합니다.
        [HideInInspector] _BandSoftness("명암 경계의 부드러움", Range(0.001, 0.15)) = 0.015 // 단계별 명암 경계를 짧게 연결해 작은 물체의 깜빡임을 줄입니다.
        [HideInInspector] _OutlineColor("모델 윤곽선 색상", Color) = (0.28627452, 0.2, 0.15294118, 1) // M 친구와 같은 계열의 짙은 갈색을 윤곽선에만 사용합니다.
        [HideInInspector] _OutlineWidth("모델 윤곽선 두께(픽셀)", Range(0, 4)) = 1.6 // 물체의 원래 색면을 가리지 않는 얇은 화면 두께를 사용합니다.
    } // 내부 재질 속성의 끝입니다.
    SubShader // 현재 프로젝트의 URP 파이프라인에서 사용하는 불투명 표면입니다.
    { // 기본 색면과 그림자 및 깊이를 같은 메시에서 출력합니다.
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" } // URP의 일반 불투명 물체로 분류합니다.
        HLSLINCLUDE // 표면·윤곽선·그림자 패스에서 동일한 재질 상수 배치를 사용합니다.
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 기존 URP 좌표 변환과 재질 상수 버퍼 정의를 공유합니다.
        CBUFFER_START(UnityPerMaterial) // 패스마다 같은 재질 메모리 구조를 사용해 기존 SRP 배칭을 유지합니다.
            half4 _BaseColor; // 기존 건물·도로·차량·소품에 저장된 원래 색상입니다.
            half4 _OutlineColor; // 모델 바깥쪽 윤곽에만 출력할 선 색상입니다.
            half _ShadeStrength; // 각 기존 재질에 저장된 그늘의 밝기를 유지합니다.
            half _ShadeThreshold; // 기존 그늘에서 중간 명암으로 바뀌는 경계입니다.
            half _MidTone; // 그늘과 원래 색상 사이에서 중간 면의 밝기를 정합니다.
            half _LightThreshold; // 중간 명암에서 원래 색상으로 바뀌는 경계입니다.
            half _BandSoftness; // 명암 단계 사이의 짧은 전환 폭입니다.
            half _OutlineWidth; // 화면에서 사용할 윤곽선의 목표 픽셀 두께입니다.
            half _Cull; // 양면 얼굴 도형에는 별도 팽창 윤곽선을 겹치지 않게 합니다.
        CBUFFER_END // 모든 렌더링 패스가 공유하는 재질 상수 정의를 마칩니다.
        ENDHLSL // 공통 재질 선언을 각 패스에 전달합니다.
        Pass // 캐릭터와 건물의 실제 화면 색상을 그리는 패스입니다.
        { // 주광과 그림자에 따른 밝음·중간·그늘의 세 단계 명암을 계산합니다.
            Name "CartoonForward" // 모델의 기본 표시 패스 이름입니다.
            Tags { "LightMode"="UniversalForward" } // URP 전방 렌더링에 연결합니다.
            Cull [_Cull] // 재질에 저장된 원본 면 표시 설정으로 얇은 얼굴 표면도 그립니다.
            ZWrite On // 다른 물체와 실제 깊이 순서를 유지합니다.
            HLSLPROGRAM // URP 좌표 변환과 조명을 사용하는 셰이더 코드입니다.
            #pragma vertex CartoonVertex // 메시 정점의 위치와 법선을 화면으로 전달합니다.
            #pragma fragment CartoonFragment // 큰 색면의 단계별 명암을 출력합니다.
            #pragma target 3.5 // 현재 URP와 XR의 인스턴스 렌더링에 맞는 셰이더 기능을 사용합니다.
            #pragma multi_compile_instancing // 반복 배치한 배경과 VR 인스턴스 렌더링을 지원합니다.
            #pragma multi_compile_fog // 원경의 지형과 하늘이 자연스럽게 이어지는 씬 안개를 지원합니다.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE // URP 주광의 그림자를 받습니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Unity 6 URP의 기본 좌표 변환을 사용합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // 기존 씬의 주광과 그림자 정보를 읽습니다.
            struct Attributes // 실제 메시에서 읽을 정점 속성입니다.
            { // 월드 위치와 명암에 필요한 속성만 사용합니다.
                float4 positionOS : POSITION; // 모델 로컬 좌표의 정점 위치입니다.
                float3 normalOS : NORMAL; // 표면의 바깥 방향을 나타내는 법선입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 반복 배치와 VR의 인스턴스 정보를 받습니다.
            }; // 입력 정점 속성의 끝입니다.
            struct Varyings // 정점 단계에서 픽셀 단계로 전달할 값입니다.
            { // 각 눈에 맞는 위치와 월드 법선을 보관합니다.
                float4 positionCS : SV_POSITION; // 화면에 표시할 클립 좌표입니다.
                float3 positionWS : TEXCOORD0; // 주광 그림자를 조회할 월드 좌표입니다.
                half3 normalWS : TEXCOORD1; // 세 단계 명암을 결정할 월드 법선입니다.
                half fog : TEXCOORD2; // 같은 거리 기준으로 원경의 색면을 하늘에 이어 줍니다.
                UNITY_VERTEX_OUTPUT_STEREO // VR 양안 렌더링에 필요한 눈 정보를 전달합니다.
            }; // 출력 정점 속성의 끝입니다.
            Varyings CartoonVertex(Attributes input) // 실제 메시를 현재 카메라 좌표로 옮깁니다.
            { // 머티리얼이 기존 카메라와 XR 설정을 그대로 따르게 합니다.
                Varyings output = (Varyings)0; // 사용하지 않는 보간값을 초기화합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 현재 물체와 VR 인스턴스를 선택합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 현재 눈에 맞는 출력 정보를 설정합니다.
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz); // 모델의 실제 배치 위치를 반영합니다.
                output.positionCS = TransformWorldToHClip(output.positionWS); // 현재 눈의 화면 위치로 변환합니다.
                output.normalWS = TransformObjectToWorldNormal(input.normalOS); // 스케일과 회전이 반영된 표면 방향입니다.
                output.fog = ComputeFogFactor(output.positionCS.z); // 원경 표면을 위한 기존 URP 안개 계수입니다.
                return output; // 변환한 정점 정보를 픽셀 단계로 전달합니다.
            } // 정점 변환의 끝입니다.
            half4 CartoonFragment(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target // 얼굴 표면의 앞뒷면에 맞는 카툰 색상과 그늘을 출력합니다.
            { // 세밀한 하이라이트와 점토형 명암 그라데이션은 사용하지 않습니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 눈에 맞는 그림자 정보를 사용합니다.
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS)); // 기존 씬의 주광과 실제 물체 그림자를 읽습니다.
                half3 visibleNormalWS = IS_FRONT_VFACE(isFrontFace, input.normalWS, -input.normalWS); // 양면 얼굴의 보이는 쪽으로 법선을 향하게 하여 원본 명암을 유지합니다.
                half facing = dot(normalize(visibleNormalWS), mainLight.direction); // 현재 보이는 표면이 주광을 향하는 정도입니다.
                half edge = max(_BandSoftness, fwidth(facing)); // 멀리 있는 작은 면도 명암 경계가 한 픽셀보다 날카롭게 깜빡이지 않게 합니다.
                half sunlight = step(0.5h, mainLight.shadowAttenuation); // 실제 그림자 안의 면은 기존 재질의 그늘 밝기를 사용합니다.
                half middleBand = smoothstep(_ShadeThreshold - edge, _ShadeThreshold + edge, facing) * sunlight; // 빛을 일부 받는 면을 중간 명암으로 구분합니다.
                half brightThreshold = max(_LightThreshold, _ShadeThreshold + edge * 2.0h); // 밝은 면의 경계가 기존 그늘 경계보다 뒤에 오도록 합니다.
                half litBand = smoothstep(brightThreshold - edge, brightThreshold + edge, facing) * sunlight; // 광원을 충분히 향한 면만 원래 팔레트의 밝기로 표시합니다.
                half brightness = lerp(_ShadeStrength, lerp(_ShadeStrength, 1.0h, _MidTone), middleBand); // 기존 그늘과 원래 색상 사이에 중간 밝기를 추가합니다.
                brightness = lerp(brightness, 1.0h, litBand); // 밝은 면에서는 기존 머티리얼의 색상을 그대로 출력합니다.
                return half4(MixFog(_BaseColor.rgb * brightness, input.fog), _BaseColor.a); // 가까운 팔레트는 유지하고 원경만 하늘의 옅은 색으로 연결합니다.
            } // 카툰 색상 출력의 끝입니다.
            ENDHLSL // 기본 화면 패스의 끝입니다.
        } // 전방 렌더링 패스의 끝입니다.
        Pass // 원래 표면의 바깥쪽에 얇은 카툰 윤곽선을 그립니다.
        { // 기존 모델 메시의 뒷면만 조금 확장해 물체의 실루엣을 표시합니다.
            Name "CartoonOutline" // 모델 윤곽선 전용 패스 이름입니다.
            Tags { "LightMode"="CrosswalkModelOutline" } // 실제 표면 깊이가 준비된 뒤 전용 렌더러가 그리는 윤곽선 패스로 연결합니다.
            Cull Front // 확장한 메시의 뒷면만 그려 원래 표면의 색면을 유지합니다.
            ZWrite Off // 윤곽선이 기존 모델의 실제 깊이나 관찰 판정을 덮어쓰지 않게 합니다.
            ZTest LEqual // 다른 물체 뒤에 가려진 윤곽선은 화면에 나타내지 않습니다.
            HLSLPROGRAM // 기존 모델의 배치와 VR 양안을 따르는 윤곽선 코드입니다.
            #pragma vertex OutlineVertex // 실제 메시의 법선 방향으로 윤곽선 위치를 확장합니다.
            #pragma fragment OutlineFragment // 확장한 실루엣에만 윤곽선 색상을 출력합니다.
            #pragma target 3.5 // 기존 표면과 같은 URP 셰이더 기능을 사용합니다.
            #pragma multi_compile_instancing // 반복 배치한 모델과 VR 인스턴스 렌더링을 지원합니다.
            #pragma multi_compile_fog // 윤곽선도 원경의 기존 안개와 함께 흐려지게 합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // 작은 부품 뒤의 원래 표면 깊이를 읽어 번지는 내부 선을 제외합니다.
            struct OutlineAttributes // 윤곽선 계산에 필요한 기존 메시 속성입니다.
            { // 위치와 법선만 사용해 원래 메시와 재질 데이터를 유지합니다.
                float4 positionOS : POSITION; // 기존 모델의 로컬 정점 위치입니다.
                float3 normalOS : NORMAL; // 실루엣을 바깥쪽으로 확장할 표면 방향입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 물체와 VR 인스턴스를 선택할 정보입니다.
            }; // 윤곽선 입력 속성 정의를 마칩니다.
            struct OutlineVaryings // 윤곽선 픽셀에 필요한 위치와 안개 정보입니다.
            { // 각 눈에 맞는 위치와 같은 거리의 안개를 전달합니다.
                float4 positionCS : SV_POSITION; // 확장한 실루엣의 화면 위치입니다.
                half fog : TEXCOORD0; // 원래 배경과 윤곽선이 같은 안개를 사용하게 합니다.
                float3 positionWS : TEXCOORD1; // 확장한 실루엣의 깊이를 실제 모델 표면과 비교합니다.
                UNITY_VERTEX_OUTPUT_STEREO // VR에서 현재 눈에 맞는 윤곽선을 출력합니다.
            }; // 윤곽선 출력 속성 정의를 마칩니다.
            OutlineVaryings OutlineVertex(OutlineAttributes input) // 모델 스케일과 카메라 거리를 반영해 얇은 선을 만듭니다.
            { // 비균일하게 배치한 건물과 소품도 월드 좌표에서 같은 선 두께를 사용합니다.
                OutlineVaryings output = (OutlineVaryings)0; // 윤곽선 출력 구조를 초기화합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 현재 모델의 변환과 VR 인스턴스를 선택합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 현재 눈의 화면 위치를 준비합니다.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz); // 기존 모델의 실제 배치 위치를 구합니다.
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS)); // 모델 크기에 영향받지 않는 확장 방향을 구합니다.
                float eyeDepth = max(_ProjectionParams.y, -TransformWorldToView(positionWS).z); // 현재 눈에서 정점까지의 전방 거리를 구합니다.
                float cameraFactor = lerp(eyeDepth, 1.0, unity_OrthoParams.w); // 원근 카메라에서는 거리, 직교 카메라에서는 일정한 배율을 사용합니다.
                float widthWS = min(0.035, 2.0 * _OutlineWidth * cameraFactor / max(1.0, _ScaledScreenParams.y * abs(UNITY_MATRIX_P._m11))); // 화면 해상도에 맞는 얇은 선으로 확장하고 먼 물체의 과도한 팽창을 제한합니다.
                output.positionWS = positionWS + normalWS * widthWS; // 다른 모델과 비교할 확장 실루엣의 월드 위치를 보관합니다.
                output.positionCS = TransformWorldToHClip(output.positionWS); // 원래 표면 바깥쪽의 실루엣을 화면으로 옮깁니다.
                output.fog = ComputeFogFactor(output.positionCS.z); // 기존 배경과 같은 거리 기준으로 안개를 계산합니다.
                return output; // 확장한 위치와 안개를 윤곽선 픽셀에 전달합니다.
            } // 윤곽선 정점 계산을 마칩니다.
            half4 OutlineFragment(OutlineVaryings input) : SV_Target // 물체 바깥쪽의 얇은 선 색상만 출력합니다.
            { // 기존 모델의 색상은 앞서 그린 표면 패스가 계속 담당합니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 눈의 윤곽선 픽셀을 선택합니다.
                clip(min(_OutlineWidth - 0.001h, _Cull - 0.5h)); // 선 두께가 0인 재질과 얇은 양면 얼굴 도형에는 추가 선을 그리지 않습니다.
                float rawDepth = SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS)); // 현재 선 픽셀 뒤에 있는 원래 불투명 표면의 깊이를 읽습니다.
                float surfaceDepth = unity_OrthoParams.w > 0.5 ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams); // 원근·직교 카메라의 깊이를 같은 거리 단위로 변환합니다.
                float outlineDepth = -TransformWorldToView(input.positionWS).z; // 현재 눈에서 확장한 선까지의 실제 전방 거리를 구합니다.
                float surfaceTolerance = max(0.08, outlineDepth * 0.002); // 수 cm 두께의 이음새와 선 팽창이 같은 표면 위를 더럽히지 않도록 근접 범위를 정합니다.
                clip(surfaceDepth - outlineDepth - surfaceTolerance); // 바로 뒤에 같은 표면이 이어지는 내부 선을 제외하고 배경과 분리된 외곽만 남깁니다.
                return half4(MixFog(_OutlineColor.rgb, input.fog), _OutlineColor.a); // 원경에서는 기존 안개에 자연스럽게 섞이는 윤곽선을 출력합니다.
            } // 윤곽선 픽셀 계산을 마칩니다.
            ENDHLSL // 윤곽선 코드 정의를 마칩니다.
        } // 모델 윤곽선 패스를 마칩니다.
        Pass // 인도와 건물 및 캐릭터가 실제 그림자를 만들게 합니다.
        { // 기본 색상과 무관하게 같은 메시의 외곽으로 그림자를 생성합니다.
            Name "ShadowCaster" // URP의 그림자 생성 패스 이름입니다.
            Tags { "LightMode"="ShadowCaster" } // 기존 씬의 그림자 지도에 연결합니다.
            ZWrite On // 그림자 지도의 깊이를 기록합니다.
            ZTest LEqual // 가까운 그림자 표면을 유지합니다.
            ColorMask 0 // 그림자 패스에서는 색상을 기록하지 않습니다.
            Cull [_Cull] // 얼굴 표면의 양면 설정을 그림자에도 동일하게 적용합니다.
            HLSLPROGRAM // URP의 실제 그림자 좌표 변환입니다.
            #pragma vertex ShadowVertex // 광원 기준의 정점 위치를 계산합니다.
            #pragma fragment ShadowFragment // 깊이 기록만 수행합니다.
            #pragma target 3.5 // 현재 URP의 그림자 및 인스턴스 기능을 사용합니다.
            #pragma multi_compile_instancing // 반복된 거리 부품도 그림자를 생성합니다.
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW // 방향광과 점광원의 그림자 방향을 구분합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // URP의 기본 좌표 변환입니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl" // 기존 파이프라인의 그림자 바이어스를 사용합니다.
            float3 _LightDirection; // 방향광 그림자의 광원 방향입니다.
            float3 _LightPosition; // 점광원 그림자의 실제 광원 위치입니다.
            struct ShadowAttributes // 그림자를 만들 메시 속성입니다.
            { // 실제 위치와 표면 방향을 받습니다.
                float4 positionOS : POSITION; // 로컬 정점 위치입니다.
                float3 normalOS : NORMAL; // 로컬 표면 방향입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 반복 배치한 물체의 인스턴스 정보입니다.
            }; // 그림자 입력의 끝입니다.
            float4 ShadowVertex(ShadowAttributes input) : SV_POSITION // 광원 기준의 그림자 정점입니다.
            { // 그림자 번짐과 자기 그림자의 줄무늬를 줄이는 기본 바이어스를 반영합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 현재 물체의 변환을 사용합니다.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz); // 물체의 실제 배치 좌표입니다.
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS); // 실제 표면 방향입니다.
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW) // 점광원은 물체 위치에 따라 방향이 달라집니다.
                    float3 lightDirection = normalize(_LightPosition - positionWS); // 정점에서 광원으로 향하는 방향입니다.
                #else // 방향광은 모든 정점에서 같은 방향을 사용합니다.
                    float3 lightDirection = _LightDirection; // 기존 주광의 방향입니다.
                #endif // 광원 종류별 방향 결정의 끝입니다.
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection)); // URP 기본 바이어스로 그림자 위치를 보정합니다.
                #if UNITY_REVERSED_Z // 플랫폼의 깊이 방향에 맞춰 근평면을 처리합니다.
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w); // 뒤집힌 깊이 버퍼의 근평면을 유지합니다.
                #else // 일반 깊이 버퍼의 근평면 처리입니다.
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w); // 일반 깊이 버퍼의 유효 범위를 유지합니다.
                #endif // 플랫폼별 깊이 처리의 끝입니다.
                return positionCS; // 그림자 지도에 기록할 정점입니다.
            } // 그림자 정점 변환의 끝입니다.
            half4 ShadowFragment() : SV_Target // 색상 대신 깊이만 기록하는 픽셀 단계입니다.
            { // 패스의 ColorMask 설정에 따라 색상은 사용하지 않습니다.
                return 0; // 추가 표면 효과 없이 그림자 깊이만 만듭니다.
            } // 그림자 픽셀 단계의 끝입니다.
            ENDHLSL // 그림자 패스의 끝입니다.
        } // 그림자 생성 패스의 끝입니다.
    } // URP 카툰 표면 정의의 끝입니다.
} // 셰이더 정의의 끝입니다.
