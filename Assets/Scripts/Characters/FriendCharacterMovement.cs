using UnityEngine; // 캐릭터 루트의 이동과 기존 뼈대의 Animator를 제어합니다.

namespace Y.Crosswalk // 차량·신호등과 같은 개인 작업 영역에 두되 해당 기능을 직접 참조하지 않습니다.
{ // 친구 캐릭터의 한 번 걷기 기능을 선언합니다.
    [DisallowMultipleComponent] // 한 캐릭터에 이동 스크립트가 중복되어 위치를 덮어쓰는 일을 막습니다.
    public sealed class FriendCharacterMovement : MonoBehaviour // 인도 접근과 명령을 받은 뒤의 횡단에 기존 걷기·정지 애니메이션을 공통 적용합니다.
    { // 경로 설정과 이번 이동의 진행 상태를 보관합니다.
        private const float k_StrideLength = 0.4352f; // 새 캐릭터의 다리 비율 0.68에 맞춰 제작한 걷기 클립의 실제 전진 거리로 걸음 위상을 맞춥니다.
        private const float k_StopDistance = 0.13056f; // 기존 정지 클립의 마지막 발 디딤 거리에 맞춰 감속합니다.
        private const float k_SignalReactionDuration = 0.2f; // 기존 빨간불 명령의 발 디딤 반응 시간을 유지합니다.
        private const float k_ScenarioFirstStepDistance = 0.18f; // 횡단보도 입구의 인도 안에서 위험한 출발 의도를 드러낼 만큼만 앞으로 움직입니다.
        private static readonly int k_IdleState = Animator.StringToHash("Base Layer.Idle"); // 대기 상태를 문자열 검색 없이 선택합니다.
        private static readonly int k_WalkState = Animator.StringToHash("Base Layer.Walk"); // 기존 스킨 관절을 움직이는 걷기 상태를 선택합니다.
        private static readonly int k_StopState = Animator.StringToHash("Base Layer.Stop"); // 지지발을 유지하며 마지막 한 걸음을 마무리하는 상태를 선택합니다.
        private static readonly int k_WalkPhase = Animator.StringToHash("걸음 진행도"); // 실제 이동 거리에 맞춘 걷기 클립의 정규화 시간을 전달합니다.
        private static readonly int k_StopPhase = Animator.StringToHash("정지 진행도"); // 감속 이동과 정지 클립을 같은 진행도로 재생합니다.

        [Header("인도 위 이동 경로")] // 씬에 놓인 두 지점을 캐릭터 모델과 분리하여 설정합니다.
        [SerializeField, InspectorName("출발 지점")] // 대기할 위치와 경로의 시작점을 지정합니다.
        [Tooltip("캐릭터의 발바닥 높이에 빈 오브젝트를 놓아 주세요. 도착 지점과 같은 높이의 안전한 인도 위에 배치합니다.")] // 루트가 발바닥 기준이라는 점을 설명합니다.
        private Transform m_StartPoint; // 캐릭터의 자식이 아닌 씬의 고정 출발점을 참조합니다.

        [SerializeField, InspectorName("도착 지점 및 대기 방향")] // 도착 위치와 멈춘 뒤 바라볼 방향을 함께 지정합니다.
        [Tooltip("최소 0.5m 떨어진 같은 높이의 인도 위 지점입니다. 파란색 Z축을 횡단보도와 신호등 쪽으로 향하게 해 주세요. 경로 변경은 다음 활성화 시 적용됩니다.")] // 마지막 시선 방향과 안전한 경로 설정 방법을 안내합니다.
        private Transform m_EndPoint; // 자동차·보행 신호와 연결되지 않은 이동 목표를 보관합니다.

        [Header("명령으로 시작하는 횡단 경로")] // 자동 인도 접근과 구분하여 플레이어 명령으로만 사용하는 도착점을 지정합니다.
        [SerializeField, InspectorName("횡단보도 반대편 도착점")] // 현재 인도에서 출발한 친구가 닿을 건너편 위치를 지정합니다.
        [Tooltip("횡단보도 줄무늬를 따라 도로를 건넌 뒤 도착할 반대편 인도 위 지점입니다.")] // 실제 횡단 경로의 끝이 인도에 있도록 안내합니다.
        private Transform m_CrossingEndPoint; // 플레이어가 고개를 돌리거나 움직여도 횡단 경로가 흔들리지 않는 고정 도착점입니다.

        [SerializeField, InspectorName("횡단 바닥 환경")] // 기존 횡단보도 환경 계층에서 바닥 Collider를 읽습니다.
        [Tooltip("씬의 CrosswalkEnvironment를 연결합니다. 이 환경의 Collider만 사용해 도로·연석·인도의 실제 바닥 높이를 따르며 자동차와 플레이어는 바닥으로 취급하지 않습니다.")] // 별도의 충돌 판정이나 환경 수정 없이 발 높이를 맞추는 기준을 설명합니다.
        private Transform m_CrosswalkGroundRoot; // 횡단 중 지면 높이를 읽을 기존 환경의 루트입니다.

        [Header("걷기 설정")] // 걷기 속도와 출발 전 대기 시간을 조절합니다.
        [SerializeField, InspectorName("대기 후 자동 출발")] // 친구별로 기존 시간 기반 출발을 사용할지 지정합니다.
        [Tooltip("끄면 출발 지점에서 대기하며 기존 BeginWalk 호출이나 멈춤 명령을 받을 때까지 자동 이동하지 않습니다. 공간 배치용 초기 상태에서는 꺼 주세요.")] // 초기 배치를 유지하면서 기존 수동 걷기 진입점을 재사용하는 방법을 안내합니다.
        private bool m_AutoStart = true; // 기존 프리팹의 자동 보행은 보존하고 씬의 각 친구에서 별도로 끌 수 있게 합니다.

        [SerializeField, Min(0f), InspectorName("이동 속도(m/s)")] // 초당 전진할 목표 거리를 미터 단위로 입력합니다.
        [Tooltip("기본값은 0.8m/s입니다. 실제 이동 거리에 걸음 주기를 맞춥니다. 0에서는 현재 자세로 일시 대기하며, 지나치게 큰 값은 자연스러운 보행에 적합하지 않습니다.")] // 속도와 애니메이션의 관계 및 0의 의미를 설명합니다.
        private float m_Speed = 0.8f; // 어린이 학생의 여유 있는 시험용 걷기 속도입니다.

        [SerializeField, Min(0f), InspectorName("시작 대기 시간(초)")] // 활성화 후 자동 출발까지 기다릴 시간을 지정합니다.
        [Tooltip("기본값 10초 동안 대기한 뒤 한 번만 이동합니다. VR 착용과 조준에 시간이 더 필요하면 늘려 주세요. 대기 중 멈춤 신호를 받아도 자동 출발이 취소됩니다.")] // 시작 대기와 멈춤 명령의 관계를 안내합니다.
        private float m_StartDelay = 10f; // 컨트롤러를 준비하고 건너편 친구를 조준할 시간을 확보합니다.

        [SerializeField, Min(0.1f), InspectorName("출발 가속도(m/s²)")] // 걷기 시작 시 정지 상태에서 부드럽게 속도를 높입니다.
        [Tooltip("기본값 1.6에서 약 0.5초에 걸쳐 기본 보행 속도에 도달합니다. 마지막 감속은 정지 클립의 보폭에 맞춰 처리합니다.")] // 출발 가속과 마지막 발 디딤을 구분해 설명합니다.
        private float m_Acceleration = 1.6f; // 출발 순간의 루트 위치가 갑자기 빨라지는 것을 줄입니다.

        [SerializeField, Min(1f), InspectorName("도착 방향 회전 속도(도/초)")] // 마지막 걸음과 도착 대기 중 방향을 서서히 맞춥니다.
        [Tooltip("도착 지점의 Z축 방향까지 캐릭터 루트만 회전합니다. VR 카메라는 제어하지 않습니다.")] // 회전이 관찰자의 시점에 영향을 주지 않음을 설명합니다.
        private float m_TurnSpeed = 180f; // 현재 경로의 작은 방향 차이를 급격한 회전 없이 정리합니다.

        [Header("기존 골격 애니메이션")] // 시각 모델의 Animator를 이동 루트와 분리하여 연결합니다.
        [SerializeField, InspectorName("캐릭터 Animator")] // VisualRoot에 연결된 전용 상태 머신을 지정합니다.
        [Tooltip("FriendLocomotion 컨트롤러가 연결된 VisualRoot의 Animator입니다. 루트 모션은 끄고 캐릭터 루트 이동은 이 스크립트만 담당합니다.")] // 이동 중복과 잘못된 Animator 연결을 방지합니다.
        private Animator m_Animator; // 스킨 메시를 변형하는 실제 관절 애니메이션을 재생합니다.

        [SerializeField, Range(0.05f, 0.4f), InspectorName("자세 전환 시간(초)")] // 대기에서 걷기 및 정지 후 대기로 부드럽게 섞습니다.
        [Tooltip("기본값 0.22초입니다. 걷기에서 정지 클립으로는 동일한 발 디딤 자세에서 연결합니다.")] // 중간 정지 전환에 자세가 튀지 않는 이유를 설명합니다.
        private float m_FadeTime = 0.22f; // 팔과 몸통이 한 프레임에 다른 자세로 바뀌지 않게 합니다.

