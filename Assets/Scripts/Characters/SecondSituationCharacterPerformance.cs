using System.Collections; // 경고·물러남·놀람을 순서대로 재생합니다.
using UnityEngine; // 기존 사람형 골격의 관절과 발 접지만 제어합니다.

namespace Y.Crosswalk // 두 번째 상황의 친구 2 표현을 진행·입력과 분리합니다.
{ // 기존 모델과 Animator를 그대로 사용합니다.
    [DefaultExecutionOrder(350)] // 기존 보행과 호출 표현 뒤에 결과 관절 자세를 적용합니다.
    public sealed class SecondSituationCharacterPerformance : MonoBehaviour // 친구 2의 결과 동작과 관찰 자세 복원을 담당합니다.
    { // 모든 관절은 실제 친구 2 인스턴스의 Inspector 참조로 연결합니다.
        [SerializeField, InspectorName("친구 2 이동"), Tooltip("기존 보행과 개별 정지를 담당하는 친구 2 이동입니다.")] private FriendCharacterMovement m_Movement; // 루트의 일반 보행은 기존 이동기가 소유합니다.
        [SerializeField, InspectorName("친구 2 Animator"), Tooltip("친구 2 VisualRoot의 기존 Animator입니다.")] private Animator m_Animator; // 기존 대기·걷기 애니메이션을 유지합니다.
        [SerializeField, InspectorName("골반"), Tooltip("친구 2의 Hips입니다.")] private Transform m_Hips; // 체중 이동과 무릎 굽힘의 중심입니다.
        [SerializeField, InspectorName("허리"), Tooltip("친구 2의 Spine입니다.")] private Transform m_Spine; // 놀랄 때 몸통이 나눠 젖혀지게 합니다.
        [SerializeField, InspectorName("가슴"), Tooltip("친구 2의 UpperChest입니다.")] private Transform m_Chest; // 손짓과 상체 반동을 표현합니다.
        [SerializeField, InspectorName("머리"), Tooltip("친구 2의 Head입니다.")] private Transform m_Head; // 신호 확인과 고개 끄덕임을 표현합니다.
        [SerializeField, InspectorName("양쪽 위팔"), Tooltip("왼쪽·오른쪽 UpperArm 순서입니다.")] private Transform[] m_Arms; // 놀랄 때 양팔을 벌려 균형을 잡습니다.
        [SerializeField, InspectorName("양쪽 아래팔"), Tooltip("왼쪽·오른쪽 LowerArm 순서입니다.")] private Transform[] m_Forearms; // 팔꿈치 반동을 따로 표현합니다.
        [SerializeField, InspectorName("양쪽 허벅지"), Tooltip("왼쪽·오른쪽 UpperLeg 순서입니다.")] private Transform[] m_Thighs; // 발 접지와 뒷걸음의 두 관절 계산에 사용합니다.
        [SerializeField, InspectorName("양쪽 종아리"), Tooltip("왼쪽·오른쪽 LowerLeg 순서입니다.")] private Transform[] m_Shins; // 무릎을 굽혀 발을 실제 바닥에 놓습니다.
        [SerializeField, InspectorName("양발"), Tooltip("왼발·오른발 순서입니다.")] private Transform[] m_Feet; // 회전하거나 물러날 때 지지하는 발을 고정합니다.
        [SerializeField, InspectorName("놀란 입"), Tooltip("친구 2 머리에 실제로 배치한 둥근 입 표현입니다.")] private GameObject m_SurpriseMouth; // 기존 미소 위에 필요한 순간만 놀란 표정을 표시합니다.
        [SerializeField, InspectorName("뒷걸음 발소리"), Tooltip("기존 친구 2 보행 소리와 같은 발소리 AudioSource입니다.")] private AudioSource m_Footsteps; // 보행기가 멈춘 물러남에서도 실제 발 교대에 맞춰 소리를 냅니다.
        [SerializeField, Range(0.25f, 0.45f), InspectorName("뒷걸음 한 발 시간"), Tooltip("발을 번갈아 디딜 때 한 발의 초 단위 시간입니다.")] private float m_StepDuration = 0.34f; // 몸 전체가 미끄러지는 회전을 줄입니다.
        private enum PoseMode { None, Wave, Notice, Retreat, Thanks, Panic, PanicHold } // 보행 위 표현과 개별 정지 후 관절 연출을 구분합니다.
        private PoseMode m_Mode; // 현재 적용할 친구 2의 표현만 보관합니다.
        private Transform[] m_Bones; // 기존 Animator 아래의 모든 관절과 얼굴 소품을 저장합니다.
        private Vector3[] m_CheckPositions, m_CheckScales, m_BasePositions, m_BaseScales; // 관찰 복원용 자세와 결과 연출 기준 자세를 별도로 보관합니다.
        private Quaternion[] m_CheckRotations, m_BaseRotations; // 재시도와 누적 회전 방지를 위한 로컬 회전 배열입니다.
        private readonly Vector3[] m_Planted = new Vector3[2], m_SwingFrom = new Vector3[2], m_SwingTo = new Vector3[2], m_FootOffsets = new Vector3[2]; // 지지 발과 움직이는 발을 각자 관리합니다.
        private readonly Quaternion[] m_FootRotations = new Quaternion[2]; // 모델의 실제 발 방향을 저장하여 발바닥이 뒤집히지 않게 합니다.
        private Vector3 m_RootStart, m_RootEnd; // 보행기를 정지한 결과 구간에서만 적용할 루트 이동입니다.
        private Quaternion m_RootRotation, m_EndRotation; // 발걸음에 맞춰 친구만 부드럽게 돌아보게 합니다.
        private Transform m_LookTarget; // 신호나 플레이어를 볼 때의 실제 대상입니다.
        private float m_Time, m_PlaybackRate = 1f, m_StepProgress; // 표현 시간과 이 캐릭터만의 느린 동작을 관리합니다.
        private int m_StepIndex = -1; // 한 발이 바뀔 때만 새로운 착지점을 준비합니다.
        private bool m_NoticeSurprised; // 같은 응시 동작에서 빨간불 놀람과 파란불·차량 확인을 구분합니다.
        private bool m_HasCheckpoint; // 관찰 전에 저장되지 않은 자세를 복원하지 않게 합니다.
        public Transform HeadTarget => m_Head; // 재회한 다른 친구가 발 위치가 아닌 실제 얼굴을 바라보게 합니다.
        public Vector3 HeadPosition => m_Head.position; // 기존 월드 말풍선을 실제 친구 머리 옆에 배치합니다.
        public void SetPlaybackRate(float rate) { m_PlaybackRate = Mathf.Clamp(rate, 0.08f, 1f); } // 신체 반동만 개별적으로 느리게 재생합니다.

