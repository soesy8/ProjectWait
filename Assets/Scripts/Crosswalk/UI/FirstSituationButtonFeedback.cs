using System.Collections.Generic; // 양손 레이가 같은 버튼을 가리킬 때 각 포인터의 상태를 따로 보관합니다.
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems; // 기존 XR UI 모듈이 전달하는 포인터 이벤트를 수신합니다.

namespace Y.Crosswalk // 첫 번째 상황 버튼의 촉감 표현만 담당합니다.
{ // 실제 선택 처리는 씬 Button의 onClick 연결을 유지합니다.
    public sealed class FirstSituationButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler // 컨트롤러 조준·누름·선택에 대응합니다.
    { // 버튼별 크기 반응을 Inspector에 노출합니다.
        [SerializeField, Range(1f, 1.15f), InspectorName("조준 확대 비율"), Tooltip("버튼을 가리킬 때 글자에만 적용하는 배율입니다.")] private float m_HoverScale = 1.1f;
        [SerializeField, InspectorName("공유 선택 효과음"), Tooltip("씬에 배치된 UI 전용 AudioSource입니다. 클릭을 확정할 때만 짧은 소리를 재생합니다.")] private AudioSource m_ClickAudio; // 모든 버튼이 같은 기존 씬 음원을 공유하여 소리가 중첩 생성되지 않게 합니다.
        [SerializeField, InspectorName("선택 확인 소리"), Tooltip("버튼을 누르는 순간 들려줄 짧고 부드러운 선택 효과음입니다.")] private AudioClip m_ClickClip; // 대사와 경적을 가리지 않는 짧은 음색을 사용합니다.
        private UnityEngine.UI.Selectable m_Selectable; // 비활성 선택지에는 확정 효과음을 내지 않도록 실제 버튼 상태를 읽습니다.
        private RectTransform m_Label;
        private Vector3 m_LabelScale;
        private bool m_Hovered, m_Pressed; // 조준과 눌림 및 실제 선택 가능한 상태에서 시작한 클릭을 구분합니다.
        private readonly HashSet<int> m_HoverPointers = new HashSet<int>(), m_PressedPointers = new HashSet<int>(), m_ArmedPointers = new HashSet<int>(); // 한 손이 나가더라도 다른 손의 조준과 누름 표시를 지우지 않습니다.
        private float m_ClickTime; // 선택 직후 짧은 팽창의 남은 시간입니다.
        private void Awake()
        {
            m_Selectable = GetComponent<UnityEngine.UI.Selectable>();
            var text = GetComponentInChildren<TMP_Text>(true);
            if (text == null || text.transform == transform) return;
            text.raycastTarget = false;
            m_Label = text.rectTransform;
            m_LabelScale = m_Label.localScale;
        }
        public void OnPointerEnter(PointerEventData data) { m_HoverPointers.Add(data.pointerId); m_Hovered = m_HoverPointers.Count > 0; } // 실제 UI 레이가 닿은 포인터를 기록해 버튼을 확대합니다.
        public void OnPointerExit(PointerEventData data) { m_HoverPointers.Remove(data.pointerId); m_PressedPointers.Remove(data.pointerId); m_ArmedPointers.Remove(data.pointerId); m_Hovered = m_HoverPointers.Count > 0; m_Pressed = m_PressedPointers.Count > 0; } // 벗어난 손의 상태만 지우고 계속 가리키는 다른 손의 확대는 유지합니다.
        public void OnPointerDown(PointerEventData data) { if (data.button != PointerEventData.InputButton.Left || m_Selectable != null && (!m_Selectable.IsActive() || !m_Selectable.IsInteractable())) return; m_ArmedPointers.Add(data.pointerId); m_PressedPointers.Add(data.pointerId); m_Pressed = true; } // 마우스나 양손 트리거가 실제 선택 가능한 버튼을 눌렀을 때만 눌림을 표시합니다.
        public void OnPointerUp(PointerEventData data) { m_PressedPointers.Remove(data.pointerId); m_Pressed = m_PressedPointers.Count > 0; } // 놓은 포인터의 눌림만 해제하고 클릭 확정은 기존 이벤트 순서로 받습니다.
        public void OnPointerClick(PointerEventData data) // 기존 버튼 이벤트와 같은 클릭 순간에 크기와 소리 반응을 재생합니다.
        { // 실제 행동 선택과 다음 단계 호출은 원래 Button 연결에 맡깁니다.
            if (!m_ArmedPointers.Remove(data.pointerId)) return; // 같은 포인터가 이 버튼을 누르기 시작한 클릭에만 확인 반응을 적용합니다.
            m_ClickTime = 0.22f; // 선택 확정의 짧은 탄성 반응을 시작합니다.
            if (m_ClickAudio != null && m_ClickClip != null) m_ClickAudio.PlayOneShot(m_ClickClip, 0.6f); // 다른 상황 음원을 끊지 않고 짧은 확인음만 더합니다.
        } // 버튼 명령을 중복 실행하거나 진행 시간을 변경하지 않습니다.
        private void Update() // UI 시간은 캐릭터의 정지와 독립적으로 흐릅니다.
        { // 기존 Button의 색상 전환과 함께 크기만 보조합니다.
            m_ClickTime = Mathf.Max(0f, m_ClickTime - Time.unscaledDeltaTime); // 클릭 반응을 한 번만 끝냅니다.
            float pulse = m_ClickTime > 0f ? Mathf.Sin((1f - m_ClickTime / 0.22f) * Mathf.PI) * 0.07f : 0f; // 클릭이 확정되었음을 더 분명한 짧은 팽창으로 표현합니다.
            float target = (m_Pressed ? 0.94f : m_Hovered ? m_HoverScale : 1f) + pulse; // 눌림과 복원 및 조준 크기의 차이를 선명하게 만듭니다.
            // 버튼 배경은 고정된 판정면입니다. 손을 놓을 때까지 같은 면이 레이를 받습니다.
            if (m_Label) m_Label.localScale = Vector3.Lerp(m_Label.localScale, m_LabelScale * target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        } // 선택 명령을 중복 호출하지 않습니다.
        private void OnDisable() { m_HoverPointers.Clear(); m_PressedPointers.Clear(); m_ArmedPointers.Clear(); m_Hovered = false; m_Pressed = false; m_ClickTime = 0f; if (m_Label) m_Label.localScale = m_LabelScale; }
    } // 버튼 크기 반응을 마칩니다.
} // 프로젝트의 다른 버튼에는 개입하지 않습니다.
