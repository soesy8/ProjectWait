using System.Collections; // 신호 전환부터 관찰·선택·재시도·재회를 순서대로 연결합니다.
using TMPro; // 실제 씬에 저장한 세 번째 생각 말풍선의 문구를 연결합니다.
using UnityEngine; // 기존 캐릭터·차량·신호·월드 UI를 제어합니다.
using UnityEngine.Events; // 안전 완료와 사용자의 최종 종료를 다른 진행 시스템에 알립니다.

namespace Y.Crosswalk // 첫 번째·두 번째 완료 기록과 독립된 마지막 상황입니다.
{ // 기존 입력·관찰·골격을 재사용해 신호가 바뀔 때의 차량 통과를 보여 줍니다.
    [DefaultExecutionOrder(500)] // 기존 이동·관절·차량의 최종 프레임 뒤에 관찰을 정지합니다.
    public sealed class ThirdSituationController : MonoBehaviour // 파란불 접근 차량의 전체 안전 체험을 진행합니다.
    { // 문구·위치·참조는 실제 씬 Inspector에서 편집합니다.
        public enum StageState { Waiting, Introduction, Observation, Choice, WaitingForCar, Crossing, Danger, Retry, Reunion, Complete, Ended, [InspectorName("전체 체험 마무리")] OverallSummary, [InspectorName("전체 마무리로 전환 중")] OpeningOverallSummary } // 완료 전 두 결과를 동시에 실행하지 않도록 상태를 구분합니다.
        [SerializeField, InspectorName("첫 번째 완료 기록"), Tooltip("기존 전봇대 상황의 완료 상태를 읽기만 합니다.")] private FirstSituationController m_First; // 첫 번째 결과는 재시도에서 초기화하지 않습니다.
        [SerializeField, InspectorName("두 번째 안전 완료"), Tooltip("기존 두 번째 완료 이벤트를 BeginAfterSecondSituation에 연결합니다.")] private SecondSituationController m_Second; // 빨간불에서 안전하게 기다린 경우만 시작합니다.
        [SerializeField, InspectorName("공유 관찰·전환 UI"), Tooltip("기존 관찰 완료 버튼과 짧은 화면 전환입니다.")] private FirstSituationUI m_SharedUI; // 별도 관찰 입력 시스템을 만들지 않습니다.
        [SerializeField, InspectorName("세 번째 실제 패널"), Tooltip("기존 Canvas 아래에 배치한 선택·대사·결과·최종 안내입니다.")] private SecondSituationUI m_UI; // 같은 파스텔 표시·버튼 반응을 사용합니다.
        [SerializeField, InspectorName("세 번째 생각 말풍선"), Tooltip("실제 Hierarchy에 배치한 세 번째 생각 패널입니다.")] private FirstSituationPanel m_Thought; // 첫 두 상황의 생각 패널과 표시 상태를 분리합니다.
        [SerializeField, InspectorName("세 번째 생각 문구"), Tooltip("생각 말풍선 내부의 실제 TMP 텍스트입니다.")] private TMP_Text m_ThoughtText; // 클릭한 단서의 문구를 표시합니다.
        [SerializeField, InspectorName("기존 플레이어 카메라"), Tooltip("기존 XR Origin의 추적 카메라입니다. UI와 캐릭터 응시 기준으로만 사용합니다.")] private Camera m_Viewer; // 실제 카메라는 이동·회전·흔들지 않습니다.
        [SerializeField, InspectorName("재사용 관찰 대상 네 개"), Tooltip("친구 2·보행 신호·기존 차량·횡단보도의 동일한 관찰 인스턴스입니다.")] private FirstSituationObservation[] m_Observations; // 같은 Collider·외곽선을 사용하고 세 번째 문구만 선택합니다.
        [SerializeField, InspectorName("친구 1 이동"), Tooltip("현재 플레이어 곁의 친구 1 이동입니다.")] private FriendCharacterMovement m_Friend1; // 재회 시 친구 2를 바라봅니다.
        [SerializeField, InspectorName("친구 1 개별 정지"), Tooltip("기존 첫 번째 캐릭터 자세 제어입니다. 완료 기록이나 체크포인트는 변경하지 않습니다.")] private FirstSituationCharacterPerformance m_Friend1Performance; // 관찰에서는 현재 대기 자세를 유지합니다.
        [SerializeField, InspectorName("친구 1 재회 손짓"), Tooltip("기존 호출 손짓 컴포넌트를 친구 1의 관절에 연결한 인스턴스입니다.")] private SecondSituationFriendGreeting m_Friend1Greeting; // 기존 호출과 같은 미소·팔 흔들기를 재회 장면에서도 사용합니다.
        [SerializeField, InspectorName("친구 2 이동"), Tooltip("기존 횡단 경로와 보행 애니메이션입니다.")] private FriendCharacterMovement m_Friend2; // 접근 차량이 완전히 지나간 뒤 안전 횡단을 시작합니다.
        [SerializeField, InspectorName("친구 2 관절 연출"), Tooltip("기존 신호 확인·손짓·놀람·뒷걸음·감사 표현입니다.")] private SecondSituationCharacterPerformance m_Performance; // 새 사람형 모델을 만들지 않습니다.
        [SerializeField, InspectorName("친구 2 발소리"), Tooltip("기존 친구 2의 보폭에 동기화된 소리입니다.")] private FirstSituationWalkAudio m_WalkAudio; // 실제 횡단 보행과 결과를 연결합니다.
        [SerializeField, InspectorName("기존 자동차"), Tooltip("기존 한 대의 차량을 같은 도로에서 재사용합니다.")] private TrafficCarMovement m_Car; // 첫 두 상황의 원래 주행 경로는 보관합니다.
        [SerializeField, InspectorName("자동차 몸 Collider"), Tooltip("기존 차량 전체의 관찰 Collider입니다.")] private Collider m_CarBody; // 차의 앞과 뒤가 횡단보도에 닿거나 빠져나가는 순간을 계산합니다.
        [SerializeField, InspectorName("기존 보행 신호"), Tooltip("기존 파란 사람 표시와 타이머를 유지합니다.")] private PedestrianTrafficLight m_Signal; // 횡단 완료까지 파란 신호를 유지합니다.
        [SerializeField, InspectorName("파란 신호 응시점"), Tooltip("기존 파란 사람 표시의 Transform입니다.")] private Transform m_GreenSignalPoint; // 친구가 실제 신호를 바라봅니다.
        [SerializeField, InspectorName("접근 차량 시작"), Tooltip("같은 도로의 건너편 차로 위 시작점입니다.")] private Transform m_CarEntry; // 화면 전환 중에만 기존 차량을 배치합니다.
        [SerializeField, InspectorName("접근 차로 끝"), Tooltip("기존 도로 끝의 같은 높이 지점입니다.")] private Transform m_CarExit; // 차량의 접근 방향을 명확하게 정합니다.
        [SerializeField, InspectorName("파란불 전환 위치"), Tooltip("다가오는 차가 이 위치를 지나면 보행 신호가 파란불로 바뀝니다.")] private Transform m_CarSignalChangePoint; // 차가 아직 접근 중인 시점에만 파란불을 켭니다.
        [SerializeField, Min(0.1f), InspectorName("통과 차량 속도(m/s)"), Tooltip("세 번째 상황에서 신호가 바뀐 뒤에도 차가 유지하는 접근 속도입니다.")] private float m_CarApproachSpeed = 8f; // 마지막 상황에만 일반 주행보다 빠른 통과 속도를 사용합니다.
        [SerializeField, Min(0.1f), InspectorName("횡단보도 반폭(m)"), Tooltip("친구가 건너는 선에서 차가 횡단보도를 완전히 빠져나갔다고 판단하는 거리입니다.")] private float m_CrosswalkHalfWidth = 1.9f; // 차량 뒤쪽까지 이 거리를 벗어나야 횡단할 수 있습니다.
        [SerializeField, InspectorName("친구 2 첫 발 대기점"), Tooltip("건너편 인도 끝의 기존 대기 위치입니다.")] private Transform m_CurbPoint; // 관찰 시작 전에 도로에 들어가지 않습니다.
        [SerializeField, InspectorName("위험 선택 접근 제한점"), Tooltip("친구가 차도에 발을 내딛되 달리는 차량의 옆면에 닿기 전에 멈출 지점입니다.")] private Transform m_NearMissPoint; // 기존 횡단 보행을 차도 가장자리에서 제한합니다.
        [SerializeField, InspectorName("기존 횡단 도착점"), Tooltip("기존 플레이어 쪽 인도의 횡단 도착점입니다.")] private Transform m_ArrivalPoint; // 도착 후 추가 이동 없이 플레이어를 바라봅니다.
        [SerializeField, InspectorName("도로 왼쪽 확인점"), Tooltip("횡단 전 반대 방향을 확인할 도로 위 응시점입니다.")] private Transform m_CheckLeft; // 실제 주변을 살펴보는 고개 동작을 연결합니다.
        [SerializeField, InspectorName("도로 오른쪽 확인점"), Tooltip("횡단 전 접근 방향을 재확인할 도로 위 응시점입니다.")] private Transform m_CheckRight; // 신호만 보고 출발하지 않는 행동을 보여 줍니다.
        [SerializeField, InspectorName("두 번째 상황 대기점"), Tooltip("중간에 제어기를 해제할 때 복원할 기존 건너편 인도 대기점입니다.")] private Transform m_PreviousWaitingPoint; // 이전 완료 상황의 배치를 보존합니다.
        [SerializeField, Min(1f), InspectorName("두 번째 완료 표시 시간"), Tooltip("기존 안전 결과를 읽은 뒤 세 번째 상황을 준비할 시간입니다.")] private float m_SecondResultHold = 1.8f; // 두 번째 감사와 결과를 생략하지 않습니다.
        [SerializeField, Range(0.12f, 0.3f), InspectorName("접근 장면 전환 시간"), Tooltip("같은 장소에서 다음 차량 접근을 준비할 때만 짧게 가리는 시간입니다.")] private float m_TransitionDuration = 0.18f; // 시점 이동 없이 시간 경과만 부드럽게 표현합니다.
        [SerializeField, Min(2f), InspectorName("위험 설명 표시 시간"), Tooltip("설명 후 같은 관찰로 복원합니다. 버튼으로 먼저 재시도할 수도 있습니다.")] private float m_ResultDuration = 4f; // 관찰·선택에는 제한 시간을 두지 않습니다.
        [SerializeField, InspectorName("세 번째 진행 상태"), Tooltip("도입·관찰·선택·결과·재회·최종 완료를 구분합니다.")] private StageState m_State; // 다른 상황의 진행 상태와 분리합니다.
        [SerializeField, InspectorName("세 번째 안전 완료"), Tooltip("차량 통과 확인 후 횡단과 감사까지 끝나야 켜집니다.")] private bool m_Completed; // 첫 두 상황 기록은 그대로 유지합니다.
        [SerializeField, InspectorName("세 번째 완료 이벤트"), Tooltip("친구 재회와 감사 후 세 번째 안전 완료 화면을 표시할 때 한 번 호출합니다.")] private UnityEvent m_OnCompleted = new UnityEvent(); // 점수나 보상을 추가하지 않습니다.
        [SerializeField, InspectorName("사용자 종료 이벤트"), Tooltip("사용자가 체험 마치기를 선택했을 때 호출합니다.")] private UnityEvent m_OnFinished = new UnityEvent(); // 최종 화면은 자동 종료하지 않습니다.
        private bool m_Started, m_QueueFreeze, m_HasCheckpoint, m_RetryRequested, m_Friend1WasPaused; // 한 번의 도입과 재시도·중단을 관리합니다.
        public StageState CurrentState => m_State; // PC 입력이 실제 단계 전환을 감지하여 이전 커서 잠금을 해제하도록 제공합니다.
        public bool IsCompleted => m_Completed; // 마지막 안전 행동이 끝난 사실만 외부에 제공합니다.
        public bool BlocksPlayerTranslation => isActiveAndEnabled && m_Started; // 기존 직접 이동은 추가하지 않고 체험 구도를 유지합니다.
        public bool AllowsLooking => m_Started; // PC 마우스와 기존 헤드셋의 자연스러운 둘러보기를 유지합니다.
        public bool IsObservationReady => isActiveAndEnabled && m_State == StageState.Observation; // 정지된 장면에서만 네 단서의 클릭을 허용합니다.
        public bool HasResultUIInput => isActiveAndEnabled && (m_State == StageState.Choice || m_State == StageState.Retry || m_State == StageState.Complete || m_State == StageState.OverallSummary || m_State == StageState.OpeningOverallSummary); // 선택·재시도·마무리 버튼도 실제 마우스로 클릭합니다.

