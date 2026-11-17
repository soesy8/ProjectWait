Shader "Y/Crosswalk/StorybookSky" // 파스텔 거리와 연결되는 맑은 그림책 하늘입니다.
{ // 실사 대기 산란 대신 넓은 색면과 부드러운 햇무리를 사용합니다.
    Properties // 하늘의 팔레트만 재질 에셋에 저장합니다.
    { // 원본 Blender 배경과 동일한 선형 색상입니다.
        [HideInInspector] _Horizon("수평선 색상", Vector) = (0.72,0.86,0.88,1) // 지형 안개와 이어지는 밝은 하늘입니다.
        [HideInInspector] _Zenith("머리 위 색상", Vector) = (0.29,0.57,0.73,1) // 카툰 팔레트의 차분한 하늘색입니다.
        [HideInInspector] _SunDirection("햇빛 방향", Vector) = (-0.35,0.55,-0.76,0) // 기존 게임 광원과 같은 방향의 장식 태양입니다.
    } // 별도의 실행 중 하늘 제어는 추가하지 않습니다.
    SubShader // 현재 URP 카메라가 배경으로 그리는 하늘입니다.
    { // VR 눈별 카메라 투영을 그대로 사용합니다.
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" } // 기존 하늘 상자 자리에 연결합니다.
        Cull Off ZWrite Off // 카메라 안쪽에서 보이며 장면 깊이를 기록하지 않습니다.
        Pass // 한 번의 색상 출력으로 하늘을 완성합니다.
        { // 구름의 입체 형상은 Blender에서 만든 메시가 담당합니다.
            HLSLPROGRAM // 설치된 URP의 좌표와 스테레오 기능을 사용합니다.
            #pragma vertex Vert // 하늘 큐브의 방향을 전달합니다.
            #pragma fragment Frag // 같은 파스텔 팔레트의 하늘을 계산합니다.
            #pragma multi_compile_instancing // VR 인스턴스 렌더링을 지원합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 현재 Unity 6의 좌표 변환을 사용합니다.
            CBUFFER_START(UnityPerMaterial) // 하늘 재질의 상수를 묶습니다.
                float4 _Horizon, _Zenith, _SunDirection; // 원본 색상과 태양 방향입니다.
            CBUFFER_END // 하늘 재질 상수의 끝입니다.
            struct Input // 하늘 큐브에서 가져올 정점 정보입니다.
            { // 카메라 위치 대신 방향만 필요합니다.
                float4 vertex : POSITION; // 하늘 큐브의 실제 정점입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 VR 눈 정보를 받습니다.
            }; // 정점 입력의 끝입니다.
            struct Output // 각 눈의 픽셀 단계로 전달할 방향입니다.
            { // 실제 장면의 물체 위치와 분리된 배경입니다.
                float4 position : SV_POSITION; // 현재 눈의 하늘 투영입니다.
                float3 direction : TEXCOORD0; // 하늘의 위아래를 결정하는 방향입니다.
                UNITY_VERTEX_OUTPUT_STEREO // 양쪽 눈의 투영을 유지합니다.
            }; // 하늘 픽셀 입력의 끝입니다.
            Output Vert(Input input) // 현재 카메라의 하늘 큐브를 투영합니다.
            { // 기존 카메라의 위치나 회전은 변경하지 않습니다.
                Output output = (Output)0; // 출력 필드를 초기화합니다.
                UNITY_SETUP_INSTANCE_ID(input); // 실제 하늘 인스턴스를 선택합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 현재 VR 눈을 연결합니다.
                output.position = TransformObjectToHClip(input.vertex.xyz); // 원래 카메라 투영으로 하늘을 그립니다.
                output.direction = input.vertex.xyz; // 수평선 높이 계산에 사용할 방향입니다.
                return output; // 하늘 색상 계산으로 전달합니다.
            } // 하늘 정점 처리의 끝입니다.
            half4 Frag(Output input) : SV_Target // 넓은 색면과 작고 따뜻한 햇무리를 그립니다.
            { // 반짝임과 시간에 따른 변화는 넣지 않습니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 눈의 배경 정보를 유지합니다.
                float3 direction = normalize(input.direction); // 방향의 길이에 따라 색이 바뀌지 않게 합니다.
                float height = smoothstep(-0.04, 0.82, direction.y); // 수평선에서 천정까지 넓고 부드럽게 이어 줍니다.
                float3 color = lerp(_Horizon.rgb, _Zenith.rgb, height); // 정해진 카툰 팔레트 안에서 하늘을 만듭니다.
                float sun = saturate(dot(direction, normalize(_SunDirection.xyz))); // 기존 빛 방향에 작은 태양을 놓습니다.
                color = lerp(color, float3(1.0,0.91,0.68), pow(sun, 96.0) * 0.18); // 과한 번짐 없이 연한 햇무리를 더합니다.
                color = lerp(color, float3(1.0,0.97,0.82), smoothstep(0.9994,0.99985,sun)); // 선명하지만 작고 부드러운 태양 면입니다.
                return half4(color, 1); // 불투명 하늘 배경으로 출력합니다.
            } // 카툰 하늘 출력의 끝입니다.
            ENDHLSL // 하늘 셰이더를 마칩니다.
        } // 배경 패스의 끝입니다.
    } // URP 하늘의 끝입니다.
} // 하늘 재질 정의의 끝입니다.
