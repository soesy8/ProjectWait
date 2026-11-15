using System.Collections.Generic; // 실제 헤드셋 표시 장치를 조회하는 목록을 재사용합니다.
using TMPro; // PC 조작일 때 씬의 기존 보행 안내 문구만 바꿉니다.
using Unity.XR.CoreUtils; // 기존 XR Origin과 1인칭 카메라를 그대로 사용합니다.
using UnityEngine; // 마우스 시점과 실제 물체의 포인터 선택을 처리합니다.
using UnityEngine.InputSystem; // WASD와 마우스 입력을 New Input System으로 읽습니다.
using UnityEngine.EventSystems; // 선택지가 열릴 때 기존 UI 모듈의 최종 포인터 판정을 걸러 같은 클릭이 이어지지 않게 합니다.
using UnityEngine.InputSystem.XR; // 헤드셋이 없을 때만 기존 TrackedPoseDriver를 잠시 보류합니다.
using UnityEngine.UI; // 기존 월드 Canvas의 마우스용 GraphicRaycaster를 연결합니다.
using UnityEngine.XR; // 헤드셋의 실제 연결 상태를 읽어 VR 조작을 보존합니다.
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers; // 키보드 벡터를 기존 이동 Provider의 지원 입력 경로로 전달합니다.
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement; // 새 이동기를 만들지 않고 기존 연속 이동·충돌·중력을 사용합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors; // 기존 양손 Near-Far 상호작용기를 재사용합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters; // UI와 실제 물체를 가리키는 같은 곡선 레이의 판정 마스크를 사용합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals; // 레이 표시 길이를 실제 판정 거리와 일치시킵니다.
using UnityEngine.XR.Interaction.Toolkit.UI; // 기존 XR UI 모듈의 마우스 입력을 PC 모드에서 사용합니다.

