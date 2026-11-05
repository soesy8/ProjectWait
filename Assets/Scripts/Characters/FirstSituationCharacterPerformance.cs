using System.Collections; // 충돌과 감사 동작을 순서대로 재생하고 상황 제어기에서 종료를 기다립니다.
using UnityEngine; // 기존 골격 Transform과 Animator의 자세를 재사용합니다.

namespace Y.Crosswalk // 첫 번째 상황의 캐릭터 연출을 기존 개인 작업 영역에 둡니다.
{ // 보행 경로와 분리하여 관절 연출과 정확한 재시도 복원만 담당합니다.
    [DefaultExecutionOrder(300)] // 기존 Animator·휴대전화·멈춤 반응 이후에 충격과 감사 자세를 적용합니다.
    public sealed class FirstSituationCharacterPerformance : MonoBehaviour // 친구 1 전용 연출이며 친구 2의 상태를 참조하지 않습니다.
    { // 씬에 저장된 실제 관절과 재시도할 자세를 보관합니다.
        [SerializeField, InspectorName("기존 친구 이동"), Tooltip("친구 1의 FriendCharacterMovement를 연결합니다.")] private FriendCharacterMovement m_Movement; // 기존 걷기와 정지 기능을 호출합니다.
        [SerializeField, InspectorName("기존 휴대전화 자세"), Tooltip("친구 1의 FriendPhoneUsage를 연결합니다.")] private FriendPhoneUsage m_Phone; // 관찰 중 전화 자세를 고정하고 안전 결과에서는 자연스럽게 해제합니다.
        [SerializeField, InspectorName("기존 Animator"), Tooltip("친구 1 VisualRoot의 Animator입니다.")] private Animator m_Animator; // 기존 골격과 컨트롤러를 그대로 사용합니다.
        [SerializeField, InspectorName("시각 모델 루트"), Tooltip("발의 접지 높이를 보정할 VisualRoot입니다.")] private Transform m_VisualRoot; // 충격 중 발이 바닥 아래로 내려가는 경우 모델만 높이를 보정합니다.
        [SerializeField, InspectorName("골반"), Tooltip("기존 Hips 관절입니다.")] private Transform m_Hips; // 균형을 잃을 때 무게중심을 낮춥니다.
        [SerializeField, InspectorName("허리"), Tooltip("기존 Spine 관절입니다.")] private Transform m_Spine; // 골반과 독립적으로 허리를 뒤로 젖힙니다.
        [SerializeField, InspectorName("가슴"), Tooltip("기존 UpperChest 관절입니다.")] private Transform m_Chest; // 충격이 상체로 이어지는 동작을 만듭니다.
        [SerializeField, InspectorName("머리"), Tooltip("기존 Head 관절입니다.")] private Transform m_Head; // 충격 반동과 감사의 끄덕임을 표현합니다.
        [SerializeField, InspectorName("왼쪽 위팔"), Tooltip("기존 LeftUpperArm 관절입니다.")] private Transform m_LeftArm; // 휴대전화를 든 팔의 반동을 표현합니다.
        [SerializeField, InspectorName("왼쪽 아래팔"), Tooltip("기존 LeftLowerArm 관절입니다.")] private Transform m_LeftForearm; // 휴대전화가 손과 함께 흔들리게 합니다.
        [SerializeField, InspectorName("오른쪽 위팔"), Tooltip("기존 RightUpperArm 관절입니다.")] private Transform m_RightArm; // 균형 잡기와 감사 손짓을 표현합니다.
        [SerializeField, InspectorName("오른쪽 아래팔"), Tooltip("기존 RightLowerArm 관절입니다.")] private Transform m_RightForearm; // 팔꿈치를 구부려 손을 흔듭니다.
        [SerializeField, InspectorName("왼쪽 허벅지"), Tooltip("기존 LeftUpperLeg 관절입니다.")] private Transform m_LeftLeg; // 뒤로 물러서며 균형을 잡는 다리를 표현합니다.
        [SerializeField, InspectorName("왼쪽 종아리"), Tooltip("기존 LeftLowerLeg 관절입니다.")] private Transform m_LeftShin; // 무릎을 굽혀 충격을 받습니다.
        [SerializeField, InspectorName("오른쪽 허벅지"), Tooltip("기존 RightUpperLeg 관절입니다.")] private Transform m_RightLeg; // 반대 다리가 뒤늦게 디디는 움직임을 표현합니다.
        [SerializeField, InspectorName("오른쪽 종아리"), Tooltip("기존 RightLowerLeg 관절입니다.")] private Transform m_RightShin; // 엇갈린 무릎 움직임으로 균형 회복 시도를 보여 줍니다.
        [SerializeField, InspectorName("양발"), Tooltip("왼발과 오른발 관절을 연결합니다.")] private Transform[] m_Feet; // 밑창의 최저 높이를 계산합니다.
        [SerializeField, InspectorName("놀란 입 표정"), Tooltip("Head 아래 미리 배치한 표정 이미지입니다. 숨기면 기존 모델의 미소가 보입니다.")] private GameObject m_SurpriseMouth; // 원본 얼굴 메시를 바꾸지 않고 놀란 표정과 미소를 전환합니다.
        [SerializeField, InspectorName("전봇대 안전 우회 지점"), Tooltip("전봇대 앞에서 옆으로 비키는 지점과 기둥을 지나 멈출 지점 두 곳을 순서대로 연결합니다. 두 번째 지점에서는 추가 이동 없이 플레이어를 바라보고 감사합니다.")] private Transform[] m_SafeWaypoints; // 기둥을 피하는 두 보행 구간만 기존 이동기에 전달합니다.
        [SerializeField, Range(0.3f, 1.6f), InspectorName("안전 우회 보행 속도"), Tooltip("경고를 듣고 앞을 보며 전봇대를 피할 때의 실제 보행 속도입니다.")] private float m_SafeWalkSpeed = 1.1f; // 도약이나 순간 이동 없이 기존 발걸음으로 피하게 합니다.
        [SerializeField, Min(0.5f), InspectorName("우회 중 기둥과의 여유 거리(m)"), Tooltip("전봇대 Collider 표면에서 친구 중심까지 확보할 최소 거리입니다. 몸통과 걷는 팔이 기둥에 붙지 않는 인도 경로만 사용합니다.")] private float m_PoleClearance = 0.8f; // 목적지만 보지 않고 각 직선 보행 구간 전체에서 기둥과의 간격을 확보합니다.
        [SerializeField, Range(0.22f, 0.6f), InspectorName("돌아설 때 한 발 디딤 시간"), Tooltip("플레이어에게 돌아설 때 발을 번갈아 들어 내려놓는 시간입니다. 이동 루트는 그 자리에 유지합니다.")] private float m_TurnStepDuration = 0.36f; // 대기 자세로 미끄러지듯 돌지 않고 짧은 발 디딤으로 방향을 바꿉니다.
        [SerializeField, Range(0.02f, 0.1f), InspectorName("돌아설 때 발 들기 높이(m)"), Tooltip("방향을 바꿀 때 옮기는 발의 최대 높이입니다. 반대쪽 발은 디딘 위치를 유지합니다.")] private float m_TurnFootLift = 0.055f; // 발끝이 지면을 끌지 않는 낮은 제자리 디딤을 표현합니다.
        private Transform[] m_Bones; // 초기화 시 확보한 이 친구의 실제 시각 계층입니다.
        private Vector3[] m_CheckpointPositions, m_CheckpointScales; // 관찰 시점의 소품·관절 위치와 크기를 보관합니다.
        private Quaternion[] m_CheckpointRotations, m_ImpactRotations; // 재시도 자세와 충돌 직전 자세를 따로 보관합니다.
        private Vector3[] m_ImpactPositions, m_ImpactScales; // 충격 연출의 기준 자세를 매 프레임 복원하여 누적 변형을 막습니다.
        private Vector3 m_ImpactRootPosition, m_Backward; // 실제 접촉 위치와 뒤로 물러날 수평 방향을 보관합니다.
        private float m_ImpactTime, m_SafeTime; // 전역 시간 배율과 무관한 이 친구의 연출 시간입니다.
        private bool m_ImpactPlaying, m_SafePlaying, m_TurnPlaying; // 충돌·감사·돌아서기 중 현재 연출에 필요한 관절 처리만 실행합니다.
        private readonly Vector3[] m_TurnFootPositions = new Vector3[2]; // 왼발과 오른발이 마지막으로 디딘 월드 위치를 각각 유지합니다.
        private readonly Quaternion[] m_TurnFootRotations = new Quaternion[2]; // 지지하는 발의 방향이 루트 회전에 끌려 돌지 않게 합니다.
        private Vector3 m_TurnLookDirection; // 머리와 가슴이 몸보다 먼저 향할 플레이어의 수평 방향입니다.
        private float m_TurnStepTime; // 현재 옮기는 한 발의 진행 시간입니다.
        private int m_TurnSwingFoot, m_TurnSettleSteps; // 지금 옮기는 발과 몸이 돌아선 뒤 마무리할 발 디딤 수를 보관합니다.
        private bool m_TurnFeetSettled; // 양발이 새 방향으로 내려온 뒤에만 감사 손짓을 시작합니다.
        private enum SafePhase { None, Notice, Avoidance, Turning, Thanks, Finished } // 위험 발견·실제 우회·플레이어 보기·감사·완료를 순서대로 구분합니다.
        private SafePhase m_SafePhase; // 걷는 동안 감사 손짓이 먼저 나오지 않게 합니다.
        public bool IsThanking => m_SafePhase == SafePhase.Thanks; // 상황 제어기가 우회 후의 감사 시점에만 대사를 표시하게 합니다.
        public bool IsSafeSequenceRunning => m_SafePhase != SafePhase.None && m_SafePhase != SafePhase.Finished; // 도중 중단을 정상 완료로 오인하지 않게 합니다.
        public bool HasCompletedSafeAvoidance => m_SafePhase == SafePhase.Finished; // 실제 우회와 감사가 모두 끝나야 첫 번째 상황 완료를 허용합니다.

