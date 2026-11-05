using UnityEngine; // 신호 표시 오브젝트와 Unity의 배정밀도 게임 시간을 사용합니다.

namespace Y.Crosswalk // 개인 작업 영역의 횡단보도 기능을 기존 VR 템플릿 코드와 구분합니다.
{ // 보행자 신호등에 필요한 형식의 범위를 시작합니다.
    [DisallowMultipleComponent] // 같은 신호등에 타이머가 중복으로 추가되는 것을 막습니다.
    public sealed class PedestrianTrafficLight : MonoBehaviour // 신호등 한 개의 표시와 자동 전환 시간을 독립적으로 관리합니다.
    { // 신호 상태, 표시 참조와 시간 계산을 이 컴포넌트 안에 둡니다.
        public enum SignalState // 다른 스크립트에서도 의미가 명확한 두 가지 보행 신호를 정의합니다.
        { // 외부 판정에서 색상 값이나 오브젝트 이름에 의존하지 않도록 상태를 구분합니다.
            Red, // 서 있는 빨간 사람만 표시하는 정지 신호입니다.
            Green // 걷는 초록 사람만 표시하는 보행 신호입니다.
        } // 신호 상태에는 아직 구현하지 않는 점멸이나 카운트다운을 추가하지 않습니다.

        private const float k_MinimumDuration = 0.1f; // 유지 시간이 0 이하가 되어 시간 나머지 계산이 성립하지 않는 것을 막습니다.

        [Header("보행 신호 표시")] // 사람이 서 있거나 걷는 두 표시의 연결 항목을 묶습니다.
        [SerializeField, InspectorName("빨간색 정지 사람 표시")] // 빨간 표시의 부모 오브젝트를 인스펙터에서 연결합니다.
        [Tooltip("빨간불일 때만 켜질 서 있는 사람 모양의 부모 오브젝트입니다.")] // 검은 표시부가 아니라 사람 도형 전체를 연결하도록 설명합니다.
        private GameObject m_RedSignal; // 활성 상태를 바꾸어 빨간 사람의 모든 도형을 함께 켜고 끕니다.

        [SerializeField, InspectorName("초록색 보행 사람 표시")] // 초록 표시의 부모 오브젝트를 인스펙터에서 연결합니다.
        [Tooltip("초록불일 때만 켜질 걷는 사람 모양의 부모 오브젝트입니다.")] // 비활성화될 때 사람 도형 전체가 사라지는 연결 단위를 안내합니다.
        private GameObject m_GreenSignal; // 빨간 표시와 분리된 오브젝트로 보행 신호를 제어합니다.

        [Header("자동 전환 시간")] // 시험할 신호 유지 시간을 별도 구역에 표시합니다.
        [SerializeField, Min(k_MinimumDuration), InspectorName("빨간불 유지 시간(초)")] // 정지 신호의 기본 시간을 7초로 제공하고 양수만 입력하게 합니다.
        [Tooltip("빨간불이 유지되는 게임 시간입니다. 실행 중 바꾸면 현재 신호의 시작 시각을 기준으로 반영됩니다.")] // 시간 변경 시 타이머 전체가 처음부터 재시작되지 않는 동작을 설명합니다.
        private float m_RedDuration = 7f; // 기본 시험 설정에서 첫 보행 신호까지 7초를 기다립니다.

        [SerializeField, Min(k_MinimumDuration), InspectorName("초록불 유지 시간(초)")] // 보행 신호의 유지 시간도 인스펙터에서 따로 조절합니다.
        [Tooltip("초록불이 유지되는 게임 시간입니다. 0.1초 이상으로 설정하며, 시간이 끝나면 다시 빨간불이 됩니다.")] // 초록불 이후 정지 신호로 되돌아가는 반복 동작을 안내합니다.
        private float m_GreenDuration = 7f; // 기본 시험 설정에서는 보행 신호를 7초 동안 유지합니다.

        private double m_StateStartedAt; // 현재 신호가 시작한 절대 시간을 저장하여 프레임별 누적 오차를 피합니다.
        private bool m_ScenarioPaused; // 관찰 중에도 표시를 켜 둔 채 이 신호의 자동 전환만 보류합니다.
        private double m_ScenarioPausedAt; // 재개할 때 정지한 시간을 신호 기준 시각에서 제외합니다.
        public bool IsScenarioPaused => m_ScenarioPaused; // 상황 제어기가 기존 개별 정지 상태를 보관하고 복원하게 합니다.

