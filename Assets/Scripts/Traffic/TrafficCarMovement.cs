using System; // 감속 중 이동 거리와 정지 시간을 배정밀도로 계산합니다.
using UnityEngine; // 차량의 Transform과 Unity의 프레임 경과 시간을 사용합니다.

namespace Y.Crosswalk // 횡단보도 개인 작업 영역의 차량 기능을 기존 VR 템플릿과 구분합니다.
{ // 기존 보행 신호를 참조하는 차량 이동 컴포넌트의 범위를 시작합니다.
    [DisallowMultipleComponent] // 차량 한 대에 이동 계산이 중복으로 적용되는 것을 막습니다.
    public sealed class TrafficCarMovement : MonoBehaviour // 기존 직선 반복 주행에 보행 신호에 따른 정지와 재출발을 적용합니다.
    { // 이동 설정과 경로 계산에 필요한 상태만 보관합니다.
        private const float k_MinimumPathLength = 0.001f; // 시작점과 종료점이 사실상 같을 때 방향 계산과 나머지 연산을 막습니다.

        [Header("직선 주행 경로")] // 현재 도로에 맞출 시작점과 종료점 설정을 묶습니다.
        [SerializeField, InspectorName("시작 위치(월드 좌표)")] // 부모 오브젝트의 위치와 관계없는 실제 도로 위 출발 좌표를 지정합니다.
        [Tooltip("차량 바닥 중심의 월드 좌표입니다. 도로 표면 높이를 사용하며, 경로 변경은 다음 활성화 시 적용됩니다.")] // 바퀴 중심이 아니라 도로에 놓이는 루트의 위치를 입력하도록 안내합니다.
        private Vector3 m_StartPosition = new Vector3(-37.5f, 0f, -1.75f); // 가까운 차로 중앙의 도로 왼쪽 끝에서 출발합니다.

        [SerializeField, InspectorName("종료 위치(월드 좌표)")] // 횡단보도를 지난 뒤 반복 주행을 시작할 도로 끝을 지정합니다.
        [Tooltip("시작점과 같은 높이의 다른 월드 좌표를 지정합니다. 두 점 모두 차량이 차로 안에 머무는 도로 끝에 배치해 주세요.")] // 현재 평평한 도로의 직선 차로를 따라 설정해야 함을 설명합니다.
        private Vector3 m_EndPosition = new Vector3(37.5f, 0f, -1.75f); // 같은 차로를 유지하며 도로 오른쪽 끝까지 이동합니다.

        [Header("이동 속도")] // 경로와 독립적으로 시험 속도를 조절할 수 있게 합니다.
        [SerializeField, Min(0f), InspectorName("주행 속도(m/s)")] // 초당 이동 거리를 미터 단위로 입력하며 음수 입력을 제한합니다.
        [Tooltip("기본값은 초당 4m입니다. 실행 중 변경할 수 있으며, 0 이하 또는 유효하지 않은 값에서는 감속하여 멈춥니다.")] // 주행 목표 속도 변경에도 설정한 가감속을 적용함을 설명합니다.
        private float m_Speed = 4f; // 어린이가 차량 움직임을 관찰할 수 있는 초기 시험 속도입니다.

        [SerializeField, Min(0.1f), InspectorName("출발 가속도(m/s²)")] // 정지 후 목표 주행 속도로 회복하는 가속도를 조절합니다.
        [Tooltip("다시 출발할 때 초당 증가하는 속도입니다. 기본값 2에서 약 2초에 걸쳐 4m/s에 도달합니다.")] // 가속도 값과 기본 주행 속도의 관계를 안내합니다.
        private float m_Acceleration = 2f; // 보행자 신호가 빨간불로 바뀌면 점진적으로 출발합니다.

        [SerializeField, Min(0.1f), InspectorName("정지 감속도(m/s²)")] // 정지점 접근 시 감속을 시작할 거리를 결정합니다.
        [Tooltip("기본 감속도입니다. 정지점 직전에 신호가 바뀌면 침범하지 않도록 남은 거리에 맞춰 실제 감속도를 높입니다.")] // 늦은 신호 전환에서도 위치를 되돌리거나 정지점을 넘기지 않는 처리를 설명합니다.
        private float m_Deceleration = 3f; // 기본 4m/s 주행에서는 정지점 약 2.67m 전부터 감속합니다.

        [Header("보행 신호와 정지 위치")] // 이동 경로와 별도로 신호 판단에 필요한 씬 참조를 연결합니다.
        [SerializeField, InspectorName("보행자 신호등")] // 머티리얼 대신 공개된 현재 신호 상태를 제공하는 컴포넌트를 참조합니다.
        [Tooltip("이 차량이 따를 보행자 신호등입니다. 빨간불에는 주행하고 초록불에는 정지점 앞에서 대기합니다.")] // 보행자 신호와 차량 이동의 대응 규칙을 안내합니다.
        private PedestrianTrafficLight m_TrafficLight; // 기존 신호등의 전환 주기나 표시 상태는 변경하지 않습니다.

        [SerializeField, InspectorName("차량 중심 정지 지점")] // 씬의 빈 오브젝트로 차량 루트가 멈출 위치를 지정합니다.
        [Tooltip("차량 중심의 정지 위치입니다. 앞범퍼가 중심보다 약 1.99m 앞에 있으므로 정지선 뒤에 여유를 두세요. 위치 변경은 다음 활성화 시 적용됩니다.")] // 모델 앞부분이 횡단보도나 정지선을 침범하지 않도록 배치 기준을 설명합니다.
        private Transform m_StopPoint; // 차량의 자식이 아닌 월드 공간의 고정 정지점을 참조합니다.