        private void Awake() // 기존 시각 계층의 Transform만 찾아 재사용합니다.
        { // UI나 캐릭터 오브젝트를 실행 중 생성하지 않습니다.
            m_Bones = m_Animator.GetComponentsInChildren<Transform>(true); // 휴대전화까지 포함하여 정확한 복원 대상을 확보합니다.
            m_CheckpointPositions = new Vector3[m_Bones.Length]; m_CheckpointScales = new Vector3[m_Bones.Length]; // 체크포인트용 위치·크기 저장 공간을 준비합니다.
            m_CheckpointRotations = new Quaternion[m_Bones.Length]; m_ImpactRotations = new Quaternion[m_Bones.Length]; // 체크포인트와 충격 기준 회전을 독립적으로 보관합니다.
            m_ImpactPositions = new Vector3[m_Bones.Length]; m_ImpactScales = new Vector3[m_Bones.Length]; // 충격 중 덮어쓴 자세가 다음 프레임에 누적되지 않게 합니다.
            BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); // 첫 보행에는 기존 친구의 얼굴을 표시합니다.
        } // Animator 컨트롤러와 모델 자산은 교체하지 않습니다.

        public void Freeze(bool frozen) // 관찰 중 친구의 경로·Animator·휴대전화 자세만 함께 멈춥니다.
        { // 소품은 손에 붙은 현재 위치를 유지합니다.
            if (frozen) m_TurnPlaying = false; // 상황을 중단하거나 관찰 자세로 복원할 때 남은 발 디딤을 진행하지 않습니다.
            if (m_Movement != null) m_Movement.SetScenarioPaused(frozen); // 씬 종료 중 아직 살아 있는 친구의 이동만 정지하거나 재개합니다.
            if (m_Phone != null) m_Phone.SetScenarioPaused(frozen); // 먼저 해제된 휴대전화 컴포넌트에는 접근하지 않습니다.
        } // 헤드셋 추적과 XR 선택 입력에는 접근하지 않습니다.

