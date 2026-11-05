using UnityEngine; // 기존 관절 회전과 휴대전화 사용 자세의 부드러운 전환을 계산합니다.

namespace Y.Crosswalk // 학교 앞 횡단보도의 친구 연출을 개인 작업 영역에 둡니다.
{ // 휴대전화 자세를 이동과 VR 입력 처리에서 분리합니다.
    [DisallowMultipleComponent] // 같은 친구의 왼팔과 고개에 휴대전화 자세가 중복 적용되지 않게 합니다.
    [RequireComponent(typeof(FriendCharacterMovement))] // 기존 이동 상태를 읽어 보행 중에만 휴대전화 사용 자세를 적용합니다.
    [DefaultExecutionOrder(100)] // Animator 평가와 기존 이동·오른팔 멈춤 반응 뒤에 휴대전화 관절 자세를 적용합니다.
    public sealed class FriendPhoneUsage : MonoBehaviour // 기존 걷기를 유지하면서 왼손 휴대전화와 시선 자세만 담당합니다.
    { // 현재 친구 모델의 관절과 자세 전환에 필요한 값만 보관합니다.
        private static readonly Quaternion k_HoldingUpperArm = Quaternion.Euler(-28f, 0f, 78f); // 왼쪽 위팔을 몸 옆에 두고 팔꿈치를 앞쪽으로 조금 내밉니다.
        private static readonly Quaternion k_HoldingLowerArm = Quaternion.Euler(0f, 108f, 17f); // 이 모델의 왼팔 축에 맞춰 팔꿈치를 접어 휴대전화를 가슴 앞에 올립니다.

        [Header("기존 왼팔 관절")] // 오른팔의 기존 멈춤 응답과 겹치지 않는 왼팔만 지정합니다.
        [SerializeField, InspectorName("왼쪽 위팔")] // 기존 LeftUpperArm 관절을 연결합니다.
        [Tooltip("친구 VisualRoot 아래 LeftUpperArm입니다. 기존 관절 위치와 신체 길이를 유지하면서 회전만 조절합니다.")] // 몸을 바꾸지 않고 팔을 드는 기준임을 설명합니다.
        private Transform m_LeftUpperArm; // 어깨에서 팔꿈치로 이어지는 기존 위팔입니다.

        [SerializeField, InspectorName("왼쪽 아래팔")] // 기존 LeftLowerArm 관절을 연결합니다.
        [Tooltip("LeftUpperArm 아래 LeftLowerArm입니다. 팔꿈치를 접어 손과 휴대전화를 몸 앞쪽에 둡니다.")] // 휴대전화가 몸통 안으로 들어가지 않는 자세의 역할을 안내합니다.
        private Transform m_LeftLowerArm; // 휴대전화가 달린 손을 들어 올리는 기존 아래팔입니다.

        [SerializeField, InspectorName("왼손")] // 휴대전화 소품을 자식으로 둔 기존 LeftHand를 연결합니다.
        [Tooltip("기존 LeftHand입니다. 휴대전화는 이 손의 자식으로 고정되어 걷기와 자세 전환 중 손을 그대로 따라갑니다.")] // 소품 위치를 매 프레임 따로 추적하지 않는 연결 방식을 설명합니다.
        private Transform m_LeftHand; // 손목 회전을 맞춰 손바닥에 붙인 화면이 얼굴 쪽을 향하게 합니다.

        [SerializeField, InspectorName("화면을 조작하는 엄지"), Tooltip("선택 항목입니다. 친구 1의 LeftThumbProximal을 연결하면 휴대전화 자세 위에 작은 터치 움직임을 더합니다.")] // 기존 골격의 손가락을 가까운 장면에서도 관찰할 수 있게 합니다.
        private Transform m_TappingThumb; // 휴대전화 소품을 재부착하지 않고 기존 엄지 관절만 움직입니다.
        private Quaternion m_ThumbRest; // 엄지에 터치 회전이 프레임마다 누적되지 않게 기본 회전을 보관합니다.
        private float m_TapTime; // 이 친구의 전화 자세가 진행되는 동안만 터치 시간을 누적합니다.

