using System.Collections; // 친구의 발견·호출·손 흔들기를 하나의 연출 순서로 진행합니다.
using UnityEngine; // 기존 캐릭터 골격의 손짓과 표정을 제어합니다.

namespace Y.Crosswalk // 두 번째 상황의 친구 2 연출만 분리합니다.
{ // 친구 1의 관절과 이동 상태는 참조하지 않습니다.
    [DefaultExecutionOrder(300)] // 기존 Animator가 대기 자세를 적용한 뒤 손짓과 표정을 섞습니다.
    public sealed class SecondSituationFriendGreeting : MonoBehaviour // 친구 2의 호출 표현만 담당합니다.
    { // 손짓에 사용할 기존 모델의 관절을 Inspector에서 연결합니다.
        [SerializeField, InspectorName("이 친구의 이동"), Tooltip("호출이나 마지막 재회 손짓을 재생할 실제 캐릭터의 기존 이동입니다.")] private FriendCharacterMovement m_Movement; // 몸이 실제 플레이어 쪽을 향하게 합니다.
        [SerializeField, InspectorName("머리 관절"), Tooltip("친구 2의 기존 Head입니다.")] private Transform m_Head; // 플레이어를 반갑게 바라보는 시선을 표현합니다.
        [SerializeField, InspectorName("가슴 관절"), Tooltip("친구 2의 기존 UpperChest입니다.")] private Transform m_Chest; // 부르는 동작에 작은 상체 움직임을 연결합니다.
        [SerializeField, InspectorName("오른쪽 위팔"), Tooltip("친구 2의 기존 RightUpperArm입니다.")] private Transform m_Arm; // 손이 머리 가까이 올라가는 큰 손짓을 표현합니다.
        [SerializeField, InspectorName("오른쪽 아래팔"), Tooltip("친구 2의 기존 RightLowerArm입니다.")] private Transform m_Forearm; // 팔꿈치를 접어 손을 흔듭니다.
        [SerializeField, InspectorName("오른손"), Tooltip("친구 2의 기존 RightHand입니다.")] private Transform m_Hand; // 손바닥을 좌우로 흔들어 멀리서도 호출을 알아볼 수 있게 합니다.
        [SerializeField, Min(2f), InspectorName("호출 손짓 시간(초)"), Tooltip("대사와 함께 손을 들어 흔든 뒤 내리는 전체 시간입니다.")] private float m_Duration = 3f; // 플레이어가 자연스럽게 고개를 돌릴 시간을 확보합니다.
        private Transform m_Viewer; // 현재 플레이어 머리 위치를 시선의 기준으로 사용합니다.
        private float m_Time; // 호출 동작만의 경과 시간입니다.
        private bool m_Playing; // 호출 중에만 관절 표현을 적용합니다.
        public Vector3 HeadPosition => m_Head != null ? m_Head.position : transform.position + Vector3.up * 1.5f; // 머리 참조가 빠진 경우에도 호출 말풍선 위치 계산이 예외로 중단되지 않도록 합니다.

        private void Awake() // 이 친구의 손 관절 참조를 보완합니다.
        { // 모델이나 소품을 새로 만들지 않고 현재 캐릭터의 실제 골격만 사용합니다.
            ResolveHandReference(); // 씬에서 오른손 참조가 비어 있으면 연결된 오른쪽 아래팔의 자식 관절에서 복구합니다.
        } // 표정은 같은 친구의 기존 스킨에서 따로 적용합니다.

        private void ResolveHandReference() // 인스펙터에서 오른손 연결이 누락된 경우 실제 캐릭터의 RightHand 관절을 찾습니다.
        { // 관절 이름과 부모 관계는 기존 FriendCharacter 프리팹에서 확인한 구조를 사용합니다.
            if (m_Hand != null) return; // 이미 정상적으로 연결된 손은 변경하지 않습니다.
            if (m_Forearm != null) m_Hand = m_Forearm.Find("RightHand"); // 오른쪽 아래팔의 바로 아래에 있는 실제 오른손 관절을 우선 찾습니다.
            if (m_Hand != null || m_Movement == null) return; // 손을 찾았거나 소속 캐릭터가 없다면 다른 오브젝트는 건드리지 않습니다.
            Transform[] bones = m_Movement.GetComponentsInChildren<Transform>(true); // 해당 친구 한 명의 비활성 관절까지 포함하여 누락된 손을 탐색합니다.
            for (int i = 0; i < bones.Length; ++i) // 같은 이름의 손 관절 중 현재 친구의 오른손만 찾습니다.
            { // 다른 친구나 씬 전체의 관절을 잘못 연결하지 않도록 소속 캐릭터 안에서만 검색합니다.
                Transform bone = bones[i]; // 현재 조사하는 캐릭터 관절을 가져옵니다.
                if (bone.name != "RightHand" || (m_Forearm != null && !bone.IsChildOf(m_Forearm))) continue; // 오른손 이름과 오른쪽 아래팔의 자식 관계를 확인합니다.
                m_Hand = bone; // 발견한 실제 오른손 Transform을 이 연출의 손짓 대상에 연결합니다.
                break; // 동일한 이름을 가진 다른 관절을 더 이상 탐색하지 않습니다.
            } // 해당 친구 골격에서만 손 참조를 복구합니다.
        } // 연결이 불가능한 경우에는 손 관절 애니메이션만 생략하도록 LateUpdate에서 방어합니다.