        [Header("멈춤 신호 반응")] // 기존 모델의 팔 관절을 이용하는 짧은 손들기 반응을 설정합니다.
        [SerializeField, InspectorName("오른쪽 위팔 관절")] // 모델에 이미 존재하는 RightUpperArm을 연결합니다.
        [Tooltip("VisualRoot 아래 기존 RightUpperArm입니다. 대기 애니메이션 위에 손들기 자세를 잠시 섞습니다.")] // 새 골격이나 휴머노이드 Avatar 없이 사용하는 관절임을 안내합니다.
        private Transform m_RightUpperArm; // 어깨에서 팔을 살짝 들어 올릴 기존 뼈대를 참조합니다.

        [SerializeField, InspectorName("오른쪽 아래팔 관절")] // 기존 RightLowerArm을 연결하여 팔꿈치를 구부립니다.
        [Tooltip("RightUpperArm 아래 기존 RightLowerArm입니다. 손을 들었다 내린 뒤 기존 대기 애니메이션으로 돌아갑니다.")] // 반응이 끝나면 Animator가 자세를 다시 전담함을 설명합니다.
        private Transform m_RightLowerArm; // 손과 손가락은 기존 아래팔 계층을 그대로 따라갑니다.

        private enum MovementState { Waiting, Walking, Stopping, Arrived, CommandStopped, TurningToCross, Crossed, RedSignalReaction, TurningToWalk } // 기존 횡단 상태를 보존하고 첫 번째 상황의 인도 우회 준비를 별도 상태로 구분합니다.
        private MovementState m_State; // 현재 단계만 갱신하여 도착 후 다시 걷지 않게 합니다.
        private Transform[] m_DetourWaypoints; // 전봇대 우회처럼 중간에 정지하지 않을 실제 경유점들을 보관합니다.
        private int m_DetourIndex; // 현재 보행 구간의 경유점 번호입니다.
        public float ScenarioTravelBudget => Mathf.Max(0f, m_PathLength - m_Distance) / Mathf.Max(0.3f, m_Speed) + 6f; // 남은 보행 거리와 가속·착지를 포함한 실제 완료 대기 상한입니다.
        public float ScenarioTurnSpeed => Mathf.Max(1f, PositiveOrZero(m_TurnSpeed)); // 발 디딤 연출이 실제 몸 회전 속도에 맞춰 대기 시간을 계산하도록 제공합니다.
        private Vector3 m_PathStart; // 활성화 시 확정한 출발 위치를 보관합니다.
        private Vector3 m_PathDirection; // 평평한 인도 위의 정규화된 이동 방향입니다.
        private Quaternion m_ArrivalRotation; // 도착 지점의 수평 방향을 보관합니다.
        private float m_PathLength; // 전체 경로 길이를 미터 단위로 보관합니다.
        private float m_Distance; // 루트가 실제로 전진한 거리를 누적합니다.
        private float m_CurrentSpeed; // 출발 가속을 적용한 현재 속도입니다.
        private float m_WaitRemaining; // 자동 출발까지 남은 게임 시간입니다.
        private float m_PhaseOffset; // 마지막 정지 클립과 같은 발 디딤 자세에서 걷기가 끝나도록 주기를 맞춥니다.
        private float m_StopProgress; // 정지 클립과 감속 곡선이 공유하는 0~1 진행도입니다.
        private float m_StopEntrySpeed; // 마지막 걸음에 진입한 속도로 연속적인 감속을 시작합니다.
        private float m_StopSpeedSetting; // 정지 중 Inspector 속도 변경을 비율로 반영하기 위한 진입 당시 설정입니다.
        private float m_ResponseElapsed = 1.6f; // 손들기 반응을 받은 뒤 흐른 시간이며 초기에는 반응을 재생하지 않습니다.
        private bool m_CrossingStarted; // 한 번 수락한 횡단 명령을 완료·중단·재활성화 뒤에도 다시 시작하지 못하게 유지합니다.
        private Collider[] m_CrossingGrounds; // 명령을 받을 때 기존 환경에서 가져온 바닥 Collider를 횡단 중 재사용합니다.
        private Transform m_RejectedSignalTarget; // 위험 명령에 대한 발 디딤 시도 후 몸을 돌려 바라볼 신호등 위치입니다.
        private bool m_ScenarioPaused; // 첫 번째 상황의 관찰과 충돌 연출 동안 이 친구의 경로 갱신만 중단합니다.
        private bool m_ScenarioFirstStepHold; // 빨간불과 파란불에서 첫 발이 보인 자세를 선택할 때까지 유지합니다.
        private Vector3 m_ScenarioFirstStepOrigin, m_CheckpointFirstStepOrigin; // 시작점을 보존해 첫 발의 전진량이 프레임마다 누적되지 않고 재시도에서도 같은 위치를 사용하게 합니다.
        private float m_ScenarioFirstStepTime = 0.14f; // 실제로 보여 줄 첫 발 반응의 최대 진행 시간입니다.
        private bool m_AnimatorWasEnabled; // 상황 정지 이전의 Animator 활성 상태를 재개 시 복원합니다.
        private Vector3 m_CheckpointPosition; // 재시도할 관찰 지점의 실제 루트 위치를 보관합니다.
        private Quaternion m_CheckpointRotation, m_CheckpointArrivalRotation; // 재시도 시 몸 방향과 기존 도착 목표 방향을 함께 복원합니다.
        private float m_CheckpointDistance, m_CheckpointSpeed, m_CheckpointWalkPhase, m_CheckpointAnimationTime; // 경로 거리와 걸음 위상을 함께 저장하여 복원 직후 발이 튀지 않게 합니다.
        private int m_CheckpointAnimationState; // 재시도할 때 기존 걷기 상태를 동일한 정규화 시간으로 선택합니다.
        private MovementState m_CheckpointState; // 관찰 전 보행 상태를 명령 정지나 충돌 상태와 구분하여 보관합니다.
        private Transform m_ScenarioApproachLimit, m_CheckpointApproachLimit; // 관찰 전에는 전봇대 앞의 안전 지점을 넘지 않고 재시도에도 같은 제한을 복원합니다.
        private Vector3 m_CheckpointPathStart, m_CheckpointPathDirection; // 안전 우회로가 기존 보행 경로를 바꾸어도 관찰 경로로 정확히 돌아가게 합니다.
        private float m_CheckpointPathLength, m_CheckpointPhaseOffset, m_CheckpointSpeedSetting; // 경로 길이·보폭 기준·설정 속도를 관찰 시점과 함께 저장합니다.
        private bool m_UnsafeScenarioCrossing, m_CheckpointCrossingStarted, m_CheckpointFirstStepHold; // 위험 선택에서만 횡단 제한을 적용하고 재시도할 때 횡단 시작 기록과 첫 발 보류를 복원합니다.
        private float m_ScenarioPlaybackRate = 1f, m_CheckpointWaitRemaining, m_CheckpointFirstStepTime; // 개별 느린 동작의 배율과 관찰 당시 첫 발 반응 시간을 보관합니다.
        private Transform m_CheckpointRejectedSignalTarget; // 재시도 후에도 원래 빨간 신호를 바라보는 반응을 재사용합니다.
        public bool IsScenarioDetouring => isActiveAndEnabled && (m_State == MovementState.TurningToWalk || IsWalking); // 우회 방향 준비와 실제 보행·감속이 모두 끝났는지 제공합니다.
        public void SetScenarioApproachLimit(Transform limit) { m_ScenarioApproachLimit = limit; } // 도입 연출만 관찰 지점에서 대기시키고 위험 선택에서는 제한을 명시적으로 해제합니다.
        public bool IsScenarioPaused => m_ScenarioPaused; // 다른 상황이 이 친구의 기존 정지 상태를 보존하고 복원하게 합니다.
        public float ScenarioLimitRemaining => m_ScenarioApproachLimit != null ? Mathf.Max(0f, Vector3.Dot(m_ScenarioApproachLimit.position - transform.position, m_PathDirection)) : float.PositiveInfinity; // 위험 횡단에서 실제 경로를 따라 남은 접근 거리만 제공합니다.
        public void SetScenarioPlaybackRate(float rate) { m_ScenarioPlaybackRate = Mathf.Clamp(rate, 0.08f, 1f); if (m_Animator != null) m_Animator.speed = m_ScenarioPlaybackRate; } // 이 친구의 걸음과 Animator만 느리게 재생하고 VR 입력과 전역 시간은 유지합니다.

        public bool TryBeginUnsafeScenarioCrossing(Transform limit) // 두 번째 상황의 명시적인 위험 선택에 한해서 기존 횡단 보행을 재사용합니다.
        { // 기존 빨간불 경고 반응과 일반 횡단 명령의 조건은 변경하지 않습니다.
            if (!isActiveAndEnabled || !m_ScenarioPaused || m_State != MovementState.RedSignalReaction || m_CrossingStarted || limit == null) return false; // 관찰 중 멈춘 첫 발에서만 위험 연출을 시작합니다.
            m_State = MovementState.CommandStopped; // 신호 판정을 거치지 않는 이번 연출이 기존 횡단 경로 초기화를 호출할 수 있게 합니다.
            if (!TryBeginCrossing()) { m_State = MovementState.RedSignalReaction; return false; } // 경로 연결이 없는 경우 원래 관찰 상태를 유지합니다.
            m_ScenarioFirstStepHold = false; m_UnsafeScenarioCrossing = true; m_ScenarioApproachLimit = limit; // 첫 발 자동 정지를 해제하되 차 앞의 연출 지점을 넘지 못하게 제한합니다.
            SetScenarioPaused(false); // 기존 보행·Animator를 함께 재개하고 이동 루트의 소유자는 그대로 유지합니다.
            return true; // 실제 횡단 보행이 수락된 경우에만 차량 결과 연출을 이어갑니다.
        } // 신호와 차량을 기다리는 기존 안전 횡단 기능은 이 경로에서 호출하지 않습니다.

