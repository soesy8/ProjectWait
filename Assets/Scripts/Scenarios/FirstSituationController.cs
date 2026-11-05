using System.Collections; // 클로즈업·충돌·복원을 중복 없이 순서대로 진행합니다.
using Unity.XR.CoreUtils; // 기존 XR Origin에서 추적 카메라와 플레이어 위치를 읽습니다.
using UnityEngine; // 상황 상태·실제 접촉·씬에 저장된 연출 자산을 제어합니다.
using UnityEngine.Events; // 추후 진행 기능이 완료 상태에 연결할 수 있는 빈 이벤트를 제공합니다.

namespace Y.Crosswalk // 첫 번째 보행 안전 상황만 기존 개인 작업 영역에서 제어합니다.
{ // 두 번째·세 번째 상황의 진행이나 신호·차량 로직은 변경하지 않습니다.
    [DefaultExecutionOrder(400)] // 기존 보행·전화 자세·관절 연출이 적용된 뒤 접촉과 체크포인트를 처리합니다.
    public sealed class FirstSituationController : MonoBehaviour // 보행부터 안전한 완료 또는 사고 후 재시도까지 한 번의 상태 흐름을 관리합니다.
    { // 실제 씬의 기존 구성과 미리 제작한 UI·연출만 연결합니다.
        public enum SituationState { [InspectorName("보행 시작")] Walking, [InspectorName("친구 클로즈업")] Closeup, [InspectorName("주변 상황 정지")] Frozen, [InspectorName("관찰")] Observation, [InspectorName("행동 선택")] Choice, [InspectorName("사고 연출")] Accident, [InspectorName("재시도")] Retry, [InspectorName("안전한 결과")] Safe, [InspectorName("첫 번째 상황 완료")] Complete, [InspectorName("시작 화면 대기")] WaitingToStart } // Inspector에 현재 진행 단계를 명확히 표시합니다.
        [SerializeField, InspectorName("기존 XR Origin"), Tooltip("현재 씬의 XR Origin입니다. 카메라 추적이나 시야각은 변경하지 않습니다.")] private XROrigin m_Origin; // 실제 헤드셋 위치를 읽는 기준입니다.
        [SerializeField, InspectorName("친구 1 이동"), Tooltip("친구 1의 기존 걷기 컴포넌트입니다.")] private FriendCharacterMovement m_Movement; // 루트 이동과 보폭 계산을 재사용합니다.
        [SerializeField, InspectorName("친구 1 연출"), Tooltip("친구 1에 연결한 골격 연출과 체크포인트 담당자입니다.")] private FirstSituationCharacterPerformance m_Performance; // 관찰 정지·충격·감사·복원을 요청합니다.
        [SerializeField, InspectorName("첫 번째 상황 UI"), Tooltip("씬에 미리 배치한 월드 공간 UI입니다.")] private FirstSituationUI m_UI; // 상태에 맞는 실제 패널만 표시합니다.
        [SerializeField, Min(1.5f), InspectorName("자동 보행 도입 시간"), Tooltip("조작 없이 보여 줄 1인칭 보행 도입 장면의 초 단위 길이입니다.")] private float m_IntroductionDuration = 3.2f; // 플레이어 이동을 시작 조건으로 사용하지 않습니다.
        [SerializeField, InspectorName("친구의 관찰 전 대기 한계"), Tooltip("클로즈업이 길어져도 친구가 전봇대 앞에서 더 진행하지 않도록 제한할 인도 지점입니다.")] private Transform m_IntroductionLimit; // 선택 전에 전봇대에 자동으로 도달하지 않게 합니다.
        [SerializeField, InspectorName("1인칭 관찰 배치 지점"), Tooltip("전체 시야 전환 동안 XR Origin 루트 위치만 맞출 기존 인도 지점입니다. 루트 회전과 카메라의 머리 추적은 유지합니다.")] private Transform m_ObservationPlayerPoint; // 플레이어는 기존 관찰 위치에 두고 친구의 실제 출발 경로에서 앞쪽 배치를 조정합니다.
        [SerializeField, InspectorName("첫 번째 상황의 보행 소리"), Tooltip("기존 걸음 위상에 연결된 발소리와 보행 환경음 제어기입니다.")] private FirstSituationWalkAudio m_WalkAudio; // 관찰과 사고 정지에서는 이 상황의 소리도 함께 정리합니다.
        [SerializeField, InspectorName("네 관찰 대상"), Tooltip("친구·휴대전화·전봇대·보도 구간을 각각 연결합니다.")] private FirstSituationObservation[] m_Observations; // 단계별 관찰 가능 여부를 함께 전환합니다.
        [SerializeField, InspectorName("접촉할 전봇대 기둥"), Tooltip("전봇대 프리팹의 실제 기둥 CapsuleCollider입니다.")] private Collider m_Pole; // 다른 물체 접촉을 사고로 처리하지 않습니다.
        [SerializeField, InspectorName("친구 이마 접촉 범위"), Tooltip("Head 아래에 배치한 작은 Trigger BoxCollider입니다. 이 범위가 기둥에 실제로 맞닿아야 충돌합니다.")] private BoxCollider m_ContactProbe; // 시각적 얼굴 앞면과 전봇대의 접촉을 판정합니다.
        [SerializeField, InspectorName("클로즈업 카메라"), Tooltip("RenderTexture만 출력하는 별도 카메라입니다. XR 카메라와 연결하지 않습니다.")] private Camera m_CloseupCamera; // 고정 월드 화면에 얼굴·전화·발걸음을 보여 줍니다.
        [SerializeField, Min(2f), InspectorName("클로즈업 길이(초)"), Tooltip("상반신과 전화에서 발걸음까지 이어지는 연출 시간입니다.")] private float m_CloseupDuration = 4f; // 짧은 영상 뒤 실제 1인칭 관찰로 돌아옵니다.
        [SerializeField, InspectorName("첫 번째 상황 중 보류할 직접 이동"), Tooltip("기존 연속 이동과 텔레포트 Provider입니다. 첫 상황이 끝날 때까지 직접 이동만 보류하고 머리 추적과 선택 입력은 유지합니다.")] private Behaviour[] m_Locomotion; // 자동 도입과 관찰을 자유 이동 입력에 의존하지 않게 합니다.
        [SerializeField, InspectorName("장면 전환 음원"), Tooltip("씬에 미리 배치한 비공간 AudioSource입니다.")] private AudioSource m_TransitionAudio; // 클로즈업 시작과 종료에 짧은 전환음을 냅니다.
        [SerializeField, InspectorName("충돌 음원"), Tooltip("전봇대 근처에 미리 배치한 공간 AudioSource입니다.")] private AudioSource m_ImpactAudio; // 실제 접촉 순간에만 충격음을 냅니다.
        [SerializeField, InspectorName("충격 효과"), Tooltip("미리 제작한 작은 별과 원형 충격 효과의 Transform입니다.")] private Transform m_ImpactEffect; // 실행 중 새 파티클이나 UI를 생성하지 않습니다.
        [SerializeField, InspectorName("현재 첫 번째 상황 단계"), Tooltip("진행 상태를 확인할 수 있습니다. 게임 실행 중 제어기가 순서대로 변경합니다.")] private SituationState m_State; // 한 상태에서만 다음 명령을 수락합니다.
        [SerializeField, InspectorName("첫 번째 상황 완료"), Tooltip("안전한 행동과 감사 연출을 끝내야 켜지며 연결된 두 번째 도입이 이 상태를 확인합니다.")] private bool m_Completed; // 충돌이나 재시도는 완료로 기록하지 않습니다.
        [SerializeField, InspectorName("첫 번째 상황 완료 이벤트"), Tooltip("씬에 연결된 두 번째 상황의 BeginAfterFirstSituation을 실제 완료 후 한 번 호출합니다.")] private UnityEvent m_OnCompleted = new UnityEvent(); // 기존 완료 이벤트를 사용해 다음 도입과 연결합니다.
        [SerializeField, InspectorName("제목·조작 안내 후 시작"), Tooltip("이 씬에서는 시작 화면과 알겠어 버튼을 마친 뒤 기존 자동 도입을 실행합니다.")] private bool m_WaitForStartScreen; // 다른 씬의 기존 자동 시작 설정은 기본값으로 보존합니다.
        [SerializeField, InspectorName("기존 PC 입력"), Tooltip("암전 중 첫 관찰 위치의 PC 방향만 설정합니다. 헤드셋 회전은 변경하지 않습니다.")] private CrosswalkDesktopInput m_DesktopInput; // 별도 시점 제어기를 추가하지 않습니다.
        public bool IsSceneTransitioning => m_UI != null && m_UI.IsTransitioning; // 기존 전환막이 가려지는 동안 입력이 들어오지 않게 합니다.
        private bool m_MenuPoseFrozen; // 첫 Animator 갱신이 끝난 대기 자세를 한 번만 정지합니다.
        public bool IsWaitingForStartScreen => isActiveAndEnabled && m_State == SituationState.WaitingToStart; // 기존 PC 입력도 이 상태에서 시점 회전 없이 UI 클릭만 처리합니다.
        private readonly RaycastHit[] m_ContactHits = new RaycastHit[24]; // 접촉 구간의 작은 상자 쓸기 결과를 재사용합니다.
        private bool[] m_LocomotionEnabled; // 연출 전 이동 Provider의 활성 상태를 각각 보존합니다.
        private Vector3 m_PreviousProbeCenter, m_ContactPoint; // 프레임 사이 접촉과 실제 충격 효과 위치를 보관합니다.
        private bool m_IntroductionPrepared, m_HasCheckpoint, m_QueueFreeze, m_ContactReached, m_RetryRequested, m_LocomotionPaused, m_Started; // 자동 도입·관찰 체크포인트·중복 선택과 복원을 관리합니다.
        private float m_ImpactEffectTime; // 작은 충격 효과의 표시 시간을 별도로 계산합니다.
        public bool IsCompleted => m_Completed; // 다른 진행 기능이 첫 번째 상황 완료만 읽을 수 있게 합니다.
        public SituationState CurrentState => m_State; // 관찰이나 선택 상태를 다른 기능에서 읽을 수 있게 합니다.
        public bool BlocksPlayerTranslation => isActiveAndEnabled && !m_Completed; // 기존 PC 입력도 첫 번째 상황에서는 시점과 선택만 사용하도록 이동 보류 상태를 제공합니다.
        public bool ShouldFrameFriend => isActiveAndEnabled && m_Movement != null && (m_State == SituationState.Safe || m_State == SituationState.Accident); // 실제 친구 시선은 안전·사고 자동 연출 중에만 PC 카메라에 요청합니다.
        public Vector3 FriendViewFocus => m_Movement.transform.position + Vector3.up * 1.1f; // 확대 없이 머리·상체와 주변 동작이 보이도록 기존 친구 위치의 상체 높이를 제공합니다.