        public void CaptureCheckpoint() // 전화 사용 자세까지 적용된 프레임을 재시도의 기준으로 저장합니다.
        { // 상황 제어기의 LateUpdate에서 한 번 호출합니다.
            m_Movement.CaptureScenarioCheckpoint(); // 루트 경로 거리와 기존 걸음 상태를 저장합니다.
            m_Phone.CaptureScenarioCheckpoint(); // 휴대전화의 자세 가중치와 엄지 조작 진행도도 같은 시점에 저장합니다.
            SavePose(m_CheckpointPositions, m_CheckpointRotations, m_CheckpointScales); // 휴대전화와 모든 기존 관절의 실제 자세를 저장합니다.
            Freeze(true); // 저장한 순간의 걷기 자세로 관찰을 시작합니다.
        } // 휴대전화를 따로 재부착하거나 다른 위치에 생성하지 않습니다.

        public void RestoreCheckpoint() // 사고 효과를 끝내고 같은 관찰 자세로 복원합니다.
        { // 캐릭터 복원은 화면 전환으로 가린 동안 수행합니다.
            m_ImpactPlaying = false; m_SafePlaying = false; m_TurnPlaying = false; // 이전 충격·감사·발 디딤이 복원한 관찰 자세를 다시 덮어쓰지 않게 합니다.
            m_SafePhase = SafePhase.None; m_Phone.RestoreScenarioCheckpoint(); // 취소된 안전 우회와 전화 사용 중단 상태를 관찰 시점으로 되돌립니다.
            Freeze(true); // 복원 중에도 보행은 멈춘 상태를 유지합니다.
            m_Movement.RestoreScenarioCheckpoint(); // 관찰 지점의 루트와 보행 진행도를 함께 되돌립니다.
            LoadPose(m_CheckpointPositions, m_CheckpointRotations, m_CheckpointScales); // 손에 붙은 전화까지 같은 자세로 되돌립니다.
            BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); // 사고 표정을 지우고 원래 관찰 표정을 복원합니다.
            BlenderRigPose.ResetFace(m_Head); // 다시 관찰할 때 취소된 감사 대사의 입 움직임과 웃음을 지웁니다.
        } // 전봇대나 플레이어 위치는 되돌리지 않습니다.

        public IEnumerator PlayImpact() // 실제 전봇대 접촉 뒤 머리·상체·팔·양다리에 이어지는 반동을 재생합니다.
        { // 이동을 멈춘 뒤 기존 자세에서 충격을 시작합니다.
            Freeze(true); // 경로 이동이 전봇대를 관통하지 못하게 즉시 중단합니다.
            SavePose(m_ImpactPositions, m_ImpactRotations, m_ImpactScales); // 맞닿은 프레임의 전화 사용 자세를 기준으로 삼습니다.
            m_ImpactRootPosition = transform.position; m_Backward = -transform.forward; // 접촉 위치에서 뒤로만 물러나게 합니다.
            m_ImpactPlaying = true; m_ImpactTime = 0f; // 새로운 충격을 한 번만 시작합니다.
            while (m_ImpactTime < 2.2f) // 접촉 정지와 슬로우모션을 포함한 한 번의 휘청임을 진행합니다.
            { // Animator나 게임 전체 속도를 변경하지 않습니다.
                float rate = m_ImpactTime < 0.12f ? 1f : m_ImpactTime < 0.24f ? 0.24f : m_ImpactTime < 0.7f ? 0.48f : 1f; // 충격 직후의 반동을 잠깐 느리게 보여 줍니다.
                m_ImpactTime += Time.unscaledDeltaTime * rate; // 이 연출의 진행도만 늦추고 XR 입력은 정상 속도로 유지합니다.
                yield return null; // 다음 프레임의 관절 적용을 기다립니다.
            } // 마지막 휘청인 자세는 원인 설명을 읽는 동안 유지합니다.
        } // 관찰 지점 복원 전까지 위치를 앞으로 보내지 않습니다.

        public IEnumerator PlaySafe(Transform viewer, Collider pole, System.Action onThank) // 실제 기둥을 피해 멈춘 뒤 감사 동작과 대사를 같은 시점에 시작합니다.
        { // 기존 이동 정지와 휴대전화 내리기를 우선 재사용합니다.
            m_ImpactPlaying = false; Freeze(false); // 관찰 정지를 해제하고 기존 대기 애니메이션을 다시 평가합니다.
            m_Phone.SetScenarioPhoneReleased(true); m_SafePhase = SafePhase.Notice; // 고개와 전화가 내려오며 위험을 확인한 뒤에는 화면을 다시 보지 않게 합니다.
            m_Movement.TryStop(pole.transform); // 위험 지점 전에 발걸음을 멈추고 실제 전봇대를 바라봅니다.
            m_SafePlaying = true; m_SafeTime = 0f; // 놀람에서 감사로 이어지는 연출을 시작합니다.
            while (m_SafeTime < 1.15f) // 먼저 전화에서 고개를 들고 전봇대를 발견하는 동작만 보여 줍니다.
            { // 같은 경고 선택이 연출을 재시작하지 않게 상태 제어기에서 입력을 잠급니다.
                m_SafeTime += Time.unscaledDeltaTime; // 놀람의 길이를 실제 경과 시간으로 유지합니다.
                yield return null; // 기존 Animator와 전화 내리기 보간을 계속 적용합니다.
            } // 우회 중에는 별도의 손짓을 덮어쓰지 않고 기존 걷기의 양팔을 사용합니다.
            m_SafePlaying = false; BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); m_SafePhase = SafePhase.Avoidance; // 위험을 확인했으면 앞을 보며 실제 인도 동선을 걷습니다.
            m_Movement.SetScenarioApproachSpeed(m_SafeWalkSpeed); // 기존 보폭 동기화 걷기에 안전한 우회 속도를 전달합니다.
            if (m_SafeWaypoints == null || m_SafeWaypoints.Length != 2) { m_SafePhase = SafePhase.None; yield break; } // 옆으로 비키기와 기둥 지나기 두 지점이 연결된 경로만 시작합니다.
            Vector3 segmentStart = transform.position; float distance = 0f; // 실제 우회 거리로 완료 대기 상한을 계산합니다.
            foreach (Transform waypoint in m_SafeWaypoints) // 두 구간 모두 실제 기둥과의 여유를 먼저 확인합니다.
            { // 첫 구간을 걷고 난 뒤 경로가 거절되는 일을 방지합니다.
                if (waypoint == null || !HasPoleClearance(segmentStart, waypoint.position, pole)) { m_SafePhase = SafePhase.None; yield break; } // 안전 여유가 없는 경로는 관찰 복원으로 돌려보냅니다.
                distance += Vector3.Distance(segmentStart, waypoint.position); segmentStart = waypoint.position; // 다음 구간도 앞 경유점을 기준으로 판단합니다.
            } // 기둥을 통과하는 우회는 수락하지 않습니다.
            if (!m_Movement.TryBeginScenarioDetour(m_SafeWaypoints)) { m_SafePhase = SafePhase.None; yield break; } // 전체 경로를 하나의 보행으로 시작합니다.
            float deadline = Time.unscaledTime + distance / Mathf.Max(0.3f, m_SafeWalkSpeed) * 2f + 2f; // 가속·감속을 포함한 실제 거리 기반 대기 상한입니다.
            while (m_Movement.IsScenarioDetouring && Time.unscaledTime < deadline) yield return null; // 정상 보행은 실제 마지막 도착으로 끝냅니다.
            if (m_Movement.IsScenarioDetouring || Vector3.Distance(transform.position, m_SafeWaypoints[m_SafeWaypoints.Length - 1].position) > 0.08f) { m_SafePhase = SafePhase.None; yield break; } // 실패를 감사·완료로 건너뛰지 않고 기존 관찰로 복원합니다.
            m_SafePhase = SafePhase.Turning; // 멈춘 자리에서 몸 방향만 플레이어 쪽으로 돌려 감사로 연결합니다.
            Vector3 direction = Vector3.ProjectOnPlane(viewer.position - transform.position, Vector3.up); // 실제 관찰자 방향에서 머리 높이를 제외합니다.
            if (direction.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, direction) > 5f) // 실제로 돌아설 각도가 있을 때만 제자리 발 디딤을 시작합니다.
            { // 루트 방향은 기존 이동기가 맡고 이 연출은 발 접지와 관절 움직임을 연결합니다.
                BeginTurnSteps(direction); float turnDeadline = Time.unscaledTime + Vector3.Angle(transform.forward, direction) / m_Movement.ScenarioTurnSpeed + 0.6f; // 실제 몸 회전 속도에 맞는 발 디딤 완료 상한입니다. // 현재 양발의 접지 위치에서 자연스럽게 첫 발을 떼게 합니다.
                while (direction.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, direction) > 5f && Time.unscaledTime < turnDeadline) // 플레이어를 향하는 동안 양발을 번갈아 옮깁니다.
                { // 카메라와 캐릭터 위치는 움직이지 않습니다.
                    m_TurnLookDirection = direction; m_Movement.FaceScenarioTarget(viewer); // 몸의 기존 회전 목표와 머리가 먼저 바라볼 방향을 함께 갱신합니다.
                    yield return null; // 기존 이동기의 루트 회전과 이번 프레임의 발 접지를 기다립니다.
                    direction = Vector3.ProjectOnPlane(viewer.position - transform.position, Vector3.up); // 실제 플레이어 위치를 기준으로 남은 방향 차이를 구합니다.
                } // 몸이 돌아선 순간에도 들고 있는 발을 바로 대기 자세로 바꾸지 않습니다.
                if (Vector3.Angle(transform.forward, direction) > 5f) { m_TurnPlaying = false; m_SafePhase = SafePhase.None; yield break; } // 회전 실패를 성공으로 기록하지 않습니다.
                m_TurnSettleSteps = 1; // 몸이 돌아선 뒤 다음 한 번의 착지만 마치고 감사로 이어갑니다. // 현재 들고 있는 발과 반대 발을 순서대로 새 정면에 내려놓습니다.
                float settleDeadline = Time.unscaledTime + m_TurnStepDuration + 0.1f; // 마지막 착지 대기가 무기한 남지 않게 합니다.
                while (!m_TurnFeetSettled && Time.unscaledTime < settleDeadline) yield return null; // 양발 디딤 두 번을 추가로 반복하지 않습니다. // 두 발이 모두 내려오기 전에는 감사 손짓으로 넘어가지 않습니다.
                m_TurnPlaying = false; // 접지가 끝난 뒤 기존 대기 자세와 감사 연출로 제어를 넘깁니다.
            } // 기존 얼굴의 미소가 플레이어에게 보이는 구도에서 인사합니다.
            m_SafePhase = SafePhase.Thanks; m_SafePlaying = true; m_SafeTime = 1.2f; // 감사 말풍선과 기존 오른팔 손짓을 같은 시점에서 시작합니다.
            BlenderRigPose.PlayFace(m_Head, 1.8f, 4.2f); // 감사할 때 입과 눈을 움직이고 안전 완료 화면까지 웃는 표정을 이어갑니다.
            onThank?.Invoke(); // 감사 단계에 도달한 바로 이 지점에서 대사를 표시하고 보행 소리를 멈추도록 알립니다.
            while (m_SafeTime < 4f) { m_SafeTime += Time.unscaledDeltaTime; yield return null; } // 미소·끄덕임·손 흔들기를 충분히 보여 줍니다.
            m_SafePlaying = false; BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); // 다음 프레임부터 기존 대기 애니메이션에 제어를 돌려줍니다.
            m_SafePhase = SafePhase.Finished; // 실제 회피와 감사가 모두 끝난 경우에만 완료 상태를 제공합니다.
        } // 두 번째 상황을 시작하거나 친구 2에게 명령하지 않습니다.

        private bool HasPoleClearance(Vector3 start, Vector3 destination, Collider pole) // 현재 위치에서 다음 우회 지점까지 기둥과 충분한 간격을 유지하는지 계산합니다.
        { // 도착점이 떨어져 있어도 중간 경로가 기둥을 통과하는 경우에는 걷기를 시작하지 않습니다.
            Vector3 path = Vector3.ProjectOnPlane(destination - start, Vector3.up); // 현재 구간의 출발점에서 기둥까지 실제 최단 거리를 계산합니다. // 기존 이동기가 사용하는 같은 수평 보행 구간을 구합니다.
            if (path.sqrMagnitude <= 0.0001f) return false; // 길이가 없는 구간에서는 가장 가까운 지점의 비율을 계산하지 않습니다.
            float fraction = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(pole.bounds.center - start, Vector3.up), path) / path.sqrMagnitude); // 기둥 중심에서 이 보행 선분에 가장 가까운 위치를 구합니다.
            Vector3 nearest = start + path * fraction; nearest.y = m_Head.position.y; // 몸이 지나는 높이에서 실제 기둥 표면과의 간격을 계산합니다.
            Vector3 separation = Vector3.ProjectOnPlane(nearest - pole.ClosestPoint(nearest), Vector3.up); // 프리팹의 실제 크기를 반영하여 기둥 표면 바깥의 수평 여유 거리를 구합니다.
            return separation.sqrMagnitude >= m_PoleClearance * m_PoleClearance; // 친구의 몸과 팔이 붙지 않는 여유가 있는 구간만 기존 보행기에 전달합니다.
        } // 친구 2의 횡단 경로나 자동차·신호 상태에는 관여하지 않습니다.

        private void LateUpdate() // 기존 전화 자세 뒤에 첫 번째 상황의 관절 연출만 덧붙입니다.
        { // 연출이 없는 관찰·보행에는 뼈대에 추가 대입하지 않습니다.
            if (m_ImpactPlaying) // 실제 충돌을 받은 뒤의 반동 자세입니다.
            { // 기준 자세부터 다시 계산하여 팔·상체 회전이 누적되지 않게 합니다.
                LoadPose(m_ImpactPositions, m_ImpactRotations, m_ImpactScales); // 충돌 직전의 기존 관절 자세를 복원합니다.
                float recoil = Mathf.Sin(Mathf.Clamp01(m_ImpactTime / 0.72f) * Mathf.PI * 0.5f); // 접촉 직후 빠르게 뒤로 젖혀지는 반동을 계산합니다.
                float recover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.1f, 2.2f, m_ImpactTime)); // 크게 휘청인 뒤에도 약간 뒤로 젖힌 자세를 남깁니다.
                float lean = recoil * (1f - recover * 0.65f); // 넘어지려는 최대 자세와 부분 회복을 연결합니다.
                float sway = Mathf.Sin(m_ImpactTime * 16f) * Mathf.Exp(-m_ImpactTime * 1.4f); // 팔과 몸의 흔들림은 충격 뒤 자연스럽게 잦아듭니다.
                transform.position = m_ImpactRootPosition + m_Backward * (0.42f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_ImpactTime / 1.3f))); // 한 걸음 뒤로 물러나며 전봇대와 다시 겹치지 않게 합니다.
                m_Hips.localPosition += Vector3.down * (0.06f * lean); // 무게중심을 낮춰 다리로 버티는 모습을 만듭니다.
                Rotate(m_Hips, -16f * lean, 0f, 5f * sway); Rotate(m_Spine, -22f * lean, 0f, 0f); // 골반과 허리를 서로 다른 크기로 뒤로 젖힙니다.
                Rotate(m_Chest, -25f * lean, 0f, -6f * sway); Rotate(m_Head, -24f * lean, 9f * sway, 0f); // 상체에 이어 머리도 뒤로 젖혀 충돌 반동을 분명히 보여 줍니다.
                Rotate(m_LeftArm, -52f * lean, 0f, -60f * lean + 20f * sway); Rotate(m_LeftForearm, 0f, -30f * lean, 22f * sway); // 전화가 붙은 왼팔이 충격으로 벌어지고 흔들리게 합니다.
                Rotate(m_RightArm, -60f * lean, 0f, 68f * lean + 22f * sway); Rotate(m_RightForearm, 0f, 0f, -45f * lean); // 반대 팔도 균형을 잡으려 들리게 합니다.
                Rotate(m_LeftLeg, -28f * lean, 0f, -5f * lean); Rotate(m_LeftShin, 48f * lean, 0f, 0f); // 한쪽 다리를 뒤로 빼고 무릎을 접어 크게 휘청이게 합니다.
                Rotate(m_RightLeg, 22f * lean, 0f, 6f * lean); Rotate(m_RightShin, 30f * lean, 0f, 0f); // 다른 다리로 지지하려는 비대칭 동작을 연결합니다.
                float lowest = float.PositiveInfinity; // 바닥보다 내려간 밑창이 있는지 계산할 기준입니다.
                foreach (Transform foot in m_Feet) lowest = Mathf.Min(lowest, foot.TransformPoint(BlenderRigPose.ModelOffset(foot, new Vector3(0f, -0.0782f, 0f))).y); // 기존 발목에서 밑창까지의 실제 오프셋을 사용합니다.
                if (lowest < transform.position.y) m_VisualRoot.position += Vector3.up * (transform.position.y - lowest); // 최소한 한쪽 밑창이 바닥을 뚫지 않도록 시각 모델 높이를 보정합니다.
                BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, true); // 충돌 동안 둥글게 벌어진 입을 표시합니다.
            } // 충돌 중에는 감사 자세를 함께 적용하지 않습니다.
            else if (m_TurnPlaying) ApplyTurnSteps(); // 돌아서는 동안에는 지지발을 고정하고 반대 발을 들어 새 방향에 내려놓습니다.
            else if (m_SafePlaying) // 경고 뒤의 놀람과 감사 손짓입니다.
            { // 기존 정지·휴대전화 해제와 함께 얼굴과 오른손만 조절합니다.
                BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, m_SafeTime > 0.2f && m_SafeTime < 1.15f); // 앞의 위험을 깨달은 짧은 놀람 후 기존 미소로 돌아갑니다.
                if (m_SafeTime > 1.2f) // 팔과 고개가 먼저 올라온 뒤 감사 동작을 보여 줍니다.
                { // 손짓의 앞뒤를 부드럽게 섞습니다.
                    float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min((m_SafeTime - 1.2f) / 0.35f, (4f - m_SafeTime) / 0.45f)); // 손을 들고 내리는 양끝의 급격한 회전을 줄입니다.
                    m_RightArm.localRotation = Quaternion.Slerp(m_RightArm.localRotation, BlenderRigPose.LocalRotation(m_RightArm, Quaternion.Euler(-18f, 0f, -30f)), blend); // 오른손을 보기 편한 높이로 들어 올립니다.
                    m_RightForearm.localRotation = Quaternion.Slerp(m_RightForearm.localRotation, BlenderRigPose.LocalRotation(m_RightForearm, Quaternion.Euler(0f, 18f * Mathf.Sin(m_SafeTime * 11f), 112f)), blend); // 팔꿈치를 접고 손을 좌우로 흔듭니다.
                    Rotate(m_Head, 9f * blend * Mathf.Sin((m_SafeTime - 1.2f) * 5f), 0f, 0f); // 미소와 함께 짧게 고개를 끄덕입니다.
                } // 감사 동작 외의 다리와 몸통은 기존 대기 애니메이션을 유지합니다.
            } // 연출하지 않는 동안에는 원래 얼굴과 몸을 그대로 사용합니다.
        } // XR 카메라의 위치·회전·시야각을 변경하지 않습니다.

        private void BeginTurnSteps(Vector3 direction) // 돌아서기 직전의 실제 양발 위치를 접지 기준으로 준비합니다.
        { // 기존 보행이 끝난 지점에서 시작하며 루트 위치는 변경하지 않습니다.
            for (int i = 0; i < 2; ++i) { m_TurnFootPositions[i] = m_Feet[i].position; m_TurnFootRotations[i] = m_Feet[i].rotation; } // 왼발과 오른발의 발목 위치·방향을 각각 보관합니다.
            m_TurnSwingFoot = Vector3.SignedAngle(transform.forward, direction, Vector3.up) >= 0f ? 1 : 0; // 돌아서는 방향의 발을 먼저 들어 몸이 꼬이지 않게 합니다.
            m_TurnLookDirection = direction; m_TurnStepTime = 0f; m_TurnSettleSteps = 0; // 첫 발의 진행도와 마무리 상태를 초기화합니다.
            m_TurnFeetSettled = false; m_TurnPlaying = true; // 다음 LateUpdate부터 기존 골격에 제자리 발 디딤만 적용합니다.
        } // 기존 Animator와 걷기 클립은 그대로 유지합니다.

        private void ApplyTurnSteps() // 대기 애니메이션 위에 발 접지·체중 이동·시선 선행을 연결합니다.
        { // 캐릭터 루트 회전은 기존 FriendCharacterMovement가 한 번만 적용합니다.
            if (m_TurnFeetSettled) return; // 마무리 디딤이 끝난 프레임에는 새 발걸음을 시작하지 않습니다.
            m_TurnStepTime = Mathf.Min(m_TurnStepDuration, m_TurnStepTime + Time.unscaledDeltaTime); // 상황 연출의 한 발 디딤을 실제 경과 시간으로 진행합니다.
            float phase = Mathf.Clamp01(m_TurnStepTime / Mathf.Max(0.22f, m_TurnStepDuration)), blend = Mathf.SmoothStep(0f, 1f, phase); // 발을 떼고 내려놓는 양끝의 움직임을 부드럽게 만듭니다.
            float lift = Mathf.Sin(phase * Mathf.PI); // 디딤 중간에서 발을 가장 높게 들고 접지 시점에는 높이를 0으로 만듭니다.
            m_Hips.localPosition += Vector3.right * ((m_TurnSwingFoot == 0 ? 1f : -1f) * 0.014f * lift); // 발을 든 쪽의 반대 다리에 체중을 조금 옮깁니다.
            float lookAngle = Mathf.Clamp(Vector3.SignedAngle(transform.forward, m_TurnLookDirection, Vector3.up), -45f, 45f); // 몸보다 먼저 플레이어를 볼 머리·가슴의 추가 회전 범위를 제한합니다.
            Rotate(m_Chest, 0f, lookAngle * 0.18f, 0f); Rotate(m_Head, 0f, lookAngle * 0.65f, 0f); // 머리와 어깨가 먼저 돌아가고 몸과 발이 뒤따르는 동작을 만듭니다.
            Vector3 swingPosition = Vector3.Lerp(m_TurnFootPositions[m_TurnSwingFoot], m_Feet[m_TurnSwingFoot].position, blend) + Vector3.up * (m_TurnFootLift * lift); // 옮기는 발만 기존 디딤 위치에서 새 정면의 대기 위치로 낮은 호를 그리며 이동합니다.
            Quaternion swingRotation = Quaternion.Slerp(m_TurnFootRotations[m_TurnSwingFoot], m_Feet[m_TurnSwingFoot].rotation, blend); // 발을 들어 옮기는 동안에만 발끝 방향을 새 몸 방향에 맞춥니다.
            for (int i = 0; i < 2; ++i) // 양다리의 길이는 유지하면서 한 발은 접지하고 다른 발만 옮깁니다.
            { // 왼발과 오른발은 씬에 연결된 기존 관절을 사용합니다.
                Vector3 position = i == m_TurnSwingFoot ? swingPosition : m_TurnFootPositions[i]; // 지지발의 월드 위치는 캐릭터 회전 중에도 유지합니다.
                Quaternion rotation = i == m_TurnSwingFoot ? swingRotation : m_TurnFootRotations[i]; // 지지발이 바닥에서 함께 회전하며 미끄러지지 않게 합니다.
                PlaceTurnFoot(i == 0 ? m_LeftLeg : m_RightLeg, i == 0 ? m_LeftShin : m_RightShin, m_Feet[i], position, rotation); // 허벅지와 무릎으로 발의 접지 위치를 맞춥니다.
            } // 루트는 그대로이며 다리 관절만 움직입니다.
            if (phase < 1f) return; // 발이 내려오기 전에는 반대 발의 디딤을 시작하지 않습니다.
            m_TurnFootPositions[m_TurnSwingFoot] = m_Feet[m_TurnSwingFoot].position; m_TurnFootRotations[m_TurnSwingFoot] = m_Feet[m_TurnSwingFoot].rotation; // 실제로 내려놓은 위치와 방향을 다음 지지발 기준으로 저장합니다.
            if (m_TurnSettleSteps > 0) { --m_TurnSettleSteps; m_TurnFeetSettled = m_TurnSettleSteps == 0; } // 몸이 정면을 향한 뒤에는 두 발을 내려놓고 마무리합니다.
            m_TurnSwingFoot = 1 - m_TurnSwingFoot; m_TurnStepTime = 0f; // 다음 디딤에서는 반대쪽 발을 들어 자연스럽게 번갈아 돌아섭니다.
        } // 양발 접지가 끝난 다음 감사 손짓과 대사가 시작됩니다.

        private void PlaceTurnFoot(Transform thigh, Transform shin, Transform foot, Vector3 target, Quaternion rotation) // 기존 두 다리 관절의 회전만으로 발을 원하는 위치에 놓습니다.
        { // 관절 길이나 발바닥 높이를 늘리거나 줄이지 않습니다.
            Vector3 hip = thigh.position, offset = target - hip; // 골반 쪽 허벅지 시작점에서 목표 발목까지의 방향을 구합니다.
            float upperLength = Vector3.Distance(hip, shin.position), lowerLength = Vector3.Distance(shin.position, foot.position); // 현재 캐릭터의 실제 허벅지·종아리 길이를 사용합니다.
            float distance = Mathf.Clamp(offset.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.001f, upperLength + lowerLength - 0.001f); // 무릎이 반대로 꺾이거나 다리가 늘어나는 범위를 제외합니다.
            Vector3 direction = offset.sqrMagnitude > 0.000001f ? offset.normalized : Vector3.down; // 목표 발목 방향을 정규화하되 길이가 없는 경우 아래쪽을 사용합니다.
            Vector3 bend = Vector3.ProjectOnPlane(transform.forward, direction).normalized; // 무릎은 새 몸 방향의 앞쪽으로 굽히며 뒤집히지 않게 합니다.
            float along = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2f * distance); // 두 다리 길이를 유지하는 무릎의 전진 거리를 코사인 법칙으로 구합니다.
            Vector3 knee = hip + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along)); // 접지 발목과 현재 골반을 연결하는 실제 무릎 위치를 구합니다.
            thigh.rotation = Quaternion.FromToRotation(shin.position - hip, knee - hip) * thigh.rotation; // 허벅지를 먼저 돌려 무릎이 계산한 위치로 오게 합니다.
            shin.rotation = Quaternion.FromToRotation(foot.position - shin.position, hip + direction * distance - shin.position) * shin.rotation; // 종아리를 돌려 발목을 접지 또는 들어 올린 목표에 맞춥니다.
            foot.rotation = rotation; // 발바닥 방향은 지지 또는 디딤 상태에서 정한 월드 방향을 유지합니다.
        } // 본의 위치·크기와 기존 스킨 연결은 변경하지 않습니다.

        private static void Rotate(Transform bone, float x, float y, float z) // 기존 자세 위에 관절별 반동을 더합니다.
        { // 모든 회전은 모델의 기존 로컬 축을 사용합니다.
            bone.localRotation *= BlenderRigPose.DeltaRotation(bone, Quaternion.Euler(x, y, z)); // 캐릭터 전체를 하나의 덩어리로 기울이지 않습니다.
        } // 관절의 위치와 골격 연결은 유지합니다.

        private void SavePose(Vector3[] positions, Quaternion[] rotations, Vector3[] scales) // 기존 계층의 현재 자세를 저장합니다.
        { // 전화와 얼굴 표정 UI의 RectTransform도 동일한 계층에 포함됩니다.
            for (int i = 0; i < m_Bones.Length; ++i) { positions[i] = m_Bones[i].localPosition; rotations[i] = m_Bones[i].localRotation; scales[i] = m_Bones[i].localScale; } // 배열 인덱스를 유지하여 같은 오브젝트로 복원합니다.
        } // 새 메시나 별도 모델 복사본을 만들지 않습니다.

        private void LoadPose(Vector3[] positions, Quaternion[] rotations, Vector3[] scales) // 저장된 각 관절과 소품의 위치·회전·크기를 되돌립니다.
        { // Animator를 끈 충격과 관찰 시점에만 전체 자세 복원을 사용합니다.
            for (int i = 0; i < m_Bones.Length; ++i) { m_Bones[i].localPosition = positions[i]; m_Bones[i].localRotation = rotations[i]; m_Bones[i].localScale = scales[i]; } // 같은 프레임의 상대 자세를 정확히 복원합니다.
        } // 캐릭터 루트의 경로 진행도는 기존 이동 컴포넌트가 복원합니다.
    } // 첫 번째 친구의 골격 연출과 체크포인트 복원을 마칩니다.
} // 기존 두 번째·세 번째 상황의 코드에는 연결하지 않습니다.