        public bool TryBeginScenarioFirstStep(Transform signal, float elapsed) // 보행 신호 색과 무관하게 상황 연출용 첫 발 자세를 기존 반응으로 준비합니다.
        { // 실제 횡단 시작은 별도 선택과 차량 정지 판단 뒤에만 허용합니다.
            if (!TryReactToRedSignal(signal)) return false; // 기존 대기·경로 조건을 통과한 같은 친구만 첫 발을 준비합니다.
            m_ScenarioFirstStepOrigin = transform.position; // 실제 보행을 마친 인도 위치를 첫 발의 기준으로 잡습니다.
            m_ScenarioFirstStepTime = Mathf.Clamp(elapsed, 0.08f, 0.16f); // 기존 반응 시간 안에서 첫 발의 이동을 끝내도록 제한합니다.
            m_ScenarioFirstStepHold = true; // 발을 내민 자세에서 멈추고 플레이어의 결정을 기다립니다.
            return true; // 실제 움직임을 시작한 경우에만 다음 관찰 단계를 예약합니다.
        } // 기존 빨간불 명령 판정 함수의 동작은 변경하지 않습니다.

        public void StopScenarioFirstStep() // 접근하는 차를 기다리라는 말을 들으면 친구를 현재 인도 위치에서 멈춥니다.
        { // 보류한 첫 발을 일반 대기 자세로 부드럽게 연결합니다.
            if (m_State != MovementState.RedSignalReaction || !m_ScenarioFirstStepHold) return; // 도입에서 내민 첫 발만 멈추고 다른 보행에는 관여하지 않습니다.
            SetScenarioPaused(false); // 대기 애니메이션과 차를 확인하는 관절 연출이 다시 적용되게 합니다.
            m_ScenarioFirstStepHold = false; m_WaitRemaining = 0f; m_State = MovementState.CommandStopped; // 이전 출발 반응이 이어지거나 신호등을 다시 바라보지 않게 현재 상태를 정리합니다.
            m_Animator.CrossFadeInFixedTime(k_IdleState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 실제 전진은 멈추고 발을 내민 자세에서 대기로 전환합니다.
        } // 안전한 횡단은 차체가 모두 지나간 뒤 별도 명령으로 시작합니다.

        public void PlaceWaitingForScenario(Transform point) // 씬에 배치한 인도 기준점으로 대기 상태를 설정합니다.
        { // 기준점을 지정한 호출부도 위치와 방향을 직접 복원하는 것과 같은 경로를 사용합니다.
            if (point != null) PlaceWaitingForScenario(point.position, point.rotation); // 연결된 대기점에서만 경로와 자세를 갱신합니다.
        } // 위치가 없는 경우 현재 이동 상태를 유지합니다.

        public void PlaceWaitingForScenario(Vector3 position, Quaternion rotation) // 상황 취소 시 기록한 실제 위치와 방향으로 같은 친구를 복원합니다.
        { // 보행과 첫 발의 잔여 상태가 새 대기에 섞이지 않게 합니다.
            if (m_CrossingStarted) return; // 먼저 관찰 체크포인트를 복원해야 진행 중인 횡단을 되돌릴 수 있습니다.
            m_DetourWaypoints = null; // 이전 인도 보행이 끝난 뒤 중간 지점을 다시 따르지 않게 합니다.
            m_PathStart = position; m_PathDirection = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up).normalized; // 대기 위치와 다음 방향을 실제 인도 기준으로 설정합니다.
            m_PathLength = 0f; m_Distance = 0f; m_CurrentSpeed = 0f; m_StopProgress = 0f; m_WaitRemaining = 0f; // 직전 보행과 감속 수치를 초기화합니다.
            m_ScenarioApproachLimit = null; m_ResponseElapsed = 1.6f; m_RejectedSignalTarget = null; m_ScenarioFirstStepHold = false; // 첫 발이나 차량 앞 제한이 남지 않게 합니다.
            m_ArrivalRotation = rotation; m_State = MovementState.Arrived; // 같은 위치에서 다음 횡단 명령을 받을 수 있는 대기 상태로 둡니다.
            transform.SetPositionAndRotation(position, rotation); // 이동 루트와 실제 보이는 친구의 방향을 함께 맞춥니다.
            m_Animator.Play(k_IdleState, 0, 0f); // 뒷걸음이나 취소 뒤 기존 대기 클립을 이어서 사용합니다.
        } // 다른 친구와 XR Origin의 위치·상태는 변경하지 않습니다.

        public bool TryBeginScenarioDetour(Transform waypoint) // 단일 도착 보행도 기존과 같은 진입점을 사용합니다.
        { // 중간 정지가 필요한 별도의 이동 시스템을 만들지 않습니다.
            if (waypoint == null) return false; // 유효하지 않은 경로는 시작하지 않습니다.
            return TryBeginScenarioDetour(new[] { waypoint }); // 하나의 경유점도 같은 연속 보행 준비를 사용합니다.
        } // 경로 배열은 보행 시작 때 한 번만 준비합니다.

        public bool TryBeginScenarioDetour(Transform[] waypoints) // 여러 우회 지점을 하나의 보행으로 연결합니다.
        { // 경로 전체를 준비한 뒤에만 실제 상태를 변경합니다.
            if (!isActiveAndEnabled || m_CrossingStarted || waypoints == null || waypoints.Length == 0 || (m_State != MovementState.CommandStopped && m_State != MovementState.Arrived)) return false; // 현재 대기 중인 친구만 새 보행을 시작합니다.
            Vector3 previous = transform.position; // 각 구간의 높이와 길이를 실제 시작 위치부터 확인합니다.
            foreach (Transform waypoint in waypoints) // 잘못된 뒤쪽 경유점 때문에 이동 도중 멈추지 않게 합니다.
            { // 기존 인도 높이를 그대로 사용합니다.
                if (waypoint == null || Mathf.Abs(waypoint.position.y - previous.y) > 0.01f || (waypoint.position - previous).sqrMagnitude < 0.0025f) return false; // 없는 지점·단차·겹친 지점은 이동 전에 거절합니다.
                previous = waypoint.position; // 다음 구간은 현재 경유점에서 시작합니다.
            } // 유효한 전체 경로만 수락합니다.
            m_DetourWaypoints = waypoints; m_DetourIndex = 0; m_PhaseOffset = m_Animator.GetFloat(k_WalkPhase); // 기존 걸음 위상을 이어 받아 같은 발이 갑자기 반복되지 않게 합니다.
            PrepareDetourLeg(); m_CurrentSpeed = 0f; m_StopProgress = 0f; m_WaitRemaining = 0f; m_ResponseElapsed = 1.6f; // 처음 출발할 때만 가속을 준비합니다.
            m_Animator.SetFloat(k_WalkPhase, m_PhaseOffset); m_Animator.SetFloat(k_StopPhase, 0f); // 실제 이동 거리와 같은 보폭을 사용합니다.
            m_Animator.CrossFadeInFixedTime(k_WalkState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); m_State = MovementState.Walking; // 제자리 회전 완료를 기다리지 않고 걷는 중 몸을 돌립니다.
            return true; // 상황 제어기는 마지막 도착까지 같은 보행을 기다립니다.
        } // 경유점 사이에서는 속도·애니메이션을 초기화하지 않습니다.

        private void PrepareDetourLeg() // 다음 경유점으로 이동 기준만 바꿉니다.
        { // 중간 경유점에서 Idle이나 Stop을 재생하지 않습니다.
            m_PathStart = transform.position; Vector3 path = m_DetourWaypoints[m_DetourIndex].position - m_PathStart; path.y = 0f; // 현재 발 위치에서 다음 인도 구간을 구합니다.
            m_PathLength = path.magnitude; m_PathDirection = path / m_PathLength; m_Distance = 0f; m_ScenarioApproachLimit = null; // 다음 구간의 실제 길이와 진행 방향입니다.
            m_ArrivalRotation = Quaternion.LookRotation(m_PathDirection, Vector3.up); // 이동 중 회전할 목표 방향만 갱신합니다.
        } // 현재 속도와 걸음 위상은 호출자가 이어 갑니다.

        public void SetScenarioPaused(bool paused) // 전체 시간 배율 대신 이 인스턴스의 이동과 Animator만 정지하거나 재개합니다.
        { // 같은 정지 요청이 원래 Animator 상태를 덮어쓰지 않게 합니다.
            if (m_ScenarioPaused == paused) return; // 이미 적용한 상태는 중복 처리하지 않습니다.
            m_ScenarioPaused = paused; // 다음 Update와 손들기 처리부터 상황 정지를 반영합니다.
            if (m_Animator == null) return; // 씬 종료 중 Animator가 먼저 파괴되어도 남은 이동 정리를 계속할 수 있게 합니다.
            if (paused) m_AnimatorWasEnabled = m_Animator.enabled; // 정지 전에 활성 상태를 한 번만 보관합니다.
            m_Animator.enabled = paused ? false : m_AnimatorWasEnabled; // Animator 평가까지 멈춰 휴대전화가 섞인 실제 관절 자세를 유지합니다.
        } // XR 입력·신호등·차량·다른 친구의 시간은 변경하지 않습니다.

