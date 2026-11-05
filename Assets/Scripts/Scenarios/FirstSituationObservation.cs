using UnityEngine; // 미리 배치한 외곽선 Renderer의 색상과 두께를 조절합니다.
using UnityEngine.XR.Interaction.Toolkit; // 기존 XR 조준·선택 이벤트를 수신합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactables; // 별도로 잡거나 이동하지 않는 관찰 대상을 사용합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors; // 컨트롤러 레이와 새 선택 입력을 구분합니다.

namespace Y.Crosswalk // 각 상황의 단서를 동일한 입력 규칙의 독립적인 인스턴스로 관리합니다.
{ // 원본 물체의 색을 변경하지 않고 별도 실루엣 외곽선만 표시합니다.
    public sealed class FirstSituationObservation : MonoBehaviour // 친구·소품·교통·보행 구간에 같은 관찰 입력과 테두리 전환을 재사용합니다.
    { // 각 대상의 생각과 외곽선을 Inspector에서 편집할 수 있게 합니다.
        [SerializeField, InspectorName("관찰 상호작용"), Tooltip("이 대상 Collider만 등록한 XRSimpleInteractable입니다.")] private XRSimpleInteractable m_Interactable; // 부모 친구와 전화 선택을 분리합니다.
        [SerializeField, InspectorName("VR·PC 입력 전환"), Tooltip("VR 연결 상태를 판정하는 기존 입력 컴포넌트입니다. PC에서는 마우스 조준만, VR에서는 컨트롤러 레이만 사용합니다.")] private CrosswalkDesktopInput m_InputSource; // 서로 다른 입력 방식의 강조가 동시에 남지 않게 합니다.
        [SerializeField, InspectorName("공유 생각 말풍선 UI"), Tooltip("두 상황이 함께 사용하는 기존 월드 공간 생각 말풍선 제어기입니다.")] private FirstSituationUI m_UI; // 새 말풍선을 만들지 않고 실제 씬 패널에 이 대상의 문구만 표시합니다.
        [SerializeField, TextArea(2, 4), InspectorName("선택했을 때 생각"), Tooltip("관찰 대상에 맞는 짧은 생각 문구입니다.")] private string m_Thought; // 정답을 미리 표시하지 않는 대상별 단서를 저장합니다.
        [SerializeField, InspectorName("관찰 강조 표시"), Tooltip("관찰할 수 있는 친구나 소품의 윤곽을 표시하는 기존 렌더러입니다.")] private Renderer[] m_Outlines; // 같은 조준 규칙으로 친구와 소품에 연결된 각자의 외곽선을 제어합니다.
        [SerializeField, ColorUsage(true), InspectorName("관찰 가능 색상"), Tooltip("관찰 단계에서 아직 조준하지 않은 단서의 외곽선에 사용하는 주황색입니다.")] private Color m_AvailableColor = new Color(1f, 0.43f, 0.12f, 0.85f); // 조사할 수 있는 대상을 조준하기 전에도 같은 표시로 알아볼 수 있게 합니다.
        [SerializeField, ColorUsage(false, true), InspectorName("조준 색상"), Tooltip("마우스나 VR 컨트롤러로 가리키는 동안만 단서의 강조 표시에 사용하는 파란색입니다.")] private Color m_Color = new Color(0.015f, 0.1f, 1.75f, 1f); // 씬에서 대상별로 지정한 기존 파란색을 조준에만 사용합니다.
        [SerializeField, Range(0.001f, 0.12f), InspectorName("테두리 두께(m)"), Tooltip("월드 길이로 확장하는 진한 외곽선의 두께입니다. 작은 휴대전화는 대상 크기에 맞는 값을 사용합니다.")] private float m_Width = 0.048f; // 기존 기본값의 네 배 두께로 클릭 가능한 윤곽을 분명하게 표시합니다.
        [SerializeField, Min(0.05f), InspectorName("테두리 전환 시간"), Tooltip("조준 진입·이탈에 걸리는 초 단위 시간입니다.")] private float m_FadeDuration = 0.16f; // 조준을 빠르게 바꿔도 자연스럽게 사라지게 합니다.
        [SerializeField, TextArea(2, 4), InspectorName("세 번째 상황에서의 생각"), Tooltip("같은 대상을 파란불 상황에서 조사할 때의 문구입니다. 이전 상황의 문구는 별도로 보존합니다.")] private string m_ThirdThought; // 실제 같은 대상의 상황별 관찰 데이터를 분리합니다.
        [SerializeField, InspectorName("마우스 관찰 우선순위"), Tooltip("같은 친구의 신체와 소품 판정이 겹칠 때 높은 값을 우선합니다. 손의 전화만 10으로 설정합니다.")] private int m_PointerPriority; // 벽이나 다른 대상 너머의 물체에는 적용하지 않습니다.
        public int PointerPriority => m_PointerPriority; // 입력이 실제 맞힌 같은 캐릭터의 단서끼리만 비교합니다.
        private bool m_ThirdObservation; // 현재 활성화한 관찰 단계가 세 번째인지 보관합니다.
        private MaterialPropertyBlock m_Properties; // 공유 외곽선 자산을 변경하지 않고 이 인스턴스의 표현만 조절합니다.
        private bool m_Available; // 관찰 단계 밖에서는 단서 선택을 받지 않습니다.
        private bool m_DesktopHovered; // 마우스 조준도 같은 관찰 대상의 외곽선 상태에 합칩니다.
        private float m_Weight; // 현재 테두리의 투명도입니다.
        private static readonly int k_Color = Shader.PropertyToID("_OutlineColor"), k_Width = Shader.PropertyToID("_OutlineWidth"); // 매 프레임 문자열 조회를 반복하지 않습니다.

