using UnityEngine; // 실제 씬의 파스텔 패널 배치와 표시를 제어합니다.
using Unity.XR.CoreUtils; // PC 마무리 패널이 지면에 들어가지 않도록 기존 XR Origin의 바닥 높이만 읽습니다.

namespace Y.Crosswalk // 기존 관찰 UI와 같은 월드 공간 입력 구성을 사용합니다.
{ // 새 패널은 실행 전 씬의 Hierarchy에 저장합니다.
    public sealed class SecondSituationUI : MonoBehaviour // 선택·경고·감사·안전·위험·재시도 패널을 표시합니다.
    { // 각 패널의 문구와 색상은 실제 TMP·Image·RectTransform에서 편집합니다.
        [SerializeField, InspectorName("기존 두 번째 월드 Canvas"), Tooltip("기존 호출 화면과 같은 World Space Canvas입니다.")] private Transform m_Anchor; // 별도의 입력 시스템이나 Canvas를 생성하지 않습니다.
        [SerializeField, InspectorName("실제 플레이어 카메라"), Tooltip("기존 XR Origin의 추적 카메라입니다. UI 배치에만 사용합니다.")] private Camera m_Viewer; // 카메라 이동·회전·시야각은 변경하지 않습니다.
        [SerializeField, InspectorName("빨간불 행동 선택"), Tooltip("기다려·건너와·다시 관찰하기 버튼을 포함한 실제 패널입니다.")] private FirstSituationPanel m_Choice; // 두 번째 상황의 선택지만 표시합니다.
        [SerializeField, InspectorName("기다려 경고"), Tooltip("플레이어의 빨간불 경고를 표시할 실제 말풍선입니다.")] private FirstSituationPanel m_Warning; // 안전 선택이 확정된 뒤에만 표시합니다.
        [SerializeField, InspectorName("친구 2 감사 대사"), Tooltip("아, 빨간불이었네! 고마워! 문구를 저장한 말풍선입니다.")] private FirstSituationPanel m_Thanks; // 신호를 확인하고 물러난 친구의 응답입니다.
        [SerializeField, InspectorName("안전 행동 완료"), Tooltip("민트색 안전 결과 패널입니다.")] private FirstSituationPanel m_Safe; // 다음 신호 상황을 자동 시작하지 않는 완료 표시입니다.
        [SerializeField, InspectorName("무단횡단 위험 설명"), Tooltip("부드러운 주황색 사고 원인 설명 패널입니다.")] private FirstSituationPanel m_Unsafe; // 실제 급정거 연출 뒤에만 설명을 표시합니다.
        [SerializeField, InspectorName("다시 관찰 안내"), Tooltip("관찰 복원 안내와 재시도 버튼이 있는 실제 패널입니다.")] private FirstSituationPanel m_Retry; // 기존 파스텔 버튼의 확대 반응을 재사용합니다.
        [SerializeField, Range(1.8f, 3.5f), InspectorName("UI 읽기 거리(m)"), Tooltip("패널을 배치할 편안한 읽기 거리입니다.")] private float m_Distance = 2.4f; // 친구가 건너편에 있어도 글자는 가까운 읽기 거리에서 표시합니다.
        [SerializeField, InspectorName("전체 체험 마무리 패널"), Tooltip("세 번째 상황 완료와 구분되는 전체 안전 수칙·체험 종료 패널입니다. 두 번째 UI에서는 비워 둡니다.")] private FirstSituationPanel m_OverallSummary; // 상황 완료와 전체 마무리를 서로 다른 실제 패널로 표시합니다.
        public float CompletionTransitionDuration => m_Safe.TransitionDuration; // 실제 세 번째 완료 패널이 퇴장한 뒤 전체 마무리로 전환합니다.
        private Vector3 m_OriginalAnchorScale; // 마무리 화면의 각도와 크기를 조절할 때 실제 Canvas 배율을 보존합니다.
        [SerializeField, InspectorName("공유 말풍선 배치"), Tooltip("첫 상황에서 사용하는 실제 말풍선 배치입니다. 활성 UI와 얼굴을 함께 피합니다.")] private FirstSituationUI m_SharedLayout; // 생각·감사 대사에 별도의 배치 규칙을 만들지 않습니다.
        private FirstSituationPanel[] m_Panels; // 이번 선택과 결과의 실제 패널 목록입니다.
        [SerializeField, InspectorName("기존 VR·마우스 전환"), Tooltip("헤드셋 연결 상태를 판정하는 기존 입력 컴포넌트입니다.")] private CrosswalkDesktopInput m_InputSource; // 입력 모드 판정을 새로 만들지 않습니다.
        [SerializeField, InspectorName("마우스 UI 레이캐스터"), Tooltip("같은 Canvas에 미리 배치한 GraphicRaycaster입니다.")] private Behaviour m_MouseRaycaster; // 실제 커서 위치의 기존 EventSystem 클릭을 받습니다.
        [SerializeField, InspectorName("VR UI 레이캐스터"), Tooltip("같은 Canvas에 미리 배치한 TrackedDeviceGraphicRaycaster입니다.")] private Behaviour m_VRRaycaster; // 기존 컨트롤러의 UI 조준·선택을 받습니다.
        [SerializeField, InspectorName("공유 Canvas 입력 전환 담당"), Tooltip("같은 Canvas를 사용하는 추가 상황 UI에서는 끕니다. 기존 두 번째 UI만 레이캐스터의 PC·VR 모드를 전환합니다.")] private bool m_ManageInputRaycasters = true; // 같은 입력 수신기를 여러 상황이 중복 제어하지 않습니다.
        private bool m_InputReady, m_Desktop; // 입력 모드가 바뀔 때만 이 Canvas의 레이캐스터를 전환합니다.