        private void Start() // 기존 XR와 실제 씬 UI가 준비되면 별도 조작 없이 도입을 시작합니다.
        { // 머리 추적과 컨트롤러 선택은 유지합니다.
            m_Started = true; m_LocomotionEnabled = new bool[m_Locomotion.Length]; // 직접 이동 설정만 나중에 복원할 수 있게 보관합니다.
            m_ImpactEffect.gameObject.SetActive(false); m_CloseupCamera.enabled = false; // 사고 효과와 별도 카메라는 필요한 구간에서만 표시합니다.
            m_TransitionAudio.spatialBlend = 0f; m_ImpactAudio.spatialBlend = 1f; m_ImpactAudio.dopplerLevel = 0f; // 전환음과 실제 접촉 충격음을 분리합니다.
            SetObservation(false); m_UI.PlaceInFront(m_Origin.Camera.transform); m_WalkAudio.Stop(); // 초기 선택과 남은 소리 없이 보행 도입을 준비합니다.
            if (m_Completed) { m_State = SituationState.Complete; m_UI.ShowSafe(); return; } // 이미 완료된 상황을 다시 자동 시작하지 않습니다.
            if (m_WaitForStartScreen) // 실제 씬에 연결한 제목·조작 안내가 있는 경우만 시작을 기다립니다.
            { // 기존 보행·클로즈업 코루틴은 아직 실행하지 않습니다.
                m_State = SituationState.WaitingToStart; SetLocomotionPaused(true); m_UI.HideAll(); return; // 시점 추적과 UI 입력을 유지하고 직접 이동·교육 연출만 대기합니다.
            } // 다른 씬에서의 기존 자동 시작은 그대로 사용합니다.
            BeginAutomaticIntroduction(); // 대기 화면을 사용하지 않는 구성은 원래 도입으로 진입합니다.
        } // 두 번째 횡단보도 상황에는 시작 명령을 전달하지 않습니다.

