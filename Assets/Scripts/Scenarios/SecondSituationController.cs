using System.Collections; // 완료 후 보행·암전·도착·호출·첫걸음을 순서대로 연결합니다.
using Unity.XR.CoreUtils; // 기존 XR Origin 루트의 위치만 암전 중 배치합니다.
using UnityEngine; // 실제 씬의 캐릭터·교통·월드 공간 UI를 연결합니다.
using UnityEngine.Events; // 관찰을 마친 사실만 이후 행동 선택 단계에 전달합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors; // 기존 양손 Near-Far 컨트롤러의 관찰 레이를 찾습니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters; // 기존 레이 판정 거리를 건너편 단서까지 확보합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals; // 기존 레이 표시 거리도 실제 판정 범위와 일치시킵니다.

namespace Y.Crosswalk // 두 번째 상황의 도입·관찰·행동 선택과 빨간불 결과를 담당합니다.
{ // 기존 도입을 유지하며 세 번째 신호 상황은 시작하지 않습니다.
    [DefaultExecutionOrder(450)] // 기존 캐릭터와 차량의 최종 자세가 적용된 뒤 관찰 장면을 정지합니다.
    public sealed class SecondSituationController : MonoBehaviour // 첫 번째 상황의 실제 완료 이벤트에서만 시작합니다.
    { // 현재 프로젝트의 실제 구성요소를 Inspector에서 연결합니다.
        public enum StageState { WaitingForFirst, FirstResult, Walking, Transition, Arriving, Calling, Attempt, Observation, ActionSelectionReady, SafeResult, UnsafeResult, Retry, Complete } // 기존 도입·관찰 값은 유지하고 선택 결과·재시도·안전 완료를 구분합니다.
        [SerializeField, InspectorName("첫 번째 상황 완료"), Tooltip("완료 이벤트를 이 컴포넌트의 BeginAfterFirstSituation에 연결합니다.")] private FirstSituationController m_First; // 안전한 완료가 아닌 충돌·재시도로는 시작하지 않습니다.
        [SerializeField, InspectorName("기존 전환·생각 UI"), Tooltip("기존 암전과 관찰 말풍선을 제공하는 첫 번째 상황 UI입니다.")] private FirstSituationUI m_SharedUI; // 같은 한국어 글꼴과 부드러운 전환 자산을 재사용합니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("헤드셋 추적과 컨트롤러가 연결된 기존 Origin입니다.")] private XROrigin m_Origin; // 실제 카메라의 로컬 자세와 방향은 변경하지 않습니다.
        [SerializeField, InspectorName("친구 1 이동"), Tooltip("플레이어 곁에 도착할 기존 친구 1 이동입니다.")] private FriendCharacterMovement m_Friend1; // 전봇대 뒤의 기존 보행을 재사용합니다.
        [SerializeField, InspectorName("친구 1 자세 제어"), Tooltip("기존 친구 1의 개별 정지·재개 컴포넌트입니다.")] private FirstSituationCharacterPerformance m_Friend1Performance; // 전화 사용을 중단한 안전 결과 자세를 유지합니다.
        [SerializeField, InspectorName("친구 2 이동"), Tooltip("건너편 친구 2의 기존 이동입니다.")] private FriendCharacterMovement m_Friend2; // 기존 첫 발 디딤을 인도 위에서만 재사용합니다.
        [SerializeField, InspectorName("친구 2 호출 연출"), Tooltip("친구 2의 호출 말풍선과 함께 보여 줄 손짓·미소입니다.")] private SecondSituationFriendGreeting m_Greeting; // 이동과 별도로 호출 표현을 진행합니다.
        [SerializeField, InspectorName("기존 보행 신호"), Tooltip("현재 횡단보도에서 차량도 참조하는 신호등입니다.")] private PedestrianTrafficLight m_Signal; // 실제 빨간 신호 상태와 타이머를 함께 유지합니다.
        [SerializeField, InspectorName("빨간 표시 위치"), Tooltip("기존 빨간 사람 표시의 Transform입니다. 짧은 첫 발 자세의 기존 참조로만 사용합니다.")] private Transform m_RedSignalPoint; // 관찰 정지 전에 신호를 바라보는 후속 반응으로 넘어가지 않게 합니다.
        [SerializeField, InspectorName("관련 차량"), Tooltip("현재 횡단보도에서 주행하는 기존 차량들입니다.")] private TrafficCarMovement[] m_Cars; // 관찰 중 이 차량들의 속도와 현재 위치만 보존합니다.
        [SerializeField, InspectorName("차량 도입 배치 지점"), Tooltip("차량 배열과 같은 순서의 기존 차로 위 시작점입니다. 암전 중에만 배치합니다.")] private Transform[] m_CarEntries; // 관찰 때 횡단보도 근처로 접근하는 주행 장면을 구성합니다.
        [SerializeField, InspectorName("도입 중 보류할 기존 명령"), Tooltip("친구 2의 기존 멈춤 입력과 횡단 판단 컴포넌트입니다. 삭제하지 않고 도입 중에만 보류합니다.")] private Behaviour[] m_LegacyCommands; // 새 선택 단계가 없는 이번 도입에서 이전 횡단 명령이 먼저 실행되지 않게 합니다.
        [SerializeField, InspectorName("보류할 직접 이동"), Tooltip("기존 연속 이동·텔레포트 Provider만 연결하며 머리 추적과 컨트롤러 레이는 제외합니다.")] private Behaviour[] m_Locomotion; // 직접 조작 없이 진행하되 VR 입력과 시선은 유지합니다.
        [SerializeField, InspectorName("재사용할 관찰 대상"), Tooltip("친구 2·보행 신호·차량·횡단보도의 개별 관찰 컴포넌트입니다. 첫 번째 상황의 대상은 포함하지 않습니다.")] private FirstSituationObservation[] m_Observations; // 기존 조준·선택·테두리와 공유 생각 말풍선을 두 번째 상황의 네 단서에 연결합니다.
        [SerializeField, Min(12f), InspectorName("관찰 레이 거리(m)"), Tooltip("기존 양손 Near-Far 레이의 판정과 표시 범위를 기본 30m까지 확보합니다. 현재 더 긴 설정은 줄이지 않습니다.")] private float m_ObservationDistance = 30f; // 반대편 친구와 보행 신호를 같은 인도에서 조사할 수 있게 합니다.
        [SerializeField, InspectorName("관찰 완료 후 연결"), Tooltip("관찰에서 행동 선택 상태가 되었을 때 한 번 호출합니다. 이번 씬에서는 추가 이벤트 없이 자체 선택 패널을 표시합니다.")] private UnityEvent m_OnObservationFinished = new UnityEvent(); // 관찰 완료 사실을 알리되 행동 선택 전에는 결과를 자동 실행하지 않습니다.
        [SerializeField, InspectorName("첫 번째 전봇대"), Tooltip("장소 전환 후 숨길 기존 전봇대 인스턴스입니다.")] private GameObject m_FirstPole; // 두 번째 장소에 첫 번째 위험 오브젝트가 남지 않게 합니다.
        [SerializeField, InspectorName("함께 걷기 촬영 카메라"), Tooltip("기존 RenderTexture 카메라를 1인칭 동행 보행에만 재사용합니다. 얼굴 클로즈업은 하지 않습니다.")] private Camera m_WalkCamera; // 실제 XR 카메라를 강제로 전진시키지 않습니다.
        [SerializeField, InspectorName("기존 보행 환경·발소리"), Tooltip("친구 1의 실제 걸음 위상에 연결된 기존 소리입니다.")] private FirstSituationWalkAudio m_WalkAudio; // 전환 전후 실제 보행에서만 소리를 냅니다.
        [SerializeField, InspectorName("전봇대 이후 보행 지점"), Tooltip("첫 장소에서 함께 걷는 보행의 기존 인도 위 도착점입니다.")] private Transform m_LeavePoint; // 횡단보도와 충분히 떨어진 첫 장소를 유지합니다.
        [SerializeField, InspectorName("플레이어 도착 위치"), Tooltip("암전 중 Origin 루트 위치만 놓을 횡단보도 앞 인도 지점입니다.")] private Transform m_PlayerPoint; // 기존 루트 회전과 헤드셋 자세를 보존합니다.
        [SerializeField, InspectorName("친구 1 도착 보행 시작"), Tooltip("친구 1이 플레이어 곁으로 걸어갈 인도 시작점입니다.")] private Transform m_Friend1Entry; // 두 번째 장소에서 짧은 도착 보행을 보여 줍니다.
        [SerializeField, InspectorName("친구 1 대기 위치"), Tooltip("플레이어 옆에서 친구 2를 바라볼 안전한 인도 지점입니다.")] private Transform m_Friend1Arrival; // 도로와 횡단보도를 가리지 않게 배치합니다.
        [SerializeField, InspectorName("친구 2 건너편 대기 위치"), Tooltip("기존 건너편 인도 시작점입니다.")] private Transform m_Friend2Waiting; // 호출과 첫 발 자세 모두 인도 안에서 시작합니다.
        [SerializeField, InspectorName("두 번째 상황 월드 Canvas"), Tooltip("씬에 저장한 보행 화면과 호출 말풍선의 Canvas입니다.")] private Transform m_Canvas; // 첫 번째 상황의 안내 패널과 분리된 실제 UI를 사용합니다.
        [SerializeField, InspectorName("함께 걷기 화면"), Tooltip("씬에 배치한 1인칭 동행 영상 패널입니다.")] private FirstSituationPanel m_WalkPanel; // 기존 파스텔 이미지와 등장 효과를 재사용합니다.
        [SerializeField, InspectorName("야 여기야 말풍선"), Tooltip("씬에 배치하고 한국어 글꼴을 연결한 호출 대사입니다.")] private FirstSituationPanel m_CallPanel; // 문구와 RectTransform은 Hierarchy에서 직접 편집합니다.
        [SerializeField, InspectorName("호출 말풍선 위치 간격(m)"), Tooltip("친구 2 머리에서 말풍선까지의 월드 간격입니다. 호출 시작 시 한 번 배치하며 이후 RectTransform 편집을 덮어쓰지 않습니다.")] private Vector3 m_CallOffset = new Vector3(-1.2f, 0.85f, 0f); // 얼굴·손짓·신호를 가리지 않는 말풍선 위치를 Inspector에서 조절합니다.
        [SerializeField, Min(0.5f), InspectorName("첫 안전 결과 표시 시간"), Tooltip("첫 상황의 기존 안전 결과를 읽은 뒤 이동을 시작할 시간입니다.")] private float m_ResultHold = 1.5f; // 기존 안전한 결과 설명을 생략하지 않습니다.
        [SerializeField, Min(2f), InspectorName("함께 걷는 시간"), Tooltip("친구가 다시 걷기 시작한 뒤 보여 줄 동행 보행 시간입니다.")] private float m_WalkDuration = 3.5f; // 직접 이동 입력을 요구하지 않는 보행 맥락을 만듭니다.
        [SerializeField, Range(0.25f, 0.8f), InspectorName("장소 이동 암전 시간"), Tooltip("다음 장소로 배치할 때만 쓰는 부드러운 암전 시간입니다.")] private float m_TransitionDuration = 0.4f; // 첫 번째 상황의 기존 페이드 설정은 변경하지 않습니다.
        [SerializeField, Range(0.08f, 0.16f), InspectorName("첫 발 자세 정지 시점"), Tooltip("기존 0.2초 첫 발 반응이 끝나기 전에 정지할 시각입니다. 루트는 도로로 전진하지 않습니다.")] private float m_FirstStepTime = 0.14f; // 친구가 도로 한가운데까지 걸어가는 상태를 만들지 않습니다.
        [SerializeField, InspectorName("현재 진행 단계"), Tooltip("첫 완료 대기·도입·관찰·행동 선택 진입 대기를 구분합니다.")] private StageState m_State; // 이후 진행 시스템은 관찰과 선택 준비 여부를 구분하여 참조합니다.
        private bool[] m_LegacyEnabled, m_LocomotionEnabled, m_CarsWerePaused; // 이 도입이 보류한 기존 구성의 상태를 각각 보관합니다.
        private bool m_Initialized, m_Started, m_QueueFreeze, m_Friend1WasPaused, m_Friend2WasPaused, m_SignalWasPaused, m_PoleWasActive; // 시작 중복과 취소·복원 상태를 관리합니다.
        private Vector3 m_WalkForward; // 동행 영상은 친구의 몸 회전과 무관한 같은 인도 방향을 유지합니다.
        [SerializeField, InspectorName("두 번째 선택·결과 UI"), Tooltip("실제 씬에 배치한 파스텔 선택·대사·결과·재시도 패널입니다.")] private SecondSituationUI m_ResultUI; // 첫 번째 상황의 선택지와 결과 패널은 변경하지 않습니다.
        [SerializeField, InspectorName("친구 2 결과 동작"), Tooltip("친구 2 관절의 놀람·뒷걸음·감사와 관찰 자세 저장을 담당합니다.")] private SecondSituationCharacterPerformance m_Performance; // 기존 호출과 일반 보행을 결과 표현에 연결합니다.
        [SerializeField, InspectorName("친구 2 보행 소리"), Tooltip("기존 발소리·환경음 방식을 친구 2에 연결한 인스턴스입니다.")] private FirstSituationWalkAudio m_FriendWalkAudio; // 친구 1의 소리 참조와 별개로 유지합니다.
        [SerializeField, InspectorName("안전한 인도 안쪽 위치"), Tooltip("친구 2가 빨간불을 확인하고 물러날 건너편 인도 지점입니다.")] private Transform m_RetreatPoint; // 도로 쪽이 아닌 기존 인도 안쪽으로만 물러납니다.
        [SerializeField, InspectorName("차량 앞 횡단 제한 위치"), Tooltip("친구가 기존 횡단 경로에서 더 진행하지 않을 도로 위 지점입니다.")] private Transform m_NearMissPoint; // 사고 연출 중 친구가 차로를 계속 가로질러 가지 않게 제한합니다.
        [SerializeField, InspectorName("친구 2 몸 Collider"), Tooltip("기존 친구 2의 관찰·몸 Collider입니다.")] private Collider m_FriendBody; // 정지 거리를 실제 친구의 크기로 계산합니다.
        [SerializeField, InspectorName("기존 차량 몸 Collider"), Tooltip("기존 자동차 전체를 감싸는 관찰 Collider입니다.")] private Collider m_CarBody; // 앞범퍼를 포함하는 안전 여유를 계산합니다.
        [SerializeField, Min(0.3f), InspectorName("차량과 친구의 최소 간격(m)"), Tooltip("차체 Collider와 친구 Collider 표면 사이에 남길 여유 거리입니다.")] private float m_CarClearance = 0.4f; // 실제 차체가 친구를 관통하거나 겹치지 않게 합니다.
        [SerializeField, InspectorName("차량 경적"), Tooltip("기존 자동차 아래에 배치한 경적 AudioSource입니다.")] private AudioSource m_HornAudio; // 급정거 직전에 한 번만 울립니다.
        [SerializeField, InspectorName("급정거 소리"), Tooltip("기존 자동차의 짧은 제동 소리입니다.")] private AudioSource m_BrakeAudio; // 속도가 급히 줄어드는 순간을 전달합니다.
        [SerializeField, InspectorName("타이어 마찰음"), Tooltip("기존 자동차에 배치한 타이어 마찰 AudioSource입니다.")] private AudioSource m_TireAudio; // 실제 제동 구간에만 재생합니다.
        [SerializeField, InspectorName("복원 전환 소리"), Tooltip("기존 짧고 부드러운 전환 음원을 재사용하는 AudioSource입니다.")] private AudioSource m_ResetAudio; // 사고 설명 후 화면 전환에만 사용합니다.
        [SerializeField, InspectorName("제동 자국 기준"), Tooltip("씬에 미리 배치한 두 타이어 자국의 기준 Transform입니다.")] private Transform m_BrakeMarks; // 런타임에 시각 효과 오브젝트를 생성하지 않습니다.
        [SerializeField, InspectorName("양쪽 타이어 자국"), Tooltip("얇은 왼쪽·오른쪽 제동 자국 Transform입니다.")] private Transform[] m_TireMarks; // 자동차가 제동한 실제 거리만큼만 길이를 늘립니다.
        [SerializeField, Min(2f), InspectorName("위험 원인 표시 시간"), Tooltip("자동 복원 전 설명을 보여 줄 시간입니다. 재시도 버튼으로 먼저 돌아갈 수 있습니다.")] private float m_ResultDuration = 4f; // 관찰이나 행동 선택에는 시간 제한을 두지 않습니다.
        [SerializeField, InspectorName("두 번째 안전 행동 완료"), Tooltip("친구 2가 인도에서 기다리고 감사한 뒤에만 기록됩니다.")] private bool m_Completed; // 첫 번째 상황의 완료 기록과 분리된 상태입니다.
        [SerializeField, InspectorName("두 번째 안전 행동 완료 이벤트"), Tooltip("안전한 기다리기와 감사가 끝났을 때 한 번 호출하여 세 번째 상황을 연결합니다.")] private UnityEvent m_OnCompleted = new UnityEvent(); // 외부 진행 시스템이 안전 완료만 참조할 수 있게 합니다.
        [SerializeField, InspectorName("기존 PC 입력"), Tooltip("암전 중 횡단보도 도착 지점에 맞춰 PC 관찰 방향을 설정합니다.")] private CrosswalkDesktopInput m_DesktopInput; // 머리 추적이나 실제 XR Origin 회전은 바꾸지 않습니다.
        private bool m_CheckpointReady, m_RetryRequested, m_BrakeEffectActive;
        private Coroutine m_SlowMotionReset; // 실패 복원 뒤 이전 느린 동작 코루틴이 다음 결과를 덮어쓰지 않게 보관합니다. // 관찰 저장과 중복 재시도·제동 효과를 관리합니다.
        private Vector3 m_BrakeStart; // 타이어 자국이 시작된 실제 차량 위치입니다.
        public StageState CurrentState => m_State; // PC 입력이 실제 단계 전환을 감지하여 이전 커서 잠금을 해제하도록 제공합니다.
        public bool IsCompleted => m_Completed; // 안전 행동을 완료한 경우에만 참조할 수 있습니다.
        public bool HasResultUIInput => isActiveAndEnabled && (m_State == StageState.ActionSelectionReady || m_State == StageState.Retry); // 기존 마우스 입력도 두 번째 선택·재시도 버튼을 클릭하게 합니다.
        public bool BlocksPlayerTranslation => isActiveAndEnabled && m_Started; // 첫 상황 완료 후에도 이 도입과 관찰에서는 직접 이동을 보류합니다.
        public bool AllowsLooking => m_Started && (m_State == StageState.Arriving || m_State == StageState.Calling || m_State == StageState.Attempt || m_State == StageState.Observation || m_State == StageState.ActionSelectionReady || m_State == StageState.SafeResult || m_State == StageState.UnsafeResult || m_State == StageState.Retry || m_State == StageState.Complete); // 도착 이후에는 결과를 포함해 기존 마우스로 자유롭게 둘러볼 수 있습니다.
        public bool IsObservationReady => isActiveAndEnabled && m_State == StageState.Observation; // 정지된 관찰 단계에서만 기존 마우스 단서 선택을 허용합니다.
        public bool IsActionSelectionReady => isActiveAndEnabled && m_State == StageState.ActionSelectionReady; // 다음 진행 시스템이 결과 실행 없이 관찰 완료 상태를 참조할 수 있게 합니다.