        [Header("기존 상체와 시선")] // 걷기의 하체와 호흡을 유지하면서 휴대전화에 집중하는 상체만 조절합니다.
        [SerializeField, InspectorName("윗가슴")] // 기존 UpperChest 관절을 연결합니다.
        [Tooltip("기존 UpperChest입니다. Animator의 보행 흔들림 위에 작은 앞으로 숙임을 더합니다.")] // 기존 걷기와 상체 자세를 조합하는 역할을 설명합니다.
        private Transform m_UpperChest; // 상체를 조금 기울여 휴대전화에 주의를 기울이는 모습을 만듭니다.

        [SerializeField, InspectorName("목")] // 기존 Neck 관절을 연결합니다.
        [Tooltip("기존 Neck입니다. 고개를 숙이는 동작을 목과 머리에 나눠 한 관절만 과하게 꺾이지 않게 합니다.")] // 작은 목 숙임으로 머리의 시선 회전을 보조합니다.
        private Transform m_Neck; // 기존 목 자세에 휴대전화 쪽 기울임을 더합니다.

        [SerializeField, InspectorName("머리")] // 기존 Head 관절을 연결합니다.
        [Tooltip("기존 Head입니다. 휴대전화 화면의 실제 위치를 바라보며 정지하면 기존 대기 자세로 돌아갑니다.")] // 이동 컴포넌트의 플레이어 방향 회전과 연결되는 시선 동작을 설명합니다.
        private Transform m_Head; // 모델의 기존 정면 축을 사용해 화면 쪽 시선을 계산합니다.

        [SerializeField, InspectorName("휴대전화 화면")] // 왼손에 붙인 소품의 화면 Transform을 연결합니다.
        [Tooltip("왼손 아래 휴대전화 화면입니다. 팔과 상체의 이번 프레임 회전이 적용된 화면 위치를 머리의 시선 목표로 사용합니다.")] // 소품을 향한 시선이 보행 중에도 유지되는 기준을 안내합니다.
        private Transform m_PhoneScreen; // 걷기와 손의 움직임을 따라 변하는 화면 중심입니다.

        [Header("사용 자세 전환")] // 휴대전화를 들고 내리는 속도만 조절합니다.
        [SerializeField, InspectorName("출발 대기 중 휴대전화 보기")] // 실제 보행을 시작하지 않아도 친구 1의 초기 휴대전화 자세를 표시할 수 있게 합니다.
        [Tooltip("친구 1에서 켜면 출발 전 대기에도 기존 왼팔과 고개 자세를 사용합니다. 멈춤 명령이나 횡단 이후에는 해제되며 다른 친구의 설정에는 영향을 주지 않습니다.")] // 초기 자세가 이동 시작이나 새 상황 진행을 유발하지 않음을 설명합니다.
        private bool m_UsePhoneWhileWaiting; // 기본값은 기존 보행 중 전용 연출을 유지하고 씬 인스턴스에서만 대기 자세를 선택합니다.

        [SerializeField, Min(0.1f), InspectorName("자세 전환 시간(초)")] // 보행 시작과 정지 후 상체 자세가 바뀌는 시간을 지정합니다.
        [Tooltip("기본값 0.5초입니다. 멈춤 신호로 이동은 즉시 중단되며 왼팔과 고개는 이 시간에 걸쳐 자연스럽게 대기 자세로 돌아갑니다.")] // 이동 정지와 시각적인 자세 전환의 시간을 구분합니다.
        private float m_PoseTransitionTime = 0.5f; // 갑자기 팔이 내려가거나 고개가 튀지 않도록 전환 시간을 확보합니다.

        private FriendCharacterMovement m_Movement; // 보행·명령 정지·정상 도착을 결정하는 기존 컴포넌트입니다.
        private bool m_ScenarioPaused; // 관찰과 충돌의 관절 연출이 진행될 때 기존 휴대전화 자세를 덮어쓰지 않게 합니다.
        private bool m_ScenarioPhoneReleased; // 안전 경고 이후에는 우회 보행을 시작해도 다시 전화 화면을 보지 않게 합니다.
        private float m_CheckpointPoseWeight, m_CheckpointTapTime; // 재시도할 관찰 시점의 전화 자세와 손가락 동작 시간을 보관합니다.
        public void SetScenarioPhoneReleased(bool released) { m_ScenarioPhoneReleased = released; } // 안전한 경고에서 전화 사용만 중단하고 기존 내리기 보간은 계속 사용합니다.
        public void CaptureScenarioCheckpoint() { m_CheckpointPoseWeight = m_PoseWeight; m_CheckpointTapTime = m_TapTime; } // 실제 관찰 자세와 같은 가중치·엄지 동작 진행도를 저장합니다.
        public void RestoreScenarioCheckpoint() { m_ScenarioPhoneReleased = false; m_PoseWeight = m_CheckpointPoseWeight; m_TapTime = m_CheckpointTapTime; m_HasAppliedPose = false; } // 취소·재시도 시 안전 우회의 전화 해제 상태를 제거합니다.