        private void Start() // 실제 씬의 UI와 기존 캐릭터 초기화가 끝난 뒤 시작을 기다립니다.
        { // 두 번째 완료 이벤트가 아직 없으면 자동으로 건너뛰지 않습니다.
            m_UI.HideAll(); m_Thought.Show(false); // 새 상황의 문구와 정답을 미리 표시하지 않습니다.
            if (m_Second.IsCompleted) BeginAfterSecondSituation(); // 완료 상태로 다시 연 경우에도 같은 진입점을 사용합니다.
        } // 관찰 대상은 두 번째 제어기가 소유하는 동안 건드리지 않습니다.

        public void BeginAfterSecondSituation() // 두 번째 안전 완료 이벤트가 호출하는 유일한 시작점입니다.
        { // 실패나 중복 클릭으로 세 번째 상황을 먼저 시작하지 않습니다.
            if (!isActiveAndEnabled || m_Started || m_Completed || !m_First.IsCompleted || !m_Second.IsCompleted) return; // 실제 앞선 두 안전 완료가 있어야 시작합니다.
            m_Started = true; m_State = StageState.Introduction; m_Friend1WasPaused = m_Friend1.IsScenarioPaused; // 입력 상태를 먼저 확정하고 친구 1의 원래 정지를 저장합니다.
            StartCoroutine(RunIntroduction()); // 같은 횡단보도에서 다음 접근 차량을 준비합니다.
        } // 앞선 상황의 완료 기록을 초기화하지 않습니다.

