using UnityEngine; // 명령 순간의 신호 판정과 짧은 신호등 응시 반응을 관리합니다.

namespace Y.Crosswalk // 기존 횡단보도 이동·신호·차량과 같은 개인 작업 영역을 사용합니다.
{ // 신호에 따른 명령 분기를 캐릭터의 보폭·경로 계산에서 분리합니다.
    [DisallowMultipleComponent] // 한 친구의 신호 판정과 위험 반응이 중복 실행되지 않게 합니다.
    [RequireComponent(typeof(FriendCharacterMovement))] // 실제 이동과 정지는 기존 친구 이동 컴포넌트에 요청합니다.
    [DefaultExecutionOrder(200)] // 이동·신호 갱신 뒤 상태를 읽고 기존 휴대전화 자세 이후 머리 시선을 적용합니다.
    public sealed class FriendCrossingDecision : MonoBehaviour // 현재 신호의 스냅샷으로 횡단 허용과 위험 반응을 구분합니다.
    { // 현재 요청의 진행 상태와 이후에도 읽을 판정 결과를 보관합니다.
        public enum CrossingCommandResult // 이후 사고·교육 시스템이 이동 상태와 별도로 읽을 명령 판정입니다.
        { // 실제 구현한 두 보행 신호에 대응하는 결과만 정의합니다.
            [InspectorName("아직 명령 없음")] None, // 이번 실행에서 아직 횡단 명령을 판정하지 않은 상태입니다.
            [InspectorName("초록불의 안전 명령")] SafeOnGreen, // 명령 순간 초록불이어서 차량 진입을 보호하며 횡단하는 요청입니다.
            [InspectorName("빨간불의 위험 명령")] DangerousOnRed // 명령 순간 빨간불이어서 인도에서 멈칫하고 대기하는 요청입니다.
        } // 점수·사고·정답 판정은 추가하지 않습니다.

        private enum CommandPhase { Idle, WaitingForClearRoad, Crossing, RedReaction } // 명령 대기·차량 통과 대기·정상 횡단·빨간불 반응을 서로 배타적으로 관리합니다.

        [Header("기존 신호와 차량 연결")] // 머티리얼이나 표시 색상이 아닌 기존 상태 컴포넌트를 연결합니다.
        [SerializeField, InspectorName("보행자 신호등")] // 차량이 따르는 것과 같은 보행자 신호를 사용합니다.
        [Tooltip("CrosswalkScene의 기존 PedestrianTrafficLight입니다. 건너와!를 누른 순간 CurrentState를 한 번 읽어 이번 요청의 판정을 고정합니다.")] // 횡단 도중 바뀐 신호로 최초 명령의 결과를 다시 판정하지 않음을 설명합니다.
        private PedestrianTrafficLight m_TrafficLight; // 자동 전환 시간과 표시를 변경하지 않는 신호 상태 참조입니다.

        [SerializeField, InspectorName("횡단 구역 차량")] // 현재 씬의 기존 차량을 연결합니다.
        [Tooltip("이 횡단보도를 지나는 기존 TrafficCarMovement입니다. 이미 정지점을 지난 차량이 완전히 통과한 뒤 친구가 출발합니다.")] // 차량 급정거나 충돌 연출 대신 기존 통과·제동 흐름을 재사용함을 안내합니다.
        private TrafficCarMovement[] m_TrafficCars; // 정상 횡단 출발 전에 통과 여부를 읽을 현재 차량 목록입니다.

        [Header("빨간불 반응")] // 기존 머리 뼈대와 실제 신호 표시 위치만 사용합니다.
        [SerializeField, InspectorName("신호등 응시 지점")] // 빨간 사람 표시의 중심 Transform을 연결합니다.
        [Tooltip("기존 RedSignal_StandingPerson입니다. 표시가 꺼지더라도 같은 신호등 위치를 바라보며 판정 결과는 바꾸지 않습니다.")] // 신호가 반응 중 바뀌어도 최초 위험 명령 반응을 끝내도록 설명합니다.
        private Transform m_SignalLookTarget; // 친구가 고개를 들어 바라볼 기존 신호등의 높이와 위치입니다.