        [SerializeField, InspectorName("친구 횡단 판단")] // 초록불에서 수락된 친구의 횡단에 필요한 진입 보호 상태를 읽습니다.
        [Tooltip("씬의 친구에 연결된 FriendCrossingDecision입니다. 친구가 안전한 횡단을 기다리거나 건너는 동안 빨간불로 바뀌어도 정지점 앞에서 대기합니다.")] // 자동 신호를 바꾸지 않고 차량 진입만 보호하는 연결임을 설명합니다.
        private FriendCrossingDecision m_FriendCrossing; // 위험 명령 반응에는 개입하지 않는 최소한의 차량 연동입니다.

        [SerializeField, InspectorName("횡단보도 완전 통과 지점")] // 정지점을 이미 지난 차량이 횡단 구역에서 빠져나간 기준을 지정합니다.
        [Tooltip("CarCrosswalkClearPoint를 연결합니다. 현재 씬의 차량 중심 X=4.25m에서 뒤범퍼도 흰색 줄무늬 구간을 벗어나므로 이 지점 이후 친구가 출발할 수 있습니다.")] // 차량 중심만 중앙선을 지났다고 출발하지 않도록 배치 기준을 안내합니다.
        private Transform m_CrosswalkClearPoint; // 기존 차량 크기와 횡단보도 폭을 기준으로 씬에 배치한 완전 통과 위치입니다.

        private Vector3 m_PathStart; // 활성화 시 확정한 출발 좌표를 보관하여 주행 중 경로가 부분적으로 변경되지 않게 합니다.
        private Vector3 m_PathDirection; // 시작점에서 종료점으로 향하는 단위 방향을 한 번 계산하여 재사용합니다.
        private double m_PathLength; // 반복 구간의 길이를 배정밀도로 보관하여 이동 거리 나머지를 계산합니다.
        private double m_TravelledDistance; // 반복 구간 안에서 이동한 거리를 배정밀도로 누적하여 긴 실행의 정밀도 손실을 줄입니다.
        private double m_StopDistance; // 시작점에서 차량 중심 정지점까지 경로를 따라 잰 거리를 저장합니다.
        private float m_CurrentSpeed; // 목표 속도와 구분되는 현재 속도로 연속적인 감속과 가속을 계산합니다.
        private bool m_HasPassedStopPoint; // 정지점을 지난 회차에는 뒤늦은 초록불로 횡단보도 안에서 멈추지 않게 합니다.
        private bool m_ScenarioPaused; // 관찰 중에는 누적 거리·속도·현재 위치를 그대로 보존합니다.
        public bool IsScenarioPaused => m_ScenarioPaused; // 상황 종료 시 원래 차량 정지 상태를 복원할 수 있게 합니다.
        public void SetScenarioPaused(bool paused) { m_ScenarioPaused = paused; } // 차량 컴포넌트의 재활성화 초기화 없이 이 차량의 주행만 보류합니다.
        private double m_CheckpointDistance, m_NearMissStopDistance; // 관찰 당시 누적 거리와 친구 앞에서 실제로 멈출 경로 거리를 구분합니다.
        private float m_CheckpointSpeed, m_NearMissSpeed, m_ScenarioPlaybackRate = 1f; // 원래 속도와 이번 접근 속도·개별 재생 배율을 따로 보관합니다.
        private bool m_CheckpointPassedStop, m_CheckpointPaused, m_HasCheckpoint, m_NearMissRunning, m_EmergencyBraking; // 재시도 복원과 이번 급정거의 중복 실행을 관리합니다.
        public bool NeedsScenarioBraking => m_NearMissRunning && !m_EmergencyBraking && m_NearMissStopDistance - m_TravelledDistance <= Mathf.Max(1.1f, m_CurrentSpeed * 0.4f); // 차체가 친구 앞 정지점에 가까워진 순간만 경적·급정거의 시작 조건으로 제공합니다.
        public bool HasScenarioStopped => m_NearMissRunning && m_EmergencyBraking && m_CurrentSpeed <= 0.01f && m_NearMissStopDistance - m_TravelledDistance <= 0.002d; // 실제 이동과 제동이 끝난 뒤에만 사고 직전 정지를 완료로 알립니다.
        public bool IsScenarioBraking => m_NearMissRunning && m_EmergencyBraking; // 차량 LateUpdate에서 먼저 시작한 제동도 다음 입력 프레임에 정확히 알립니다.
        public void SetScenarioPlaybackRate(float rate) { m_ScenarioPlaybackRate = Mathf.Clamp(rate, 0.08f, 1f); } // 해당 차량만 느리게 움직이며 신호·XR 입력의 시간에는 관여하지 않습니다.
        public void BeginScenarioBraking() { if (m_NearMissRunning) m_EmergencyBraking = true; } // 한 번 시작한 급정거는 같은 정지점까지 이어지게 합니다.