        private void Awake() // 이미 저장된 외곽선과 XR 관찰 컴포넌트를 준비합니다.
        { // Renderer나 UI 오브젝트를 런타임에 생성하지 않습니다.
            m_Properties = new MaterialPropertyBlock(); // 대상별 색상·두께의 전달 공간만 준비합니다.
            foreach (Renderer outline in m_Outlines) if (outline != null) outline.enabled = false; // 살아 있는 외곽선만 숨겨 씬 오브젝트의 해제 순서에 의존하지 않게 합니다.
            if (m_Interactable != null) m_Interactable.enabled = false; // 관찰 전 입력을 끄되 이미 해제된 상호작용에는 접근하지 않습니다.
        } // 원본 모델의 Renderer는 계속 표시합니다.

        private void OnEnable() // 이 관찰 대상의 기존 XR 이벤트에 한 번만 연결합니다.
        { // 선택과 조준은 같은 대상의 등록 Collider를 사용합니다.
            if (m_Interactable == null) return; // 연결된 상호작용이 먼저 제거된 대상에는 이벤트를 등록하지 않습니다.
            m_Interactable.selectEntered.AddListener(Selected); // 기존 컨트롤러 Select를 생각 표시로 연결합니다.
        } // 새로운 InputAction이나 별도 레이캐스터를 만들지 않습니다.

        public void SetAvailable(bool available, bool thirdSituation = false) // 각 상황 제어기가 같은 대상의 입력과 상황별 문구를 함께 전환합니다.
        { // 새 입력 시스템이나 관찰 오브젝트를 만들지 않습니다.
            m_Available = available && isActiveAndEnabled && m_Interactable != null; // 살아 있는 실제 상호작용만 관찰할 수 있게 합니다.
            m_DesktopHovered = false; m_ThirdObservation = m_Available && thirdSituation; // 이전 조준을 해제하고 세 번째일 때만 별도 문구를 선택합니다.
            if (m_Interactable != null) m_Interactable.enabled = m_Available; // 기존 Collider와 XR 이벤트 수신을 같은 상태로 전환합니다.
        } // 테두리 알파는 기존 Update가 부드럽게 처리합니다.

        public bool OwnsCollider(Collider candidate) // 마우스도 XR에 실제 등록된 네 관찰 대상의 Collider 구성을 그대로 사용합니다.
        { // 별도 위치 기반 판정으로 전화와 친구 또는 전봇대 선택이 섞이지 않게 합니다.
            return m_Available && m_Interactable != null && m_Interactable.isActiveAndEnabled && candidate != null && m_Interactable.colliders.Contains(candidate); // 실제로 활성화된 같은 Collider 목록으로 테두리와 클릭을 판정합니다.
        } // 관찰 단계 밖의 물체는 선택 대상으로 반환하지 않습니다.

        public void SetDesktopHover(bool hovered) { m_DesktopHovered = m_Available && hovered; } // 마우스 진입·이탈을 기존 외곽선 페이드에 전달합니다.
        public void SelectThought(Vector3 hitPosition) // VR과 마우스가 실제 선택한 표면 옆에 같은 말풍선을 표시합니다.
        { // 입력 장치와 무관하게 관찰 단계에서만 생각을 표시합니다.
            string thought = m_ThirdObservation ? m_ThirdThought : m_Thought; // 활성 상황에 저장한 독립 문구를 고릅니다.
            if (m_Available && isActiveAndEnabled && m_UI != null && !string.IsNullOrWhiteSpace(thought)) m_UI.ShowThought(thought, hitPosition, ObservationBounds(hitPosition)); // 실제 조준한 대상의 현재 상황 문구만 기존 말풍선에 표시합니다.
        } // 친구의 행동이나 상황 진행은 변경하지 않습니다.

        private Bounds ObservationBounds(Vector3 fallback) // 실제 등록된 Collider 영역을 말풍선 배치에 전달합니다.
        { // 친구 전체와 휴대전화의 크기를 서로 분리합니다.
            Bounds bounds = new Bounds(fallback, Vector3.one * 0.1f); // 클릭 지점도 최소한의 보호 영역에 포함합니다.
            foreach (Collider collider in m_Interactable.colliders) if (collider != null && collider.enabled) bounds.Encapsulate(collider.bounds); // 이 관찰 대상이 실제로 소유한 표면만 합칩니다.
            return bounds; // 공유 UI가 대상 외형을 가리지 않는 위치를 고를 수 있게 합니다.
        } // 관찰 데이터나 상황 진행은 변경하지 않습니다.