        public void SetScenarioApproachSpeed(float speed) // 동행 중 플레이어와의 간격을 기존 보폭 동기화 이동에 전달합니다.
        { // 첫 번째 인도 보행에만 속도 조절을 허용합니다.
            if (!m_CrossingStarted) m_Speed = Mathf.Max(0f, speed); // 횡단 기능의 속도나 상태에는 개입하지 않습니다.
        } // 실제 루트 전진과 애니메이션 위상 계산은 기존 이동 함수가 전담합니다.

        public void FaceScenarioTarget(Transform target) // 안전하게 멈춘 친구가 전봇대를 확인한 뒤 플레이어를 바라보게 합니다.
        { // 기존 정지 상태의 회전 처리에 목표만 전달합니다.
            if ((m_State != MovementState.CommandStopped && m_State != MovementState.Arrived && m_State != MovementState.Crossed) || target == null) return; // 경고 정지 또는 인도 우회를 마친 친구만 새 대상을 바라보게 합니다.
            Vector3 direction = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up); // 시선 대상의 머리 높이를 제외한 몸 방향을 계산합니다.
            if (direction.sqrMagnitude > 0.001f) m_ArrivalRotation = Quaternion.LookRotation(direction, Vector3.up); // 기존 각속도 기반 회전이 천천히 새 방향을 적용하게 합니다.
        } // 루트 위치나 XR 카메라는 강제로 바꾸지 않습니다.

        public void CaptureScenarioCheckpoint() // 관찰을 시작한 한 지점의 이동 진행도를 저장합니다.
        { // 새 경로나 새 Animator를 만들지 않고 기존 보행 상태를 보관합니다.
            m_CheckpointPosition = transform.position; // 경로상의 실제 위치를 저장합니다.
            m_CheckpointRotation = transform.rotation; // 휴대전화를 보던 몸 방향을 저장합니다.
            m_CheckpointArrivalRotation = m_ArrivalRotation; // 사고나 경고 이전의 회전 목표를 보관합니다.
            m_CheckpointDistance = m_Distance; // 재개할 때 출발점으로 되돌아가지 않도록 누적 거리를 저장합니다.
            m_CheckpointSpeed = m_CurrentSpeed; // 재개 직후 기존 보행 속도를 이어 갑니다.
            m_CheckpointWalkPhase = m_Animator.GetFloat(k_WalkPhase); // 실제 발 디딤에 대응하는 위상을 저장합니다.
            AnimatorStateInfo animation = m_Animator.GetCurrentAnimatorStateInfo(0); // 현재 기존 걷기 클립의 시간을 읽습니다.
            m_CheckpointAnimationState = animation.fullPathHash; // 동일한 상태를 재선택할 식별자를 저장합니다.
            m_CheckpointAnimationTime = animation.normalizedTime; // 전환 시점의 클립 시간을 저장합니다.
            m_CheckpointState = m_State; // 대기나 정지로 잘못 복원하지 않도록 현재 보행 상태를 저장합니다.
            m_CheckpointPathStart = m_PathStart; m_CheckpointPathDirection = m_PathDirection; m_CheckpointPathLength = m_PathLength; // 우회 전에 사용하던 직선 경로 전체를 보관합니다.
            m_CheckpointPhaseOffset = m_PhaseOffset; m_CheckpointSpeedSetting = m_Speed; m_CheckpointApproachLimit = m_ScenarioApproachLimit; // 경로별 보폭 기준과 관찰 지점 제한도 저장합니다.
            m_CheckpointCrossingStarted = m_CrossingStarted; m_CheckpointFirstStepHold = m_ScenarioFirstStepHold; // 위험 선택으로 생길 횡단 기록을 관찰 당시 상태와 분리합니다.
            m_CheckpointWaitRemaining = m_WaitRemaining; m_CheckpointFirstStepTime = m_ScenarioFirstStepTime; m_CheckpointRejectedSignalTarget = m_RejectedSignalTarget; // 관찰 당시 첫 발의 남은 시간과 신호 위치를 기록합니다.
            m_CheckpointFirstStepOrigin = m_ScenarioFirstStepOrigin; // 사고 뒤에도 관찰 때의 실제 한 걸음이 같은 인도 위치에 남게 시작점을 저장합니다.
        } // 휴대전화의 추가 자세와 뼈대 복원은 첫 번째 상황의 캐릭터 연출이 담당합니다.

        public void RestoreScenarioCheckpoint() // 사고 후 관찰 위치와 기존 걷기 진행도를 함께 되돌립니다.
        { // 활성화 초기화나 출발 지점 재설정을 호출하지 않습니다.
            m_DetourWaypoints = null; // 관찰 복원에서는 완료하지 못한 우회 경로를 폐기합니다.
            transform.SetPositionAndRotation(m_CheckpointPosition, m_CheckpointRotation); // 캐릭터만 저장된 위치로 복원하고 XR Origin은 그대로 둡니다.
            m_ArrivalRotation = m_CheckpointArrivalRotation; // 이전 경고 반응의 회전 목표를 제거합니다.
            m_Distance = m_CheckpointDistance; // 다음 전진이 관찰 지점에서 이어지게 합니다.
            m_CurrentSpeed = m_CheckpointSpeed; // 사고 직전 속도를 이동 재개에 사용합니다.
            m_State = m_CheckpointState; // 기존 보행 상태를 복원합니다.
            m_PathStart = m_CheckpointPathStart; m_PathDirection = m_CheckpointPathDirection; m_PathLength = m_CheckpointPathLength; // 우회 중 중단되어도 처음 관찰했던 보행 경로로 복원합니다.
            m_PhaseOffset = m_CheckpointPhaseOffset; m_Speed = m_CheckpointSpeedSetting; m_ScenarioApproachLimit = m_CheckpointApproachLimit; // 같은 속도 설정·보폭·안전 대기 지점으로 재시도합니다.
            m_CrossingStarted = m_CheckpointCrossingStarted; m_ScenarioFirstStepHold = m_CheckpointFirstStepHold; m_UnsafeScenarioCrossing = false; // 위험 횡단 기록을 없애고 원래 첫 발의 인도 대기 상태로 되돌립니다.
            m_WaitRemaining = m_CheckpointWaitRemaining; m_ScenarioFirstStepTime = m_CheckpointFirstStepTime; m_RejectedSignalTarget = m_CheckpointRejectedSignalTarget; SetScenarioPlaybackRate(1f); // 첫 발의 남은 반응 시간과 정상 재생 속도를 복원합니다.
            m_ScenarioFirstStepOrigin = m_CheckpointFirstStepOrigin; // 복원한 첫 발 자세의 전진 기준도 같은 위치로 맞춥니다.
            m_ResponseElapsed = 1.6f; // 이전 명령의 손들기 반응이 재시도에 남지 않게 합니다.
            m_Animator.SetFloat(k_WalkPhase, m_CheckpointWalkPhase); // 보폭과 자세의 기준을 함께 복원합니다.
            m_Animator.Play(m_CheckpointAnimationState, 0, m_CheckpointAnimationTime); // 정지된 Animator에도 재개할 상태와 시간을 예약합니다.
        } // 관찰 중에는 상황 정지를 유지하여 복원 직후 자동으로 걷지 않습니다.

        public bool CanReceiveStopCommand => isActiveAndEnabled && m_Animator != null && m_State != MovementState.CommandStopped && m_State != MovementState.Crossed && m_State != MovementState.RedSignalReaction; // 이동 중 멈춤은 유지하되 명령 정지·횡단 완료·빨간불 반응에는 같은 동작을 중복 적용하지 않습니다.
        public bool CanReceiveCrossCommand => isActiveAndEnabled && !m_CrossingStarted && m_CrossingEndPoint != null && m_CrosswalkGroundRoot != null && (m_State == MovementState.Arrived || m_State == MovementState.CommandStopped); // 건너편 인도에서 멈춰 있고 경로가 연결된 친구에게만 최초 횡단 명령을 허용합니다.
        public bool IsWalking => isActiveAndEnabled && (m_State == MovementState.Walking || m_State == MovementState.Stopping); // 접근과 횡단 모두에서 기존 보행 및 마지막 감속 걸음의 진행 여부를 제공합니다.
        public bool IsApproaching => IsWalking && !m_CrossingStarted; // 휴대전화 연출은 인도 접근에만 적용하여 횡단 중에는 팔과 고개를 내린 자세를 유지합니다.
        public bool IsWaitingForWalk => isActiveAndEnabled && m_State == MovementState.Waiting; // 휴대전화 연출이 출발 전 대기와 명령 정지를 구분하여 각 친구의 초기 자세만 설정하게 합니다.
        public bool HasStartedCrossing => m_CrossingStarted; // 신호 판정 담당자가 실제 횡단 시작 여부를 읽어 차량 진입 보호를 유지하게 합니다.
        public bool HasCompletedCrossing => m_State == MovementState.Crossed; // 가까운 인도에 도착한 뒤 차량 진입 보호를 해제할 수 있게 완료 상태를 제공합니다.