        private double m_GreenSignalChangeDistance; // 세 번째 접근 차량의 위치에 맞춰 보행 신호를 바꿀 경로 거리입니다.
        private bool m_GreenRoutePrepared, m_KeepApproachSpeed, m_BeforeGreenPassed, m_BeforeGreenPaused; // 세 번째 상황에서만 차로를 전환하고 원래 주행 상태를 보존합니다.
        private Vector3 m_BeforeGreenPathStart, m_BeforeGreenDirection, m_BeforeGreenPosition; // 기존 차로와 실제 차량 위치를 복원할 기준입니다.
        private Quaternion m_BeforeGreenRotation; // 원래 차량의 주행 방향을 보관합니다.
        private double m_BeforeGreenLength, m_BeforeGreenDistance; // 원래 경로 길이와 누적 거리를 따로 저장합니다.
        private float m_BeforeGreenSpeed; // 원래 정상 주행 속도를 보관합니다.
        public float ScenarioTravelBudget => (float)Math.Max(0d, (m_NearMissRunning ? m_NearMissStopDistance : m_PathLength) - m_TravelledDistance) / Mathf.Max(0.2f, m_GreenRoutePrepared ? m_CurrentSpeed : m_NearMissRunning ? m_NearMissSpeed : m_Speed) + 6f; // 세 번째 통과는 현재 접근 속도로 대기 상한을 계산합니다.
        public bool HasPassedCrosswalkForScenario => m_CrosswalkClearPoint != null && m_HasPassedStopPoint && m_TravelledDistance >= Vector3.Dot(m_CrosswalkClearPoint.position - m_PathStart, m_PathDirection); // 두 번째 상황의 차량 뒤쪽까지 횡단 구역을 빠져나간 사실을 제공합니다.
        public bool HasReachedGreenSignalChangePoint => m_GreenRoutePrepared && m_TravelledDistance >= m_GreenSignalChangeDistance; // 차가 다가오는 동안에만 세 번째 보행 신호를 전환하게 합니다.

        public bool PrepareGreenScenarioRoute(Transform entry, Transform exit, Transform signalChangePoint, float approachSpeed) // 세 번째 차량이 같은 차로를 일정한 속도로 한 번 통과하도록 준비합니다.
        { // 첫 번째·두 번째 주행 설정과 차량 인스턴스를 보존합니다.
            if (m_GreenRoutePrepared || !isActiveAndEnabled || entry == null || exit == null || signalChangePoint == null || !IsFinite(approachSpeed) || approachSpeed <= 0f) return false; // 연결된 차로와 양수 속도가 있을 때만 세 번째 통과를 준비합니다.
            Vector3 path = exit.position - entry.position; float length = path.magnitude; // 기존 도로 위 세 번째 차로의 길이를 구합니다.
            if (length < 1f || Mathf.Abs(path.y) > 0.01f) return false; // 바닥 높이가 다른 경로나 지나치게 짧은 경로는 사용하지 않습니다.
            Vector3 direction = path / length; float signalChangeDistance = Vector3.Dot(signalChangePoint.position - entry.position, direction); // 차로를 따라 신호가 바뀔 위치를 구합니다.
            if (signalChangeDistance <= 0f || signalChangeDistance >= length) return false; // 실제로 달리는 경로 안에서만 보행 신호를 바꿉니다.
            m_BeforeGreenPathStart = m_PathStart; m_BeforeGreenDirection = m_PathDirection; m_BeforeGreenPosition = transform.position; m_BeforeGreenRotation = transform.rotation; // 실제 기존 배치와 방향을 저장합니다.
            m_BeforeGreenLength = m_PathLength; m_BeforeGreenDistance = m_TravelledDistance; m_BeforeGreenSpeed = m_CurrentSpeed; // 앞선 상황의 주행 상태를 복원할 수 있게 저장합니다.
            m_BeforeGreenPassed = m_HasPassedStopPoint; m_BeforeGreenPaused = m_ScenarioPaused; m_GreenRoutePrepared = true; // 세 번째 상황만 경로를 소유하고 있음을 기록합니다.
            m_PathStart = entry.position; m_PathDirection = direction; m_PathLength = length; m_GreenSignalChangeDistance = signalChangeDistance; m_TravelledDistance = 0d; // 기존 정지선과 별도로 세 번째 차로와 신호 전환 위치를 저장합니다.
            m_CurrentSpeed = approachSpeed; m_HasPassedStopPoint = false; m_ScenarioPaused = true; m_NearMissRunning = false; m_EmergencyBraking = false; m_KeepApproachSpeed = false; m_ScenarioPlaybackRate = 1f; // 재개할 때 바로 지정한 접근 속도로 진행하도록 준비합니다.
            transform.SetPositionAndRotation(entry.position, Quaternion.LookRotation(direction)); // 기존 차체를 올바른 통행 방향으로 같은 도로에 배치합니다.
            return true; // 암전이 걷힌 뒤 같은 속도로 횡단보도를 통과할 준비가 끝났습니다.
        } // XR Origin과 도로·횡단보도는 이동하지 않습니다.

        public void RestoreBeforeGreenScenario() // 세 번째 상황을 중간에 해제할 때 원래 주행 경로를 복원합니다.
        { // 재시도는 이 함수 대신 세 번째 관찰 체크포인트만 사용합니다.
            if (!m_GreenRoutePrepared) return; // 이전 두 상황의 일반 주행에는 개입하지 않습니다.
            m_PathStart = m_BeforeGreenPathStart; m_PathDirection = m_BeforeGreenDirection; m_PathLength = m_BeforeGreenLength; // 앞선 상황이 사용하던 차로를 되돌립니다.
            m_TravelledDistance = m_BeforeGreenDistance; m_CurrentSpeed = m_BeforeGreenSpeed; m_HasPassedStopPoint = m_BeforeGreenPassed; m_ScenarioPaused = m_BeforeGreenPaused; // 원래 이동 상태를 복원합니다.
            m_NearMissRunning = false; m_EmergencyBraking = false; m_KeepApproachSpeed = false; m_ScenarioPlaybackRate = 1f; m_GreenRoutePrepared = false; m_HasCheckpoint = false; // 세 번째 통과와 그 관찰 체크포인트를 종료합니다.
            transform.SetPositionAndRotation(m_BeforeGreenPosition, m_BeforeGreenRotation); // 실제 차량 배치도 저장한 같은 상태로 돌립니다.
        } // 이전 상황의 완료 기록은 변경하지 않습니다.