        [SerializeField, InspectorName("친구 머리 관절")] // 기존 모델의 Head를 연결합니다.
        [Tooltip("VisualRoot 아래 기존 Head입니다. 새 모델이나 얼굴 구조 없이 고개를 들어 신호등을 보는 짧은 반응을 만듭니다.")] // 현재 일반 골격 Animator와 호환되는 회전 방식임을 안내합니다.
        private Transform m_Head; // 휴대전화 자세 적용 뒤 신호등 쪽으로 조절할 기존 머리 뼈대입니다.

        [SerializeField, Min(1.8f), InspectorName("위험 반응 시간(초)")] // 발 디딤 시도와 기존 손들기를 볼 수 있는 짧은 시간을 지정합니다.
        [Tooltip("기본값 2초 동안 빨간불이야!를 표시하고 신호등을 바라봅니다. 반응이 끝나면 같은 인도 위치에서 다시 명령을 받을 수 있습니다.")] // 반복 입력은 반응을 다시 시작하지 않음을 안내합니다.
        private float m_RedReactionDuration = 2f; // 기존 0.2초 발 디딤 시도와 1.6초 손들기를 포함하는 반응 시간입니다.

        [Header("횡단 명령 판정 기록")] // 이후 기능에서 사용할 결과와 위험 이력을 이동 상태와 구분합니다.
        [SerializeField, InspectorName("마지막 횡단 명령 판정")] // 마지막으로 수락한 횡단 명령의 결과를 표시합니다.
        [Tooltip("명령 순간의 신호를 기록합니다. 신호가 자동 전환되어도 바뀌지 않으며 다음 횡단 명령을 수락할 때 갱신됩니다.")] // 현재 신호 자체와 명령 판정 결과의 차이를 설명합니다.
        private CrossingCommandResult m_LastDecision; // 마지막 명령이 초록불이었는지 빨간불이었는지 보존합니다.

        [SerializeField, InspectorName("위험 명령을 받은 이력")] // 빨간불 명령이 한 번이라도 있었는지 별도로 표시합니다.
        [Tooltip("빨간불 명령을 받으면 켜지고 이후 초록불 명령을 받아도 유지됩니다. 이번 단계에서는 점수나 사고 연출에 연결하지 않습니다.")] // 위험 이력이 정상 횡단 판정으로 덮어써지지 않음을 안내합니다.
        private bool m_HasDangerousCommand; // 이번 실행에서 위험 명령이 있었던 사실을 보존합니다.

        private FriendCharacterMovement m_Movement; // 경로와 Animator 상태는 기존 이동 컴포넌트가 전담합니다.
        private CommandPhase m_Phase; // 이번 명령의 중복 실행과 반응 중 새 명령을 막는 진행 상태입니다.
        private float m_RedReactionRemaining; // 위험 반응을 처음 정한 시간만큼만 유지합니다.
        private Quaternion m_AnimatedHead; // 연출을 끌 때 복원할 이번 프레임의 기존 머리 자세입니다.
        private bool m_HeadPoseApplied; // 이 컴포넌트가 머리 회전을 추가했는지 기록합니다.