        private void OnEnable() // 씬 시작 또는 명시적으로 다시 활성화할 때 설정된 출발 지점에서 준비합니다.
        { // 실제 이동에 필요한 씬 참조와 평면 경로를 먼저 확보합니다.
            if (m_State == MovementState.CommandStopped || m_CrossingStarted) // 명령 정지나 횡단 이후에는 재활성화되어도 인도 접근을 다시 초기화하지 않습니다.
            { // 출발점으로 되돌리거나 시작 대기 타이머를 다시 만들지 않습니다.
                if (m_CrossingStarted && m_State != MovementState.Crossed) m_State = MovementState.CommandStopped; // 비활성화로 중단된 횡단은 현재 자리에서 정지하며 자동 재개하지 않습니다.
                m_CurrentSpeed = 0f; // 재활성화 시 이전 접근·횡단 속도를 남기지 않습니다.
                m_Animator.Play(k_IdleState, 0, 0f); // 재활성화 시에도 기존 대기 애니메이션을 이어 갑니다.
                return; // 아래의 자동 걷기 초기화를 건너뛰어 현재 위치를 보존합니다.
            } // 아직 명령을 받지 않은 캐릭터만 기존 이동 경로를 초기화합니다.
            if (m_StartPoint == null || m_EndPoint == null || m_Animator == null || m_Animator.runtimeAnimatorController == null) // 필수 경로와 전용 Animator 연결이 누락되었는지 확인합니다.
            { // 연결이 없는 프리팹이 임의의 월드 원점으로 이동하지 않게 합니다.
                Debug.LogError("친구 캐릭터에 출발 지점, 도착 지점과 FriendLocomotion Animator를 연결해 주세요.", this); // 수정할 컴포넌트를 Console에서 선택할 수 있게 합니다.
                enabled = false; // 불완전한 초기화 상태에서 프레임 갱신을 막습니다.
                return; // 경로 참조에 접근하기 전에 초기화를 마칩니다.
            } // 필요한 참조가 모두 있는 경우에만 경로를 계산합니다.
            m_PathStart = m_StartPoint.position; // 캐릭터 모델의 원래 발바닥 높이를 유지합니다.
            Vector3 path = m_EndPoint.position - m_PathStart; // 출발점에서 도착점으로 향하는 월드 공간 경로입니다.
            if (!IsFinite(path.x) || !IsFinite(path.y) || !IsFinite(path.z) || Mathf.Abs(path.y) > 0.01f || path.sqrMagnitude < 0.25f) // 평지용 보행 클립에 맞지 않는 높이 차이와 너무 짧은 경로를 거릅니다.
            { // 유효하지 않은 경로에서 회전이나 보폭 계산이 시작되지 않게 합니다.
                Debug.LogError("친구 이동 경로는 같은 높이의 인도 위에서 최소 0.5m 길이로 지정해 주세요.", this); // 현재 걷기 클립의 배치 조건을 안내합니다.
                enabled = false; // 잘못된 경로로 캐릭터를 움직이지 않습니다.
                return; // 사용자가 경로를 수정한 뒤 다시 활성화하도록 마칩니다.
            } // 경로가 유효하면 이번 한 번의 이동 상태를 초기화합니다.
            path.y = 0f; // 인도 표면에서 위아래로 이동하지 않도록 수평 경로만 사용합니다.
            m_PathLength = path.magnitude; // 걷기와 마지막 정지 구간을 나눌 길이입니다.
            m_PathDirection = path / m_PathLength; // 경로를 매번 정규화하지 않고 재사용합니다.
            Vector3 arrivalForward = Vector3.ProjectOnPlane(m_EndPoint.forward, Vector3.up); // 도착 방향에서 위아래 기울기를 제외합니다.
            m_ArrivalRotation = Quaternion.LookRotation(arrivalForward.sqrMagnitude > 0.001f ? arrivalForward : m_PathDirection, Vector3.up); // 목표 방향이 수직이면 경로 방향을 유지합니다.
            m_Distance = 0f; // 이전 이동의 누적 거리를 남기지 않습니다.
            m_CurrentSpeed = 0f; // 대기 상태에서 출발 가속을 시작합니다.
            m_StopProgress = 0f; // 마지막 한 걸음은 아직 시작하지 않았습니다.
            m_WaitRemaining = PositiveOrZero(m_StartDelay); // 음수·비정상 대기 시간은 즉시 출발로 취급합니다.
            m_PhaseOffset = Mathf.Repeat(-(m_PathLength - k_StopDistance) / k_StrideLength, 1f); // 정지 구간 진입 시 걷기 위상이 정확히 0이 되도록 설정합니다.
            m_State = MovementState.Waiting; // 자동 시작은 이 대기 상태에서 한 번만 발생합니다.
            transform.SetPositionAndRotation(m_PathStart, Quaternion.LookRotation(m_PathDirection, Vector3.up)); // 캐릭터만 출발점에 놓고 실제 이동 방향을 바라보게 합니다.
            m_Animator.applyRootMotion = false; // 클립과 스크립트가 이동 거리를 중복 적용하지 않게 합니다.
            m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // VR에서 시선을 돌렸다가 돌아와도 걸음 진행이 유지되게 합니다.
            m_Animator.SetFloat(k_WalkPhase, m_PhaseOffset); // 최초 걷기 자세의 위상을 준비합니다.
            m_Animator.SetFloat(k_StopPhase, 0f); // 정지 클립의 첫 자세를 준비합니다.
            m_Animator.Play(k_IdleState, 0, 0f); // 출발 대기 동안 호흡이 있는 대기 클립을 재생합니다.
        } // 위치 이동과 시각적 애니메이션을 같은 출발 상태로 준비합니다.

