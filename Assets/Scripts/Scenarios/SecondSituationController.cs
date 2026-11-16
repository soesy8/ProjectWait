using System.Collections; // 완료 후 보행·암전·도착·호출·첫걸음을 순서대로 연결합니다.
using Time = ProjectWait.M.Crosswalk.CrosswalkStoryTime;
using WaitForSecondsRealtime = ProjectWait.M.Crosswalk.CrosswalkStoryWait;
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
        public enum StageState { WaitingForFirst, FirstResult, Walking, Transition, Arriving, Calling, Attempt, Observation, ActionSelectionReady, SafeResult, UnsafeResult, Retry, Complete } // 저장된 상태 값과 기존 선택·사고·재시도 분기의 번호를 유지합니다.
        [SerializeField, InspectorName("첫 번째 상황 완료"), Tooltip("완료 이벤트를 이 컴포넌트의 BeginAfterFirstSituation에 연결합니다.")] private FirstSituationController m_First; // 전봇대를 안전하게 피한 경우에만 동행을 시작합니다.
        [SerializeField, InspectorName("기존 전환·생각 UI"), Tooltip("기존 재시도 암전과 관찰 말풍선을 사용합니다.")] private FirstSituationUI m_SharedUI; // 관찰과 재도전에 기존 UI를 재사용합니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("동행 보행 중 카메라의 시선은 유지하면서 이동할 기존 Origin입니다.")] private XROrigin m_Origin; // 실제 1인칭 플레이어 루트를 인도 경로로 옮깁니다.
        [SerializeField, InspectorName("공통 친구 이동"), Tooltip("전봇대를 피하고 같은 인도로 횡단보도에 도착하는 친구입니다.")] private FriendCharacterMovement m_Friend; // 도착·첫 발·위험 횡단을 하나의 캐릭터에서 처리합니다.
        [SerializeField, InspectorName("첫 상황 자세 제어"), Tooltip("전봇대 연출이 끝난 친구의 이동·휴대전화 자세 정지를 해제합니다.")] private FirstSituationCharacterPerformance m_FirstPerformance; // 첫 상황에서 정지한 구성 요소를 원래 동작으로 돌립니다.
        [SerializeField, InspectorName("기존 보행 신호"), Tooltip("현재 횡단보도에서 차량도 참조하는 신호등입니다.")] private PedestrianTrafficLight m_Signal; // 실제 빨간 신호 상태와 타이머를 함께 유지합니다.
        [SerializeField, InspectorName("깜빡이는 파란 신호 위치"), Tooltip("친구가 깜빡이는 신호를 보고 출발하려 할 때 바라볼 기존 파란 사람 표시입니다.")] private Transform m_GreenSignalPoint; // 실제 깜빡이는 신호의 위치로 고개와 첫 발 방향을 연결합니다.
        [SerializeField, InspectorName("관련 차량"), Tooltip("현재 횡단보도에서 주행하는 기존 차량들입니다.")] private TrafficCarMovement[] m_Cars; // 관찰 중 이 차량들의 속도와 현재 위치만 보존합니다.
        [SerializeField, InspectorName("차량 접근 시작점"), Tooltip("차량 배열과 같은 순서로 첫 인도 구간 뒤쪽의 차로 위 출발점을 연결합니다.")] private Transform[] m_CarEntries; // 플레이어와 친구가 인도를 걷는 동안 뒤에서 차가 접근할 경로를 준비합니다.
        [SerializeField, InspectorName("보류할 직접 이동"), Tooltip("기존 연속 이동·텔레포트 Provider만 연결하며 머리 추적과 컨트롤러 레이는 제외합니다.")] private Behaviour[] m_Locomotion; // 직접 조작 없이 진행하되 VR 입력과 시선은 유지합니다.
        [SerializeField, InspectorName("횡단보도 관찰 대상"), Tooltip("친구·신호·차량·횡단보도의 기존 관찰 대상입니다.")] private FirstSituationObservation[] m_Observations; // 실제 클릭 가능한 기존 물체를 재사용합니다.
        [SerializeField, InspectorName("보행 신호 클릭 대상"), Tooltip("빨간 보행 신호를 확인할 신호등 관찰 컴포넌트입니다.")] private FirstSituationObservation m_SignalObservation; // 친구에게 답하기 전에 직접 신호를 확인하게 합니다.
        [SerializeField, InspectorName("길 찾기 횡단보도 대상"), Tooltip("전봇대를 지난 뒤 건너갈 길을 찾을 때 강조하고 직접 누를 기존 횡단보도입니다.")] private FirstSituationObservation m_CrosswalkObservation; // 횡단보도를 고른 뒤에만 입구까지 동행을 이어갑니다.
        [SerializeField, InspectorName("친구 클릭 대상"), Tooltip("첫 상황부터 같이 걷는 친구의 관찰 컴포넌트입니다.")] private FirstSituationObservation m_FriendObservation; // 신호를 확인한 다음 친구에게 대답합니다.
        private bool m_SignalChecked; // 위험 횡단 재시도에서는 확인한 빨간불 설명을 유지합니다.
        [SerializeField, Min(12f), InspectorName("관찰 레이 거리(m)"), Tooltip("양손 Near-Far 레이의 판정과 표시 범위가 신호와 접근 차량까지 닿게 합니다.")] private float m_ObservationDistance = 30f; // 기존에 더 긴 관찰 범위는 그대로 사용합니다.
        [SerializeField, InspectorName("친구 대화 시작 이벤트"), Tooltip("친구에게 말을 걸어 빨간불 선택지가 열릴 때 한 번 호출합니다.")] private UnityEvent m_OnObservationFinished = new UnityEvent(); // 기존 직렬화 이벤트를 같은 선택 진입 시점에 유지합니다.
        [SerializeField, InspectorName("공통 친구 발소리"), Tooltip("전봇대부터 횡단보도와 횡단까지 같은 친구의 걸음에 연결된 소리입니다.")] private FirstSituationWalkAudio m_WalkAudio; // 동행과 위험 횡단에서 같은 걸음 소리를 재생합니다.
        [SerializeField, InspectorName("전봇대 이후 보행 지점"), Tooltip("친구가 전봇대를 피한 뒤 그대로 따라갈 인도 중간 지점입니다.")] private Transform m_LeavePoint; // 회피 경로의 연장선에서 인도를 따라 걷게 합니다.
        [SerializeField, InspectorName("플레이어 대기 위치"), Tooltip("친구 옆에서 신호를 관찰할 플레이어 쪽 인도 위치입니다.")] private Transform m_PlayerPoint; // 실제 플레이어 이동의 끝점을 정합니다.
        [SerializeField, InspectorName("친구 횡단 시작 위치"), Tooltip("전봇대 쪽 인도에서 횡단보도 입구에 닿는 지점입니다.")] private Transform m_FriendArrival; // 도착 후 같은 인도에서 첫 발 동작을 시작합니다.
        [SerializeField, Min(0.1f), InspectorName("동행 보행 속도(m/s)"), Tooltip("전봇대를 피한 뒤 횡단보도까지의 보행 속도입니다.")] private float m_WalkSpeed = 1.55f; // 동행 시간과 발걸음 위상을 실제 이동 거리에 연결합니다.
        [SerializeField, Min(1f), InspectorName("성급한 횡단 달리기 속도(m/s)"), Tooltip("깜빡이는 파란불에 뛰어드는 위험 선택에서 같은 친구가 실제로 달리는 속도입니다.")] private float m_RunSpeed = 2.8f; // 인도 동행 속도와 구분해 위험 횡단만 빠른 보폭과 속도로 진행합니다.
        [SerializeField, Min(1f), InspectorName("차량 출발 시 남은 인도 거리(m)"), Tooltip("친구가 차량 차선에 도달할 때 차도 같은 교차점 앞에 접근하도록 정한 출발 여유 거리입니다.")] private float m_CarReleaseDistance = 7.5f; // 차량 차선까지 짧아진 달리기 거리와 기존 6m/s 차량의 접근 시간을 맞춥니다.
        [SerializeField, Min(0f), InspectorName("회피 후 대사 여유"), Tooltip("전봇대를 피한 친구의 마지막 대사를 읽고 동행을 시작하기까지의 시간입니다.")] private float m_ResultHold = 0.8f; // 앞선 대사가 끝난 자리에서 보행을 이어갑니다.
        [SerializeField, Range(0.08f, 0.16f), InspectorName("첫 발 자세 정지 시점"), Tooltip("기존 0.2초 첫 발 반응이 끝나기 전에 정지할 시각입니다. 루트는 도로로 전진하지 않습니다.")] private float m_FirstStepTime = 0.14f; // 친구가 도로 한가운데까지 걸어가는 상태를 만들지 않습니다.
        [SerializeField, InspectorName("현재 진행 단계"), Tooltip("첫 완료 대기·도입·관찰·행동 선택 진입 대기를 구분합니다.")] private StageState m_State; // 이후 진행 시스템은 관찰과 선택 준비 여부를 구분하여 참조합니다.
        private bool[] m_LocomotionEnabled, m_CarsWerePaused; // 동행을 취소하면 기존 입력과 차량의 정지 설정을 복원합니다.
        private bool m_Initialized, m_Started, m_QueueFreeze, m_FriendWasPaused, m_SignalWasPaused; // 보행 시작과 관찰 정지의 수명을 구분합니다.
        private Vector3 m_PlayerStart, m_FriendStart; // 전환을 중단하면 전봇대 회피가 끝난 위치로 돌아갑니다.
        private Quaternion m_FriendStartRotation; // 재개할 때 같은 회피 직후 방향을 사용합니다.
        private Quaternion m_PlayerStartRotation;
        private CrosswalkDesktopInput m_ViewInput;
        [SerializeField, InspectorName("두 번째 선택·결과 UI"), Tooltip("실제 씬에 배치한 파스텔 선택·대사·결과·재시도 패널입니다.")] private SecondSituationUI m_ResultUI; // 첫 번째 상황의 선택지와 결과 패널은 변경하지 않습니다.
        [SerializeField, InspectorName("공통 친구 결과 동작"), Tooltip("같은 친구의 놀람·뒷걸음·감사와 관찰 자세 저장을 담당합니다.")] private SecondSituationCharacterPerformance m_Performance; // 기존 사고와 안전 결과의 골격 연출을 공통 친구에 적용합니다.
        [SerializeField, InspectorName("안전한 인도 안쪽 위치"), Tooltip("친구가 빨간불을 확인하고 플레이어와 같은 인도에서 물러날 지점입니다.")] private Transform m_RetreatPoint; // 세 번째 상황이 이어지는 대기 위치입니다.
        [SerializeField, InspectorName("차량 앞 횡단 제한 위치"), Tooltip("친구가 기존 횡단 경로에서 더 진행하지 않을 도로 위 지점입니다.")] private Transform m_NearMissPoint; // 사고 연출 중 친구가 차로를 계속 가로질러 가지 않게 제한합니다.
        [SerializeField, InspectorName("공통 친구 몸 Collider"), Tooltip("공통 친구의 관찰 및 차량과의 간격 계산에 쓰는 몸 Collider입니다.")] private Collider m_FriendBody; // 기존 급정거가 새 횡단 방향에서도 친구 앞에 멈추도록 합니다.
        [SerializeField, InspectorName("기존 차량 몸 Collider"), Tooltip("기존 자동차 전체를 감싸는 관찰 Collider입니다.")] private Collider m_CarBody; // 앞범퍼를 포함하는 안전 여유를 계산합니다.
        [SerializeField, Min(0.3f), InspectorName("차량과 친구의 최소 간격(m)"), Tooltip("차체 Collider와 친구 Collider 표면 사이에 남길 여유 거리입니다.")] private float m_CarClearance = 0.4f; // 실제 차체가 친구를 관통하거나 겹치지 않게 합니다.
        [SerializeField, InspectorName("차량 경적"), Tooltip("기존 자동차 아래에 배치한 경적 AudioSource입니다.")] private AudioSource m_HornAudio; // 급정거 직전에 한 번만 울립니다.
        [SerializeField, InspectorName("급정거 소리"), Tooltip("기존 자동차의 짧은 제동 소리입니다.")] private AudioSource m_BrakeAudio; // 속도가 급히 줄어드는 순간을 전달합니다.
        [SerializeField, InspectorName("타이어 마찰음"), Tooltip("기존 자동차에 배치한 타이어 마찰 AudioSource입니다.")] private AudioSource m_TireAudio; // 실제 제동 구간에만 재생합니다.
        [SerializeField, InspectorName("복원 전환 소리"), Tooltip("기존 짧고 부드러운 전환 음원을 재사용하는 AudioSource입니다.")] private AudioSource m_ResetAudio; // 사고 설명 후 화면 전환에만 사용합니다.
        [SerializeField, InspectorName("제동 자국 기준"), Tooltip("씬에 미리 배치한 두 타이어 자국의 기준 Transform입니다.")] private Transform m_BrakeMarks; // 런타임에 시각 효과 오브젝트를 생성하지 않습니다.
        [SerializeField, InspectorName("양쪽 타이어 자국"), Tooltip("얇은 왼쪽·오른쪽 제동 자국 Transform입니다.")] private Transform[] m_TireMarks; // 자동차가 제동한 실제 거리만큼만 길이를 늘립니다.
        [SerializeField, InspectorName("두 번째 안전 행동 완료"), Tooltip("친구가 빨간불에 인도 안쪽으로 물러난 뒤 기록합니다.")] private bool m_Completed; // 같은 선택이나 완료 이벤트를 중복 실행하지 않게 합니다.
        [SerializeField, InspectorName("두 번째 안전 행동 완료 이벤트"), Tooltip("친구가 인도에서 기다리게 된 뒤 세 번째 상황을 연결합니다.")] private UnityEvent m_OnCompleted = new UnityEvent(); // 다음 상황에 동일한 위치와 친구 상태를 넘깁니다.
        private bool m_CheckpointReady, m_RetryRequested, m_BrakeEffectActive;
        private Coroutine m_SlowMotionReset; // 실패 복원 뒤 이전 느린 동작 코루틴이 다음 결과를 덮어쓰지 않게 보관합니다. // 관찰 저장과 중복 재시도·제동 효과를 관리합니다.
        private Vector3 m_BrakeStart; // 타이어 자국이 시작된 실제 차량 위치입니다.
        public StageState CurrentState => m_State; // PC 입력이 실제 단계 전환을 감지하여 이전 커서 잠금을 해제하도록 제공합니다.
        public bool IsCompleted => m_Completed; // 안전 행동을 완료한 경우에만 참조할 수 있습니다.
        public bool HasResultUIInput => isActiveAndEnabled && (m_State == StageState.ActionSelectionReady || m_State == StageState.Retry); // 실제 클릭할 버튼이 있는 단계에서 PC 커서를 요청합니다.
        public bool BlocksPlayerTranslation => isActiveAndEnabled && m_Started; // 직접 이동과 자동 동행이 동시에 Origin을 갱신하지 않게 합니다.
        public bool AllowsLooking => m_Started; // 인도를 걷거나 결과를 볼 때도 플레이어가 카메라 방향을 직접 정합니다.
        public bool IsObservationReady => isActiveAndEnabled && m_State == StageState.Observation; // 정지된 관찰 단계에서만 기존 마우스 단서 선택을 허용합니다.
        public bool IsActionSelectionReady => isActiveAndEnabled && m_State == StageState.ActionSelectionReady; // 다음 진행 시스템이 결과 실행 없이 관찰 완료 상태를 참조할 수 있게 합니다.

        private void Awake() { m_ViewInput = m_First.GetComponent<CrosswalkDesktopInput>(); } // 기존 첫 상황 루트에 연결된 입력기를 사용합니다.

        private void Start() // 씬의 관찰 대상이 준비된 뒤 첫 상황 완료를 기다립니다.
        { // 이전 저장 상태에서도 같은 진입점을 사용합니다.
            m_Initialized = true; // 초기 활성화와 플레이 중 재활성화를 구분합니다.
            if (m_First.IsCompleted) BeginAfterFirstSituation(); // 저장된 완료 상태로 시작한 경우에도 같은 진입점을 사용합니다.
        } // 일반 시작은 첫 번째 상황의 실제 완료 이벤트를 기다립니다.

        public void BeginAfterFirstSituation() // 첫 번째 상황 완료 이벤트에서 호출하는 유일한 도입 진입점입니다.
        { // 수동 호출이나 중복 이벤트로 이전 상황을 건너뛰지 않습니다.
            if (!isActiveAndEnabled || !m_First.IsCompleted || m_Started || m_Completed) return; // 실제 안전 완료와 아직 시작하지 않은 도입만 수락합니다.
            m_Started = true; m_State = StageState.FirstResult; // 다음 입력 프레임보다 먼저 두 번째 상황의 이동 보류 상태를 확정합니다.
            m_SharedUI.SetDialogueSpeaker(m_Friend.transform, m_Performance.HeadTarget);
            m_FriendWasPaused = m_Friend.IsScenarioPaused; m_SignalWasPaused = m_Signal.IsScenarioPaused; // 중간에 제어기가 꺼지면 동행 전 정지 설정을 복원합니다.
            m_PlayerStart = m_Origin.Origin.transform.position; // 실제 플레이어가 전봇대를 피한 장면을 보던 위치부터 이어 걷습니다.
            m_PlayerStartRotation = m_Origin.Origin.transform.rotation;
            m_Friend.transform.GetPositionAndRotation(out m_FriendStart, out m_FriendStartRotation); // 친구의 회피 완료 위치에서 끊김 없이 경로를 연결합니다.
            m_LocomotionEnabled = new bool[m_Locomotion.Length]; m_CarsWerePaused = new bool[m_Cars.Length]; // 취소 시 원래 이동과 차량 정지를 복원할 공간을 준비합니다.
            for (int i = 0; i < m_Locomotion.Length; ++i) { m_LocomotionEnabled[i] = m_Locomotion[i].enabled; m_Locomotion[i].enabled = false; } // 기존 직접 이동만 보류하고 추적·컨트롤러 레이는 유지합니다.
            for (int i = 0; i < m_Cars.Length; ++i) m_CarsWerePaused[i] = m_Cars[i].IsScenarioPaused; // 차량의 기존 상태를 별도로 보관합니다.
            StartCoroutine(RunIntroduction()); // 완료 설명 뒤의 보행과 장소 전환을 순서대로 시작합니다.
        } // 이 진입점은 행동 선택이나 횡단 결과를 실행하지 않습니다.

        private IEnumerator RunIntroduction() // 전봇대 이후의 보행·길 찾기·횡단보도 이동·깜빡이는 신호를 하나의 흐름으로 진행합니다.
        { // 휴대전화를 넣겠다는 대사는 반복하지 않고 실제 보행과 물체 선택으로 이어갑니다.
            yield return new WaitForSecondsRealtime(m_ResultHold); // 앞선 감사 대사가 끝난 뒤 기존 동행 여유만 유지합니다.
            m_SharedUI.HideAll(); m_FirstPerformance.Freeze(false); m_Friend.SetScenarioApproachSpeed(m_WalkSpeed); m_SignalChecked = false; // 첫 상황의 정지를 풀고 새로운 횡단 신호 관찰을 준비합니다.
            m_Signal.HoldGreenForScenario(); // 횡단보도로 걸어가는 동안은 파란불을 유지하고 입구에 가까워진 실제 이동 시점에 깜빡임을 시작합니다.
            for (int i = 0; i < m_Cars.Length; ++i) { m_Cars[i].PlaceForScenario(m_CarEntries[i].position); m_Cars[i].SetScenarioPaused(true); } // 기존 차로의 시작점에서 같은 차량을 준비합니다.
            Vector3 questionPlayerPoint = m_PlayerStart + Vector3.ProjectOnPlane(m_LeavePoint.position - m_FriendStart, Vector3.up); // 전봇대를 지난 정지 지점까지 현재 친구와 플레이어의 간격을 유지합니다.
            yield return WalkTogetherTo(m_LeavePoint, questionPlayerPoint); // 횡단보도 입구로 바로 가지 않고 먼저 전봇대를 지난 인도 지점까지 걷습니다.
            if (Vector3.Distance(m_Friend.transform.position, m_LeavePoint.position) > 0.08f) { Debug.LogError("전봇대 이후 길 찾기 지점까지 동행하지 못했습니다.", this); enabled = false; yield break; } // 도착하지 않은 상태에서 길 찾기 대사로 넘어가지 않습니다.
            m_State = StageState.Calling; m_Friend.SetScenarioPaused(true); SetObservations(false); PrepareObservationRays(); // 친구와 플레이어를 멈추고 멀리 있는 횡단보도를 가리킬 수 있게 합니다.
            bool crosswalkSelected = false; m_CrosswalkObservation.SetAvailable(true); // 길 찾기 단계에서는 실제 횡단보도만 이야기 진행 대상으로 엽니다.
            m_SharedUI.ShowTask(m_CrosswalkObservation, "횡단보도", () => crosswalkSelected = true, "친구", "건너편으로 가야 하는데 어떻게 가지?"); // 요청한 질문과 함께 횡단보도의 기존 강조 효과를 표시합니다.
            while (!crosswalkSelected) yield return null; m_CrosswalkObservation.SetAvailable(false); // 횡단보도를 직접 고르기 전에는 보행을 재개하지 않습니다.
            yield return m_SharedUI.PlayCaption("친구", "그래, 횡단보도로 건너자.", 2.1f); // 횡단보도를 고른 결과를 짧은 확인 대사로 보여 줍니다.
            m_SharedUI.HideAll(); m_Friend.SetScenarioPaused(false); // 확인 대사가 끝난 뒤 기존 보행 애니메이션을 다시 켭니다.
            yield return WalkTogetherTo(m_FriendArrival, m_PlayerPoint.position); // 선택한 횡단보도 입구까지 같은 친구와 함께 이동합니다.
            if (Vector3.Distance(m_Friend.transform.position, m_FriendArrival.position) > 0.08f) { Debug.LogError("친구가 선택한 횡단보도 입구까지 도착하지 못했습니다.", this); enabled = false; yield break; } // 실제 도착 이후에만 신호를 보고 출발하려는 장면을 엽니다.
            foreach (TrafficCarMovement car in m_Cars) car.SetScenarioPaused(true); // 암전 중 정한 플레이어 위치·눈 중심 회전을 다시 덮지 않습니다.
            m_State = StageState.Arriving;
            m_Friend.SetScenarioPaused(false); // 방향 배치는 이미 완전 암전 중에 끝났습니다.
            yield return m_Performance.LookAtTarget(m_GreenSignalPoint, 0.9f); // 도착하기 전부터 이어진 파란불 깜빡임을 끊거나 다시 시작하지 않고 친구가 신호를 바라보게 합니다.
            m_SharedUI.ShowCinematicCaption("친구", "빨리 건너자!"); yield return new WaitForSecondsRealtime(0.9f); // 신호를 보고 서두르는 짧은 대사만 표시합니다.
            if (!m_Friend.TryBeginScenarioFirstStep(m_GreenSignalPoint, m_FirstStepTime)) { Debug.LogError("깜빡이는 파란불에서 친구의 출발 자세를 준비하지 못했습니다.", this); enabled = false; yield break; } // 기존 첫 발을 사용해 친구가 뛰어들려는 순간을 준비합니다.
            m_State = StageState.Attempt; yield return new WaitForSecondsRealtime(m_FirstStepTime); m_QueueFreeze = true; // 첫 발 직후의 자세를 관찰과 선택의 같은 체크포인트로 저장합니다.
        } // 이후 기다리기와 성급한 횡단의 기존 선택 분기로 이어집니다.

        private IEnumerator WalkTogetherTo(Transform destination, Vector3 playerDestination) // 길 찾기 전후의 두 보행 구간에서 같은 이동과 시점 보존 처리를 사용합니다.
        { // 구간 사이의 위치와 도착 방향을 같은 검은 전환으로 한 번만 배치합니다.
            m_State=StageState.Walking;m_WalkAudio.Stop();m_Friend.SetScenarioPaused(true);
            yield return ProjectWait.M.Shared.ComfortBlackout.Relocate(m_SharedUI, () =>
            {
                m_Friend.PlaceWaitingForScenario(destination);
                m_Origin.Origin.transform.position=playerDestination;
                if(destination==m_FriendArrival)
                {
                    m_Signal.HoldFlashingGreenForScenario();
                    if(m_ViewInput)m_ViewInput.PlaceScenarioHeading(-90f,m_FriendArrival.position-m_LeavePoint.position);
                }
                if(Vector3.Distance(destination.position,m_FriendArrival.position)<=m_CarReleaseDistance)
                    foreach(var car in m_Cars)car.SetScenarioPaused(false);
            });
        } // 다음 보행은 실제 횡단보도 선택이 끝난 뒤에만 시작합니다.

        private void LateUpdate() // 실제 골격과 차체 이동이 적용된 프레임에서 관찰을 정지합니다.
        { // 급정거 연출의 타이어 자국도 기존 프레임 순서를 유지합니다.
            if (ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked) return;
            if (m_BrakeEffectActive) UpdateBrakeMarks(); // 차가 제동하며 움직인 거리만큼 자국을 연장합니다.
            if (!m_QueueFreeze) return; // 관찰 진입이 예약되지 않았으면 진행 상태를 바꾸지 않습니다.
            m_QueueFreeze = false; m_Friend.SetScenarioPaused(true); // 공통 친구의 첫 발 자세를 그대로 멈춥니다.
            m_Signal.SetScenarioPaused(true); foreach (TrafficCarMovement car in m_Cars) car.SetScenarioPaused(true); // 관찰 중에는 빨간불과 차량 위치를 유지합니다.
            PrepareObservationRays(); // 기존 VR 관찰 레이가 신호와 차에 닿게 합니다.
            m_Performance.CaptureObservation(); foreach (TrafficCarMovement car in m_Cars) car.CaptureScenarioCheckpoint(); m_CheckpointReady = true; // 같은 시점의 친구와 차량을 재시도 기준으로 저장합니다.
            EnterObservation(); // 빨간불 관찰을 표시하고 공통 조작을 엽니다.
        } // 상태 저장이 끝난 다음에만 관찰 입력을 받습니다.

        private void PrepareObservationRays() // 기존 컨트롤러의 레이를 새로 만들지 않고 반대편 인도까지 조사할 수 있게 합니다.
        { // 입력 액션과 상호작용 레이어는 기존 XR 구성 값을 유지합니다.
            foreach (NearFarInteractor interactor in m_Origin.GetComponentsInChildren<NearFarInteractor>(true)) // 아직 추적되지 않은 양손 컨트롤러도 기존 계층에서 포함합니다.
            { // 같은 컨트롤러의 실제 판정 거리와 표시 거리만 함께 조정합니다.
                if (interactor.TryGetComponent(out CurveInteractionCaster caster)) caster.castDistance = Mathf.Max(caster.castDistance, m_ObservationDistance); // 원래 더 긴 범위는 유지하고 건너편 단서까지 레이가 닿게 합니다.
                foreach (CurveVisualController visual in interactor.GetComponentsInChildren<CurveVisualController>(true)) visual.maxVisualCurveDistance = Mathf.Max(visual.maxVisualCurveDistance, m_ObservationDistance); // 실제 판정이 닿는 거리에서 기존 레이 표시가 먼저 끊기지 않게 합니다.
            } // XR Origin·카메라 추적·컨트롤러 입력은 변경하지 않습니다.
        } // 새로운 레이캐스터나 관찰 입력 시스템은 생성하지 않습니다.

        private void OpenFriendChoices() // 한 발 내민 친구를 누르면 빨간불에서 할 말을 선택할 수 있게 합니다.
        { // 친구에게 말을 걸기 전까지 신호와 주변은 원할 때 조사할 수 있습니다.
            if (!IsObservationReady) return; // 친구가 아직 도착하지 않았거나 이미 대화가 열린 경우에는 새 선택을 시작하지 않습니다.
            m_State = StageState.ActionSelectionReady; // 친구를 한 번 누르면 현재 상황의 대답만 열리도록 상태를 먼저 바꿉니다.
            SetObservations(true); m_SharedUI.HideAll(); // 네 단서의 입력과 테두리를 해제하고 기존 안내·생각 패널을 부드럽게 숨깁니다.
            m_OnObservationFinished.Invoke(); // 대화가 열린 시점에 기존 상황 이벤트를 전달합니다.
            if (m_State == StageState.ActionSelectionReady) m_ResultUI.ShowChoice(); // 친구의 질문에 바로 답할 수 있도록 두 행동을 표시합니다.
        } // 친구의 자세·차량 위치·빨간 신호의 개별 정지는 다음 단계가 연결되기 전까지 그대로 유지합니다.

        private void EnterObservation() // 횡단보도 도착 대사를 읽으면서 실제 신호를 조사하게 합니다.
        { // 화면 클릭이나 출발용 친구 클릭을 추가하지 않습니다.
            m_ResultUI.HideAll(); m_RetryRequested = false; m_State = StageState.Observation; SetObservations(true); // 빨간불 첫 발과 차량을 저장한 같은 장면의 관찰을 엽니다.
            if (m_SignalChecked) PromptFriend(); else PromptSignal(); // 실패 재시도에서도 신호를 이미 확인한 상태를 보존합니다.
        } // 다음 행동은 실제 보행자 신호나 친구를 선택할 때만 바뀝니다.

        private void PromptSignal() // 현재 횡단보도 신호를 직접 보고 선택하도록 안내합니다.
        { // 친구의 질문과 물체 조사를 같은 단계에 놓습니다.
            m_SharedUI.ShowTask(m_SignalObservation, "보행자 신호등", OnSignalSelected, "친구", "빨리 건너자!"); // 친구가 서두르는 대사를 유지하면서 실제로 깜빡이는 신호를 먼저 확인하게 합니다.
        } // 대사 자체를 넘기는 클릭은 받지 않습니다.

        private void OnSignalSelected() // 빨간불 조사 결과로 친구에게 할 말을 고르게 합니다.
        { // 실제 신호를 조사한 결과를 상태에 반영합니다.
            m_SignalChecked = true; PromptFriend(); // 발견 대사를 읽고 닫는 절차 없이 경고 대화에 접근하게 합니다.
        } // 다음 세계 선택 입력을 그대로 유지합니다.

        private void PromptFriend() // 빨간불에 내딛으려는 친구에게 경고하는 실제 대화를 엽니다.
        { // 친구 클릭을 단순한 다음 장면 버튼으로 사용하지 않습니다.
            m_SharedUI.ShowTask(m_FriendObservation, "친구에게 말하기", OpenFriendChoices, "", "파란불이 깜빡이고 있어."); // 확인한 깜빡임을 짧게 보여 주고 친구에게 기다리라고 말할 선택으로 연결합니다.
        } // 응답 선택 뒤에는 실제 후퇴 또는 위험 횡단을 보여 줍니다.
        public void ReturnToObservation() // 선택을 확정하기 전에 다시 주변을 살펴볼 수 있게 합니다.
        { // 선택 결과가 이미 시작된 경우에는 다시 관찰 버튼을 받지 않습니다.
            if (m_State == StageState.ActionSelectionReady) EnterObservation(); // 그대로 정지된 장면에서 기존 네 단서를 다시 활성화합니다.
        } // 되돌아가도 모든 단서를 클릭해야 한다는 조건은 없습니다.
        public void ChooseSafe() // 기존 VR·마우스 버튼의 안전 선택 진입점입니다.
        { // 코루틴을 시작하기 전에 상태를 잠가 두 결과의 중복 실행을 막습니다.
            if (m_State != StageState.ActionSelectionReady) return; // 관찰 중이거나 결과를 재생하는 동안의 입력은 무시합니다.
            m_State = StageState.SafeResult; SetObservations(false); m_ResultUI.HideAll(); m_SharedUI.HideAll(); StartCoroutine(RunSafeResult()); // 선택지를 부드럽게 숨기고 기다리기 결과만 시작합니다.
        } // 같은 프레임에 들어온 위험 버튼은 상태 조건에서 거절됩니다.
        public void ChooseUnsafe() // 빨간불 위험 선택에서만 신호 자동 정지를 우회한 횡단을 시작합니다.
        { // 기존 일반 횡단 판단 컴포넌트는 계속 보류한 상태입니다.
            if (m_State != StageState.ActionSelectionReady) return; // 이미 수락된 선택이나 관찰 중 입력은 받지 않습니다.
            m_State = StageState.UnsafeResult; SetObservations(false); m_ResultUI.HideAll(); m_SharedUI.HideAll(); StartCoroutine(RunUnsafeResult()); // 결과 상태를 먼저 확정하고 위험 연출을 한 번만 재생합니다.
        } // 첫 번째 상황의 선택 이벤트와 연결하지 않습니다.
        public void RequestRetry() { if (m_State == StageState.Retry) m_RetryRequested = true; } // 실제 사고 원인 설명 중에만 빠른 관찰 복원을 요청합니다.

        private IEnumerator RunSafeResult() // 깜빡이는 신호에서 친구를 말리면 같은 인도에서 다음 신호를 기다립니다.
        { // 건너기 시작하지 않고 내밀었던 첫 발을 거두는 기존 동작을 사용합니다.
            m_ResultUI.ShowWarning(m_Performance.HeadPosition); // 플레이어가 기다리라고 말한 짧은 대사를 표시합니다.
            yield return m_Performance.RetreatInsideSidewalk(m_RetreatPoint, m_Origin.Camera.transform); // 친구가 도로 쪽 발을 거두고 인도 안쪽으로 물러납니다.
            m_Signal.HoldRedForScenario(); foreach (TrafficCarMovement car in m_Cars) car.SetScenarioPaused(false); // 깜빡임이 끝나 빨간불이 되고 기존 차량은 지나가게 합니다.
            m_Friend.SetScenarioPaused(false); m_Friend.PlaceWaitingForScenario(m_RetreatPoint); yield return m_Performance.AcknowledgeWaiting(); // 같은 친구를 인도에서 기다리는 자세로 연결합니다.
            yield return m_SharedUI.PlayCaption("친구", "알겠어. 기다릴게.", 1.8f); // 기다리기로 한 결과를 반복 설명 없이 짧게 마무리합니다.
            m_SharedUI.HideAll(); m_Completed = true; m_State = StageState.Complete; m_SharedUI.PlaySuccessCue(); m_OnCompleted.Invoke(); // 다음 파란불의 차량 확인 상황으로 기존 완료 이벤트를 연결합니다.
        } // 같은 도로에서 기다린 뒤 다음 상황을 이어갑니다.

        private IEnumerator RunUnsafeResult() // 빨간불 횡단 뒤 실제 차로에서 충돌 직전 급정거를 보여 줍니다.
        { // 친구와 차의 Transform을 다른 스크립트가 동시에 제어하지 않게 합니다.
            m_Signal.HoldFlashingGreenForScenario(); Vector3 crossingStart = m_Friend.transform.position; // 뛰어들기 시작할 때는 깜빡이는 파란불이며 실제 도로 진입 뒤에 빨간불로 바꿉니다.
            TrafficCarMovement car = m_Cars[0]; m_Friend.SetScenarioApproachSpeed(m_RunSpeed); m_Friend.SetScenarioRunning(true); // 위험 선택에서만 같은 친구의 이동 속도와 관절 보폭을 달리기로 전환합니다.
            float arrivalTime = Vector3.ProjectOnPlane(m_NearMissPoint.position - m_Friend.transform.position, Vector3.up).magnitude / m_RunSpeed + m_RunSpeed / 12f; // 횡단보도 중간까지의 달리기 거리와 출발 가속 시간에 차량 급정거를 맞춥니다.
            if (!car.PrepareScenarioNearMiss(m_NearMissPoint.position, m_CarBody, m_FriendBody, arrivalTime, m_CarClearance)) // 차량 준비 실패와 친구 보행 실패를 분리하여 어느 조건에서 진행이 막혔는지 드러냅니다.
            { // 준비 실패를 정상 관찰 전환처럼 숨기지 않습니다.
                RestoreFailedSelection("두 번째 상황의 차량 급정거 준비가 거절되었습니다. 관찰 정지 상태, 차량·친구 Collider와 차로 위 정지 여유를 확인해 주세요."); yield break; // 저장 상태를 복원하고 기존 선택 화면을 다시 표시합니다.
            } // 차량이 준비된 경우에만 빨간불 위험 보행을 요청합니다.
            if (!m_Friend.TryBeginUnsafeScenarioCrossing(m_NearMissPoint)) // 정지된 첫 발 상태에서만 같은 친구의 위험 횡단을 시작합니다.
            { // 횡단이 거절되면 결과를 진행하지 않고 선택지를 다시 엽니다.
                RestoreFailedSelection("두 번째 상황의 위험 횡단 시작이 거절되었습니다. 공통 친구의 첫 발 상태와 횡단 경로를 확인해 주세요."); yield break; // 차량·친구·선택 패널을 기존 관찰 기준으로 되돌립니다.
            } // 준비가 완료된 경우에만 차량을 다시 움직입니다.
            car.SetScenarioPaused(false); m_WalkAudio.SetActive(true); float deadline = Time.unscaledTime + Mathf.Max(car.ScenarioTravelBudget, m_Friend.ScenarioTravelBudget) + 4f; // 실제 접근을 기다리되 차량이나 친구가 멈추면 복원할 수 있게 합니다.
            while (!car.IsScenarioBraking && !car.NeedsScenarioBraking && car.isActiveAndEnabled && Time.unscaledTime < deadline) { UpdateRunningCrossingSignal(crossingStart); yield return null; } // 친구가 도로에 들어선 실제 이동을 보며 신호를 빨간불로 바꾸고 차량 접근을 기다립니다.
            if (!car.isActiveAndEnabled || !car.IsScenarioBraking && !car.NeedsScenarioBraking) { RestoreFailedSelection("차량 접근이 진행되지 않아 두 번째 선택 이전 상태를 복원합니다."); yield break; } // 접근 실패를 사고 완료로 처리하지 않습니다. // 차체가 실제 제동 지점에 가까워질 때까지 보행과 주행을 보여 줍니다.
            car.BeginScenarioBraking(); m_HornAudio.Play(); m_BrakeAudio.Play(); m_TireAudio.Play(); BeginBrakeMarks(car); m_SharedUI.PlayDangerCue(); // 실제 급제동 시점에 기존 소리와 자국 및 주황 주변광을 함께 시작합니다.
            m_Friend.SetScenarioPlaybackRate(0.22f); car.SetScenarioPlaybackRate(0.22f); m_Performance.SetPlaybackRate(0.22f); // 기존 급정거의 느린 구간을 공통 친구와 차에 같은 속도로 적용합니다.
            m_SlowMotionReset = StartCoroutine(RestoreResultPlaybackRate()); // 짧은 강조가 끝나면 기본 진행 속도로 돌아갑니다.
            while (m_Friend.ScenarioLimitRemaining > 0.2f && m_Friend.isActiveAndEnabled && Time.unscaledTime < deadline) { UpdateRunningCrossingSignal(crossingStart); yield return null; } // 신호가 바뀐 상태에서 친구가 횡단보도 절반쯤까지 달려가는 실제 이동을 유지합니다.
            if (m_Friend.ScenarioLimitRemaining > 0.2f) { RestoreFailedSelection("친구의 위험 횡단이 진행되지 않아 관찰 당시 자세로 복원합니다."); yield break; } // 접근 실패가 재시도 불가능한 연출 상태로 남지 않게 합니다.
            m_WalkAudio.Stop(); m_Friend.SetScenarioRunning(false); Coroutine panic = StartCoroutine(m_Performance.PanicNearCar()); // 횡단보도 중간에서 달리기를 멈추고 차를 보고 놀라는 기존 급정거 반응으로 연결합니다.
            while (!car.HasScenarioStopped && car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 실제 속도와 급정거 위치가 맞아야 다음 결과를 진행합니다.
            if (!car.HasScenarioStopped) { RestoreFailedSelection("차량 제동이 완료되지 않아 관찰 상태를 복원합니다."); yield break; } // 정지 실패를 임의로 스킵하지 않습니다. // 실제 앞범퍼가 친구 표면과 여유를 남기고 멈출 때까지 기다립니다.
            yield return panic; // 양발 뒷걸음과 놀란 자세가 끝난 뒤 설명을 표시합니다.
            m_State = StageState.Retry; m_ResultUI.ShowUnsafe(); // 실제 급정거 뒤 원인을 읽고 다시 시도할 수 있게 합니다.
            while (!m_RetryRequested) yield return null; // 원인 설명을 읽고 다시 해보기를 누를 때 같은 첫 발 장면으로 돌아갑니다.
            m_ResultUI.HideAll(); m_SharedUI.Transition(true); m_ResetAudio.Play(); // 첫 번째 상황의 짧은 암전 방식을 같은 공유 전환막으로 재사용합니다.
            yield return new WaitForSecondsRealtime(m_SharedUI.FadeCoverDuration); // 실제 배치와 자세 변경은 가려진 동안에만 적용합니다.
            RestoreObservationScene(); EnterObservation(); m_SharedUI.Transition(false); // 카메라를 되감지 않고 친구·차·신호를 같은 관찰 순간으로 복원합니다.
        } // 첫 번째 완료 기록과 플레이어 위치는 유지합니다.

        private void UpdateRunningCrossingSignal(Vector3 crossingStart) // 성급한 횡단에서 실제 출발한 뒤에만 신호를 빨간불로 바꿉니다.
        { // 선택 버튼을 누른 시점과 도로에 뛰어든 시점을 구분합니다.
            if (m_Signal.CurrentState == PedestrianTrafficLight.SignalState.Green && Vector3.ProjectOnPlane(m_Friend.transform.position - crossingStart, Vector3.up).sqrMagnitude >= 0.5625f) m_Signal.HoldRedForScenario(); // 인도 입구의 첫 발 위치에서 75cm 전진해 실제 차도에 들어서면 깜빡임을 끝내고 빨간불을 표시합니다.
        } // 바뀐 빨간불은 중간 지점의 급정거가 끝날 때까지 유지합니다.

        private IEnumerator RestoreResultPlaybackRate() // 실제 접근 위치와 무관하게 짧은 느린 동작의 종료를 보장합니다.
        { // 친구·차량만 제어하며 입력과 UI 시간은 바꾸지 않습니다.
            yield return new WaitForSecondsRealtime(0.85f); // 기존 결과의 짧은 강조 시간은 유지합니다.
            m_Friend.SetScenarioPlaybackRate(1f); m_Cars[0].SetScenarioPlaybackRate(1f); m_Performance.SetPlaybackRate(1f); // 사고의 강조 구간이 끝난 뒤 친구와 차량의 이동 속도를 함께 복원합니다.
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
            if (m_Signal != null) m_Signal.HoldFlashingGreenForScenario(); // 재시도하면 뛰어들기 전의 같은 깜빡이는 파란불로 돌아갑니다.
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
            if (m_WalkAudio != null) m_WalkAudio.Stop(); // 공통 친구가 보행 중일 때 켠 환경음과 발소리를 정리합니다.
            foreach (AudioSource source in new[] { m_HornAudio, m_BrakeAudio, m_TireAudio }) if (source != null) source.Stop(); // 이번 결과에서 시작한 공간 음향만 중단합니다.
            m_BrakeEffectActive = false; if (m_BrakeMarks != null) m_BrakeMarks.gameObject.SetActive(false); // 제동 자국을 감춰 반복 재시도에서 이전 효과가 남지 않게 합니다.
        } // 공유 전환 음향과 첫 번째 상황의 소리는 변경하지 않습니다.

        private void SetObservations(bool available) // 같은 친구가 빨간불에서 조사되도록 공통 관찰 목록을 제어합니다.
        { // 기존 신호·차량·횡단보도 컴포넌트도 이 상태를 따릅니다.
            foreach (FirstSituationObservation target in m_Observations) if (target != null) target.SetAvailable(available, FirstSituationObservation.Context.RedSignal); // 공통 친구에는 빨간불 생각 문구를 선택합니다.
        } // 관찰 상태를 벗어나면 모든 단서의 입력을 닫습니다.

        private void OnDisable() // 동행이나 위험 결과가 중단되면 저장한 상태를 사용해 정리합니다.
        { // 코루틴이나 예약된 정지 작업이 다음 활성화에 남지 않게 합니다.
            StopAllCoroutines(); m_QueueFreeze = false; // 중단된 연출이 다시 재생되더라도 기존 코루틴이 함께 실행되지 않습니다.
            if (!m_Started) return; // 첫 상황을 기다리는 동안에는 다른 제어기의 상태에 관여하지 않습니다.
            if (m_ViewInput != null) m_ViewInput.CancelScenarioViewTurn();
            if (m_SharedUI != null) m_SharedUI.SetMovementLetterbox(false);
            if (!m_Completed) // 세 번째 상황에 넘긴 친구와 차량은 다음 제어기가 계속 사용합니다.
            { // 횡단보도 도착 전이나 재시도 중에는 동행 전 상태로 돌아갑니다.
                SetObservations(false); // 빨간불 관찰의 입력과 윤곽만 정리합니다.
                if (m_CheckpointReady) RestoreObservationScene(); // 위험 횡단의 이동 제한과 차량 제동을 먼저 되돌립니다.
                StopResultEffects(); // 급정거 소리와 자국, 동행 발소리를 끕니다.
                if (m_ResetAudio != null) m_ResetAudio.Stop(); // 재도전 암전이 중단되었을 때 효과음도 끝냅니다.
                if (m_ResultUI != null) m_ResultUI.HideAll(); // 두 번째 상황에서 열린 패널을 닫습니다.
                if (m_Friend != null) { m_Friend.SetScenarioPaused(false); m_Friend.PlaceWaitingForScenario(m_FriendStart, m_FriendStartRotation); } // 공통 친구를 회피 완료 위치로 돌리고 Animator에 대기 자세를 적용합니다.
                if (m_FirstPerformance != null) m_FirstPerformance.Freeze(m_FriendWasPaused); // 동행 전 이동과 휴대전화의 정지 상태를 복원합니다.
                if (m_Origin != null) m_Origin.Origin.transform.SetPositionAndRotation(m_PlayerStart, m_PlayerStartRotation); // 회전 중 중단되어도 동행 전 위치와 방향을 복원합니다.
                if (m_Signal != null) m_Signal.SetScenarioPaused(m_SignalWasPaused); // 이 상황에서 설정하기 전의 신호 타이머 상태로 돌아갑니다.
                for (int i = 0; i < m_Cars.Length; ++i) if (m_Cars[i] != null) m_Cars[i].SetScenarioPaused(m_CarsWerePaused[i]); // 차량별 이전 정지 상태를 돌려줍니다.
                if (m_SharedUI != null) { m_SharedUI.HideAll(); m_SharedUI.Transition(false); } // 공유 관찰창과 재시도 암전이 남지 않게 합니다.
                m_State = StageState.WaitingForFirst; // 재활성화하면 정상적인 동행 시작점에서 다시 연결합니다.
            } // 완료한 뒤에는 공통 관찰·친구·차량 상태를 건드리지 않습니다.
            for (int i = 0; i < m_Locomotion.Length; ++i) if (m_Locomotion[i] != null) m_Locomotion[i].enabled = m_LocomotionEnabled[i]; // 해당 제어기가 보류한 직접 이동 설정을 반환합니다.
            m_CheckpointReady = false; m_Started = false; // 중단 전 관찰 지점이나 입력 소유권을 재사용하지 않습니다.
        } // 같은 제어기를 다시 켜도 동행이 한 번만 시작됩니다.

        private void OnEnable() // 명시적으로 다시 활성화하면 완료된 첫 상황에서 이어갑니다.
        { // 첫 활성화는 씬 UI가 준비된 Start에서 처리합니다.
            if (m_Initialized && m_First.IsCompleted) BeginAfterFirstSituation(); // 기존 진입점이 이미 완료한 상황의 중복 실행을 차단합니다.
        } // 친구 2의 다음 행동 선택은 여기에서 구현하지 않습니다.
    } // 두 번째 상황의 관찰 직전 도입을 마칩니다.
} // 초록불 상황과 사고 결과는 포함하지 않습니다.