        public CrossingCommandResult LastDecision => m_LastDecision; // 이후 시스템이 최초 신호에 따른 마지막 판정을 읽을 수 있게 합니다.
        public bool HasDangerousCommand => m_HasDangerousCommand; // 마지막 판정과 별개로 위험 명령 이력을 제공합니다.
        public bool IsReactingToRed => m_Phase == CommandPhase.RedReaction; // 기존 월드 안내가 빨간불이야!를 표시할 조건입니다.
        public bool CanReceiveCrossCommand => isActiveAndEnabled && m_Phase == CommandPhase.Idle && m_Movement != null && m_Movement.CanReceiveCrossCommand && m_TrafficLight != null && m_TrafficLight.isActiveAndEnabled && m_TrafficCars != null && m_TrafficCars.Length > 0 && m_SignalLookTarget != null; // 신호와 차량 연결이 있고 이전 요청의 처리 중이 아닌 인도 대기 상태에서만 새 횡단 명령을 받습니다.
        public bool CanReceiveStopCommand => m_Movement != null && !IsReactingToRed && (m_Phase == CommandPhase.WaitingForClearRoad || m_Movement.CanReceiveStopCommand); // 보행 중 멈춤과 초록불 명령 뒤 차량 통과를 기다리는 요청의 취소를 허용합니다.
        public bool RequiresTrafficHold => m_Movement != null && gameObject.activeInHierarchy && (m_Phase == CommandPhase.WaitingForClearRoad || (m_Movement.HasStartedCrossing && !m_Movement.HasCompletedCrossing)); // 신호 변경이나 도로 위 멈춤에도 친구가 횡단을 마칠 때까지 차량 진입을 막고 친구가 사라지면 해제합니다.

        private void Awake() // 같은 친구의 기존 이동 상태 제공자를 준비합니다.
        { // 신호와 차량의 씬 참조는 프리팹 인스턴스에 저장된 연결을 사용합니다.
            m_Movement = GetComponent<FriendCharacterMovement>(); // 입력·판정 쪽에서 보폭이나 Transform 이동을 중복 구현하지 않습니다.
        } // 명령이 없을 때에는 신호나 차량 상태에 개입하지 않습니다.

        public bool TryRequestCrossing() // 기존 선택 입력이 건너와!를 실행할 때 진입하는 판정 지점입니다.
        { // 한 번 수락한 요청은 완료·취소·위험 반응 종료 전까지 다시 시작하지 않습니다.
            if (!CanReceiveCrossCommand) return false; // 이동 중·반응 중·연결 누락 상태의 입력을 무시합니다.
            PedestrianTrafficLight.SignalState signal = m_TrafficLight.CurrentState; // 선택 입력을 받은 이 순간의 기존 신호 상태를 한 번만 읽습니다.
            if (signal == PedestrianTrafficLight.SignalState.Red) // 빨간불 명령은 실제 도로 횡단으로 전달하지 않습니다.
            { // 차량의 주행이나 급정거를 유발하지 않고 친구의 짧은 반응만 요청합니다.
                if (!m_Movement.TryReactToRedSignal(m_SignalLookTarget)) return false; // 이동 상태가 바뀌어 반응을 수락할 수 없으면 중복 실행하지 않습니다.
                m_LastDecision = CrossingCommandResult.DangerousOnRed; // 위험 명령의 판정을 현재 신호 자동 전환과 독립적으로 기록합니다.
                m_HasDangerousCommand = true; // 이후 안전 명령이 들어와도 남을 위험 이력을 설정합니다.
                m_RedReactionRemaining = Mathf.Max(1.8f, m_RedReactionDuration); // 발 디딤 시도와 손들기를 볼 수 있는 시간을 한 번만 시작합니다.
                m_Phase = CommandPhase.RedReaction; // 새 횡단·멈춤 입력이 같은 반응을 반복시키지 못하게 합니다.
                return true; // 기존 상호작용 표시가 빨간불 안내를 켜도록 명령 수락을 알립니다.
            } // 초록불에서 수락한 요청은 이후 신호가 바뀌어도 이 판정을 유지합니다.
            m_LastDecision = CrossingCommandResult.SafeOnGreen; // 초록불 명령이라는 사실을 차량 통과 대기 전에 기록합니다.
            m_Phase = CommandPhase.WaitingForClearRoad; // 차량이 이번 프레임부터 기존 정지점 앞에서 진입을 보류하게 합니다.
            return TryStartWhenRoadIsClear(); // 이미 진입한 차량이 없으면 즉시 기존 횡단 준비를 시작하고 있으면 인도에서 기다립니다.
        } // 신호등의 색상 머티리얼·자동 전환 시간은 변경하지 않습니다.