        public void BeginFromStartScreen() // 실제 알겠어 버튼이 기존 자동 도입에 진입하는 한 번의 연결점입니다.
        { // 첫 번째 상황의 보행·클로즈업·관찰 로직을 복제하지 않습니다.
            if (!m_Started || !IsWaitingForStartScreen || m_Completed) return; // 초기화 전·중복 클릭·이미 완료한 상황은 다시 시작하지 않습니다.
            BeginAutomaticIntroduction(); // 기존 시작 상태 적용과 1인칭 보행 도입 코루틴을 그대로 호출합니다.
        } // 이후 기존 관찰·사고·성공과 다음 두 상황의 연결을 유지합니다.

        private void BeginAutomaticIntroduction() // 첫 상황의 진입과 도입 중 재활성화를 같은 자동 흐름으로 연결합니다.
        { // 기존 연속 이동과 텔레포트만 보류합니다.
            m_State = SituationState.Walking; SetObservation(false); SetLocomotionPaused(true); // 머리 추적과 레이·마우스 선택은 계속 사용할 수 있습니다.
            m_Movement.SetScenarioApproachLimit(m_IntroductionLimit); // 관찰·선택 전에 전봇대로 자동 진입하지 못하게 합니다.
            StartCoroutine(RunIntroduction()); // 저장된 실제 UI와 별도 연출 카메라를 사용합니다.
        } // 직접 이동을 진행 조건으로 사용하지 않습니다.

        private IEnumerator RunIntroduction() // 1인칭으로 함께 걷는 장면을 안전한 월드 공간 연출 화면으로 보여 줍니다.
        { // 실제 XR 카메라를 전진시키거나 흔들지 않습니다.
            m_UI.PlaceInFront(m_Origin.Camera.transform, 2.15f); // 시작부터 화면을 가리지 않고 보행 도입 화면만 부드럽게 표시합니다.
            yield return null; // 기존 Animator의 첫 실제 자세가 적용된 뒤 도입을 시작합니다.
            if (!m_IntroductionPrepared) { m_Performance.CaptureCheckpoint(); m_IntroductionPrepared = true; } // 도입 취소 시 돌아갈 실제 초기 경로와 전화 자세를 한 번 저장합니다.
            else m_Performance.RestoreCheckpoint(); // 중간에 취소된 보행이 전봇대까지 계속 진행되지 않게 합니다.
            m_Movement.SetScenarioApproachLimit(m_IntroductionLimit); m_Movement.SetScenarioApproachSpeed(0.9f); // 기존 걸음 주기와 전화 사용을 같은 느긋한 속도로 연결합니다.
            m_Performance.Freeze(false); m_Movement.BeginWalk(); m_WalkAudio.SetActive(true); // 친구만 기존 보행으로 자동 출발하고 환경음과 발소리를 함께 시작합니다.
            m_UI.ShowIntroduction(); m_CloseupCamera.enabled = true; // 실제 씬의 영상 화면에서 1인칭 보행 구도를 보여 줍니다.
            PositionIntroductionCamera(); m_UI.Transition(false); // 실제 헤드셋 대신 연출용 카메라의 구도만 준비합니다.
            float elapsed = 0f; // 도입 장면의 독립적인 경과 시간입니다.
            while (elapsed < m_IntroductionDuration && m_Movement.ScenarioLimitRemaining > 0.03f) // 충분한 보행 맥락을 보여 준 뒤 전화 행동으로 관심을 옮깁니다.
            { // 강제 XR 전진이나 카메라 흔들림을 사용하지 않습니다.
                elapsed += Time.unscaledDeltaTime; PositionIntroductionCamera(); // 별도 화면 속 1인칭 카메라만 친구의 발걸음과 함께 진행합니다.
                yield return null; // 실제 머리 추적과 주변 시선 이동은 계속 허용합니다.
            } // 같은 상황의 전화 클로즈업으로 이어 갑니다.
            yield return RunCloseup(); // 도입과 클로즈업을 중복 없이 순서대로 진행합니다.
        } // 헤드셋 시야각이나 추적 컴포넌트는 변경하지 않습니다.