        public void BeginWalk() // 다른 기능에서도 현재 대기를 끝내고 동일한 경로 이동을 요청할 수 있는 진입점입니다.
        { // 반복 호출이 도착한 캐릭터를 다시 이동시키지 않게 합니다.
            if (!isActiveAndEnabled || m_State != MovementState.Waiting) return; // 활성 상태의 출발 대기 중에만 한 번 이동을 시작합니다.
            m_State = MovementState.Walking; // 이번 경로의 걷기 단계로 전환합니다.
            m_Animator.CrossFadeInFixedTime(k_WalkState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 대기에서 관절 걷기로 부드럽게 전환합니다.
        } // 신호등·차량·플레이어 입력은 이 메서드에서 참조하지 않습니다.

        public bool TryStop(Transform commandSource) // 입력 장치와 무관하게 멈춤 명령을 받아 캐릭터의 상태와 반응만 변경합니다.
        { // 상호작용 쪽에서 성공 여부에 따라 확인 문구를 표시할 수 있도록 결과를 반환합니다.
            if (!CanReceiveStopCommand || commandSource == null) return false; // 중복 명령이나 반응할 상대가 없는 요청은 기존 상태를 바꾸지 않습니다.
            m_State = MovementState.CommandStopped; // 다른 호출이나 다음 Update가 걷기·도착 감속을 다시 실행하지 못하게 먼저 상태를 잠급니다.
            m_CurrentSpeed = 0f; // 현재 속도를 즉시 제거하며 루트 위치는 명령을 받은 자리에 유지합니다.
            m_WaitRemaining = 0f; // 출발 대기 중 받은 신호도 이후 자동 출발로 이어지지 않게 합니다.
            Vector3 facing = Vector3.ProjectOnPlane(commandSource.position - transform.position, Vector3.up); // 플레이어의 머리 높이를 제외한 수평 방향을 계산합니다.
            if (facing.sqrMagnitude > 0.001f) m_ArrivalRotation = Quaternion.LookRotation(facing, Vector3.up); // 기존 회전 처리에 플레이어 쪽 목표 방향을 전달합니다.
            m_ResponseElapsed = 0f; // 이번에 성공한 명령에 대해서만 한 번 손들기 반응을 시작합니다.
            m_Animator.CrossFadeInFixedTime(k_IdleState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 전진하는 정지 클립을 거치지 않고 기존 대기 자세로 전환합니다.
            return true; // 상호작용 담당 스크립트에 실제 명령 수락을 알립니다.
        } // XR 입력과 안내 표시는 이동 컴포넌트에 포함하지 않습니다.

        public bool TryBeginCrossing() // 기존 선택 입력이 전달한 횡단 명령을 한 번만 수락합니다.
        { // 현재 정지 위치에서 시작하여 카메라가 아닌 씬의 고정 도착점으로 걷습니다.
            if (!CanReceiveCrossCommand) return false; // 접근 중이거나 이미 횡단을 시작·완료한 친구는 경로를 다시 초기화하지 않습니다.
            Vector3 path = m_CrossingEndPoint.position - transform.position; // 건너편 인도의 현재 대기 위치와 플레이어 쪽 인도 끝을 연결합니다.
            if (!IsFinite(path.x) || !IsFinite(path.y) || !IsFinite(path.z)) return false; // 방향과 보행 거리를 계산할 수 없는 경로는 시작하지 않습니다.
            path.y = 0f; // 보폭과 걸음 진행도는 횡단보도 위 수평 이동 거리로 맞춥니다.
            if (path.sqrMagnitude < 0.25f) return false; // 기존 마지막 정지 걸음을 포함할 수 없는 짧은 경로는 거릅니다.
            m_CrossingGrounds = m_CrosswalkGroundRoot.GetComponentsInChildren<Collider>(); // 자동차·친구·XR Origin을 제외한 기존 환경의 지면만 확보합니다.
            if (m_CrossingGrounds.Length == 0) return false; // 바닥 높이를 읽을 수 없는 경로로 횡단을 시작하지 않습니다.
            m_CrossingStarted = true; // 다음 입력이 경로와 위치를 다시 초기화하지 못하게 명령 수락을 먼저 기록합니다.
            m_PathStart = transform.position; // 접근 출발점으로 되돌리지 않고 현재 멈춘 자리를 횡단 출발점으로 삼습니다.
            m_PathLength = path.magnitude; // 현재 대기 위치에서 가까운 인도까지의 수평 길이입니다.
            m_PathDirection = path / m_PathLength; // 흰색 줄무늬의 중앙을 지나는 고정 직선 방향을 보관합니다.
            m_ArrivalRotation = Quaternion.LookRotation(m_PathDirection, Vector3.up); // 횡단 준비와 완료 시 모두 플레이어 쪽 인도 방향을 바라봅니다.
            m_Distance = 0f; // 접근에서 누적한 거리를 횡단 경로에 섞지 않습니다.
            m_CurrentSpeed = 0f; // 기존 출발 가속을 사용해 정지 상태에서 자연스럽게 출발합니다.
            m_StopProgress = 0f; // 가까운 인도에 도착할 때 마지막 감속 걸음을 처음부터 적용합니다.
            m_PhaseOffset = Mathf.Repeat(-(m_PathLength - k_StopDistance) / k_StrideLength, 1f); // 횡단 거리에도 기존 보폭과 마지막 정지 자세를 일치시킵니다.
            m_WaitRemaining = 0f; // 횡단 출발에서도 불필요한 제자리 대기를 두지 않습니다. // 현재 휴대전화의 0.5초 자세 해제 동안 제자리에서 횡단 방향을 준비합니다.
            m_ResponseElapsed = 1.6f; // 이전 멈춤 응답의 손들기를 횡단 동작에 남기지 않습니다.
            m_Animator.SetFloat(k_WalkPhase, m_PhaseOffset); // 새 경로에 맞는 걷기 위상을 준비합니다.
            m_Animator.SetFloat(k_StopPhase, 0f); // 이전 접근 감속의 진행도를 제거합니다.
            m_Animator.CrossFadeInFixedTime(k_WalkState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 횡단 방향으로 회전하며 기존 보행을 시작합니다.
            m_State = MovementState.Walking; // 방향 정렬을 완료할 때까지 제자리에서 기다리지 않습니다. // 명령을 받은 경우에만 도달하는 횡단 준비 상태로 전환합니다.
            return true; // 상호작용 담당자에게 최초 횡단 명령이 수락되었음을 알립니다.
        } // 신호등과 자동차에 따른 허용·충돌·교육 판정은 포함하지 않습니다.

        public bool TryReactToRedSignal(Transform signalTarget) // 신호 판정 담당자가 거절한 횡단 명령에 필요한 캐릭터 동작만 수행합니다.
        { // 실제 횡단 시작 기록과 인도 위 루트 위치는 바꾸지 않습니다.
            if (!CanReceiveCrossCommand || signalTarget == null) return false; // 대기 중인 친구에게 전달된 최초 반응 요청만 수락합니다.
            m_RejectedSignalTarget = signalTarget; // 짧은 출발 시도 뒤 바라볼 신호등을 보관합니다.
            Vector3 crossingDirection = Vector3.ProjectOnPlane(m_CrossingEndPoint.position - transform.position, Vector3.up); // 기존 횡단 경로 방향으로 발을 내딛으려는 몸 방향을 구합니다.
            if (crossingDirection.sqrMagnitude > 0.001f) m_ArrivalRotation = Quaternion.LookRotation(crossingDirection, Vector3.up); // 도로에 진입하지 않고 제자리에서 횡단 방향을 향합니다.
            m_CurrentSpeed = 0f; // 출발 시도 중 루트의 전진 속도를 만들지 않아 반복 명령으로 인도 끝에서 밀려나지 않게 합니다.
            m_WaitRemaining = k_SignalReactionDuration; // 기존 명령 반응의 전체 시간을 사용해 걷기에서 대기로 돌아갑니다.
            m_ResponseElapsed = 1.6f; // 이전 손들기 반응을 출발 시도에 겹치지 않습니다.
            m_Animator.SetFloat(k_WalkPhase, 0f); // 기존 걷기 클립의 첫 발 디딤 자세부터 사용합니다.
            m_Animator.CrossFadeInFixedTime(k_WalkState, 0.08f, 0, 0f); // 기존 걷기를 짧게 보여 횡단하려다가 멈추는 의도를 표현합니다.
            m_State = MovementState.RedSignalReaction; // 이동 거리 계산을 실행하지 않는 반응 상태로 전환합니다.
            return true; // 판단 담당자가 빨간불 안내와 시선 반응을 시작할 수 있게 합니다.
        } // 신호 색의 판정과 위험 명령 이력은 이 이동 컴포넌트에서 관리하지 않습니다.

        private void OnDisable() // 캐릭터가 꺼지거나 이동 컴포넌트가 비활성화될 때 반응의 남은 시간을 정리합니다.
        { // 명령 정지 상태 자체는 보존하여 다시 켜져도 자동으로 걷지 않습니다.
            m_ResponseElapsed = 1.6f; // 재활성화 시 이전 손들기 반응이 중간부터 다시 나타나지 않게 합니다.
            if (m_State == MovementState.RedSignalReaction) // 짧은 반응 도중 꺼져도 접근 출발점으로 되돌아가지 않게 합니다.
            { // 현재 인도 위 위치에서 반응을 종료합니다.
                m_State = MovementState.CommandStopped; // 재활성화 후에도 정지 상태를 유지하고 다음 명령을 기다립니다.
                if (m_Animator != null && m_Animator.gameObject.activeInHierarchy) m_Animator.Play(k_IdleState, 0, 0f); // 이동 컴포넌트만 꺼지면 즉시 대기 자세를 적용하고, 캐릭터 계층이 꺼지면 기존 OnEnable에서 적용합니다.
            } // 위험 반응이 아닌 이동에는 기존 비활성화 동작을 유지합니다.
        } // 기존 Animator가 다시 평가되면 팔도 정상 대기 자세를 사용합니다.

        private void LateUpdate() // Animator가 기존 대기 자세를 적용한 뒤 같은 팔 뼈대에 짧은 손들기 자세를 섞습니다.
        { // Idle 클립이 매 프레임 두 관절을 기록하므로 이전 프레임의 손들기 회전이 누적되지 않습니다.
            if (m_ScenarioPaused) return; // 관찰이나 사고 연출이 소유한 관절 자세를 손들기 반응이 덮어쓰지 않게 합니다.
            if (m_State != MovementState.CommandStopped || m_ResponseElapsed >= 1.6f || m_RightUpperArm == null || m_RightLowerArm == null) return; // 명령에 반응하는 동안 연결된 기존 팔만 조절합니다.
            m_ResponseElapsed = Mathf.Min(1.6f, m_ResponseElapsed + Time.deltaTime * m_ScenarioPlaybackRate); // 손들기 반응도 이 친구의 재생 배율에 맞춰 한 번만 진행합니다.
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(m_ResponseElapsed / 0.3f, (1.6f - m_ResponseElapsed) / 0.4f)); // 처음 0.3초에 들고 마지막 0.4초에 내려 부드럽게 응답합니다.
            m_RightUpperArm.localRotation = Quaternion.Slerp(m_RightUpperArm.localRotation, Quaternion.Euler(-15f, 0f, -20f), blend); // 기존 위팔을 몸 옆에서 조금 들어 플레이어가 손을 볼 수 있게 합니다.
            m_RightLowerArm.localRotation = Quaternion.Slerp(m_RightLowerArm.localRotation, Quaternion.Euler(0f, -10f, 105f), blend); // 기존 팔꿈치를 접어 손을 올리고 반응 후 원래 자세로 되돌립니다.
        } // 모델 계층과 스킨·Animator 컨트롤러는 교체하지 않습니다.

        private void Update() // Animator가 이번 프레임의 자세를 평가하기 전에 루트 위치와 진행도를 전달합니다.
        { // 게임 시간에 맞춰 대기·걷기·정지 중 현재 단계만 갱신합니다.
            if (m_ScenarioPaused) return; // 이 친구의 시간만 멈추고 프로젝트의 전역 시간과 입력은 유지합니다.
            float deltaTime = Time.deltaTime * m_ScenarioPlaybackRate; // 이 친구의 경로와 걸음 진행에만 개별 느린 동작을 반영합니다.
            if (deltaTime <= 0f) return; // 게임 일시정지 중에는 대기와 이동을 진행하지 않습니다.
            if (m_State == MovementState.RedSignalReaction) // 기존 첫 발 클립을 도로에 진입하지 않는 짧은 시도로 사용합니다.
            { // 걷기 자세와 몸 방향이 같은 횡단 방향을 가리키게 합니다.
                m_WaitRemaining = Mathf.Max(m_ScenarioFirstStepHold ? k_SignalReactionDuration - m_ScenarioFirstStepTime : 0f, m_WaitRemaining - deltaTime); // 상황 도입은 첫 발의 끝에서 보류하고 일반 명령 반응은 기존 시간까지 진행합니다.
                if (m_ScenarioFirstStepHold) // 빨간불과 파란불의 상황 연출에서만 인도 위로 짧게 한 발을 내밉니다.
                { // 기존 걸음 위상과 실제 전진 거리가 어긋나지 않게 같이 갱신합니다.
                    float progress = Mathf.Clamp01((k_SignalReactionDuration - m_WaitRemaining) / m_ScenarioFirstStepTime); // 긴 프레임에서도 한 걸음의 범위를 넘지 않는 진행도를 구합니다.
                    float distance = k_ScenarioFirstStepDistance * Mathf.SmoothStep(0f, 1f, progress); // 한 발을 내딛고 끝에서 멈추도록 이동을 완만하게 만듭니다.
                    transform.position = m_ScenarioFirstStepOrigin + m_ArrivalRotation * Vector3.forward * distance; // 현재 횡단 방향으로 친구의 루트만 인도 위에서 움직입니다.
                    m_Animator.SetFloat(k_WalkPhase, distance / k_StrideLength); // 루트가 실제 움직인 거리에 맞춰 기존 걷기 클립의 발 디딤을 표시합니다.
                } // 선택 대기에서는 실제 위치와 자세가 한 프레임의 상태로 함께 저장됩니다.
                else m_Animator.SetFloat(k_WalkPhase, (k_SignalReactionDuration - m_WaitRemaining) * 0.6f); // 일반 빨간불 거절 반응은 기존의 제자리 발 디딤을 사용합니다.
                float turnSpeed = m_ScenarioFirstStepHold ? Mathf.Max(720f, ScenarioTurnSpeed) : ScenarioTurnSpeed; // 짧은 관찰 첫 발에서도 인도 진행 방향에서 횡단 방향으로 충분히 돌아봅니다.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, m_ArrivalRotation, turnSpeed * deltaTime); // 캐릭터 몸만 회전해 플레이어 시점을 유지합니다.
                if (m_ScenarioFirstStepHold || m_WaitRemaining > 0f) return; // 관찰 정지나 실제 첫 발이 끝나기 전에는 대기 상태로 바꾸지 않습니다.
                m_State = MovementState.CommandStopped; // 안전 선택으로 첫 발이 풀리면 같은 인도에서 기다립니다.
                if (m_RejectedSignalTarget != null) FaceScenarioTarget(m_RejectedSignalTarget); // 대기 자세에서 실제 보행 신호를 바라봅니다.
                m_ResponseElapsed = 0f; // 기존 제지 반응의 시작 시각을 기록합니다.
                m_Animator.CrossFadeInFixedTime(k_IdleState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 첫 발에서 대기 자세로 이어집니다.
                return; // 이 프레임에는 인도나 횡단 경로로 전진하지 않습니다.
            } // 정상 횡단은 기존 준비·보행·감속 상태를 그대로 사용합니다.
            if (m_State == MovementState.TurningToCross || m_State == MovementState.TurningToWalk) // 횡단과 첫 번째 상황의 인도 우회 모두 기존 제자리 방향 전환을 사용합니다.
            { // 회전이 끝나기 전에는 걷기 애니메이션과 루트 전진을 시작하지 않습니다.
                m_WaitRemaining = Mathf.Max(0f, m_WaitRemaining - deltaTime); // 기존 대기 타이머를 횡단 준비 시간으로 재사용합니다.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, m_ArrivalRotation, Mathf.Max(1f, PositiveOrZero(m_TurnSpeed)) * deltaTime); // 카메라 대신 친구 몸만 횡단 방향으로 자연스럽게 돌립니다.
                if (m_WaitRemaining > 0f || Quaternion.Angle(transform.rotation, m_ArrivalRotation) > 0.5f) return; // 팔을 내릴 시간과 몸의 방향 전환이 모두 끝날 때까지 위치를 유지합니다.
                transform.rotation = m_ArrivalRotation; // 남은 0.5도 이하의 차이를 정리하여 실제 이동 방향과 정면을 맞춥니다.
                m_State = MovementState.Walking; // 횡단과 안전 우회 모두 기존 보폭 동기화·가속 이동으로 이어 갑니다.
                m_Animator.CrossFadeInFixedTime(k_WalkState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 휴대전화 자세를 적용하지 않는 기존 걷기 애니메이션으로 전환합니다.
                return; // 준비에 사용한 프레임 시간을 중복해 전진시키지 않습니다.
            } // 명령이 없으면 이 상태로 자동 전환하지 않습니다.
            if (m_State == MovementState.Waiting) // 아직 자동 출발 시간이 지나지 않은 경우입니다.
            { // 대기 종료 프레임의 남는 시간도 실제 이동에 반영합니다.
                if (!m_AutoStart) return; // 자동 출발을 끈 친구는 초기 위치를 유지하며 기존 BeginWalk 요청만 기다립니다.
                float waiting = Mathf.Min(m_WaitRemaining, deltaTime); // 이번 프레임에서 대기에 사용한 시간입니다.
                m_WaitRemaining -= waiting; // 남은 대기 시간을 줄입니다.
                deltaTime -= waiting; // 대기 후 남은 시간만 걷기에 사용합니다.
                if (m_WaitRemaining > 0f) return; // Inspector에 지정한 시작 대기 시간이 끝나기 전에는 위치와 방향을 유지합니다.
                BeginWalk(); // 대기를 마쳤으므로 한 번의 이동을 시작합니다.
            } // 이어지는 코드는 남은 프레임 시간만 사용합니다.
            if (m_State == MovementState.Walking) // 방향을 맞추는 동안에도 실제 경로를 따라 보행합니다.
            { // 프레임 시간을 경로 구간과 감속에 순서대로 배분합니다.
                float turnSpeed = m_DetourWaypoints != null || m_CrossingStarted ? Mathf.Max(360f, ScenarioTurnSpeed) : ScenarioTurnSpeed; // 꺾이는 동행 경로와 횡단에서 느린 몸 돌리기를 줄입니다.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, m_ArrivalRotation, turnSpeed * deltaTime); // 애니메이션이 끝나기를 기다리지 않고 이동 중에 몸을 돌립니다.
                for (int leg = 0; leg < 8 && deltaTime > 0.00001f && m_State == MovementState.Walking; ++leg) deltaTime = AdvanceWalk(deltaTime); // 중간 지점을 통과하고 남은 시간은 다음 인도 구간이나 감속에 사용합니다.
            } // 감속에 들어가면 보행에 쓴 시간은 중복 적용하지 않습니다.
            if (m_State == MovementState.Stopping) AdvanceStop(deltaTime); // 실제 이동 거리가 도착점에 닿았을 때 상태를 완료로 바꿉니다.
            else if (m_State == MovementState.Arrived || m_State == MovementState.CommandStopped || m_State == MovementState.Crossed) // 정지 상태에서 필요한 마지막 방향만 맞춥니다.
            { // 회전 대상은 캐릭터 루트이며 VR 카메라는 접근하지 않습니다.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, m_ArrivalRotation, Mathf.Max(1f, PositiveOrZero(m_TurnSpeed)) * deltaTime); // 갑자기 최종 방향으로 뛰지 않도록 각속도를 제한합니다.
            } // 도착 상태에서는 위치를 더 이동하지 않습니다.
        } // 한 프레임의 위치와 Animator 진행도 갱신을 마칩니다.

        private float AdvanceWalk(float deltaTime) // 출발 가속과 일정한 속도의 인도 위 걷기를 계산합니다.
        { // 마지막 한 걸음을 남겨 두고 걷기 루프를 끝냅니다.
            float targetSpeed = PositiveOrZero(m_Speed); // Inspector 속도가 비정상적이면 전진을 멈춥니다.
            if (targetSpeed == 0f) { m_CurrentSpeed = 0f; return 0f; } // 진행도를 함께 정지시켜 발만 제자리에서 걷지 않게 합니다.
            float acceleration = Mathf.Max(0.1f, PositiveOrZero(m_Acceleration)); // 출발 가속 계산에서 0으로 나누지 않도록 합니다.
            float previousSpeed = m_CurrentSpeed; // 가속 구간 이동 거리를 계산할 시작 속도입니다.
            float rampTime = Mathf.Min(deltaTime, Mathf.Abs(targetSpeed - previousSpeed) / acceleration); // 목표 속도까지 가감속한 실제 시간을 구합니다.
            m_CurrentSpeed = Mathf.MoveTowards(previousSpeed, targetSpeed, acceleration * deltaTime); // 이번 프레임 끝의 속도를 연속적으로 갱신합니다.
            float step = (previousSpeed + m_CurrentSpeed) * 0.5f * rampTime + targetSpeed * (deltaTime - rampTime); // 가감속 구간과 정속 구간을 따로 적분하여 프레임 길이의 영향을 줄입니다.
            if (m_ScenarioApproachLimit != null && (!m_CrossingStarted || m_UnsafeScenarioCrossing)) // 일반 인도 접근과 명시적인 위험 횡단에서만 지정 지점을 넘지 않게 제한합니다.
            { // 기존 직선 경로 위 관찰 지점에서 보행 위상을 그대로 유지합니다.
                float limitDistance = Mathf.Clamp(Vector3.Dot(m_ScenarioApproachLimit.position - m_PathStart, m_PathDirection), 0f, m_PathLength - k_StopDistance); // 씬의 안전 대기 지점을 같은 경로 거리로 환산합니다.
                if (m_Distance + step >= limitDistance) // 이번 프레임에 안전 지점에 도달하거나 넘어가려는 경우입니다.
                { // 초과 거리를 적용하기 전에 위치와 걸음 진행도를 제한합니다.
                    m_Distance = limitDistance; m_CurrentSpeed = 0f; ApplyPathPosition(); // 정확한 관찰 위치에서 앞으로 더 걷지 않게 합니다.
                    m_Animator.SetFloat(k_WalkPhase, Mathf.Repeat(m_PhaseOffset + m_Distance / k_StrideLength, 1f)); // 휴대전화를 보며 걷던 발 자세를 유지합니다.
                    return 0f; // 선택이 확정되어 제한을 해제할 때까지 도입 경로를 더 진행하지 않습니다.
                } // 안전 지점 이전에는 기존 가속과 발 디딤을 그대로 적용합니다.
            } // 횡단과 명시적으로 재개한 위험 보행은 기존 도착점까지 진행합니다.
            bool intermediate = m_DetourWaypoints != null && m_DetourIndex + 1 < m_DetourWaypoints.Length; // 중간 지점에서는 정지 거리를 예약하지 않습니다.
            float stopDistance = intermediate ? 0f : Mathf.Min(k_StopDistance, m_PathLength * 0.5f); // 짧은 마지막 구간도 음수 남은 거리를 만들지 않습니다.
            float remaining = Mathf.Max(0f, m_PathLength - stopDistance - m_Distance); // 실제 구간 끝까지 사용할 이동 거리입니다. // 마지막 정지 클립에 들어가기 전까지 남은 거리입니다.
            float consumedTime = deltaTime; // 정지 구간을 넘지 않았다면 프레임 시간을 모두 사용합니다.
            if (step >= remaining) // 이번 프레임에 마지막 한 걸음의 시작점까지 도달하는 경우입니다.
            { // 경계까지 걸린 시간을 구해 초과 시간을 정지 단계로 넘깁니다.
                float rampDistance = (previousSpeed + m_CurrentSpeed) * 0.5f * rampTime; // 현재 프레임의 가감속 구간 거리입니다.
                float signedAcceleration = targetSpeed >= previousSpeed ? acceleration : -acceleration; // 속도가 올라가는지 내려가는지 구분합니다.
                consumedTime = remaining <= rampDistance && rampTime > 0f ? 2f * remaining / Mathf.Max(0.0001f, previousSpeed + Mathf.Sqrt(Mathf.Max(0f, previousSpeed * previousSpeed + 2f * signedAcceleration * remaining))) : rampTime + (remaining - rampDistance) / targetSpeed; // 경계 통과 시간을 이차식 또는 정속식으로 구합니다.
                m_CurrentSpeed = Mathf.MoveTowards(previousSpeed, targetSpeed, acceleration * consumedTime); // 정지 진입 순간의 실제 속도를 유지합니다.
                step = remaining; // 걷기 루프가 마지막 정지 구간을 침범하지 않게 합니다.
            } // 경계 뒤의 이동은 마지막 발 디딤 계산에서 처리합니다.
            m_Distance += step; // 실제 전진한 거리만 걸음 진행도에 누적합니다.
            ApplyPathPosition(); // 접근의 기존 높이를 유지하고 횡단에서는 같은 고정 경로 위 실제 지면 높이를 적용합니다.
            m_Animator.SetFloat(k_WalkPhase, Mathf.Repeat(m_PhaseOffset + m_Distance / k_StrideLength, 1f)); // 속도 변경과 관계없이 발 디딤을 실제 이동 거리에 맞춥니다.
            if (intermediate && m_Distance >= m_PathLength - 0.00001f) // 도착한 중간 지점에서는 보행 상태를 계속 유지합니다.
            { // 같은 속도와 누적 걸음 위상을 다음 구간에 전달합니다.
                m_PhaseOffset = Mathf.Repeat(m_PhaseOffset + m_Distance / k_StrideLength, 1f); ++m_DetourIndex; PrepareDetourLeg(); // 경로만 바꾸고 대기·감속·출발을 반복하지 않습니다.
                return Mathf.Max(0f, deltaTime - consumedTime); // 이번 프레임의 남은 이동 시간을 보존합니다.
            } // 마지막 경유점에서만 기존 정지 애니메이션을 재생합니다.
            if (m_Distance >= m_PathLength - stopDistance) // 루프 마지막 자세가 정지 클립의 첫 자세와 만나는 지점입니다.
            { // 동일한 자세에서 연결하여 갑작스럽게 발을 모으지 않습니다.
                m_StopEntrySpeed = Mathf.Max(0.001f, m_CurrentSpeed); // 감속이 현재 보행 속도에서 이어지도록 진입 속도를 보관합니다.
                m_StopSpeedSetting = targetSpeed; // 정지 중 속도 설정 변경을 반영할 기준입니다.
                m_StopProgress = 0f; // 정지 클립을 첫 접지 자세부터 재생합니다.
                m_Animator.SetFloat(k_StopPhase, 0f); // Animator 평가 전에 정지 시간 파라미터를 초기화합니다.
                m_Animator.Play(k_StopState, 0, 0f); // 걷기 위상 0과 같은 자세의 정지 클립으로 이어 붙입니다.
                m_State = MovementState.Stopping; // 이제 루프를 반복하지 않고 마지막 발 디딤만 처리합니다.
            } // 정지로 전환되었다면 남은 시간으로 감속을 계속합니다.
            return Mathf.Max(0f, deltaTime - consumedTime); // 경계 통과 후 사용하지 않은 시간을 반환합니다.
        } // 걷기 루프와 마지막 한 걸음의 이동 구간을 구분합니다.

        private void AdvanceStop(float deltaTime) // 보행 클립과 무관하게 남은 거리를 감속 곡선으로 이동합니다.
        { // 이번 프레임에 배정된 시간만큼 실제 도착에 가까워집니다.
            float stopDistance = Mathf.Min(k_StopDistance, m_PathLength * 0.5f); // 짧은 경로에서도 마지막 감속이 경로 절반을 넘지 않게 합니다.
            float speedRatio = PositiveOrZero(m_Speed) / m_StopSpeedSetting; // 현재 지정한 보행 속도에 맞춰 감속 진행도도 함께 조정합니다.
            m_StopProgress = Mathf.Min(1f, m_StopProgress + deltaTime * m_StopEntrySpeed * speedRatio / (2f * stopDistance)); // 기본 속도에서 약 0.48초에 걸쳐 마지막 19.2cm를 이동합니다.
            float easedDistance = stopDistance * (2f * m_StopProgress - m_StopProgress * m_StopProgress); // 시작 속도에서 0까지 선형 감속하는 이동 거리입니다.
            m_Distance = m_PathLength - stopDistance + easedDistance; // 마지막 발 디딤을 실제 도착점까지 맞춥니다. // 도착 지점을 넘거나 다시 뒤로 이동하지 않는 단조 증가 거리입니다.
            ApplyPathPosition(); // 마지막 발 디딤에도 같은 경로와 바닥 높이를 적용해 가까운 인도 위에서 멈춥니다.
            m_Animator.SetFloat(k_StopPhase, m_StopProgress); // 감속과 실제 마지막 발 디딤을 하나의 시간으로 맞춥니다.
            if (m_StopProgress < 1f) return; // 발이 완전히 모이기 전에는 대기 자세로 전환하지 않습니다.
            m_CurrentSpeed = 0f; // 도착한 캐릭터의 이동 속도를 확실히 제거합니다.
            m_State = m_CrossingStarted ? MovementState.Crossed : MovementState.Arrived; // 접근 완료는 횡단 명령을 기다리고 횡단 완료는 추가 횡단을 허용하지 않습니다.
            m_Animator.CrossFadeInFixedTime(k_IdleState, Mathf.Clamp(m_FadeTime, 0.05f, 0.4f), 0, 0f); // 정지 클립의 마지막 자세와 같은 대기 자세에서 호흡을 이어갑니다.
        } // 도착 뒤에는 경로 위치를 변경하지 않습니다.

        private void ApplyPathPosition() // 접근과 횡단의 실제 루트 위치 적용을 같은 보행 거리에서 처리합니다.
        { // 좌우 경로는 고정하고 횡단할 때만 기존 도로와 인도의 높이 차이를 반영합니다.
            Vector3 position = m_PathStart + m_PathDirection * m_Distance; // 현재 보폭에 대응하는 횡단보도 중심선 위 수평 위치를 구합니다.
            if (m_CrossingStarted) // 기존 인도 접근에는 바닥 조회나 높이 변경을 추가하지 않습니다.
            { // 환경의 Collider를 직접 조회하므로 자동차나 친구를 바닥으로 오인하지 않습니다.
                Ray groundRay = new Ray(position + Vector3.up, Vector3.down); // 출발 인도보다 1m 위에서 수직으로 실제 지면을 찾습니다.
                float groundHeight = float.NegativeInfinity; // 도로 아래 바탕 지면보다 실제 보행 표면을 우선하기 위한 초기 높이입니다.
                foreach (Collider ground in m_CrossingGrounds) // 최초 명령 때 확보한 기존 환경 Collider만 조회합니다.
                { // 새 Collider나 자동차 충돌 판정을 만들지 않습니다.
                    if (ground == null || !ground.enabled || ground.isTrigger || !ground.gameObject.activeInHierarchy) continue; // 실제 바닥으로 사용되는 활성 일반 Collider만 읽습니다.
                    if (ground.Raycast(groundRay, out RaycastHit hit, 2f)) groundHeight = Mathf.Max(groundHeight, hit.point.y); // 겹친 바탕 지면·도로·연석·인도 중 가장 위의 표면을 선택합니다.
                } // 횡단보도의 고정 X·Z 경로는 지면 조회 결과에 의해 바뀌지 않습니다.
                position.y = IsFinite(groundHeight) ? groundHeight : transform.position.y; // 실제 바닥에 발 높이를 맞추고 조회가 없는 위치에서는 직전 높이를 유지합니다.
            } // 접근은 기존 평면 경로의 높이를 그대로 사용합니다.
            transform.position = position; // 경로 이탈이나 도착점 초과 없이 계산한 루트 위치를 최종 적용합니다.
        } // Animator의 걸음 진행도는 기존 수평 전진 거리를 계속 사용합니다.

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value); // 좌표와 설정값에 비정상적인 부동소수점 값이 들어왔는지 판별합니다.
        private static float PositiveOrZero(float value) => IsFinite(value) && value > 0f ? value : 0f; // 시간·속도 설정을 계산 가능한 0 이상의 값으로 제한합니다.
    } // 친구 캐릭터의 독립적인 한 번 걷기 컴포넌트를 마칩니다.
} // 기존 차량·신호·XR 기능은 이 스크립트에서 변경하지 않습니다.