        public bool TryStopCommand(Transform commandSource) // 기존 멈춤 기능과 차량 통과 대기 취소를 같은 입력으로 처리합니다.
        { // 이미 도로에 있는 친구를 멈춰도 차량 진입 보호는 유지합니다.
            if (!CanReceiveStopCommand) return false; // 위험 반응 중 입력이나 이미 멈춘 친구의 반복 명령은 처리하지 않습니다.
            bool cancelledWait = m_Phase == CommandPhase.WaitingForClearRoad; // 아직 도로로 출발하지 않은 요청을 취소하는지 기록합니다.
            if (cancelledWait) m_Phase = CommandPhase.Idle; // 대기를 취소하면 신호가 바뀌거나 차량이 통과해도 자동 출발하지 않습니다.
            return m_Movement.TryStop(commandSource) || cancelledWait; // 기존 정지 반응을 재사용하고 이미 정지한 채 기다리던 요청의 취소도 수락합니다.
        } // 도로 위 정지는 실제 횡단 완료가 아니므로 차량 보호를 해제하지 않습니다.

        private bool TryStartWhenRoadIsClear() // 초록불 요청에 필요한 차량 통과 확인만 수행합니다.
        { // 최초 신호 판정을 다시 읽지 않아 대기 중 신호 변경이 결과를 덮어쓰지 않습니다.
            if (!m_Movement.CanReceiveCrossCommand) // 대기 중 친구 이동이 비활성화되거나 다른 상태로 바뀐 경우입니다.
            { // 인도에서 대기하던 요청이 더 이상 뒤늦게 출발하지 않게 합니다.
                m_Phase = CommandPhase.Idle; // 실제 횡단 전 요청의 차량 진입 보호를 해제합니다.
                return false; // 새로운 입력이 필요한 대기 상태로 돌아갑니다.
            } // 기존 이동 상태가 유효한 경우에만 차량 통과 여부를 읽습니다.
            foreach (TrafficCarMovement car in m_TrafficCars) // 현재 횡단보도를 지나는 연결된 기존 차량만 확인합니다.
            { // 정지점을 지난 차량을 횡단보도 안에서 강제로 세우지 않습니다.
                if (car == null || !car.IsClearForFriendCrossing) return true; // 차량 뒤쪽까지 통과할 때까지 요청은 유지하고 친구는 인도에서 기다립니다.
            } // 모든 차량이 진입 전이거나 완전히 통과한 경우에만 보행을 시작합니다.
            bool started = m_Movement.TryBeginCrossing(); // 이전 작업의 방향 전환·고정 경로·걷기·감속·대기를 그대로 실행합니다.
            m_Phase = started ? CommandPhase.Crossing : CommandPhase.Idle; // 실제 시작이 실패하면 대기 요청과 차량 보호를 남기지 않습니다.
            return started; // 입력 처리에 이번 요청의 수락 결과를 돌려줍니다.
        } // 자동차 충돌·급정거·점수 판정은 수행하지 않습니다.

        private void Update() // 기존 이동과 신호 갱신 뒤 수락된 요청의 남은 단계만 진행합니다.
        { // 명령 대기 상태에서는 자동으로 횡단을 시작하지 않습니다.
            if (m_Phase == CommandPhase.WaitingForClearRoad) TryStartWhenRoadIsClear(); // 초록불 때 수락한 요청만 차량 통과 후 출발시킵니다.
            if (m_Phase == CommandPhase.Crossing && m_Movement.HasCompletedCrossing) m_Phase = CommandPhase.Idle; // 가까운 인도 도착 뒤 기존 빨간불 차량 주행을 허용합니다.
            if (!IsReactingToRed) return; // 위험 반응 타이머는 빨간불 명령이 있을 때만 진행합니다.
            m_RedReactionRemaining = Mathf.Max(0f, m_RedReactionRemaining - Time.deltaTime); // 반복 입력 없이 최초 반응 시간만 줄입니다.
            if (m_RedReactionRemaining <= 0f) m_Phase = CommandPhase.Idle; // 반응이 끝나면 위험 이력은 남기고 같은 인도에서 새 명령을 허용합니다.
        } // 신호가 바뀌었다는 이유만으로 새 횡단이나 새 위험 반응을 만들지 않습니다.