        private void Awake() // 기존 친구 2 명령이 도입보다 먼저 실행되지 않게 준비합니다.
        { // 기존 명령 스크립트 자체는 제거하거나 재작성하지 않습니다.
            m_LegacyEnabled = new bool[m_LegacyCommands.Length]; // 원래 명령 컴포넌트의 활성 상태를 저장합니다.
            for (int i = 0; i < m_LegacyCommands.Length; ++i) { m_LegacyEnabled[i] = m_LegacyCommands[i].enabled; m_LegacyCommands[i].enabled = false; } // 초기 배치와 호출 도입 동안만 기존 명령 입력을 보류합니다.
        } // 다른 교통과 첫 번째 상황은 계속 작동합니다.

        private void Start() // 모든 실제 씬 패널과 기존 이동기가 준비된 뒤 완료 상태를 확인합니다.
        { // 미완료 첫 상황은 자동으로 건너뛰지 않습니다.
            m_Initialized = true; m_WalkPanel.Show(false); m_CallPanel.Show(false); SetObservations(false); // 두 번째 상황의 화면과 단서 입력을 처음에는 숨깁니다.
            if (m_First.IsCompleted) BeginAfterFirstSituation(); // 저장된 완료 상태로 시작한 경우에도 같은 진입점을 사용합니다.
        } // 일반 시작은 첫 번째 상황의 실제 완료 이벤트를 기다립니다.