        public void SetScenarioPaused(bool paused) // 첫 번째 상황에서만 이 친구의 휴대전화 자세 계산을 개별 정지합니다.
        { // 자세 가중치를 지우거나 소품을 분리하지 않아 같은 보행 자세로 재개할 수 있습니다.
            m_ScenarioPaused = paused; // 다음 LateUpdate부터 관찰 또는 사고 연출의 자세 소유권을 존중합니다.
        } // 다른 친구의 휴대전화 상태에는 영향을 주지 않습니다.
        private float m_PoseWeight; // 휴대전화 자세가 적용되는 정도를 0에서 1 사이로 유지합니다.
        private bool m_HasAppliedPose; // 비활성화할 때 직접 적용한 관절 자세를 되돌릴 필요가 있는지 보관합니다.
        private Quaternion m_AnimatedUpperChest; // 이번 프레임 Animator가 만든 상체 자세를 보관합니다.
        private Quaternion m_AnimatedNeck; // 휴대전화 기울임을 더하기 전 목 자세입니다.
        private Quaternion m_AnimatedHead; // 화면을 바라보기 전 기존 애니메이션의 머리 자세입니다.
        private Quaternion m_AnimatedUpperArm; // 휴대전화 들기와 섞을 기존 왼쪽 위팔의 보행 자세입니다.
        private Quaternion m_AnimatedLowerArm; // 멈출 때 복원할 기존 왼쪽 아래팔 자세입니다.
        private Quaternion m_AnimatedHand; // 손목을 과하게 돌리지 않도록 기준으로 삼는 기존 손 자세입니다.

        private void Awake() // 연출에 필요한 기존 이동 컴포넌트와 프리팹의 관절 연결을 준비합니다.
        { // 새로운 Animator나 이동 제어기를 만들지 않습니다.
            m_Movement = GetComponent<FriendCharacterMovement>(); // 이동 상태를 읽기만 하여 기존 정지와 자동 이동 흐름을 유지합니다.
            if (m_TappingThumb != null) m_ThumbRest = m_TappingThumb.localRotation; // 연결된 친구의 엄지 기본 자세만 저장합니다.
            if (m_LeftUpperArm == null || m_LeftLowerArm == null || m_LeftHand == null || m_UpperChest == null || m_Neck == null || m_Head == null || m_PhoneScreen == null) // 현재 모델에서 자세와 시선에 쓰는 참조가 모두 연결되었는지 확인합니다.
            { // 불완전한 참조로 일부 관절만 변형하는 상황을 막습니다.
                Debug.LogError("친구 휴대전화 연출에 왼팔·왼손·윗가슴·목·머리와 휴대전화 화면을 연결해 주세요.", this); // 프리팹에서 연결할 항목을 알려 줍니다.
                enabled = false; // 휴대전화 연출만 중단하고 기존 이동과 멈춤 신호는 유지합니다.
            } // 정상 연결된 친구는 다음 LateUpdate부터 보행 상태를 따라갑니다.
        } // 소품의 손 연결과 기본 위치는 프리팹에 저장된 값을 사용합니다.

