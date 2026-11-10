using System.Collections; // 신호 전환부터 관찰·선택·재시도·재회를 순서대로 연결합니다.
using TMPro;
using Unity.XR.CoreUtils; // 횡단 중 카메라 회전은 유지하면서 실제 플레이어 루트의 위치만 이동합니다.
using UnityEngine; // 기존 캐릭터·차량·신호·월드 UI를 제어합니다.
using UnityEngine.Events; // 안전 완료와 사용자의 최종 종료를 다른 진행 시스템에 알립니다.

namespace Y.Crosswalk // 첫 번째·두 번째 완료 기록과 독립된 마지막 상황입니다.
{ // 기존 입력·관찰·골격을 재사용해 신호가 바뀔 때의 차량 통과를 보여 줍니다.
    [DefaultExecutionOrder(500)] // 기존 이동·관절·차량의 최종 프레임 뒤에 관찰을 정지합니다.
    public sealed class ThirdSituationController : MonoBehaviour // 파란불 접근 차량의 전체 안전 체험을 진행합니다.
    { // 문구·위치·참조는 실제 씬 Inspector에서 편집합니다.
        public enum StageState { Waiting, Introduction, Observation, Choice, WaitingForCar, Crossing, Danger, Retry, [InspectorName("횡단 도착")] Arrival, Complete, Ended, [InspectorName("전체 체험 마무리")] OverallSummary, [InspectorName("전체 마무리로 전환 중")] OpeningOverallSummary } // 이전 재회 상태의 번호를 같은 친구와의 횡단 도착에 사용합니다.
        [SerializeField, InspectorName("첫 번째 완료 기록"), Tooltip("기존 전봇대 상황의 완료 상태를 읽기만 합니다.")] private FirstSituationController m_First; // 첫 번째 결과는 재시도에서 초기화하지 않습니다.
        [SerializeField, InspectorName("두 번째 안전 완료"), Tooltip("기존 두 번째 완료 이벤트를 BeginAfterSecondSituation에 연결합니다.")] private SecondSituationController m_Second; // 빨간불에서 안전하게 기다린 경우만 시작합니다.
        [SerializeField, InspectorName("공유 관찰·전환 UI"), Tooltip("기존 관찰 완료 버튼과 짧은 화면 전환입니다.")] private FirstSituationUI m_SharedUI; // 별도 관찰 입력 시스템을 만들지 않습니다.
        [SerializeField, InspectorName("세 번째 실제 패널"), Tooltip("기존 Canvas 아래에 배치한 선택·대사·결과·최종 안내입니다.")] private SecondSituationUI m_UI; // 같은 파스텔 표시·버튼 반응을 사용합니다.
        [SerializeField, InspectorName("세 번째 생각 말풍선"), Tooltip("실제 Hierarchy에 배치한 세 번째 생각 패널입니다.")] private FirstSituationPanel m_Thought; // 첫 두 상황의 생각 패널과 표시 상태를 분리합니다.
        [SerializeField, InspectorName("세 번째 생각 문구"), Tooltip("생각 말풍선 내부의 실제 TMP 텍스트입니다.")] private TMP_Text m_ThoughtText; // 클릭한 단서의 문구를 표시합니다.
        [SerializeField, InspectorName("기존 플레이어 카메라"), Tooltip("기존 XR Origin의 추적 카메라입니다. UI와 캐릭터 응시 기준으로만 사용합니다.")] private Camera m_Viewer; // 실제 카메라는 이동·회전·흔들지 않습니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("안전한 선택 뒤 공통 친구와 함께 횡단할 플레이어 루트입니다.")] private XROrigin m_Origin; // 시점 방향을 바꾸지 않고 이동 위치를 갱신합니다.
        [SerializeField, InspectorName("재사용 관찰 대상 네 개"), Tooltip("빨간불 때 조사한 공통 친구·신호·차량·횡단보도의 관찰 컴포넌트입니다.")] private FirstSituationObservation[] m_Observations; // 같은 단서가 파란불 상황에 맞는 생각 문구를 표시합니다.
        [SerializeField, InspectorName("공통 친구 이동"), Tooltip("빨간불 때 옆에서 대기한 친구의 이동과 기존 횡단 애니메이션입니다.")] private FriendCharacterMovement m_Friend; // 두 번째 상황의 친구 위치와 대기 상태에서 계속 진행합니다.
        [SerializeField, InspectorName("공통 친구 관절 연출"), Tooltip("기존 신호 확인·놀람·뒷걸음·감사 표현을 같은 친구에 적용합니다.")] private SecondSituationCharacterPerformance m_Performance; // 관찰 정지 자세와 위험 결과를 재사용합니다.
        [SerializeField, InspectorName("공통 친구 발소리"), Tooltip("앞선 보행과 같은 친구의 걸음에 동기화됩니다.")] private FirstSituationWalkAudio m_WalkAudio; // 실제 횡단하는 동안에만 발소리를 재생합니다.
        [SerializeField, InspectorName("기존 자동차"), Tooltip("기존 한 대의 차량을 같은 도로에서 재사용합니다.")] private TrafficCarMovement m_Car; // 첫 두 상황의 원래 주행 경로는 보관합니다.
        [SerializeField, InspectorName("자동차 몸 Collider"), Tooltip("기존 차량 전체의 관찰 Collider입니다.")] private Collider m_CarBody; // 차의 앞과 뒤가 횡단보도에 닿거나 빠져나가는 순간을 계산합니다.
        [SerializeField, InspectorName("기존 보행 신호"), Tooltip("기존 파란 사람 표시와 타이머를 유지합니다.")] private PedestrianTrafficLight m_Signal; // 횡단 완료까지 파란 신호를 유지합니다.
        [SerializeField, InspectorName("파란 신호 응시점"), Tooltip("기존 파란 사람 표시의 Transform입니다.")] private Transform m_GreenSignalPoint; // 친구가 실제 신호를 바라봅니다.
        [SerializeField, InspectorName("접근 차량 시작"), Tooltip("같은 도로의 건너편 차로 위 시작점입니다.")] private Transform m_CarEntry; // 화면 전환 중에만 기존 차량을 배치합니다.
        [SerializeField, InspectorName("접근 차로 끝"), Tooltip("기존 도로 끝의 같은 높이 지점입니다.")] private Transform m_CarExit; // 차량의 접근 방향을 명확하게 정합니다.
        [SerializeField, InspectorName("파란불 전환 위치"), Tooltip("다가오는 차가 이 위치를 지나면 보행 신호가 파란불로 바뀝니다.")] private Transform m_CarSignalChangePoint; // 차가 아직 접근 중인 시점에만 파란불을 켭니다.
        [SerializeField, Min(0.1f), InspectorName("통과 차량 속도(m/s)"), Tooltip("세 번째 상황에서 신호가 바뀐 뒤에도 차가 유지하는 접근 속도입니다.")] private float m_CarApproachSpeed = 8f; // 마지막 상황에만 일반 주행보다 빠른 통과 속도를 사용합니다.
        [SerializeField, Min(0.1f), InspectorName("횡단보도 반폭(m)"), Tooltip("친구가 건너는 선에서 차가 횡단보도를 완전히 빠져나갔다고 판단하는 거리입니다.")] private float m_CrosswalkHalfWidth = 1.9f; // 차량 뒤쪽까지 이 거리를 벗어나야 횡단할 수 있습니다.
        [SerializeField, InspectorName("횡단보도 중심선 기준"), Tooltip("전봇대 쪽 인도의 횡단 시작점입니다. 차량이 지나갔는지 판단할 때 이 지점의 차로 방향 좌표를 사용합니다.")] private Transform m_CurbPoint; // 앞뒤 범퍼 위치를 같은 횡단보도 기준으로 계산합니다.
        [SerializeField, InspectorName("위험 선택 접근 제한점"), Tooltip("가까운 인도에서 출발한 친구가 건너편 차로에 닿기 전에 멈출 지점입니다.")] private Transform m_NearMissPoint; // 위험 선택의 실제 이동이 차량의 측면까지 들어가지 않게 합니다.
        [SerializeField, InspectorName("도로 왼쪽 확인점"), Tooltip("횡단 전 반대 방향을 확인할 도로 위 응시점입니다.")] private Transform m_CheckLeft; // 실제 주변을 살펴보는 고개 동작을 연결합니다.
        [SerializeField, InspectorName("도로 오른쪽 확인점"), Tooltip("횡단 전 접근 방향을 재확인할 도로 위 응시점입니다.")] private Transform m_CheckRight; // 신호만 보고 출발하지 않는 행동을 보여 줍니다.
        [SerializeField, Min(2f), InspectorName("위험 설명 표시 시간"), Tooltip("설명 후 같은 관찰로 복원합니다. 버튼으로 먼저 재시도할 수도 있습니다.")] private float m_ResultDuration = 4f; // 관찰·선택에는 제한 시간을 두지 않습니다.
        [SerializeField, InspectorName("세 번째 진행 상태"), Tooltip("같은 장소의 신호 전환·관찰·선택·횡단·최종 완료를 구분합니다.")] private StageState m_State; // 현재 단계에 맞춰 자유 시점과 버튼 입력을 구분합니다.
        [SerializeField, InspectorName("세 번째 안전 완료"), Tooltip("차량이 지나간 뒤 친구와 반대편 인도에 도착해야 켜집니다.")] private bool m_Completed; // 실제 도착 전에는 마무리로 넘어가지 않습니다.
        [SerializeField, InspectorName("세 번째 완료 이벤트"), Tooltip("안전 횡단 뒤 기존 완료 화면을 표시할 때 한 번 호출합니다.")] private UnityEvent m_OnCompleted = new UnityEvent(); // 완료 후 다른 씬으로 연결할 기존 이벤트를 유지합니다.
        [SerializeField, InspectorName("사용자 종료 이벤트"), Tooltip("사용자가 체험 마치기를 선택했을 때 호출합니다.")] private UnityEvent m_OnFinished = new UnityEvent(); // 최종 화면은 자동 종료하지 않습니다.
        private bool m_Initialized, m_Started, m_QueueFreeze, m_HasCheckpoint, m_RetryRequested; // 관찰 진입과 재시도, 중단 후 재활성화를 분리합니다.
        private Vector3 m_ReadyPlayerPosition, m_ReadyFriendPosition; // 횡단을 취소하거나 재시도할 때 돌아갈 같은 쪽 인도 위치입니다.
        private Quaternion m_ReadyFriendRotation; // 세 번째 상황이 시작되기 직전 친구의 실제 대기 방향을 저장합니다.
        public StageState CurrentState => m_State; // PC 입력이 실제 단계 전환을 감지하여 이전 커서 잠금을 해제하도록 제공합니다.
        public bool IsCompleted => m_Completed; // 마지막 안전 행동이 끝난 사실만 외부에 제공합니다.
        public bool BlocksPlayerTranslation => isActiveAndEnabled && m_Started; // 기존 직접 이동은 추가하지 않고 체험 구도를 유지합니다.
        public bool AllowsLooking => m_Started; // PC 마우스와 기존 헤드셋의 자연스러운 둘러보기를 유지합니다.
        public bool IsObservationReady => isActiveAndEnabled && m_State == StageState.Observation; // 정지된 장면에서만 네 단서의 클릭을 허용합니다.
        public bool HasResultUIInput => isActiveAndEnabled && (m_State == StageState.Choice || m_State == StageState.Retry || m_State == StageState.Complete || m_State == StageState.OverallSummary || m_State == StageState.OpeningOverallSummary); // 선택·재시도·마무리 버튼도 실제 마우스로 클릭합니다.