        public void BeginAfterFirstSituation() // 첫 번째 상황 완료 이벤트에서 호출하는 유일한 도입 진입점입니다.
        { // 수동 호출이나 중복 이벤트로 이전 상황을 건너뛰지 않습니다.
            if (!isActiveAndEnabled || !m_First.IsCompleted || m_Started || m_Completed) return; // 실제 안전 완료와 아직 시작하지 않은 도입만 수락합니다.
            m_Started = true; m_State = StageState.FirstResult; // 다음 입력 프레임보다 먼저 두 번째 상황의 이동 보류 상태를 확정합니다.
            m_Friend1WasPaused = m_Friend1.IsScenarioPaused; m_Friend2WasPaused = m_Friend2.IsScenarioPaused; m_SignalWasPaused = m_Signal.IsScenarioPaused; m_PoleWasActive = m_FirstPole.activeSelf; // 상황 전의 실제 개별 정지와 전봇대 상태를 보관합니다.
            m_LocomotionEnabled = new bool[m_Locomotion.Length]; m_CarsWerePaused = new bool[m_Cars.Length]; // 취소 시 원래 이동과 차량 정지를 복원할 공간을 준비합니다.
            for (int i = 0; i < m_Locomotion.Length; ++i) { m_LocomotionEnabled[i] = m_Locomotion[i].enabled; m_Locomotion[i].enabled = false; } // 기존 직접 이동만 보류하고 추적·컨트롤러 레이는 유지합니다.
            for (int i = 0; i < m_Cars.Length; ++i) m_CarsWerePaused[i] = m_Cars[i].IsScenarioPaused; // 차량의 기존 상태를 별도로 보관합니다.
            StartCoroutine(RunIntroduction()); // 완료 설명 뒤의 보행과 장소 전환을 순서대로 시작합니다.
        } // 이 진입점은 행동 선택이나 횡단 결과를 실행하지 않습니다.