        public void CaptureScenarioCheckpoint() // 빨간불 관찰을 시작한 순간의 실제 주행 상태를 저장합니다.
        { // 활성화 초기화나 출발 지점 재배치를 사용하지 않습니다.
            m_CheckpointDistance = m_TravelledDistance; m_CheckpointSpeed = m_CurrentSpeed; // 다음 프레임에도 이어질 누적 거리와 속도를 함께 보관합니다.
            m_CheckpointPassedStop = m_HasPassedStopPoint; m_CheckpointPaused = m_ScenarioPaused; m_HasCheckpoint = true; // 기존 정지선 통과 여부와 관찰 정지를 같은 시점에 저장합니다.
        } // 첫 번째 상황의 차량 주행에는 영향을 주지 않습니다.

        public void RestoreScenarioCheckpoint() // 사고 뒤 차량을 관찰 당시 경로와 속도로 복원합니다.
        { // 같은 차량 인스턴스의 기존 신호 연동은 계속 유지합니다.
            if (!m_HasCheckpoint) return; // 관찰을 시작하기 전의 취소에서는 저장되지 않은 위치를 적용하지 않습니다.
            m_NearMissRunning = false; m_EmergencyBraking = false; m_KeepApproachSpeed = false; m_ScenarioPlaybackRate = 1f; // 급정거와 느린 동작이 다음 선택에 남지 않게 초기화합니다.
            m_TravelledDistance = m_CheckpointDistance; m_CurrentSpeed = m_CheckpointSpeed; m_HasPassedStopPoint = m_CheckpointPassedStop; m_ScenarioPaused = m_CheckpointPaused; // 주행 계산의 원래 상태를 전부 되돌립니다.
            transform.position = m_PathStart + m_PathDirection * (float)m_TravelledDistance; // 보이는 위치도 같은 누적 거리에서 즉시 복원합니다.
        } // 자동차를 껐다 켜서 경로나 회차를 초기화하지 않습니다.

        public bool PrepareScenarioNearMiss(Vector3 crossingPoint, Collider carBody, Collider friendBody, float arrivalTime, float clearance, bool preserveSpeed = false) // 기존 차로에서 친구의 횡단 위치 직전까지 접근할 주행만 준비합니다.
        { // 친구와 차체의 실제 Collider 크기를 사용하여 겹치지 않는 정지점을 구합니다.
            if (!m_ScenarioPaused || carBody == null || friendBody == null || m_PathLength <= k_MinimumPathLength) return false; // 관찰 중 정지한 유효한 차량만 위험 선택에 사용할 수 있습니다.
            Vector3 axis = new Vector3(Mathf.Abs(m_PathDirection.x), Mathf.Abs(m_PathDirection.y), Mathf.Abs(m_PathDirection.z)); // 경로 방향에 투영할 월드 AABB의 양수 축을 만듭니다.
            Bounds friendBounds = friendBody.bounds; FriendCharacterMovement friend = friendBody.GetComponentInParent<FriendCharacterMovement>(); // 몸 Collider보다 바깥으로 나오는 팔과 시각 모델도 정지 여유에 포함합니다.
            if (friend != null) foreach (Renderer visual in friend.GetComponentsInChildren<Renderer>(true)) if (visual.enabled && visual.gameObject.activeInHierarchy) friendBounds.Encapsulate(visual.bounds); // 꺼진 관찰 테두리는 제외하고 실제 캐릭터 전체의 범위를 보수적으로 확보합니다.
            float separation = Vector3.Dot(carBody.bounds.extents + friendBounds.extents, axis) + Mathf.Max(0.3f, clearance); // 앞범퍼와 친구의 팔·몸 표면 사이에 설정한 여유 거리를 확보합니다.
            m_NearMissStopDistance = Vector3.Dot(crossingPoint - m_PathStart, m_PathDirection) - separation; // 차량 중심의 정지 위치를 기존 누적 거리 단위로 환산합니다.
            double remaining = m_NearMissStopDistance - m_TravelledDistance; // 현재 관찰 위치에서 움직일 수 있는 거리를 구합니다.
            if (remaining <= 1.5d || m_NearMissStopDistance >= m_PathLength) return false; // 정지 여유가 없는 배치를 순간 이동으로 보정하거나 친구를 관통하게 두지 않습니다.
            m_NearMissSpeed = Mathf.Clamp((float)(remaining - 1.1d) / Mathf.Max(1f, arrivalTime - 0.35f), 0.4f, Mathf.Max(0.4f, m_Speed)); // 친구의 실제 보행 시간에 맞춰 기존 차로에서 접근하고 마지막 구간은 급정거에 남깁니다.
            m_KeepApproachSpeed = preserveSpeed; if (!preserveSpeed) m_CurrentSpeed = m_NearMissSpeed; m_NearMissRunning = true; m_EmergencyBraking = false; m_ScenarioPlaybackRate = 1f; // 정지된 장면에서 결과 전용 접근 속도를 확정합니다.
            return true; // 위치나 회전을 바꾸지 않고 다음 주행 프레임부터 접근할 수 있게 합니다.
        } // 일반 신호에 따른 차량 정지와 반복 주행은 별도 기존 경로를 유지합니다.

