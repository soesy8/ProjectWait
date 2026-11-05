using UnityEngine; // 버튼의 기존 RectTransform 크기를 부드럽게 조절합니다.
using UnityEngine.EventSystems; // 기존 XR UI 모듈이 전달하는 포인터 이벤트를 수신합니다.

namespace Y.Crosswalk // 첫 번째 상황 버튼의 촉감 표현만 담당합니다.
{ // 실제 선택 처리는 씬 Button의 onClick 연결을 유지합니다.
    public sealed class FirstSituationButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler // 컨트롤러 조준·누름·선택에 대응합니다.
    { // 버튼별 크기 반응을 Inspector에 노출합니다.
        [SerializeField, Range(1f, 1.15f), InspectorName("조준 확대 비율"), Tooltip("버튼을 가리킬 때 원래 크기에 곱할 배율입니다.")] private float m_HoverScale = 1.07f; // 조준한 선택지가 분명히 떠오르도록 기존 버튼의 크기 반응을 강화합니다.
        [SerializeField, InspectorName("공유 선택 효과음"), Tooltip("씬에 배치된 UI 전용 AudioSource입니다. 클릭을 확정할 때만 짧은 소리를 재생합니다.")] private AudioSource m_ClickAudio; // 모든 버튼이 같은 기존 씬 음원을 공유하여 소리가 중첩 생성되지 않게 합니다.
        [SerializeField, InspectorName("선택 확인 소리"), Tooltip("버튼을 누르는 순간 들려줄 짧고 부드러운 선택 효과음입니다.")] private AudioClip m_ClickClip; // 대사와 경적을 가리지 않는 짧은 음색을 사용합니다.
        private UnityEngine.UI.Selectable m_Selectable; // 비활성 선택지에는 확정 효과음을 내지 않도록 실제 버튼 상태를 읽습니다.
        private Vector3 m_BaseScale; // 편집된 원래 버튼 크기를 보존합니다.
        private bool m_Hovered, m_Pressed, m_ClickArmed; // 조준과 눌림 및 실제 선택 가능한 상태에서 시작한 클릭을 구분합니다.
        private float m_ClickTime; // 선택 직후 짧은 팽창의 남은 시간입니다.
        private void Awake() { m_BaseScale = transform.localScale; m_Selectable = GetComponent<UnityEngine.UI.Selectable>(); } // 원본 버튼 크기와 실제 선택 가능 상태를 함께 준비합니다.
        public void OnPointerEnter(PointerEventData data) { m_Hovered = true; } // 기존 XR 조준이 들어오면 쿠션을 조금 부풀립니다.
        public void OnPointerExit(PointerEventData data) { m_Hovered = false; m_Pressed = false; m_ClickArmed = false; } // 버튼 밖으로 벗어난 클릭에는 눌림과 확정 반응을 남기지 않습니다.
        public void OnPointerDown(PointerEventData data) { m_ClickArmed = data.button == PointerEventData.InputButton.Left && (m_Selectable == null || m_Selectable.IsActive() && m_Selectable.IsInteractable()); m_Pressed = m_ClickArmed; } // 패널을 닫기 전의 실제 버튼 상태와 기본 선택 입력으로 클릭의 유효성을 기록합니다.
        public void OnPointerUp(PointerEventData data) { m_Pressed = false; } // 선택 입력을 놓으면 기본 높이로 돌아갑니다.
        public void OnPointerClick(PointerEventData data) // 기존 버튼 이벤트와 같은 클릭 순간에 크기와 소리 반응을 재생합니다.
        { // 실제 행동 선택과 다음 단계 호출은 원래 Button 연결에 맡깁니다.
            if (!m_ClickArmed) return; // 사용할 수 없거나 조준을 벗어난 버튼은 확정 효과를 재생하지 않습니다.
            m_ClickArmed = false; // 같은 입력에서 선택 효과음을 한 번만 재생합니다.
            m_ClickTime = 0.22f; // 선택 확정의 짧은 탄성 반응을 시작합니다.
            if (m_ClickAudio != null && m_ClickClip != null) m_ClickAudio.PlayOneShot(m_ClickClip, 0.6f); // 다른 상황 음원을 끊지 않고 짧은 확인음만 더합니다.
        } // 버튼 명령을 중복 실행하거나 진행 시간을 변경하지 않습니다.
        private void Update() // UI 시간은 캐릭터의 정지와 독립적으로 흐릅니다.
        { // 기존 Button의 색상 전환과 함께 크기만 보조합니다.
            m_ClickTime = Mathf.Max(0f, m_ClickTime - Time.unscaledDeltaTime); // 클릭 반응을 한 번만 끝냅니다.
            float pulse = m_ClickTime > 0f ? Mathf.Sin((1f - m_ClickTime / 0.22f) * Mathf.PI) * 0.07f : 0f; // 클릭이 확정되었음을 더 분명한 짧은 팽창으로 표현합니다.
            float target = (m_Pressed ? 0.94f : m_Hovered ? m_HoverScale : 1f) + pulse; // 눌림과 복원 및 조준 크기의 차이를 선명하게 만듭니다.
            transform.localScale = Vector3.Lerp(transform.localScale, m_BaseScale * target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime)); // 프레임 수에 상관없이 부드럽게 변화시킵니다.
        } // 선택 명령을 중복 호출하지 않습니다.
        private void OnDisable() { m_Hovered = false; m_Pressed = false; m_ClickArmed = false; m_ClickTime = 0f; transform.localScale = m_BaseScale; } // 재시도와 화면 종료에 이전 조준과 클릭 반응을 남기지 않습니다.
    } // 버튼 크기 반응을 마칩니다.
} // 프로젝트의 다른 버튼에는 개입하지 않습니다.