        private IEnumerator RunIntroduction() // 함께 걷고 횡단보도에 도착한 뒤 친구 2의 호출로 관찰을 시작합니다.
        { // 전역 시간 배율이나 실제 VR 카메라 방향은 바꾸지 않습니다.
            yield return new WaitForSecondsRealtime(m_ResultHold); // 기존 안전 결과 설명을 먼저 보여 줍니다.
            m_SharedUI.HideAll(); m_Friend1Performance.Freeze(false); m_Friend1.SetScenarioApproachSpeed(1.1f); // 첫 상황 UI를 숨기고 전화 사용을 멈춘 친구의 기존 보행을 재개합니다.
            m_WalkForward = Vector3.ProjectOnPlane(m_LeavePoint.position - m_Friend1.transform.position, Vector3.up).normalized; // 영상 속 동행 방향을 인도 진행 방향으로 고정합니다.
            if (!m_Friend1.TryBeginScenarioDetour(m_LeavePoint)) { enabled = false; yield break; } // 기존 보행이 시작되지 않으면 입력 잠금을 남기지 않고 도입을 취소합니다.
            PlaceWalkingScreen(); m_WalkPanel.Show(true); PositionWalkingCamera(); m_WalkCamera.enabled = true; m_WalkAudio.SetActive(true); m_State = StageState.Walking; // 실제 씬에 배치한 동행 화면과 기존 보행 소리를 함께 시작합니다.
            float elapsed = 0f; // 실제 동행 보행을 보여 준 길이를 관리합니다.
            while (elapsed < m_WalkDuration && m_Friend1.IsScenarioDetouring) { elapsed += Time.unscaledDeltaTime; yield return null; } // XR 강제 전진 없이 별도 1인칭 보행 화면으로 함께 걷는 맥락을 보여 줍니다.
            m_State = StageState.Transition; m_SharedUI.Transition(true, m_TransitionDuration); // 장소가 바뀌는 동안만 기존 전체 시야 전환막을 사용합니다.
            yield return new WaitForSecondsRealtime(m_SharedUI.FadeCoverDuration); // 머리 추적을 유지한 채 완전한 암전이 될 때까지 기다립니다.
            m_WalkCamera.enabled = false; m_WalkPanel.Show(false); m_WalkAudio.Stop(); m_Friend1Performance.Freeze(true); // 가려진 동안 첫 장소의 영상과 움직임을 정리합니다.
            m_Origin.Origin.transform.position = m_PlayerPoint.position; m_DesktopInput.SetScenarioView(m_PlayerPoint); m_FirstPole.SetActive(false); // 장소 전환의 암전 중 위치와 PC 관찰 방향을 함께 맞춥니다. // Origin 위치만 횡단보도 앞에 놓고 이전 전봇대는 숨깁니다.
            m_Friend1.PlaceWaitingForScenario(m_Friend1Entry); m_Friend2.PlaceWaitingForScenario(m_Friend2Waiting); m_Friend1Performance.Freeze(false); m_Friend2.SetScenarioPaused(false); // 두 친구의 경로 상태를 각 인도 지점에서 따로 준비합니다.
            m_Signal.HoldRedForScenario(); // 기존 신호의 빨간 표시와 자동 전환 보류를 한 번에 적용합니다.
            for (int i = 0; i < m_Cars.Length; ++i) { m_Cars[i].PlaceForScenario(m_CarEntries[i].position); m_Cars[i].SetScenarioPaused(false); } // 같은 차로의 실제 누적 거리에서 기존 빨간불 주행을 이어갑니다.
            if (!m_Friend1.TryBeginScenarioDetour(m_Friend1Arrival)) Debug.LogWarning("친구 1의 짧은 도착 보행이 수락되지 않았습니다. 현재 인도 위치를 유지하고 친구 2의 호출을 진행합니다.", this); // 동행 친구의 보행 거절로 전체 제어기를 꺼서 호출·관찰 UI까지 사라지게 하지 않습니다.
            m_State = StageState.Arriving; m_WalkAudio.SetActive(true); m_SharedUI.Transition(false, m_TransitionDuration); // 횡단보도가 보이는 인도에서 사용자가 자유롭게 둘러볼 수 있게 합니다.
            yield return new WaitForSecondsRealtime(m_TransitionDuration); // 플레이어는 이미 횡단보도 앞에 배치되었으므로 장소 전환의 암전이 걷히는 시간만 기다립니다.
            m_State = StageState.Calling; // 친구 1의 선택적인 도착 보행과 분리하여 실제 플레이어 도착에서 호출 장면을 시작합니다.
            PlaceCallBubble(); m_CallPanel.Show(true); // 실제 친구 위치의 위쪽에 편집 가능한 호출 대사를 표시합니다.
            Coroutine greeting = StartCoroutine(m_Greeting.Play(m_Origin.Camera.transform, speaks: true)); // 호출 말풍선이 열리는 시점에 친구 2의 손짓과 말하는 표정을 시작합니다.
            yield return greeting; // 호출을 재생하는 동안 친구 1의 기존 도착 보행도 계속 진행되며 그 완료가 다음 단계의 필수 조건은 아닙니다.
            m_WalkAudio.Stop(); m_Friend1.FaceScenarioTarget(m_Friend2.transform); // 짧은 동행 보행 소리를 마치고 대기 중인 친구 1만 건너편을 바라보게 합니다.
            if (!m_Friend2.TryReactToRedSignal(m_RedSignalPoint)) // 기존 첫 발 준비가 수락되기 전에는 호출 UI와 제어기를 함께 없애지 않습니다.
            { // 잘못된 캐릭터 상태를 아무 화면도 없는 정상 도착처럼 숨기지 않습니다.
                Debug.LogError($"친구 2의 첫 발 준비가 거절되었습니다. 활성={m_Friend2.isActiveAndEnabled}, 횡단 명령 가능={m_Friend2.CanReceiveCrossCommand}, 횡단 시작 이력={m_Friend2.HasStartedCrossing}. 기존 횡단 경로와 빨간 표시 참조를 확인해 주세요.", this); // 실제 거절 시점의 기존 이동 상태를 이 제어기와 함께 남깁니다.
                yield break; // 호출 말풍선과 현재 인도 위치는 보존하여 제어기 비활성화로 모든 UI가 숨겨지는 경로를 막습니다.
            } // 정상적으로 수락된 첫 발만 관찰 정지로 이어갑니다.
            m_CallPanel.Show(false); m_State = StageState.Attempt; // 첫 발이 준비된 뒤에만 호출을 숨기고 다음 상태를 확정합니다.
            m_Friend2.SetScenarioFirstStepHold(true, m_FirstStepTime); // 긴 프레임에서도 첫 발이 끝나 신호를 바라보는 다음 반응으로 넘어가지 않게 합니다.
            elapsed = 0f; while (elapsed < Mathf.Clamp(m_FirstStepTime, 0.08f, 0.16f)) { elapsed += Time.unscaledDeltaTime; yield return null; } // 루트가 도로로 전진하지 않는 첫 발 자세가 적용될 시간을 확보합니다.
            m_QueueFreeze = true; // 기존 Animator와 교통이 이번 프레임을 마친 뒤 한 번에 관찰 장면을 고정합니다.
        } // 이번 작업은 관찰 상태 진입에서 끝납니다.

