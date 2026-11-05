using System.Collections.Generic; // UI 레이 결과를 프레임마다 새로 할당하지 않고 재사용합니다.
using TMPro; // PC 조작일 때 씬의 기존 보행 안내 문구만 바꿉니다.
using Unity.XR.CoreUtils; // 기존 XR Origin과 1인칭 카메라를 그대로 사용합니다.
using UnityEngine; // 마우스 시점과 실제 물체의 포인터 선택을 처리합니다.
using UnityEngine.EventSystems; // 기존 EventSystem으로 UI가 마우스를 받는 위치를 확인합니다.
using UnityEngine.InputSystem; // WASD와 마우스 입력을 New Input System으로 읽습니다.
using UnityEngine.InputSystem.XR; // 헤드셋이 없을 때만 기존 TrackedPoseDriver를 잠시 보류합니다.
using UnityEngine.UI; // 기존 월드 Canvas의 마우스용 GraphicRaycaster를 연결합니다.
using UnityEngine.XR; // 헤드셋의 실제 연결 상태를 읽어 VR 조작을 보존합니다.
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers; // 키보드 벡터를 기존 이동 Provider의 지원 입력 경로로 전달합니다.
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement; // 새 이동기를 만들지 않고 기존 연속 이동·충돌·중력을 사용합니다.
using UnityEngine.XR.Interaction.Toolkit.UI; // 기존 XR UI 모듈의 마우스 입력을 PC 모드에서 사용합니다.

