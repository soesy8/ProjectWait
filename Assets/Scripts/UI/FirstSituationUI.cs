using System.Collections; // 실제 연출의 대사 시간을 기다리는 기존 코루틴을 사용합니다.
using System.Collections.Generic; // 말풍선 한 번의 배치 동안 UI 및 관찰 대상의 화면 영역을 재사용합니다.
using TMPro; // 씬에 미리 제작된 선명한 한국어 UI 텍스트를 사용합니다.
using UnityEngine; // 월드 공간 UI의 배치와 포함 글꼴 리소스를 관리합니다.
using UnityEngine.UI; // 대화 카드 없이 장면 아래 자막 띠를 그리는 기존 Unity UI를 사용합니다.
using UnityEngine.Events; // 친구에게 말을 걸었을 때 현재 상황의 선택지를 여는 콜백을 사용합니다.

namespace Y.Crosswalk // 첫 번째 상황의 표시를 진행 로직과 분리합니다.
{ // UI 오브젝트는 모두 씬에 저장하며 실행 중에는 표시 상태만 바꿉니다.
    public sealed class FirstSituationUI : MonoBehaviour // 안내·생각·선택·결과·재시도의 실제 패널을 연결합니다.
    { // 모든 주요 UI를 Inspector에서 직접 선택할 수 있게 합니다.
        [SerializeField, InspectorName("월드 공간 UI 기준"), Tooltip("미리 제작한 World Space Canvas의 RectTransform입니다.")] private Transform m_Anchor; // 사용자가 고개를 돌려도 패널을 매 프레임 따라 붙이지 않습니다.
        [SerializeField, InspectorName("보행 안내"), Tooltip("왼쪽 스틱 보행과 친구 관찰 안내 패널입니다.")] private FirstSituationPanel m_Guidance; // 첫 보행 중 간단한 조작을 안내합니다.
        [SerializeField, InspectorName("클로즈업 화면"), Tooltip("별도 카메라 RenderTexture를 표시하는 월드 공간 화면입니다.")] private FirstSituationPanel m_Closeup; // 실제 XR 카메라를 바꾸지 않고 가까운 장면을 보여 줍니다.
        [SerializeField, InspectorName("관찰 안내"), Tooltip("관찰을 마쳤어요 버튼이 포함된 패널입니다.")] private FirstSituationPanel m_Observation; // 시간 제한 없이 다음 단계의 시점을 플레이어가 정하게 합니다.
        [SerializeField, InspectorName("생각 말풍선"), Tooltip("관찰 대상 선택 시 표시할 둥근 말풍선입니다.")] private FirstSituationPanel m_Thought; // 네 대상의 생각을 공통 패널에서 표시합니다.
        [SerializeField, InspectorName("생각 문구"), Tooltip("생각 말풍선 안의 실제 TextMeshPro 텍스트입니다.")] private TMP_Text m_ThoughtText; // 선택된 대상의 Inspector 문구를 표시합니다.
        [SerializeField, Range(1.5f, 3.5f), InspectorName("말풍선 읽기 거리(m)"), Tooltip("생각과 감사 말풍선을 읽을 수 있는 카메라 앞 거리입니다. 선택 지점의 화면 위치를 유지하며 카메라는 움직이지 않습니다.")] private float m_BubbleDistance = 2.15f; // 멀리 있는 친구나 낮은 보도를 선택해도 글자를 편안한 거리에서 읽게 합니다.
        [SerializeField, Range(0.01f, 0.08f), InspectorName("선택 지점과 말풍선 간격"), Tooltip("선택한 화면 지점과 말풍선 가장자리 사이에 둘 화면 비율 간격입니다.")] private float m_BubbleGap = 0.025f; // 말풍선이 클릭한 대상의 표면을 덮지 않도록 옆으로 띄웁니다.
        [SerializeField, Range(0.01f, 0.1f), InspectorName("말풍선 화면 여백"), Tooltip("말풍선이 화면 가장자리에 잘리지 않도록 남길 화면 비율 여백입니다.")] private float m_BubbleScreenPadding = 0.035f; // 글자와 둥근 배경이 함께 보이는 위치로 제한합니다.
        [SerializeField, InspectorName("행동 선택"), Tooltip("두 행동과 다시 살펴보기 버튼이 포함된 패널입니다.")] private FirstSituationPanel m_Choice; // 선택을 확정하기 전에는 계속 표시합니다.
        [SerializeField, InspectorName("위험 행동 결과"), Tooltip("사고 원인을 설명하는 주황색 말풍선입니다.")] private FirstSituationPanel m_Unsafe; // 실제 충돌 연출이 끝난 뒤 원인을 표시합니다.
        [SerializeField, InspectorName("안전 행동 결과"), Tooltip("위험을 피한 이유를 설명하는 민트색 패널입니다.")] private FirstSituationPanel m_Safe; // 첫 번째 상황 완료 뒤에 표시합니다.
        [SerializeField, InspectorName("감사 대사"), Tooltip("안전하게 피한 실제 친구 옆에 표시할 감사 말풍선입니다.")] private FirstSituationPanel m_Thanks; // 친구의 미소와 손짓을 가리지 않는 위치에서 감사 문구를 읽게 합니다.
        [SerializeField, InspectorName("재시도 안내"), Tooltip("자동 복원 중임을 알리고 빠른 재시도 버튼을 제공하는 패널입니다.")] private FirstSituationPanel m_Retry; // 관찰 복원 전 짧게 안내합니다.
        [SerializeField, InspectorName("한국어 UI 글꼴"), Tooltip("M 원본 ElevatorReadable에 포함된 Noto Sans CJK KR Medium 글꼴입니다. 세 상황의 본문과 버튼에 같은 원본을 사용합니다.")] private TMP_FontAsset m_Font; // 한국어 원본 글꼴이 연결된 프로젝트 에셋을 직접 사용하여 기본 영문 글꼴로 돌아가지 않게 합니다.
        [SerializeField, InspectorName("전체 UI 텍스트"), Tooltip("씬에 저장된 안내·생각·버튼·결과 텍스트입니다.")] private TMP_Text[] m_Texts; // UI 생성 없이 같은 글꼴 리소스를 공유합니다.
        [SerializeField, InspectorName("자동 보행 도입 제목"), Tooltip("Hierarchy에서 편집하는 1인칭 보행 도입 제목입니다.")] private GameObject m_IntroductionHeading; // 도입과 전화 클로즈업의 실제 문구 오브젝트를 구분합니다.
        [SerializeField, InspectorName("자동 보행 도입 설명"), Tooltip("Hierarchy에서 편집하는 보행 도입의 짧은 설명입니다.")] private GameObject m_IntroductionCaption; // 조작 안내 대신 자동 도입의 상황을 설명합니다.
        [SerializeField, InspectorName("전화 클로즈업 제목"), Tooltip("기존 클로즈업 화면의 실제 제목 오브젝트입니다.")] private GameObject m_CloseupHeading; // 도입 종료 뒤 전화 사용에 관심을 모읍니다.
        [SerializeField, InspectorName("전화 클로즈업 설명"), Tooltip("기존 클로즈업 화면의 실제 설명 오브젝트입니다.")] private GameObject m_CloseupCaption; // 기존의 편집 가능한 문구를 보존합니다.
        [SerializeField, InspectorName("헤드셋 전체 시야 전환막"), Tooltip("실제 카메라 자식으로 미리 배치한 안쪽 면 전환 구체입니다. 추적이나 시야각을 변경하지 않습니다.")] private Renderer m_ImmersiveFade; // 고개를 돌려도 위치 배치 전환이 보이지 않도록 시야 전체를 부드럽게 가립니다.
        [SerializeField, Min(0.05f), InspectorName("위치 복원 전환 시간"), Tooltip("실제 위치 배치·재시도 복원에만 사용하는 짧은 전환 시간입니다.")] private float m_FadeDuration = 0.12f; // 도입과 클로즈업 사이에서는 전체 화면을 가리지 않습니다.
        [SerializeField, ColorUsage(false), InspectorName("위치 복원 전환 색상"), Tooltip("밝은 화면 점멸 대신 복원 순간만 짧게 가리는 어두운 색입니다.")] private Color m_FadeColor = new Color(0.04f, 0.05f, 0.07f, 1f); // 흰색 전환막이 반복적으로 눈앞을 가리지 않게 합니다.
        [SerializeField, Range(0f, 0.4f), InspectorName("상황 강조 테두리 강도"), Tooltip("관찰·위험·성공 순간에 화면 가장자리만 물들이는 짧은 효과의 최대 투명도입니다.")] private float m_CueStrength = 0.24f; // 중앙의 친구와 도로를 가리지 않는 주변부 강조를 사용합니다.
        [SerializeField, InspectorName("UI 연출 음원"), Tooltip("씬에 미리 배치한 2D AudioSource로 관찰 시작과 안전 완료 차임을 재생합니다.")] private AudioSource m_PresentationAudio; // 발걸음과 경적 및 충돌 음원을 변경하지 않는 UI 전용 음원입니다.
        [SerializeField, InspectorName("관찰 시작 차임"), Tooltip("자유 관찰이 시작되는 순간의 짧고 맑은 두 음 효과입니다.")] private AudioClip m_ObservationClip; // 여러 상황에서 같은 관찰 진입을 일관된 소리로 알립니다.
        [SerializeField, InspectorName("안전 완료 차임"), Tooltip("실제 안전 완료 패널이 열릴 때 재생하는 따뜻한 상승 차임입니다.")] private AudioClip m_SuccessClip; // 선택 직후가 아닌 실제 완료 시점에 성취감을 전달합니다.
        private Color m_CueColor; // 현재 상황에서 요청한 주변부 강조 색상입니다.
        private float m_CueRemaining, m_CueDuration; // 짧은 화면 강조를 전역 시간 배율과 독립적으로 끝냅니다.
        private static readonly int k_CueColor = Shader.PropertyToID("_CueColor"); // 기존 전환막에 주변부 강조 색과 알파를 전달합니다.
        public bool IsTransitioning => m_FadeVisible || m_FadeWeight > 0.01f; // 암전 중에는 마우스 회전과 3D 클릭을 보류합니다.
        public float FadeCoverDuration => Mathf.Max(0.05f, m_ActiveFadeDuration) + 0.03f; // 배치 변경은 현재 상황이 요청한 실제 페이드가 끝난 뒤에만 수행합니다.
        private float m_ActiveFadeDuration; // 첫 번째 상황과 다음 장소 이동의 전환 시간을 별도로 적용합니다.
        private MaterialPropertyBlock m_FadeProperties; // 전환막 원본 머티리얼을 변경하지 않고 현재 알파만 전달합니다.
        private float m_FadeWeight; // 전환막의 실제 표시 진행도를 보관합니다.
        private bool m_FadeVisible; // 배치 전환을 가릴지 표시할지 요청된 상태입니다.
        private static readonly int k_FadeColor = Shader.PropertyToID("_FadeColor"); // 매 프레임 전환막 색상 이름을 다시 조회하지 않습니다.
        private FirstSituationPanel[] m_Panels; // 단계 전환 시 숨길 기존 패널 목록입니다.
        [SerializeField, InspectorName("기존 PC·VR 입력 모드"), Tooltip("PC에서만 현재 시선에 패널을 배치하고 VR에서는 기존 수평 읽기 위치를 유지합니다.")] private CrosswalkDesktopInput m_InputSource; // 헤드셋 회전값은 수정하지 않습니다.
        private Camera m_Viewer; // 기존 월드 Canvas에 연결된 XR Origin 카메라로 실제 선택 위치를 화면에 투영합니다.
        private FirstSituationPanel m_ContextThought; // 현재 관찰이 지정한 실제 씬 말풍선입니다.
        private TMP_Text m_ContextThoughtText; // 현재 말풍선의 실제 TMP 문구를 연결합니다.
        [SerializeField, InspectorName("중요 관찰 대상의 영역"), Tooltip("친구·휴대전화·신호등의 기존 Collider입니다. 말풍선이 얼굴과 중요한 단서를 덮지 않도록 사용합니다.")] private Collider[] m_ProtectedObjects; // 현재 시야에 보이는 기존 대상만 배치에 반영합니다.
        [SerializeField, InspectorName("가림 방지 한국어 글자 재질"), Tooltip("기존 Noto 글꼴의 Overlay 재질입니다. 월드 물체 뒤에 글자가 가려지지 않게 합니다.")] private Material m_TextOverlay; // 원래 글꼴과 아틀라스는 유지합니다.
        private FirstSituationPanel m_PlacedBubble; // 현재 위치를 부드럽게 조정할 실제 말풍선입니다.
        private Vector3 m_BubbleSource; // 클릭한 지점이나 친구 머리의 마지막 표시 기준입니다.
        private Bounds m_BubbleProtectedBounds; // 현재 선택 대상의 실제 화면 영역을 보호합니다.
        private bool m_BubbleHasBounds; // 단서 크기가 전달된 경우에만 전체 대상을 보호합니다. // 실제 관찰 안내 패널을 화면에 투영할 네 모서리입니다.
        private readonly List<Rect> m_OccupiedUIAreas = new List<Rect>(); // 후보마다 투영을 반복하지 않고 화면에 보이는 패널 영역을 한 번만 수집합니다.
        private readonly List<Rect> m_ProtectedWorldAreas = new List<Rect>(); // 친구 얼굴·휴대전화·신호등 등 보호할 세계 오브젝트 영역을 한 번만 계산합니다.
        [SerializeField, InspectorName("이야기 화자"), Tooltip("M의 이름 띠 위에 표시하는 현재 화자의 텍스트입니다.")] private TMP_Text m_StorySpeaker; // 친구와 플레이어의 말을 구분합니다.
        [SerializeField, InspectorName("이야기 대사"), Tooltip("M 원본의 읽기 전용 설명 본문입니다. 조사 중 세계 선택을 막지 않고 짧은 문장 전체를 표시합니다.")] private TMP_Text m_StoryText; // 한 번에 한 대사를 보여 줍니다.
        [SerializeField, InspectorName("이야기 다음 버튼"), Tooltip("이전에 추가했던 대사 넘김 버튼입니다. 현재 흐름에서는 숨기며 진행 입력으로 사용하지 않습니다.")] private GameObject m_StoryNextButton; // 대화를 읽는 동안 선택용 커서를 표시합니다.
        [SerializeField, InspectorName("친구 얼굴 배지"), Tooltip("M의 친구 초상 이미지를 사용하는 화자 배지입니다.")] private GameObject m_StoryPortrait; // 친구 대사에서 얼굴 배지를 표시합니다.
        [SerializeField, InspectorName("행동 안내 문구"), Tooltip("다음으로 직접 눌러야 할 물체와 행동을 알려 주는 문구입니다.")] private TMP_Text m_TaskText; // 현재 장면에 맞는 클릭 지시를 표시합니다.
        private UnityAction m_TaskCompleted; // 실제 물체를 선택했을 때에만 다음 상황 행동을 한 번 실행합니다.
        private FirstSituationObservation m_TaskTarget; // 이야기 진행을 기다리고 있는 실제 물체입니다.
        public bool HasTask => m_TaskTarget != null && m_TaskCompleted != null; // 클릭할 물체를 기다리는 동안 중앙 조준을 허용합니다.
        public bool IsTaskTarget(FirstSituationObservation target) => HasTask && m_TaskTarget == target; // 현재 안내 물체에만 행동 강조를 적용합니다.
        [SerializeField, InspectorName("자막 띠 재질"), Tooltip("M 원본 UIOverlay 재질입니다. 읽기 그림·응답 버튼·연출 띠에 동일한 가림 방지 표시를 사용합니다.")] private Material m_SubtitleBackground; // 친구와 도로 앞에 대화 카드를 띄우지 않고 화면 아래의 가독성을 확보합니다.
        [SerializeField, InspectorName("M 선택 버튼 그림"), Tooltip("원본 ButtonTeal 스프라이트입니다. 안전·위험 응답에 같은 모양을 사용합니다.")] private Sprite m_PrimaryButton; // M의 판단 선택지처럼 정답을 색으로 구별하지 않습니다.
        [SerializeField, InspectorName("M 돌아보기 버튼 그림"), Tooltip("원본 ButtonCream 스프라이트입니다. 다시 살펴보기 같은 보조 행동에 사용합니다.")] private Sprite m_SecondaryButton; // 원본과 같은 보조 버튼의 모양을 사용합니다.
        [SerializeField, InspectorName("M 대사 꼬리"), Tooltip("기존 대사 프레임 아래의 SpeechTail 그림입니다.")] private RectTransform m_SpeechTail; // M 원본의 72×42 장식 위치를 그대로 적용합니다.
        private RectTransform m_SubtitleRoot, m_SubtitleBar, m_SubtitleTop, m_ReadingColumn, m_ReadingPaper; // 연출 자막과 조사 중 읽기 영역을 M처럼 별도로 배치합니다.
        private TMP_Text m_CaptionText; // 연출 자막은 조사 카드의 본문과 분리하여 자동 장면 진행 중에만 표시합니다.
        private Material m_RuntimeTextOverlay; // M 글꼴의 실제 아틀라스를 복사하고 원본의 4201 표시 순서를 적용합니다.
        private FirstSituationPanel m_ActionPanel; // 세 상황에서 현재 표시하는 실제 행동 버튼 패널을 기억합니다.
        private bool m_ReadingPlaced, m_ReadingHadActions; // VR의 최초 배치와 선택지 유무 변경을 구분하고 PC는 화면 기준 배치를 사용합니다.
        private Vector3 m_ObservationLocalPosition; // VR과 숨김 상태에서 되돌릴 안내창의 씬 배치입니다.
        private Quaternion m_ObservationLocalRotation; // PC용 배치가 끝나면 기존 월드 UI의 방향을 복원합니다.
        private bool m_ObservationInViewport; // PC에서 조작 안내를 화면 가장자리로 옮긴 경우만 복원합니다.