        private void LateUpdate() // 최종 보행 자세와 교통 위치를 기준으로 도입 화면과 정지를 처리합니다.
        { // 카메라 추적과 VR 입력 갱신은 중단하지 않습니다.
            if (m_State == StageState.Walking) PositionWalkingCamera(); // 별도 동행 카메라만 친구의 실제 걸음을 따라갑니다.
            if (m_BrakeEffectActive) UpdateBrakeMarks(); // 실제 차의 이동 거리에 맞춰 이미 배치한 자국만 갱신합니다.
            if (!m_QueueFreeze) return; // 첫 발 순간이 아니면 관찰 정지를 실행하지 않습니다.
            m_QueueFreeze = false; m_Friend2.SetScenarioPaused(true); m_Friend1Performance.Freeze(true); // 각 친구의 경로와 Animator만 현재 자세에서 정지합니다.
            m_Signal.SetScenarioPaused(true); foreach (TrafficCarMovement car in m_Cars) car.SetScenarioPaused(true); // 빨간 표시를 유지하며 관련 차량은 이번 프레임 위치와 속도를 보관합니다.
            PrepareObservationRays(); // 도입 중 보류된 예전 명령 컴포넌트의 Start에 의존하지 않고 기존 관찰 레이의 거리만 확보합니다.
            m_Performance.CaptureObservation(); foreach (TrafficCarMovement car in m_Cars) car.CaptureScenarioCheckpoint(); m_CheckpointReady = true; // 개별 정지가 끝난 같은 프레임의 친구 자세와 차량 주행을 저장합니다.
            EnterObservation(); // 기존 네 단서와 공통 관찰 완료 버튼으로 진입합니다.
        } // Time.timeScale과 헤드셋 추적은 변경하지 않습니다.

        private void PrepareObservationRays() // 기존 컨트롤러의 레이를 새로 만들지 않고 반대편 인도까지 조사할 수 있게 합니다.
        { // 입력 액션과 상호작용 레이어는 기존 XR 구성 값을 유지합니다.
            foreach (NearFarInteractor interactor in m_Origin.GetComponentsInChildren<NearFarInteractor>(true)) // 아직 추적되지 않은 양손 컨트롤러도 기존 계층에서 포함합니다.
            { // 같은 컨트롤러의 실제 판정 거리와 표시 거리만 함께 조정합니다.
                if (interactor.TryGetComponent(out CurveInteractionCaster caster)) caster.castDistance = Mathf.Max(caster.castDistance, m_ObservationDistance); // 원래 더 긴 범위는 유지하고 건너편 단서까지 레이가 닿게 합니다.
                foreach (CurveVisualController visual in interactor.GetComponentsInChildren<CurveVisualController>(true)) visual.maxVisualCurveDistance = Mathf.Max(visual.maxVisualCurveDistance, m_ObservationDistance); // 실제 판정이 닿는 거리에서 기존 레이 표시가 먼저 끊기지 않게 합니다.
            } // XR Origin·카메라 추적·컨트롤러 입력은 변경하지 않습니다.
        } // 새로운 레이캐스터나 관찰 입력 시스템은 생성하지 않습니다.

        public void FinishObservation() // 기존 공통 관찰 완료 버튼으로만 다음 행동 선택 진입 대기를 요청합니다.
        { // 조사 시간이나 조사한 단서 개수를 진행 조건으로 추가하지 않습니다.
            if (!IsObservationReady) return; // 도입 중 입력이나 이미 완료한 버튼의 중복 선택은 무시합니다.
            m_State = StageState.ActionSelectionReady; // 이벤트를 보내기 전에 상태를 확정하여 같은 관찰이 중복 완료되지 않게 합니다.
            SetObservations(false); m_SharedUI.HideAll(); // 네 단서의 입력과 테두리를 해제하고 기존 안내·생각 패널을 부드럽게 숨깁니다.
            m_OnObservationFinished.Invoke(); // 관찰 완료 이벤트는 유지하며 선택이 확정되기 전에는 결과를 실행하지 않습니다.
            if (m_State == StageState.ActionSelectionReady) m_ResultUI.ShowChoice(); // 관찰을 직접 마친 뒤에만 두 번째 상황의 실제 선택 패널을 표시합니다.
        } // 친구의 자세·차량 위치·빨간 신호의 개별 정지는 다음 단계가 연결되기 전까지 그대로 유지합니다.

        private void EnterObservation() // 도입 직후와 사고 복원 뒤에 같은 기존 관찰 흐름을 사용합니다.
        { // 조사 개수와 관찰 시간을 진행 조건으로 사용하지 않습니다.
            m_ResultUI.HideAll(); m_RetryRequested = false; m_State = StageState.Observation; SetObservations(true); // 결과 버튼을 해제하고 두 번째 상황의 단서만 다시 엽니다.
            m_SharedUI.PlaceInFront(m_Origin.Camera.transform); m_SharedUI.ShowObservation(FinishObservation); // 기존 생각 말풍선과 완료 버튼을 이번 상황에 연결합니다.
        } // 첫 번째 상황의 UI나 완료 상태는 되돌리지 않습니다.

        public void ReturnToObservation() // 선택을 확정하기 전에 다시 주변을 살펴볼 수 있게 합니다.
        { // 선택 결과가 이미 시작된 경우에는 다시 관찰 버튼을 받지 않습니다.
            if (m_State == StageState.ActionSelectionReady) EnterObservation(); // 그대로 정지된 장면에서 기존 네 단서를 다시 활성화합니다.
        } // 되돌아가도 모든 단서를 클릭해야 한다는 조건은 없습니다.
        public void ChooseSafe() // 기존 VR·마우스 버튼의 안전 선택 진입점입니다.
        { // 코루틴을 시작하기 전에 상태를 잠가 두 결과의 중복 실행을 막습니다.
            if (m_State != StageState.ActionSelectionReady) return; // 관찰 중이거나 결과를 재생하는 동안의 입력은 무시합니다.
            m_State = StageState.SafeResult; m_ResultUI.HideAll(); m_SharedUI.HideAll(); StartCoroutine(RunSafeResult()); // 선택지를 부드럽게 숨기고 기다리기 결과만 시작합니다.
        } // 같은 프레임에 들어온 위험 버튼은 상태 조건에서 거절됩니다.
        public void ChooseUnsafe() // 빨간불 위험 선택에서만 신호 자동 정지를 우회한 횡단을 시작합니다.
        { // 기존 일반 횡단 판단 컴포넌트는 계속 보류한 상태입니다.
            if (m_State != StageState.ActionSelectionReady) return; // 이미 수락된 선택이나 관찰 중 입력은 받지 않습니다.
            m_State = StageState.UnsafeResult; m_ResultUI.HideAll(); m_SharedUI.HideAll(); StartCoroutine(RunUnsafeResult()); // 결과 상태를 먼저 확정하고 위험 연출을 한 번만 재생합니다.
        } // 첫 번째 상황의 선택 이벤트와 연결하지 않습니다.
        public void RequestRetry() { if (m_State == StageState.Retry) m_RetryRequested = true; } // 실제 사고 원인 설명 중에만 빠른 관찰 복원을 요청합니다.

