using System.Collections.Generic; // 말풍선 한 번의 배치 동안 UI 및 관찰 대상의 화면 영역을 재사용합니다.
using TMPro; // 씬에 미리 제작된 선명한 한국어 UI 텍스트를 사용합니다.
using UnityEngine; // 월드 공간 UI의 배치와 포함 글꼴 리소스를 관리합니다.
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
        [SerializeField, InspectorName("한국어 UI 글꼴"), Tooltip("제공받은 Noto Sans KR Regular의 동적 SDF 에셋입니다. 씬에 저장된 텍스트와 실행 중 텍스트가 같은 글꼴을 사용합니다.")] private TMP_FontAsset m_Font; // 한국어 원본 글꼴이 연결된 프로젝트 에셋을 직접 사용하여 기본 영문 글꼴로 돌아가지 않게 합니다.
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
        private UnityAction m_FriendConversation; // 관찰 중인 상황에서 친구에게 말을 걸면 실행할 선택지 연결입니다.
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
            foreach (TMP_Text label in m_Texts) // 에디터와 실행 중에 동일한 Noto 한국어 글꼴 및 머티리얼 연결을 유지합니다.
            { // 기존 UI 오브젝트와 편집한 문구·크기는 그대로 사용합니다.
                label.font = m_Font; // 씬에 저장된 동적 SDF 에셋을 사용하여 한국어 글리프를 같은 원본에서 가져옵니다.
                label.fontSharedMaterial = m_TextOverlay != null ? m_TextOverlay : m_Font.material; // 씬에 연결한 동일 글꼴의 가림 방지 재질을 초기화에서 덮어쓰지 않습니다. // 이전 LiberationSans 머티리얼이 남아 다른 아틀라스를 참조하지 않게 합니다.
            } // 글꼴의 생성과 수명은 프로젝트 에셋이 관리합니다.
        } // 포함된 글꼴과 이미지 자산은 원본 상태로 보존합니다.

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
            m_FriendConversation = null; if (m_ContextThought != null && m_ContextThought != m_Thought) m_ContextThought.Show(false); // 상황이 끝나면 친구 대화 입력과 현재 말풍선을 함께 정리합니다.
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
        public void ShowObservation(UnityAction onTalkToFriend, FirstSituationPanel thoughtPanel = null, TMP_Text thoughtText = null) // 주변 조사 안내를 띄우고 같은 친구의 클릭을 현재 상황에 연결합니다.
        { // 관찰 문구와 결과 선택지는 상황 제어기가 각각 소유합니다.
            HideAll(); // 이전 결과와 대화를 닫고 이번 상황에 맞는 관찰부터 시작합니다.
            m_ContextThought = thoughtPanel != null ? thoughtPanel : m_Thought; m_ContextThoughtText = thoughtText != null ? thoughtText : m_ThoughtText; // 세 번째 상황도 실제 씬에 저장된 말풍선을 같은 표시 규칙에 연결합니다.
            m_FriendConversation = onTalkToFriend; // 친구를 직접 선택했을 때 열어야 할 현재 상황의 선택지를 기억합니다.
            m_Observation.Show(true); // 기존 파스텔 안내 패널을 시간 제한 없이 표시합니다.
            BeginCue(new Color(0.14f, 0.48f, 1f, 0.65f), 0.8f, m_ObservationClip); // 관찰이 시작된 순간에만 파란 주변광과 짧은 차임으로 입력 가능한 단계를 알립니다.
        } // 단서를 모두 조사했는지는 완료 조건으로 사용하지 않습니다.
        public bool BeginFriendConversation() // 마우스와 XR의 친구 선택을 현재 상황의 행동 선택으로 연결합니다.
        { // 활성 관찰에 등록된 대화만 한 번 실행합니다.
            UnityAction conversation = m_FriendConversation; // 현재 상황이 등록한 선택 진입점을 이 호출에 보관합니다.
            if (conversation == null) return false; // 대화 연결이 끝난 상태의 클릭은 소비하지 않습니다.
            m_FriendConversation = null; // 선택지로 전환하기 전에 중복 클릭 연결을 비웁니다.
            conversation.Invoke(); // 전봇대·빨간불·파란불 제어기가 각각 자신의 선택 패널을 엽니다.
            return true; // PC 입력은 대화가 열렸을 때 바로 선택용 커서를 표시합니다.
        } // 다시 둘러보기를 선택하면 관찰 진입에서 대화를 새로 연결합니다.

        public void ShowFriendDialogue(string line, Vector3 headPosition) // 지금 관찰 중인 상황에 친구가 먼저 건네는 대사를 표시합니다.
        { // 기존 말풍선 글꼴과 위치 배치 규칙을 사용합니다.
            m_ContextThoughtText.text = line; // 관찰 진입 직후에는 친구의 첫마디를 표시하고, 단서를 선택하면 그 단서의 생각으로 바꿉니다.
            ShowDialogue(m_ContextThought, headPosition); // 현재 상황의 말풍선을 친구 얼굴을 피하는 위치에 띄웁니다.
        } // 친구를 누를 수 있는 상태에서 대사를 읽으며 주변도 조사할 수 있습니다.
        public void ShowThought(string thought, Vector3 hitPosition, Bounds bounds) // 실제 클릭한 지점 옆에 해당 관찰 대상의 생각을 표시합니다.
        { // 선택 패널 전체를 시야 중앙으로 옮기지 않고 생각 말풍선만 배치합니다.
            m_ContextThoughtText.text = thought; // 기존 씬 텍스트에 선택한 단서의 문구를 반영합니다.
            TrackBubble(m_ContextThought, hitPosition, bounds, true); // 선택 대상의 실제 크기까지 고려하여 같은 말풍선을 배치합니다. // 마우스 클릭 또는 VR 레이가 닿은 위치를 기준으로 말풍선을 놓습니다.
            m_ContextThought.Show(true); // 기존 둥근 말풍선의 부드러운 등장 효과를 사용합니다.
        } // 다른 패널의 Inspector 위치와 실제 관찰 대상은 변경하지 않습니다.
        public void ShowChoice() { HideAll(); PlaceInFront(m_Viewer.transform); m_Choice.Show(true); } // 친구에게 말을 걸면 기존 전봇대 선택지 두 개를 표시합니다.
        public void ShowUnsafe() { HideAll(); PlaceInFront(m_Viewer.transform); m_Unsafe.Show(true); m_Retry.Show(true); } // 사고 원인과 관찰 복원 안내를 함께 표시합니다.
        public void ShowSafe() { HideAll(); PlaceInFront(m_Viewer.transform); m_Safe.Show(true); PlaySuccessCue(); } // 실제 안전 완료 패널과 같은 시점에 성공 광채와 차임을 재생합니다.
        public void ShowDialogue(FirstSituationPanel panel, Vector3 headPosition) // 두 번째·세 번째 대사도 동일한 배치 규칙을 사용합니다.
        { // 실제 씬 말풍선만 표시합니다.
            TrackBubble(panel, headPosition, new Bounds(headPosition - Vector3.up * 0.1f, new Vector3(0.7f, 0.7f, 0.5f)), true); panel.Show(true); // 친구 얼굴 주변의 영역을 비워 둡니다.
        } // UI·문구·입력 시스템을 복제하지 않습니다.

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
                Vector3 center = m_Viewer.ViewportToWorldPoint(new Vector3(0.04f + size.x * fit / (2f * width), 0.09f + size.y * fit / (2f * height), depth)); // 하단 조작 문구 위에 여백을 두고 왼쪽 가장자리에 놓습니다.
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

        private void OnDisable() // UI가 먼저 비활성화되어도 전환막이 시야에 남지 않게 합니다.
        { // 해제된 Renderer의 속성에는 접근하지 않습니다.
            m_FadeVisible = false; m_FadeWeight = 0f; m_CueRemaining = 0f; // 재활성화 시 이전 암전과 상황 강조를 남기지 않습니다.
            if (m_PresentationAudio != null) m_PresentationAudio.Stop(); // UI가 종료되면 재생 중인 연출 차임과 선택음을 함께 정리합니다.
            if (m_ImmersiveFade != null) m_ImmersiveFade.enabled = false; // 살아 있는 전환막만 즉시 숨깁니다.
        } // 원본 머티리얼과 헤드셋 추적은 유지합니다.

    } // 첫 번째 상황의 월드 공간 표시를 마칩니다.
} // 버튼의 진행 명령은 씬에 저장된 onClick 연결을 사용합니다.