        private void AdvanceScenarioNearMiss(float deltaTime) // 이 차량의 최종 위치를 적용하는 기존 LateUpdate에서만 위험 접근과 제동을 진행합니다.
        { // 외부 연출 스크립트가 차량 Transform을 동시에 덮어쓰지 않게 합니다.
            double remaining = Math.Max(0d, m_NearMissStopDistance - m_TravelledDistance); // 정지점 뒤로 넘어갈 수 없는 남은 거리를 구합니다.
            if (NeedsScenarioBraking) m_EmergencyBraking = true; // 긴 프레임에서도 제동 시작점을 지나 정상 주행을 계속하지 않게 합니다.
            double step; // 이번 프레임에 실제로 이동할 거리를 보관합니다.
            if (m_EmergencyBraking) // 남은 거리에 맞춰 현재 속도에서 연속적으로 급감속합니다.
            { // 차량이 정지점을 지나거나 친구와 겹치지 않도록 적분 결과도 제한합니다.
                double deceleration = remaining > 0.001d ? Math.Max(0.05d, m_CurrentSpeed * (double)m_CurrentSpeed / (2d * remaining)) : 1000d; // 현재 속도로 지정한 지점에 정확히 정지할 감속도를 구합니다.
                double brakingTime = Math.Min(deltaTime, m_CurrentSpeed / deceleration); // 정지한 뒤 남은 프레임 시간에는 이동하지 않습니다.
                float nextSpeed = (float)Math.Max(0d, m_CurrentSpeed - deceleration * brakingTime); // 이번 제동 구간의 마지막 속도를 구합니다.
                step = (m_CurrentSpeed + (double)nextSpeed) * 0.5d * brakingTime; m_CurrentSpeed = nextSpeed; // 평균 속도로 급정거 거리를 계산합니다.
            } // 급정거 효과와 음향은 상황 제어기가 한 번만 시작합니다.
            else if (m_KeepApproachSpeed) // 세 번째 상황에서는 관찰 당시 속도를 순간적으로 바꾸지 않습니다.
            { // 기존 접근 속도에서 연출 속도로 부드럽게 이어갑니다.
                float nextSpeed = Mathf.MoveTowards(m_CurrentSpeed, m_NearMissSpeed, Mathf.Max(0.1f, m_Deceleration) * deltaTime); // 실제 감속 시간에 따라 속도를 줄입니다.
                step = (m_CurrentSpeed + nextSpeed) * 0.5d * deltaTime; m_CurrentSpeed = nextSpeed; // 같은 평균 속도로 실제 이동 거리도 계산합니다.
            } // 마지막 급정거는 위의 동일한 정지 거리 제한을 사용합니다.
            else step = m_NearMissSpeed * deltaTime; // 위험 접근 전반부도 같은 기존 차로 위에서 이동합니다.
            m_TravelledDistance += Math.Min(step, remaining); // 어떤 프레임 길이에서도 차체 중심이 안전 정지점보다 앞으로 나가지 않게 합니다.
            if (m_NearMissStopDistance - m_TravelledDistance <= 0.001d || m_EmergencyBraking && m_CurrentSpeed <= 0.01f && remaining <= 0.02d) { m_TravelledDistance = m_NearMissStopDistance; m_CurrentSpeed = 0f; m_EmergencyBraking = true; } // 마지막 미세 오차를 정리하고 정지 상태를 유지합니다.
            m_HasPassedStopPoint = m_TravelledDistance > m_StopDistance; // 원래 신호 정지선 기준도 현재 위치와 일치시킵니다.
            transform.position = m_PathStart + m_PathDirection * (float)m_TravelledDistance; // 기존 경로 계산을 사용하여 차량 위치를 한 곳에서만 적용합니다.
        } // 이 결과에서는 정지 후 차량이 다시 가속하거나 반복 경로로 돌아가지 않습니다.

        public void PlaceForScenario(Vector3 position) // 암전 중 기존 주행 경로 위의 상황 시작 위치를 배치합니다.
        { // 새 경로나 별도 차량 이동기를 만들지 않습니다.
            if (!isActiveAndEnabled || m_PathLength <= k_MinimumPathLength) return; // 기존 주행 경로가 준비된 차량만 배치합니다.
            m_TravelledDistance = Math.Min(Math.Max(0d, Vector3.Dot(position - m_PathStart, m_PathDirection)), m_PathLength - k_MinimumPathLength); // 기존 경로의 실제 누적 거리로 시작점을 환산합니다.
            m_HasPassedStopPoint = m_TravelledDistance > m_StopDistance; // 배치한 위치가 실제 정지점을 지났는지 같은 기준으로 기록합니다.
            m_CurrentSpeed = IsFinite(m_Speed) && m_Speed > 0f ? m_Speed : 0f; // 빨간 보행 신호에서 기존 설정 속도로 주행을 이어갑니다.
            transform.position = m_PathStart + m_PathDirection * (float)m_TravelledDistance; // 차량 Transform과 다음 프레임의 거리 계산이 같은 위치에서 시작하게 합니다.
        } // 기존 신호 제동과 반복 주행은 그대로 사용합니다.

        public bool IsClearForFriendCrossing => !gameObject.activeInHierarchy || !m_HasPassedStopPoint || (m_CrosswalkClearPoint != null && m_TravelledDistance >= Vector3.Dot(m_CrosswalkClearPoint.position - m_PathStart, m_PathDirection)); // 정지점 앞에서 진입을 막을 수 있거나 차량 뒤쪽까지 완전히 통과한 경우에만 친구의 출발을 허용합니다.