        private IEnumerator RunIntroduction() // 빨간불에서 접근하던 차량과 실제 파란불 전환을 보여 줍니다.
        { // 플레이어의 위치와 헤드셋 추적은 바꾸지 않습니다.
            yield return new WaitForSecondsRealtime(m_SecondResultHold); // 두 번째 결과 안내를 읽을 시간을 유지합니다.
            float clearDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 이전 차량의 남은 주행 거리로 정리 대기 상한을 구합니다.
            while (!m_Car.HasPassedCrosswalkForScenario && m_Car.isActiveAndEnabled && Time.unscaledTime < clearDeadline) yield return null; // 두 번째 안전 행동이 끝난 뒤 정상 차량 통과를 먼저 기다립니다.
            if (!m_Car.HasPassedCrosswalkForScenario) Debug.LogWarning("이전 차량의 통과가 완료되지 않아 다음 암전에서 세 번째 접근 경로를 다시 준비합니다.", this); // 이미 끝난 친구의 안전 행동은 다시 실행하거나 건너뛰지 않습니다. // 현재 차량의 뒤범퍼까지 실제로 횡단 구역을 빠져나가게 합니다.
            m_Second.HideCompletedResultForNextSituation(); m_SharedUI.Transition(true, m_TransitionDuration); // 기존 결과를 닫고 다음 접근 배치 순간만 짧게 가립니다.
            yield return new WaitForSecondsRealtime(m_SharedUI.FadeCoverDuration); // 실제 차로 변경은 가려진 동안에만 적용합니다.
            if (!m_Car.PrepareGreenScenarioRoute(m_CarEntry, m_CarExit, m_CarSignalChangePoint, m_CarApproachSpeed)) // 같은 차로에서 멀리부터 다가와 한 번 통과하도록 준비합니다.
            { // 잘못된 경로로 친구와 차량을 강제로 겹치게 하지 않습니다.
                Debug.LogError("세 번째 접근 차량 경로를 준비하지 못했습니다. 시작·끝·신호 전환 위치를 확인해 주세요.", this); m_SharedUI.Transition(false); yield break; // 암전을 해제하고 차량 경로의 준비 실패를 알립니다.
            } // 원래 두 상황의 차량 경로는 이 차량이 별도로 보관합니다.
            m_Friend1Performance.Freeze(true); m_Friend2.SetScenarioPaused(true); m_Friend2.PlaceWaitingForScenario(m_CurbPoint); // 두 친구를 기존 인도 안에서 준비합니다.
            m_Signal.HoldRedForScenario(); m_Friend2.SetScenarioPaused(false); m_Car.SetScenarioPaused(false); m_SharedUI.Transition(false, m_TransitionDuration); // 빨간불인 동안 멀리서 빠르게 다가오는 차량부터 보여 줍니다.
            float signalDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 실제 접근 속도를 기준으로 신호 전환을 기다릴 시간을 정합니다.
            while (!m_Car.HasReachedGreenSignalChangePoint && m_Car.isActiveAndEnabled && Time.unscaledTime < signalDeadline) yield return null; // 차가 지정된 접근 위치에 도착한 프레임까지 빨간불을 유지합니다.
            if (!m_Car.HasReachedGreenSignalChangePoint) { Debug.LogError("차량이 보행 신호 전환 위치에 도착하지 못했습니다.", this); yield break; } // 차량이 없는 장면에서 관찰을 열지 않습니다.
            m_Signal.HoldGreenForScenario(); // 차가 계속 다가오는 동안 실제 보행 신호를 파란불로 바꿉니다.
            m_ThoughtText.text = "파란불이다!"; // 친구가 신호 전환을 보고 바로 출발하려는 의도를 보여 줍니다.
            m_SharedUI.ShowDialogue(m_Thought, m_Friend2.transform.position + Vector3.up * 1.25f); // 기존 세 번째 말풍선을 친구 얼굴 옆에 배치합니다.
            BlenderRigPose.PlayFace(m_Performance.HeadTarget, 0.6f); // 첫 발을 떼기 전 짧은 대사에 맞춰 입을 움직이고 눈을 한 번 깜빡입니다.
            m_Friend2.FaceScenarioTarget(m_GreenSignalPoint); yield return m_Performance.LookAtTarget(m_GreenSignalPoint, 0.6f); // 신호를 짧게 확인하는 사이에도 차는 계속 접근합니다.
            if (!m_Friend2.TryBeginScenarioFirstStep(m_GreenSignalPoint, 0.14f)) // 도로로 이동하기 전 기존 첫 발 자세를 준비합니다.
            { // 첫 발이 준비되지 않은 상태를 정상 관찰로 가장하지 않습니다.
                Debug.LogError("세 번째 상황에서 친구 2의 첫 발을 준비하지 못했습니다. 기존 대기 상태와 횡단 경로를 확인해 주세요.", this); yield break; // 실제 거절 원인을 남기고 임의의 횡단을 실행하지 않습니다.
            } // 정상 첫 발에서만 관찰 상태로 이어갑니다.
            yield return new WaitForSecondsRealtime(0.14f); m_QueueFreeze = true; // 이번 프레임의 기존 Animator와 차량이 적용된 뒤 함께 정지합니다.
        } // 차가 지나가기 전에 친구의 첫 발과 접근 차량을 함께 관찰할 수 있게 합니다.