        private void Awake() // 기존 골격의 저장 공간만 준비합니다.
        { // 오브젝트나 애니메이션 모델을 실행 중 생성하지 않습니다.
            m_Bones = m_Animator.GetComponentsInChildren<Transform>(true); // 눈과 입을 포함한 이 친구의 시각 계층만 가져옵니다.
            m_CheckPositions = new Vector3[m_Bones.Length]; m_CheckScales = new Vector3[m_Bones.Length]; m_CheckRotations = new Quaternion[m_Bones.Length]; // 관찰 당시 자세를 저장할 공간입니다.
            m_BasePositions = new Vector3[m_Bones.Length]; m_BaseScales = new Vector3[m_Bones.Length]; m_BaseRotations = new Quaternion[m_Bones.Length]; // 누적 덧셈 없이 결과 자세를 계산할 기준입니다.
            BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); // 일반 등장에서는 기존 미소만 보이게 합니다.
        } // 친구 1의 구성요소나 저장 자세에는 접근하지 않습니다.

        public void CaptureObservation() // 첫 발을 멈춘 최종 프레임을 재시도의 기준으로 저장합니다.
        { // 이동 경로와 시각 골격을 같은 시점에 보관합니다.
            m_Movement.CaptureScenarioCheckpoint(); SavePose(m_CheckPositions, m_CheckRotations, m_CheckScales); m_HasCheckpoint = true; // 현재 루트·보행 상태와 각 관절 자세를 함께 저장합니다.
        } // 첫 번째 상황의 완료 기록과 체크포인트는 변경하지 않습니다.