        private void OnEnable() // 씬 시작이나 차량 재활성화 시 지정된 경로의 출발점부터 주행합니다.
        { // 유효한 경로에서만 위치와 진행 방향을 초기화합니다.
            if (!TryPreparePath() || !TryPrepareStopPoint()) // 기존 경로와 새 신호·정지점 연결이 모두 유효할 때만 주행을 시작합니다.
            { // 사용할 수 없는 방향으로 회전하거나 0으로 나누는 계산을 방지합니다.
                enabled = false; // 오류가 있는 경로에서 매 프레임 같은 계산을 반복하지 않습니다.
                return; // Transform에 비정상적인 좌표가 전달되기 전에 초기화를 마칩니다.
            } // 유효한 두 점이 연결된 경우에만 출발 상태를 적용합니다.

            m_TravelledDistance = 0d; // 이전 활성화에서 이동한 거리를 남기지 않고 처음부터 출발합니다.
            m_CurrentSpeed = IsFinite(m_Speed) && m_Speed > 0f ? m_Speed : 0f; // 씬 시작 시 기존 기본 주행 속도를 유지하고 비정상 속도는 0으로 취급합니다.
            m_HasPassedStopPoint = false; // 출발점으로 돌아온 차량은 이번 회차의 신호를 다시 따라야 합니다.
            transform.SetPositionAndRotation(m_PathStart, Quaternion.LookRotation(m_PathDirection, Vector3.up)); // 모델의 로컬 +Z 앞면을 실제 경로 방향에 맞추고 도로 위에 놓습니다.
        } // 주행 중에는 경로 방향을 다시 돌리지 않도록 최초 회전을 확정합니다.

        private void LateUpdate() // 신호등의 Update가 현재 프레임의 신호를 적용한 다음 차량 이동을 판단합니다.
        { // 신호 판단과 이동 거리 계산을 분리하고 차량 루트만 갱신합니다.
            if (m_ScenarioPaused) return; // 관찰 중에는 속도 적분과 위치 적용을 함께 정지합니다.
            float deltaTime = Time.deltaTime * m_ScenarioPlaybackRate; // 개별 차량의 느린 동작만 반영하며 전역 시간은 유지합니다. // 게임 시간 배율과 일시정지를 따르는 이번 프레임의 경과 초를 사용합니다.
            if (deltaTime <= 0f) // 일시정지 상태에서는 속도와 위치를 함께 유지합니다.
                return; // 정지된 시간에 가감속 계산을 수행하지 않습니다.

            if (m_GreenRoutePrepared) // 세 번째에 연출되는 차량은 보행 신호가 바뀐 뒤에도 같은 속도로 통과합니다.
            { // 관찰로 정지했다 재개해도 저장한 접근 속도와 위치에서 이어집니다.
                m_TravelledDistance = Math.Min(m_PathLength, m_TravelledDistance + m_CurrentSpeed * deltaTime); // 한 번 지나간 차량이 횡단 중 반대편 시작점에 다시 나타나지 않게 합니다.
                transform.position = m_PathStart + m_PathDirection * (float)m_TravelledDistance; // 같은 차로 위에서 도로 끝까지 연속적으로 이동합니다.
                if (m_TravelledDistance >= m_PathLength) m_ScenarioPaused = true; // 통과를 끝낸 차량은 차로 끝에서 계산을 멈춥니다.
                return; // 이 차량에만 앞선 상황의 신호 정지나 급제동을 적용하지 않습니다.
            } // 세 번째 경로를 해제하면 기존 신호 주행으로 돌아갑니다.
            if (m_NearMissRunning) { AdvanceScenarioNearMiss(deltaTime); return; } // 위험 선택 동안만 같은 차량의 경로 소유권을 급정거 분기에 넘깁니다.
            double step = CalculateMovementStep(deltaTime, MustStopForPedestrians()); // 현재 신호를 판단한 뒤 가감속을 반영한 실제 전진 거리를 계산합니다.
            double distance = m_TravelledDistance + step; // 기존 경로에서 이번 프레임이 끝났을 때의 진행 거리를 구합니다.
            if (distance >= m_PathLength) // 도로 끝에서 시작점으로 반복 배치할 때만 이번 회차의 통과 상태를 초기화합니다.
                m_HasPassedStopPoint = false; // 다음 회차에는 초록불에 다시 정지할 수 있게 합니다.

            m_TravelledDistance = distance % m_PathLength; // 도로 양끝의 기존 반복 배치와 초과 거리 보존을 유지합니다.
            if (m_TravelledDistance > m_StopDistance) // 차량 중심이 정지점을 실제로 지난 뒤에는 그 회차의 통과를 확정합니다.
                m_HasPassedStopPoint = true; // 신호가 바뀌어도 이미 진입한 차량을 멈추거나 뒤로 보내지 않습니다.

            transform.position = m_PathStart + m_PathDirection * (float)m_TravelledDistance; // 확정된 직선 위에 차량 전체를 배치하고 이동 중 회전은 변경하지 않습니다.
        } // 한 프레임에 차량 루트의 위치를 한 번만 적용합니다.

        private bool MustStopForPedestrians() // 신호 판단을 가감속과 위치 계산에서 분리합니다.
        { // 기존 신호등의 공개 상태만 사용하고 표시 색상이나 머티리얼은 검사하지 않습니다.
            bool greenSignal = m_TrafficLight != null && m_TrafficLight.isActiveAndEnabled && m_TrafficLight.CurrentState == PedestrianTrafficLight.SignalState.Green; // 기존 보행자 초록불의 정지 조건을 유지합니다.
            bool protectedCrossing = m_FriendCrossing != null && m_FriendCrossing.RequiresTrafficHold; // 명령 때 초록불이었던 친구의 횡단이 끝날 때까지 신호 변경과 무관하게 진입을 막습니다.
            return !m_HasPassedStopPoint && (greenSignal || protectedCrossing); // 이미 정지점을 지난 차량은 계속 통과시키고 다음 차량 진입만 기존 제동으로 차단합니다.
        } // 신호 자동 전환과 차량의 반복 주행·가감속 계산은 그대로 유지합니다.