        private void Awake() // 실제 씬 UI의 연결과 한글 표시를 준비합니다.
        { // 패널·텍스트·버튼 오브젝트를 실행 중 생성하지 않습니다.
            m_Panels = new[] { m_Guidance, m_Closeup, m_Observation, m_Thought, m_Choice, m_Unsafe, m_Safe, m_Thanks, m_Retry }; // 전환막은 다른 패널과 독립적으로 페이드합니다.
            m_Viewer = m_Anchor.GetComponent<Canvas>().worldCamera; // 월드 패널과 같은 카메라를 화면 배치 기준으로 사용합니다.
            m_ObservationLocalPosition = m_Observation.transform.localPosition; // 관찰 안내의 씬에 저장된 위치를 보관합니다.
            m_ObservationLocalRotation = m_Observation.transform.localRotation; // PC 모드가 끝나면 되돌릴 안내 방향을 보관합니다.
            m_FadeProperties = new MaterialPropertyBlock(); m_ImmersiveFade.enabled = false; // 씬에 저장한 전체 시야 전환막은 필요할 때만 표시합니다.
            m_ActiveFadeDuration = m_FadeDuration; // 기존 첫 번째 상황의 짧은 전환 시간을 기본값으로 유지합니다.
            m_RuntimeTextOverlay = new Material(m_Font.material) { name = "횡단보도 한국어 자막", shader = m_TextOverlay != null ? m_TextOverlay.shader : m_Font.material.shader, renderQueue = 4201 }; // M 원본처럼 글자를 배경 재질의 4100보다 나중인 4201에 그려 프레임이 글자를 덮지 않게 합니다.
            foreach (TMP_Text label in m_Texts) // 에디터와 실행 중에 동일한 Noto 한국어 글꼴 및 머티리얼 연결을 유지합니다.
            { // 기존 UI 오브젝트와 편집한 문구·크기는 그대로 사용합니다.
                label.font = m_Font; // 씬에 저장된 동적 SDF 에셋을 사용하여 한국어 글리프를 같은 원본에서 가져옵니다.
                label.fontSharedMaterial = m_RuntimeTextOverlay; // 현재 동적 한국어 글꼴에서 복사한 재질을 사용하여 글자와 아틀라스의 연결을 일치시킵니다.
            } // 원본 글꼴 에셋과 그 안의 글리프는 수정하지 않습니다.
            BuildSubtitles(); // 연출 자막과 조사 설명을 M의 서로 다른 표시 위치에 연결합니다.
            foreach (Button button in m_Anchor.GetComponentsInChildren<Button>(true)) StyleButton(button, button.targetGraphic is Image image && image.sprite == m_PrimaryButton); // 시작 화면을 포함한 기존 버튼의 글자·색상 전환을 M 원본 기준으로 적용합니다.
        } // 포함된 글꼴과 이미지 자산은 원본 상태로 보존합니다.

