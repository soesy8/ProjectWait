using System.Collections; // 신호 전환부터 관찰·선택·재시도·재회를 순서대로 연결합니다.
using Time = ProjectWait.M.Crosswalk.CrosswalkStoryTime;
using WaitForSecondsRealtime = ProjectWait.M.Crosswalk.CrosswalkStoryWait;
using TMPro;
using Unity.XR.CoreUtils; // 횡단 중 카메라 회전은 유지하면서 실제 플레이어 루트의 위치만 이동합니다.
using UnityEngine; // 기존 캐릭터·차량·신호·월드 UI를 제어합니다.
using UnityEngine.Events; // 안전 완료와 사용자의 최종 종료를 다른 진행 시스템에 알립니다.

namespace Y.Crosswalk // 첫 번째·두 번째 완료 기록과 독립된 마지막 상황입니다.
{ // 기존 입력·관찰·골격을 재사용해 신호가 바뀔 때의 차량 통과를 보여 줍니다.
    [DefaultExecutionOrder(500)] // 기존 이동·관절·차량의 최종 프레임 뒤에 관찰을 정지합니다.
    public sealed class ThirdSituationController : MonoBehaviour // 파란불 접근 차량의 전체 안전 체험을 진행합니다.
    { // 문구·위치·참조는 실제 씬 Inspector에서 편집합니다.
        public enum StageState { Waiting, Introduction, Observation, Choice, WaitingForCar, Crossing, Danger, Retry, [InspectorName("횡단 도착")] Arrival, Complete, [InspectorName("횡단보도 파트 완료 확인")] Ended } // 이 씬의 진행과 완료만 기록하고 전체 프로그램 종료 상태는 만들지 않습니다.
        [SerializeField, InspectorName("첫 번째 완료 기록"), Tooltip("기존 전봇대 상황의 완료 상태를 읽기만 합니다.")] private FirstSituationController m_First; // 첫 번째 결과는 재시도에서 초기화하지 않습니다.
        [SerializeField, InspectorName("두 번째 안전 완료"), Tooltip("기존 두 번째 완료 이벤트를 BeginAfterSecondSituation에 연결합니다.")] private SecondSituationController m_Second; // 빨간불에서 안전하게 기다린 경우만 시작합니다.
        [SerializeField, InspectorName("공유 관찰·전환 UI"), Tooltip("기존 관찰 완료 버튼과 짧은 화면 전환입니다.")] private FirstSituationUI m_SharedUI; // 별도 관찰 입력 시스템을 만들지 않습니다.
        [SerializeField, InspectorName("세 번째 실제 패널"), Tooltip("기존 Canvas 아래에 배치한 선택·대사·결과·최종 안내입니다.")] private SecondSituationUI m_UI; // 같은 파스텔 표시·버튼 반응을 사용합니다.
        [SerializeField, InspectorName("세 번째 생각 말풍선"), Tooltip("실제 Hierarchy에 배치한 세 번째 생각 패널입니다.")] private FirstSituationPanel m_Thought; // 첫 두 상황의 생각 패널과 표시 상태를 분리합니다.
        [SerializeField, InspectorName("세 번째 생각 문구"), Tooltip("생각 말풍선 내부의 실제 TMP 텍스트입니다.")] private TMP_Text m_ThoughtText; // 클릭한 단서의 문구를 표시합니다.
        [SerializeField, InspectorName("기존 플레이어 카메라"), Tooltip("기존 XR Origin의 추적 카메라입니다. UI와 캐릭터 응시 기준으로만 사용합니다.")] private Camera m_Viewer; // 추적 카메라의 로컬 자세는 직접 변경하지 않습니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("친구와 함께 횡단하고, 도착 후 친구 쪽으로 돌아볼 플레이어 루트입니다.")] private XROrigin m_Origin; // 회전은 기존 입력기의 XR Origin 연출을 재사용합니다.
        [SerializeField, InspectorName("파란불 관찰 대상"), Tooltip("친구·신호·차량·횡단보도의 기존 관찰 인스턴스입니다.")] private FirstSituationObservation[] m_Observations; // 접근 차량과 횡단 시작 위치를 실제로 누를 수 있게 합니다.
        [SerializeField, InspectorName("접근 자동차 클릭 대상"), Tooltip("파란불에도 접근하는 자동차의 관찰 컴포넌트입니다.")] private FirstSituationObservation m_CarObservation; // 움직이던 차를 확인하고 친구를 말릴 이유를 발견합니다.
        [SerializeField, InspectorName("기다리며 확인할 신호등"), Tooltip("빨간불에서 바로 다음 장면으로 넘어가지 않고 플레이어가 직접 확인할 기존 신호등입니다.")] private FirstSituationObservation m_SignalObservation; // 다음 신호를 기다리는 실제 물체 클릭을 이야기 진행에 연결합니다.
        [SerializeField, InspectorName("친구 클릭 대상"), Tooltip("빨간불 때부터 옆에서 기다린 친구의 관찰 컴포넌트입니다.")] private FirstSituationObservation m_FriendObservation; // 차를 확인한 뒤 친구에게 기다리자고 말합니다.
        [SerializeField, InspectorName("횡단 시작 클릭 대상"), Tooltip("차량 통과 뒤 직접 눌러 함께 건너기 시작할 횡단보도 관찰 컴포넌트입니다.")] private FirstSituationObservation m_CrosswalkObservation; // 안전 확인 뒤 마지막 행동도 플레이어가 시작합니다.
        private bool m_CarChecked; // 위험 선택 재시도에서는 앞서 확인한 자동차 설명을 유지합니다.
        [SerializeField, InspectorName("공통 친구 이동"), Tooltip("빨간불 때 옆에서 대기한 친구의 이동과 기존 횡단 애니메이션입니다.")] private FriendCharacterMovement m_Friend; // 두 번째 상황의 친구 위치와 대기 상태에서 계속 진행합니다.
        [SerializeField, InspectorName("공통 친구 관절 연출"), Tooltip("기존 신호 확인·놀람·뒷걸음·감사 표현을 같은 친구에 적용합니다.")] private SecondSituationCharacterPerformance m_Performance; // 관찰 정지 자세와 위험 결과를 재사용합니다.
        [SerializeField, InspectorName("공통 친구 발소리"), Tooltip("앞선 보행과 같은 친구의 걸음에 동기화됩니다.")] private FirstSituationWalkAudio m_WalkAudio; // 실제 횡단하는 동안에만 발소리를 재생합니다.
        [SerializeField, InspectorName("기존 자동차"), Tooltip("기존 한 대의 차량을 같은 도로에서 재사용합니다.")] private TrafficCarMovement m_Car; // 첫 두 상황의 원래 주행 경로는 보관합니다.
        [SerializeField, InspectorName("자동차 몸 Collider"), Tooltip("기존 차량 전체의 관찰 Collider입니다.")] private Collider m_CarBody; // 차의 앞과 뒤가 횡단보도에 닿거나 빠져나가는 순간을 계산합니다.
        [SerializeField, InspectorName("친구 몸 Collider"), Tooltip("차량과 같은 차선에서 부딪힐 뻔하는 간격을 계산할 기존 친구 몸 Collider입니다.")] private Collider m_FriendBody; // 차량과 친구의 실제 크기를 같은 교차점의 급정거 계산에 사용합니다.
        [SerializeField, Min(1f), InspectorName("위험 횡단 달리기 속도(m/s)"), Tooltip("차량을 확인하지 않고 뛰어드는 선택에서 실제 친구의 달리기 속도입니다.")] private float m_DangerRunSpeed = 2.8f; // 차량 차선에 도달하는 실제 시간에 급정거 접근을 맞춥니다.
        [SerializeField, InspectorName("기존 보행 신호"), Tooltip("기존 파란 사람 표시와 타이머를 유지합니다.")] private PedestrianTrafficLight m_Signal; // 횡단 완료까지 파란 신호를 유지합니다.
        [SerializeField, InspectorName("파란 신호 응시점"), Tooltip("기존 파란 사람 표시의 Transform입니다.")] private Transform m_GreenSignalPoint; // 친구가 실제 신호를 바라봅니다.
        [SerializeField, InspectorName("접근 차량 시작"), Tooltip("플레이어 쪽 첫 차로에서 왼쪽으로 접근하는 차량의 시작점입니다.")] private Transform m_CarEntry; // 두 번째 차량이 통과한 뒤 같은 통행 방향의 시작점에서 다음 접근을 준비합니다.
        [SerializeField, InspectorName("접근 차로 끝"), Tooltip("플레이어 쪽 첫 차로에서 횡단보도 오른쪽 도로 끝에 놓는 지점입니다.")] private Transform m_CarExit; // 차량이 친구 바로 앞 차로를 왼쪽에서 오른쪽으로 통과하도록 방향을 정합니다.
        [SerializeField, InspectorName("파란불 전환 위치"), Tooltip("친구가 인도에서 첫 차로 앞까지 내딛는 동안 차가 횡단보도에 도달하도록 정한 신호 전환 지점입니다.")] private Transform m_CarSignalChangePoint; // 먼 거리의 차량을 오래 보여 주지 않고 출발하려는 순간의 측면 접근에 신호를 맞춥니다.
        [SerializeField, Min(0.1f), InspectorName("통과 차량 속도(m/s)"), Tooltip("세 번째 상황의 접근 차량이 기존 차로에서 유지할 실제 이동 속도입니다.")] private float m_CarApproachSpeed = 10f; // 이전 8m/s보다 빠르게 접근하도록 통과 차량 속도를 높입니다.
        [SerializeField, Min(0.1f), InspectorName("횡단보도 반폭(m)"), Tooltip("친구가 건너는 선에서 차가 횡단보도를 완전히 빠져나갔다고 판단하는 거리입니다.")] private float m_CrosswalkHalfWidth = 1.9f; // 차량 뒤쪽까지 이 거리를 벗어나야 횡단할 수 있습니다.
        [SerializeField, InspectorName("횡단보도 중심선 기준"), Tooltip("전봇대 쪽 인도의 횡단 시작점입니다. 차량이 지나갔는지 판단할 때 이 지점의 차로 방향 좌표를 사용합니다.")] private Transform m_CurbPoint; // 앞뒤 범퍼 위치를 같은 횡단보도 기준으로 계산합니다.
        [SerializeField, InspectorName("차량 차선의 위험 교차점"), Tooltip("친구의 횡단 경로와 실제 차량 차선이 만나는 지점입니다. 차량 앞에서 멈추는 친구와 급정거를 같은 위치에 맞춥니다.")] private Transform m_NearMissPoint; // 친구가 차선보다 앞이나 뒤로 달려가 버리지 않게 실제 차량 경로에 맞춘 위치를 사용합니다.
        [SerializeField, InspectorName("도로 왼쪽 확인점"), Tooltip("횡단 전 반대 방향을 확인할 도로 위 응시점입니다.")] private Transform m_CheckLeft; // 실제 주변을 살펴보는 고개 동작을 연결합니다.
        [SerializeField, InspectorName("도로 오른쪽 확인점"), Tooltip("횡단 전 접근 방향을 재확인할 도로 위 응시점입니다.")] private Transform m_CheckRight; // 신호만 보고 출발하지 않는 행동을 보여 줍니다.
        [SerializeField, InspectorName("세 번째 진행 상태"), Tooltip("같은 장소의 신호 전환·관찰·선택·횡단·최종 완료를 구분합니다.")] private StageState m_State; // 현재 단계에 맞춰 자유 시점과 버튼 입력을 구분합니다.
        [SerializeField, InspectorName("세 번째 안전 완료"), Tooltip("차량이 지나간 뒤 친구와 반대편 인도에 도착해야 켜집니다.")] private bool m_Completed; // 실제 도착 전에는 마무리로 넘어가지 않습니다.
        [SerializeField, InspectorName("세 번째 완료 이벤트"), Tooltip("안전 횡단 뒤 기존 완료 화면을 표시할 때 한 번 호출합니다.")] private UnityEvent m_OnCompleted = new UnityEvent(); // 완료 후 다른 씬으로 연결할 기존 이벤트를 유지합니다.
        [SerializeField, InspectorName("횡단보도 완료 확인 이벤트"), Tooltip("사용자가 이 파트의 확인 버튼을 누르면 호출합니다. 다른 파트로의 연결은 통합 프로젝트가 결정합니다.")] private UnityEvent m_OnFinished = new UnityEvent(); // 현재 파트의 확인만 전달하며 앱이나 Unity 플레이를 종료하지 않습니다.
        private bool m_Initialized, m_Started, m_QueueFreeze, m_HasCheckpoint, m_RetryRequested; // 관찰 진입과 재시도, 중단 후 재활성화를 분리합니다.
        private Vector3 m_ReadyPlayerPosition, m_ReadyFriendPosition; // 횡단을 취소하거나 재시도할 때 돌아갈 같은 쪽 인도 위치입니다.
        private Quaternion m_ReadyFriendRotation; // 세 번째 상황이 시작되기 직전 친구의 실제 대기 방향을 저장합니다.
        private Quaternion m_ReadyPlayerRotation; // 도착 회전이 중단되면 횡단 전의 루트 방향도 복원합니다.
        private CrosswalkDesktopInput m_ViewInput;
        public StageState CurrentState => m_State; // PC 입력이 실제 단계 전환을 감지하여 이전 커서 잠금을 해제하도록 제공합니다.
        public bool IsCompleted => m_Completed; // 마지막 안전 행동이 끝난 사실만 외부에 제공합니다.
        public bool BlocksPlayerTranslation => isActiveAndEnabled && m_Started; // 기존 직접 이동은 추가하지 않고 체험 구도를 유지합니다.
        public bool AllowsLooking => m_Started; // PC 마우스와 기존 헤드셋의 자연스러운 둘러보기를 유지합니다.
        public bool IsObservationReady => isActiveAndEnabled && m_State == StageState.Observation; // 정지된 장면에서만 네 단서의 클릭을 허용합니다.
        public bool HasResultUIInput => isActiveAndEnabled && (m_State == StageState.Choice || m_State == StageState.Retry || m_State == StageState.Complete); // 이 파트의 선택·재시도·완료 확인 버튼에만 UI 입력을 연결합니다.