        private void LateUpdate() // 첫 발과 접근 차량의 최종 프레임을 동시에 보관합니다.
        { // 시간 배율을 정지시키지 않습니다.
            if (!m_QueueFreeze) return; // 도입의 첫 발 순간에만 저장합니다.
            m_QueueFreeze = false; m_Friend2.SetScenarioPaused(true); m_Car.SetScenarioPaused(true); m_Friend1Performance.Freeze(true); m_Signal.HoldGreenForScenario(); // 자세·위치·접근 속도를 보존하고 입력은 유지합니다.
            m_Performance.CaptureObservation(); m_Car.CaptureScenarioCheckpoint(); m_HasCheckpoint = true; // 같은 세 번째 순간의 캐릭터와 차량 상태를 저장합니다.
            EnterObservation(); // 기존 네 단서와 완료 버튼을 마우스 입력에 연결합니다.
        } // 앞선 두 상황의 체크포인트로 돌아가지 않습니다.

        private void SetObservations(bool available) // 같은 관찰 인스턴스에 세 번째 문구를 선택합니다.
        { // Collider와 실루엣 Renderer를 복제하지 않습니다.
            foreach (FirstSituationObservation target in m_Observations) if (target != null) target.SetAvailable(available, true); // 조준·좌클릭 규칙은 기존 시스템을 그대로 사용합니다.
        } // 단서를 모두 조사했는지 세지 않습니다.
        private void EnterObservation() // 도입과 실패 복원에서 같은 정지·관찰 상태로 진입합니다.
        { // UI는 실제 씬의 패널만 표시합니다.
            m_UI.HideAll(); m_RetryRequested = false; m_State = StageState.Observation; SetObservations(true); // 이전 결과 입력을 지우고 세 번째 단서만 엽니다.
            m_SharedUI.PlaceInFront(m_Viewer.transform); m_SharedUI.ShowObservation(FinishObservation, m_Thought, m_ThoughtText); // 기존 완료 버튼과 세 번째의 실제 생각 말풍선을 연결합니다.
        } // 관찰 시간 제한은 없습니다.
        public void FinishObservation() // 사용자가 직접 관찰을 마친 경우에만 선택 패널을 표시합니다.
        { // 조사한 단서의 수는 확인하지 않습니다.
            if (!IsObservationReady) return; // 이미 선택이나 결과 단계이면 중복 완료를 무시합니다.
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
            m_UI.ShowWarning(); m_Signal.HoldGreenForScenario(); m_Car.SetScenarioPaused(false); // 경고를 보여 주고 차량은 접근하던 속도 그대로 지나가게 합니다.
            m_Friend2.SetScenarioFirstStepHold(false); m_Friend2.SetScenarioPaused(false); // 친구는 첫 발을 거두고 인도에서 대기합니다.
            yield return m_Performance.LookAtTarget(m_Car.transform, 1.1f); // 아직 움직이는 실제 자동차를 발견합니다.
            yield return m_Performance.AcknowledgeWaiting(); // 친구가 고개를 끄덕이고 건너편 인도에서 먼저 기다립니다.
            float clearDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 차량이 같은 차로를 끝까지 진행할 시간 안에서 통과를 기다립니다.
            while (CarBumperProgress(true) < m_CrosswalkHalfWidth && m_Car.isActiveAndEnabled && Time.unscaledTime < clearDeadline) yield return null; // 뒷범퍼까지 횡단보도 바깥에 나갈 때까지 인도에서 기다립니다.
            if (CarBumperProgress(true) < m_CrosswalkHalfWidth) { Debug.LogWarning("차량이 횡단보도를 완전히 빠져나가지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 차가 가로막고 있는 동안에는 안전 횡단을 시작하지 않습니다.
            m_UI.HideAll(); // 통과가 끝난 뒤 안내를 닫고 양쪽 도로 확인을 보여 줍니다.
            m_Friend2.FaceScenarioTarget(m_CheckLeft); yield return m_Performance.LookAtTarget(m_CheckLeft, 0.8f); // 반대 방향의 도로도 살펴봅니다.
            m_Friend2.FaceScenarioTarget(m_CheckRight); yield return m_Performance.LookAtTarget(m_CheckRight, 0.8f); // 접근 방향을 다시 확인합니다.
            m_Friend2.SetScenarioApproachLimit(null); m_Friend2.SetScenarioApproachSpeed(0.9f); // 원래 횡단 도착점까지 기존 보폭에 맞춰 걷게 합니다.
            if (CarBumperProgress(true) < m_CrosswalkHalfWidth || !m_Friend2.TryBeginCrossing()) // 차량 뒤쪽이 완전히 지난 상태에서만 기존 횡단을 수락합니다.
            { // 경로 실패를 완료로 기록하지 않습니다.
                Debug.LogError("차량 통과 확인 후 기존 안전 횡단을 시작하지 못했습니다.", this); yield return RestoreObservation(); yield break; // 같은 세 번째 관찰을 복원합니다.
            } // 일반 걷기 애니메이션과 기존 바닥 높이 계산을 재사용합니다.
            m_State = StageState.Crossing; m_WalkAudio.SetActive(true); // 플레이어 쪽 인도까지 발소리를 보행 위상에 연결합니다.
            float crossingDeadline = Time.unscaledTime + m_Friend2.ScenarioTravelBudget; // 기존 횡단 경로의 실제 남은 거리와 착지 시간을 사용합니다.
            bool reachedSidewalk = false; float stationaryArrivalTime = 0f; Vector3 previousCrossingPosition = m_Friend2.transform.position; // 이동 완료 플래그뿐 아니라 실제 인도 근처에서 더 이상 움직이지 않는 상태를 확인합니다.
            while (m_Friend2.isActiveAndEnabled && Time.unscaledTime < crossingDeadline) // 안전 횡단이 실제 도착 지점에 이르렀는지 제한 시간 안에서 확인합니다.
            { // 횡단보도 중간에서 일시적으로 정지한 상태는 완료로 오인하지 않습니다.
                Vector3 arrivalOffset = Vector3.ProjectOnPlane(m_Friend2.transform.position - m_ArrivalPoint.position, Vector3.up); // 기존 횡단 도착점과 친구 루트 사이의 수평 거리만 측정합니다.
                bool atArrival = m_Friend2.HasStartedCrossing && arrivalOffset.sqrMagnitude <= 0.2025f; // 도착점으로부터 45cm 이내에 들어온 친구만 실제 인도 근처라고 인정합니다.
                Vector3 frameMotion = Vector3.ProjectOnPlane(m_Friend2.transform.position - previousCrossingPosition, Vector3.up); previousCrossingPosition = m_Friend2.transform.position; // 보행 상태 플래그가 걸린 채 멈춰도 실제 프레임 사이 위치 변화로 정지를 확인합니다.
                stationaryArrivalTime = atArrival && frameMotion.sqrMagnitude <= 0.000025f ? stationaryArrivalTime + Time.unscaledDeltaTime : 0f; // 도착점 45cm 이내에서 실제로 0.45초 움직이지 않은 경우만 보조 완료로 인정합니다.
                reachedSidewalk = m_Friend2.HasCompletedCrossing || stationaryArrivalTime >= 0.45f; // 정상 횡단 완료를 우선하고 위치에서 정지한 버그 상황을 보완합니다.
                if (reachedSidewalk) break; // 도착한 친구를 기다리게 하지 않고 재회 연출로 넘깁니다.
                yield return null; // 기존 이동기와 Animator의 보행을 그대로 유지합니다.
            }
            if (!reachedSidewalk) { Debug.LogWarning("친구가 횡단 도착점에 이르지 못해 세 번째 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 도로 중간의 캐릭터를 완료 처리하지 않습니다.
            m_WalkAudio.Stop(); m_State = StageState.Reunion; m_Friend1Performance.Freeze(false); // 플레이어 쪽 인도에 도착한 즉시 재회 단계로 전환합니다.
            m_Friend1.FaceScenarioTarget(m_Friend2.transform); StartCoroutine(m_Friend1Greeting.Play(m_Performance.HeadTarget)); // 친구 1의 환영 손짓을 기존 캐릭터 관절로 별도로 재생합니다.
            m_UI.ShowThanks(m_Performance.HeadPosition); // 동작이 끝나기를 기다리기 전에 먼저 감사 대사를 띄워 정지 화면처럼 보이지 않게 합니다.
            BlenderRigPose.PlayFace(m_Performance.HeadTarget, 2.1f, 4.2f); // 감사 말풍선이 나타날 때 바로 말하는 얼굴을 시작하고 재회 결과까지 미소를 유지합니다.
            StartCoroutine(PlayReunionGestures()); // 돌아보기와 감사 몸짓은 별도 코루틴으로 연결해 완료 화면의 진행을 막지 않습니다.
            yield return new WaitForSecondsRealtime(2.8f); // 플레이어가 감사 대사를 읽을 시간을 주되 관절 동작의 종료를 무기한 기다리지 않습니다.
            if (m_State != StageState.Reunion) yield break; // 상황이 중단되거나 전환되었다면 지난 완료 이벤트를 다시 호출하지 않습니다.
            m_Completed = true; m_State = StageState.Complete; m_UI.ShowSafe(); m_OnCompleted.Invoke(); // 실제 도착 후 정해진 시간에 완료 화면을 표시해 다음 진행을 보장합니다.
        } // 사용자 입력 없이는 마지막 패널을 닫거나 종료하지 않습니다.

        private IEnumerator PlayReunionGestures() // 완료 화면 진행과 독립적으로 기존 사람형 캐릭터의 돌아보기·감사 동작을 재생합니다.
        { // 기존 관절 연출만 재사용하며 이동 경로와 신호 상태는 변경하지 않습니다.
            yield return m_Performance.RetreatInsideSidewalk(m_ArrivalPoint, m_Viewer.transform); // 인도 안쪽 도착점에서 실제 플레이어 쪽으로 자연스럽게 돌아섭니다.
            if (m_State != StageState.Reunion && m_State != StageState.Complete) yield break; // 다른 상황으로 전환됐다면 이후 감사 동작을 시작하지 않습니다.
            yield return m_Performance.ThankPlayer(2.6f); // 몸짓이 완료 화면 진행을 멈추지 않도록 독립적으로 재생합니다.
        } // 이동기·관절·기존 감사 자세를 그대로 사용합니다.

        private IEnumerator RunDanger() // 파란불만 보고 서둘러 내딛은 친구 옆으로 차량이 그대로 지나가는 상황을 보여 줍니다.
        { // 친구의 실제 보행은 차량 옆면에 닿기 전 차도 가장자리까지만 이어집니다.
            m_Signal.HoldGreenForScenario(); m_Friend2.SetScenarioApproachSpeed(2.2f); // 망설임 없이 출발하는 모습에 기존 보행 가속과 발 디딤을 사용합니다.
            if (!m_Friend2.TryBeginUnsafeScenarioCrossing(m_NearMissPoint)) // 관찰에서 저장한 첫 발로부터 기존 위험 접근을 시작합니다.
            { // 잘못된 배치를 억지로 통과시키지 않습니다.
                Debug.LogError("세 번째 위험 접근의 첫 발 또는 차도 가장자리 위치를 준비하지 못했습니다.", this); yield return RestoreObservation(); yield break; // 준비에 실패하면 같은 관찰에서 다시 선택할 수 있게 합니다.
            } // 준비가 끝난 경우에만 차량과 발소리를 재개합니다.
            m_Car.SetScenarioPaused(false); m_WalkAudio.SetActive(true); // 관찰 당시의 위치와 속도에서 두 배우를 함께 재개합니다.
            float deadline = Time.unscaledTime + Mathf.Max(m_Car.ScenarioTravelBudget, m_Friend2.ScenarioTravelBudget); // 자동차와 친구가 실제 접근 지점에 도달할 시간을 확보합니다.
            while ((m_Friend2.ScenarioLimitRemaining > 0.12f || CarBumperProgress(false) < -m_CrosswalkHalfWidth) && m_Friend2.isActiveAndEnabled && m_Car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 친구가 가장자리에 다가가고 차 앞부분이 횡단보도에 들어올 때까지 기다립니다.
            if (m_Friend2.ScenarioLimitRemaining > 0.12f || CarBumperProgress(false) < -m_CrosswalkHalfWidth) { Debug.LogWarning("차량 또는 친구의 접근이 끝나지 않아 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 접근 실패를 정상 연출로 기록하지 않습니다.
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
            StopEffects(); m_Performance.RestoreObservation(); m_Car.RestoreScenarioCheckpoint(); m_Signal.HoldGreenForScenario(); m_Friend1Performance.Freeze(true); // 실제 접근 위치·속도·첫 발·파란 신호·친구 1 대기를 복원합니다.
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

        private void OnDisable() // 중단·씬 종료에서 이 상황이 소유한 일시 상태만 정리합니다.
        { // 첫 번째·두 번째 완료를 취소하지 않습니다.
            StopAllCoroutines(); m_QueueFreeze = false; if (!m_Started) return; // 시작 전에는 이전 상황의 관찰 대상에 개입하지 않습니다.
            SetObservations(false); StopEffects(); if (m_UI != null) m_UI.HideAll(); if (m_Thought != null) m_Thought.Show(false); // 실제 세 번째 패널과 효과만 숨깁니다.
            if (m_Performance != null) BlenderRigPose.ResetFace(m_Performance.HeadTarget); // 첫 발을 저장하기 전에 중단해도 친구 2의 대사와 표정을 끝냅니다.
            if (m_Friend1Greeting != null) m_Friend1Greeting.Stop(); // 재회 도중 중단되면 친구 1의 환영 손짓과 웃음도 정리합니다.
            if (!m_Completed) // 완료 전 중단한 경우만 이전 상황의 대기 배치로 돌아갑니다.
            { // 살아 있는 원래 인스턴스만 복원합니다.
                if (m_HasCheckpoint && m_Performance != null) m_Performance.RestoreObservation(); // 진행 중 횡단 기록과 관절을 먼저 되돌립니다.
                if (m_Car != null) m_Car.RestoreBeforeGreenScenario(); // 원래 두 상황의 차로와 실제 주행 상태를 복원합니다.
                if (m_Friend2 != null && m_PreviousWaitingPoint != null) { m_Friend2.PlaceWaitingForScenario(m_PreviousWaitingPoint); m_Friend2.SetScenarioPaused(false); } // 건너편의 기존 안전 대기를 유지합니다.
                if (m_Friend1Performance != null) m_Friend1Performance.Freeze(m_Friend1WasPaused); if (m_Signal != null) m_Signal.HoldRedForScenario(); // 이전 완료 상황의 친구 1과 빨간 신호를 복원합니다.
            } // 정상 완료에서는 파란 신호와 차량이 지나간 상태를 유지합니다.
            if (m_SharedUI != null) { m_SharedUI.HideAll(); m_SharedUI.Transition(false); } m_Started = false; // 중단된 전환막과 입력 소유 상태를 정리합니다.
        } // 별도 플레이어나 입력 시스템을 생성하지 않습니다.
    } // 세 번째이자 마지막 보행 안전 상황을 마칩니다.
} // 점수·도장·새로운 보상 스테이지는 포함하지 않습니다.