        private void BuildSubtitles() // M의 연출 자막과 조사 중 읽기 영역을 기존 UI에 연결합니다.
        { // 화면 클릭이나 대사를 넘기는 입력 영역은 만들지 않습니다.
            m_ReadingColumn = (RectTransform)new GameObject("M 방식 조사 읽기 영역", typeof(RectTransform)).transform; m_ReadingColumn.SetParent(m_Anchor, false); // 설명과 실제 선택지를 같은 기준점에서 배치합니다.
            Image frame = m_Guidance.GetComponent<Image>(); if (frame != null) { frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 4f; frame.material = m_SubtitleBackground; frame.color = Color.white; } // 설명 프레임에 M 원본과 같은 그림 재질과 슬라이스 비율을 적용합니다.
            m_Guidance.transform.SetParent(m_ReadingColumn, false); m_StoryNextButton.SetActive(false); // 기존 프레임은 읽기 전용으로 사용하고 제가 넣었던 다음 버튼은 표시하지 않습니다.
            GameObject paper = new GameObject("ReadingPaper", typeof(RectTransform), typeof(Image)) { layer = 5 }; m_ReadingPaper = (RectTransform)paper.transform; m_ReadingPaper.SetParent(m_Guidance.transform, false); m_ReadingPaper.SetSiblingIndex(0); Image fill = paper.GetComponent<Image>(); fill.material = m_SubtitleBackground; fill.color = new Color(1f, 0.975f, 0.89f, 1f); fill.raycastTarget = false; // M의 본문 안쪽 읽기 종이 색과 여백을 그대로 사용합니다.
            GameObject root = new GameObject("장면 연출 자막", typeof(RectTransform), typeof(Canvas)) { layer = 5 }; // M의 CinematicOverlay와 같은 연출 전용 Canvas입니다.
            m_SubtitleRoot = (RectTransform)root.transform; m_SubtitleRoot.SetParent(m_Viewer.transform, false); // 카메라의 추적을 유지하면서 자막만 같은 시야를 따라갑니다.
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = m_Viewer; canvas.overrideSorting = true; canvas.sortingOrder = 100; canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent; // M과 같은 연출 정렬 및 TMP 정점 채널을 사용합니다.
            m_SubtitleBar = CreateCaptionBar("아래 연출 띠", new Vector2(0.5f, 0f)); m_SubtitleTop = CreateCaptionBar("위 연출 띠", new Vector2(0.5f, 1f)); // M 원본의 위아래 82 포인트 띠를 함께 표시합니다.
            GameObject caption = new GameObject("연출 대사", typeof(RectTransform), typeof(TextMeshProUGUI)) { layer = 5 }; caption.transform.SetParent(m_SubtitleBar, false); // 조사 본문을 옮기지 않고 연출 자막에만 별도의 글자를 둡니다.
            m_CaptionText = caption.GetComponent<TMP_Text>(); m_CaptionText.font = m_Font; m_CaptionText.fontSharedMaterial = m_RuntimeTextOverlay; ApplyTypography(m_CaptionText, 28f, false); m_CaptionText.color = Color.white; m_CaptionText.alignment = TextAlignmentOptions.Center; // M의 CaptionSize 28과 흰색 중앙 자막을 그대로 적용합니다.
            m_CaptionText.rectTransform.anchoredPosition = Vector2.zero; PositionSubtitles(); root.SetActive(false); // 실제 연출을 시작하기 전에는 자막 띠를 숨깁니다.
        } // 일반 조사 설명은 기존 M 그림 프레임에서 읽습니다.

        private RectTransform CreateCaptionBar(string label, Vector2 anchor) // M 연출의 화면 위아래 띠를 같은 크기로 만듭니다.
        { // 원본 UIOverlay 재질을 써서 실제 월드 물체 뒤로 가려지지 않게 합니다.
            GameObject bar = new GameObject(label, typeof(RectTransform), typeof(Image)) { layer = 5 }; RectTransform rect = (RectTransform)bar.transform; rect.SetParent(m_SubtitleRoot, false); // 연출 Canvas 안에서만 배치되는 띠입니다.
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = Vector2.zero; Image image = bar.GetComponent<Image>(); image.material = m_SubtitleBackground; image.color = Color.black; image.raycastTarget = false; // 실제 물체나 선택 버튼의 클릭은 막지 않습니다.
            return rect; // 화면 비율에 맞는 폭은 PositionSubtitles에서 적용합니다.
        } // 위쪽과 아래쪽 모두 같은 원본 재질을 사용합니다.

        private static void ApplyTypography(TMP_Text text, float size, bool bold) // M의 ElevatorTypography에 저장된 글자 규칙을 그대로 적용합니다.
        { // 글자 크기를 줄여 문장을 억지로 끼워 넣지 않습니다.
            text.fontSize = size; text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; text.fontWeight = bold ? FontWeight.Bold : FontWeight.Regular; text.enableAutoSizing = false; text.extraPadding = true; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.maxVisibleCharacters = int.MaxValue; // 원본처럼 짧은 문장 전체를 즉시 보여 주고 읽기를 별도 클릭으로 소비하지 않습니다.
        } // 문장 길이에 따른 높이는 읽기 영역이 확보합니다.

        public void StyleButton(Button button, bool primary) // 기존 버튼의 동작은 유지하고 M 원본의 그림·글자·색상 전환을 적용합니다.
        { // 버튼 확대와 눌림 이동 대신 ColorTint만 사용합니다.
            Image image = button.targetGraphic as Image; if (image != null) { image.sprite = primary ? m_PrimaryButton : m_SecondaryButton; image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple; image.pixelsPerUnitMultiplier = 6f; image.material = m_SubtitleBackground; image.color = Color.white; image.raycastTarget = true; } // M 원본의 버튼 아트와 슬라이스 비율을 사용합니다.
            button.transition = Selectable.Transition.ColorTint; ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 1f, 0.94f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(0.78f, 0.84f, 0.77f); colors.disabledColor = new Color(0.58f, 0.60f, 0.56f, 0.8f); colors.fadeDuration = 0.1f; button.colors = colors; // M의 SkinButton과 같은 색상과 전환 시간을 적용합니다.
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true); if (text != null) { text.font = m_Font; text.fontSharedMaterial = m_RuntimeTextOverlay != null ? m_RuntimeTextOverlay : m_TextOverlay; ApplyTypography(text, 34f, true); text.color = primary ? new Color32(255, 249, 231, 255) : new Color32(58, 36, 24, 255); } // 원본 ChoiceSize 34와 종이색·잉크색 글자를 적용합니다.
            FirstSituationButtonFeedback feedback = button.GetComponent<FirstSituationButtonFeedback>(); if (feedback != null) feedback.enabled = false; button.transform.localScale = Vector3.one; // M에서 사용하지 않는 기존 확대·튀어 오르는 반응을 끕니다.
        } // Button.onClick에 연결된 실제 행동은 변경하지 않습니다.