        private void Awake() { m_ViewInput = m_First.GetComponent<CrosswalkDesktopInput>(); } // 입구의 좌측 회전과 같은 PC·VR 입력기를 사용합니다.

        private void Start() // 실제 씬의 UI와 기존 캐릭터 초기화가 끝난 뒤 시작을 기다립니다.
        { // 두 번째 완료 이벤트가 아직 없으면 자동으로 건너뛰지 않습니다.
            m_Initialized = true; m_UI.HideAll(); m_Thought.Show(false); // 화면이 준비된 뒤 이전 상황 완료 이벤트를 받을 수 있게 합니다.
            if (m_Second.IsCompleted) BeginAfterSecondSituation(); // 완료 상태로 다시 연 경우에도 같은 진입점을 사용합니다.
        } // 관찰 대상은 두 번째 제어기가 소유하는 동안 건드리지 않습니다.

        public void BeginAfterSecondSituation() // 두 번째 안전 완료 이벤트가 호출하는 유일한 시작점입니다.
        { // 실패나 중복 클릭으로 세 번째 상황을 먼저 시작하지 않습니다.
            if (!isActiveAndEnabled || m_Started || m_Completed || !m_First.IsCompleted || !m_Second.IsCompleted) return; // 실제 앞선 두 안전 완료가 있어야 시작합니다.
            m_Started = true; m_State = StageState.Introduction; // 빨간불에 함께 기다리는 현재 장면에서 세 번째 흐름을 시작합니다.
            m_SharedUI.SetDialogueSpeaker(m_Friend.transform, m_Performance.HeadTarget);
            m_ReadyPlayerPosition = m_Origin.Origin.transform.position; // 횡단 결과를 재시도할 때 같은 인도 위치로 돌아갑니다.
            m_ReadyPlayerRotation = m_Origin.Origin.transform.rotation;
            m_Friend.transform.GetPositionAndRotation(out m_ReadyFriendPosition, out m_ReadyFriendRotation); // 두 번째 상황이 끝난 친구의 위치와 방향을 그대로 이어받습니다.
            StartCoroutine(RunIntroduction()); // 같은 횡단보도에서 다음 접근 차량을 준비합니다.
        } // 앞선 상황의 완료 기록을 초기화하지 않습니다.