        private void PositionIntroductionCamera() // 기존 인도를 친구와 함께 걷는 별도 영상의 1인칭 구도를 잡습니다.
        { // 실제 XR Origin의 현재 시선이나 위치를 강제로 돌리지 않습니다.
            Vector3 forward = Vector3.ProjectOnPlane(m_Movement.transform.forward, Vector3.up).normalized; // 친구의 실제 보행 방향을 사용해 플레이어가 둘러봐도 영상의 동선은 유지합니다.
            Vector3 right = Vector3.Cross(Vector3.up, forward); // 같은 인도에서 친구의 옆에 선 간격을 계산합니다.
            m_CloseupCamera.transform.position = m_Movement.transform.position - forward * 1.6f - right * 1.25f + Vector3.up * 1.65f; // 화면 속 눈높이에서 앞쪽 친구와 함께 이동하는 장면을 구성합니다.
            m_CloseupCamera.transform.LookAt(m_Movement.transform.position + forward * 0.75f + Vector3.up * 1.12f, Vector3.up); // 영상에서 친구의 행동과 앞의 보행 공간을 함께 보이게 합니다.
        } // 헤드셋의 자연스러운 회전은 기존 XR 카메라가 계속 담당합니다.

        private IEnumerator RunCloseup() // 얼굴·상체·휴대전화·발걸음에 집중한 뒤 실제 1인칭 관찰로 돌아옵니다.
        { // 도입 내내 보류한 직접 이동은 첫 번째 상황 완료까지 유지합니다.
            m_State = SituationState.Closeup; // 전체 화면을 가리지 않고 같은 영상 안에서 전화 클로즈업으로 이어 갑니다.
            Vector3 previousPosition = m_CloseupCamera.transform.position; Quaternion previousRotation = m_CloseupCamera.transform.rotation; // 도입의 마지막 구도를 연속 전환의 출발점으로 저장합니다.
            m_UI.ShowCloseup(); m_TransitionAudio.Play(); // 실제 문구와 짧은 효과음만 바꾸고 영상 패널은 유지합니다.
            float elapsed = 0f; // 전화 사용 장면만의 경과 시간을 계산합니다.
            while (elapsed < m_CloseupDuration && (elapsed < 1.8f || m_Movement.ScenarioLimitRemaining > 0.03f)) // 전화 조작과 부주의한 보행을 같은 장면에서 보여 줍니다.
            { // 연출 카메라만 움직여 XR 강제 줌이나 회전을 피합니다.
                elapsed += Time.unscaledDeltaTime; // 클로즈업의 진행도를 실제 시간으로 유지합니다.
                float shot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.55f, m_CloseupDuration - 0.35f, elapsed)); // 얼굴과 전화에서 손·발걸음이 포함된 구도로 천천히 넓힙니다.
                Vector3 cameraLocal = Vector3.Lerp(new Vector3(-0.72f, 1.24f, 0.94f), new Vector3(-1.02f, 0.83f, 1.8f), shot); // 기존 왼손 전화가 잘 보이는 별도 카메라 위치입니다.
                Vector3 lookLocal = Vector3.Lerp(new Vector3(-0.08f, 1.12f, 0.22f), new Vector3(0f, 0.75f, 0.12f), shot); // 화면과 손 조작을 먼저 보여 주고 발걸음으로 연결합니다.
                Vector3 shotPosition = m_Movement.transform.TransformPoint(cameraLocal); // 전화와 얼굴이 잘 보이는 현재 영상 구도를 구합니다.
                Quaternion shotRotation = Quaternion.LookRotation(m_Movement.transform.TransformPoint(lookLocal) - shotPosition, Vector3.up); // 실제 XR 카메라 대신 영상 카메라만 친구를 향합니다.
                float framing = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.65f)); // 검은 화면이나 흰 점멸 없이 도입에서 가까운 구도로 부드럽게 연결합니다.
                m_CloseupCamera.transform.SetPositionAndRotation(Vector3.Lerp(previousPosition, shotPosition, framing), Quaternion.Slerp(previousRotation, shotRotation, framing)); // 하나의 연속된 연출 화면으로 전화 사용 행동을 보여 줍니다.
                yield return null; // 기존 Animator와 전화 자세가 계속 적용되게 합니다.
            } // 원래 1인칭 관찰 구도로 돌아가는 전환을 시작합니다.
            m_UI.Transition(true); m_TransitionAudio.Play(); // 고개를 돌려도 위치 배치 전환이 보이지 않게 합니다.
            yield return new WaitForSecondsRealtime(m_UI.FadeCoverDuration); // 전체 시야가 충분히 가려진 뒤에만 관찰 위치를 배치합니다.
            m_UI.HideAll(); m_CloseupCamera.enabled = false; m_QueueFreeze = true; // 기존 골격과 전화의 최종 자세를 LateUpdate에서 정지·저장합니다.
            while (m_QueueFreeze) yield return null; // 실제 보행 자세가 저장된 프레임을 기다립니다.
            PlaceObservationPlayer(); EnterObservation(); m_UI.Transition(false); // 가려진 동안 배치한 동일한 인도에서 자유로운 주변 관찰을 시작합니다.
        } // 사고 후 재시도에는 이 위치 배치를 다시 실행하지 않습니다.

        private void PlaceObservationPlayer() // 기존 불투명 전환 동안 원래 인도의 관찰 위치에 XR Origin 루트만 배치합니다.
        { // 플레이어의 방향과 카메라의 로컬 머리 자세는 변경하지 않습니다.
            m_Origin.Origin.transform.position = m_ObservationPlayerPoint.position; m_DesktopInput.SetScenarioView(m_ObservationPlayerPoint); // 암전 중 PC 관찰 방향도 배치 지점에 맞추며 VR 추적은 유지합니다. // 친구를 앞에 배치하기 위해 플레이어를 앞쪽으로 이동시키지 않습니다.
        } // 실제 관찰과 결과 연출 중에는 플레이어 위치를 추가로 변경하지 않습니다.

        private IEnumerator RestoreObservationAfterEnable() // 관찰 이후 상황이 명시적으로 다시 켜지면 같은 체크포인트에서 이어 갑니다.
        { // 취소된 연출의 몸 위치가 잠깐 보이지 않게 가립니다.
            m_UI.Transition(true); yield return new WaitForSecondsRealtime(m_UI.FadeCoverDuration); // 전체 시야 전환이 끝날 때까지 머리 추적은 유지합니다.
            m_Performance.RestoreCheckpoint(); PlaceObservationPlayer(); // 같은 관찰 지점의 친구와 초기 관찰 배치를 복원합니다.
            EnterObservation(); m_UI.Transition(false); // 제한 시간 없이 다시 관찰·선택할 수 있게 합니다.
        } // 정상 사고 재시도에서는 XR Origin을 움직이지 않습니다.

        private void LateUpdate() // 최종 전화 자세에서 체크포인트와 실제 접촉을 처리합니다.
        { // 충격 순간까지 기존 이동의 최종 루트 위치를 기준으로 삼습니다.
            if (m_State == SituationState.WaitingToStart && !m_MenuPoseFrozen) // 제목 화면에서는 보행 전 대기 자세만 유지합니다.
            { // 최초 Animator와 휴대전화 관절이 실제로 한 프레임 적용된 뒤 정지합니다.
                m_MenuPoseFrozen = true; m_Performance.Freeze(true); // 초기화되지 않은 애니메이션 상태를 도입 체크포인트로 저장하지 않게 합니다.
            } // 기존 관찰 정지와 접촉 계산은 아래 흐름을 그대로 사용합니다.
            if (m_QueueFreeze) // 클로즈업이 끝난 뒤 한 번만 저장합니다.
            { // Animator와 휴대전화 자세가 모두 적용된 프레임을 보관합니다.
                m_State = SituationState.Frozen; m_Performance.CaptureCheckpoint(); // 친구의 경로와 실제 전화 자세를 함께 정지합니다.
                m_WalkAudio.Stop(); // 정지한 보행과 같은 시점에 발소리와 환경음을 정리합니다.
                m_HasCheckpoint = true; m_QueueFreeze = false; // 재시도 가능 여부와 저장 완료를 기록합니다.
            } // 관찰 중에는 접촉 사고를 계산하지 않습니다.
            if (m_State != SituationState.Accident) return; // 위험 선택 이후에만 전봇대 충돌을 허용합니다.
            if (!m_ContactReached) DetectPoleContact(); // 실제 이마 접촉 또는 프레임 사이 쓸기 접촉을 확인합니다.
            if (!m_ContactReached) return; // 실제 접촉 전에는 효과음이나 넘어짐을 시작하지 않습니다.
            m_ImpactEffectTime += Time.unscaledDeltaTime; // 작은 충격 효과만 정상 시간으로 진행합니다.
            m_ImpactEffect.gameObject.SetActive(m_ImpactEffectTime < 0.55f); // 슬로우모션 동안에도 효과가 계속 남아 있지 않게 합니다.
            m_ImpactEffect.localScale = Vector3.one * (0.0012f * Mathf.Lerp(0.65f, 1.5f, Mathf.Clamp01(m_ImpactEffectTime / 0.55f))); // 별과 원형 효과를 작게 퍼뜨립니다.
        } // 친구 2나 자동차의 접촉을 첫 번째 사고로 처리하지 않습니다.

        private void DetectPoleContact() // 이마 앞면의 실제 Collider 경계와 전봇대가 맞닿는 순간을 찾습니다.
        { // 프레임이 길어져도 작은 접촉 상자가 기둥을 건너뛰지 않게 합니다.
            Vector3 center = m_ContactProbe.transform.TransformPoint(m_ContactProbe.center); // 현재 숙인 머리 앞의 실제 접촉 중심입니다.
            Vector3 half = Vector3.Scale(m_ContactProbe.size, m_ContactProbe.transform.lossyScale) * 0.5f; // 기존 머리 회전과 크기를 반영한 상자 반길이입니다.
            Vector3 step = center - m_PreviousProbeCenter; // 직전 프레임부터 실제로 이동한 거리를 구합니다.
            float nearest = float.PositiveInfinity; // 여러 쓸기 결과 중 실제 기둥의 가장 이른 접촉만 선택합니다.
            if (step.sqrMagnitude > 0.000001f) // 이동이 있을 때만 연속 접촉을 계산합니다.
            { // 입력 레이나 관찰 Collider는 접촉 대상에서 제외합니다.
                int count = Physics.BoxCastNonAlloc(m_PreviousProbeCenter, half, step.normalized, m_ContactHits, m_ContactProbe.transform.rotation, step.magnitude, ~0, QueryTriggerInteraction.Ignore); // 이마 상자를 실제 이동 거리만큼 쓸어 기둥을 찾습니다.
                for (int i = 0; i < count; ++i) if (m_ContactHits[i].collider == m_Pole && m_ContactHits[i].distance < nearest) { nearest = m_ContactHits[i].distance; m_ContactPoint = m_ContactHits[i].point; } // 다른 Collider는 사고로 처리하지 않습니다.
                if (!float.IsPositiveInfinity(nearest)) m_Movement.transform.position -= step.normalized * Mathf.Max(0f, step.magnitude - nearest); // 접촉 경계를 넘어간 거리만 되돌려 실제 맞닿은 위치에 멈춥니다.
            } // 이미 겹친 상태로 시작한 프레임도 실제 Collider 형상으로 정리합니다.
            bool overlapping = Physics.ComputePenetration(m_ContactProbe, m_ContactProbe.transform.position, m_ContactProbe.transform.rotation, m_Pole, m_Pole.transform.position, m_Pole.transform.rotation, out Vector3 separation, out float depth); // 머리와 기둥의 실제 겹침을 계산합니다.
            if (overlapping) { m_Movement.transform.position += separation * depth; m_ContactPoint = m_Pole.ClosestPoint(center); } // 관통한 만큼만 분리하여 멀리 떨어진 위치에서 충돌하지 않게 합니다.
            m_PreviousProbeCenter = m_ContactProbe.transform.TransformPoint(m_ContactProbe.center); // 조정된 실제 접촉 위치를 다음 프레임 기준으로 보관합니다.
            if (!overlapping && float.IsPositiveInfinity(nearest)) return; // 실제 접촉이 없으면 기존 보행을 계속합니다.
            m_ContactReached = true; m_Performance.Freeze(true); // 첫 접촉 한 번에서 이동과 전화 자세를 즉시 멈춥니다.
            m_WalkAudio.Stop(); // 실제 접촉 뒤에는 걷는 소리 대신 충격음만 들려줍니다.
            m_ImpactEffectTime = 0f; m_ImpactEffect.position = m_ContactPoint; // 충격 효과를 엉뚱한 루트 원점 대신 접촉 지점에 둡니다.
            m_ImpactEffect.rotation = Quaternion.LookRotation(m_ContactPoint - m_Origin.Camera.transform.position); // 충격 효과는 실제 플레이어 카메라에서 보이는 방향으로 표시합니다.
            m_ImpactAudio.Play(); m_UI.PlayDangerCue(); // 실제 이마 접촉 순간에 기존 충격음과 주황 주변광을 함께 재생합니다.
        } // 거리 임계값만으로 사고를 미리 발생시키지 않습니다.

        private void EnterObservation() // 복원 또는 첫 정지 이후 같은 관찰 상태로 진입합니다.
        { // 행동 선택을 자동으로 시작하지 않습니다.
            m_State = SituationState.Observation; m_RetryRequested = false; // 이전 실패의 재시도 요청을 지웁니다.
            m_WalkAudio.Stop(); // 관찰과 재시도에서는 상황 오디오를 진행시키지 않습니다.
            SetObservation(true); m_UI.PlaceInFront(m_Origin.Camera.transform); m_UI.ShowObservation(FinishObservation); // 첫 번째 상황의 네 단서와 기존 선택 단계만 공통 관찰 완료 버튼에 연결합니다.
        } // 플레이어가 관찰을 마쳤다고 선택할 때까지 기다립니다.
        public void FinishObservation() { if (m_State != SituationState.Observation) return; m_State = SituationState.Choice; SetObservation(false); m_UI.ShowChoice(); } // 관찰 완료 버튼으로만 행동 선택을 표시합니다.
        public void ResumeObservation() { if (m_State == SituationState.Choice) EnterObservation(); } // 선택 전 다시 주변을 살펴볼 수 있게 합니다.
        public void ChooseUnsafe() { if (m_State != SituationState.Choice) return; m_State = SituationState.Accident; m_UI.HideAll(); StartCoroutine(RunUnsafe()); } // 상태를 먼저 잠가 위험 선택을 한 번만 실행합니다.
        public void ChooseSafe() { if (m_State != SituationState.Choice) return; m_State = SituationState.Safe; m_UI.HideAll(); StartCoroutine(RunSafe()); } // 상태를 먼저 잠가 경고 선택을 한 번만 실행합니다.
        public void RequestRetry() { if (m_State == SituationState.Retry) m_RetryRequested = true; } // 결과 설명 뒤의 버튼으로 복원을 조금 빨리 시작할 수 있게 합니다.

        private IEnumerator RunUnsafe() // 위험 행동을 선택한 경우에만 보행·접촉·원인 설명·복원을 순서대로 실행합니다.
        { // 관찰 상태의 정확한 체크포인트는 덮어쓰지 않습니다.
            m_ContactReached = false; m_ImpactEffectTime = 0f; m_Movement.SetScenarioApproachSpeed(0.8f); // 이전 사고의 접촉 기록과 연출 시간을 지웁니다.
            m_Movement.SetScenarioApproachLimit(null); m_WalkAudio.SetActive(true); // 위험 행동을 확정한 뒤에만 관찰 전 한계를 해제하고 보행을 재개합니다.
            m_PreviousProbeCenter = m_ContactProbe.transform.TransformPoint(m_ContactProbe.center); m_Performance.Freeze(false); // 같은 전화 사용 보행을 관찰 지점부터 재개합니다.
            float deadline = Time.unscaledTime + Vector3.Distance(m_Movement.transform.position, m_Pole.bounds.center) / 0.8f + 4f; // 실제 전봇대 접근 거리에 가속 여유를 더한 상한입니다.
            while (!m_ContactReached && m_Movement.isActiveAndEnabled && Time.unscaledTime < deadline) yield return null; // 실제 접촉이 발생할 때만 충돌 동작으로 이어갑니다.
            if (!m_ContactReached) { Debug.LogWarning("전봇대 접근이 접촉까지 이어지지 않아 관찰 상태를 복원합니다.", this); yield return RestoreResultObservation(); yield break; } // 접촉 실패를 사고·성공으로 건너뛰지 않습니다. // 실제 기둥 접촉 전에는 충돌 동작을 재생하지 않습니다.
            yield return m_Performance.PlayImpact(); // 머리·상체·팔·양다리의 큰 휘청임과 순간 슬로우모션을 끝까지 보여 줍니다.
            m_State = SituationState.Retry; m_UI.PlaceInFront(m_Origin.Camera.transform); m_UI.ShowUnsafe(); // 사고 원인과 같은 관찰 지점으로 돌아간다는 안내를 표시합니다.
            float elapsed = 0f; // 결과 설명에만 짧은 표시 시간을 적용합니다.
            while (elapsed < 4f && !m_RetryRequested) { elapsed += Time.unscaledDeltaTime; yield return null; } // 관찰·선택에는 제한 시간을 두지 않고 결과 뒤에만 자동 복원합니다.
            yield return RestoreResultObservation(); // 사고 설명 뒤 같은 관찰 체크포인트로 돌아갑니다.
        } // 접촉과 결과가 실제 완료된 경우만 설명을 표시합니다.

        private IEnumerator RestoreResultObservation() // 접촉 실패·우회 실패·사고 재시도의 복원을 같은 순서로 처리합니다.
        { // 이전 효과나 숨김 입력이 다음 관찰에 남지 않게 합니다.
            m_UI.HideAll(); SetObservation(false); m_WalkAudio.Stop(); SetLocomotionPaused(true); m_UI.Transition(true); m_TransitionAudio.Play(); // 복원할 순간을 기존 짧은 전환으로 가립니다.
            yield return new WaitForSecondsRealtime(m_UI.FadeCoverDuration); // 위치와 자세는 가려진 뒤에만 변경합니다.
            m_Performance.RestoreCheckpoint(); m_ImpactEffect.gameObject.SetActive(false); m_ImpactAudio.Stop(); m_ContactReached = false; m_ImpactEffectTime = 0f; // 관찰 당시의 신체·전화·이동과 충격 효과를 복원합니다.
            EnterObservation(); m_UI.Transition(false); // 관찰과 선택은 다시 제한 시간 없이 진행합니다.
        } // 완료 기록은 실패 복원에서 켜지지 않습니다.

        private IEnumerator RunSafe() // 안전한 경고를 선택하면 위험 전에 멈추고 기존 얼굴의 미소로 감사하게 합니다.
        { // 충돌 판정은 Accident 상태에서만 실행되므로 이 경로에서는 사고가 시작되지 않습니다.
            m_WalkAudio.SetActive(true); // 실제 우회 보행의 발걸음과 환경음을 기존 걸음 위상에 연결합니다.
            yield return m_Performance.PlaySafe(m_Origin.Camera.transform, m_Pole, () => // 우회·방향 전환·감사를 하나의 순서로 기다리며 실제 감사 시점을 직접 전달받습니다.
            { // 캐릭터 연출이 알려 주는 감사 시작점에 말풍선과 음향을 함께 연결합니다.
                m_WalkAudio.Stop(); // 친구가 플레이어에게 돌아선 순간 보행 소리를 멈춥니다.
                m_UI.ShowThanks(m_Movement.transform); // 손 흔들기와 고개 끄덕임이 시작되는 프레임에 읽을 수 있는 감사 말풍선을 표시합니다.
            }); // 감사 동작이 끝나기 전에는 완료 설명으로 넘어가지 않습니다.
            m_WalkAudio.Stop(); // 우회 구간이 취소된 경우에도 보행 소리가 남지 않게 합니다.
            if (!m_Performance.HasCompletedSafeAvoidance) { yield return RestoreResultObservation(); yield break; } // 우회 실패도 위치를 드러내며 순간 복원하지 않습니다. // 중단된 경로나 연출을 완료로 기록하지 않습니다.
            m_Completed = true; m_State = SituationState.Complete; m_UI.ShowSafe(); // 첫 번째 상황 완료만 기록하고 안전한 이유를 표시합니다.
            SetLocomotionPaused(false); // 첫 상황이 보류한 이동 설정을 복원한 뒤 다음 도입이 필요한 보류 상태를 직접 인계받게 합니다.
            m_OnCompleted.Invoke(); // 실제 씬에 연결된 두 번째 상황 도입을 한 번만 시작합니다.
        } // 두 번째 상황의 호출과 관찰 정지는 별도 제어기가 담당합니다.

        private void SetObservation(bool available) { foreach (FirstSituationObservation target in m_Observations) if (target != null) target.SetAvailable(available); } // 씬 종료 중 먼저 파괴된 관찰 대상을 건너뛰고 남은 입력을 정리합니다.
        private void SetLocomotionPaused(bool paused) // 기존 보행 Provider의 활성 상태를 연출 전후로 보존합니다.
        { // 머리 추적·양손 조준·선택 컴포넌트는 이 목록에 넣지 않습니다.
            if (m_LocomotionPaused == paused) return; // 중복 잠금으로 원래 활성 상태를 덮어쓰지 않습니다.
            for (int i = 0; i < m_Locomotion.Length; ++i) // 해제 중인 씬에서는 이동 Provider도 다른 순서로 파괴될 수 있습니다.
            { // 살아 있는 기존 이동 기능의 상태만 저장하거나 복원합니다.
                if (m_Locomotion[i] == null) continue; // 먼저 해제된 Provider의 enabled 속성에 접근하지 않습니다.
                if (paused) m_LocomotionEnabled[i] = m_Locomotion[i].enabled; // 잠글 때만 원래 활성 상태를 저장합니다.
                m_Locomotion[i].enabled = paused ? false : m_LocomotionEnabled[i]; // 연출이 끝나면 살아 있는 Provider의 기존 상태를 되돌립니다.
            } // 남은 Provider 정리를 계속합니다.
            m_LocomotionPaused = paused; // 다음 잠금·복원 요청의 기준 상태를 저장합니다.
        } // Time.timeScale이나 카메라 추적은 변경하지 않습니다.

        private void OnDisable() // 씬 종료나 상황 비활성화로 중간 연출이 남지 않게 정리합니다.
        { // 아직 Start하지 않은 씬 편집 상태는 처리하지 않습니다.
            if (!m_Started) return; // 초기화되지 않은 이동 상태 배열에 접근하지 않습니다.
            StopAllCoroutines(); SetObservation(false); SetLocomotionPaused(false); // 진행과 관찰 입력을 정리하고 기존 플레이어 이동을 복원합니다.
            if (m_CloseupCamera != null) m_CloseupCamera.enabled = false; // 먼저 파괴되지 않은 연출 카메라 출력만 중지합니다.
            if (m_ImpactEffect != null) m_ImpactEffect.gameObject.SetActive(false); // 씬에 남아 있는 충격 효과만 숨깁니다.
            if (m_TransitionAudio != null) m_TransitionAudio.Stop(); // 살아 있는 전환 음원만 정지합니다.
            if (m_ImpactAudio != null) m_ImpactAudio.Stop(); // 살아 있는 충격 음원만 정지합니다.
            if (m_WalkAudio != null) m_WalkAudio.Stop(); // 자동 도입과 우회의 보행 소리도 함께 초기화합니다.
            if (m_Performance != null) m_Performance.Freeze(true); // 친구가 먼저 제거되지 않았을 때만 연출을 정지합니다.
            if (m_Movement != null) BlenderRigPose.ResetFace(m_Movement.transform); // 상황을 중단하면 친구 1의 대사와 결과 표정도 끝냅니다.
            if (m_UI != null) { m_UI.HideAll(); m_UI.Transition(false); } // 살아 있는 UI 제어기의 표시와 전환을 정리합니다.
            m_QueueFreeze = false; // 오브젝트 해제 순서에 관계없이 대기 중인 체크포인트 요청을 취소합니다.
        } // 원본 신호등과 자동차는 활성 상태를 유지합니다.

        private void OnEnable() // 상황을 명시적으로 다시 켰을 때 중간 코루틴 없이 안전한 진입점으로 돌아갑니다.
        { // 최초 활성화의 초기화는 Start에서 수행합니다.
            if (!m_Started || m_Completed) return; // 시작 전이나 이미 완료된 상황을 자동 재실행하지 않습니다.
            SetLocomotionPaused(true); // 재진입도 직접 이동 조작 없이 같은 상황 흐름을 사용합니다.
            if (m_State == SituationState.WaitingToStart) { m_MenuPoseFrozen = false; return; } // 시작 화면 대기 중 재활성화되어도 버튼 없이 도입을 시작하지 않습니다.
            if (m_HasCheckpoint) { m_State = SituationState.Frozen; StartCoroutine(RestoreObservationAfterEnable()); } // 관찰 체크포인트는 가려진 배치 전환 뒤에 복원합니다.
            else BeginAutomaticIntroduction(); // 아직 도입 중이었다면 저장된 초기 보행 상태부터 다시 자동으로 시작합니다.
        } // 기존 XR Origin의 위치는 어느 재진입 경로에서도 되돌리지 않습니다.
    } // 첫 번째 보행 안전교육의 상태 흐름을 마칩니다.
} // 다음 상황들은 완료 기록만 추후 읽도록 분리합니다.
