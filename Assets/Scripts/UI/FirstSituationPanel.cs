using System.Collections.Generic; // 이미 씬에 있는 활성 패널의 화면 영역을 다른 말풍선 배치에서 함께 사용합니다.
using UnityEngine; // 월드 공간 UI의 크기와 투명도를 프레임에 맞춰 보간합니다.

namespace Y.Crosswalk // 첫 번째 상황의 UI 표현을 개인 작업 영역에 둡니다.
{ // 실제 씬에 저장된 패널만 부드럽게 표시합니다.
    [RequireComponent(typeof(CanvasGroup))] // 자식 텍스트와 버튼의 투명도·선택 상태를 함께 제어합니다.
    public sealed class FirstSituationPanel : MonoBehaviour // 패널 생성 없이 기존 RectTransform의 등장과 퇴장만 담당합니다.
    { // 디자이너가 Inspector에서 전환을 조절할 수 있게 합니다.
        [SerializeField, Min(0.05f), InspectorName("등장·퇴장 시간"), Tooltip("패널 투명도와 확대·축소를 전환하는 초 단위 시간입니다.")] private float m_Duration = 0.28f; // 순간적으로 UI가 튀어나오는 것을 줄입니다.
        [SerializeField, Range(0.7f, 1f), InspectorName("숨김 크기 비율"), Tooltip("숨길 때 원래 RectTransform 크기에 곱할 비율입니다.")] private float m_HiddenScale = 0.84f; // 작은 종이 카드가 펼쳐지는 크기 차이를 분명하게 만듭니다.
        [SerializeField, Range(0f, 0.06f), InspectorName("등장 탄성 강도"), Tooltip("전환 중 원래 크기를 살짝 넘었다가 정착하는 탄성의 세기입니다.")] private float m_EntrancePop = 0.035f; // 최종 UI 크기를 바꾸지 않고 등장과 퇴장의 리듬만 보강합니다.
        [SerializeField, Range(0f, 1f), InspectorName("표시 투명도"), Tooltip("완전히 등장했을 때 CanvasGroup에 적용할 알파입니다.")] private float m_VisibleAlpha = 1f; // 패널별 투명도를 직접 조절하게 합니다.
        public float TransitionDuration => Mathf.Max(0.05f, m_Duration); // 실제 Inspector에 설정한 퇴장 시간이 끝난 뒤 다음 화면으로 연결할 수 있게 합니다.
        private static readonly List<FirstSituationPanel> s_Panels = new List<FirstSituationPanel>(); // 실제 패널만 등록하며 임시 UI는 생성하지 않습니다.
        public static IReadOnlyList<FirstSituationPanel> ActivePanels => s_Panels; // 다른 활성 UI와의 겹침 계산에 기존 패널 목록을 제공합니다.
        public bool IsDisplayed => isActiveAndEnabled && (m_Visible || m_Progress > 0.01f); // 퇴장 중인 패널도 글자가 보이는 동안 배치 영역에서 제외합니다.
        public bool WantsVisible => isActiveAndEnabled && m_Visible; // 다음 단계에서 숨긴 말풍선을 다시 배치하지 않게 합니다.
        private float m_LayoutScale = 1f, m_ShowAfter; // 화면 크기 보정과 이전 패널의 퇴장 완료 시점을 보관합니다.
        private CanvasGroup m_Group; // 씬에 저장된 CanvasGroup을 재사용합니다.
        private Vector3 m_OriginalScale; // 사용자가 배치한 원래 크기를 보존합니다.
        private float m_Progress; // 등장과 퇴장을 같은 진행도에서 이어 갑니다.
        private bool m_Visible; // 현재 요청된 표시 상태입니다.

        private void Awake() // 씬 패널의 편집된 크기를 기준으로 전환을 준비합니다.
        { // 새 Canvas나 Image를 만들지 않습니다.
            m_Group = GetComponent<CanvasGroup>(); m_OriginalScale = transform.localScale; // 원본 컴포넌트와 크기를 저장합니다.
            m_Group.alpha = 0f; m_Group.interactable = false; m_Group.blocksRaycasts = false; // 시작 시 숨긴 패널이 조준을 가로채지 않게 합니다.
        } // 첫 표시는 상황 제어기의 요청을 따릅니다.

        public Vector2 GetVisibleWorldSize() // 말풍선을 화면 안에 배치할 때 완전히 등장한 상태의 실제 크기를 제공합니다.
        { // 등장 애니메이션의 일시적인 축소 때문에 화면 가장자리 계산이 달라지지 않게 합니다.
            RectTransform rectangle = (RectTransform)transform; // 씬에서 편집한 현재 말풍선의 가로·세로 크기를 읽습니다.
            Vector3 scale = Vector3.Scale(m_OriginalScale * m_LayoutScale, transform.parent.lossyScale); // 원래 패널 배율과 월드 Canvas의 실제 배율을 함께 반영합니다.
            return new Vector2(rectangle.rect.width * Mathf.Abs(scale.x), rectangle.rect.height * Mathf.Abs(scale.y)); // 표시가 끝난 말풍선의 월드 미터 크기를 반환합니다.
        } // 패널 위치나 크기를 변경하지 않고 표시 담당자에게 배치에 필요한 값만 전달합니다.