        public IEnumerator Play(Transform viewer, bool speaks = false) // 건너편의 실제 호출과 말없이 맞이하는 재회 손짓을 구분합니다.
        { // 빨간불 판정이나 횡단 명령은 이 표현 컴포넌트에서 실행하지 않습니다.
            ResolveHandReference(); // 플레이 직전에 인스펙터의 오른손 참조가 비어 있어도 실제 골격에서 다시 찾습니다.
            m_Viewer = viewer; m_Time = 0f; m_Playing = true; // 이번 호출의 대상과 동작 진행도를 초기화합니다.
            if (m_Movement != null) m_Movement.TryStop(viewer); // 기존 이동 컴포넌트가 연결된 경우에만 플레이어를 바라보는 대기 회전을 시작합니다.
            float duration = m_Duration; // Inspector에 저장한 손짓 시간 동안 호출 말풍선과 동작을 유지합니다.
            BlenderRigPose.PlayFace(m_Head, speaks ? Mathf.Min(1.35f, duration) : 0f, speaks ? duration : duration + 1.2f); // 실제 호출에만 입을 움직이고 말없는 환영에는 성공 미소를 유지합니다.
            while (m_Time < duration) { m_Time += Time.unscaledDeltaTime; yield return null; } // 입력과 머리 추적을 멈추지 않고 호출 표현만 진행합니다.
            m_Playing = false; // 손 흔들기는 끝내고 얼굴은 정해진 대사·미소 시간에 맞춰 복원합니다.
        } // 도로로 이동할지는 상황 제어기가 별도로 정합니다.

        private void LateUpdate() // 현재 Animator 자세 위에만 반가운 손짓을 덧붙입니다.
        { // 누적 회전이 생기지 않도록 매 프레임 새 대기 자세를 기준으로 사용합니다.
            if (!m_Playing || m_Viewer == null) return; // 호출이 끝나거나 플레이어가 해제되면 표현을 적용하지 않습니다.
            float duration = m_Duration; // 재생 코루틴과 같은 손짓 길이를 사용합니다.
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(m_Time / 0.35f, (duration - m_Time) / 0.45f))); // 손을 들고 내릴 때 자연스럽게 대기 자세와 섞습니다.
            if (m_Arm != null) m_Arm.localRotation = Quaternion.Slerp(m_Arm.localRotation, BlenderRigPose.LocalRotation(m_Arm, Quaternion.Euler(-12f, 0f, 25f)), weight); // 위팔 관절이 있을 때만 기존 자세에 손 들기 동작을 섞습니다.
            if (m_Forearm != null) m_Forearm.localRotation = Quaternion.Slerp(m_Forearm.localRotation, BlenderRigPose.LocalRotation(m_Forearm, Quaternion.Euler(0f, 18f * Mathf.Sin(m_Time * 8f), 105f)), weight); // 아래팔 관절이 있을 때만 팔꿈치 흔들기를 적용합니다.
            if (m_Hand != null) m_Hand.localRotation *= BlenderRigPose.DeltaRotation(m_Hand, Quaternion.Euler(0f, 0f, 20f * Mathf.Sin(m_Time * 10f) * weight)); // 손 참조가 누락됐거나 해제됐다면 손목 동작만 생략해 매 프레임 예외를 막습니다.
            if (m_Chest != null) m_Chest.localRotation *= BlenderRigPose.DeltaRotation(m_Chest, Quaternion.Euler(-3f * weight, 0f, 2f * Mathf.Sin(m_Time * 4f) * weight)); // 가슴 관절이 있을 때만 작은 상체 움직임을 적용합니다.
            if (m_Head != null && m_Head.parent != null) // 머리 관절과 부모가 실제로 존재할 때만 플레이어 방향을 계산합니다.
            { // 빠진 관절 때문에 남은 손짓과 상황 진행이 멈추지 않도록 합니다.
                Vector3 direction = m_Head.parent.InverseTransformDirection(m_Viewer.position - m_Head.position); // 현재 머리 부모 기준으로 실제 플레이어 방향을 구합니다.
                float yaw = Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -40f, 40f); // 고개가 무리하게 뒤집히지 않는 범위에서 일행을 바라봅니다.
                float pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, -20f, 20f); // 플레이어 눈높이를 향한 작은 상하 시선만 적용합니다.
                m_Head.localRotation = Quaternion.Slerp(m_Head.localRotation, BlenderRigPose.LocalRotation(m_Head, Quaternion.Euler(pitch, yaw, 0f)), weight); // 원래 미소가 플레이어 쪽으로 보이게 시선을 연결합니다.
            } // 머리 관절이 없어도 손짓 재생 시간은 정상적으로 진행합니다.
        } // 다리와 루트 이동은 기존 이동 컴포넌트가 전담합니다.

        public void Stop() // 호출 종료·취소·씬 해제에서 이 친구의 손짓과 표정을 정리합니다.
        { // 다음 Animator 평가가 팔과 몸을 자연스럽게 이어받습니다.
            m_Playing = false; // 다음 LateUpdate부터 손짓을 덧붙이지 않습니다.
            BlenderRigPose.ResetFace(m_Head); // 중간에 호출이나 환영을 취소하면 입과 눈의 남은 표현을 정리합니다.
        } // 다른 캐릭터의 표정에는 영향을 주지 않습니다.
        private void OnDisable() { Stop(); } // 호출이 꺼지면 남은 손짓과 표정을 함께 정리합니다.
    } // 친구 2의 호출 표현을 마칩니다.
} // 행동 선택과 사고 결과는 추가하지 않습니다.