        public void SetScenarioPaused(bool paused) // 신호 오브젝트를 끄지 않고 현재 표시와 남은 시간을 유지합니다.
        { // 전역 시간 배율과 헤드셋 입력은 변경하지 않습니다.
            if (m_ScenarioPaused == paused) return; // 같은 요청이 타이머 기준 시각을 반복 변경하지 않게 합니다.
            if (paused) m_ScenarioPausedAt = Time.timeAsDouble; // 자동 전환을 보류하기 시작한 게임 시각을 기록합니다.
            else m_StateStartedAt += Time.timeAsDouble - m_ScenarioPausedAt; // 보류한 시간만큼 시작 기준을 옮겨 남은 신호 시간을 이어갑니다.
            m_ScenarioPaused = paused; // 다음 Update부터 이 신호의 타이머만 정지하거나 재개합니다.
        } // 기존 자동 빨간불·초록불 순환은 재개 후 그대로 사용합니다.

        public void HoldRedForScenario() // 두 번째 상황은 기존 빨간 표시를 켠 상태로 시작합니다.
        { // 유지 시간을 임의로 늘리거나 매 프레임 색을 덮어쓰지 않습니다.
            m_StateStartedAt = Time.timeAsDouble; m_ScenarioPausedAt = m_StateStartedAt; // 이번 빨간불의 시작과 타이머 보류 시점을 함께 설정합니다.
            ApplyState(SignalState.Red); m_ScenarioPaused = true; // 기존 표시 함수를 사용하고 관찰이 끝날 때까지 자동 전환을 보류합니다.
        } // 차량도 같은 CurrentState에서 실제 빨간불 상태를 읽습니다.

        public void HoldGreenForScenario() // 세 번째 상황의 관찰과 안전 횡단이 끝날 때까지 기존 파란 보행 표시를 유지합니다.
        { // 표시 오브젝트를 복제하거나 기존 자동 신호 순서를 다시 만들지 않습니다.
            m_StateStartedAt = Time.timeAsDouble; m_ScenarioPausedAt = m_StateStartedAt; // 기존 신호 타이머의 시작 기준을 현재 시각으로 맞춥니다.
            ApplyState(SignalState.Green); m_ScenarioPaused = true; // 실제 파란 보행 표시를 켜고 시험용 자동 전환만 보류합니다.
        } // 차량과 XR 입력의 시간을 전역으로 정지하지 않습니다.

        public SignalState CurrentState { get; private set; } = SignalState.Red; // 다른 스크립트는 현재 신호를 읽을 수 있지만 임의로 바꾸지는 못합니다.

        private void OnEnable() // 씬 시작이나 신호등 재활성화 시 정지 신호부터 시간을 계산합니다.
        { // 오브젝트 연결을 확인한 뒤 최초 상태와 시작 시각을 함께 설정합니다.
            if (m_RedSignal == null || m_GreenSignal == null || m_RedSignal == m_GreenSignal) // 서로 다른 두 표시가 있어야 한 신호만 켜는 규칙을 지킬 수 있습니다.
            { // 불완전한 표시 연결로 자동 전환이 시작되지 않게 합니다.
                Debug.LogError("보행자 신호등에는 서로 다른 빨간색·초록색 사람 표시 오브젝트가 필요합니다.", this); // 누락된 프리팹 연결을 해당 컴포넌트와 함께 알립니다.
                enabled = false; // 표시 참조가 올바르지 않으면 타이머를 실행하지 않습니다.
                return; // 연결되지 않은 오브젝트에 접근하지 않고 초기화를 마칩니다.
            } // 올바르게 연결된 프리팹만 정지 신호 초기화로 진행합니다.

            m_StateStartedAt = Time.timeAsDouble; // 시간 배율과 일시정지를 따르는 배정밀도 게임 시간을 기준점으로 삼습니다.
            ApplyState(SignalState.Red); // 저장된 표시 상태와 관계없이 실행 시작 시 빨간 사람만 켭니다.
        } // 최초 신호와 타이머 기준점이 같은 활성화 시점에 설정됩니다.