        private void LateUpdate() // Animator가 걷기·정지·대기 자세를 평가한 뒤 상체에만 휴대전화 자세를 조합합니다.
        { // 기존 발 디딤·루트 이동·오른팔 응답은 그대로 유지합니다.
            if (m_ScenarioPaused) return; // 정지된 전화 사용 자세를 유지하고 사고 중에는 별도 관절 연출에 제어를 맡깁니다.
            float targetWeight = !m_ScenarioPhoneReleased && (m_Movement.IsApproaching || (m_UsePhoneWhileWaiting && m_Movement.IsWaitingForWalk)) ? 1f : 0f; // 위험 보행에는 기존 전화 자세를 유지하고 경고 뒤 우회에서는 앞을 보며 걷습니다.
            m_PoseWeight = Mathf.MoveTowards(m_PoseWeight, targetWeight, Time.deltaTime / Mathf.Max(0.1f, m_PoseTransitionTime)); // 반복 신호에 반응을 재시작하지 않고 현재 자세에서 목표로 이어 갑니다.
            if (m_PoseWeight <= 0f) // 시작 대기와 정지 후 전환이 끝난 상태에서는 Animator가 관절을 전담합니다.
            { // 매 프레임 새로 평가되는 기본 자세에 불필요한 회전을 더하지 않습니다.
                if (m_TappingThumb != null) m_TappingThumb.localRotation = m_ThumbRest; // 휴대전화를 내린 뒤 엄지의 마지막 터치 자세도 완전히 해제합니다.
                m_HasAppliedPose = false; // 직접 적용한 휴대전화 자세가 남아 있지 않음을 기록합니다.
                return; // 대기 중에는 휴대전화 소품만 손의 움직임을 따라가게 둡니다.
            } // 보행 시작 또는 정지 전환 중에만 기존 자세를 저장하고 섞습니다.
            m_AnimatedUpperChest = m_UpperChest.localRotation; // 현재 보행의 가슴 회전을 기준으로 삼아 몸통 흔들림을 유지합니다.
            m_AnimatedNeck = m_Neck.localRotation; // 이번 프레임의 원래 목 자세를 보관하여 이전 기울임이 누적되지 않게 합니다.
            m_AnimatedHead = m_Head.localRotation; // 화면을 향한 회전을 기존 머리 애니메이션과 섞을 기준입니다.
            m_AnimatedUpperArm = m_LeftUpperArm.localRotation; // 기존 걷기의 왼쪽 위팔 자세를 전환의 출발점으로 사용합니다.
            m_AnimatedLowerArm = m_LeftLowerArm.localRotation; // 팔을 내릴 때 기존 대기·정지 자세로 이어질 기준을 보관합니다.
            m_AnimatedHand = m_LeftHand.localRotation; // 손목 자세도 같은 전환 비율로 풀기 위해 보관합니다.
            m_HasAppliedPose = true; // 연출이 비활성화되면 이번 프레임의 원래 관절 자세를 복원하게 합니다.
            float blend = Mathf.SmoothStep(0f, 1f, m_PoseWeight); // 들기와 내리기의 시작·끝을 부드럽게 만듭니다.
            m_TapTime += Time.deltaTime; // 상황 정지 중에는 이 메서드를 건너뛰므로 손가락도 같은 자세로 멈춥니다.
            if (m_TappingThumb != null) m_TappingThumb.localRotation = m_ThumbRest * BlenderRigPose.DeltaRotation(m_TappingThumb, Quaternion.Euler(0f, 15f * blend * Mathf.Sin(m_TapTime * 5.5f), 8f * blend * Mathf.Sin(m_TapTime * 5.5f))); // 손바닥과 전화 위치를 유지한 채 엄지로 작게 화면을 조작합니다.
            m_UpperChest.localRotation = m_AnimatedUpperChest * BlenderRigPose.DeltaRotation(m_UpperChest, Quaternion.Euler(5f * blend, 0f, 0f)); // 다리의 보행을 건드리지 않고 상체만 조금 앞으로 기울입니다.
            m_Neck.localRotation = m_AnimatedNeck * BlenderRigPose.DeltaRotation(m_Neck, Quaternion.Euler(8f * blend, -4f * blend, 0f)); // 왼손의 화면 쪽으로 목을 조금 숙여 집중하는 모습을 만듭니다.
            m_LeftUpperArm.localRotation = Quaternion.Slerp(m_AnimatedUpperArm, BlenderRigPose.LocalRotation(m_LeftUpperArm, k_HoldingUpperArm), blend); // 팔꿈치를 몸 앞에 두되 모델의 어깨 위치와 팔 길이는 보존합니다.
            m_LeftLowerArm.localRotation = Quaternion.Slerp(m_AnimatedLowerArm, BlenderRigPose.LocalRotation(m_LeftLowerArm, k_HoldingLowerArm), blend); // 팔꿈치를 접어 손바닥의 휴대전화를 가슴 앞에 들어 올립니다.
            m_LeftHand.localRotation = Quaternion.Slerp(m_AnimatedHand, BlenderRigPose.LocalRotation(m_LeftHand, Quaternion.identity), blend); // 손바닥에 맞춰 고정된 휴대전화의 화면 방향을 안정적으로 유지합니다.
            Vector3 localLookDirection = m_Head.parent.InverseTransformDirection(m_PhoneScreen.position - m_Head.position); // 변경된 상체와 팔을 따라 움직인 화면 위치를 머리 부모의 좌표로 변환합니다.
            if (localLookDirection.sqrMagnitude <= 0.0001f) return; // 시선 목표와 머리가 겹쳐 방향을 구할 수 없는 경우 기본 머리 자세를 유지합니다.
            float horizontalDistance = new Vector2(localLookDirection.x, localLookDirection.z).magnitude; // 위아래 숙임을 구하기 위한 수평 거리를 계산합니다.
            float pitch = Mathf.Clamp(-Mathf.Atan2(localLookDirection.y, horizontalDistance) * Mathf.Rad2Deg, -5f, 40f); // 화면 쪽으로 고개를 숙이되 목이 과하게 꺾이지 않게 제한합니다.
            float yaw = Mathf.Clamp(Mathf.Atan2(localLookDirection.x, localLookDirection.z) * Mathf.Rad2Deg, -40f, 40f); // 왼손 화면을 바라볼 만큼만 머리를 좌우로 돌립니다.
            m_Head.localRotation = Quaternion.Slerp(m_AnimatedHead, BlenderRigPose.LocalRotation(m_Head, Quaternion.Euler(pitch, yaw, 0f)), blend); // 정지하면 기존 대기 자세로 돌아가 이동 컴포넌트가 향한 플레이어 쪽을 바라봅니다.
        } // 손의 자식인 휴대전화는 별도 위치 대입이나 물리 이동 없이 관절을 따라갑니다.