        private void Start() // 실제 씬의 UI와 기존 캐릭터 초기화가 끝난 뒤 시작을 기다립니다.
        { // 두 번째 완료 이벤트가 아직 없으면 자동으로 건너뛰지 않습니다.
            m_Initialized = true; m_UI.HideAll(); m_Thought.Show(false); // 화면이 준비된 뒤 이전 상황 완료 이벤트를 받을 수 있게 합니다.
            if (m_Second.IsCompleted) BeginAfterSecondSituation(); // 완료 상태로 다시 연 경우에도 같은 진입점을 사용합니다.
        } // 관찰 대상은 두 번째 제어기가 소유하는 동안 건드리지 않습니다.

        public void BeginAfterSecondSituation() // 두 번째 안전 완료 이벤트가 호출하는 유일한 시작점입니다.
        { // 실패나 중복 클릭으로 세 번째 상황을 먼저 시작하지 않습니다.
            if (!isActiveAndEnabled || m_Started || m_Completed || !m_First.IsCompleted || !m_Second.IsCompleted) return; // 실제 앞선 두 안전 완료가 있어야 시작합니다.
            m_Started = true; m_State = StageState.Introduction; // 빨간불에 함께 기다리는 현재 장면에서 세 번째 흐름을 시작합니다.
            m_ReadyPlayerPosition = m_Origin.Origin.transform.position; // 횡단 결과를 재시도할 때 같은 인도 위치로 돌아갑니다.
            m_Friend.transform.GetPositionAndRotation(out m_ReadyFriendPosition, out m_ReadyFriendRotation); // 두 번째 상황이 끝난 친구의 위치와 방향을 그대로 이어받습니다.
            StartCoroutine(RunIntroduction()); // 같은 횡단보도에서 다음 접근 차량을 준비합니다.
        } // 앞선 상황의 완료 기록을 초기화하지 않습니다.