        private double CalculateMovementStep(float deltaTime, bool mustStop) // 현재 속도를 연속적으로 바꾸면서 이번 프레임의 전진 거리를 구합니다.
        { // 먼저 목표 속도로 향하는 일반 가감속을 계산하고 필요한 경우 정지점 접근 계산을 적용합니다.
            float targetSpeed = IsFinite(m_Speed) && m_Speed > 0f ? m_Speed : 0f; // 잘못된 목표 속도는 후진 대신 정지 요청으로 처리합니다.
            float acceleration = IsFinite(m_Acceleration) ? Mathf.Max(0.1f, m_Acceleration) : 2f; // 가속도는 양수로 제한하고 유효하지 않으면 기본값을 사용합니다.
            float deceleration = IsFinite(m_Deceleration) ? Mathf.Max(0.1f, m_Deceleration) : 3f; // 감속 계산과 제동거리 나눗셈에 0이나 비정상 값이 들어가지 않게 합니다.
            float rate = targetSpeed > m_CurrentSpeed ? acceleration : deceleration; // 현재 속도와 목표 속도의 관계에 맞는 가속 또는 감속을 선택합니다.
            float nextSpeed = Mathf.MoveTowards(m_CurrentSpeed, targetSpeed, rate * deltaTime); // 한 프레임에 허용된 속도 변화만 적용하여 출발과 속도 변경을 부드럽게 만듭니다.
            double changeTime = Math.Min(deltaTime, Math.Abs((double)targetSpeed - m_CurrentSpeed) / rate); // 프레임 중간에 목표 속도에 도달하면 실제 가감속 구간만 따로 계산합니다.
            double step = (m_CurrentSpeed + (double)nextSpeed) * 0.5d * changeTime + nextSpeed * (deltaTime - changeTime); // 가감속 구간과 남은 정속 구간의 거리를 적분하여 프레임 길이에 따른 차이를 줄입니다.

            if (mustStop) // 초록불 또는 진행 중인 안전 횡단 때문에 정지점 앞에서 기다려야 하는 경우 기존 제동을 적용합니다.
            { // 남은 거리 안에서 속도를 0으로 만들고 정지점을 넘지 않도록 제한합니다.
                double remaining = Math.Max(0d, m_StopDistance - m_TravelledDistance); // 정지점까지의 전방 거리만 사용하여 후진 거리가 생기지 않게 합니다.
                double brakingDistance = (double)nextSpeed * nextSpeed / (2d * deceleration); // 현재 설정으로 다음 속도에서 완전히 멈추는 데 필요한 거리를 구합니다.
                if (remaining > k_MinimumPathLength && m_CurrentSpeed > 0f && step + brakingDistance >= remaining) // 다음 프레임까지 기다리면 제동 시작 시점이 늦어질 때 감속을 시작합니다.
                { // 이번 프레임 시작 속도와 실제 남은 거리에 맞는 연속적인 제동 곡선을 계산합니다.
                    double brakingRate = (double)m_CurrentSpeed * m_CurrentSpeed / (2d * remaining); // 정지점 직전의 신호 변경에도 남은 거리 안에서 멈출 감속도를 계산합니다.
                    double brakingTime = Math.Min(deltaTime, m_CurrentSpeed / brakingRate); // 프레임 중간에 완전히 멈추면 그 시각까지만 이동을 적분합니다.
                    nextSpeed = (float)Math.Max(0d, m_CurrentSpeed - brakingRate * brakingTime); // 속도를 음수로 만들지 않아 정지점에서 후진하지 않게 합니다.
                    step = (m_CurrentSpeed + (double)nextSpeed) * 0.5d * brakingTime; // 제동 중 평균 속도와 실제 제동 시간으로 이동 거리를 구합니다.
                } // 제동 구간에서는 속도와 위치를 같은 감속식으로 갱신합니다.

                if (remaining <= k_MinimumPathLength || step >= remaining - k_MinimumPathLength) // 마지막 1mm 이하의 오차나 정지점 도달을 한 번에 정리합니다.
                { // 수치 오차로 정지점 주변을 반복 통과하거나 앞뒤로 흔들리지 않게 합니다.
                    m_CurrentSpeed = 0f; // 정지 상태의 속도를 정확히 0으로 고정합니다.
                    return remaining; // 차량 중심을 정지점에 맞추되 이전 위치보다 뒤로 이동시키지 않습니다.
                } // 빨간불이 될 때까지 같은 위치와 0의 속도를 유지할 수 있습니다.
            } // 정지점 이후의 차량에는 제동 계산을 적용하지 않습니다.

            m_CurrentSpeed = nextSpeed; // 다음 프레임의 가감속이 이번 프레임의 실제 종료 속도에서 이어지게 합니다.
            return step; // 계산된 전진 거리를 기존 반복 경로 처리에 전달합니다.
        } // 신호 판단과 독립적인 속도·거리 계산을 마칩니다.