        private void LateUpdate() // 기존 Animator와 휴대전화 자세 뒤에 신호등을 올려다보는 머리 회전만 더합니다.
        { // 새 표정 모델이나 별도 Animator를 만들지 않습니다.
            if (!IsReactingToRed || m_Head == null || m_SignalLookTarget == null) // 빨간불 반응이 아닌 때에는 기존 고개 동작을 유지합니다.
            { // Animator가 매 프레임 원래 자세를 기록하므로 이전 반응 회전을 누적하지 않습니다.
                m_HeadPoseApplied = false; // 현재 프레임에는 별도 머리 회전을 적용하지 않았음을 기록합니다.
                return; // 기존 카메라와 친구의 다른 뼈대에는 접근하지 않습니다.
            } // 활성 위험 반응에 대해서만 실제 신호등 위치를 바라봅니다.
            m_AnimatedHead = m_Head.localRotation; // 비활성화 시 복원할 기존 애니메이션의 머리 자세를 저장합니다.
            Vector3 direction = m_Head.parent.InverseTransformDirection(m_SignalLookTarget.position - m_Head.position); // 몸이 신호등 쪽으로 돌아가는 동안에도 머리 부모 좌표에서 응시 방향을 구합니다.
            if (direction.sqrMagnitude <= 0.0001f) return; // 목표와 머리가 겹쳐 방향을 구할 수 없을 때는 기본 자세를 유지합니다.
            float elapsed = Mathf.Max(1.8f, m_RedReactionDuration) - m_RedReactionRemaining; // 현재 반응에서 경과한 시간으로 머리 들기 비율을 계산합니다.
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / 0.2f, m_RedReactionRemaining / 0.3f)); // 처음 0.2초에 고개를 들고 마지막 0.3초에 대기 자세로 부드럽게 돌려줍니다.
            float pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, -40f, 20f); // 높은 신호 표시를 보되 머리를 과도하게 젖히지 않게 합니다.
            float yaw = Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -60f, 60f); // 몸의 회전을 기다리는 동안 신호등 쪽으로 머리도 자연스럽게 돌립니다.
            m_Head.localRotation = Quaternion.Slerp(m_AnimatedHead, BlenderRigPose.LocalRotation(m_Head, Quaternion.Euler(pitch, yaw, 0f)), blend); // 기존 골격의 머리만 응시 자세와 섞습니다.
            m_HeadPoseApplied = true; // 이 연출을 끌 때 원래 머리 자세로 복원할 수 있게 기록합니다.
        } // 플레이어 시점과 XR 카메라 추적은 변경하지 않습니다.

        private void OnDisable() // 비활성화로 남은 차량 통과 대기와 위험 안내를 정리합니다.
        { // 실제 횡단 중의 차량 보호는 이동 완료 상태에서 계속 계산하여 도로 위 친구를 통과하지 않게 합니다.
            m_Phase = CommandPhase.Idle; // 비활성화 중 취소된 대기 요청이 재활성화 뒤 자동 출발하지 않게 합니다.
            m_RedReactionRemaining = 0f; // 이전 위험 반응을 중간부터 다시 재생하지 않습니다.
            if (m_HeadPoseApplied && m_Head != null) m_Head.localRotation = m_AnimatedHead; // 직접 추가한 신호등 응시 회전만 복원합니다.
            m_HeadPoseApplied = false; // 같은 머리 자세를 중복해서 복원하지 않습니다.
        } // 마지막 판정과 위험 명령 이력은 비활성화로 지우지 않습니다.
    } // 현재 작업 범위의 신호별 횡단 분기만 관리합니다.
} // 사고·교육 점수·재시도 연출은 추가하지 않습니다.