        private IEnumerator RunIntroduction() // 앞선 차가 지나가면 같은 장소에서 다음 차량과 파란불을 보여 줍니다.
        { // 친구나 플레이어의 위치를 바꾸는 장면 전환 없이 이어집니다.
            float clearDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 이전 주행이 중단되어도 도입이 무한 대기하지 않게 합니다.
            while (!m_Car.HasPassedCrosswalkForScenario && m_Car.isActiveAndEnabled && Time.unscaledTime < clearDeadline) yield return null; // 빨간불 동안 앞선 차가 횡단보도를 빠져나갈 시간을 줍니다.
            m_Second.HideCompletedResultForNextSituation(); // 친구의 이전 대사를 닫고 같은 화면에서 파란불에 집중하게 합니다.
            if (!m_Car.PrepareGreenScenarioRoute(m_CarEntry, m_CarExit, m_CarSignalChangePoint, m_CarApproachSpeed)) // 통과한 차량 인스턴스를 먼 반대 차로의 다음 접근에 재사용합니다.
            { // 경로 준비가 실패하면 대기 상태를 복원합니다.
                Debug.LogError("파란불 상황의 접근 차량 경로를 준비하지 못했습니다.", this); // 출발과 신호 전환 위치를 확인할 수 있게 합니다.
                enabled = false; yield break; // 친구와 플레이어는 앞선 대기 위치를 유지합니다.
            } // 차량은 새 접근 경로를 준비한 뒤에만 다시 움직입니다.
            m_Car.SetScenarioPaused(false); // 다음 차가 접근하는 동안 보행 신호는 빨간불을 유지합니다.
            float signalDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 다음 차가 전환 지점에 도착하지 못하는 경우를 제한합니다.
            while (!m_Car.HasReachedGreenSignalChangePoint && m_Car.isActiveAndEnabled && Time.unscaledTime < signalDeadline) yield return null; // 실제 접근 위치를 기준으로 신호를 바꿉니다.
            if (!m_Car.HasReachedGreenSignalChangePoint) // 차량이 없는 상태에서 파란불 위험 선택을 열지 않습니다.
            { // 진행이 중단된 경우 기존 상태를 정리합니다.
                Debug.LogError("차량이 보행 신호 전환 위치에 도착하지 못했습니다.", this); // 도입을 이어갈 수 없었던 이동 상태를 알립니다.
                enabled = false; yield break; // 다음 활성화 때 빨간불 대기부터 다시 시작할 수 있게 합니다.
            } // 차가 접근하는 순간에만 다음 관찰을 준비합니다.
            m_Signal.HoldGreenForScenario(); // 친구가 기다리던 자리에서 신호가 파란불로 바뀝니다.
            BlenderRigPose.PlayFace(m_Performance.HeadTarget, 0.6f); // 짧은 발화 동작을 기존 친구의 얼굴에 적용합니다.
            const float firstStepTime = 0.14f; // 짧은 첫 발만 보여 주고 차가 아직 접근 중일 때 관찰을 정지합니다.
            if (!m_Friend.TryBeginScenarioFirstStep(m_GreenSignalPoint, firstStepTime)) // 앞서 대기하던 상태에서 횡단하려는 첫 발을 시작합니다.
            { // 첫 발이 준비되지 않으면 잘못된 자세로 선택을 열지 않습니다.
                Debug.LogError("파란불에서 공통 친구의 첫 발 자세를 준비하지 못했습니다.", this); // 앞선 기다리기와 횡단 경로 연결을 확인할 단서를 남깁니다.
                enabled = false; yield break; // 친구와 차량을 두 번째 상황 종료 지점으로 되돌립니다.
            } // 첫 발과 차량 이동은 같은 프레임에 멈춥니다.
            yield return new WaitForSecondsRealtime(firstStepTime); m_QueueFreeze = true; // 골격 갱신 뒤 현재 자세와 차량 위치를 저장합니다.
        } // 차가 지나가기 전에 친구의 첫 발과 접근 차량을 함께 관찰할 수 있게 합니다.