        public void Show(bool visible) // 같은 패널을 여러 번 요청해도 현재 전환에서 자연스럽게 이어 갑니다.
        { // 입력은 퇴장 요청과 동시에 막습니다.
            if (m_Group == null) return; // 씬 종료 중 CanvasGroup이 먼저 제거되면 표시 속성에 접근하지 않습니다.
            if (visible && !m_Visible) // 새 화면의 글자가 이전 화면의 퇴장과 겹치지 않게 합니다.
            { // 함께 표시하도록 요청한 결과 설명과 재시도 패널은 서로 지연시키지 않습니다.
                m_ShowAfter = Time.unscaledTime; // 기존 패널이 없으면 즉시 등장합니다.
                foreach (FirstSituationPanel panel in s_Panels) if (panel != null && panel != this && !panel.m_Visible && panel.m_Progress > 0.01f) m_ShowAfter = Mathf.Max(m_ShowAfter, Time.unscaledTime + panel.m_Progress * panel.TransitionDuration); // 실제 남은 퇴장 시간만 기다립니다.
            } // 같은 말풍선의 문구 변경에는 다시 지연을 추가하지 않습니다.
            m_Visible = visible; // 다음 Update에서 목표 투명도와 크기로 보간합니다.
            if (!visible) { m_Group.interactable = false; m_Group.blocksRaycasts = false; } // 사라지는 선택지를 다시 누를 수 없게 합니다.
        } // 제한 시간이나 자동 선택은 이 컴포넌트에 두지 않습니다.

        public void SetLayoutScale(float scale) { m_LayoutScale = Mathf.Clamp(scale, 0.35f, 1f); } // 실제 RectTransform과 클릭 영역을 같은 비율로 줄여 화면 잘림을 방지합니다.
        public Rect ViewportArea(Camera viewer) // 등장 크기의 변화와 무관한 완전히 표시된 패널 영역입니다.
        { // 네 모서리만 투영하여 별도 배열을 할당하지 않습니다.
            Vector2 size = GetVisibleWorldSize(); Vector3 center = transform.TransformPoint(((RectTransform)transform).rect.center); // 편집한 패널 중심과 실제 월드 크기입니다.
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity; // 화면상 사각형의 경계를 누적합니다.
            for (int i = 0; i < 4; ++i) // 카메라에 대해 기울어진 패널도 실제 네 귀퉁이를 반영합니다.
            { // 뒤쪽 모서리는 가림 영역에 포함하지 않습니다.
                Vector3 point = viewer.WorldToViewportPoint(center + transform.right * (size.x * (i % 2 == 0 ? -0.5f : 0.5f)) + transform.up * (size.y * (i < 2 ? -0.5f : 0.5f))); // 실제 크기의 모서리를 투영합니다.
                if (point.z <= 0f) continue; Vector2 xy = new Vector2(point.x, point.y); min = Vector2.Min(min, xy); max = Vector2.Max(max, xy); // 보이는 모서리로 경계를 확장합니다.
            } // 카메라 뒤의 UI는 앞쪽 말풍선을 막지 않습니다.
            return float.IsInfinity(min.x) ? new Rect() : Rect.MinMaxRect(min.x, min.y, max.x, max.y); // 현재 보이는 전체 패널 영역을 반환합니다.
        } // 버튼의 실제 입력 영역도 같은 RectTransform을 사용합니다.
        private void OnEnable() { if (!s_Panels.Contains(this)) s_Panels.Add(this); } // 씬의 활성 패널을 한 번만 등록합니다.
        private void OnDisable() // 씬 전환과 재활성화에서 숨겨진 입력과 배치 정보를 남기지 않습니다.
        { // 원래 크기는 Awake에서 저장한 값을 유지합니다.
            s_Panels.Remove(this); m_Visible = false; m_Progress = 0f; // 다음 배치가 사라진 패널을 피하지 않게 합니다.
            if (m_Group != null) { m_Group.alpha = 0f; m_Group.interactable = false; m_Group.blocksRaycasts = false; } // 꺼진 UI는 클릭을 차단하지 않습니다.
        } // 다음 표시는 실제 상황 제어기의 요청을 따릅니다.

        private void Update() // 상황 정지 중에도 UI 전환은 정상 속도로 동작합니다.
        { // 전역 시간 배율에 의존하지 않고 실제 경과 시간을 사용합니다.
            m_Progress = Mathf.MoveTowards(m_Progress, m_Visible && Time.unscaledTime >= m_ShowAfter ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.05f, m_Duration)); // 중간 방향 전환에도 깜빡임을 만들지 않습니다.
            float eased = Mathf.SmoothStep(0f, 1f, m_Progress); // 확대와 투명도의 시작·끝을 부드럽게 만듭니다.
            m_Group.alpha = eased * m_VisibleAlpha; // 자식 말풍선·문구·버튼을 함께 페이드합니다.
            float unfold = 1f - Mathf.Pow(1f - m_Progress, 3f); // 초반에는 빠르게 펼치고 마지막에는 천천히 정착하는 크기 곡선입니다.
            float bounce = Mathf.Sin(m_Progress * Mathf.PI) * m_EntrancePop; // 전환 양끝에서는 사라지는 작은 탄성으로 방향 전환도 연속적으로 연결합니다.
            transform.localScale = m_OriginalScale * (m_LayoutScale * (Mathf.Lerp(m_HiddenScale, 1f, unfold) + bounce)); // 원래 배치와 최종 크기를 보존하면서 카드가 펼쳐지는 움직임을 만듭니다.
            m_Group.interactable = m_Visible && m_Progress >= 0.95f; m_Group.blocksRaycasts = m_Group.interactable; // 등장 완료 후에만 버튼 입력을 허용합니다.
        } // 숨겨진 패널은 Hierarchy에 그대로 남아 편집할 수 있습니다.
    } // 씬 패널의 표시 전환을 마칩니다.
} // 다른 씬의 UI에는 자동으로 적용하지 않습니다.
