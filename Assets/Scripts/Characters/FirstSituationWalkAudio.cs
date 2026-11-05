using UnityEngine; // 기존 보행 위상에 맞춘 발소리와 보행 환경음을 개별 제어합니다.

namespace Y.Crosswalk // 첫 번째 상황의 소리만 연출 상태와 연결합니다.
{ // 전역 오디오나 다른 횡단보도 구간의 음원은 변경하지 않습니다.
    public sealed class FirstSituationWalkAudio : MonoBehaviour // 씬에 미리 배치한 두 AudioSource를 재사용합니다.
    { // 실제 보행과 연결되는 참조만 Inspector에 노출합니다.
        [SerializeField, InspectorName("친구 1 이동"), Tooltip("기존 FriendCharacterMovement입니다. 실제 걸을 때만 발소리를 냅니다.")] private FriendCharacterMovement m_Movement; // 관찰 정지나 인사에는 발소리를 내지 않습니다.
        [SerializeField, InspectorName("친구 1 Animator"), Tooltip("기존 걸음 진행도 파라미터를 읽을 Animator입니다.")] private Animator m_Animator; // 새 보행 타이머 대신 실제 발걸음 위상을 읽습니다.
        [SerializeField, InspectorName("보행 환경음"), Tooltip("미리 배치한 바람과 작은 새소리의 반복 음원입니다.")] private AudioSource m_Ambience; // 도입과 결과 보행에만 낮은 음량의 환경음을 표시합니다.
        [SerializeField, InspectorName("발소리"), Tooltip("기존 보행 위상에 맞춰 재생할 짧은 발소리 음원입니다.")] private AudioSource m_Footsteps; // 친구의 실제 발 위치에서 발걸음을 들려줍니다.
        private static readonly int k_WalkPhase = Animator.StringToHash("걸음 진행도"); // 기존 애니메이션이 사용하는 정확한 파라미터를 재사용합니다.
        private float m_AmbienceVolume; // Inspector에서 설정한 원래 음량을 페이드의 목표로 보관합니다.
        private int m_LastFoot = -1; // 왼발과 오른발 접지 구간의 전환만 한 번씩 재생합니다.
        private bool m_Active; // 해당 상황의 소리를 재생할지 개별 관리합니다.

        private void Awake() // 실제 씬의 음원 설정을 준비합니다.
        { // AudioSource나 클립을 실행 중 생성하지 않습니다.
            m_AmbienceVolume = m_Ambience.volume; m_Ambience.volume = 0f; m_Ambience.loop = true; m_Ambience.spatialBlend = 0f; // 환경음은 작은 공통 음량으로 부드럽게 재생합니다.
            m_Footsteps.loop = false; m_Footsteps.spatialBlend = 1f; m_Footsteps.dopplerLevel = 0f; // 발소리는 실제 발 근처의 짧은 공간 음향으로 사용합니다.
        } // 기존 신호등·차량 음원과 오디오 리스너는 수정하지 않습니다.

        public void SetActive(bool active) // 보행 도입과 결과 연출에서만 이 상황의 소리를 재생합니다.
        { // 같은 요청이 반복되어도 환경음 클립을 다시 시작하지 않습니다.
            m_Active = active; // 다음 Update에서 음량을 부드럽게 전환합니다.
            if (active && !m_Ambience.isPlaying) m_Ambience.Play(); // 활성화할 때만 저장된 환경음을 재생합니다.
        } // 실제 발소리는 보행 위상으로만 발생시킵니다.

        private void Update() // 캐릭터의 정지와 보행을 따라 소리를 개별 갱신합니다.
        { // 환경음과 발걸음을 한 번의 전역 정지로 묶지 않습니다.
            m_Ambience.volume = Mathf.MoveTowards(m_Ambience.volume, m_Active ? m_AmbienceVolume : 0f, Time.unscaledDeltaTime * Mathf.Max(0.01f, m_AmbienceVolume) * 3f); // 장면 전환에 맞춰 작은 음량으로 페이드합니다.
            if (!m_Active || !m_Movement.IsWalking || !m_Animator.enabled) { m_LastFoot = -1; return; } // 관찰·충돌 정지·대기에는 접지음을 추가하지 않습니다.
            int foot = Mathf.Repeat(m_Animator.GetFloat(k_WalkPhase), 1f) < 0.5f ? 0 : 1; // 기존 걸음 주기의 반주기마다 접지하는 발을 구분합니다.
            m_Footsteps.transform.position = m_Movement.transform.position + Vector3.up * 0.08f; // 루트 이동을 별도로 제어하지 않고 소리 위치만 발 가까이에 맞춥니다.
            if (m_LastFoot >= 0 && foot != m_LastFoot) m_Footsteps.PlayOneShot(m_Footsteps.clip); // 실제 발이 바뀌는 순간에만 짧은 한 걸음을 재생합니다.
            m_LastFoot = foot; // 같은 지지 구간에서 발소리가 반복되지 않게 합니다.
        } // 캐릭터가 정지하면 고정된 걸음 위상도 더 이상 소리를 만들지 않습니다.

        public void Stop() // 관찰·사고 접촉·취소·재시도에서 남은 소리를 함께 초기화합니다.
        { // 씬 종료 순서와 관계없이 살아 있는 음원만 정리합니다.
            m_Active = false; m_LastFoot = -1; // 이전 발 접지 구간을 다음 연출에 남기지 않습니다.
            if (m_Ambience != null) { m_Ambience.Stop(); m_Ambience.volume = 0f; } // 관찰 시 환경음도 같은 시점에서 멈춥니다.
            if (m_Footsteps != null) m_Footsteps.Stop(); // 재생 중인 마지막 발소리도 정리합니다.
        } // 다음 활성화는 원래 Inspector 음량으로 다시 페이드합니다.
        private void OnDisable() { Stop(); } // 상황이 꺼져도 발소리나 환경음이 계속 재생되지 않게 합니다.
    } // 첫 번째 상황의 보행 소리 연결을 마칩니다.
} // 새 직접 이동 입력이나 별도 캐릭터 애니메이션을 만들지 않습니다.