        private void LateUpdate() // 첫 발과 접근 차량의 최종 프레임을 동시에 보관합니다.
        { // 시간 배율을 정지시키지 않습니다.
            if (!m_QueueFreeze) return; // 도입의 첫 발 순간에만 저장합니다.
            m_QueueFreeze = false; m_Friend.SetScenarioPaused(true); m_Car.SetScenarioPaused(true); m_Signal.HoldGreenForScenario(); // 한 명의 친구와 다가오는 차량, 파란불을 같은 관찰 상태로 멈춥니다.
            m_Performance.CaptureObservation(); m_Car.CaptureScenarioCheckpoint(); m_HasCheckpoint = true; // 같은 세 번째 순간의 캐릭터와 차량 상태를 저장합니다.
            EnterObservation(); // 첫 발을 정지한 상태에서 관찰 조작을 엽니다.
        } // 앞선 두 상황의 체크포인트로 돌아가지 않습니다.

        private void SetObservations(bool available) // 같은 관찰 인스턴스에 세 번째 문구를 선택합니다.
        { // Collider와 실루엣 Renderer를 복제하지 않습니다.
            foreach (FirstSituationObservation target in m_Observations) if (target != null) target.SetAvailable(available, FirstSituationObservation.Context.GreenSignal); // 기존 조사 입력에 파란불 상황의 생각 문구를 연결합니다.
        } // 단서를 모두 조사했는지 세지 않습니다.
        private void EnterObservation() // 도입과 실패 복원에서 같은 정지·관찰 상태로 진입합니다.
        { // UI는 실제 씬의 패널만 표시합니다.
            m_UI.HideAll(); m_RetryRequested = false; m_State = StageState.Observation; SetObservations(true); // 이전 결과 입력을 지우고 세 번째 단서만 엽니다.
            m_SharedUI.PlaceInFront(m_Viewer.transform); m_SharedUI.ShowObservation(OpenFriendChoices, m_Thought, m_ThoughtText); // 접근하는 차량을 조사하거나 친구에게 곧바로 말을 걸 수 있게 합니다.
            m_SharedUI.ShowFriendDialogue("파란불이다! 이제 건너갈까?", m_Performance.HeadPosition); // 재시도와 다시 둘러보기에서도 같은 질문을 표시하고 차를 조사하면 접근 단서를 보여 줍니다.
        } // 관찰 시간 제한은 없습니다.
        private void OpenFriendChoices() // 친구를 눌렀을 때 차량을 기다릴지 바로 건널지 선택하게 합니다.
        { // 조사한 단서 수와 관계없이 친구에게 답할 수 있습니다.
            if (!IsObservationReady) return; // 이미 대화가 열렸거나 차량 결과가 진행 중이면 새 선택을 시작하지 않습니다.
            m_State = StageState.Choice; SetObservations(false); m_SharedUI.HideAll(); m_UI.ShowChoice(); // 외곽선과 생각을 부드럽게 숨기고 실제 두 선택지를 표시합니다.
        } // 자동으로 정답을 선택하지 않습니다.
        public void ReturnToObservation() { if (m_State == StageState.Choice) EnterObservation(); } // 선택을 확정하기 전에는 마우스로 다시 주변을 볼 수 있습니다.
        public void ChooseSafe() // 차량이 다 지나갈 때까지 기다리기 선택을 한 번만 수락합니다.
        { // 상태를 코루틴보다 먼저 잠급니다.
            if (m_State != StageState.Choice) return; // 중복 좌클릭과 결과 중 입력은 무시합니다.
            m_State = StageState.WaitingForCar; SetObservations(false); m_SharedUI.HideAll(); m_UI.HideAll(); StartCoroutine(RunSafe()); // 같은 버튼으로 다른 결과를 동시에 실행하지 않습니다.
        } // 실제 자동차 정지가 뒤따라야 횡단할 수 있습니다.
        public void ChooseUnsafe() // 파란불만 믿고 건너오기 선택을 한 번만 수락합니다.
        { // 일반 신호 자동 대기와 별도의 명시적인 위험 연출입니다.
            if (m_State != StageState.Choice) return; // 관찰이나 이미 시작한 결과에서는 받지 않습니다.
            m_State = StageState.Danger; SetObservations(false); m_SharedUI.HideAll(); m_UI.HideAll(); StartCoroutine(RunDanger()); // 기존 캐릭터와 차량의 위험 결과만 시작합니다.
        } // 앞선 두 상황은 다시 실행하지 않습니다.
        public void RequestRetry() { if (m_State == StageState.Retry) m_RetryRequested = true; } // 사고 원인 설명 중에만 빠른 관찰 복원을 요청합니다.