        private void OnEnable() { m_InputReady = false; } // 재활성화한 Canvas도 현재 입력 모드를 다시 적용합니다.
        private void Update() // 기존 입력 모드에 맞춰 두 번째 Canvas의 이벤트 수신기만 선택합니다.
        { // 버튼이나 입력 액션을 별도로 만들지 않습니다.
            if (!m_ManageInputRaycasters) return; // 추가 상황의 UI는 기존 Canvas 입력 구성을 그대로 공유합니다.
            bool desktop = m_InputSource != null && m_InputSource.IsDesktopActive; // 기존 헤드셋 판정 결과만 사용합니다.
            if (m_InputReady && m_Desktop == desktop) return; // 같은 모드에서는 레이캐스터의 활성 상태를 매 프레임 덮어쓰지 않습니다.
            m_InputReady = true; m_Desktop = desktop; m_MouseRaycaster.enabled = desktop; m_VRRaycaster.enabled = !desktop; // 마우스와 VR이 같은 클릭을 중복 처리하지 않게 합니다.
        } // 기존 XR Origin과 EventSystem은 유지합니다.

        private void Awake() // 이미 씬에 있는 패널만 준비합니다.
        { // 코드로 사각형·텍스트·버튼을 만들지 않습니다.
            m_OriginalAnchorScale = m_Anchor.localScale; m_Panels = new[] { m_Choice, m_Warning, m_Thanks, m_Safe, m_Unsafe, m_Retry, m_OverallSummary }; HideAll(); // 초기에는 선택 정답이나 결과를 미리 보여 주지 않습니다.
        } // 글꼴·배경·버튼 이미지 자산은 기존 리소스를 사용합니다.

        public void HideAll() // 다음 상태에서 이전 결과와 선택 버튼이 남지 않게 합니다.
        { // 아직 Awake 전이거나 씬 해제 중인 패널은 건너뜁니다.
            if (m_Panels == null) return; // 초기화 전 비활성화에서도 잘못된 참조를 사용하지 않습니다.
            foreach (FirstSituationPanel panel in m_Panels) if (panel != null) panel.Show(false); // 기존 CanvasGroup의 크기·투명도 퇴장 효과를 그대로 사용합니다.
        } // 숨긴 버튼은 기존 패널 코드가 즉시 입력을 해제합니다.

        private void PlaceAnchor() // 화면 전환 시 한 번만 실제 월드 UI 위치를 정합니다.
        { // 사용자가 고개를 돌릴 때 패널이 계속 따라붙지 않게 합니다.
            bool desktop = m_InputSource != null && m_InputSource.IsDesktopActive; // 기존 실제 PC·VR 입력 상태를 사용합니다.
            Vector3 forward = desktop ? m_Viewer.transform.forward : Vector3.ProjectOnPlane(m_Viewer.transform.forward, Vector3.up); // PC에서 아래를 보고 선택해도 패널이 수평 시야 밖에 생기지 않게 합니다. // 수평 시선 방향에서 읽기 위치를 구합니다.
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(m_Viewer.transform.parent.forward, Vector3.up); // 수직 시선에서는 기존 부모의 수평 방향을 사용합니다.
            forward.Normalize(); m_Anchor.SetPositionAndRotation(m_Viewer.transform.position + forward * m_Distance - Vector3.up * 0.15f, desktop ? m_Viewer.transform.rotation : Quaternion.LookRotation(forward)); // 실제 UI만 배치하고 XR 추적은 유지합니다.
        } // 중요한 도로와 캐릭터를 계속 둘러볼 수 있습니다.