namespace Y.Crosswalk // 횡단보도 씬에서만 PC 입력을 기존 VR 구성에 연결합니다.
{ // 헤드셋이 감지되면 원래 추적·입력 참조를 복원합니다.
    [DefaultExecutionOrder(-100)] // 기존 이동 Provider가 입력을 읽기 전에 이번 프레임의 WASD 값을 전달합니다.
    public sealed class CrosswalkDesktopInput : MonoBehaviour // PC 둘러보기·3D 단서 선택과 기존 UI 입력 모듈의 모드 전환을 담당합니다.
    { // 실제 씬의 기존 구성과 PC 조절 항목을 Inspector에 노출합니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("현재 씬의 XR Origin입니다. PC에서도 같은 루트와 카메라를 사용합니다.")] private XROrigin m_Origin; // 중복 플레이어나 별도 1인칭 카메라를 생성하지 않습니다.
        [SerializeField, InspectorName("첫 번째 상황 진행"), Tooltip("자동 연출에서는 PC 시점을 고정하고, 관찰·선택·재시도에서는 보이는 마우스 커서로 조작합니다.")] private FirstSituationController m_Situation; // 실제 진행 상태로 시점 회전과 포인터 입력의 허용 시점을 결정합니다.
        [SerializeField, InspectorName("두 번째 상황 도입"), Tooltip("첫 완료 이후의 이동 보류와 호출·관찰 중 둘러보기 허용 상태를 읽습니다.")] private SecondSituationController m_SecondSituation; // 첫 상황 완료 직후 PC 자유 이동이 다음 도입에 끼어들지 않게 합니다.
        [SerializeField, InspectorName("기존 연속 이동"), Tooltip("현재 XR Origin의 DynamicMoveProvider를 연결합니다. WASD도 이 이동기의 충돌과 중력을 사용합니다.")] private ContinuousMoveProvider m_MoveProvider; // VR 이동과 같은 실제 루트 이동 경로를 사용합니다.
        [SerializeField, InspectorName("기존 XR UI 입력"), Tooltip("씬의 기존 XRUIInputModule입니다. PC에서도 이 모듈의 New Input System 마우스 클릭을 사용하고 VR 복귀 시 원래 입력 설정을 복원합니다.")] private XRUIInputModule m_UIInput; // 같은 클릭이 두 입력 경로에서 중복 실행되지 않게 합니다.
        [SerializeField, InspectorName("마우스 UI 레이캐스터"), Tooltip("월드 Canvas에 저장한 GraphicRaycaster입니다. VR용 TrackedDeviceGraphicRaycaster는 그대로 유지합니다.")] private GraphicRaycaster m_MouseUI; // 일반 마우스 포인터가 실제 월드 공간 버튼을 누르게 합니다.
        [SerializeField, InspectorName("네 관찰 대상"), Tooltip("상황 제어기와 같은 친구·전화·전봇대·보도 관찰 컴포넌트입니다.")] private FirstSituationObservation[] m_Targets; // 각 대상이 실제 등록한 Collider로 조준을 판정합니다.
        [SerializeField, InspectorName("기존 보행 안내 문구"), Tooltip("PC 조작 설명을 표시할 씬의 보행 안내 텍스트입니다. VR 복귀 시 원래 문구를 돌려줍니다.")] private TMP_Text m_Guidance; // 다른 패널이나 결과 설명은 변경하지 않습니다.
        [SerializeField, Min(1f), InspectorName("PC 눈높이(m)"), Tooltip("헤드셋이 없을 때의 발 기준 1인칭 눈높이입니다.")] private float m_EyeHeight = 1.65f; // 추적 장치가 없어도 바닥 높이에서 장면을 보지 않게 합니다.
        [SerializeField, Range(0.02f, 0.4f), InspectorName("마우스 회전 감도"), Tooltip("관찰 중 마우스 이동만으로 화면을 돌립니다. 화면 중앙으로 단서를 조준해 좌클릭으로 조사하고 Tab으로 버튼 클릭용 커서와 전환합니다.")] private float m_LookSensitivity = 0.12f; // 커서의 가장자리 위치가 아니라 실제 마우스 이동량으로 시점을 조절합니다.
        [SerializeField, Min(1f), InspectorName("마우스 선택 거리(m)"), Tooltip("관찰 대상과 기존 친구를 마우스로 선택할 수 있는 거리입니다.")] private float m_SelectDistance = 30f; // 기존 친구의 VR 명령 거리와 맞춥니다.
        [SerializeField, InspectorName("세 번째 상황 진행"), Tooltip("마지막 관찰·선택·재시도·종료 버튼을 같은 마우스 입력에 연결합니다.")] private ThirdSituationController m_ThirdSituation; // 활성화한 세 번째 상황이 완료된 두 번째 상태보다 입력 우선권을 가집니다.
        [SerializeField, InspectorName("PC 관찰 HUD"), Tooltip("씬에 미리 배치한 화면 중앙 조준점과 하단 조작 안내를 묶은 캔버스입니다.")] private GameObject m_DesktopObservationHUD; // PC 관찰 중에만 보이는 실제 씬 UI이며 VR에서는 숨깁니다.
        [SerializeField, InspectorName("PC 중앙 조준 표시"), Tooltip("마우스가 시점을 회전할 때만 화면 중앙에 표시하는 조준점입니다.")] private GameObject m_DesktopAimMark; // 잠긴 커서를 숨긴 대신 명확한 화면 조준 표시를 보여 줍니다.
        [SerializeField, InspectorName("PC 조작 모드 안내"), Tooltip("둘러보기와 커서 조작 중 현재 상태를 알려 주는 씬의 텍스트입니다.")] private TMP_Text m_DesktopModeHint; // 버튼이 클릭되지 않는 이유를 화면 안에서 즉시 알 수 있게 합니다.
        [SerializeField, InspectorName("관찰 완료 처리 UI"), Tooltip("기존 세 상황에서 공통으로 사용하는 관찰 완료 이벤트를 Space 키에도 연결합니다.")] private FirstSituationUI m_ObservationUI; // 실제 씬의 공통 완료 버튼과 같은 메서드만 호출합니다.
        [SerializeField, InspectorName("관찰 완료 버튼"), Tooltip("중앙 조준 모드에서도 조준점에 맞추고 좌클릭할 수 있는 공통 관찰 완료 버튼입니다.")] private Button m_ObservationFinishButton; // 잠긴 커서로 다른 UI를 누르지 않도록 이 버튼만 직접 연결합니다.
        private readonly RaycastHit[] m_WorldHits = new RaycastHit[128]; // 반복되는 물리 조준 결과를 재사용하며 가득 찬 경우에만 전체 결과를 가져옵니다.
        private EventSystem m_PointerEvents; // 포인터 데이터를 만든 기존 EventSystem을 보관합니다.
        private PointerEventData m_PointerData; // 기존 EventSystem의 포인터 데이터를 프레임마다 새로 할당하지 않습니다.
        private bool m_UIWasEnabledBeforeLook; // 회전 시작 전 UI 모듈 활성 상태를 그대로 복원합니다.
        private bool m_Looking, m_ApplicationPaused; // 잠긴 마우스 시점 회전과 앱 일시정지를 별도로 관리합니다.
        private bool m_ObservationCursorMode; // 관찰·결과에서 자유 시점, 선택·재시도·마무리에서 버튼 커서를 기본으로 사용하며 Tab으로 상호 전환합니다.
        private int m_InputStage = int.MinValue; // 상황이 바뀌면 이전 우클릭 잠금이 다음 화면에 남지 않게 합니다.
        private Vector2 m_CursorBeforeLook; // 회전을 마치면 버튼을 누르기 전의 커서 위치로 복원합니다.
        private readonly List<RaycastResult> m_UIHits = new List<RaycastResult>(); // 버튼 위 클릭이 뒤의 관찰 대상을 동시에 선택하지 않게 합니다.
        private readonly List<XRDisplaySubsystem> m_Displays = new List<XRDisplaySubsystem>(); // 로더 등록 여부와 실제 실행 중인 XR 표시 장치를 구분합니다.
        private UnityEngine.XR.InputDevice m_TrackedHead; // 실제 추적을 확인한 XR 장치 자체를 보관하여 공개되지 않은 장치 ID에 접근하지 않습니다.
        private bool m_HasTrackedHead, m_OriginalXRInputEnabled; // 잠깐의 추적 손실로 PC·VR 모드가 매 프레임 뒤바뀌지 않게 합니다.
        public bool IsDesktopActive => m_Desktop; // 관찰 대상도 현재 입력 방식 하나만 사용하게 합니다.
        private InputAction m_KeyboardMove; // 이 씬에서만 사용하는 WASD 액션입니다.
        private XRInputValueReader<Vector2> m_KeyboardReader, m_OriginalMoveReader; // 기존 이동 입력 참조를 보관하고 PC 입력만 임시로 연결합니다.
        private Camera m_View; // XR Origin이 이미 사용하는 실제 카메라입니다.
        private TrackedPoseDriver m_Tracking; // PC 시점과 헤드셋 추적이 동시에 카메라를 덮어쓰지 않게 합니다.
        private FirstSituationObservation m_Hovered; // 현재 마우스로 가리킨 첫 번째 상황의 단서입니다.
        private FriendStopInteraction m_FriendHovered; // 기존 건너편 친구의 마우스 조준 안내입니다.
        private Vector3 m_OriginalViewPosition; // 헤드셋 복귀 시 원래 카메라의 로컬 위치를 복원합니다.
        private Quaternion m_OriginalViewRotation; // 헤드셋 복귀 시 원래 카메라의 로컬 방향을 복원합니다.
        private CursorLockMode m_OriginalCursorLock; // PC 입력을 끌 때 커서 상태를 원래대로 돌려줍니다.
        private string m_OriginalGuidance; // 원래 VR 조작 안내를 보관합니다.
        private float m_Yaw, m_Pitch; // 직접 관찰이 허용된 단계에서 마우스 이동량으로 조절하는 PC 카메라 각도입니다.
        private bool m_Desktop, m_OriginalTrackingEnabled, m_OriginalMouseEnabled, m_OriginalRaycasterEnabled, m_OriginalCursorVisible; // PC 전환 전의 실제 활성 상태와 커서 표시 설정을 보존합니다.

        private void Awake() // PC 입력 리소스만 준비하고 원래 XR 설정은 아직 변경하지 않습니다.
        { // 기존 프로젝트의 공유 입력 액션 자산을 수정하지 않습니다.
            m_KeyboardMove = new InputAction("횡단보도 PC 보행", InputActionType.Value); // 씬 전용 WASD 액션을 준비합니다.
            m_KeyboardMove.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); // 대각선도 기존 2D 이동 벡터로 전달합니다.
            m_KeyboardReader = new XRInputValueReader<Vector2>(); // 이동 Provider가 지원하는 입력 리더를 사용합니다.
            m_KeyboardReader.inputSourceMode = XRInputValueReader.InputSourceMode.ManualValue; // 읽은 WASD 값을 기존 이동 계산에 주입합니다.
            m_View = m_Origin.Camera; m_Tracking = m_View.GetComponent<TrackedPoseDriver>(); // 중복 카메라 없이 기존 시점과 추적 컴포넌트를 확보합니다.
        } // Collider·중력·루트 이동은 기존 XR 이동 기능에 남깁니다.