        private bool HasXRPointerHover() // 누적한 이벤트 이력 대신 Toolkit이 현재 판정한 실제 조준 목록을 읽습니다.
        { // 비활성화되거나 사라진 컨트롤러의 테두리가 남지 않게 합니다.
            if (!m_Available || m_Interactable == null || !m_Interactable.isActiveAndEnabled || (m_InputSource != null && m_InputSource.IsDesktopActive)) return false; // PC 모드에서는 XR의 남은 조준을 사용하지 않습니다.
            foreach (IXRHoverInteractor interactor in m_Interactable.interactorsHovering) // 양손 중 어느 레이라도 현재 가리키는지를 확인합니다.
            { // 실제로 살아 있고 활성화된 기존 컨트롤러만 인정합니다.
                if (interactor is XRBaseInputInteractor controller && controller != null && controller.isActiveAndEnabled && controller is IXRRayProvider) return true; // 현재의 레이 조준만 같은 외곽선으로 표시합니다.
            } // 조준을 벗어난 대상은 다음 Update부터 페이드아웃합니다.
            return false; // 유효한 현재 조준이 없으면 강조를 해제합니다.
        } // 별도 레이캐스터나 컨트롤러 이벤트를 중복 생성하지 않습니다.
        private void Selected(SelectEnterEventArgs args) // 누른 채 다른 단서로 옮기는 동작은 새 클릭으로 처리하지 않습니다.
        { // 기존 XR 입력의 수행 프레임을 확인합니다.
            if (!m_Available || (m_InputSource != null && m_InputSource.IsDesktopActive) || !(args.interactorObject is XRBaseInputInteractor controller) || !(controller is IXRRayProvider pointer) || !controller.selectInput.ReadWasPerformedThisFrame()) return; // VR 모드의 새 선택과 실제 컨트롤러 레이를 함께 확인합니다.
            SelectThought(pointer.rayEndPoint); // 컨트롤러가 선택한 레이 끝점 옆에 마우스와 동일한 규칙으로 말풍선을 표시합니다.
        } // 물체나 친구를 이동시키지 않습니다.

        private void Update() // 관찰 가능 상태는 투명도를 정하고 실제 조준 여부는 강조 색을 정합니다.
        { // 친구의 실루엣과 소품 외곽선은 같은 관찰 상태와 색상 규칙을 사용합니다.
            bool desktop = m_InputSource != null && m_InputSource.IsDesktopActive; // 현재 입력 방식 하나만 강조의 기준으로 사용합니다.
            bool hovered = m_Available && (desktop ? m_DesktopHovered : HasXRPointerHover()); // 현재 선택할 수 있고 실제로 가리키는 단서에만 조준 색을 적용합니다.
            m_Weight = Mathf.MoveTowards(m_Weight, m_Available ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.05f, m_FadeDuration)); // 조준을 옮겨도 다른 단서의 표시가 사라지지 않고 관찰 단계가 닫힐 때만 함께 페이드합니다.
            Color color = hovered ? m_Color : m_AvailableColor; // 조준을 벗어나는 즉시 파란색을 거두고 관찰 가능 색으로 되돌립니다.
            color.a *= Mathf.SmoothStep(0f, 1f, m_Weight); // 선택된 강조 색의 투명도에 기존 등장·퇴장 전환만 적용합니다.
            m_Properties.SetColor(k_Color, color); m_Properties.SetFloat(k_Width, m_Width); // 편집한 색상과 실제 두께를 전달합니다.
            foreach (Renderer outline in m_Outlines) if (outline != null) { outline.enabled = m_Weight > 0.001f; outline.SetPropertyBlock(m_Properties); } // 해제된 Renderer는 건너뛰고 살아 있는 윤곽만 갱신합니다.
        } // 대상이 달라도 공유 머티리얼 상태가 섞이지 않습니다.

        private void OnDisable() // 상황 오브젝트가 꺼질 때 이벤트와 남은 윤곽을 정리합니다.
        { // 재활성화 시 리스너가 중복 등록되지 않게 합니다.
            if (m_Interactable != null) // 같은 씬의 XR 컴포넌트가 먼저 파괴된 경우에는 이벤트 해제를 시도하지 않습니다.
            { // 살아 있는 발신기에 등록했던 리스너만 제거합니다.
                m_Interactable.selectEntered.RemoveListener(Selected); // 이 컴포넌트가 등록한 선택 이벤트만 해제합니다.
            } // 상호작용의 해제 순서와 관계없이 나머지 정리를 계속합니다.
            m_Available = false; m_DesktopHovered = false; m_Weight = 0f; // 이전 입력 방식의 조준과 표시 상태를 남기지 않습니다.
            foreach (Renderer outline in m_Outlines) if (outline != null) outline.enabled = false; // 이미 파괴된 윤곽에는 접근하지 않고 남은 표시만 지웁니다.
        } // 원본 모델과 Collider는 제거하지 않습니다.
    } // 하나의 독립적인 관찰 단서를 마칩니다.
} // 정답 판정과 행동 선택은 상황 제어기에서만 수행합니다.
