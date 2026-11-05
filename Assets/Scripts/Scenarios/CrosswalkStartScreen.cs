using System.Collections; // 실제 패널의 퇴장이 끝난 뒤 안내와 기존 보행 도입을 연결합니다.
using UnityEngine; // 기존 월드 Canvas와 배경 오브젝트의 대기만 제어합니다.

namespace Y.Crosswalk // 세 안전 상황 앞에 붙는 제목·조작 안내입니다.
{ // 새로운 교육 상황이나 별도의 입력 시스템은 추가하지 않습니다.
    [DefaultExecutionOrder(550)] // 기존 카메라·첫 상황·UI의 초기화가 끝난 뒤 제목을 표시합니다.
    public sealed class CrosswalkStartScreen : MonoBehaviour // 제목 → 조작 안내 → 기존 첫 번째 상황 진입만 담당합니다.
    { // 화면 구성은 모두 실제 씬 오브젝트로 연결합니다.
        private enum ScreenState { [InspectorName("숨김")] Hidden, [InspectorName("제목")] Title, [InspectorName("안내로 전환")] ChangingToInstructions, [InspectorName("조작 안내")] Instructions, [InspectorName("첫 상황으로 전환")] Starting, [InspectorName("교육 진행 중")] Running } // 중복 클릭과 게임 도중 안내 재등장을 막습니다.
        [SerializeField, InspectorName("기존 첫 번째 상황"), Tooltip("시작 대기와 기존 자동 보행 도입을 제공하는 제어기입니다.")] private FirstSituationController m_First; // 기존 사고·관찰·성공 로직은 재작성하지 않습니다.
        [SerializeField, InspectorName("기존 월드 Canvas"), Tooltip("기존 VR·마우스 레이캐스터가 연결된 실제 World Space Canvas입니다.")] private Transform m_Anchor; // 같은 입력 모듈과 UI 리소스를 사용합니다.
        [SerializeField, InspectorName("기존 플레이어 카메라"), Tooltip("XR Origin의 실제 카메라입니다. 화면 배치 기준으로만 사용합니다.")] private Camera m_Viewer; // 머리 추적이나 카메라 방향을 변경하지 않습니다.
        [SerializeField, InspectorName("잠깐 제목 패널"), Tooltip("제목·부제·시작하기 버튼이 있는 실제 Hierarchy 패널입니다.")] private FirstSituationPanel m_Title; // 기존 파스텔 배경과 버튼 반응을 재사용합니다.
        [SerializeField, InspectorName("조작 안내 패널"), Tooltip("세 안내 문구·그림 아이콘·알겠어 버튼을 저장한 실제 패널입니다.")] private FirstSituationPanel m_Instructions; // 코드로 텍스트나 기본 패널을 생성하지 않습니다.
        [SerializeField, Range(2f, 3.5f), InspectorName("시작 화면 읽기 거리(m)"), Tooltip("제목과 한 화면의 안내를 표시할 편안한 월드 거리입니다.")] private float m_Distance = 2.5f; // VR에서도 얼굴에 지나치게 가까이 붙지 않게 합니다.
        [SerializeField, InspectorName("대기할 친구 2"), Tooltip("제목과 안내 동안 배경의 친구 2 자세만 정지합니다.")] private FriendCharacterMovement m_Friend2; // 친구 1의 대기는 기존 첫 번째 제어기가 담당합니다.
        [SerializeField, InspectorName("대기할 기존 자동차"), Tooltip("시작 전 주행만 보류하고 실제 시작 시 원래 상태를 복원합니다.")] private TrafficCarMovement m_Car; // 제목을 보는 동안 교통 연출이 먼저 진행되지 않게 합니다.
        [SerializeField, InspectorName("대기할 기존 신호"), Tooltip("표시를 끄지 않고 자동 신호 타이머만 보류합니다.")] private PedestrianTrafficLight m_Signal; // 전역 시간과 UI 애니메이션은 계속 갱신합니다.
        [SerializeField, InspectorName("현재 시작 화면 상태"), Tooltip("제목·안내 전환·첫 상황 진입을 구분합니다.")] private ScreenState m_State; // 버튼이 실제 화면 단계에서만 동작하게 합니다.
        private bool m_BackgroundHeld, m_Friend2WasPaused, m_CarWasPaused, m_SignalWasPaused; // 이 화면이 보류한 기존 상태만 저장합니다.

        private void Start() // 첫 번째 상황이 시작 대기 상태인지 확인한 뒤 제목을 표시합니다.
        { // 이미 진행하거나 완료한 교육에 안내를 다시 띄우지 않습니다.
            m_Title.Show(false); m_Instructions.Show(false); // 초기 중복 패널을 정리합니다.
            if (!m_First.IsWaitingForStartScreen) { m_State = ScreenState.Running; return; } // 기존 진행 상태가 있으면 시작 화면에 개입하지 않습니다.
            HoldBackground(); PlaceScreen(); m_State = ScreenState.Title; m_Title.Show(true); // 기존 첫 장소의 배경 위에 편집 가능한 제목을 표시합니다.
        } // PC에서는 기존 입력 모듈이 실제 마우스 커서를 표시하고 버튼을 처리합니다.