        private void PositionSubtitles() // M의 720 기준 연출 자막을 현재 시야각에 맞춰 배치합니다.
        { // 실제 플레이어 카메라의 위치와 방향은 건드리지 않습니다.
            float distance = Mathf.Max(0.45f, m_Viewer.nearClipPlane + 0.06f); float height = 2f * distance * Mathf.Tan(m_Viewer.fieldOfView * 0.5f * Mathf.Deg2Rad); float width = 720f * m_Viewer.aspect; // 원본 CinematicOverlay와 같은 화면 크기 계산을 사용합니다.
            m_SubtitleRoot.localPosition = new Vector3(0f, 0f, distance); m_SubtitleRoot.localRotation = Quaternion.identity; m_SubtitleRoot.localScale = Vector3.one * (height / 720f); m_SubtitleRoot.sizeDelta = new Vector2(width, 720f); // 시선 입력과 독립적으로 연출 띠를 화면 가장자리에 둡니다.
            m_SubtitleBar.sizeDelta = m_SubtitleTop.sizeDelta = new Vector2(width + 20f, 82f); if (m_CaptionText != null) m_CaptionText.rectTransform.sizeDelta = new Vector2(Mathf.Min(1100f, width - 48f), 82f); // 원본과 같은 띠 높이와 자막 여백을 적용합니다.
        } // 연출이 끝나면 HideAll에서 위아래 띠를 함께 숨깁니다.

        public void ShowCinematicCaption(string speaker, string line) // M처럼 실제 행동 연출 위에 자동 대사만 표시합니다.
        { // 화면 클릭으로 문장을 넘기는 입력이나 다음 버튼은 연결하지 않습니다.
            HideAll(); m_CaptionText.text = speaker + "  ·  " + line.Replace("\n", " "); m_SubtitleRoot.gameObject.SetActive(true); // 원본 Caption과 같은 한 줄 자막을 사용합니다.
        } // 장면 진행과 다음 문장은 상황의 실제 연출 코루틴이 결정합니다.

        public IEnumerator PlayCaption(string speaker, string line, float duration) // 기존 행동의 결과 대사를 연출 시간에 맞춰 자동으로 이어갑니다.
        { // 조사 중 읽기와 달리 컷씬 대사는 M의 시간 진행을 따릅니다.
            ShowCinematicCaption(speaker, line); yield return new WaitForSecondsRealtime(duration); // 친구의 동작과 대사를 함께 보여 주고 별도의 화면 클릭은 기다리지 않습니다.
        } // 호출한 상황 제어기가 다음 행동이나 관찰 단계로 이어갑니다.

        private void SetReading(string speaker, string line) // 조사 중에는 M의 읽기 전용 설명과 다음 물체 선택을 동시에 열어 둡니다.
        { // 본문을 읽기 위해 닫기나 다음을 누를 필요가 없습니다.
            m_ReadingPlaced = false; m_StorySpeaker.gameObject.SetActive(true); m_StorySpeaker.text = speaker; m_StoryText.text = line; m_StoryText.gameObject.SetActive(true); m_StoryPortrait.SetActive(speaker == "친구"); // M의 이름 띠와 친구 초상을 기존 프레임에서 표시합니다.
            ApplyTypography(m_StoryText, 35f, false); m_StoryText.font = m_Font; m_StoryText.fontSharedMaterial = m_RuntimeTextOverlay; m_StoryText.color = new Color32(58, 36, 24, 255); m_StoryText.lineSpacing = 9f; m_StoryText.alignment = TextAlignmentOptions.MidlineLeft; // 원본 BodySize 35와 줄 간격 및 잉크색을 적용합니다.
            ApplyTypography(m_StorySpeaker, 28f, true); m_StorySpeaker.color = new Color32(58, 36, 24, 255); m_StorySpeaker.textWrappingMode = TextWrappingModes.NoWrap; m_Guidance.Show(true); LayoutReading(); // 전체 짧은 문장을 즉시 표시하고 세계 선택은 계속 허용합니다.
        } // 내용을 읽어도 현재 물체 클릭 과제는 유지됩니다.