namespace Y.Crosswalk // 횡단보도 씬에서만 PC 입력을 기존 VR 구성에 연결합니다.
{ // 헤드셋이 감지되면 원래 추적·입력 참조를 복원합니다.
    [DefaultExecutionOrder(-100)] // 기존 이동 Provider가 입력을 읽기 전에 이번 프레임의 WASD 값을 전달합니다.
    public sealed class CrosswalkDesktopInput : MonoBehaviour // PC 둘러보기·3D 단서 선택과 기존 UI 입력 모듈의 모드 전환을 담당합니다.
    { // 실제 씬의 기존 구성과 PC 조절 항목을 Inspector에 노출합니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("현재 씬의 XR Origin입니다. PC에서도 같은 루트와 카메라를 사용합니다.")] private XROrigin m_Origin; // 중복 플레이어나 별도 1인칭 카메라를 생성하지 않습니다.
        [SerializeField, InspectorName("첫 번째 상황 진행"), Tooltip("첫 상황에서 관찰·선택·연출의 입력 상태를 읽습니다.")] private FirstSituationController m_Situation; // 처음 걷기와 이후 세 상황의 마우스 입력을 연결합니다.
        [SerializeField, InspectorName("이야기와 행동 안내"), Tooltip("M 방식의 읽기 전용 설명과 실제 물체·응답 버튼을 제공하는 씬 UI입니다.")] private FirstSituationUI m_StoryUI; // 설명을 읽어도 세계 선택을 유지하며 실제 행동 과제만 입력 상태에 반영합니다.
        [SerializeField, InspectorName("두 번째 상황 진행"), Tooltip("동행 보행과 빨간불 관찰·선택의 입력 상태를 읽습니다.")] private SecondSituationController m_SecondSituation; // 첫 상황이 끝나면 횡단보도 제어기의 입력 상태를 우선합니다.
        [SerializeField, InspectorName("기존 연속 이동"), Tooltip("현재 XR Origin의 DynamicMoveProvider를 연결합니다. WASD도 이 이동기의 충돌과 중력을 사용합니다.")] private ContinuousMoveProvider m_MoveProvider; // VR 이동과 같은 실제 루트 이동 경로를 사용합니다.
        [SerializeField, InspectorName("기존 XR UI 입력"), Tooltip("씬의 기존 XRUIInputModule입니다. PC에서도 이 모듈의 New Input System 마우스 클릭을 사용하고 VR 복귀 시 원래 입력 설정을 복원합니다.")] private XRUIInputModule m_UIInput; // 같은 클릭이 두 입력 경로에서 중복 실행되지 않게 합니다.
        [SerializeField, InspectorName("마우스 UI 레이캐스터"), Tooltip("월드 Canvas에 저장한 GraphicRaycaster입니다. VR용 TrackedDeviceGraphicRaycaster는 그대로 유지합니다.")] private GraphicRaycaster m_MouseUI; // 일반 마우스 포인터가 실제 월드 공간 버튼을 누르게 합니다.
        [SerializeField, InspectorName("관찰 대상"), Tooltip("세 상황의 관찰 제어기에서 사용하는 친구와 소품의 관찰 컴포넌트입니다.")] private FirstSituationObservation[] m_Targets; // 현재 상황에서 활성화된 단서만 PC 조준 판정에 사용합니다.
        [SerializeField, Min(1f), InspectorName("PC 눈높이(m)"), Tooltip("헤드셋이 없을 때의 발 기준 1인칭 눈높이입니다.")] private float m_EyeHeight = 1.65f; // 추적 장치가 없어도 바닥 높이에서 장면을 보지 않게 합니다.
        [SerializeField, Range(0.02f, 0.4f), InspectorName("마우스 회전 감도"), Tooltip("사물 조사 중 마우스로 둘러보는 감도입니다. 선택 버튼이 표시되면 시점 회전을 멈추고 커서로 누릅니다.")] private float m_LookSensitivity = 0.1f; // 사물 조사 단계의 상대 마우스 이동량만 시점에 적용합니다.
        [SerializeField, Min(1f), InspectorName("마우스 선택 거리(m)"), Tooltip("관찰 대상과 기존 친구를 마우스로 선택할 수 있는 거리입니다.")] private float m_SelectDistance = 30f; // 기존 친구의 VR 명령 거리와 맞춥니다.
        [SerializeField, InspectorName("세 번째 상황 진행"), Tooltip("마지막 관찰·선택·재시도·종료 버튼을 같은 마우스 입력에 연결합니다.")] private ThirdSituationController m_ThirdSituation; // 활성화한 세 번째 상황이 완료된 두 번째 상태보다 입력 우선권을 가집니다.
        [SerializeField, InspectorName("PC 관찰 HUD"), Tooltip("씬에 미리 배치한 화면 중앙 조준점과 하단 조작 안내를 묶은 캔버스입니다.")] private GameObject m_DesktopObservationHUD; // PC 관찰 중에만 보이는 실제 씬 UI이며 VR에서는 숨깁니다.
        [SerializeField, InspectorName("PC 중앙 조준 표시"), Tooltip("마우스가 시점을 회전할 때만 화면 중앙에 표시하는 조준점입니다.")] private GameObject m_DesktopAimMark; // 잠긴 커서를 숨긴 대신 명확한 화면 조준 표시를 보여 줍니다.
        [SerializeField, InspectorName("PC 조작 모드 안내"), Tooltip("둘러보기와 커서 조작 중 현재 상태를 알려 주는 씬의 텍스트입니다.")] private TMP_Text m_DesktopModeHint; // 버튼이 클릭되지 않는 이유를 화면 안에서 즉시 알 수 있게 합니다.
        private readonly List<Button> m_Buttons = new List<Button>(); // 세 상황과 시작 화면에 이미 존재하는 버튼 참조만 보관합니다.
        private CrosswalkReticleGraphic m_Reticle; // M 원본의 작은 원과 점 조준 표시를 사용합니다.
        private readonly RaycastHit[] m_WorldHits = new RaycastHit[128]; // 반복되는 물리 조준 결과를 재사용하며 가득 찬 경우에만 전체 결과를 가져옵니다.
        private bool m_UIWasEnabledBeforeLook; // 자유 시점에 들어가기 전의 마우스 UI 입력 상태를 저장해 선택용 커서로 돌아갈 때 복원합니다.
        private bool m_ChoiceClickBlocked; // 선택지를 연 클릭과 연속 클릭을 새 선택지에 전달하지 않도록 입력 보호 상태를 보관합니다.
        private float m_ChoiceInputReadyAt; // 선택지 표시 직후의 짧은 입력 보호 구간이 끝나는 실제 시간을 저장합니다.
        private NearFarInteractor[] m_Hands; // 현재 XR Origin 안의 기존 양손 레이만 사용합니다.
        private XRInputButtonReader[] m_OriginalHandSelect; // 이 씬을 나갈 때 각 손의 기존 선택 입력을 복원합니다.
        private CurveInteractionCaster[] m_HandCasters; // 표시 레이와 UI 판정이 사용하는 실제 캐스터를 보관합니다.
        private LayerMask[] m_OriginalHandMasks; // 선택지가 사라지면 각 손의 기존 물체 판정 마스크를 복원합니다.
        private bool[] m_OriginalHandUI, m_OriginalHandNear, m_OriginalHandFar; // 이 씬에서 바꾸는 기존 레이 설정을 비활성화 시 돌려놓습니다.
        private float[] m_OriginalHandDistances; // 기존 곡선 레이의 판정 거리를 보관합니다.
        private CurveVisualController[][] m_HandVisuals; // 각 손의 기존 레이 표시기를 그대로 사용합니다.
        private float[][] m_OriginalVisualDistances; // 레이 표시 거리도 이 씬을 나갈 때 복원합니다.
        private bool m_Looking, m_ApplicationPaused; // 상대 마우스 입력의 적용 여부와 앱의 일시정지 상태를 따로 추적합니다.
        private bool m_CursorRequested; // 자유 시점 중에만 Tab이나 Escape로 커서를 요청한 상태를 기억합니다.
        private Vector2 m_CursorBeforeLook; // 회전을 마치면 버튼을 누르기 전의 커서 위치로 복원합니다.
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