        public void RestoreObservation() // 위험 결과와 임시 느린 동작을 지우고 처음의 관찰 자세로 돌아갑니다.
        { // 복원은 화면 전환으로 가린 동안만 상황 제어기가 호출합니다.
            if (!m_HasCheckpoint || m_Movement == null) return; // 저장 전 취소나 먼저 해제된 친구에는 접근하지 않습니다.
            m_Mode = PoseMode.None; m_Time = 0f; m_PlaybackRate = 1f; m_Movement.SetScenarioPaused(true); // 결과 관절 처리와 경로 갱신을 먼저 보류합니다.
            m_Movement.RestoreScenarioCheckpoint(); LoadPose(m_CheckPositions, m_CheckRotations, m_CheckScales); // 루트와 골격을 동일한 관찰 프레임으로 복원합니다.
            if (m_SurpriseMouth != null) BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); // 이전 놀람 효과가 다음 선택에 남지 않게 합니다.
            BlenderRigPose.ResetFace(m_Head); // 이전 대사나 성공 미소가 복원한 얼굴에 남지 않게 합니다.
            if (m_Footsteps != null) m_Footsteps.Stop(); // 마지막 뒷걸음 소리가 관찰 복원 뒤에 이어지지 않게 합니다.
        } // 헤드셋과 플레이어 카메라에는 접근하지 않습니다.

        public IEnumerator WaveBeforeCrossing() // 반가운 손짓을 먼저 보여 준 뒤 기존 횡단 보행으로 연결합니다.
        { // 멈춘 첫 발 자세는 매 프레임 같은 기준에서 손짓만 더합니다.
            SavePose(m_BasePositions, m_BaseRotations, m_BaseScales); m_Time = 0f; m_Mode = PoseMode.Wave; // 결과 시작의 기준 자세를 저장합니다.
            while (m_Time < 0.8f && isActiveAndEnabled && m_Mode == PoseMode.Wave) yield return null; // 복원·비활성화 시 이전 관절 연출의 대기를 종료합니다.
            if (!isActiveAndEnabled || m_Mode != PoseMode.Wave) yield break; // 종료된 코루틴이 다음 상태의 자세를 덮어쓰지 않습니다. // 손을 들고 한 번 크게 흔들 시간을 둡니다.
            m_Mode = PoseMode.None; LoadPose(m_BasePositions, m_BaseRotations, m_BaseScales); // 기존 보행 재개 전에 손짓의 마지막 회전을 제거합니다.
        } // 걷기 루트와 신호 판단은 상황 제어기가 연결합니다.

        public IEnumerator NoticeRedSignal(Transform signal) // 기존 두 번째 상황의 빨간불 놀람 시간과 표정을 유지합니다.
        { // 응시 관절 계산만 다음 상황에서도 재사용합니다.
            yield return LookAtTarget(signal, 1.4f, true); // 기존 빨간불 반응은 놀란 입을 포함합니다.
        } // 두 번째 상황의 결과 순서는 변경하지 않습니다.

        public void BeginLookingAt(Transform target, bool surprised = false) // 신호나 계속 움직이는 자동차를 실제 위치에 맞춰 바라보기 시작합니다.
        { // 같은 고개 움직임을 시간 지정 연출과 차량 통과 대기에 사용합니다.
            m_LookTarget = target; m_NoticeSurprised = surprised; m_Time = 0f; m_Mode = PoseMode.Notice; // 현재 응시 대상과 표정을 골격 갱신에 전달합니다.
        } // 시선의 지속 시간은 이 동작을 시작한 상황에서 정합니다.

        public void StopLookingAt() // 현재 응시 동작을 정리하고 다음 주변 확인이나 보행으로 이어갑니다.
        { // 이미 다른 관절 연출로 넘어갔다면 그 자세를 건드리지 않습니다.
            if (m_Mode != PoseMode.Notice) return; // 차량 확인을 끝낼 때 뒷걸음이나 감사 동작의 상태를 지우지 않습니다.
            m_Mode = PoseMode.None; m_LookTarget = null; // 다음 프레임부터 기존 Animator에 기본 자세를 맡깁니다.
            BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, false); // 다음 표정으로 넘어갈 때 입에 이전 반응을 남기지 않습니다.
        } // 친구의 위치나 플레이어 시점은 유지합니다.

        public IEnumerator LookAtTarget(Transform target, float duration = 1f, bool surprised = false) // 신호나 좌우 방향을 일정 시간 확인하는 기존 연출에 사용합니다.
        { // 같은 응시 자세를 정해진 길이로 재생합니다.
            BeginLookingAt(target, surprised); // 현재 실제 응시점과 필요한 표정을 지정합니다.
            while (m_Time < Mathf.Max(0.2f, duration) && isActiveAndEnabled && m_Mode == PoseMode.Notice) yield return null; // 지정한 시간까지 보되 취소된 연출은 다음 상태에 접근하지 않습니다.
            if (isActiveAndEnabled) StopLookingAt(); // 응시가 계속 살아 있는 경우에만 자세를 정리합니다.
        } // 다음 시선이나 횡단 명령은 현재 상황 제어기에서 시작합니다.

        private float m_RetreatDuration = 1.8f; // 실제 이동 거리와 돌아설 각도로 한 번의 발 디딤 연출 시간을 결정합니다.
        public IEnumerator RetreatInsideSidewalk(Transform destination, Transform viewer) // 도로 쪽 첫 발을 거두고 인도 안으로 물러나며 플레이어를 바라봅니다.
        { // 일반 보행기를 멈춘 구간에서만 이 연출이 루트를 소유합니다.
            m_Movement.SetScenarioPaused(true); BeginSteps(destination.position, viewer); m_Time = 0f; m_Mode = PoseMode.Retreat; // 원래 접지된 발을 기준으로 이동·회전을 시작합니다.
            m_RetreatDuration = Mathf.Clamp(Mathf.Max(Vector3.Distance(m_RootStart, m_RootEnd) / 0.8f, Quaternion.Angle(m_RootRotation, m_EndRotation) / 180f) + m_StepDuration, 0.6f, 1.8f); // 제자리 방향 전환도 무조건 1.8초 기다리지 않습니다. // 양발 접지와 짧은 뒷걸음 경로를 준비합니다.
            while (m_Time < m_RetreatDuration && isActiveAndEnabled && m_Mode == PoseMode.Retreat) yield return null; // 실제 발 디딤과 몸 회전이 완료될 때까지만 기다립니다.
            if (m_Mode != PoseMode.Retreat || !isActiveAndEnabled) yield break; // 복원이나 종료로 취소된 이전 연출을 다시 적용하지 않습니다. // 발을 번갈아 디디며 몸과 머리를 함께 돌립니다.
            m_Time = m_RetreatDuration; ApplySteps(1f); m_Mode = PoseMode.None; // 마지막 착지 위치를 확정하고 추가 측면 이동은 하지 않습니다.
        } // 도착 후 감사는 그 자리에서만 진행합니다.

        public IEnumerator AcknowledgeWaiting() // 차를 확인한 뒤 인도에서 고개를 끄덕이며 기다림을 표현합니다.
        { // 기존 감사의 짧은 끄덕임·가슴 높이 손짓만 재사용하며 감사 대사는 아직 표시하지 않습니다.
            SavePose(m_BasePositions, m_BaseRotations, m_BaseScales); m_Time = 0f; m_Mode = PoseMode.Thanks; // 현재 대기 자세에서 부드러운 응답을 시작합니다.
            while (m_Time < 0.75f && isActiveAndEnabled && m_Mode == PoseMode.Thanks) yield return null; // 복원·비활성화 시 이전 관절 연출의 대기를 종료합니다.
            if (!isActiveAndEnabled || m_Mode != PoseMode.Thanks) yield break; // 종료된 코루틴이 다음 상태의 자세를 덮어쓰지 않습니다. // 움직이는 차를 앞에 두고 먼저 기다리는 모습을 보여 줍니다.
            m_Mode = PoseMode.None; LoadPose(m_BasePositions, m_BaseRotations, m_BaseScales); // 원래 대기 자세로 돌아간 뒤 실제 차량 정지를 확인합니다.
        } // 차량과 입력은 따로 계속 갱신합니다.

        public IEnumerator ThankPlayer(float duration) // 기다리기로 위험을 피한 친구가 미소와 끄덕임으로 응답합니다.
        { // 대사 문구는 씬의 실제 감사 말풍선에서 표시합니다.
            m_Time = 0f; m_Mode = PoseMode.Thanks; SavePose(m_BasePositions, m_BaseRotations, m_BaseScales); // 감사 시작의 착지 자세를 고정합니다.
            while (m_Time < Mathf.Max(2.6f, duration) && isActiveAndEnabled && m_Mode == PoseMode.Thanks) yield return null; // 복원·비활성화 시 이전 관절 연출의 대기를 종료합니다.
            if (!isActiveAndEnabled || m_Mode != PoseMode.Thanks) yield break; // 종료된 코루틴이 다음 상태의 자세를 덮어쓰지 않습니다. // 말풍선을 읽을 시간과 감사 동작을 함께 유지하며 최소 2.6초는 확보합니다.
            m_Mode = PoseMode.None; LoadPose(m_BasePositions, m_BaseRotations, m_BaseScales); // 과장된 팔과 눈의 표현을 정리합니다.
        } // 친구는 건너편 인도에 그대로 남습니다.

        public IEnumerator PanicNearCar() // 차를 뒤늦게 본 친구의 관절 반동과 두 번의 뒷걸음을 재생합니다.
        { // 몸 전체를 뒤로 눕히는 대신 가슴·허리·팔·양다리를 각각 움직입니다.
            m_Movement.SetScenarioPaused(true); BeginSteps(transform.position - transform.forward * 0.6f, null); m_Time = 0f; m_Mode = PoseMode.Panic; // 차에서 멀어지는 뒷걸음과 실제 접지를 준비합니다.
            while (m_Time < 2.2f && isActiveAndEnabled && m_Mode == PoseMode.Panic) yield return null; // 복원·비활성화 시 이전 관절 연출의 대기를 종료합니다.
            if (!isActiveAndEnabled || m_Mode != PoseMode.Panic) yield break; // 종료된 코루틴이 다음 상태의 자세를 덮어쓰지 않습니다. // 느린 동작은 이 시간의 진행 배율만 줄여 표현합니다.
            m_Time = 2.2f; m_Mode = PoseMode.PanicHold; // 사고 설명 동안 마지막 놀란 자세를 유지합니다.
        } // 차량 위치나 VR 카메라를 흔들지 않습니다.

        private void LateUpdate() // Animator가 적용한 기본 자세 뒤에 필요한 표현만 덧붙입니다.
        { // 정지된 결과 표현은 매번 기준 자세를 불러와 회전 누적을 방지합니다.
            if (m_Mode == PoseMode.None) return; // 평소에는 기존 모델·Animator의 자세를 그대로 둡니다.
            if (m_Mode != PoseMode.PanicHold) m_Time += Time.unscaledDeltaTime * m_PlaybackRate; // 이 친구의 연출 시간만 개별 배율로 진행합니다.
            if (m_Mode != PoseMode.Notice) LoadPose(m_BasePositions, m_BaseRotations, m_BaseScales); // Animator가 멈춘 표현에서도 매 프레임 같은 관절 기준을 사용합니다.
            if (m_Mode == PoseMode.Wave) // 빨간불을 모르고 반갑게 손을 흔드는 동작입니다.
            { // 기존 호출 때와 같은 사람형 오른팔 관절을 재사용합니다.
                float weight = Mathf.Sin(Mathf.Clamp01(m_Time / 0.8f) * Mathf.PI); // 손을 올렸다 내릴 때 튀지 않게 가중치를 만듭니다.
                m_Arms[1].localRotation = Quaternion.Slerp(m_Arms[1].localRotation, BlenderRigPose.LocalRotation(m_Arms[1], Quaternion.Euler(-12f, 0f, 25f)), weight); // 오른쪽 어깨를 들어 반가움을 표현합니다.
                m_Forearms[1].localRotation = Quaternion.Slerp(m_Forearms[1].localRotation, BlenderRigPose.LocalRotation(m_Forearms[1], Quaternion.Euler(0f, 22f * Mathf.Sin(m_Time * 12f), 105f)), weight); // 팔꿈치와 손 방향을 크게 흔듭니다.
            } // 위험 선택이 수락된 뒤에만 이 손짓을 재생합니다.
            else if (m_Mode == PoseMode.Notice) // 신호를 확인하며 놀라는 동작입니다.
            { // 이동기의 기존 신호 쪽 회전을 덮어쓰지 않습니다.
                Vector3 direction = m_Head.parent.InverseTransformDirection(m_LookTarget.position - m_Head.position); // 실제 빨간 표시를 머리 부모 기준으로 변환합니다.
                m_Head.localRotation = BlenderRigPose.LocalRotation(m_Head, Quaternion.Euler(Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, -25f, 25f), Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -45f, 45f), 0f)); // 고개만 신호를 바라보게 제한합니다.
                BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, m_NoticeSurprised && m_Time > 0.35f && m_Time < 1.1f); // 빨간불을 알아본 순간만 입을 벌립니다.
            } // 원래 걷기에서 정지로 이어지는 애니메이션은 유지합니다.
            else if (m_Mode == PoseMode.Retreat) ApplySteps(Mathf.Clamp01(m_Time / m_RetreatDuration)); // 인도 안쪽의 뒷걸음과 돌아보기를 양발 접지로 연결합니다.
            else if (m_Mode == PoseMode.Thanks) // 물러난 위치에서 미소와 감사 손짓을 표현합니다.
            { // 카메라가 아닌 친구의 머리와 팔만 움직입니다.
                float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_Time / 0.3f)); // 감사 동작을 부드럽게 시작합니다.
                m_Head.localRotation *= BlenderRigPose.DeltaRotation(m_Head, Quaternion.Euler((5f + 7f * Mathf.Sin(m_Time * 5f)) * weight, 0f, -3f * weight)); // 고개를 두 번 끄덕여 감사하는 모습을 보입니다.
                m_Arms[1].localRotation = Quaternion.Slerp(m_Arms[1].localRotation, BlenderRigPose.LocalRotation(m_Arms[1], Quaternion.Euler(-8f, 0f, 10f)), weight * 0.7f); // 얼굴을 가리지 않는 낮은 손짓을 합니다.
                m_Forearms[1].localRotation *= BlenderRigPose.DeltaRotation(m_Forearms[1], Quaternion.Euler(0f, 0f, 55f * weight)); // 오른손을 가슴 높이로 올립니다.
            } // 감사 대사가 끝나면 기준 자세로 복원합니다.
            else // 급정거를 알아본 놀람과 마지막 균형 잡기 자세입니다.
            { // 반동과 뒷걸음을 같은 국소 시간으로 맞춥니다.
                float progress = Mathf.Clamp01(m_Time / 2.2f); // 신체 반동과 뒷걸음의 공통 진행도를 구합니다.
                float shock = Mathf.Exp(-progress * 4f) * Mathf.Sin(progress * Mathf.PI * 5f); // 첫 충격을 크게 하고 흔들림은 점차 잦아들게 합니다.
                m_Hips.localPosition += Vector3.down * (0.045f + Mathf.Abs(shock) * 0.025f); // 골반을 낮춰 균형을 잃는 모습을 만듭니다.
                m_Spine.localRotation *= BlenderRigPose.DeltaRotation(m_Spine, Quaternion.Euler(-10f - shock * 12f, 0f, 5f * shock)); // 허리가 먼저 뒤로 젖혀졌다 돌아오게 합니다.
                m_Chest.localRotation *= BlenderRigPose.DeltaRotation(m_Chest, Quaternion.Euler(-12f - shock * 14f, 0f, -7f * shock)); // 가슴 반동을 허리와 다른 크기로 나눕니다.
                m_Head.localRotation *= BlenderRigPose.DeltaRotation(m_Head, Quaternion.Euler(-16f - shock * 7f, -10f, 0f)); // 얼굴을 들어 자동차를 바라보며 놀랍니다.
                m_Arms[0].localRotation *= BlenderRigPose.DeltaRotation(m_Arms[0], Quaternion.Euler(-30f, -10f, -42f - shock * 18f)); m_Arms[1].localRotation *= BlenderRigPose.DeltaRotation(m_Arms[1], Quaternion.Euler(-35f, 10f, 45f + shock * 16f)); // 양팔을 서로 다른 방향으로 벌려 균형을 잡습니다.
                m_Forearms[0].localRotation *= BlenderRigPose.DeltaRotation(m_Forearms[0], Quaternion.Euler(0f, 0f, -55f)); m_Forearms[1].localRotation *= BlenderRigPose.DeltaRotation(m_Forearms[1], Quaternion.Euler(0f, 0f, 65f)); // 팔꿈치를 굽혀 단순한 몸통 회전과 구분되는 놀람을 표현합니다.
                ApplySteps(progress); // 골반 높이와 반동을 적용한 뒤 양발을 실제 바닥 높이에 다시 접지합니다.
                BlenderRigPose.SetSurprise(m_Head, m_SurpriseMouth, true); // 사고 원인을 설명하는 동안 놀란 입을 유지합니다.
            } // 플레이어의 시점이나 시간 배율은 바꾸지 않습니다.
        } // 친구 1의 자세와 애니메이션에는 접근하지 않습니다.

        private void BeginSteps(Vector3 destination, Transform viewer) // 정지된 기존 골격에서 뒷걸음의 시작과 양발 기준을 저장합니다.
        { // 결과 이동을 적용하기 전에 일반 보행은 이미 개별 정지되어 있습니다.
            SavePose(m_BasePositions, m_BaseRotations, m_BaseScales); m_RootStart = transform.position; m_RootEnd = destination; m_RootRotation = transform.rotation; m_EndRotation = m_RootRotation; // 현재 위치와 실제 인도·도로 높이를 기준으로 움직입니다.
            Vector3 direction = viewer != null ? Vector3.ProjectOnPlane(viewer.position - destination, Vector3.up) : Vector3.zero; // 안전 결과에서만 플레이어를 바라볼 최종 방향을 구합니다.
            if (direction.sqrMagnitude > 0.001f) m_EndRotation = Quaternion.LookRotation(direction); // 유효한 수평 방향일 때 친구만 돌아보게 합니다.
            for (int i = 0; i < 2; ++i) { m_Planted[i] = m_Feet[i].position; m_FootOffsets[i] = transform.InverseTransformPoint(m_Feet[i].position); m_FootRotations[i] = m_Feet[i].rotation; } // 실제로 디딘 두 발의 위치와 루트 기준 간격을 저장합니다.
            m_StepIndex = -1; m_StepProgress = 0f; // 이전 결과의 발걸음을 새 동작에 넘기지 않습니다.
        } // 임시 모델이나 대체 Animator는 만들지 않습니다.

        private void ApplySteps(float progress) // 루트 이동과 지지 발·움직이는 발을 같은 진행도로 계산합니다.
        { // 관절은 기존 사람형 다리의 길이를 유지합니다.
            float eased = Mathf.SmoothStep(0f, 1f, progress); transform.SetPositionAndRotation(Vector3.Lerp(m_RootStart, m_RootEnd, eased), Quaternion.Slerp(m_RootRotation, m_EndRotation, eased)); // 친구만 짧게 물러나며 돌아봅니다.
            int index = Mathf.FloorToInt(m_Time / Mathf.Max(0.25f, m_StepDuration)); int swing = index % 2; // 두 발이 동시에 미끄러지지 않게 교대로 들어 올립니다.
            if (index != m_StepIndex) // 새로운 발걸음이 시작된 순간에만 착지 목표를 갱신합니다.
            { // 반대쪽 발은 앞선 걸음에서 디딘 위치를 유지합니다.
                if (m_StepIndex >= 0) m_Planted[m_StepIndex % 2] = m_SwingTo[m_StepIndex % 2]; // 앞선 발을 실제 착지점에 고정합니다.
                if (m_StepIndex >= 0 && m_Footsteps != null && m_Mode != PoseMode.PanicHold) m_Footsteps.PlayOneShot(m_Footsteps.clip); // 실제로 한 발을 디딘 순간에만 기존 발소리를 재사용합니다.
                m_SwingFrom[swing] = m_Planted[swing]; m_SwingTo[swing] = transform.TransformPoint(m_FootOffsets[swing]); m_StepIndex = index; // 이번 발은 현재 몸 위치에 맞는 착지점까지 옮깁니다.
            } // 매 프레임 새 목표를 만들어 지지 발을 끌지 않습니다.
            m_StepProgress = Mathf.Repeat(m_Time / Mathf.Max(0.25f, m_StepDuration), 1f); // 한 발의 들어 올림과 착지 위상을 구합니다.
            for (int i = 0; i < 2; ++i) // 두 다리의 접지 목표를 독립적으로 적용합니다.
            { // 마지막에는 양발을 최종 몸 위치에 안정적으로 내려놓습니다.
                Vector3 target = i == swing ? Vector3.Lerp(m_SwingFrom[i], m_SwingTo[i], Mathf.SmoothStep(0f, 1f, m_StepProgress)) + Vector3.up * (Mathf.Sin(m_StepProgress * Mathf.PI) * 0.055f) : m_Planted[i]; // 움직이는 발만 작은 포물선으로 들어 올립니다.
                if (progress > 0.88f) target = Vector3.Lerp(target, transform.TransformPoint(m_FootOffsets[i]), Mathf.InverseLerp(0.88f, 1f, progress)); // 끝부분에서 두 발의 잔여 오차를 부드럽게 정리합니다.
                PlaceFoot(i, target); // 기존 허벅지·종아리 두 관절로 발을 목표 위치에 놓습니다.
            } // 골격 길이와 모델의 크기는 바꾸지 않습니다.
        } // 카메라에는 이 움직임을 전달하지 않습니다.

        private void PlaceFoot(int index, Vector3 target) // 두 관절 역기구학으로 무릎을 굽히고 발을 접지합니다.
        { // 기존 사람형 다리의 실제 길이를 사용합니다.
            Transform thigh = m_Thighs[index], shin = m_Shins[index], foot = m_Feet[index]; Vector3 hip = thigh.position; // 현재 다리의 관절과 시작점을 가져옵니다.
            float upper = Vector3.Distance(hip, shin.position), lower = Vector3.Distance(shin.position, foot.position); // 실제 모델의 허벅지·종아리 길이를 측정합니다.
            Vector3 delta = target - hip; float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + 0.001f, upper + lower - 0.001f); Vector3 direction = delta.normalized; // 무릎이 뒤집히거나 다리가 늘어나지 않는 도달 범위를 사용합니다.
            float along = (upper * upper + distance * distance - lower * lower) / (2f * distance); Vector3 bend = Vector3.ProjectOnPlane(transform.forward, direction).normalized; // 코사인 법칙과 몸의 앞 방향으로 자연스러운 무릎 위치를 구합니다.
            Vector3 knee = hip + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along)); // 지지 발 높이를 유지할 무릎 목표점입니다.
            thigh.rotation = Quaternion.FromToRotation(shin.position - hip, knee - hip) * thigh.rotation; // 허벅지를 돌려 무릎을 목표에 맞춥니다.
            shin.rotation = Quaternion.FromToRotation(foot.position - shin.position, target - shin.position) * shin.rotation; // 종아리를 돌려 발을 실제 착지점에 맞춥니다.
            foot.rotation = transform.rotation * Quaternion.Inverse(m_RootRotation) * m_FootRotations[index]; // 모델의 원래 발바닥 방향을 보존하며 몸이 돌아선 만큼만 발을 돌립니다.
        } // 모델 전체를 뒤로 기울이는 대체 연출은 사용하지 않습니다.

        private void SavePose(Vector3[] positions, Quaternion[] rotations, Vector3[] scales) // 기존 시각 계층의 로컬 자세만 저장합니다.
        { // 친구의 이동 루트 저장은 기존 이동 컴포넌트가 담당합니다.
            for (int i = 0; i < m_Bones.Length; ++i) { positions[i] = m_Bones[i].localPosition; rotations[i] = m_Bones[i].localRotation; scales[i] = m_Bones[i].localScale; } // 각 관절과 표정 크기를 같은 프레임에서 기록합니다.
        } // 관찰용 저장과 결과 기준 저장은 서로 다른 배열을 사용합니다.
        private void LoadPose(Vector3[] positions, Quaternion[] rotations, Vector3[] scales) // 저장한 로컬 자세로 누적 회전과 표정 변형을 정리합니다.
        { // 씬 종료 중 먼저 해제된 관절은 건너뜁니다.
            for (int i = 0; i < m_Bones.Length; ++i) if (m_Bones[i] != null) { m_Bones[i].localPosition = positions[i]; m_Bones[i].localRotation = rotations[i]; m_Bones[i].localScale = scales[i]; } // 살아 있는 같은 친구의 관절만 복원합니다.
        } // 다른 캐릭터의 Renderer나 머티리얼은 변경하지 않습니다.
    } // 친구 2의 결과 표현을 마칩니다.
} // 세 번째 상황의 보행이나 신호 전환은 포함하지 않습니다.