        private IEnumerator RunIntroduction() // 친구가 플레이어를 보고 기다리다가 신호 클릭에 맞춰 파란불과 첫 발을 이어갑니다.
        { // 신호를 고르기 전에는 친구가 횡단을 시작하지 않습니다.
            m_Second.HideCompletedResultForNextSituation(); SetObservations(false); m_Signal.HoldRedForScenario(); // 앞선 기다리기 결과를 닫고 실제 빨간 신호를 유지합니다.
            m_Friend.FaceScenarioTarget(m_Viewer.transform); m_Performance.BeginLookingAt(m_Viewer.transform); // 몸과 고개를 플레이어 쪽으로 향해 다음 행동을 함께 기다리게 합니다.
            float clearDeadline = Time.unscaledTime + m_Car.ScenarioTravelBudget; // 앞선 차량이 횡단보도를 빠져나갈 기존 이동 범위를 사용합니다.
            while (!m_Car.HasPassedCrosswalkForScenario && m_Car.isActiveAndEnabled && Time.unscaledTime < clearDeadline) yield return null; // 앞선 차량이 남아 있는 동안 다른 색의 다음 차량을 같은 자리에 겹치지 않습니다.
            if (!m_Car.PrepareGreenScenarioRoute(m_CarEntry, m_CarExit, m_CarSignalChangePoint, m_CarApproachSpeed, true)) { Debug.LogError("신호 클릭에 맞출 접근 차량을 준비하지 못했습니다.", this); enabled = false; yield break; } // 다음 차량을 미리 준비해 클릭 뒤 별도 주행 대기를 없앱니다.
            bool signalSelected = false; m_SignalObservation.SetAvailable(true); // 현재 실제로 선택할 물체를 기존 신호등으로 지정합니다.
            m_SharedUI.ShowTask(m_SignalObservation, "보행자 신호등", () => signalSelected = true, "", "신호를 확인해 보자."); // 신호등을 선택해야 신호 전환과 친구의 다음 행동을 시작합니다.
            while (!signalSelected) yield return null; m_SignalObservation.SetAvailable(false); // 클릭 전에는 빨간불과 플레이어를 바라보는 대기 자세를 유지합니다.
            m_Performance.StopLookingAt(); m_SharedUI.HideAll(); m_Signal.HoldGreenForScenario(); m_Car.SetScenarioPaused(false); // 클릭 직후 파란불로 바꾸고 같은 순간 접근 차량의 이동을 재개합니다.
            m_SharedUI.ShowCinematicCaption("친구", "파란불이다! 건너자."); BlenderRigPose.PlayFace(m_Performance.HeadTarget, 1.2f); // 요청한 친구 대사를 출발 동작과 같은 시점에 표시합니다.
            const float firstStepTime = 0.14f; // 기존 첫 발의 짧은 움직임을 관찰 체크포인트로 사용합니다.
            if (!m_Friend.TryBeginScenarioFirstStep(m_GreenSignalPoint, firstStepTime)) { Debug.LogError("파란불에서 친구의 첫 발 자세를 준비하지 못했습니다.", this); enabled = false; yield break; } // 친구가 횡단보도로 실제 발을 내딛게 합니다.
            yield return new WaitForSecondsRealtime(firstStepTime); m_QueueFreeze = true; // 발을 내민 실제 자세에서 주변 차량을 확인할 상황을 엽니다.
        } // 이후 기존 물체 관찰과 말리기·위험 선택으로 이어갑니다.