        private void LayoutReading() // M SpatialUI와 StorybookUI의 읽기·응답 열 크기를 적용합니다.
        { // 설명은 화면 왼쪽 아래에 두고 선택지가 열리면 그 위치를 고정합니다.
            const float width = 580f; float body = Mathf.Max(102f, m_StoryText.GetPreferredValues(m_StoryText.text, 476f, 0f).y + 12f); float height = Mathf.Max(210f, body + 80f); bool actions = m_ActionPanel != null && m_ActionPanel.WantsVisible; // 원본 PanelWidth와 본문 여백을 기준으로 높이를 확보합니다.
            RectTransform panel = (RectTransform)m_Guidance.transform; panel.sizeDelta = new Vector2(width, height); m_StoryText.rectTransform.sizeDelta = new Vector2(width - 104f, height - 80f); m_StoryText.rectTransform.anchoredPosition = Vector2.zero; // 원본 프레임 안쪽의 52 포인트 여백과 본문 위치를 적용합니다.
            m_ReadingPaper.sizeDelta = m_StoryText.rectTransform.sizeDelta + new Vector2(24f, 14f); m_ReadingPaper.anchoredPosition = Vector2.zero; // M 원본의 본문보다 가로 24·세로 14 포인트 넓은 읽기 종이를 배치합니다.
            RectTransform tab = (RectTransform)m_StorySpeaker.transform.parent; float tagWidth = Mathf.Clamp(m_StorySpeaker.GetPreferredValues().x + 54f, 132f, 292f); tab.sizeDelta = new Vector2(tagWidth, 58f); tab.anchoredPosition = new Vector2(-width * 0.5f + (m_StoryPortrait.activeSelf ? 88f : 30f) + tagWidth * 0.5f, height * 0.5f - 1f); m_StorySpeaker.rectTransform.sizeDelta = new Vector2(tagWidth - 42f, 46f); m_StorySpeaker.rectTransform.anchoredPosition = new Vector2(-3f, 1f); // M 원본의 화자 이름 띠 크기와 위치를 적용합니다.
            RectTransform portrait = (RectTransform)m_StoryPortrait.transform; portrait.sizeDelta = new Vector2(83f, 83f); portrait.anchoredPosition = new Vector2(-width * 0.5f + 47f, height * 0.5f - 9f); if (m_SpeechTail != null) { m_SpeechTail.sizeDelta = new Vector2(72f, 42f); m_SpeechTail.anchoredPosition = new Vector2(-width * 0.21f, -height * 0.5f - 12f); } // 친구 초상과 꼬리 장식도 원본의 비율을 유지합니다.
            float actionHeight = actions ? ((RectTransform)m_ActionPanel.transform).rect.height : 0f; float top = height * 0.5f + 48f; float bottom = -height * 0.5f - (actions ? 24f + actionHeight : 34f) - 16f; float centre = (top + bottom) * 0.5f; float pixelHeight = top - bottom; float pixelWidth = width + 48f; // M처럼 전체 읽기·선택 열의 중심을 기준으로 배치합니다.
            panel.anchoredPosition = new Vector2(0f, -centre); if (actions) ((RectTransform)m_ActionPanel.transform).anchoredPosition = new Vector2(0f, -height * 0.5f - 24f - centre); // 설명 아래에 실제 선택 버튼만 세로로 연결합니다.
            bool desktop = m_InputSource != null && m_InputSource.IsDesktopActive; float distance = 1.4f; float viewHeight = 2f * distance * Mathf.Tan(m_Viewer.fieldOfView * 0.5f * Mathf.Deg2Rad); float viewWidth = viewHeight * m_Viewer.aspect; // M의 읽기 거리 상한과 화면 비율을 그대로 사용합니다.
            float scale = desktop ? Mathf.Min(viewHeight / 720f * 0.82f, Mathf.Min(viewHeight * (actions ? 0.58f : 0.34f) / pixelHeight, viewWidth * 0.52f / pixelWidth)) : 0.002f; // PC 선택지는 읽고 누를 크기를 확보하고 조사 설명은 화면 아래의 작은 영역을 사용합니다.
            if (desktop || !m_ReadingPlaced || actions != m_ReadingHadActions) // PC는 같은 화면 위치를 유지하고 VR의 기존 최초 배치 조건은 보존합니다.
            { // M 씬의 readingDistance 1.4와 worldPixelSize 0.002를 실제 적용 지점에서 사용합니다.
                Vector3 position; Quaternion rotation; // 현재 표시할 읽기 열의 월드 배치를 계산합니다.
                if (desktop) { position = m_Viewer.ViewportToWorldPoint(new Vector3(0.5f, 0.10f + pixelHeight * scale / (2f * viewHeight), distance)); rotation = m_Viewer.transform.rotation; } // PC의 설명과 선택지를 화면 아래 중앙에 두고 아래쪽 여백을 일정하게 유지합니다.
                else // VR에서는 M처럼 얼굴과 중요한 단서가 보이는 영역을 피하는 위치를 고릅니다.
                { // 새 설명을 열 때만 후보를 평가하고 플레이어의 고개를 따라 패널을 끌고 다니지 않습니다.
                    m_ProtectedWorldAreas.Clear(); foreach (Collider item in m_ProtectedObjects) if (item != null && item.enabled && item.gameObject.activeInHierarchy) { Rect area = ProjectBounds(item.bounds); if (area.width > 0f && area.height > 0f) m_ProtectedWorldAreas.Add(area); } // 기존 씬의 친구·전화·신호등 영역만 보호합니다.
                    bool friendRight = m_ProtectedObjects.Length > 0 && m_ProtectedObjects[0] != null && m_Viewer.transform.InverseTransformPoint(m_ProtectedObjects[0].bounds.center).x >= 0f; Vector2 preferred = new Vector2(friendRight ? 0.24f : 0.76f, 0.34f); float tangent = Mathf.Max(0.1f, Mathf.Tan(m_Viewer.fieldOfView * 0.5f * Mathf.Deg2Rad)); float best = float.PositiveInfinity; position = m_Viewer.transform.position + m_Viewer.transform.forward * distance; rotation = m_Viewer.transform.rotation; // M 원본의 친구 반대쪽 선호 위치와 후보 점수 기준을 사용합니다.
                    for (int x = 0; x <= 10; ++x) for (int y = 0; y <= 8; ++y) // 원본과 같은 화면 안의 11×9 후보 위치를 비교합니다.
                    { // 설명 전체와 중요한 월드 물체의 겹침을 확인합니다.
                        Vector2 center = new Vector2(Mathf.Lerp(0.12f, 0.88f, x / 10f), Mathf.Lerp(0.12f, 0.88f, y / 8f)); Vector3 direction = (m_Viewer.transform.forward + m_Viewer.transform.right * ((center.x - 0.5f) * 2f * tangent * m_Viewer.aspect) + m_Viewer.transform.up * ((center.y - 0.5f) * 2f * tangent)).normalized; Vector3 candidate = m_Viewer.transform.position + direction * distance; Quaternion facing = Quaternion.LookRotation(direction, Vector3.up); Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity; // 실제 읽기 거리의 후보와 화면 경계를 계산합니다.
                        for (int i = 0; i < 4; ++i) { Vector3 corner = candidate + facing * new Vector3(((i & 1) == 0 ? -0.5f : 0.5f) * pixelWidth * scale, ((i & 2) == 0 ? -0.5f : 0.5f) * pixelHeight * scale, 0f); Vector2 projected = m_Viewer.WorldToViewportPoint(corner); min = Vector2.Min(min, projected); max = Vector2.Max(max, projected); } // 설명과 응답 열의 네 모서리를 화면에 투영합니다.
                        if (min.x < 0.025f || max.x > 0.975f || min.y < 0.025f || max.y > 0.975f) continue; Rect region = Rect.MinMaxRect(min.x, min.y, max.x, max.y); float overlap = 0f; foreach (Rect area in m_ProtectedWorldAreas) overlap += OverlapArea(region, area); float score = overlap * 10000f + Vector2.SqrMagnitude(center - preferred) * 0.01f; if (score >= best) continue; best = score; position = candidate; rotation = facing; // M과 같이 물체를 가리지 않는 후보를 우선하고 가까운 선호 위치를 선택합니다.
                    } // 선택한 위치는 다음 설명이나 응답 전환 전까지 유지합니다.
                } // 카메라 추적이나 루트 회전은 변경하지 않습니다.
                m_ReadingColumn.SetPositionAndRotation(position, rotation); m_ReadingColumn.localScale = Vector3.one * (scale / m_Anchor.lossyScale.x); m_ReadingPlaced = true; // 표시와 실제 버튼 판정에 같은 배치를 적용하며 PC 선택 중에는 커서로 직접 누르게 합니다.
            } // 같은 선택지 위에서 시선만 바뀌어도 버튼 위치는 움직이지 않습니다.
            m_ReadingHadActions = actions; // 설명과 응답 사이 전환에서만 선택지의 기준 위치를 갱신합니다.
        } // 실제 카메라 회전이나 친구의 경로에는 관여하지 않습니다.