        private IEnumerator RunSafe() // 차량이 그대로 지나간 뒤 주변을 확인하고 기존 횡단을 재생합니다.
        { // 파란불 시험용 타이머는 횡단이 끝날 때까지 보류합니다.
            m_UI.ShowWarning(m_Performance.HeadPosition); m_Signal.HoldGreenForScenario(); m_Car.SetScenarioPaused(false); // 친구 옆에 경고를 띄우고 차량이 횡단보도를 통과하게 합니다.
            m_Friend.StopScenarioFirstStep(); // 차가 접근하는 동안 친구는 내밀었던 첫 발을 대기 자세로 거두고 인도에 머뭅니다.
            m_Performance.BeginLookingAt(m_Car.transform); // 움직이는 실제 자동차를 바라보기 시작합니다.
            float clearDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 차가 통과하지 못했을 때 관찰로 돌아갈 상한을 잡습니다.
            while (CarBumperProgress(true) < m_CrosswalkHalfWidth && m_Car.isActiveAndEnabled && Time.unscaledTime < clearDeadline) yield return null; // 친구가 차를 바라보는 동안 뒷범퍼가 횡단보도를 벗어날 때까지 기다립니다.
            m_Performance.StopLookingAt(); // 실제 통과가 끝난 뒤에만 자동차에서 시선을 떼고 좌우 확인을 준비합니다.
            if (CarBumperProgress(true) < m_CrosswalkHalfWidth) { Debug.LogWarning("차량이 횡단보도를 완전히 빠져나가지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 차량이 남아 있으면 누구도 횡단을 시작하지 않습니다.
            m_UI.HideAll(); // 경고가 횡단 장면을 가리지 않게 닫습니다.
            yield return m_Performance.LookAtTarget(m_CheckLeft, 0.4f); // 왼쪽 확인은 고개로 보여 주고 몸 전체가 제자리에서 도는 대기를 줄입니다.
            yield return m_Performance.LookAtTarget(m_CheckRight, 0.4f); // 오른쪽도 확인한 뒤 바로 같은 친구의 횡단을 시작합니다.
            m_Friend.SetScenarioApproachLimit(null); m_Friend.SetScenarioApproachSpeed(1.35f); // 위험 선택 제한점 없이 반대편 인도까지 걸어가게 합니다.
            Vector3 friendStart = m_Friend.transform.position; // 같은 쪽 인도에서 출발한 친구를 기준으로 플레이어 이동량을 계산합니다.
            if (CarBumperProgress(true) < m_CrosswalkHalfWidth || !m_Friend.TryBeginCrossing()) // 통과 상태와 횡단 경로가 유효한 경우에만 둘 다 이동합니다.
            { // 안전 조건이나 이동이 거절되면 마지막 관찰로 돌아갑니다.
                Debug.LogError("차량 통과 확인 후 공통 친구의 안전 횡단을 시작하지 못했습니다.", this); yield return RestoreObservation(); yield break; // 결과가 시작되지 않은 상태에서 완료 처리하지 않습니다.
            } // 실제 횡단이 시작되면 발소리와 플레이어 이동을 함께 연결합니다.
            m_State = StageState.Crossing; m_WalkAudio.SetActive(true); // 횡단 중에도 플레이어는 주위를 자유롭게 볼 수 있습니다.
            float crossingDeadline = Time.unscaledTime + m_Friend.ScenarioTravelBudget; // 횡단 경로가 중단된 경우 복원하기 위한 최대 대기 시간입니다.
            while (!m_Friend.HasCompletedCrossing && m_Friend.isActiveAndEnabled && Time.unscaledTime < crossingDeadline) // 친구 이동기가 반대편 인도에 도착했다고 기록할 때까지만 기다립니다.
            { // 같은 거리와 높이 변화를 적용해 플레이어가 친구 옆의 흰 줄을 따라 이동합니다.
                m_Origin.Origin.transform.position = m_ReadyPlayerPosition + (m_Friend.transform.position - friendStart); // 루트 위치만 움직여 사용자가 보고 있는 방향을 유지합니다.
                yield return null; // 다음 실제 보행 이동에 맞춰 플레이어를 갱신합니다.
            } // 별도의 정지 시간이나 애니메이션 종료 신호로 도착을 추정하지 않습니다.
            if (!m_Friend.HasCompletedCrossing) { Debug.LogWarning("친구가 횡단 도착점에 이르지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 이동이 끊긴 상태로 체험을 완료하지 않습니다.
            m_Origin.Origin.transform.position = m_ReadyPlayerPosition + (m_Friend.transform.position - friendStart); // 감속한 마지막 이동까지 같은 인도에 도착한 플레이어 위치에 반영합니다.
            m_WalkAudio.Stop(); m_State = StageState.Arrival; // 같은 친구와 함께 도착했으므로 재회용 이동과 손짓을 거치지 않습니다.
            m_Friend.FaceScenarioTarget(m_Viewer.transform); // 도착한 친구가 옆의 플레이어를 자연스럽게 돌아봅니다.
            m_UI.ShowThanks(m_Performance.HeadPosition); // 최종 결과 전에 친구의 감사 대사를 짧게 표시합니다.
            StartCoroutine(m_Performance.AcknowledgeWaiting()); // 감사 동작은 결과 진행과 나란히 재생합니다.
            yield return new WaitForSecondsRealtime(1.1f); // 애니메이션 상태와 별개로 감사 대사를 읽을 시간만 확보합니다.
            m_Completed = true; m_State = StageState.Complete; m_UI.ShowSafe(); m_OnCompleted.Invoke(); // 도착으로 완료를 확정하고 기존 전체 마무리 버튼을 표시합니다.
        } // 이 흐름은 두 번째 친구의 재회 연출에 의존하지 않습니다.

        private IEnumerator RunDanger() // 파란불만 보고 서둘러 내딛은 친구 옆으로 차량이 그대로 지나가는 상황을 보여 줍니다.
        { // 친구의 실제 보행은 차량 옆면에 닿기 전 차도 가장자리까지만 이어집니다.
            m_Signal.HoldGreenForScenario(); m_Friend.SetScenarioApproachSpeed(2.2f); // 파란불만 보고 서두르는 위험 선택을 같은 친구의 이동으로 표현합니다.
            if (!m_Friend.TryBeginUnsafeScenarioCrossing(m_NearMissPoint)) // 가까운 인도에서 접근 차량 앞까지 기존 위험 횡단을 사용합니다.
            { // 잘못된 배치를 억지로 통과시키지 않습니다.
                Debug.LogError("세 번째 위험 접근의 첫 발 또는 차도 가장자리 위치를 준비하지 못했습니다.", this); yield return RestoreObservation(); yield break; // 준비에 실패하면 같은 관찰에서 다시 선택할 수 있게 합니다.
            } // 준비가 끝난 경우에만 차량과 발소리를 재개합니다.
            m_Car.SetScenarioPaused(false); m_WalkAudio.SetActive(true); // 관찰 당시의 위치와 속도에서 두 배우를 함께 재개합니다.
            float deadline = Time.unscaledTime + Mathf.Max(m_Car.ScenarioTravelBudget, m_Friend.ScenarioTravelBudget); // 차량과 친구의 실제 이동이 제한 시간 안에 닿는지 판단합니다.
            while ((m_Friend.ScenarioLimitRemaining > 0.12f || CarBumperProgress(false) < -m_CrosswalkHalfWidth) && m_Friend.isActiveAndEnabled && m_Car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 친구가 차량 앞 제한점에 닿고 차량이 횡단보도에 접근할 때 놀람을 시작합니다.
            if (m_Friend.ScenarioLimitRemaining > 0.12f || CarBumperProgress(false) < -m_CrosswalkHalfWidth) { Debug.LogWarning("차량 또는 친구의 접근이 끝나지 않아 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 사고 연출 시작 조건을 못 채우면 같은 선택을 다시 할 수 있게 합니다.
            m_WalkAudio.Stop(); m_SharedUI.PlayDangerCue(); // 다가온 차를 뒤늦게 발견한 순간에 발소리를 멈추고 기존 위험 표시를 보여 줍니다.
            yield return m_Performance.PanicNearCar(); // 차량은 차로를 그대로 지나가고 친구는 기존 놀람 자세와 뒷걸음으로 물러납니다.
            m_State = StageState.Retry; m_UI.ShowUnsafe(); float elapsed = 0f; // 그대로 지나간 차를 보고 파란불에도 주변을 확인해야 한다고 안내합니다.
            while (elapsed < m_ResultDuration && !m_RetryRequested) { elapsed += Time.unscaledDeltaTime; yield return null; } // 설명 뒤 자동 복원하거나 버튼으로 먼저 재시도합니다.
            yield return RestoreObservation(); // 첫 두 상황이 아닌 세 번째 첫 발과 차량만 복원합니다.
        } // 카메라 진동·유혈·신체 훼손은 사용하지 않습니다.

        private float CarBumperProgress(bool rear) // 횡단 중심을 기준으로 실제 차량의 앞이나 뒤가 진행 방향으로 얼마나 지났는지 구합니다.
        { // 차로가 어느 쪽을 향해도 같은 계산으로 횡단보도의 양쪽 경계를 판단합니다.
            if (m_CarBody == null || m_Car == null) return float.NegativeInfinity; // 차량이 제거된 경우 통과가 확인됐다고 처리하지 않습니다.
            Vector3 forward = m_Car.transform.forward; // 준비된 차로에서 차량이 움직이는 방향을 가져옵니다.
            Bounds body = m_CarBody.bounds; // 차체 전체에 맞춰 씬에 저장된 관찰 콜라이더의 실제 위치와 길이를 사용합니다.
            float halfLength = Vector3.Dot(body.extents, new Vector3(Mathf.Abs(forward.x), 0f, Mathf.Abs(forward.z))); // 차체 경계의 절반 길이를 주행 방향에 투영합니다.
            return Vector3.Dot(body.center - m_CurbPoint.position, forward) + (rear ? -halfLength : halfLength); // 차체 중심에서 요청한 범퍼만큼 이동한 지점의 횡단 진행을 반환합니다.
        } // 안전 선택은 뒤쪽을, 위험 접근은 앞쪽을 기준으로 판단합니다.
        private IEnumerator RestoreObservation() // 같은 세 번째 상황을 부드럽게 다시 관찰할 수 있게 합니다.
        { // 앞선 완료 이벤트나 도입을 다시 호출하지 않습니다.
            m_UI.HideAll(); m_SharedUI.HideAll(); m_SharedUI.Transition(true); m_Second.PlaySharedResetSound(); // 기존 짧은 복원 암전과 음향을 사용합니다.
            yield return new WaitForSecondsRealtime(m_SharedUI.FadeCoverDuration); // 위치·자세 복원은 가려진 뒤에만 수행합니다.
            StopEffects(); m_Performance.RestoreObservation(); m_Car.RestoreScenarioCheckpoint(); m_Signal.HoldGreenForScenario(); // 위험 횡단 전 친구와 차량, 신호를 같은 프레임으로 복원합니다.
            m_Origin.Origin.transform.position = m_ReadyPlayerPosition; // 안전 횡단 도중 이동이 중단되어도 기존 인도 관찰 위치로 돌아갑니다.
            EnterObservation(); m_SharedUI.Transition(false); // 기존 마우스로 다시 조사하고 두 선택지를 고를 수 있게 합니다.
        } // 플레이어 카메라를 되감거나 이동시키지 않습니다.
        private void StopEffects() // 이전 사고의 소리와 자국이 다음 선택에 남지 않게 합니다.
        { // 먼저 해제된 구성요소에는 접근하지 않습니다.
            if (m_WalkAudio != null) m_WalkAudio.Stop(); // 이번 상황에서 재생한 친구의 보행 음향을 중단합니다.
            if (m_Second != null) m_Second.StopSharedBrakeEffects(); // 같은 기존 차량의 경적·제동·타이어 자국을 초기화합니다.
        } // 앞선 완료 기록은 그대로 유지합니다.

        public void ShowOverallSummary() // 세 번째 상황 마무리에서 사용자가 전체 마무리 보기를 누른 경우만 전환합니다.
        { // 친구 재회와 감사 이전에는 전체 완료를 표시하지 않습니다.
            if (!m_Completed || m_State != StageState.Complete) return; // 중복 클릭과 미완료 상태의 이벤트는 무시합니다.
            m_State = StageState.OpeningOverallSummary; m_UI.HideAll(); StartCoroutine(OpenOverallSummaryAfterFade()); // 중복 클릭을 막고 세 번째 결과가 완전히 사라진 뒤 다음 화면을 표시합니다.
        } // 사용자가 읽고 직접 종료할 때까지 화면을 유지합니다.

        private IEnumerator OpenOverallSummaryAfterFade() // 두 마무리 화면의 글자가 전환 도중에도 서로 겹치지 않게 합니다.
        { // 실제 Inspector의 패널 전환 시간을 사용합니다.
            yield return new WaitForSecondsRealtime(m_UI.CompletionTransitionDuration + 0.03f); // 세 번째 완료 패널의 퇴장이 끝날 때까지 기다립니다.
            m_State = StageState.OverallSummary; m_UI.ShowOverallSummary(); // 전체 수칙과 종료 버튼이 있는 실제 화면만 표시합니다.
        } // 사용자가 누르기 전에는 이 전환을 자동 실행하지 않습니다.

        public void FinishExperience() // 자동으로 사라지지 않는 최종 패널의 실제 종료 버튼입니다.
        { // 세 번째 안전 완료 뒤 사용자가 직접 누른 경우만 종료합니다.
            if (m_State != StageState.OverallSummary) return; // 연출 중 중복 이벤트나 미완료 종료를 받지 않습니다.
            m_State = StageState.Ended; m_UI.HideAll(); m_SharedUI.HideAll(); StopEffects(); m_OnFinished.Invoke(); // 종료 상태를 먼저 확정하여 한 번만 처리합니다.
#if UNITY_EDITOR // 개발 중에는 사용자의 종료 버튼으로만 플레이를 끝냅니다.
            UnityEditor.EditorApplication.isPlaying = false; // 에디터를 종료하지 않고 현재 체험 재생만 마칩니다.
#else // 실제 배포 앱에서는 같은 사용자 선택으로 체험을 종료합니다.
            Application.Quit(); // PC·VR 빌드의 애플리케이션을 정상 종료합니다.
#endif // 실행·종료 처리는 사용자가 실제 버튼을 눌렀을 때만 수행합니다.
        } // 자동 종료나 새 스테이지 이동은 없습니다.

        private void OnDisable() // 세 번째 상황 도중 중단되면 같은 인도에서 재개할 상태를 정리합니다.
        { // 이미 도착했다면 완료한 횡단 위치는 보존합니다.
            StopAllCoroutines(); m_QueueFreeze = false; if (!m_Started) return; // 종료된 연출이 관찰을 다시 열지 않도록 예약을 지웁니다.
            SetObservations(false); StopEffects(); if (m_UI != null) m_UI.HideAll(); if (m_Thought != null) m_Thought.Show(false); // 세 번째 상황이 사용한 조사 입력, 소리와 화면을 닫습니다.
            if (m_Performance != null) BlenderRigPose.ResetFace(m_Performance.HeadTarget); // 정지 중인 친구의 대사 얼굴을 정리합니다.
            if (!m_Completed) // 다시 시작할 경우 빨간불에서 기다린 위치를 기준으로 이어갑니다.
            { // 위험 횡단 플래그와 차량 경로부터 원래 상태로 되돌립니다.
                if (m_HasCheckpoint && m_Performance != null) m_Performance.RestoreObservation(); // 기존 복원 경로로 같은 친구의 횡단과 자세 상태를 초기화합니다.
                if (m_Car != null) m_Car.RestoreBeforeGreenScenario(); // 먼저 지나가던 차량의 경로와 위치를 돌려줍니다.
                if (m_Friend != null) { m_Friend.SetScenarioPaused(false); m_Friend.PlaceWaitingForScenario(m_ReadyFriendPosition, m_ReadyFriendRotation); } // 두 번째 상황의 실제 대기 위치에 같은 친구를 복원합니다.
                if (m_Origin != null) m_Origin.Origin.transform.position = m_ReadyPlayerPosition; // 플레이어를 함께 대기한 쪽 인도에 복원합니다.
                if (m_Signal != null) m_Signal.HoldRedForScenario(); // 재진입 때 파란불 전환을 다시 보여 주기 위해 빨간불을 유지합니다.
                m_State = StageState.Waiting; // 완료한 빨간불 상황에서 다시 진입할 수 있게 합니다.
            } // 완료 화면을 닫는 경우에는 이동 결과를 되돌리지 않습니다.
            if (m_SharedUI != null) { m_SharedUI.HideAll(); m_SharedUI.Transition(false); } // 관찰 말풍선이나 재시도 암전이 남지 않게 합니다.
            m_HasCheckpoint = false; m_Started = false; // 이전 실행의 관찰 상태를 다음 실행에서 사용하지 않습니다.
        } // 다시 활성화할 때는 새 관찰 자세를 저장합니다.

        private void OnEnable() // 명시적으로 다시 활성화된 경우 앞선 완료 상태에서 시작합니다.
        { // 처음 활성화할 때는 UI 준비를 위해 Start를 기다립니다.
            if (m_Initialized && m_Second.IsCompleted) BeginAfterSecondSituation(); // 실제로 완료한 상황은 기존 진입 조건이 중복 실행을 막습니다.
        } // 완료 이벤트와 재활성화가 같은 진입점을 사용합니다.
    } // 세 번째이자 마지막 보행 안전 상황을 마칩니다.
} // 점수·도장·새로운 보상 스테이지는 포함하지 않습니다.