        private void LateUpdate() // 첫 발과 접근 차량의 최종 프레임을 동시에 보관합니다.
        { // 시간 배율을 정지시키지 않습니다.
            if (ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked) return;
            if (!m_QueueFreeze) return; // 도입의 첫 발 순간에만 저장합니다.
            m_QueueFreeze = false; m_Friend.SetScenarioPaused(true); m_Car.SetScenarioPaused(true); m_Signal.HoldGreenForScenario(); // 한 명의 친구와 다가오는 차량, 파란불을 같은 관찰 상태로 멈춥니다.
            m_Performance.CaptureObservation(); m_Car.CaptureScenarioCheckpoint(); m_HasCheckpoint = true; // 같은 세 번째 순간의 캐릭터와 차량 상태를 저장합니다.
            EnterObservation(); // 첫 발을 정지한 상태에서 관찰 조작을 엽니다.
        } // 앞선 두 상황의 체크포인트로 돌아가지 않습니다.

        private void SetObservations(bool available) // 같은 관찰 인스턴스에 세 번째 문구를 선택합니다.
        { // Collider와 실루엣 Renderer를 복제하지 않습니다.
            foreach (FirstSituationObservation target in m_Observations) if (target != null) target.SetAvailable(available, FirstSituationObservation.Context.GreenSignal); // 기존 조사 입력에 파란불 상황의 생각 문구를 연결합니다.
        } // 단서를 모두 조사했는지 세지 않습니다.
        private void EnterObservation() // 파란불과 접근 차량을 같은 장면에서 직접 조사하게 합니다.
        { // 이전 신호 상황에서 이어진 친구의 실제 첫 발 자세를 유지합니다.
            m_UI.HideAll(); m_RetryRequested = false; m_State = StageState.Observation; SetObservations(true); // 실제 차량 위치와 친구 자세를 저장한 관찰 단계로 들어갑니다.
            if (m_CarChecked) PromptFriend(); else PromptCar(); // 이미 본 차량 설명을 대사 클릭으로 다시 넘기게 하지 않습니다.
        } // 대사를 읽는 중에도 물체 선택은 계속 열려 있습니다.