        public void ShowChoice() { HideAll(); PlaceAnchor(); m_Choice.Show(true); } // 시간 제한 없이 두 행동과 다시 관찰하기를 표시합니다.
        public void ShowWarning(Vector3 friendHeadPosition) // 플레이어의 제지 대사를 중요 단서와 떨어진 말풍선으로 표시합니다.
        { // 기존 말풍선 배치가 친구와 접근 차량의 화면 영역을 피합니다.
            HideAll(); m_SharedLayout.ShowDialogue(m_Warning, friendHeadPosition); // 현재 상황의 경고 패널만 공유 배치에 전달합니다.
        } // 경고가 끝나면 같은 패널의 기존 퇴장 효과를 사용합니다.
        public void ShowSafe() { HideAll(); if (m_OverallSummary != null) PlaceCompletionAnchor(); else PlaceAnchor(); m_Safe.Show(true); m_SharedLayout.PlaySuccessCue(); } // 두 번째와 세 번째의 실제 완료 패널에 공유 성공 광채와 차임을 맞춰 표시합니다.
        public void ShowUnsafe() { HideAll(); PlaceAnchor(); m_Unsafe.Show(true); m_Retry.Show(true); } // 사고 원인과 빠른 재시도 버튼을 함께 표시합니다.

        public void ShowOverallSummary() // 세 번째 완료 화면의 다음 버튼으로 전체 체험 마무리를 표시합니다.
        { // 이전 두 상황의 안전 결과 패널은 바꾸지 않습니다.
            if (m_OverallSummary == null) return; // 전체 마무리를 소유하지 않는 두 번째 UI에는 개입하지 않습니다.
            HideAll(); PlaceCompletionAnchor(); m_OverallSummary.Show(true); // 실제 전체 마무리 패널과 종료 버튼만 표시합니다.
        } // 자동으로 사라지는 타이머는 사용하지 않습니다.

        private void PlaceCompletionAnchor() // PC에서 완료 화면이 수평 시야 밖에 나타나지 않게 실제 현재 시선에 맞춥니다.
        { // 카메라 회전·이동이나 머리 추적은 변경하지 않습니다.
            m_Anchor.localScale = m_OriginalAnchorScale; // 이전 완료 화면에서 줄인 크기를 다음 화면에 중복 적용하지 않습니다.
            if (m_InputSource == null || !m_InputSource.IsDesktopActive) { PlaceAnchor(); return; } // VR에서는 기존 편안한 거리의 월드 공간 배치를 유지합니다.
            Transform view = m_Viewer.transform; Vector3 forward = view.forward; float distance = m_Distance; // PC의 현재 수평·수직 시선 방향을 모두 반영합니다.
            XROrigin origin = m_Viewer.GetComponentInParent<XROrigin>(); float floor = origin != null ? origin.Origin.transform.position.y : view.position.y - 1.65f; // 기존 플레이어 바닥을 기준으로 패널의 지면 관통을 방지합니다.
            if (forward.y < -0.01f) distance = Mathf.Min(distance, Mathf.Max(0.4f, view.position.y - floor - 0.5f) / -forward.y); // 아래를 보고 있으면 패널이 바닥 안으로 들어가지 않는 거리로 제한합니다.
            distance = Mathf.Max(0.4f, distance); m_Anchor.localScale = m_OriginalAnchorScale * (distance / m_Distance); // 거리를 바꿔도 화면에서 보이는 글자와 패널 크기는 일정하게 유지합니다.
            m_Anchor.SetPositionAndRotation(view.position + forward * distance, view.rotation); // 실제 완료 UI만 현재 PC 시야 안에 놓고 마우스는 버튼 조작에 사용합니다.
        } // 기존 관찰·선택 패널과 두 번째 상황 배치에는 적용하지 않습니다.

        public void ShowThanks(Vector3 headPosition) // 실제 친구의 화면 위치 옆에 감사 말풍선을 배치합니다.
        { // 친구의 얼굴이나 신호를 큰 중앙 패널로 덮지 않습니다.
            HideAll(); m_SharedLayout.ShowDialogue(m_Thanks, headPosition); // 실제 감사 말풍선을 공유 배치에 연결하여 얼굴·신호·활성 UI를 피합니다.
        } // 문구·크기·색상은 Hierarchy의 실제 패널에 저장한 값을 유지합니다.
    } // 첫 번째 상황의 UI와 선택 이벤트는 변경하지 않습니다.
} // 관찰 생각 말풍선은 기존 공유 UI를 계속 사용합니다.