        private bool TryPrepareStopPoint() // 기존 경로 위의 정지점과 보행 신호 참조를 활성화 시 준비합니다.
        { // 씬에서 연결한 정지점이 차량이 실제로 이동하는 직선 구간 안에 있는지 확인합니다.
            if (m_TrafficLight == null || m_StopPoint == null || !IsFinite(m_StopPoint.position)) // 누락된 씬 참조나 유효하지 않은 정지 좌표로 신호 주행이 시작되지 않게 합니다.
            { // 필요한 연결을 인스펙터에서 수정할 수 있도록 해당 차량을 알려 줍니다.
                Debug.LogError("차량에 보행자 신호등과 유효한 차량 중심 정지 지점을 연결해 주세요.", this); // 신호·정지점 연결이 없는 경우를 명확히 표시합니다.
                return false; // 호출부가 불완전한 설정에서 주행을 비활성화하게 합니다.
            } // 두 씬 참조가 정상일 때만 경로상의 거리를 계산합니다.

            float distance = Vector3.Dot(m_StopPoint.position - m_PathStart, m_PathDirection); // 정지점 위치를 차량의 기존 진행 방향에 투영하여 출발점으로부터의 거리를 구합니다.
            Vector3 projectedPoint = m_PathStart + m_PathDirection * distance; // 정지점이 실제 경로 위에 놓였는지 비교할 위치를 구합니다.
            if (!IsFinite(distance) || distance <= k_MinimumPathLength || distance >= m_PathLength || Vector3.Distance(projectedPoint, m_StopPoint.position) > 0.05f) // 출발점 뒤, 종료점 이후 또는 차로 경로에서 벗어난 정지점을 거부합니다.
            { // 차량이 경로를 이탈하거나 정지점을 찾아 후진하는 동작을 허용하지 않습니다.
                Debug.LogError("차량 중심 정지 지점은 시작점과 종료점 사이의 주행 경로 위에 배치해 주세요.", this); // 씬에서 조정할 정지점의 배치 조건을 안내합니다.
                return false; // 올바른 경로에 배치한 뒤 다시 활성화하도록 초기화를 중단합니다.
            } // 경로 내부의 정상 정지점만 이번 회차에 사용합니다.

            m_StopDistance = distance; // 차량의 기존 누적 이동 거리와 같은 기준으로 정지 위치를 저장합니다.
            return true; // 정지점과 신호가 준비되어 기존 출발 위치를 적용할 수 있음을 알립니다.
        } // 차량 모델이나 주행 경로 자체를 수정하지 않고 정지 설정을 준비합니다.

        private bool TryPreparePath() // 인스펙터의 두 점을 현재 평평한 도로에서 사용할 수 있는 경로로 준비합니다.
        { // 좌표 유효성과 경로 길이를 확인한 뒤 필요한 값만 저장합니다.
            if (!IsFinite(m_StartPosition) || !IsFinite(m_EndPosition)) // 어느 축에든 NaN 또는 무한대가 있으면 위치 계산을 시작하지 않습니다.
            { // 유효하지 않은 월드 좌표는 Unity Transform으로 전달하지 않습니다.
                Debug.LogError("차량의 시작 위치와 종료 위치에는 유한한 숫자를 입력해 주세요.", this); // 문제가 있는 차량 컴포넌트와 좌표 입력 오류를 함께 알립니다.
                return false; // 호출부가 차량 이동을 비활성화하도록 경로 준비 실패를 알립니다.
            } // 모든 좌표 성분이 정상인 두 점만 방향 계산에 사용합니다.

            Vector3 offset = m_EndPosition - m_StartPosition; // 출발점에서 도착점까지의 실제 월드 방향과 거리를 구합니다.
            float length = offset.magnitude; // 두 지점 사이의 직선 길이를 미터 단위로 계산합니다.
            if (!IsFinite(length) || length <= k_MinimumPathLength || Mathf.Abs(offset.y) > k_MinimumPathLength) // 지나치게 큰 값, 겹친 두 점 또는 도로와 다른 높이의 경로를 거부합니다.
            { // 현재 씬의 평평한 도로에서 벗어나는 경로로 주행하지 않게 합니다.
                Debug.LogError("차량 경로는 같은 높이에 있는 서로 다른 두 점으로 지정해 주세요. 좌표가 너무 크지 않은지도 확인해 주세요.", this); // 올바른 직선 경로를 만들기 위한 입력 조건을 안내합니다.
                return false; // 0에 가까운 길이로 나누거나 수직 방향으로 차를 세우는 계산을 중단합니다.
            } // 현재 차로 높이를 유지하는 유효한 직선만 저장합니다.

            m_PathStart = m_StartPosition; // 이번 활성화에서 사용할 경로의 출발점을 고정합니다.
            m_PathDirection = offset / length; // 길이에 영향받지 않는 단위 방향을 만들어 속도를 일정하게 유지합니다.
            m_PathLength = length; // 끝점 재배치 계산에 사용할 전체 경로 길이를 보관합니다.
            return true; // 출발 위치와 진행 방향을 적용해도 되는 경로임을 알립니다.
        } // 경로 준비 이후에는 매 프레임 같은 방향이나 길이를 다시 계산하지 않습니다.

        private static bool IsFinite(float value) // 속도나 좌표 성분을 이동 계산에 사용할 수 있는지 판단합니다.
        { // Unity 6에서 사용할 수 있는 숫자 판별 함수로 유효하지 않은 값을 걸러냅니다.
            return !float.IsNaN(value) && !float.IsInfinity(value); // 정의되지 않은 수와 양방향 무한대를 모두 제외합니다.
        } // 유한한 실수만 위치와 거리 계산에 사용하도록 결과를 돌려줍니다.

        private static bool IsFinite(Vector3 value) // 시작점과 종료점의 세 좌표를 같은 기준으로 판단합니다.
        { // 한 축만 잘못되어도 차량 위치 전체가 손상되는 것을 방지합니다.
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z); // 모든 좌표 성분이 유한할 때만 정상 벡터로 취급합니다.
        } // 경로의 두 점을 검사하는 공통 숫자 조건을 마칩니다.
    } // 차량 한 대의 기존 반복 주행과 보행 신호에 따른 정지·재출발만 포함합니다.
} // 횡단보도 차량 코드의 범위를 마칩니다.