        private void PlaceScreen() // 제목을 열 때 한 번만 편안한 읽기 위치를 배치합니다.
        { // 사용자의 머리 방향이나 시야각은 변경하지 않습니다.
            Vector3 forward = Vector3.ProjectOnPlane(m_Viewer.transform.forward, Vector3.up); // 기본 시작 구도의 수평 방향을 사용합니다.
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(m_Viewer.transform.parent.forward, Vector3.up); // 수직 시선에서는 기존 부모 방향을 사용합니다.
            forward.Normalize(); m_Anchor.SetPositionAndRotation(m_Viewer.transform.position + forward * m_Distance - Vector3.up * 0.08f, Quaternion.LookRotation(forward)); // 실제 Canvas만 배치하고 카메라는 그대로 둡니다.
        } // VR에서는 고개를 돌려 기존 첫 번째 상황의 배경을 볼 수 있습니다.

        public void ShowInstructions() // 실제 시작하기 버튼이 조작 안내로 넘어갑니다.
        { // 버튼을 여러 번 눌러도 화면 전환 코루틴은 하나만 실행합니다.
            if (m_State != ScreenState.Title) return; // 현재 제목 화면의 입력만 수락합니다.
            m_State = ScreenState.ChangingToInstructions; m_Title.Show(false); StartCoroutine(OpenInstructionsAfterFade()); // 제목 클릭을 즉시 해제하고 부드럽게 퇴장시킵니다.
        } // 아직 첫 번째 캐릭터와 도입은 대기합니다.
        private IEnumerator OpenInstructionsAfterFade() // 제목과 안내 문구가 서로 겹치지 않도록 순서대로 표시합니다.
        { // Inspector에서 편집한 실제 전환 시간을 사용합니다.
            yield return new WaitForSecondsRealtime(m_Title.TransitionDuration + 0.03f); // 제목이 완전히 사라진 뒤 안내를 엽니다.
            m_State = ScreenState.Instructions; m_Instructions.Show(true); // 실제 세 그림 아이콘과 안내 문구를 한 화면에 표시합니다.
        } // 안내에 시간 제한을 두지 않습니다.

        public void AcknowledgeAndBegin() // 실제 알겠어 버튼으로만 첫 번째 상황을 시작합니다.
        { // 중복 클릭은 도입이나 화면을 두 번 실행하지 않습니다.
            if (m_State != ScreenState.Instructions || !m_First.IsWaitingForStartScreen) return; // 안내와 기존 첫 상황의 대기가 모두 맞을 때만 수락합니다.
            m_State = ScreenState.Starting; m_Instructions.Show(false); StartCoroutine(BeginAfterFade()); // 안내 버튼 입력을 해제하고 기존 퇴장 효과를 재생합니다.
        } // 마우스 시점 회전은 기존 첫 상황 상태에 맞춰 전환됩니다.
        private IEnumerator BeginAfterFade() // 안내를 가린 뒤 기존 보행 시작 진입점만 호출합니다.
        { // 새로운 보행·클로즈업 연출을 만들지 않습니다.
            yield return new WaitForSecondsRealtime(m_Instructions.TransitionDuration + 0.03f); // 실제 안내 패널이 사라지는 시간을 기다립니다.
            RestoreBackground(); m_State = ScreenState.Running; m_First.BeginFromStartScreen(); // 원래 교통과 친구 대기를 복원한 뒤 기존 자동 도입으로 진입합니다.
        } // 이후 제목과 안내는 다시 표시하지 않습니다.

        private void HoldBackground() // 시작 전에 진행하면 안 되는 기존 배경 동작만 보류합니다.
        { // 모든 오브젝트와 시간 배율을 정지하는 방식은 사용하지 않습니다.
            m_Friend2WasPaused = m_Friend2.IsScenarioPaused; m_CarWasPaused = m_Car.IsScenarioPaused; m_SignalWasPaused = m_Signal.IsScenarioPaused; // 실제 기존 개별 정지 상태를 저장합니다.
            m_BackgroundHeld = true; m_Friend2.SetScenarioPaused(true); m_Car.SetScenarioPaused(true); m_Signal.SetScenarioPaused(true); // 머리 추적·마우스·UI는 유지하면서 배경 연출만 대기합니다.
        } // 캐릭터와 차량을 비활성화하여 초기화 경로를 다시 실행하지 않습니다.
        private void RestoreBackground() // 안내 완료나 씬 종료에서 이 화면이 보류한 상태만 돌립니다.
        { // 두 번 호출해도 진행 중인 배경을 다시 초기화하지 않습니다.
            if (!m_BackgroundHeld) return; // 이미 복원한 경우에는 관여하지 않습니다.
            m_BackgroundHeld = false; if (m_Friend2 != null) m_Friend2.SetScenarioPaused(m_Friend2WasPaused); // 살아 있는 원래 친구의 개별 정지를 복원합니다.
            if (m_Car != null) m_Car.SetScenarioPaused(m_CarWasPaused); if (m_Signal != null) m_Signal.SetScenarioPaused(m_SignalWasPaused); // 원래 주행과 자동 신호 타이머를 같은 상태로 이어갑니다.
        } // 첫 번째·두 번째·세 번째 상황의 기록은 변경하지 않습니다.
        private void OnDisable() // 중단된 시작 화면과 전환을 정리합니다.
        { // 게임 도중 새로운 안내를 재생하지 않습니다.
            StopAllCoroutines(); if (m_Title != null) m_Title.Show(false); if (m_Instructions != null) m_Instructions.Show(false); RestoreBackground(); // 실제 패널과 이 화면이 소유한 대기만 해제합니다.
        } // XR Origin과 기존 입력 모듈은 유지합니다.
    } // 제목·조작 안내·기존 첫 상황 연결만 구현합니다.
} // 새로운 안전 상황이나 보상 시스템은 추가하지 않습니다.