        public static string ReadPanelLine(FirstSituationPanel panel) // 기존 씬에 작성한 결과 설명을 읽기 영역으로 옮깁니다.
        { // 버튼 라벨을 본문으로 잘못 고르지 않습니다.
            string line = ""; foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true)) if (text.GetComponentInParent<Button>() == null && text.text.Length > line.Length) line = text.text; // 기존 제목과 본문 중 실제 설명 문장을 그대로 가져옵니다.
            return line; // 선택 결과나 안전 수칙의 기존 문구를 보존합니다.
        } // 새 설명 내용을 임의로 생성하지 않습니다.

        public void PresentChoices(FirstSituationPanel actions, string speaker, string line, bool judgment = true) // 세 상황의 판단·재시도 버튼을 M의 같은 응답 열에 연결합니다.
        { // 설명 창을 닫는 클릭 없이 실제 행동 버튼을 바로 선택하게 합니다.
            HideAll(); m_ActionPanel = actions; actions.transform.SetParent(m_ReadingColumn, false); Button[] buttons = actions.GetComponentsInChildren<Button>(true); float y = 0f; // 기존 버튼과 onClick 연결을 이동만 시켜 재사용합니다.
            foreach (Graphic graphic in actions.GetComponentsInChildren<Graphic>(true)) if (graphic.GetComponentInParent<Button>() == null) graphic.enabled = false; // 옛 중앙 팝업의 배경과 제목은 읽기 프레임에 중복 표시하지 않습니다.
            for (int i = 0; i < buttons.Length; ++i) { Button button = buttons[i]; StyleButton(button, judgment ? i < 2 : i == 0); TMP_Text text = button.GetComponentInChildren<TMP_Text>(true); float buttonHeight = Mathf.Max(76f, text.GetPreferredValues(text.text, 516f, 0f).y + 32f); RectTransform rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f); rect.sizeDelta = new Vector2(580f, buttonHeight); rect.anchoredPosition = new Vector2(0f, -y - buttonHeight * 0.5f); text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); text.rectTransform.sizeDelta = rect.sizeDelta - new Vector2(64f, 26f); text.rectTransform.anchoredPosition = Vector2.zero; y += buttonHeight + 12f; } // M 원본처럼 580 폭의 응답을 12 간격으로 놓고 글자 크기는 유지합니다.
            RectTransform actionRect = (RectTransform)actions.transform; actionRect.pivot = new Vector2(0.5f, 1f); actionRect.sizeDelta = new Vector2(580f, Mathf.Max(0f, y - 12f)); actions.Show(true); m_ReadingPlaced = false; SetReading(speaker, line); // 설명 바로 아래 실제 행동 선택지를 열고 배치 위치를 한 번만 저장합니다.
        } // 같은 선택 버튼은 PC 중앙 조준과 기존 VR 레이 모두 사용할 수 있습니다.
        public void PlaceInFront(Transform viewer, float distance = 2.15f) // 단계 시작 시 한 번만 편안한 읽기 거리로 배치합니다.
        { // 머리를 돌리는 동안 패널을 시야 중앙에 고정하지 않습니다.
            bool desktop = m_InputSource != null && m_InputSource.IsDesktopActive; // 기존 실제 입력 모드만 읽습니다.
            Vector3 forward = desktop ? viewer.forward : Vector3.ProjectOnPlane(viewer.forward, Vector3.up); // PC는 위아래 시선도 반영하여 화면 밖의 패널을 방지합니다. // 고개를 숙인 각도 대신 수평 시선 방향을 사용합니다.
            if (forward.sqrMagnitude < 0.001f) forward = viewer.parent.forward; // 수직으로 보고 있을 때는 기존 카메라 부모의 방향을 사용합니다.
            forward.Normalize(); // 표시 거리를 미터 단위로 적용합니다.
            m_Anchor.SetPositionAndRotation(viewer.position + forward * distance - Vector3.up * 0.12f, desktop ? viewer.rotation : Quaternion.LookRotation(forward)); // 카메라를 이동하지 않고 월드 Canvas만 배치합니다.
        } // 플레이어는 패널 옆의 실제 친구와 전봇대를 계속 살펴볼 수 있습니다.

        public void HideAll() // 현재 상황의 추가 말풍선까지 숨기고 다음 관찰 연결을 비웁니다.
        { // 원래 첫 번째·두 번째 패널 구성은 그대로 유지합니다.
            if (m_PlacedBubble != null) m_PlacedBubble.Show(false); m_PlacedBubble = null; // 다른 상황의 감사·호출 말풍선도 단계 종료와 함께 숨깁니다.
            if (m_SubtitleRoot != null) m_SubtitleRoot.gameObject.SetActive(false); // 단계가 바뀌면 이전 자막도 즉시 닫아 내용이 겹치지 않게 합니다.
            if (m_ActionPanel != null) m_ActionPanel.Show(false); m_ActionPanel = null; m_ReadingPlaced = false; // 세 상황에서 공유하는 현재 응답도 닫고 다음 읽기 배치를 새로 준비합니다.
            m_TaskCompleted = null; m_TaskTarget = null; if (m_ContextThought != null && m_ContextThought != m_Thought) m_ContextThought.Show(false); // 이전 대사와 물체 클릭 콜백을 닫아 다음 이야기에서 실행되지 않게 합니다.
            foreach (FirstSituationPanel panel in m_Panels) if (panel != null) panel.Show(false); // 원래 파스텔 패널의 표시를 정리합니다.
            m_ContextThought = m_Thought; m_ContextThoughtText = m_ThoughtText; // 다음 기본 관찰은 기존 말풍선으로 시작합니다.
        } // 첫 번째·두 번째 관찰 문구를 덮어쓰지 않습니다.
        public void ShowGuidance() { HideAll(); m_Guidance.Show(true); } // 보행 안내만 표시합니다.
        public void ShowIntroduction() { HideAll(); SetCinematicLabels(true); m_Closeup.Show(true); } // 별도 카메라의 1인칭 보행 장면을 실제 월드 공간 화면으로 보여 줍니다.
        public void ShowCloseup() { HideAll(); SetCinematicLabels(false); m_Closeup.Show(true); } // 같은 화면에서 전화 클로즈업의 실제 제목과 설명을 표시합니다.
        private void SetCinematicLabels(bool introduction) // 실제 Hierarchy의 문구를 전환하여 Inspector에서 편집한 내용을 유지합니다.
        { // 실행 중 텍스트나 패널 오브젝트를 만들지 않습니다.
            m_IntroductionHeading.SetActive(introduction); m_IntroductionCaption.SetActive(introduction); // 자동 보행 도입에서만 도입 문구를 표시합니다.
            m_CloseupHeading.SetActive(!introduction); m_CloseupCaption.SetActive(!introduction); // 이후 전화 클로즈업 문구로 전환합니다.
        } // 문구 내용과 글자 크기는 각 TMP 오브젝트에 저장된 값을 사용합니다.
        public void ShowTask(FirstSituationObservation target, string instruction, UnityAction onSelected, string speaker = "안내", string line = null) // M처럼 설명과 실제 물체 선택을 한 단계에서 제공합니다.
        { // 읽는 문장을 닫거나 다음을 눌러야 조사할 수 있는 구조를 사용하지 않습니다.
            HideAll(); m_TaskTarget = target; m_TaskCompleted = onSelected; m_TaskText.text = instruction; SetReading(speaker, line ?? instruction); // 현재 상호작용의 의미를 읽으면서 바로 해당 물체를 선택하게 합니다.
            m_Observation.Show(false); // 별도의 클릭 안내 팝업을 중복으로 띄우지 않습니다.
        } // 실제 선택 결과는 기존 관찰 컴포넌트의 TryCompleteTask로만 실행합니다.
        public bool TryCompleteTask(FirstSituationObservation target) // 마우스와 XR에서 같은 물체 클릭으로 현재 이야기 행동을 수행합니다.
        { // 안내한 실제 대상과 일치한 클릭만 진행으로 처리합니다.
            if (!IsTaskTarget(target)) return false; // 다른 단서 클릭은 관찰 문구만 보여 주게 합니다.
            UnityAction selected = m_TaskCompleted; // 현재 물체에 연결한 다음 이야기를 보관합니다.
            m_TaskTarget = null; m_TaskCompleted = null; // 새 대사나 행동을 열기 전에 이전 클릭 연결을 지웁니다.
            selected.Invoke(); // 물체에 맞는 대사나 다음 행동을 바로 시작합니다.
            return true; // PC 입력은 새 대화나 선택지를 조작할 커서를 즉시 표시합니다.
        } // 클릭 개수를 세지 않고 이야기의 실제 행동을 순서대로 연결합니다.

        public void ShowFriendDialogue(string line, Vector3 headPosition) // 친구의 말도 동일한 화면 자막으로 읽습니다.
        { // 별도 얼굴 옆 대화창을 띄우지 않습니다.
            SetReading("친구", line); // 친구 대사도 세계 선택을 막지 않는 M의 읽기 영역에 표시합니다. // 물체 관찰 입력은 유지하고 장면 아래에 말만 표시합니다.
        } // 다음 행동은 현재 상황의 기존 진행을 따릅니다.
        public void ShowThought(string thought, Vector3 hitPosition, Bounds bounds) // 다른 물체를 살펴본 생각은 화면 아래 자막으로 표시합니다.
        { // 진행 중인 클릭 과제의 대상과 콜백을 지우지 않습니다.
            SetReading("나", thought); // 물체 설명을 읽는 동안 현재 과제와 다른 관찰 대상의 클릭을 유지합니다. // 클릭한 물체를 말풍선으로 가리지 않고 생각을 읽게 합니다.
        } // 안내한 물체를 누르면 기존 과제가 다음 이야기를 엽니다.
        public void ShowChoice() { PresentChoices(m_Choice, "나", "친구가 휴대전화만 보고 걷고 있어." + "\n" + "어떻게 말해 줄까?"); } // 전봇대 앞에서 친구에게 실제로 할 말을 M의 응답 선택지로 표시합니다.
        public void ShowUnsafe() { PresentChoices(m_Retry, "잠깐!", ReadPanelLine(m_Unsafe), false); } // 사고 설명과 재시도 행동을 M의 같은 읽기·응답 열에 표시합니다.
        public void ShowSafe() { HideAll(); PlaceInFront(m_Viewer.transform); m_Safe.Show(true); PlaySuccessCue(); } // 실제 안전 완료 패널과 같은 시점에 성공 광채와 차임을 재생합니다.
        public void ShowDialogue(FirstSituationPanel panel, Vector3 headPosition) // 기존 경고·감사 문구를 실제 행동 연출의 자막으로 연결합니다.
        { // 결과 대사를 별도 클릭 창으로 띄우지 않습니다.
            ShowCinematicCaption("나", ReadPanelLine(panel)); // 씬에 작성된 문장을 M의 자동 연출 자막으로 보여 줍니다.
        } // 실제 행동과 다음 단계는 각 상황 코루틴이 계속 담당합니다.
        private void TrackBubble(FirstSituationPanel panel, Vector3 source, Bounds bounds, bool protect) // 대상을 바꿀 때 기존 말풍선의 위치를 자연스럽게 잇습니다.
        { // 처음 보이는 말풍선만 계산한 위치에 바로 배치합니다.
            bool first = m_PlacedBubble != panel || !panel.WantsVisible; // 같은 패널의 문구 교체 때는 이동을 보간합니다.
            if (m_PlacedBubble != null && m_PlacedBubble != panel) m_PlacedBubble.Show(false); // 이전 종류의 생각·감사 대사가 동시에 남지 않게 합니다.
            m_PlacedBubble = panel; m_BubbleSource = source; m_BubbleProtectedBounds = bounds; m_BubbleHasBounds = protect; // 다음 배치 프레임의 기준을 저장합니다.
            if (first) PlaceTrackedBubble(true); // 등장 전에 실제 읽기 위치를 먼저 정합니다.
        } // 카메라 자체는 움직이지 않습니다.

        private Rect ProjectBounds(Bounds bounds) // 실제 오브젝트 경계의 여덟 모서리를 화면에 투영합니다.
        { // 큰 친구 몸과 작은 휴대전화를 같은 크기로 취급하지 않습니다.
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity; // 앞쪽에 보이는 모서리만 경계를 누적합니다.
            for (int i = 0; i < 8; ++i) // 별도 배열 할당 없이 경계 상자의 모서리를 구합니다.
            { // 신호등이나 친구가 화면 가장자리에 있어도 실제 영역을 반영합니다.
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f)); // 각 축의 최소·최대 조합입니다.
                Vector3 screen = m_Viewer.WorldToViewportPoint(corner); if (screen.z <= 0f) continue; // 머리 뒤의 대상을 앞쪽 UI 가림 영역으로 사용하지 않습니다.
                Vector2 xy = new Vector2(screen.x, screen.y); min = Vector2.Min(min, xy); max = Vector2.Max(max, xy); // 현재 시야 경계를 확장합니다.
            } // 보이지 않는 대상은 빈 영역을 반환합니다.
            return float.IsInfinity(min.x) ? new Rect() : Rect.MinMaxRect(min.x, min.y, max.x, max.y); // 화면상 보호할 사각형입니다.
        } // Collider나 Renderer를 새로 만들지 않습니다.

        private static float OverlapArea(Rect a, Rect b) // 겹친 정도를 실제 면적으로 비교합니다.
        { // 접하기만 하는 경계는 가림으로 계산하지 않습니다.
            return Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) * Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin)); // 가로·세로 교집합의 곱입니다.
        } // 이전처럼 안내 패널 하나만 피하지 않습니다.

        private void CollectLayoutAreas() // 말풍선의 후보를 비교하기 전에 변하지 않는 대상 투영을 한 차례만 계산합니다.
        { // 42개 후보를 평가하면서 같은 렌더러·콜라이더를 수십 번 다시 투영하던 낭비를 줄입니다.
            m_OccupiedUIAreas.Clear(); m_ProtectedWorldAreas.Clear(); // 이전 상황의 화면 영역이 새 배치를 방해하지 않도록 목록만 비웁니다.
            foreach (FirstSituationPanel panel in FirstSituationPanel.ActivePanels) // 현재 보이는 실제 씬 패널만 고려합니다.
            { // 다른 상황의 비활성 패널은 포함하지 않습니다.
                if (panel == null || panel == m_PlacedBubble || !panel.IsDisplayed) continue; // 배치 대상 자신과 숨겨진 패널은 피할 필요가 없습니다.
                Rect occupied = panel.ViewportArea(m_Viewer); // 월드 공간 네 모서리를 이번 배치에서 한 번만 투영합니다.
                if (occupied.width > 0f && occupied.height > 0f) m_OccupiedUIAreas.Add(occupied); // 화면상 크기가 있는 패널만 보관합니다.
            } // 모든 후보에서 동일한 UI 영역을 재사용합니다.
            if (m_BubbleHasBounds) m_ProtectedWorldAreas.Add(ProjectBounds(m_BubbleProtectedBounds)); // 실제 클릭한 대상 자체를 강하게 보호합니다.
            if (m_ProtectedObjects != null) foreach (Collider target in m_ProtectedObjects) // 씬에서 연결한 중요한 사물·캐릭터의 경계를 확인합니다.
            { // 사라진 물체와 꺼진 충돌체는 현재 장면의 보호 대상이 아닙니다.
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy) continue; // 사용하지 않는 오브젝트를 제외합니다.
                m_ProtectedWorldAreas.Add(ProjectBounds(target.bounds)); // 같은 Collider의 여덟 모서리를 한 번만 투영합니다.
            } // 사용자 지정 충돌체나 씬 오브젝트는 변경하지 않습니다.
        } // 새 후보 위치 계산 전에 매 프레임 한 번 호출합니다.

        private float LayoutCost(Vector2 center, Vector2 half, Vector2 preferred) // 미리 계산한 화면 영역과 현재 말풍선 후보의 겹침을 비교합니다.
        { // 배치 후보 수가 증가해도 WorldToViewportPoint 호출 횟수가 함께 늘지 않습니다.
            Rect area = new Rect(center - half, half * 2f); float cost = (center - preferred).sqrMagnitude * 0.15f; // 말풍선이 가리키는 단서와의 거리를 배치 비용으로 사용합니다.
            cost += OverlapArea(area, new Rect(0.32f, 0.27f, 0.36f, 0.46f)) * 35f; // 보호 대상과 안내창을 피하면서도 중앙 조준점과 주요 연출은 비워 둡니다.
            foreach (Rect occupied in m_OccupiedUIAreas) cost += OverlapArea(area, occupied) * 1000f; // 선택·안내·결과 화면은 우선적으로 피합니다.
            for (int i = 0; i < m_ProtectedWorldAreas.Count; ++i) cost += OverlapArea(area, m_ProtectedWorldAreas[i]) * (m_BubbleHasBounds && i == 0 ? 180f : 65f); // 선택 대상과 친구 얼굴을 더 강하게 보호해 글자가 사물 위를 덮는 후보를 뒤로 미룹니다.
            return cost; // 공간이 좁을 때도 겹침이 가장 적은 위치를 반환합니다.
        } // 기존 몽실몽실한 UI 모양과 Hierarchy 구조는 유지합니다.

        private void PlaceTrackedBubble(bool immediate) // 말풍선의 실제 크기와 현재 시야를 사용해 위치를 정합니다.
        { // 카메라 회전에 따라 화면 밖으로 나간 말풍선도 읽을 수 있게 복귀시킵니다.
            FirstSituationPanel panel = m_PlacedBubble; panel.SetLayoutScale(1f); Vector2 size = panel.GetVisibleWorldSize(); // Inspector의 원래 크기를 기준으로 다시 계산합니다.
            Vector3 screenCenter = m_Viewer.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, m_BubbleDistance)); // 글자를 읽을 거리의 기준 평면입니다.
            Vector3 extent = m_Viewer.WorldToViewportPoint(screenCenter + m_Viewer.transform.right * size.x * 0.5f + m_Viewer.transform.up * size.y * 0.5f); // 실제 패널 크기의 화면 비율입니다.
            Vector2 half = new Vector2(Mathf.Abs(extent.x - 0.5f), Mathf.Abs(extent.y - 0.5f)); // 전체 말풍선의 반너비·반높이입니다.
            float fit = Mathf.Min(1f, (0.5f - m_BubbleScreenPadding) / Mathf.Max(0.001f, half.x), (0.5f - m_BubbleScreenPadding) / Mathf.Max(0.001f, half.y)); panel.SetLayoutScale(fit); half *= Mathf.Clamp(fit, 0.35f, 1f); // 화면보다 큰 말풍선만 실제 크기와 입력 영역을 함께 줄입니다.
            Vector2 margin = half + Vector2.one * m_BubbleScreenPadding; // 화면 가장자리의 여백을 확보합니다.
            Vector3 source = m_Viewer.WorldToViewportPoint(m_BubbleSource); if (source.z <= 0f) source = new Vector3(0.5f, 0.5f, 1f); // 뒤쪽 대상은 화면 안의 읽기 위치를 기준으로 합니다.
            Vector2 preferred = new Vector2(Mathf.Clamp(source.x + (source.x < 0.5f ? 1f : -1f) * (half.x + m_BubbleGap), margin.x, 1f - margin.x), Mathf.Clamp(source.y + half.y + m_BubbleGap, margin.y, 1f - margin.y)); // 선택 지점 옆의 처음 후보입니다.
            CollectLayoutAreas(); // 실제 씬 UI와 중요 오브젝트의 화면 위치를 한 번만 계산합니다.
            Vector2 best = preferred; float bestCost = LayoutCost(best, half, preferred); // 모든 배치 후보는 위에서 수집한 같은 영역을 재사용합니다.
            for (int row = 0; row < 6; ++row) for (int column = 0; column < 7; ++column) // 화면 안의 후보를 비교하여 여러 UI 사이의 남은 공간을 찾습니다.
            { // 각 후보는 전체 말풍선이 화면 안에 들어오는 중심입니다.
                Vector2 candidate = new Vector2(Mathf.Lerp(margin.x, 1f - margin.x, column / 6f), Mathf.Lerp(margin.y, 1f - margin.y, row / 5f)); float cost = LayoutCost(candidate, half, preferred); // 같은 기준으로 가림 정도를 평가합니다.
                if (cost >= bestCost) continue; best = candidate; bestCost = cost; // 덜 가리고 더 가까운 후보만 선택합니다.
            } // 화면 가장자리에서 잘리는 후보를 만들지 않습니다.
            Vector3 position = m_Viewer.ViewportToWorldPoint(new Vector3(best.x, best.y, m_BubbleDistance)); float blend = immediate ? 1f : 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime); // 처음 배치 뒤의 작은 변화는 부드럽게 보간합니다.
            panel.transform.SetPositionAndRotation(Vector3.Lerp(panel.transform.position, position, blend), Quaternion.Slerp(panel.transform.rotation, m_Viewer.transform.rotation, blend)); // 실제 월드 패널만 조정하고 XR 추적은 유지합니다.
        } // 생각·감사·호출에 동일한 규칙을 적용합니다.

        private void LateUpdate() // 시점이 갱신된 뒤 안내창과 말풍선의 화면 위치를 정합니다.
        { // 말풍선은 이동한 안내창을 가림 영역으로 읽습니다.
            if (m_SubtitleRoot != null && m_SubtitleRoot.gameObject.activeSelf) PositionSubtitles(); if (m_Guidance.WantsVisible) LayoutReading(); // 대사를 읽거나 고개를 돌리는 동안 자막의 화면 크기를 유지합니다.
            if (m_Observation.WantsVisible && m_InputSource != null && m_InputSource.IsDesktopActive) // PC에서 둘러보는 동안에도 안내창을 중앙 조준점 밖에 유지합니다.
            { // 이미 배치한 관찰 안내를 화면 왼쪽 아래에 맞춥니다.
                m_ObservationInViewport = true; m_Observation.SetLayoutScale(1f); // 씬 원래 크기를 기준으로 화면에 맞는 배율을 새로 계산합니다.
                Vector2 size = m_Observation.GetVisibleWorldSize(); // 버튼과 문구를 포함한 실제 월드 크기를 읽습니다.
                const float depth = 2.15f; // 기존 안내창과 비슷한 읽기 거리를 유지합니다.
                Vector3 corner = m_Viewer.ViewportToWorldPoint(new Vector3(0f, 0f, depth)); // 같은 깊이에서 화면 왼쪽 아래의 위치를 구합니다.
                Vector3 upper = m_Viewer.ViewportToWorldPoint(new Vector3(1f, 1f, depth)) - corner; // 카메라 기준으로 사용할 가로·세로 표시 범위입니다.
                float width = Vector3.Dot(upper, m_Viewer.transform.right), height = Vector3.Dot(upper, m_Viewer.transform.up); // 화면 비율이나 카메라 시야각에 맞는 월드 길이를 구합니다.
                float fit = Mathf.Clamp(Mathf.Min(width * 0.35f / size.x, height * 0.2f / size.y), 0.35f, 1f); // 안내가 중앙 시야를 가리지 않는 크기로 줄입니다.
                m_Observation.SetLayoutScale(fit); // 글과 버튼의 실제 클릭 영역을 같은 비율로 조정합니다.
                Vector3 center = m_Viewer.ViewportToWorldPoint(new Vector3(0.96f - size.x * fit / (2f * width), 0.91f - size.y * fit / (2f * height), depth)); // M처럼 현재 행동 안내를 오른쪽 위에 두어 친구와 도로를 비워 둡니다.
                m_Observation.transform.SetPositionAndRotation(center, m_Viewer.transform.rotation); // 플레이어가 고개를 돌려도 안내가 화면 모서리를 따라옵니다.
            } // 배치는 안내창 자체에만 적용해 공유 Canvas의 선택지와 암전을 움직이지 않습니다.
            else if (m_ObservationInViewport) // 안내가 닫히거나 VR로 전환되면 원래 배치로 되돌립니다.
            { // 데스크톱용 위치와 배율이 다른 조작 모드에 남지 않게 합니다.
                m_ObservationInViewport = false; m_Observation.SetLayoutScale(1f); // 씬 원래 글과 버튼 크기를 복원합니다.
                m_Observation.transform.SetLocalPositionAndRotation(m_ObservationLocalPosition, m_ObservationLocalRotation); // 저장한 안내창의 위치와 방향을 복원합니다.
            } // PC 이외에는 기존 월드 배치를 계속 사용합니다.
            if (m_PlacedBubble != null && m_PlacedBubble.WantsVisible) PlaceTrackedBubble(false); // 안내창과 친구·차량을 피하는 기존 말풍선 배치를 갱신합니다.
        } // UI는 카메라의 회전을 읽기만 하고 시점 입력을 변경하지 않습니다.

        public void ShowThanks(Transform friend) // 안전하게 피한 친구의 화면 위치 옆에서 감사 대사를 읽을 수 있게 합니다.
        { // 다른 패널의 편집된 위치는 변경하지 않습니다.
            HideAll(); // 이전 관찰이나 선택 패널을 숨기고 실제 친구의 감사 동작을 보여 줍니다.
            TrackBubble(m_Thanks, friend.position + Vector3.up * 1.7f, new Bounds(friend.position + Vector3.up * 1.55f, new Vector3(0.75f, 0.7f, 0.5f)), true); // 실제 친구 얼굴을 보호하며 감사 문구를 배치합니다. // 실제 친구의 얼굴 옆에서 읽을 수 있는 위치에 감사 말풍선을 배치합니다.
            m_Thanks.Show(true); // 감사 동작에 맞춰 부드럽게 표시합니다.
        } // 카메라나 친구 모델의 위치는 변경하지 않습니다.
        public void Transition(bool visible) { Transition(visible, m_FadeDuration); } // 기존 호출은 첫 번째 상황의 편집된 전환 시간을 그대로 사용합니다.
        public void Transition(bool visible, float duration) { m_ActiveFadeDuration = Mathf.Max(0.05f, duration); m_FadeVisible = visible; if (visible) m_CueRemaining = 0f; } // 위치 복원 암전을 시작하면 이전 상황의 주변광을 함께 정리합니다.

        public void PlayDangerCue() { BeginCue(new Color(1f, 0.32f, 0.08f, 1f), 0.75f); } // 실제 접촉이나 급제동 순간에만 따뜻한 주황 주변광을 짧게 표시합니다.
        public void PlaySuccessCue() { BeginCue(new Color(0.18f, 0.9f, 0.58f, 0.85f), 1.1f, m_SuccessClip); } // 기존 그래픽 컨셉의 민트빛과 상승 차임으로 실제 안전 완료를 강조합니다.
        private void BeginCue(Color color, float duration, AudioClip clip = null) // 기존 전환막과 전용 음원에 한 번의 상황 강조를 요청합니다.
        { // 상황 진행 상태와 카메라 및 배우의 움직임은 변경하지 않습니다.
            if (!isActiveAndEnabled) return; // 종료된 공유 UI에 새 효과를 남기지 않습니다.
            m_CueColor = color; m_CueDuration = duration; m_CueRemaining = duration; // 새 상황의 색상과 종료 시간을 같은 시점에 기록합니다.
            if (m_PresentationAudio != null && clip != null) m_PresentationAudio.PlayOneShot(clip, 0.8f); // 경적과 대사를 끊지 않고 짧은 연출 차임만 더합니다.
        } // 매 프레임 재생하거나 새로운 화면 오브젝트를 만들지 않습니다.

        private void Update() // 상황 정지 중에도 헤드셋 추적과 독립적으로 전환막만 페이드합니다.
        { // 실제 XR 카메라의 위치·각도·시야각은 변경하지 않습니다.
            if (m_ImmersiveFade == null) return; // 씬 종료 중 이미 해제된 전환막에는 접근하지 않습니다.
            m_FadeWeight = Mathf.MoveTowards(m_FadeWeight, m_FadeVisible ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.05f, m_ActiveFadeDuration)); // 전역 시간을 바꾸지 않고 해당 상황에서 요청한 길이로 암전을 진행합니다.
            Color tint = m_FadeColor; tint.a *= Mathf.SmoothStep(0f, 1f, m_FadeWeight); // 복원 순간에만 짧은 어두운 전환을 적용합니다.
            m_CueRemaining = Mathf.Max(0f, m_CueRemaining - Time.unscaledDeltaTime); // 강조가 끝나면 현재 상황의 주변광을 정상 시간으로 지웁니다.
            float cueProgress = 1f - m_CueRemaining / Mathf.Max(0.01f, m_CueDuration); // 현재 강조의 시작부터 끝까지 진행도를 구합니다.
            Color cue = m_CueColor; cue.a *= m_CueStrength * Mathf.Max(0f, Mathf.Sin(cueProgress * Mathf.PI)); // 점멸 없이 한 번 부드럽게 나타났다 사라지는 주변광을 만듭니다.
            m_FadeProperties.SetColor(k_FadeColor, tint); m_FadeProperties.SetColor(k_CueColor, cue); m_ImmersiveFade.SetPropertyBlock(m_FadeProperties); // 같은 전환막에 복원 암전과 상황별 주변광을 함께 전달합니다.
            m_ImmersiveFade.enabled = m_FadeWeight > 0.001f || cue.a > 0.001f; // 복원 암전과 상황 강조가 모두 끝났을 때만 렌더링을 끕니다.
        } // 머리를 돌려도 카메라 자식인 구체가 전체 시야를 동일하게 가립니다.

        private void OnDestroy() // 카메라 아래로 옮긴 자막과 복사한 글자 재질을 상황 UI와 함께 정리합니다.
        { // 원본 씬 글꼴과 M 재질 에셋에는 손대지 않습니다.
            if (m_SubtitleRoot != null) Destroy(m_SubtitleRoot.gameObject); // 별도 부모에 붙은 자막이 씬에 남지 않게 합니다.
            if (m_RuntimeTextOverlay != null) Destroy(m_RuntimeTextOverlay); // 실행 중 만든 글자 재질만 해제합니다.
        } // 원본 폰트와 UI 이미지는 프로젝트가 계속 관리합니다.
        private void OnDisable() // UI가 먼저 비활성화되어도 전환막이 시야에 남지 않게 합니다.
        { // 해제된 Renderer의 속성에는 접근하지 않습니다.
            if (m_SubtitleRoot != null) m_SubtitleRoot.gameObject.SetActive(false); m_TaskCompleted = null; m_TaskTarget = null; // 공유 UI가 비활성화되면 카메라 자막과 남은 진행 입력도 함께 닫습니다.
            m_FadeVisible = false; m_FadeWeight = 0f; m_CueRemaining = 0f; // 재활성화 시 이전 암전과 상황 강조를 남기지 않습니다.
            if (m_PresentationAudio != null) m_PresentationAudio.Stop(); // UI가 종료되면 재생 중인 연출 차임과 선택음을 함께 정리합니다.
            if (m_ImmersiveFade != null) m_ImmersiveFade.enabled = false; // 살아 있는 전환막만 즉시 숨깁니다.
        } // 원본 머티리얼과 헤드셋 추적은 유지합니다.

    } // 첫 번째 상황의 월드 공간 표시를 마칩니다.
} // 버튼의 진행 명령은 씬에 저장된 onClick 연결을 사용합니다.