        private void Update() // 활성 상태에서 현재 신호의 유지 시간이 지났는지 프레임마다 판단합니다.
        { // 지나간 시간으로 최종 상태를 먼저 계산하고 표시 변경은 마지막에 한 번만 수행합니다.
            if (m_ScenarioPaused) return; // 관찰과 호출 도입에서는 현재 빨간 표시를 유지하고 타이머만 보류합니다.
            double redDuration = Mathf.Max(k_MinimumDuration, m_RedDuration); // 직렬화된 값에도 최소 시간을 적용하여 계산에 0이 들어가지 않게 합니다.
            double greenDuration = Mathf.Max(k_MinimumDuration, m_GreenDuration); // 초록불에도 같은 양수 조건을 적용하고 실행 중 변경값을 읽습니다.
            double now = Time.timeAsDouble; // 한 프레임의 모든 시간 계산에서 같은 시각을 사용합니다.
            double elapsed = now - m_StateStartedAt; // 델타 시간을 더하지 않고 현재 신호가 시작된 이후의 시간을 구합니다.
            double duration = CurrentState == SignalState.Red ? redDuration : greenDuration; // 현재 켜진 신호에 해당하는 유지 시간을 선택합니다.
            if (elapsed < duration) // 아직 유지 시간이 끝나지 않았으면 오브젝트를 다시 켜거나 끄지 않습니다.
                return; // 같은 상태의 중복 적용을 피하고 다음 프레임을 기다립니다.

            SignalState nextState = CurrentState == SignalState.Red ? SignalState.Green : SignalState.Red; // 현재 신호의 종료 직후 시작할 반대 신호를 계산합니다.
            double remainder = (elapsed - duration) % (redDuration + greenDuration); // 초과 시간을 보존하고 긴 지연으로 지나간 완전한 반복 주기는 한 번에 건너뜁니다.
            double nextDuration = nextState == SignalState.Red ? redDuration : greenDuration; // 남은 시간이 다음 신호까지 지나쳤는지 판단할 기준을 구합니다.
            if (remainder >= nextDuration) // 나머지 시간 안에서도 다음 신호가 이미 끝났다면 그 뒤의 상태가 최종 신호입니다.
            { // 중간 상태를 실제로 표시하지 않고 시간과 상태만 한 단계 더 진행합니다.
                remainder -= nextDuration; // 최종 신호 안에서 이미 경과한 시간만 남깁니다.
                nextState = nextState == SignalState.Red ? SignalState.Green : SignalState.Red; // 지연된 프레임에서 실제로 보여야 하는 신호를 정합니다.
            } // 나머지는 최종 신호의 유지 시간보다 작으므로 추가 반복 전환이 필요하지 않습니다.

            m_StateStartedAt = now - remainder; // 예정된 시작 시각을 복원하여 매 전환마다 프레임 지연이 누적되지 않게 합니다.
            if (CurrentState != nextState) // 긴 지연 뒤에도 같은 색이면 기존 표시를 불필요하게 껐다 켜지 않습니다.
                ApplyState(nextState); // 이번 프레임에 필요한 최종 신호만 한 번 적용합니다.
        } // 시간 경과와 인스펙터 변경을 반영한 신호 판단을 마칩니다.

        private void ApplyState(SignalState state) // 두 사람 표시의 활성 상태와 공개 상태를 함께 갱신합니다.
        { // 켤 표시를 선택하기 전에 두 표시를 모두 꺼 동시 점등을 방지합니다.
            m_RedSignal.SetActive(false); // 정지 사람 도형 전체를 먼저 끕니다.
            m_GreenSignal.SetActive(false); // 보행 사람 도형도 끈 뒤 하나만 선택할 준비를 합니다.
            CurrentState = state; // 외부에서 읽는 상태를 이번에 적용할 신호로 갱신합니다.
            if (state == SignalState.Red) // 정지 상태이면 서 있는 사람 표시를 선택합니다.
                m_RedSignal.SetActive(true); // 검은 렌즈는 유지하고 빨간 사람 도형만 켭니다.
            else // 보행 상태이면 정지 표시를 끈 상태로 유지합니다.
                m_GreenSignal.SetActive(true); // 팔과 다리를 벌린 초록색 보행 사람만 켭니다.
        } // 한 신호가 끝나고 반대 신호를 켜는 모든 경로에서 같은 배타 규칙을 사용합니다.

        private void OnDisable() // 신호등이나 컴포넌트가 꺼지면 표시도 함께 정리합니다.
        { // 초기 연결이 누락되어 비활성화된 경우에도 안전하게 표시를 끕니다.
            if (m_RedSignal != null) // 빨간 표시가 연결된 경우에만 활성 상태를 바꿉니다.
                m_RedSignal.SetActive(false); // 정지 신호가 비활성 컴포넌트에 남아 켜져 있지 않게 합니다.
            if (m_GreenSignal != null) // 초록 표시의 연결도 독립적으로 확인합니다.
                m_GreenSignal.SetActive(false); // 다시 활성화될 때는 OnEnable에서 빨간불로 시작합니다.
        } // 비활성 상태에서는 타이머와 사람 표시가 함께 멈춥니다.
    } // 보행자 신호등 한 개의 자동 전환 동작을 마칩니다.
} // 횡단보도 전용 코드의 범위를 마칩니다.