        private void OnDisable() // 연출이나 친구가 비활성화될 때 직접 적용한 관절 회전만 정리합니다.
        { // 멈춤 상태·이동 경로·입력·Animator 컨트롤러는 변경하지 않습니다.
            m_PoseWeight = 0f; // 재활성화 시 현재 이동 상태에 맞춰 처음부터 부드럽게 자세를 전환합니다.
            if (m_TappingThumb != null) m_TappingThumb.localRotation = m_ThumbRest; // 연출을 꺼도 엄지의 마지막 터치 회전이 남지 않게 합니다.
            if (!m_HasAppliedPose) return; // 자세를 적용하지 않았다면 관절을 덮어쓰지 않습니다.
            if (m_UpperChest != null) m_UpperChest.localRotation = m_AnimatedUpperChest; // 연출 전의 상체 자세를 복원합니다.
            if (m_Neck != null) m_Neck.localRotation = m_AnimatedNeck; // 휴대전화를 향한 목 기울임을 제거합니다.
            if (m_Head != null) m_Head.localRotation = m_AnimatedHead; // 화면 쪽 머리 회전이 남지 않게 합니다.
            if (m_LeftUpperArm != null) m_LeftUpperArm.localRotation = m_AnimatedUpperArm; // 왼쪽 위팔을 기존 Animator 자세로 되돌립니다.
            if (m_LeftLowerArm != null) m_LeftLowerArm.localRotation = m_AnimatedLowerArm; // 접은 왼쪽 팔꿈치의 추가 자세를 제거합니다.
            if (m_LeftHand != null) m_LeftHand.localRotation = m_AnimatedHand; // 소품을 붙인 손목도 원래 자세로 복원합니다.
            m_HasAppliedPose = false; // 해제한 자세를 중복 복원하지 않도록 기록합니다.
        } // 기존 오른팔 멈춤 응답은 별도 이동 컴포넌트에 그대로 남습니다.
    } // 친구의 휴대전화 사용 연출만 담당하는 컴포넌트를 마칩니다.
} // XR Origin·VR 입력·신호등·자동차·횡단보도 환경은 참조하지 않습니다.