        private IEnumerator RunSafeResult() // 경고·신호 확인·인도 안쪽 뒷걸음·감사를 순서대로 재생합니다.
        { // 신호 타이머는 보류하여 이번 결과에서 파란불로 전환하지 않습니다.
            m_Signal.HoldRedForScenario(); m_ResultUI.ShowWarning(); m_Friend1Performance.Freeze(false); // 빨간불을 유지하며 경고 말풍선과 친구 1의 대기 애니메이션을 재개합니다.
            foreach (TrafficCarMovement car in m_Cars) car.SetScenarioPaused(false); // 자동차는 기존 빨간불 차로에서 정상적으로 지나갑니다.
            m_Friend2.SetScenarioFirstStepHold(false); m_Friend2.SetScenarioPaused(false); // 기존 첫 발 반응을 끝내며 스스로 빨간 신호를 확인하게 합니다.
            yield return m_Performance.NoticeRedSignal(m_RedSignalPoint); // 기존 걷기에서 정지·신호 확인으로 연결한 뒤 놀람을 보여 줍니다.
            m_ResultUI.HideAll(); yield return m_Performance.RetreatInsideSidewalk(m_RetreatPoint, m_Origin.Camera.transform); // 도로에 들어가지 않고 인도 안쪽으로 발을 번갈아 옮깁니다.
            m_ResultUI.ShowThanks(m_Performance.HeadPosition); // 물러난 친구의 머리 옆에 기존 감사 말풍선을 표시합니다.
            BlenderRigPose.PlayFace(m_Performance.HeadTarget, 2.1f, 4.2f); // 감사하는 동안 입과 눈을 움직이고 안전 완료에도 눈웃음을 잠시 유지합니다.
            yield return m_Performance.ThankPlayer(3f); // 감사 문구를 읽을 수 있도록 3초 동안 미소와 고개 끄덕임을 유지합니다.
            m_Friend2.PlaceWaitingForScenario(m_RetreatPoint); m_Friend2.FaceScenarioTarget(m_Origin.Camera.transform); m_Friend2.SetScenarioPaused(false); // 같은 건너편 위치에서 기존 대기 상태를 이어갑니다.
            m_Completed = true; m_State = StageState.Complete; m_ResultUI.ShowSafe(); m_OnCompleted.Invoke(); // 안전하게 기다리기를 완료로 기록하고 세 번째 상황은 시작하지 않습니다.
        } // 차량은 정상 주행하고 친구 2는 빨간불의 건너편 인도에 남습니다.

