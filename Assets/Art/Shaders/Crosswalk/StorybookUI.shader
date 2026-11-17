Shader "Y/Crosswalk/StorybookUI" // 기존 월드 UI에 종이 카드와 인쇄 테두리의 완성된 표면을 제공합니다.
{ // 텍스트와 버튼 입력 및 RectTransform은 기존 구성을 유지합니다.
    Properties // 기존 Unity UI 마스크와 색상 전환을 받습니다.
    { // 카드와 버튼 및 작은 장식에 같은 디자인 규칙을 사용합니다.
        [PerRendererData] _MainTex("기존 이미지", 2D) = "white" {} // 기존 Image 컴포넌트의 표준 텍스처 슬롯입니다.
        [HideInInspector] _Style("카드 종류", Float) = 0 // 0은 안내 카드이고 1은 버튼이며 2는 작은 장식입니다.
        [HideInInspector] _Color("기본 색상", Color) = (1,1,1,1) // 기존 버튼의 색상 전환을 유지합니다.
        [HideInInspector] _StencilComp("마스크 비교", Float) = 8 // 기존 UI 마스크의 비교 설정입니다.
        [HideInInspector] _Stencil("마스크 번호", Float) = 0 // 현재 Canvas의 스텐실 번호입니다.
        [HideInInspector] _StencilOp("마스크 연산", Float) = 0 // 기존 마스크 연산입니다.
        [HideInInspector] _StencilWriteMask("마스크 기록", Float) = 255 // 스텐실 기록 범위입니다.
        [HideInInspector] _StencilReadMask("마스크 읽기", Float) = 255 // 스텐실 읽기 범위입니다.
        [HideInInspector] _ColorMask("색상 기록", Float) = 15 // RGBA 출력 채널입니다.
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("투명 영역 자르기", Float) = 0 // 기존 UI Mask를 지원합니다.
    } // 게임의 입력 필드는 추가하지 않습니다.
    SubShader // 기존 월드 UI와 같은 깊이 규칙으로 표시합니다.
    { // 원래 Canvas의 표시 순서를 유지합니다.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" } // 장면에 가려지지 않는 UI 표면입니다.
        Pass // 같은 Image 메시 안에서 카드 표면과 테두리를 그립니다.
        { // 새로운 런타임 UI 오브젝트를 만들지 않습니다.
            Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] } // 기존 마스크 동작을 유지합니다.
            Cull Off ZWrite Off ZTest Always // 원래 월드 UI의 양면 표시와 깊이 규칙입니다.
            Blend SrcAlpha OneMinusSrcAlpha // 부드러운 테두리와 종이 그림자를 합성합니다.
            ColorMask [_ColorMask] // 기존 Canvas의 색상 마스크입니다.
            HLSLPROGRAM // 현재 URP와 VR의 좌표 기능을 사용합니다.
            #pragma target 3.5 // 픽셀 미분으로 크기가 다른 카드의 테두리를 일정하게 유지합니다.
            #pragma vertex Vert // 원래 RectTransform 메시를 투영합니다.
            #pragma fragment Frag // 카드와 버튼의 완성 표면을 그립니다.
            #pragma multi_compile_instancing // VR 양쪽 눈의 표시를 지원합니다.
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT // 기존 RectMask2D를 지원합니다.
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP // 기존 UI Mask의 알파 제거를 지원합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 설치된 URP의 좌표 변환을 사용합니다.
            CBUFFER_START(UnityPerMaterial) // 카드 재질의 내부 상수입니다.
                float4 _Color, _ClipRect; float _Style; // 기존 색상과 마스크 및 표면 종류입니다.
            CBUFFER_END // UI 재질 상수의 끝입니다.
            struct Input // 실제 Canvas에서 가져오는 정점입니다.
            { // 원래 이미지의 색과 영역을 유지합니다.
                float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; // 위치와 버튼 전환 색상 및 이미지 좌표입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 VR 눈을 받습니다.
            }; // 정점 입력의 끝입니다.
            struct Output // 픽셀 단계로 전달할 UI 정보입니다.
            { // 기존 마스크에 필요한 로컬 위치를 함께 유지합니다.
                float4 position : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 local : TEXCOORD1; // 투영 위치와 실제 카드 좌표입니다.
                UNITY_VERTEX_OUTPUT_STEREO // VR 눈별 표시를 유지합니다.
            }; // 픽셀 입력의 끝입니다.
            Output Vert(Input input) // 기존 UI 메시를 그대로 투영합니다.
            { // 입력 판정과 배치는 변경하지 않습니다.
                Output output = (Output)0; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 현재 눈의 출력 정보를 연결합니다.
                output.position = TransformObjectToHClip(input.vertex.xyz); output.local = input.vertex.xy; // 원래 UI 위치와 마스크 좌표입니다.
                output.color = input.color * _Color; output.uv = input.uv; return output; // 기존 버튼과 CanvasGroup 색상을 전달합니다.
            } // 정점 변환의 끝입니다.
            float RoundRect(float2 p, float2 size, float radius) // 테두리가 끊기지 않는 둥근 카드 외곽입니다.
            { // 도형 이미지의 늘어진 모서리를 피합니다.
                float2 q = abs(p) - size + radius; return length(max(q,0)) + min(max(q.x,q.y),0) - radius; // 같은 곡률의 실제 카드 외곽 거리를 구합니다.
            } // 카드 외곽 계산의 끝입니다.
            half4 Frag(Output input) : SV_Target // 인쇄된 종이 카드와 눌리는 버튼 표면입니다.
            { // UI 문구와 실제 선택 상태는 기존 컴포넌트가 담당합니다.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 실제 눈에 맞는 픽셀 미분을 사용합니다.
                float2 derivative = max(fwidth(input.uv), 0.00001); // 현재 화면에서 한 픽셀이 차지하는 카드 좌표입니다.
                float aspect = clamp(derivative.y / derivative.x, 0.2, 12); // 크기가 다른 기존 카드의 가로세로 비율입니다.
                float2 p = (input.uv - 0.5) * float2(aspect,1); // 세로 길이를 기준으로 일정한 모서리를 만듭니다.
                float pixel = derivative.y; float aa = max(pixel,0.001); // VR에서도 테두리가 깜박이지 않는 부드러운 경계 폭입니다.
                float radius = _Style > 1.5 ? 0.30 : (_Style > 0.5 ? 0.14 : 0.09); // 장식과 버튼 및 카드의 디자인 곡률입니다.
                float2 size = float2(aspect * 0.5 - pixel * 4, 0.5 - pixel * 5); // 기존 영역 안에 종이 그림자 여유를 남깁니다.
                radius = min(radius,max(min(size.x,size.y),0.01)); // 작은 장식에서도 반지름이 실제 표면 폭을 넘지 않게 합니다.
                float distance = RoundRect(p, size, radius); // 카드 본체의 실제 외곽입니다.
                float shadow = 1 - smoothstep(-aa, aa * 2, RoundRect(p + float2(0,pixel * 3), size, radius)); // 원래 영역 안쪽의 작고 부드러운 종이 그림자입니다.
                float body = 1 - smoothstep(-aa,aa,distance); // 둥근 외곽의 부드러운 알파입니다.
                float3 ink = float3(0.19,0.32,0.32); // 세계의 먹색과 맞춘 따뜻한 인쇄 테두리입니다.
                float3 tint = input.color.rgb; // 기존 상황별 파스텔과 버튼 전환 색상입니다.
                float3 paper = lerp(tint, float3(1.0,0.985,0.94), _Style < 0.5 ? 0.64 : 0.12); // 큰 카드는 읽기 좋은 크림색 종이로 정돈합니다.
                paper *= 0.97 + 0.03 * smoothstep(0,0.95,input.uv.y); // 위아래에 얕은 종이 표면 깊이를 줍니다.
                float grain = frac(sin(dot(floor(input.local * 0.75),float2(12.9898,78.233))) * 43758.5453); // 화면 시간과 무관한 작은 종이 입자입니다.
                paper += (grain - 0.5) * 0.007; // 넓은 색면을 해치지 않는 미세한 재질 차이입니다.
                float edge = 1 - smoothstep(pixel * 1.1,pixel * 2.2,-distance); // 카드 바깥쪽의 얇은 잉크 테두리입니다.
                paper = lerp(paper, lerp(tint * 0.72,ink,0.22), edge * 0.55); // 원색의 검은 선 대신 팔레트에 맞는 테두리를 만듭니다.
                float inset = abs(RoundRect(p, size - pixel * 5, max(radius - pixel * 5,0.01))); // 인쇄된 두 번째 가는 테두리입니다.
                float innerBorder = 1 - smoothstep(pixel * 0.35,pixel * 0.9,inset); // HLSL 예약어와 충돌하지 않는 내부 장식선의 밝기입니다.
                paper = lerp(paper, float3(1,0.995,0.97), innerBorder * 0.75); // 계산한 내부 테두리로 종이의 밝은 단면을 표현합니다.
                float bottom = (1 - smoothstep(0.04,0.18,input.uv.y)) * (1 - smoothstep(0.04,0.10,input.uv.y)); // 카드 아래쪽의 얕은 테마 띠입니다.
                paper = lerp(paper,lerp(tint,float3(0.50,0.76,0.68),0.22),bottom * (_Style < 0.5 ? 0.18 : 0.05)); // 본문 뒤를 복잡하게 만들지 않는 색상 마감입니다.
                float buttonSurface = step(0.5, _Style) * (1 - step(1.5, _Style)); // 본문 카드와 장식을 제외한 실제 버튼 표면만 선택합니다.
                float sweepPosition = frac(_Time.y * 0.16) * 1.6 - 0.3; // 간격을 두고 버튼을 천천히 가로지르는 반사광 위치입니다.
                float sheen = 1 - smoothstep(0.015, 0.09, abs(input.uv.x + input.uv.y * 0.18 - sweepPosition)); // 종이 코팅 위의 좁고 부드러운 대각선 반사광입니다.
                paper = lerp(paper, max(paper, float3(1.0, 0.98, 0.9)), sheen * buttonSurface * body * 0.18); // 기존 파스텔 팔레트를 유지하며 버튼 표면에만 은은한 빛의 움직임을 더합니다.
                float3 color = lerp(ink * 0.62, paper, body); // 종이 본체 아래에 작은 그림자만 남깁니다.
                float alpha = max(body,shadow * 0.22) * input.color.a; // 기존 CanvasGroup의 표시 상태를 유지합니다.
                #ifdef UNITY_UI_CLIP_RECT // 기존 사각형 마스크가 있는 경우입니다.
                    float2 inside = step(_ClipRect.xy,input.local) * step(input.local,_ClipRect.zw); alpha *= inside.x * inside.y; // 기존 마스크 밖의 UI는 표시하지 않습니다.
                #endif // 마스크 처리의 끝입니다.
                #ifdef UNITY_UI_ALPHACLIP // 기존 스텐실 마스크가 알파 제거를 요청한 경우입니다.
                    clip(alpha - 0.001); // 투명한 모서리만 제거합니다.
                #endif // 알파 제거의 끝입니다.
                return half4(color,alpha); // 기존 UI 위계와 입력을 유지한 완성 표면입니다.
            } // 카드 픽셀 처리의 끝입니다.
            ENDHLSL // UI 셰이더의 끝입니다.
        } // UI 표면 패스의 끝입니다.
    } // URP UI 표면의 끝입니다.
} // 그림책 UI 재질의 끝입니다.