        private void PromptCar() // 파란불이어도 접근하는 실제 자동차를 살펴보게 합니다.
        { // 신호 변화의 대사와 차량 조사를 한 단계에 연결합니다.
            m_SharedUI.ShowTask(m_CarObservation, "오른쪽에서 오는 자동차를 살펴보세요.", OnCarSelected, "친구", "파란불이다! 건너자."); // 친구는 파란불만 보고 출발하려 하며 접근하는 차의 발견과 판단은 플레이어의 실제 관찰에 맡깁니다.
        } // 화면 아무 곳을 누르는 진행은 없습니다.

        private void OnCarSelected() // 차량을 확인한 결과로 친구의 성급한 출발을 말릴 대화를 엽니다.
        { // 차를 단지 클릭 횟수로 세지 않고 현재 위험을 확인한 상태로 기록합니다.
            m_CarChecked = true; PromptFriend(); // 다음 문장을 넘기는 절차 없이 실제 경고 대화로 이어갑니다.
        } // 다른 물체의 조사도 기존 활성 관찰 범위에서 유지합니다.

        private void PromptFriend() // 아직 차가 지나지 않은 상황에서 친구에게 할 말을 선택하게 합니다.
        { // 친구 클릭은 실제 행동을 제지하는 대화에서만 사용합니다.
            m_SharedUI.ShowTask(m_FriendObservation, "친구에게 다가오는 차를 알려 주세요.", OpenFriendChoices, "", "차가 아직 멈추지 않았어.\n친구에게 어떻게 말해 줄까?"); // 차량 조사 결과가 기다림이나 성급한 출발의 판단으로 이어집니다.
        } // 선택의 결과는 실제 차량 통과 또는 위험 접근으로 보여 줍니다.
        private void OpenFriendChoices() // 친구를 눌렀을 때 차량을 기다릴지 바로 건널지 선택하게 합니다.
        { // 조사한 단서 수와 관계없이 친구에게 답할 수 있습니다.
            if (!IsObservationReady) return; // 이미 대화가 열렸거나 차량 결과가 진행 중이면 새 선택을 시작하지 않습니다.
            m_State = StageState.Choice; SetObservations(true); m_SharedUI.HideAll(); m_UI.ShowChoice(); // 외곽선과 생각을 부드럽게 숨기고 실제 두 선택지를 표시합니다.
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
            yield return m_Performance.LookAtTarget(m_CheckRight, 0.4f); // 차가 지나간 다음 친구도 양쪽 도로를 확인합니다.
            bool crossingRequested = false; m_CrosswalkObservation.SetAvailable(true, FirstSituationObservation.Context.GreenSignal); // 실제 차체가 모두 지나고 양쪽 확인이 끝난 뒤에만 횡단 시작 물체를 엽니다.
            m_SharedUI.ShowTask(m_CrosswalkObservation, "안전해진 횡단보도로 건너세요.", () => crossingRequested = true, "친구", "이제 차가 다 지나갔네.\n양쪽도 확인했어. 같이 건너가자."); // 대사를 닫는 클릭 없이 실제 횡단보도 선택이 보행을 시작합니다.
            while (!crossingRequested) yield return null; // 자동으로 건너지 않고 횡단보도 클릭을 기다립니다.
            SetObservations(false); m_SharedUI.HideAll(); // 횡단을 시작하면 행동 안내와 남은 물체 입력을 정리합니다.
            m_Friend.SetScenarioApproachLimit(null); m_Friend.SetScenarioApproachSpeed(1.35f); // 위험 선택 제한점 없이 반대편 인도까지 걸어가게 합니다.
            Vector3 friendStart = m_Friend.transform.position; // 같은 쪽 인도에서 출발한 친구를 기준으로 플레이어 이동량을 계산합니다.
            if (CarBumperProgress(true) < m_CrosswalkHalfWidth || !m_Friend.CanReceiveCrossCommand) // 통과 상태와 횡단 경로가 유효한 경우에만 둘 다 이동합니다.
            { // 안전 조건이나 이동이 거절되면 마지막 관찰로 돌아갑니다.
                Debug.LogError("차량 통과 확인 후 공통 친구의 안전 횡단을 시작하지 못했습니다.", this); yield return RestoreObservation(); yield break; // 결과가 시작되지 않은 상태에서 완료 처리하지 않습니다.
            } // 실제 횡단이 시작되면 발소리와 플레이어 이동을 함께 연결합니다.
            m_State = StageState.Crossing; m_WalkAudio.Stop();
            yield return ProjectWait.M.Shared.ComfortBlackout.Relocate(m_SharedUI, () =>
            {
                if(!m_Friend.CompleteScenarioCrossingUnderBlackout())return;
                m_Origin.Origin.transform.position=m_ReadyPlayerPosition+(m_Friend.transform.position-friendStart);
                if(m_ViewInput)m_ViewInput.PlaceScenarioHeading(90f,m_Friend.transform.position-friendStart);
                m_Friend.FaceScenarioTarget(m_Viewer.transform);
            });
            if (!m_Friend.HasCompletedCrossing) { Debug.LogWarning("친구가 횡단 도착점에 이르지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 이동이 끊긴 상태로 체험을 완료하지 않습니다.
            m_WalkAudio.Stop(); m_State = StageState.Arrival; // 완전히 도착한 뒤에만 회전을 시작합니다.
            m_SharedUI.HideAll(); // 횡단 중 대사는 닫되 도착 회전이 끝날 때까지 이동 레터박스는 유지합니다.
            m_Friend.FaceScenarioTarget(m_Viewer.transform); // 도착한 친구가 옆의 플레이어를 자연스럽게 돌아봅니다.
            m_SharedUI.SetMovementLetterbox(false);
            StartCoroutine(m_Performance.AcknowledgeWaiting()); // 감사 동작은 결과 진행과 나란히 재생합니다.
            yield return m_SharedUI.PlayCaption("친구", "덕분에 안전하게 건넜어. 같이 살펴봐서 다행이야!", 3f); // 실제 반대편 인도에 도착한 뒤 친구의 감사 대사를 자동으로 보여 줍니다.
            yield return m_SharedUI.PlayCaption("", "집에 가는 길에는 앞을 보고, 신호가 바뀌어도 차를 꼭 확인하자.", 3.5f); // 마지막 정리도 화면 클릭 없이 완료 화면으로 이어집니다.
            m_SharedUI.HideAll(); // 대사를 닫은 뒤 기존 마무리 화면으로 이어갑니다.
            m_Completed = true; m_State = StageState.Complete; m_UI.ShowSafe(); m_OnCompleted.Invoke(); // 횡단보도 파트의 완료만 확정하고 기존 완료 이벤트를 전달합니다.
        } // 이 흐름은 두 번째 친구의 재회 연출에 의존하지 않습니다.

        private IEnumerator RunDanger() // 확인하지 않고 뛰어들면 실제 차량 차선의 같은 교차점 앞에서 급정거가 발생합니다.
        { // 차가 통과할 때까지 다른 위치에서 달리던 기존 연출을 같은 지점의 접근으로 바꿉니다.
            m_Signal.HoldGreenForScenario(); m_Friend.SetScenarioApproachSpeed(m_DangerRunSpeed); m_Friend.SetScenarioRunning(true); // 빠른 이동과 기존 달리기 관절 연출을 함께 시작합니다.
            float arrivalTime = Vector3.ProjectOnPlane(m_NearMissPoint.position - m_Friend.transform.position, Vector3.up).magnitude / m_DangerRunSpeed + m_DangerRunSpeed / 12f; // 차량 차선까지 달리는 실제 거리와 출발 가속 시간으로 접근 시점을 맞춥니다.
            if (!m_Car.PrepareScenarioNearMiss(m_NearMissPoint.position, m_CarBody, m_FriendBody, arrivalTime, 0.4f)) { Debug.LogWarning("차량과 친구의 같은 교차점 급정거를 준비하지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 차가 친구를 지나친 뒤에 놀라는 결과로 넘어가지 않습니다.
            if (!m_Friend.TryBeginUnsafeScenarioCrossing(m_NearMissPoint)) { Debug.LogWarning("친구의 차량 차선 접근을 시작하지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 저장한 첫 발 자세에서 같은 차선까지의 위험 횡단만 시작합니다.
            m_Car.SetScenarioPaused(false); m_WalkAudio.SetActive(true); float deadline = Time.unscaledTime + Mathf.Max(m_Car.ScenarioTravelBudget, m_Friend.ScenarioTravelBudget) + 4f; // 차량과 친구의 실제 이동을 같은 결과 구간에서 재개합니다.
            while (!m_Car.IsScenarioBraking && !m_Car.NeedsScenarioBraking && m_Car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 차량이 친구 앞 급정거 거리까지 다가올 때만 경적과 제동을 시작합니다.
            if (!m_Car.IsScenarioBraking && !m_Car.NeedsScenarioBraking) { Debug.LogWarning("차량이 급정거 위치에 접근하지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 접근이 실패한 상태를 부딪힐 뻔한 결과로 처리하지 않습니다.
            m_Car.BeginScenarioBraking(); m_Second.PlaySharedBrakeEffects(); m_SharedUI.PlayDangerCue(); // 같은 차량의 경적·제동 소리·타이어 자국을 실제 급정거에 연결합니다.
            while (m_Friend.ScenarioLimitRemaining > 0.12f && m_Friend.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 친구는 차량 차선의 교차점까지만 달리고 차를 먼저 가로질러 지나가지 않습니다.
            if (m_Friend.ScenarioLimitRemaining > 0.12f) { Debug.LogWarning("친구가 차량 차선까지 접근하지 못해 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 실제 교차점에 도착한 경우에만 놀람 동작으로 이어갑니다.
            m_WalkAudio.Stop(); m_Friend.SetScenarioRunning(false); yield return m_Performance.PanicNearCar(); // 차량과 같은 차선에서 달리기를 멈추고 급정거를 보고 물러납니다.
            while (!m_Car.HasScenarioStopped && m_Car.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 차량 앞부분이 정해진 안전 간격을 남기고 실제로 정지할 때까지 기다립니다.
            if (!m_Car.HasScenarioStopped) { Debug.LogWarning("차량 급정거가 끝나지 않아 관찰을 복원합니다.", this); yield return RestoreObservation(); yield break; } // 통과 차량을 정지 차량처럼 설명하지 않습니다.
            m_State = StageState.Retry; m_UI.ShowUnsafe(); // 같은 차선에서 부딪힐 뻔한 결과 뒤 기존 재시도 버튼을 표시합니다.
            while (!m_RetryRequested) yield return null; yield return RestoreObservation(); // 다시 해보기를 누르면 저장한 출발 직전 위치와 차량 상태를 복원합니다.
        } // 안전하게 기다리는 선택의 통과 차량 동작은 기존 흐름을 유지합니다.

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
            m_SharedUI.SetMovementLetterbox(false); m_UI.HideAll(); m_SharedUI.HideAll(); m_SharedUI.Transition(true); m_Second.PlaySharedResetSound(); // 기존 짧은 복원 암전과 음향을 사용합니다.
            yield return new WaitForSecondsRealtime(m_SharedUI.FadeCoverDuration); // 위치·자세 복원은 가려진 뒤에만 수행합니다.
            StopEffects(); m_Performance.RestoreObservation(); m_Car.RestoreScenarioCheckpoint(); m_Signal.HoldGreenForScenario(); // 위험 횡단 전 친구와 차량, 신호를 같은 프레임으로 복원합니다.
            m_Origin.Origin.transform.SetPositionAndRotation(m_ReadyPlayerPosition, m_ReadyPlayerRotation); // 위치와 도착 회전 상태를 같은 횡단 전 체크포인트로 복원합니다.
            EnterObservation(); m_SharedUI.Transition(false); // 기존 마우스로 다시 조사하고 두 선택지를 고를 수 있게 합니다.
        } // 플레이어 카메라를 되감거나 이동시키지 않습니다.
        private void StopEffects() // 이전 사고의 소리와 자국이 다음 선택에 남지 않게 합니다.
        { // 먼저 해제된 구성요소에는 접근하지 않습니다.
            if (m_WalkAudio != null) m_WalkAudio.Stop(); // 이번 상황에서 재생한 친구의 보행 음향을 중단합니다.
            if (m_Second != null) m_Second.StopSharedBrakeEffects(); // 같은 기존 차량의 경적·제동·타이어 자국을 초기화합니다.
        } // 앞선 완료 기록은 그대로 유지합니다.

        public void ConfirmCompletion() // 횡단보도 파트의 완료 안내에서 확인 버튼을 처리합니다.
        { // 다른 두 파트의 진행이나 전체 프로그램 종료에는 관여하지 않습니다.
            if (!m_Completed || m_State != StageState.Complete) return; // 실제 횡단 완료 뒤의 확인만 한 번 받습니다.
            m_State = StageState.Ended; m_UI.HideAll(); m_SharedUI.HideAll(); StopEffects(); m_OnFinished.Invoke(); // 이 파트의 안내를 닫고 통합 측에서 연결할 완료 확인 이벤트만 호출합니다.
        } // 앱 종료·플레이 중지·추가 전체 완료 창을 실행하지 않습니다.

        private void OnDisable() // 세 번째 상황 도중 중단되면 같은 인도에서 재개할 상태를 정리합니다.
        { // 이미 도착했다면 완료한 횡단 위치는 보존합니다.
            StopAllCoroutines(); m_QueueFreeze = false; if (!m_Started) return; // 종료된 연출이 관찰을 다시 열지 않도록 예약을 지웁니다.
            if (m_ViewInput != null) m_ViewInput.CancelScenarioViewTurn(); // 회전 도중 중단해도 PC 시점 입력 보류가 남지 않게 합니다.
            SetObservations(false); StopEffects(); if (m_UI != null) m_UI.HideAll(); if (m_Thought != null) m_Thought.Show(false); // 세 번째 상황이 사용한 조사 입력, 소리와 화면을 닫습니다.
            if (m_Performance != null) { m_Performance.StopLookingAt(); BlenderRigPose.ResetFace(m_Performance.HeadTarget); } // 상황을 중단하면 플레이어를 바라보던 고개 제어와 표정도 함께 해제합니다.
            if (!m_Completed) // 다시 시작할 경우 빨간불에서 기다린 위치를 기준으로 이어갑니다.
            { // 위험 횡단 플래그와 차량 경로부터 원래 상태로 되돌립니다.
                if (m_HasCheckpoint && m_Performance != null) m_Performance.RestoreObservation(); // 기존 복원 경로로 같은 친구의 횡단과 자세 상태를 초기화합니다.
                if (m_Car != null) m_Car.RestoreBeforeGreenScenario(); // 먼저 지나가던 차량의 경로와 위치를 돌려줍니다.
                if (m_Friend != null) { m_Friend.SetScenarioPaused(false); m_Friend.PlaceWaitingForScenario(m_ReadyFriendPosition, m_ReadyFriendRotation); } // 두 번째 상황의 실제 대기 위치에 같은 친구를 복원합니다.
                if (m_Origin != null) m_Origin.Origin.transform.SetPositionAndRotation(m_ReadyPlayerPosition, m_ReadyPlayerRotation); // 도착 회전이 중단되어도 다음 실행에 회전량을 누적하지 않습니다.
                if (m_Signal != null) m_Signal.HoldRedForScenario(); // 재진입 때 파란불 전환을 다시 보여 주기 위해 빨간불을 유지합니다.
                m_State = StageState.Waiting; // 완료한 빨간불 상황에서 다시 진입할 수 있게 합니다.
            } // 완료 화면을 닫는 경우에는 이동 결과를 되돌리지 않습니다.
            if (m_SharedUI != null) { m_SharedUI.SetMovementLetterbox(false); m_SharedUI.HideAll(); m_SharedUI.Transition(false); } // 관찰 말풍선이나 재시도 암전이 남지 않게 합니다.
            m_HasCheckpoint = false; m_Started = false; // 이전 실행의 관찰 상태를 다음 실행에서 사용하지 않습니다.
        } // 다시 활성화할 때는 새 관찰 자세를 저장합니다.

        private void OnEnable() // 명시적으로 다시 활성화된 경우 앞선 완료 상태에서 시작합니다.
        { // 처음 활성화할 때는 UI 준비를 위해 Start를 기다립니다.
            if (m_Initialized && m_Second.IsCompleted) BeginAfterSecondSituation(); // 실제로 완료한 상황은 기존 진입 조건이 중복 실행을 막습니다.
        } // 완료 이벤트와 재활성화가 같은 진입점을 사용합니다.
    } // 세 번째이자 마지막 보행 안전 상황을 마칩니다.
} // 점수·도장·새로운 보상 스테이지는 포함하지 않습니다.