        private IEnumerator RunUnsafeResult() // 빨간불 횡단 뒤 실제 차로에서 충돌 직전 급정거를 보여 줍니다.
        { // 친구와 차의 Transform을 다른 스크립트가 동시에 제어하지 않게 합니다.
            m_Friend1Performance.Freeze(false); m_Signal.HoldRedForScenario(); yield return m_Performance.WaveBeforeCrossing(); // 반가운 손짓 뒤에도 빨간 신호는 그대로 유지합니다.
            TrafficCarMovement car = m_Cars[0]; m_Friend2.SetScenarioApproachSpeed(1.1f); // 기존 한 대의 차량과 친구의 보폭 동기화 걷기를 사용합니다.
            float arrivalTime = Vector3.ProjectOnPlane(m_NearMissPoint.position - m_Friend2.transform.position, Vector3.up).magnitude / 1.1f + 0.9f; // 첫 발 준비·가속을 포함한 친구의 접근 시간을 계산합니다.
            if (!car.PrepareScenarioNearMiss(m_NearMissPoint.position, m_CarBody, m_FriendBody, arrivalTime, m_CarClearance)) // 차량 준비 실패와 친구 보행 실패를 분리하여 어느 조건에서 진행이 막혔는지 드러냅니다.
            { // 준비 실패를 정상 관찰 전환처럼 숨기지 않습니다.
                RestoreFailedSelection("두 번째 상황의 차량 급정거 준비가 거절되었습니다. 관찰 정지 상태, 차량·친구 Collider와 차로 위 정지 여유를 확인해 주세요."); yield break; // 저장 상태를 복원하고 기존 선택 화면을 다시 표시합니다.
            } // 차량이 준비된 경우에만 빨간불 위험 보행을 요청합니다.
            if (!m_Friend2.TryBeginUnsafeScenarioCrossing(m_NearMissPoint)) // 기존 빨간불 반응에서 명시적인 위험 횡단으로 전환할 수 있는지 확인합니다.
            { // 자동 안전 분기에 흡수되거나 잘못된 상태로 걷지 않게 합니다.
                RestoreFailedSelection("두 번째 상황의 위험 횡단 시작이 거절되었습니다. 친구 2의 관찰 첫 발 상태와 기존 횡단 경로 연결을 확인해 주세요."); yield break; // 이미 준비된 차량도 함께 복원하여 다음 클릭을 받을 수 있게 합니다.
            } // 일반 빨간불 자동 정지는 수락된 위험 횡단에 개입하지 않습니다.
            car.SetScenarioPaused(false); m_FriendWalkAudio.SetActive(true); float deadline = Time.unscaledTime + Mathf.Max(car.ScenarioTravelBudget, m_Friend2.ScenarioTravelBudget) + 4f; // 실제 두 이동 거리와 가속·슬로모션 여유로 대기 상한을 정합니다. // 두 배우가 같은 기존 경로에서 접근을 시작합니다.
            while (!car.IsScenarioBraking && !car.NeedsScenarioBraking && car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 실제 접근이 급정거 지점에 도달하는 동안만 기다립니다.
            if (!car.isActiveAndEnabled || !car.IsScenarioBraking && !car.NeedsScenarioBraking) { RestoreFailedSelection("차량 접근이 진행되지 않아 두 번째 선택 이전 상태를 복원합니다."); yield break; } // 접근 실패를 사고 완료로 처리하지 않습니다. // 차체가 실제 제동 지점에 가까워질 때까지 보행과 주행을 보여 줍니다.
            car.BeginScenarioBraking(); m_HornAudio.Play(); m_BrakeAudio.Play(); m_TireAudio.Play(); BeginBrakeMarks(car); m_SharedUI.PlayDangerCue(); // 실제 급제동 시점에 기존 소리와 자국 및 주황 주변광을 함께 시작합니다.
            m_Friend2.SetScenarioPlaybackRate(0.22f); car.SetScenarioPlaybackRate(0.22f); m_Performance.SetPlaybackRate(0.22f); // 친구를 멈추지 않고 두 배우의 실제 접근만 잠깐 느리게 재생합니다.
            m_SlowMotionReset = StartCoroutine(RestoreResultPlaybackRate()); // 보행 위치를 기다리는 동안에도 느린 동작은 정해진 짧은 시간 뒤 정상으로 돌아갑니다.
            while (m_Friend2.ScenarioLimitRemaining > 0.2f && m_Friend2.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 실제 친구가 도로 진입 위치에 도달할 때까지 기다립니다.
            if (m_Friend2.ScenarioLimitRemaining > 0.2f) { RestoreFailedSelection("친구의 위험 횡단이 진행되지 않아 관찰 당시 자세로 복원합니다."); yield break; } // 도로에 도착하지 않은 친구의 놀람을 재생하지 않습니다. // 차량 제동 시각만으로 친구를 인도에서 멈추지 않고 실제 차 앞 지점까지 기존 보행을 이어갑니다.
            m_FriendWalkAudio.Stop(); Coroutine panic = StartCoroutine(m_Performance.PanicNearCar()); // 횡단보도에 진입한 친구만 개별 정지한 뒤 놀람과 뒷걸음으로 연결합니다.
            while (!car.HasScenarioStopped && car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 실제 속도와 급정거 위치가 맞아야 다음 결과를 진행합니다.
            if (!car.HasScenarioStopped) { RestoreFailedSelection("차량 제동이 완료되지 않아 관찰 상태를 복원합니다."); yield break; } // 정지 실패를 임의로 스킵하지 않습니다. // 실제 앞범퍼가 친구 표면과 여유를 남기고 멈출 때까지 기다립니다.
            yield return panic; // 양발 뒷걸음과 놀란 자세가 끝난 뒤 설명을 표시합니다.
            m_State = StageState.Retry; m_ResultUI.ShowUnsafe(); float elapsed = 0f; // 정지한 사고 직전 장면 위에 원인과 재시도 버튼을 표시합니다.
            while (elapsed < m_ResultDuration && !m_RetryRequested) { elapsed += Time.unscaledDeltaTime; yield return null; } // 설명을 읽은 뒤 자동 복원하며 버튼으로 먼저 다시 관찰할 수 있습니다.
            m_ResultUI.HideAll(); m_SharedUI.Transition(true); m_ResetAudio.Play(); // 첫 번째 상황의 짧은 암전 방식을 같은 공유 전환막으로 재사용합니다.
            yield return new WaitForSecondsRealtime(m_SharedUI.FadeCoverDuration); // 실제 배치와 자세 변경은 가려진 동안에만 적용합니다.
            RestoreObservationScene(); EnterObservation(); m_SharedUI.Transition(false); // 카메라를 되감지 않고 친구·차·신호를 같은 관찰 순간으로 복원합니다.
        } // 첫 번째 완료 기록과 플레이어 위치는 유지합니다.

        private IEnumerator RestoreResultPlaybackRate() // 실제 접근 위치와 무관하게 짧은 느린 동작의 종료를 보장합니다.
        { // 친구·차량만 제어하며 입력과 UI 시간은 바꾸지 않습니다.
            yield return new WaitForSecondsRealtime(0.85f); // 기존 결과의 짧은 강조 시간은 유지합니다.
            m_Friend2.SetScenarioPlaybackRate(1f); m_Cars[0].SetScenarioPlaybackRate(1f); m_Performance.SetPlaybackRate(1f); // 보행·차량·관절 표현을 함께 정상 속도로 복원합니다.
        } // 중단 시에는 기존 관찰 복원이 같은 배율을 초기화합니다.

        private void RestoreFailedSelection(string reason) // 결과 진입 조건이 거절되었을 때 원인과 입력 가능 상태를 보존합니다.
        { // 실패를 관찰 완료 이전으로 조용히 되돌려 선택이 먹히지 않은 것처럼 보이게 하지 않습니다.
            Debug.LogError(reason, this); // 실제로 거절된 조건을 이 상황 컴포넌트와 함께 Console에 남깁니다.
            RestoreObservationScene(); SetObservations(false); m_SharedUI.HideAll(); // 부분적으로 준비된 차량·친구를 복원하고 관찰 말풍선을 해제합니다.
            m_State = StageState.ActionSelectionReady; m_ResultUI.ShowChoice(); // 같은 실제 선택 버튼으로 다시 입력할 수 있는 단계로 돌아갑니다.
        } // 유효하지 않은 경로에서 차량이나 캐릭터를 강제로 통과시키지 않습니다.

        private void RestoreObservationScene() // 위험 선택과 효과의 모든 임시 상태를 관찰 시점으로 되돌립니다.
        { // 취소와 재시도에서 같은 복원 경로를 사용합니다.
            StopResultEffects(); if (m_Performance != null) m_Performance.RestoreObservation(); foreach (TrafficCarMovement car in m_Cars) if (car != null) car.RestoreScenarioCheckpoint(); // 친구의 횡단 기록·관절과 차량의 위치·속도·제동 상태를 함께 복원합니다.
            if (m_Friend1Performance != null) m_Friend1Performance.Freeze(true); if (m_Signal != null) m_Signal.HoldRedForScenario(); // 살아 있는 친구 1의 대기와 빨간 표시·정지 타이머를 다시 유지합니다.
        } // 전역 시간과 첫 번째 상황의 진행 값은 변경하지 않습니다.
        public void HideCompletedResultForNextSituation() // 실제 안전 완료 뒤에만 세 번째 상황으로 UI 소유권을 넘깁니다.
        { // 두 번째 상황의 완료 기록은 계속 유지합니다.
            if (!m_Completed) return; // 미완료한 빨간불 상황을 건너뛰지 않습니다.
            m_ResultUI.HideAll(); m_SharedUI.HideAll(); StopResultEffects(); // 완료 패널과 남은 소리만 정리합니다.
        } // 캐릭터나 신호는 다음 상황이 명시적으로 준비합니다.
        public void PlaySharedBrakeEffects() { m_HornAudio.Play(); m_BrakeAudio.Play(); m_TireAudio.Play(); BeginBrakeMarks(m_Cars[0]); m_SharedUI.PlayDangerCue(); } // 세 번째 상황의 실제 급제동도 기존 소리와 자국에 같은 주변광을 동기화합니다.
        public void StopSharedBrakeEffects() { StopResultEffects(); } // 재시도 시 같은 음향·자국을 정리하며 완료 상태는 바꾸지 않습니다.
        public void PlaySharedResetSound() { m_ResetAudio.Play(); } // 기존 짧은 복원 전환 음원만 재사용합니다.

        private void BeginBrakeMarks(TrafficCarMovement car) // 실제 제동이 시작된 차로 위치에 미리 배치한 자국을 놓습니다.
        { // 런타임에 효과 오브젝트를 만들지 않습니다.
            m_BrakeStart = car.transform.position; m_BrakeMarks.SetPositionAndRotation(new Vector3(m_BrakeStart.x, 0.02f, m_BrakeStart.z), car.transform.rotation); // 기존 차로 방향과 도로 표면에 맞춥니다.
            m_BrakeMarks.gameObject.SetActive(true); m_BrakeEffectActive = true; // 재시도 전까지 짧은 실제 제동 자국만 보여 줍니다.
        } // 차량과 친구의 Collider는 변경하지 않습니다.
        private void UpdateBrakeMarks() // 제동 자국이 실제 바퀴 이동보다 길게 늘어나지 않게 합니다.
        { // 차량 루트의 이동 소유권에는 관여하지 않습니다.
            float length = Mathf.Clamp(Vector3.Dot(m_Cars[0].transform.position - m_BrakeStart, m_Cars[0].transform.forward), 0.025f, 4f); // 제동 시작부터 경로 방향으로 움직인 실제 거리만 구합니다.
            for (int i = 0; i < m_TireMarks.Length; ++i) { m_TireMarks[i].localPosition = new Vector3(i == 0 ? -0.83f : 0.83f, 0f, 0.95f + length * 0.5f); m_TireMarks[i].localScale = new Vector3(0.065f, 0.002f, length); } // 두 앞바퀴 위치에서 도로를 따라 얇은 자국을 표시합니다.
        } // 화면 흔들림이나 전체 점멸 효과는 사용하지 않습니다.

        private void StopResultEffects() // 음향과 제동 자국이 다음 선택에 중복되지 않게 정리합니다.
        { // 씬 종료 중 먼저 해제된 구성요소는 건너뜁니다.
            if (m_SlowMotionReset != null) { StopCoroutine(m_SlowMotionReset); m_SlowMotionReset = null; } // 복원 뒤 이전 느린 동작 종료 이벤트를 남기지 않습니다.
            if (m_FriendWalkAudio != null) m_FriendWalkAudio.Stop(); // 친구 2의 접근 발소리와 환경음을 정리합니다.
            foreach (AudioSource source in new[] { m_HornAudio, m_BrakeAudio, m_TireAudio }) if (source != null) source.Stop(); // 이번 결과에서 시작한 공간 음향만 중단합니다.
            m_BrakeEffectActive = false; if (m_BrakeMarks != null) m_BrakeMarks.gameObject.SetActive(false); // 제동 자국을 감춰 반복 재시도에서 이전 효과가 남지 않게 합니다.
        } // 공유 전환 음향과 첫 번째 상황의 소리는 변경하지 않습니다.

        private void PlaceWalkingScreen() // 실제 씬의 동행 화면을 현재 시야 앞에 한 번 배치합니다.
        { // 카메라와 사용자의 머리 방향은 돌리지 않습니다.
            Vector3 forward = Vector3.ProjectOnPlane(m_Origin.Camera.transform.forward, Vector3.up).normalized; // 현재 시선의 수평 방향에서 편안한 표시 위치를 구합니다.
            if (forward.sqrMagnitude < 0.001f) forward = m_Origin.Origin.transform.forward; // 수직으로 보는 경우 기존 Origin 방향을 사용합니다.
            m_Canvas.SetPositionAndRotation(m_Origin.Camera.transform.position + forward * 2.15f - Vector3.up * 0.12f, Quaternion.LookRotation(forward)); // 월드 공간 화면만 기존 읽기 거리에 놓습니다.
        } // 화면 위치는 사용자 머리를 매 프레임 따라오지 않습니다.

        private void PositionWalkingCamera() // 같은 인도를 걷는 1인칭 동행 구도만 별도 영상으로 촬영합니다.
        { // 얼굴 클로즈업이나 실제 XR 카메라 이동은 사용하지 않습니다.
            Vector3 right = Vector3.Cross(Vector3.up, m_WalkForward); // 친구 옆에서 걷는 영상 속 간격을 구합니다.
            Vector3 position = m_Friend1.transform.position - m_WalkForward * 1.6f - right * 1.25f + Vector3.up * 1.65f; // 친구와 앞쪽 인도가 함께 보이는 눈높이를 유지합니다.
            m_WalkCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(m_Friend1.transform.position + m_WalkForward * 0.75f + Vector3.up * 1.12f - position, Vector3.up)); // RenderTexture 카메라만 실제 걸음에 맞춰 진행합니다.
        } // 기존 헤드셋 카메라의 시야각과 자세는 그대로 유지합니다.

        private void PlaceCallBubble() // 중요한 신호와 친구 얼굴을 가리지 않는 실제 호출 말풍선 위치를 적용합니다.
        { // 문구·색·크기는 씬의 실제 UI 오브젝트에 저장한 값을 사용합니다.
            Vector3 position = m_Greeting.HeadPosition + m_CallOffset; // Inspector에서 편집한 간격으로 실제 친구 머리 옆에 배치합니다.
            m_CallPanel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - m_Origin.Camera.transform.position)); // 실제 친구 근처의 월드 말풍선만 플레이어에게 읽히게 향합니다.
        } // 플레이어가 고개를 돌려 건너편 친구를 찾을 수 있게 위치를 유지합니다.