        private void OnEnable() { m_KeyboardMove?.Enable(); ConfigureVRHands(); if (m_UIInput != null) m_UIInput.finalizeRaycastResults += FilterOpeningChoiceRaycasts; } // 같은 양손 트리거와 UI 입력을 연결하고 새 선택지로 이전 입력이 넘어가지 않게 합니다.
        private void Start() { RefreshMode(); if (m_Desktop) ApplyViewPose(); m_Buttons.AddRange(m_StoryUI.GetComponentsInChildren<Button>(true)); foreach (SecondSituationUI ui in FindObjectsByType<SecondSituationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) ui.CollectButtons(m_Buttons); BuildReticle(); } // 기존 PC 눈높이를 준비하고 세 상황의 실제 버튼 참조와 M 조준 표시를 입력에 연결합니다.

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
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // PC 시작이나 VR 복귀 직후 현재 화면에서 실제 마우스 커서를 표시합니다.
                m_Yaw = m_OriginalViewRotation.eulerAngles.y; m_Pitch = Mathf.DeltaAngle(0f, m_OriginalViewRotation.eulerAngles.x);  // 현재 시점에서 시작하고 이전 PC 자동 시선의 회전 속도는 남기지 않습니다.
            } // 헤드셋을 연결하거나 이 입력기를 끌 때는 원래 구성을 돌려줍니다.
            else // PC 입력을 정리합니다.
            { // 파괴 순서와 관계없이 살아 있는 원본 컴포넌트만 복원합니다.
                EndMouseLook(); SetHovered(null, null); m_KeyboardReader.manualValue = Vector2.zero; // 마지막 마우스 테두리·버튼 누름·WASD 이동 값을 함께 지웁니다.
                if (m_MoveProvider != null) m_MoveProvider.leftHandMoveInput = m_OriginalMoveReader; // 기존 컨트롤러의 입력 리더를 정확히 복원합니다.
                if (m_View != null) m_View.transform.SetLocalPositionAndRotation(m_OriginalViewPosition, m_OriginalViewRotation); // PC 전용 눈높이와 마우스 각도를 원래 추적 구조로 되돌립니다.
                if (m_Tracking != null) m_Tracking.enabled = m_OriginalTrackingEnabled; // 실제 헤드셋의 추적 상태를 복원합니다.
                if (m_UIInput != null) { m_UIInput.enableMouseInput = m_OriginalMouseEnabled; m_UIInput.enableXRInput = m_OriginalXRInputEnabled; } // 실제 VR 연결에는 기존 컨트롤러 UI 입력을 그대로 복원합니다.
                if (m_MouseUI != null) m_MouseUI.enabled = m_OriginalRaycasterEnabled; // PC용 레이캐스터의 이전 활성 상태를 복원합니다.
                Cursor.lockState = m_OriginalCursorLock; Cursor.visible = m_OriginalCursorVisible; // VR 복귀와 씬 종료에는 PC 진입 전의 커서 상태를 복원합니다.
            } // XR Origin 루트의 현재 위치는 어느 전환에서도 되돌리지 않습니다.
            if (!desktop && m_DesktopObservationHUD != null) m_DesktopObservationHUD.SetActive(false); // 헤드셋으로 전환하는 순간 PC 조준 표시와 안내를 숨깁니다.
            SyncVRHands(false); m_Desktop = desktop; m_ChoiceClickBlocked = false; // 모드를 바꿀 때 UI 전용 물체 마스크와 이전 클릭 보호를 원래 상태로 돌립니다.
        } // 스테이지 진행 상태와 완료 기록은 변경하지 않습니다.

        private void Update() // 사물 조사와 선택 UI의 마우스 입력을 현재 표시 상태에 맞춰 구분합니다.
        { // 대사를 화면 클릭으로 넘기는 별도 입력 단계는 없습니다.
            RefreshMode(); if (!m_Desktop) { SyncVRHands(HasVisibleButtons()); return; } m_KeyboardReader.manualValue = Vector2.zero; Mouse mouse = Mouse.current; // VR에서는 머리 추적을 건드리지 않고 기존 양손 레이의 UI 우선순위만 맞춥니다.
            bool focused = Application.isFocused && !m_ApplicationPaused; bool waiting = m_Situation != null && m_Situation.IsWaitingForStartScreen; bool transitioning = m_Situation != null && m_Situation.IsSceneTransitioning; // 제목 화면과 위치 전환 중에는 기존 커서 복구를 유지합니다.
            if (!focused || mouse == null || waiting || transitioning) { EndMouseLook(); SetHovered(null, null); SyncDesktopObservationHUD(false, false, true); return; } // 시작 화면과 포커스·전환 중에는 기존 커서 상태를 복원합니다.
            if (HasVisibleButtons()) // 실제 행동 선택·재시도·마무리 버튼이 보이는 동안에는 커서로 UI를 누릅니다.
            { // 화면에 놓인 버튼을 누르려고 카메라를 돌리지 않게 합니다.
                m_CursorRequested = false; EndMouseLook(); SetHovered(null, null); SyncDesktopObservationHUD(false, false, true); // 시점 회전을 멈추고 기존 UI 입력을 복원하며 뒤쪽 사물의 선택을 보류합니다.
                return; // 같은 좌클릭은 기존 XR UI 입력 모듈에서 실제 버튼에 한 번만 전달합니다.
            } // 읽기 안내만 표시할 때는 기존 중앙 조준으로 사물을 계속 조사합니다.
            Keyboard keyboard = Keyboard.current; if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) m_CursorRequested = true; // 에디터와 운영체제의 마우스 잠금 해제를 존중합니다.
            if (m_CursorRequested && mouse.leftButton.wasPressedThisFrame) m_CursorRequested = false; // 포커스 복귀 후 첫 클릭은 둘러보기 복귀에만 사용합니다.
            if (m_Looking && Cursor.lockState != CursorLockMode.Locked) m_CursorRequested = true; // 외부에서 해제된 잠금을 강제로 즉시 다시 잡지 않습니다.
            if (m_CursorRequested) { EndMouseLook(); SetHovered(null, null); SyncDesktopObservationHUD(false, false, true); return; } // 사용자가 잠금을 해제한 동안에는 세계 입력을 잠시 보류합니다.
            if (!m_Looking) { BeginMouseLook(mouse); return; } UpdateMouseLook(mouse); ApplyViewPose(); // 중앙 판정보다 먼저 이번 프레임의 실제 시선을 반영합니다.
            bool third = m_ThirdSituation != null && m_ThirdSituation.BlocksPlayerTranslation; bool second = m_SecondSituation != null && m_SecondSituation.BlocksPlayerTranslation; // 현재 진행 중인 상황을 우선해 입력 상태를 결정합니다.
            bool observation = third ? m_ThirdSituation.IsObservationReady || m_ThirdSituation.CurrentState == ThirdSituationController.StageState.Choice : second ? m_SecondSituation.IsObservationReady || m_SecondSituation.HasResultUIInput : m_Situation.CurrentState == FirstSituationController.SituationState.Observation || m_Situation.CurrentState == FirstSituationController.SituationState.Choice; // M처럼 조사 설명과 응답 선택 중에도 물체를 살펴볼 수 있습니다.
            observation |= m_StoryUI.HasTask; // 실제 사물을 눌러 진행하는 과제도 중앙 조준을 사용합니다.
            if (observation) ProcessObservationPointer(mouse, m_View.pixelRect.center); else SetHovered(null, null); // 선택 버튼이 없는 조사 단계에서만 세계 물체에 좌클릭을 전달합니다.
            SyncDesktopObservationHUD(observation, observation, false); if (m_Reticle != null) m_Reticle.SetFocused(m_Hovered != null || m_FriendHovered != null); // 조사할 수 있는 실제 사물에만 중앙 조준 표시를 사용합니다.
        } // 빈 화면을 눌러도 대사나 상황 상태는 바뀌지 않습니다.

        private void BuildReticle() // M 원본과 같은 조준 그래픽만 기존 PC HUD 안에 연결합니다.
        { // 이전의 모드 전환 안내와 다른 조준 모양은 겹쳐 표시하지 않습니다.
            foreach (Transform child in m_DesktopObservationHUD.transform) if (child.gameObject != m_DesktopAimMark && !m_DesktopAimMark.transform.IsChildOf(child)) child.gameObject.SetActive(false); // 대사 넘김이나 커서 전환을 설명하던 이전 HUD는 숨깁니다.
            foreach (Graphic graphic in m_DesktopAimMark.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false; // 기존 이미지 대신 M의 작은 원과 점을 표시합니다.
            GameObject mark = new GameObject("M 중앙 조준점", typeof(RectTransform), typeof(CrosswalkReticleGraphic)) { layer = 5 }; mark.transform.SetParent(m_DesktopAimMark.transform, false); m_Reticle = mark.GetComponent<CrosswalkReticleGraphic>(); m_Reticle.raycastTarget = false; // 조준 그래픽 자체는 클릭을 가로채지 않습니다.
            m_Reticle.rectTransform.sizeDelta = new Vector2(24f, 24f); m_Reticle.rectTransform.anchoredPosition = Vector2.zero; // M의 원 반경 5.5~7을 유지할 고정 픽셀 영역입니다.
        } // 기존 HUD Canvas와 표시 생명 주기는 그대로 사용합니다.

        private void ConfigureVRHands() // 기존 양손의 UI 트리거를 물체 선택에도 연결합니다.
        { // 새 레이나 컨트롤러 오브젝트를 만들지 않습니다.
            m_Hands = m_Origin.GetComponentsInChildren<NearFarInteractor>(true); int count = m_Hands.Length; // 현재 Origin의 실제 양손 레이를 찾습니다.
            m_OriginalHandSelect = new XRInputButtonReader[count]; m_HandCasters = new CurveInteractionCaster[count]; m_OriginalHandMasks = new LayerMask[count]; // 바꾸는 선택 입력과 캐스터 설정을 보관할 공간을 준비합니다.
            m_OriginalHandUI = new bool[count]; m_OriginalHandNear = new bool[count]; m_OriginalHandFar = new bool[count]; m_OriginalHandDistances = new float[count]; // 기존 UI·근거리·원거리 설정과 판정 거리를 저장합니다.
            m_HandVisuals = new CurveVisualController[count][]; m_OriginalVisualDistances = new float[count][]; // 실제 표시 레이의 기존 길이를 저장합니다.
            for (int i = 0; i < count; ++i) // 왼손과 오른손을 같은 규칙으로 연결합니다.
            { // 표본 프리팹이나 전역 입력 에셋은 수정하지 않습니다.
                NearFarInteractor hand = m_Hands[i]; m_OriginalHandSelect[i] = hand.selectInput; m_OriginalHandUI[i] = hand.enableUIInteraction; m_OriginalHandNear[i] = hand.enableNearCasting; m_OriginalHandFar[i] = hand.enableFarCasting; // 각 손의 원래 설정을 먼저 보관합니다.
                hand.selectInput = hand.uiPressInput; hand.enableUIInteraction = true; // 기존 검지 트리거로 UI 버튼과 실제 물체를 모두 선택하게 합니다.
                m_HandCasters[i] = hand.farInteractionCaster as CurveInteractionCaster; // 같은 레이 표시와 판정에 연결된 실제 원거리 캐스터를 읽습니다.
                if (m_HandCasters[i] != null) { m_OriginalHandMasks[i] = m_HandCasters[i].raycastMask; m_OriginalHandDistances[i] = m_HandCasters[i].castDistance; m_HandCasters[i].castDistance = Mathf.Max(m_HandCasters[i].castDistance, m_SelectDistance); } // PC와 같은 대상 거리에 양손 레이가 닿게 합니다.
                m_HandVisuals[i] = hand.GetComponentsInChildren<CurveVisualController>(true); m_OriginalVisualDistances[i] = new float[m_HandVisuals[i].Length]; // 각 손의 기존 레이 표시기를 연결합니다.
                for (int j = 0; j < m_HandVisuals[i].Length; ++j) { m_OriginalVisualDistances[i][j] = m_HandVisuals[i][j].maxVisualCurveDistance; m_HandVisuals[i][j].maxVisualCurveDistance = Mathf.Max(m_HandVisuals[i][j].maxVisualCurveDistance, m_SelectDistance); } // 실제 판정 거리에서 레이 그림만 짧게 끊기지 않게 합니다.
            } // 컨트롤러의 위치와 머리 회전은 기존 XR 추적이 담당합니다.
        } // 같은 트리거 입력으로 양손 모두 사용할 수 있습니다.
        private void SyncVRHands(bool buttonsVisible) // 선택지가 떠 있으면 PC와 같이 UI 버튼을 우선 가리키게 합니다.
        { // UI 뒤의 친구 Collider가 보이는 버튼보다 먼저 선택되지 않도록 합니다.
            if (m_Hands == null) return; // 양손 연결 전에는 기존 초기화를 기다립니다.
            for (int i = 0; i < m_Hands.Length; ++i) // 실제 양손의 같은 레이에 표시와 판정 우선순위를 적용합니다.
            { // UI 레이어도 판정 마스크에 남겨 실제 버튼 사각형을 가리키게 합니다.
                if (m_Hands[i] == null) continue; m_Hands[i].enableNearCasting = buttonsVisible ? false : m_OriginalHandNear[i]; m_Hands[i].enableFarCasting = buttonsVisible || m_OriginalHandFar[i]; // 버튼 선택 중에는 손 주변 물체보다 같은 원거리 UI 레이를 사용합니다.
                if (m_HandCasters[i] != null) m_HandCasters[i].raycastMask = buttonsVisible ? (LayerMask)(1 << 5) : m_OriginalHandMasks[i]; // 선택지가 닫히면 같은 레이가 기존 친구·소품 판정으로 돌아갑니다.
            } // 레이를 추가하거나 화면과 다른 가상 버튼 판정을 만들지 않습니다.
        } // 실제 GraphicRaycaster가 반환한 버튼 위치를 그대로 사용합니다.
        private void RestoreVRHands() // 이 씬을 비활성화하면 연결했던 양손 설정을 복원합니다.
        { // 통합된 다른 파트의 입력 설정에 이 씬의 선택 방식을 남기지 않습니다.
            if (m_Hands == null) return; // 아직 연결하지 않은 경우 복원할 설정이 없습니다.
            for (int i = 0; i < m_Hands.Length; ++i) // 살아 있는 기존 레이만 복원합니다.
            { // 파괴된 컨트롤러를 다시 찾거나 만들지 않습니다.
                if (m_Hands[i] == null) continue; m_Hands[i].selectInput = m_OriginalHandSelect[i]; m_Hands[i].enableUIInteraction = m_OriginalHandUI[i]; m_Hands[i].enableNearCasting = m_OriginalHandNear[i]; m_Hands[i].enableFarCasting = m_OriginalHandFar[i]; // 각 손의 원래 입력과 상호작용 방식을 돌립니다.
                if (m_HandCasters[i] != null) { m_HandCasters[i].raycastMask = m_OriginalHandMasks[i]; m_HandCasters[i].castDistance = m_OriginalHandDistances[i]; } // 물체 판정 범위와 레이 거리를 함께 복원합니다.
                for (int j = 0; j < m_HandVisuals[i].Length; ++j) if (m_HandVisuals[i][j] != null) m_HandVisuals[i][j].maxVisualCurveDistance = m_OriginalVisualDistances[i][j]; // 표시 레이도 기존 거리로 복원합니다.
            } // 원본 프리팹과 입력 에셋은 변경하지 않습니다.
        } // 같은 Origin을 쓰는 다음 파트의 조작을 보존합니다.

        public void RequireFreshChoiceClick() // 세 상황의 선택지가 열리면 이전 행동의 클릭을 새 선택지와 분리합니다.
        { // 버튼 위치와 기존 커서 조작을 바꾸지 않고 입력을 받는 시점만 구분합니다.
            m_ChoiceClickBlocked = true; m_ChoiceInputReadyAt = Time.unscaledTime + 0.3f; // 선택지 등장 직후의 연속 클릭을 막고 클릭을 놓을 때까지 보호 상태를 유지합니다.
        } // 실제 입력 허용 여부는 UI 모듈이 최종 클릭 대상을 정할 때 판단합니다.
        private void FilterOpeningChoiceRaycasts(PointerEventData eventData, List<RaycastResult> results) // 같은 PC·VR UI 모듈에서 선택지를 연 입력이 새 버튼으로 이어지지 않게 합니다.
        { // 모듈이 누름과 해제를 계속 처리하도록 하면서 보호 중에는 클릭 대상만 비웁니다.
            if (!m_ChoiceClickBlocked) return; bool held = false; // 새 선택지가 열릴 때만 이전 입력의 해제를 기다립니다.
            if (m_Desktop) { Mouse mouse = Mouse.current; held = mouse == null || mouse.leftButton.isPressed || mouse.leftButton.wasReleasedThisFrame; } // PC는 실제 마우스를 놓은 프레임까지 보호합니다.
            else if (m_Hands != null) foreach (NearFarInteractor hand in m_Hands) if (hand != null && (hand.uiPressInput.ReadIsPerformed() || hand.uiPressInput.ReadWasPerformedThisFrame())) { held = true; break; } // VR은 양손 트리거가 모두 해제되어야 다음 선택을 받습니다.
            if (Time.unscaledTime >= m_ChoiceInputReadyAt && !held) { m_ChoiceClickBlocked = false; return; } // 같은 0.3초 보호 시간 뒤 새 클릭이나 트리거 누름부터 받습니다.
            results.Clear(); // 이전 누름의 해제가 다음 버튼의 클릭으로 처리되지 않게 실제 UI 판정 결과만 비웁니다.
        } // 보호가 끝나면 기존 UI 모듈과 버튼 연결을 그대로 사용합니다.
        private bool HasVisibleButtons() // 실제로 표시 요청된 패널에 선택 버튼이 있는지 확인합니다.
        { // 숨겨진 버튼과 퇴장 중인 이전 선택지는 커서 모드의 기준으로 사용하지 않습니다.
            foreach (Button button in m_Buttons) // 기존 씬에서 수집한 세 상황의 버튼 참조를 사용합니다.
            { // 화면 전환을 위해 새 버튼이나 별도 입력 모듈을 만들지 않습니다.
                if (button == null || !button.IsActive()) continue; // 꺼졌거나 제거된 버튼은 입력 모드에 영향을 주지 않습니다.
                FirstSituationPanel panel = button.GetComponentInParent<FirstSituationPanel>(); // 버튼이 실제로 속한 현재 패널을 찾습니다.
                if (panel != null && panel.WantsVisible) return true; // 표시 중인 선택·재시도·마무리 패널만 기존 마우스 UI 입력을 사용합니다.
            } // 읽기 전용 설명과 사물 조사 단계는 이 조건에 포함하지 않습니다.
            return false; // 선택할 버튼이 없으면 기존 중앙 조준으로 복귀합니다.
        } // 실제 클릭 판정과 Button.onClick 호출은 기존 XR UI 입력 모듈에 맡깁니다.
        private void SyncDesktopObservationHUD(bool show, bool observation, bool cursorMode) // M처럼 선택 가능한 장면에서 중앙 조준점만 표시합니다.
        { // 커서나 대사 넘김 안내를 별도로 띄우지 않습니다.
            if (m_DesktopObservationHUD != null && m_DesktopObservationHUD.activeSelf != show) m_DesktopObservationHUD.SetActive(show); bool aim = show && !cursorMode; if (m_DesktopAimMark != null && m_DesktopAimMark.activeSelf != aim) m_DesktopAimMark.SetActive(aim); // 제목과 자동 연출 중에는 조준점을 숨기고 조사·응답 선택에서만 엽니다.
        } // 기존 HUD Canvas를 재사용하므로 추가 창은 없습니다.
        private void BeginMouseLook(Mouse mouse) // 잠긴 상대 마우스 이동량을 사용하도록 기존 XR UI 입력을 잠시 보류합니다.
        { // 시점 회전과 마우스 UI 클릭이 한 프레임에 중복 실행되지 않게 합니다.
            if (m_Looking || mouse == null) return; // 이미 둘러보는 중에는 잠금 상태를 다시 만들지 않습니다.
            m_CursorBeforeLook = mouse.position.ReadValue(); m_Looking = true; // 커서 모드로 돌아갈 때 복원할 위치와 회전 상태를 보관합니다.
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; // 실제 커서를 잠그고 마우스 이동량으로 무제한 회전할 수 있게 합니다.
            m_UIWasEnabledBeforeLook = m_UIInput.enableMouseInput; // 모듈 활성화 상태와 별개로 마우스 입력 설정을 보관합니다.
            m_UIInput.enableMouseInput = false; // 중앙 조준 클릭이 월드 UI에도 동시에 전달되지 않게 합니다.
            SetHovered(null, null); // 이전 커서 조준 표시를 정리합니다.
        } // 말풍선과 버튼 오브젝트 자체는 변경하지 않습니다.

        private void EndMouseLook() // 선택 버튼 표시·전환·포커스 손실에서 기존 커서 상태를 복원합니다.
        { // 헤드셋 모드의 커서 상태는 변경하지 않습니다.
            if (!m_Desktop) return; // VR의 기존 입력 설정을 보존합니다.
            bool restore = m_Looking; m_Looking = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // 상대 입력과 커서 잠금을 함께 끝냅니다.
            if (restore && m_UIInput != null) m_UIInput.enableMouseInput = m_UIWasEnabledBeforeLook; // EventSystem 등록을 건드리지 않고 마우스 클릭을 다시 받게 합니다.
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

        private void ProcessObservationPointer(Mouse mouse, Vector2 position) // 실제 포인터 위치에서 주변 물체를 조사하거나 친구에게 말을 겁니다.
        { // 비대화형 안내 대신 실제 월드 Collider를 클릭 대상으로 사용합니다.
            PickWorldTarget(position, out FirstSituationObservation target, out FriendStopInteraction friend, out Vector3 hitPosition); // 전달받은 커서 좌표의 광선으로 실제 선택한 물체나 친구를 찾습니다.
            SetHovered(target, friend); // 조준한 대상의 기존 외곽선과 안내를 갱신합니다.
            if (!mouse.leftButton.wasPressedThisFrame) return; // 조준만 한 상태에서는 대화나 조사 결과를 열지 않습니다.
            if (target == null) // 관찰 목록에 없는 친구는 기존 멈춤 명령으로 처리합니다.
            { // 조준 대상에 남아 있는 기존 멈춤 상호작용을 사용합니다.
                if (friend != null) friend.SendPointerCommand(m_View); // 해당 대상을 향한 기존 명령을 전달합니다.
                return; // 관찰 대상이 없는 클릭은 여기서 끝납니다.
            } // 등록된 관찰 대상은 단서 조사 또는 대화로 이어집니다.
            if (!target.Activate(hitPosition)) return; // 주변 물체를 조사하면 중앙 조준과 자유 시점을 유지합니다.
            SetHovered(null, null); // M처럼 물체 선택 후에도 중앙 조준과 자유 시점을 유지하며 응답 버튼을 직접 가리킬 수 있게 합니다.
            SyncDesktopObservationHUD(true, true, false); // 실제 친구 대화의 응답도 같은 중앙 조준으로 선택합니다.
        } // 같은 클릭은 현재 친구의 대화 진입에만 사용합니다.

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
            m_CursorRequested = true; EndMouseLook(); m_KeyboardReader.manualValue = Vector2.zero; SetHovered(null, null); // 포커스를 다시 얻을 때까지 커서와 현재 시점을 유지합니다.
        } // 버튼 입력은 포커스를 얻은 뒤 해당 단계의 공통 분기에서 처리합니다.
        private void OnApplicationPause(bool paused) { m_ApplicationPaused = paused; if (paused) { m_CursorRequested = true; EndMouseLook(); SetHovered(null, null); } } // 앱 일시정지 중에 마우스 잠금과 관찰 강조가 남지 않게 합니다.
        private void OnDisable() { if (m_UIInput != null) m_UIInput.finalizeRaycastResults -= FilterOpeningChoiceRaycasts; m_ChoiceClickBlocked = false; EndMouseLook(); SetDesktopMode(false); RestoreVRHands(); m_KeyboardMove?.Disable(); } // 클릭 필터를 해제하고 양손 레이·기존 XR 추적·커서·키보드 상태를 복원합니다.
        private void OnDestroy() { m_KeyboardMove?.Dispose(); m_KeyboardReader?.inputAction?.Dispose(); } // 이 컴포넌트가 만든 씬 전용 입력 리소스만 해제합니다.
    } // 기존 VR 스테이지의 PC 조작 연결을 마칩니다.
} // 프로젝트의 공통 입력 자산과 다른 씬은 변경하지 않습니다.