        private void OnEnable() { m_KeyboardMove?.Enable(); } // 재활성화해도 같은 WASD 액션을 재사용합니다.
        private void Start() { RefreshMode(); if (m_Desktop) ApplyViewPose(); } // 상황 제어기가 시작 위치를 읽기 전에 PC 눈높이를 준비합니다.

        private void RefreshMode() // 실제 헤드셋 연결에 따라 PC 입력과 원래 XR 입력을 전환합니다.
        { // OpenXR 로더가 로드되었다는 이유만으로 PC 입력을 막지 않습니다.
            m_Displays.Clear(); SubsystemManager.GetSubsystems(m_Displays); // 현재 실행 중인 XR 디스플레이 목록을 읽습니다.
            bool displayRunning = false; foreach (XRDisplaySubsystem display in m_Displays) if (display.running) { displayRunning = true; break; } // 로더만 로드된 상태를 VR 연결로 판정하지 않습니다.
            var head = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head); // 실제 머리 장치의 추적 정보를 읽습니다.
            bool tracked = head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool isTracked) && isTracked; // XR 머리 장치의 실제 추적 여부를 확인합니다.
            if (!tracked && head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trackingState, out UnityEngine.XR.InputTrackingState tracking)) tracked = (tracking & (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation)) != 0; // Input System의 사용 이름과 혼동하지 않고 XR 추적 상태를 읽습니다.
            if (!displayRunning || !head.isValid) m_HasTrackedHead = false; // 디스플레이가 종료되거나 장치가 분리되면 PC 입력으로 복귀합니다.
            else if (tracked) { m_HasTrackedHead = true; m_TrackedHead = head; } // 실제로 추적된 XR 장치를 저장하여 연결 유지 여부를 비교합니다.
            SetDesktopMode(!(displayRunning && head.isValid && m_HasTrackedHead && m_TrackedHead.Equals(head))); // XR 장치의 동등성 비교로 같은 연결을 확인하고 일시적인 추적 손실에는 VR 입력을 유지합니다.
        } // VR에서는 기존 TrackedPoseDriver와 컨트롤러 입력 참조를 유지합니다.

        private void SetDesktopMode(bool desktop) // PC 진입·복귀 때 기존 설정을 한 번씩만 보관하거나 복원합니다.
        { // 매 프레임 원래 입력 상태를 덮어쓰지 않습니다.
            if (m_Desktop == desktop) return; // 이미 적용된 모드는 다시 초기화하지 않습니다.
            if (desktop) // 헤드셋이 없는 PC 조작을 시작합니다.
            { // 기존 이동기·추적·UI·커서 상태를 함께 보관합니다.
                m_OriginalMoveReader = m_MoveProvider.leftHandMoveInput; m_MoveProvider.leftHandMoveInput = m_KeyboardReader; // WASD도 기존 연속 이동과 충돌 경로로 전달합니다.
                m_OriginalViewPosition = m_View.transform.localPosition; m_OriginalViewRotation = m_View.transform.localRotation; // 원래 추적 카메라의 상대 자세를 보관합니다.
                m_OriginalTrackingEnabled = m_Tracking != null && m_Tracking.enabled; // 원래 추적이 꺼진 구성도 그대로 복원할 수 있게 합니다.
                if (m_Tracking != null) m_Tracking.enabled = false; // 추적 장치가 없는 동안 PC 시점이 원점으로 덮어써지지 않게 합니다.
                m_OriginalMouseEnabled = m_UIInput.enableMouseInput; m_OriginalXRInputEnabled = m_UIInput.enableXRInput; // 기존 UI 모듈의 원래 입력 설정을 보관합니다.
                m_UIInput.enableMouseInput = true; m_UIInput.enableXRInput = false; // PC의 버튼 누름·놓음·클릭은 기존 XRUIInputModule이 New Input System 마우스로 한 번만 처리합니다.
                m_OriginalRaycasterEnabled = m_MouseUI.enabled; m_MouseUI.enabled = true; // 월드 Canvas에 저장된 일반 마우스 레이캐스터만 켭니다.
                m_OriginalCursorLock = Cursor.lockState; m_OriginalCursorVisible = Cursor.visible; // 종료 또는 VR 복귀 시 원래 커서 상태를 복원합니다.
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // PC 시작부터 실제 마우스 커서를 보이게 하고 위치를 강제로 옮기지 않습니다.
                m_Yaw = m_OriginalViewRotation.eulerAngles.y; m_Pitch = Mathf.DeltaAngle(0f, m_OriginalViewRotation.eulerAngles.x);  // 현재 시점에서 시작하고 이전 PC 자동 시선의 회전 속도는 남기지 않습니다.
                if (m_Guidance != null) { m_OriginalGuidance = m_Guidance.text; m_Guidance.text = "관찰 중 마우스를 움직여 둘러보세요.\n화면 가운데 맞추고 좌클릭으로 조사 · 탭으로 버튼용 커서 전환"; } // 자동 연출 중에는 조작하지 않고 관찰 단계에서 마우스 이동과 선택을 사용하도록 안내합니다.
            } // 헤드셋을 연결하거나 이 입력기를 끌 때는 원래 구성을 돌려줍니다.
            else // PC 입력을 정리합니다.
            { // 파괴 순서와 관계없이 살아 있는 원본 컴포넌트만 복원합니다.
                EndMouseLook(); SetHovered(null, null); m_KeyboardReader.manualValue = Vector2.zero; // 마지막 마우스 테두리·버튼 누름·WASD 이동 값을 함께 지웁니다.
                if (m_MoveProvider != null) m_MoveProvider.leftHandMoveInput = m_OriginalMoveReader; // 기존 컨트롤러의 입력 리더를 정확히 복원합니다.
                if (m_View != null) m_View.transform.SetLocalPositionAndRotation(m_OriginalViewPosition, m_OriginalViewRotation); // PC 전용 눈높이와 마우스 각도를 원래 추적 구조로 되돌립니다.
                if (m_Tracking != null) m_Tracking.enabled = m_OriginalTrackingEnabled; // 실제 헤드셋의 추적 상태를 복원합니다.
                if (m_UIInput != null) { m_UIInput.enableMouseInput = m_OriginalMouseEnabled; m_UIInput.enableXRInput = m_OriginalXRInputEnabled; } // 실제 VR 연결에는 기존 컨트롤러 UI 입력을 그대로 복원합니다.
                if (m_MouseUI != null) m_MouseUI.enabled = m_OriginalRaycasterEnabled; // PC용 레이캐스터의 이전 활성 상태를 복원합니다.
                if (m_Guidance != null) m_Guidance.text = m_OriginalGuidance; // 원래 VR 보행 안내를 복원합니다.
                Cursor.lockState = m_OriginalCursorLock; Cursor.visible = m_OriginalCursorVisible; // VR 복귀와 씬 종료에는 PC 진입 전의 커서 상태를 복원합니다.
            } // XR Origin 루트의 현재 위치는 어느 전환에서도 되돌리지 않습니다.
            if (!desktop && m_DesktopObservationHUD != null) m_DesktopObservationHUD.SetActive(false); // 헤드셋으로 전환하는 순간 PC 조준 표시와 안내를 숨깁니다.
            m_Desktop = desktop; // 다음 프레임부터 선택한 입력 방식만 처리합니다.
        } // 스테이지 진행 상태와 완료 기록은 변경하지 않습니다.

        private void Update() // 관찰 단계는 마우스를 움직이기만 해도 시점이 돌아가고, UI 조작이 필요할 때만 커서를 표시합니다.
        { // VR 추적과 컨트롤러 입력은 기존 XR 구성에 남겨 둡니다.
            RefreshMode(); if (!m_Desktop) return; // 헤드셋이 연결된 경우에는 PC 전용 시점 제어를 적용하지 않습니다.
            m_KeyboardReader.manualValue = Vector2.zero; // 기존 1인칭 보행 연출에 별도의 직접 이동 입력을 추가하지 않습니다.
            Mouse mouse = Mouse.current; // 현재 PC 마우스를 New Input System에서 읽습니다.
            bool focused = Application.isFocused && !m_ApplicationPaused; // 창이 활성화된 동안에만 시점과 관찰 입력을 처리합니다.
            bool third = m_ThirdSituation != null && m_ThirdSituation.BlocksPlayerTranslation; // 진행 중인 세 번째 상황이 입력 판단에서 우선합니다.
            bool second = m_SecondSituation != null && m_SecondSituation.BlocksPlayerTranslation; // 세 번째 이전에는 두 번째 상황의 상태를 확인합니다.
            FirstSituationController.SituationState first = m_Situation != null ? m_Situation.CurrentState : FirstSituationController.SituationState.Complete; // 현재 첫 번째 상황 상태를 가져옵니다.
            int stage = third ? 200 + (int)m_ThirdSituation.CurrentState : second ? 100 + (int)m_SecondSituation.CurrentState : (int)first; // 상황별 상태값이 겹쳐도 별도 단계로 구분합니다.
            bool pointerNeeded; // 관찰만 잠긴 자유 시점으로 시작하고 선택지·재시도·완료 화면에서는 클릭 가능한 마우스 커서를 자동으로 보입니다.
            if (third) pointerNeeded = m_ThirdSituation.CurrentState == ThirdSituationController.StageState.Choice || m_ThirdSituation.CurrentState == ThirdSituationController.StageState.Retry || m_ThirdSituation.CurrentState == ThirdSituationController.StageState.Complete || m_ThirdSituation.CurrentState == ThirdSituationController.StageState.OverallSummary || m_ThirdSituation.CurrentState == ThirdSituationController.StageState.OpeningOverallSummary; // 세 번째 행동 선택 직후에도 Tab을 요구하지 않고 실제 버튼을 클릭하게 합니다.
            else if (second) pointerNeeded = m_SecondSituation.CurrentState == SecondSituationController.StageState.ActionSelectionReady || m_SecondSituation.CurrentState == SecondSituationController.StageState.Retry || m_SecondSituation.CurrentState == SecondSituationController.StageState.Complete; // 두 번째 행동 선택과 결과 버튼도 선택 화면 등장 즉시 커서를 사용합니다.
            else pointerNeeded = first == FirstSituationController.SituationState.Choice || first == FirstSituationController.SituationState.Retry || first == FirstSituationController.SituationState.Complete; // 첫 번째 전봇대 선택에서도 같은 커서 동작을 유지합니다.
            if (stage != m_InputStage) { EndMouseLook(); SetHovered(null, null); m_ObservationCursorMode = pointerNeeded; m_InputStage = stage; } // 상태가 바뀌는 첫 프레임에 커서 잠금을 해제하며 실제 선택 화면은 커서 모드로 엽니다.
            bool canLook = third ? m_ThirdSituation.AllowsLooking : second ? m_SecondSituation.AllowsLooking : first != FirstSituationController.SituationState.Walking && first != FirstSituationController.SituationState.Closeup && first != FirstSituationController.SituationState.Frozen && first != FirstSituationController.SituationState.WaitingToStart; // 자동 보행과 영상 전환을 제외한 장면에서만 자유 시점을 허용합니다.
            if (m_Situation != null && m_Situation.IsSceneTransitioning) canLook = false; // 암전 중에는 기존 장면 전환 처리를 유지합니다.
            bool observation = third ? m_ThirdSituation.IsObservationReady : second ? m_SecondSituation.IsObservationReady : first == FirstSituationController.SituationState.Observation; // 세 상황의 조사 단계에서만 중앙 조준을 활성화합니다.
            SyncDesktopObservationHUD(focused && canLook, observation); // 관찰 이후에도 조작 모드 안내를 유지하되, 중앙 조준점은 실제 단서 관찰에만 표시합니다.
            if (!focused || mouse == null || !canLook) // 입력을 받을 수 없는 동안에는 커서 잠금과 조준 표시를 안전하게 정리합니다.
            { // 비활성 창이나 자동 연출에서는 마우스가 의도하지 않게 움직이지 않습니다.
                EndMouseLook(); SetHovered(null, null); // 잠긴 커서와 마지막 조사 외곽선을 해제합니다.
                if (!focused || mouse == null) return; // 포커스가 없는 상태에서는 어떤 클릭도 처리하지 않습니다.
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return; // 시작 화면과 선택 패널을 마우스로 누를 수 있도록 합니다.
            }
            Keyboard keyboard = Keyboard.current; // 관찰 도중 커서 모드 전환용 키보드 입력을 확인합니다.
            bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame; // Escape는 관찰 조준 잠금을 해제하는 보조 입력입니다.
            bool tabPressed = keyboard != null && keyboard.tabKey.wasPressedThisFrame; // Tab은 자유 시점과 버튼 클릭용 커서를 전환합니다.
            bool finishPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame; // 화면 버튼을 찾지 않아도 관찰을 마칠 수 있는 보조 입력입니다.
            if (observation && finishPressed && m_ObservationUI != null) // 관찰 중 Space 입력은 현재 상황에 연결된 완료 버튼과 동일하게 처리합니다.
            { // 현재 세 상황 중 실제 관찰을 시작한 제어기만 다음 단계로 넘어갑니다.
                EndMouseLook(); SetHovered(null, null); // 다음 선택 화면에 커서 잠금과 기존 대상의 강조를 남기지 않습니다.
                m_ObservationUI.FinishCurrentObservation(); // 기존 UI가 보관한 완료 콜백만 실행하여 상황 진행 규칙을 유지합니다.
                return; // 동일 프레임의 조사 클릭이나 화면 전환 입력이 중복 처리되지 않게 합니다.
            }
            if (observation) // 위험 단서를 살피는 동안에는 우클릭을 누르지 않아도 계속 시점을 회전할 수 있습니다.
            { // 중앙 조준으로 관찰 오브젝트의 테두리를 확인하고 좌클릭으로 생각 말풍선을 띄웁니다.
                if (escapePressed) { m_ObservationCursorMode = true; EndMouseLook(); SetHovered(null, null); return; } // Escape로 언제든 포인터를 다시 볼 수 있습니다.
                if (tabPressed) { m_ObservationCursorMode = !m_ObservationCursorMode; EndMouseLook(); SetHovered(null, null); return; } // Tab으로 중앙 조준과 실제 마우스 커서를 전환합니다.
                if (m_ObservationCursorMode && mouse.rightButton.wasPressedThisFrame) { m_ObservationCursorMode = false; } // 포인터 모드에서 우클릭해도 바로 둘러보기로 돌아갑니다.
                if (!m_ObservationCursorMode) // 관찰의 기본 모드는 커서를 화면 중앙에 잠근 채 마우스 이동량으로 카메라를 회전합니다.
                { // 물체 위에 마우스가 있거나 화면 끝에 도달해도 회전이 중단되지 않습니다.
                    if (!m_Looking) { BeginMouseLook(mouse); return; } // 새로 잠근 프레임은 커서 이동량을 버려 갑작스러운 화면 튐을 막습니다.
                    if (Cursor.lockState != CursorLockMode.Locked) { m_ObservationCursorMode = true; EndMouseLook(); SetHovered(null, null); return; } // 운영체제나 에디터가 잠금을 해제했으면 커서 모드로 복원합니다.
                    UpdateMouseLook(mouse); ApplyViewPose(); // 오른쪽 버튼을 누르지 않아도 현재 마우스 이동량만큼 자유롭게 둘러봅니다.
                    ProcessPointer(mouse, m_View.pixelRect.center, false); // 화면 중앙에서 완료 버튼을 우선 확인한 뒤 3D 단서의 조사 입력을 처리합니다.
                    return; // 중앙 좌클릭은 이번 조준에서 선택한 한 대상에만 전달합니다.
                }
                EndMouseLook(); // 커서 모드에서는 기존 화면 버튼을 클릭하도록 UI 입력 모듈을 다시 켭니다.
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // 마우스를 실제 선택지와 관찰 완료 버튼 위치로 옮길 수 있습니다.
                Vector2 cursorPosition = mouse.position.ReadValue(); // UI와 같은 실제 커서 좌표를 사용합니다.
                if (m_View.pixelRect.Contains(cursorPosition)) ProcessPointer(mouse, cursorPosition, true); else SetHovered(null, null); // 커서 아래 관찰 대상 클릭도 유지합니다.
                return; // 커서 모드에서는 마우스를 움직여도 화면이 회전하지 않습니다.
            }
            if (escapePressed) { m_ObservationCursorMode = true; EndMouseLook(); SetHovered(null, null); return; } // 결과 연출 중에도 Escape로 일반 커서를 표시할 수 있습니다.
            if (tabPressed) { m_ObservationCursorMode = !m_ObservationCursorMode; EndMouseLook(); SetHovered(null, null); return; } // 관찰을 마친 뒤에도 Tab으로 자유 시점과 버튼 커서를 언제든 전환합니다.
            if (m_ObservationCursorMode && mouse.rightButton.wasPressedThisFrame) m_ObservationCursorMode = false; // 버튼을 누르기 전에 주변을 둘러보려면 우클릭으로 자유 시점에 들어갑니다.
            if (!m_ObservationCursorMode) // 안전·위험 결과와 친구가 횡단하는 동안에도 마우스 이동만으로 자유롭게 둘러봅니다.
            { // 관찰에서와 마찬가지로 화면 가장자리에 걸리지 않는 상대 이동량을 사용합니다.
                if (!m_Looking) { BeginMouseLook(mouse); return; } // 잠금 첫 프레임에는 마우스 이동량을 버려 시점이 갑자기 튀지 않게 합니다.
                if (Cursor.lockState != CursorLockMode.Locked) { m_ObservationCursorMode = true; EndMouseLook(); return; } // 운영체제나 에디터가 잠금을 해제하면 사용 가능한 커서 모드로 돌아옵니다.
                UpdateMouseLook(mouse); ApplyViewPose(); SetHovered(null, null); return; // 결과 연출에서는 조사 클릭을 발생시키지 않고 카메라 방향만 바꿉니다.
            } // 선택·재시도·마무리 패널은 커서 모드에서 기존 버튼 이벤트를 그대로 사용합니다.
            EndMouseLook(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // 실제 버튼을 클릭할 수 있도록 커서를 표시하고 UI 입력을 복원합니다.
            SetHovered(null, null); // 관찰이 끝났으면 마지막 대상의 강조를 없앱니다.
        } // 관찰 단계에서는 자유 시점이 기본, 버튼 클릭이 필요한 경우에는 Tab으로 커서를 사용합니다.

        private void SyncDesktopObservationHUD(bool show, bool observation) // 관찰·선택·결과 단계의 조작 안내를 표시하고 중앙 조준점은 조사할 때만 표시합니다.
        { // XR 입력과 다른 상황의 패널은 변경하지 않습니다.
            if (m_DesktopObservationHUD == null) return; // 오래된 씬에 이 HUD가 없어도 기존 조작은 유지합니다.
            if (m_DesktopObservationHUD.activeSelf != show) m_DesktopObservationHUD.SetActive(show); // 관찰 중에만 HUD가 표시되도록 한 번만 토글합니다.
            if (!show) return; // 시작 화면이나 사고 결과에 조준 표시가 남지 않도록 합니다.
            bool isCursorMode = m_ObservationCursorMode; // Tab으로 바뀐 버튼 클릭 모드를 확인합니다.
            bool showAimMark = observation && !isCursorMode; // 움직이는 결과 장면에서는 중앙 조준점이 캐릭터를 가리지 않도록 숨깁니다.
            if (m_DesktopAimMark != null && m_DesktopAimMark.activeSelf != showAimMark) m_DesktopAimMark.SetActive(showAimMark); // 관찰 시에만 조준점이 나타나고, 버튼 커서나 결과 장면에서는 표시하지 않습니다.
            if (m_DesktopModeHint != null) // 매 프레임 문자열을 새로 생성하지 않고 모드별 안내가 바뀔 때만 갱신합니다.
            { // Space 키와 Tab 키가 무엇을 하는지 화면에 계속 알립니다.
                string guidance = observation ? (isCursorMode ? "커서로 버튼 클릭  |  탭: 둘러보기  |  스페이스: 관찰 완료" : "중앙 + 조준 · 좌클릭: 조사/버튼  |  탭: 커서  |  스페이스: 관찰 완료") : (isCursorMode ? "커서로 선택  |  탭: 자유 시점  |  우클릭: 둘러보기" : "마우스 이동: 자유 시점  |  탭: 버튼 커서"); // 중앙 조준 모드에서 조사와 완료 버튼에 같은 좌클릭을 사용한다고 안내합니다.
                if (m_DesktopModeHint.text != guidance) m_DesktopModeHint.text = guidance; // 동일한 안내를 불필요하게 다시 쓰지 않습니다.
            }
        } // 모든 안내 UI는 씬에 직접 배치되어 Inspector에서 편집할 수 있습니다.

        private void BeginMouseLook(Mouse mouse) // 잠긴 상대 마우스 이동량을 사용하도록 기존 XR UI 입력을 잠시 보류합니다.
        { // 시점 회전과 마우스 UI 클릭이 한 프레임에 중복 실행되지 않게 합니다.
            if (m_Looking || mouse == null) return; // 이미 둘러보는 중에는 잠금 상태를 다시 만들지 않습니다.
            m_CursorBeforeLook = mouse.position.ReadValue(); m_Looking = true; // 커서 모드로 돌아갈 때 복원할 위치와 회전 상태를 보관합니다.
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; // 실제 커서를 잠그고 마우스 이동량으로 무제한 회전할 수 있게 합니다.
            m_UIWasEnabledBeforeLook = m_UIInput != null && m_UIInput.enabled; // 기존 UI 입력 모듈의 활성 상태를 보관합니다.
            if (m_UIInput != null) m_UIInput.enabled = false; // 중앙 조준 좌클릭이 숨겨진 화면 버튼도 누르는 현상을 방지합니다.
            SetHovered(null, null); // 이전 커서 조준 표시를 정리합니다.
        } // 말풍선과 버튼 오브젝트 자체는 변경하지 않습니다.

        private void EndMouseLook() // 전환·포커스 손실·우클릭 해제에서 같은 정리 경로를 사용합니다.
        { // 헤드셋 모드의 커서 상태는 변경하지 않습니다.
            if (!m_Desktop) return; // VR의 기존 입력 설정을 보존합니다.
            bool restore = m_Looking; m_Looking = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // 상대 입력과 커서 잠금을 함께 끝냅니다.
            if (restore && m_UIInput != null) m_UIInput.enabled = m_UIWasEnabledBeforeLook; // 회전 전에 활성화되어 있던 동일 UI 모듈을 복원하며 별도 이벤트 처리기를 만들지 않습니다.
            if (restore && Application.isFocused && Mouse.current != null) Mouse.current.WarpCursorPosition(m_CursorBeforeLook); // 해제 뒤 커서가 화면 중앙으로 튀어 UI를 잘못 가리키지 않게 합니다.
        } // 별도의 입력 모듈이나 클릭 이벤트를 만들지 않습니다.

        public void SetScenarioView(Transform framing) // 암전 중 PC 관찰 구도를 새 장소의 배치 지점에 맞춥니다.
        { // VR 헤드셋 회전과 XR Origin 회전은 변경하지 않습니다.
            if (!m_Desktop || framing == null) return; // PC 모드와 실제 씬 배치점이 있는 경우만 처리합니다.
            EndMouseLook(); Quaternion local = Quaternion.Inverse(m_View.transform.parent.rotation) * framing.rotation; // 실제 카메라 부모를 기준으로 새 구도를 계산합니다.
            m_Yaw = local.eulerAngles.y; m_Pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, local.eulerAngles.x), -75f, 75f); ApplyViewPose(); // 이전 장소의 PC 회전값을 다음 장면에 남기지 않습니다.
        } // 관찰 중에는 이 메서드를 호출하지 않습니다.

        private void UpdateMouseLook(Mouse mouse) // 잠긴 관찰 모드에서 우클릭 없이 상대 마우스 이동량을 시점에 반영합니다.
        { // 커서를 멈추면 화면 가장자리에서도 시점 회전이 즉시 멈춥니다.
            Vector2 delta = mouse.delta.ReadValue(); // New Input System에서 이번 프레임의 실제 가로·세로 마우스 이동량을 읽습니다.
            m_Yaw += delta.x * m_LookSensitivity; // 오른쪽으로 움직인 거리만큼 오른쪽을 보며 프레임 시간은 중복 적용하지 않습니다.
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * m_LookSensitivity, -75f, 75f); // 위로 움직이면 고개를 들고 시점이 뒤집히는 각도는 제한합니다.
        } // HMD 추적과 XR Origin 루트에는 회전을 적용하지 않습니다.

        private void LateUpdate() { if (m_Desktop) ApplyViewPose(); } // 헤드셋이 없는 PC 모드에서만 눈높이와 마우스 방향을 적용합니다.
        private void ApplyViewPose() // 기존 XR 카메라의 PC 전용 상대 자세를 적용합니다.
        { // XR Origin이 사용하는 Floor·Device 높이 설정 차이를 반영합니다.
            float offset = m_Origin.transform.InverseTransformPoint(m_View.transform.parent.position).y; // 기존 카메라 부모가 이미 올려 놓은 높이를 구합니다.
            m_View.transform.localPosition = new Vector3(m_OriginalViewPosition.x, m_EyeHeight - offset, m_OriginalViewPosition.z); // 높이를 중복 더하지 않고 발 기준 눈높이에 맞춥니다.
            m_View.transform.localRotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f); // 마지막 수동 회전 또는 암전 중 설정한 구도만 적용하여 자동 연출이 입력을 덮어쓰지 않게 합니다.
        } // 헤드셋 연결 시 원래 자세와 TrackedPoseDriver를 복원합니다.

        private Selectable FindSelectableAt(Vector2 position) // 실제 커서나 중앙 조준점이 가리키는 조작 가능한 UI를 찾습니다.
        { // 기존 EventSystem과 GraphicRaycaster를 그대로 사용합니다.
            EventSystem events = EventSystem.current; // 현재 씬의 입력 처리기를 읽습니다.
            if (events == null) return null; // UI가 없는 경우에는 물체 조준을 허용합니다.
            if (m_PointerData == null || m_PointerEvents != events) { m_PointerEvents = events; m_PointerData = new PointerEventData(events); } // EventSystem이 바뀔 때만 포인터 데이터를 준비합니다.
            m_PointerData.Reset(); m_PointerData.position = position; m_UIHits.Clear(); events.RaycastAll(m_PointerData, m_UIHits); // 화면 위의 레이 대상 중 실제 클릭 가능한 UI를 찾습니다.
            foreach (RaycastResult hit in m_UIHits) // 장식용 배경·말풍선 이미지가 실제 3D 관찰 클릭을 막지 않도록 하나씩 확인합니다.
            { // 선택 버튼과 상호작용 가능한 화면 요소만 UI 우선권을 가집니다.
                if (hit.gameObject == null) continue; // 파괴된 UI 오브젝트는 무시합니다.
                Selectable selectable = hit.gameObject.GetComponentInParent<Selectable>(); // 버튼 글자나 이미지에 닿아도 실제 조작을 담당하는 부모를 찾습니다.
                if (selectable != null && selectable.IsActive() && selectable.IsInteractable()) return selectable; // 현재 입력을 받을 수 있는 가장 앞쪽 UI만 반환합니다.
            } // 단순 안내 패널·비클릭 텍스트가 관찰 콜라이더를 가로채지 않습니다.
            return null; // 클릭할 UI가 없다면 기존 3D 관찰과 친구 조작을 허용합니다.
        } // 별도의 UI나 버튼 이벤트를 생성하지 않습니다.

        private void ProcessPointer(Mouse mouse, Vector2 position, bool useCursor) // 중앙 조준이나 실제 커서 위치에서 버튼 또는 3D 단서를 선택합니다.
        { // 먼저 실제 월드 공간 버튼의 레이를 확인합니다.
            Selectable selectable = FindSelectableAt(position); // 기존 레이캐스터로 버튼의 실제 활성 영역에 닿았는지 판단합니다.
            if (selectable != null && (useCursor || selectable == m_ObservationFinishButton)) // 커서는 기존 UI 입력을 쓰고 중앙 조준에서는 완료 버튼만 받습니다.
            { // 버튼을 조준하는 중에는 뒤쪽 단서가 파랗게 표시되거나 클릭되지 않게 합니다.
                SetHovered(null, null); // 기존 월드 단서의 조준 상태를 해제합니다.
                if (!useCursor && mouse.leftButton.wasPressedThisFrame) // 잠긴 커서에서는 새 좌클릭 한 번을 완료 버튼에 전달합니다.
                { // 씬에 연결된 실제 버튼과 그 클릭 효과를 함께 실행합니다.
                    m_PointerData.button = PointerEventData.InputButton.Left; // 버튼이 일반 좌클릭과 같은 입력으로 처리하도록 지정합니다.
                    ExecuteEvents.Execute(selectable.gameObject, m_PointerData, ExecuteEvents.pointerDownHandler); // 기존 버튼 효과가 클릭 시작을 기록합니다.
                    ExecuteEvents.Execute(selectable.gameObject, m_PointerData, ExecuteEvents.pointerUpHandler); // 같은 선택 입력에서 눌림을 해제해 효과가 남지 않게 합니다.
                    ExecuteEvents.Execute(selectable.gameObject, m_PointerData, ExecuteEvents.pointerClickHandler); // 기존 완료 이벤트와 클릭 확인음을 실행합니다.
                } // 일반 커서 모드의 누름과 놓음은 기존 UI 입력 모듈이 처리합니다.
                return; // 한 번의 클릭이 버튼 뒤에 있는 단서에도 들어가지 않게 합니다.
            } // 누를 수 있는 버튼이 없는 조준은 기존 3D 선택으로 이어집니다.
            bool thirdActive = m_ThirdSituation != null && m_ThirdSituation.BlocksPlayerTranslation; // 완료된 두 번째 상황이 세 번째 단서 클릭을 막지 않게 우선순위를 정합니다.
            if (thirdActive ? !m_ThirdSituation.IsObservationReady : m_SecondSituation != null && m_SecondSituation.BlocksPlayerTranslation && !m_SecondSituation.IsObservationReady) { SetHovered(null, null); return; } // 선택·재시도·최종 화면은 기존 UI 입력 모듈만 클릭합니다.
            PickWorldTarget(position, out FirstSituationObservation target, out FriendStopInteraction friend, out Vector3 hitPosition); // 커서 아래 실제 대상과 표면의 클릭 지점을 함께 가져옵니다.
            SetHovered(target, friend); // 바뀐 대상에만 마우스 조준 진입·이탈을 전달합니다.
            if (!mouse.leftButton.wasPressedThisFrame) return; // 누른 채 이동하는 동작을 연속 선택으로 처리하지 않습니다.
            if (target != null) target.SelectThought(hitPosition); else if (friend != null) friend.SendPointerCommand(m_View); // 관찰 말풍선에는 실제 클릭 지점을 전달하고 기존 친구 명령은 원래 경로를 유지합니다.
        } // 별도 정답 판단이나 사고 진행 코드는 만들지 않습니다.

        private void PickWorldTarget(Vector2 position, out FirstSituationObservation target, out FriendStopInteraction friend, out Vector3 hitPosition) // 실제 커서 아래 가장 가까운 관찰 대상과 표면 위치를 찾습니다.
        { // 친구와 전화의 선택 영역은 기존 XR 구성에 등록한 Collider를 그대로 사용합니다.
            target = null; friend = null; hitPosition = default; // 이전 프레임의 대상과 클릭 위치가 빈 공간에 남지 않게 합니다.
            RaycastHit hit = default; float nearest = float.PositiveInfinity; bool found = false; // 플레이어 자신을 제외한 가장 가까운 실제 차폐 표면을 고릅니다.
            Ray ray = m_View.ScreenPointToRay(position); int count = Physics.RaycastNonAlloc(ray, m_WorldHits, m_SelectDistance, ~(1 << 2), QueryTriggerInteraction.Ignore); // 일반 조준에서는 물리 결과 배열을 할당하지 않습니다.
            RaycastHit[] hits = count == m_WorldHits.Length ? Physics.RaycastAll(ray, m_SelectDistance, ~(1 << 2), QueryTriggerInteraction.Ignore) : m_WorldHits; // 결과가 가득 찬 경우에만 누락 없는 전체 표면을 가져옵니다.
            if (hits != m_WorldHits) count = hits.Length; // 예외적인 복잡한 장면도 실제 결과 수만 순회합니다.
            for (int index = 0; index < count; ++index) // 기존 관찰 거리와 레이어를 유지하며 자기 몸에 가려진 뒤쪽 결과도 가져옵니다.
            { // 원래 월드의 벽·차량·친구에 의한 앞뒤 관계는 유지합니다.
                RaycastHit candidate = hits[index]; // 재사용한 결과 배열의 현재 표면입니다.
                if (candidate.collider == null || candidate.collider.transform.IsChildOf(m_Origin.transform) || candidate.distance >= nearest) continue; // XR Origin의 몸·손 Collider와 이미 더 먼 표면은 클릭 차단 대상으로 삼지 않습니다.
                nearest = candidate.distance; hit = candidate; found = true; // 제외하지 않은 가장 가까운 표면만 관찰 대상으로 전달합니다.
            } // 뒤쪽 대상만 골라 벽을 통과해서 선택하는 판정은 하지 않습니다.
            if (!found) return; // 실제 월드 표면을 맞히지 않았으면 기존 강조를 해제합니다.
            hitPosition = hit.point; // 마우스가 실제로 선택한 표면을 말풍선 배치의 기준으로 보관합니다.
            foreach (FirstSituationObservation candidate in m_Targets) if (candidate != null && candidate.OwnsCollider(hit.collider)) { target = candidate; break; } // 네 단서 중 해당 Collider를 실제로 소유한 인스턴스를 찾습니다.
            FriendCharacterMovement owner = hit.collider.GetComponentInParent<FriendCharacterMovement>(); // 친구 몸과 손의 전화가 겹칠 때 같은 캐릭터의 단서만 비교합니다.
            if (owner != null && target != null) for (int index = 0; index < count; ++index) // 벽이나 다른 친구 뒤의 물체를 투과 선택하지 않습니다.
            { // 같은 캐릭터에 속한 가까운 소품만 우선합니다.
                RaycastHit candidate = hits[index]; if (candidate.collider == null || candidate.distance > nearest + 0.6f || candidate.collider.GetComponentInParent<FriendCharacterMovement>() != owner) continue; // 신체와 소품이 실제로 겹치는 범위로 제한합니다.
                foreach (FirstSituationObservation observation in m_Targets) if (observation != null && observation.PointerPriority > target.PointerPriority && observation.OwnsCollider(candidate.collider)) { target = observation; hitPosition = candidate.point; break; } // 실제로 맞힌 전화에만 더 높은 관찰 우선순위를 적용합니다.
            } // 전화 이외의 단서는 원래 가장 가까운 표면을 유지합니다.
            if (target == null && (m_ThirdSituation == null || !m_ThirdSituation.BlocksPlayerTranslation) && (m_SecondSituation == null || !m_SecondSituation.BlocksPlayerTranslation)) friend = hit.collider.GetComponentInParent<FriendStopInteraction>(); // 두 번째 상황의 관찰은 연결된 단서만 선택하며 기존 자유 횡단 명령을 우회 호출하지 않습니다. // 기존 친구의 멈춤·횡단 명령은 원래 컴포넌트에 전달합니다.
        } // 화면 중앙이나 별도 가상 영역으로 커서 판정을 대체하지 않습니다.

        private void SetHovered(FirstSituationObservation target, FriendStopInteraction friend) // 이전 마우스 강조와 안내를 남기지 않습니다.
        { // 파괴된 대상은 Unity의 실제 생존 여부를 확인한 뒤 접근합니다.
            if (m_Hovered != null && m_Hovered != target) m_Hovered.SetDesktopHover(false); // 이전 단서가 파란 조준 색에서 주황색 관찰 가능 표시로 돌아갑니다.
            if (m_FriendHovered != null && m_FriendHovered != friend) m_FriendHovered.SetDesktopHover(false); // 기존 친구의 마우스 안내를 해제합니다.
            m_Hovered = target; m_FriendHovered = friend; // 현재 조준을 저장합니다.
            if (m_Hovered != null) m_Hovered.SetDesktopHover(true); // 관찰 단계가 바뀐 뒤에도 현재 커서의 조준을 반영합니다.
            if (m_FriendHovered != null) m_FriendHovered.SetDesktopHover(true); // 기존 명령 안내를 마우스 조준에도 표시합니다.
        } // 마우스 이탈과 상황 정지 해제에 같은 정리 경로를 사용합니다.

        private void OnApplicationFocus(bool focused) // 창 전환으로 PC 입력이 남지 않게 합니다.
        { // VR 모드의 커서나 추적 설정은 변경하지 않습니다.
            if (focused || !m_Desktop) return; // 포커스를 잃은 PC 조작만 정리합니다.
            m_ObservationCursorMode = true; EndMouseLook(); m_KeyboardReader.manualValue = Vector2.zero; SetHovered(null, null); // 포커스가 돌아왔을 때 자동으로 마우스를 다시 잠그지 않도록 합니다.
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // 다른 창으로 전환해도 커서를 숨기거나 화면 중앙에 가두지 않습니다.
        } // 다시 게임 창에 돌아오면 현재 입력을 새로 읽습니다.
        private void OnApplicationPause(bool paused) { m_ApplicationPaused = paused; if (paused) { m_ObservationCursorMode = true; EndMouseLook(); SetHovered(null, null); } } // 앱 일시정지에서도 관찰 커서 잠금을 해제하고 복귀 시 커서를 유지합니다.
        private void OnDisable() { EndMouseLook(); SetDesktopMode(false); m_KeyboardMove?.Disable(); } // 씬 종료 때 원래 XR 입력·추적·커서를 돌려주고 PC 액션을 끕니다.
        private void OnDestroy() { m_KeyboardMove?.Dispose(); m_KeyboardReader?.inputAction?.Dispose(); } // 이 컴포넌트가 만든 씬 전용 입력 리소스만 해제합니다.
    } // 기존 VR 스테이지의 PC 조작 연결을 마칩니다.
} // 프로젝트의 공통 입력 자산과 다른 씬은 변경하지 않습니다.