        private void SetObservations(bool available) { foreach (FirstSituationObservation target in m_Observations) if (target != null) target.SetAvailable(available); } // 기존 관찰 규칙과 테두리를 같은 상태에서 개별 연결합니다.

        private void OnDisable() // 도입 취소나 씬 해제 시 이 제어기가 보류한 기능만 복원합니다.
        { // 완료되지 않은 첫 번째 상황의 영상과 이동에는 접근하지 않습니다.
            StopAllCoroutines(); m_QueueFreeze = false; SetObservations(false);
            if (m_Started && m_CheckpointReady && !m_Completed) RestoreObservationScene(); // 결과 도중 중단해도 횡단 기록·제동·느린 동작을 남기지 않습니다.
            StopResultEffects(); if (m_ResetAudio != null) m_ResetAudio.Stop(); if (m_ResultUI != null) m_ResultUI.HideAll(); // 실제 결과 패널과 이 상황의 효과만 정리합니다.
            m_CheckpointReady = false; // 재활성화한 도입은 새 관찰 프레임을 다시 저장합니다. // 중단된 코루틴과 두 번째 상황의 단서 입력을 정리합니다.
            if (m_WalkPanel != null) m_WalkPanel.Show(false); if (m_CallPanel != null) m_CallPanel.Show(false); // 살아 있는 두 번째 상황 UI만 숨깁니다.
            if (m_Started) // 이 상황이 실제로 소유한 보류 상태만 돌려줍니다.
            { // 이미 해제된 씬 구성에는 접근하지 않습니다.
                if (m_WalkCamera != null) m_WalkCamera.enabled = false; if (m_WalkAudio != null) m_WalkAudio.Stop(); if (m_Greeting != null) m_Greeting.Stop(); // 이 도입의 영상·보행음·호출을 정리합니다.
                if (m_Friend1 != null && m_Origin != null && m_Origin.Camera != null) m_Friend1.TryStop(m_Origin.Camera.transform); // 취소된 보행은 정리하되 먼저 해제된 플레이어 참조에는 접근하지 않습니다.
                if (m_Friend1Performance != null) m_Friend1Performance.Freeze(m_Friend1WasPaused); if (m_Friend2 != null) m_Friend2.SetScenarioPaused(m_Friend2WasPaused); // 두 친구의 원래 개별 정지 상태를 복원합니다.
                if (m_Friend2 != null) m_Friend2.SetScenarioFirstStepHold(false); // 취소 시 기존 짧은 반응이 원래 종료 흐름을 이어받게 합니다.
                if (m_Signal != null) m_Signal.SetScenarioPaused(m_SignalWasPaused); // 신호는 보류한 시간을 제외하고 원래 자동 전환을 이어갑니다.
                for (int i = 0; i < m_Cars.Length; ++i) if (m_Cars[i] != null) m_Cars[i].SetScenarioPaused(m_CarsWerePaused[i]); // 차량을 다시 초기화하지 않고 보류 전 상태로 돌립니다.
                for (int i = 0; i < m_Locomotion.Length; ++i) if (m_Locomotion[i] != null) m_Locomotion[i].enabled = m_LocomotionEnabled[i]; // 기존 직접 이동의 원래 활성 상태를 복원합니다.
                if (m_FirstPole != null) m_FirstPole.SetActive(m_PoleWasActive); if (m_SharedUI != null) { m_SharedUI.HideAll(); m_SharedUI.Transition(false); } // 취소 시 숨긴 첫 장소와 남은 전환막을 정리합니다.
            } // 정상 관찰 중에는 이 복원 경로를 자동 실행하지 않습니다.
            if (m_LegacyEnabled != null) for (int i = 0; i < m_LegacyCommands.Length; ++i) if (m_LegacyCommands[i] != null) m_LegacyCommands[i].enabled = m_LegacyEnabled[i]; // 기존 명령 코드를 삭제하지 않고 원래 설정을 되돌립니다.
            m_Started = false; m_State = StageState.WaitingForFirst; // 명시적으로 재활성화하면 완료 조건부터 다시 진입하게 합니다.
        } // 실제 입력과 교통을 전역 일시정지 상태로 남기지 않습니다.

        private void OnEnable() // 명시적인 재활성화에서도 완료 조건과 입력 보류를 다시 적용합니다.
        { // 최초 활성화는 Start에서 실제 패널 초기화 뒤 처리합니다.
            if (!m_Initialized) return; // Awake·Start가 끝나지 않은 씬 초기화에는 중복 진입하지 않습니다.
            foreach (Behaviour command in m_LegacyCommands) if (command != null) command.enabled = false; // 호출 도입 중 기존 횡단 명령이 함께 실행되지 않게 합니다.
            if (m_First.IsCompleted) BeginAfterFirstSituation(); // 첫 상황을 실제로 완료했을 때만 다시 도입을 시작합니다.
        } // 친구 2의 다음 행동 선택은 여기에서 구현하지 않습니다.
    } // 두 번째 상황의 관찰 직전 도입을 마칩니다.
} // 초록불 상황과 사고 결과는 포함하지 않습니다.
