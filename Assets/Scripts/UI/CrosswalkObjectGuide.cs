using TMPro; // M 안내와 같은 원본 글꼴·크기의 제목과 조작 설명을 표시합니다.
using UnityEngine; // 기존 관찰 대상의 위치를 안내 표식에 연결합니다.
using UnityEngine.UI; // M의 화면 고정 안내 Canvas와 글자 배치를 사용합니다.

namespace Y.Crosswalk // 현재 세 상황의 기존 물체 선택 상태만 읽습니다.
{ // M TutorialPreview의 화면 안내와 선택 표식 표시 경로를 공유합니다.
    public sealed class CrosswalkObjectGuide : MonoBehaviour // 실제 행동을 새로 만들지 않고 현재 클릭 대상만 안내합니다.
    { // 물체 선택과 상황 진행은 기존 관찰·상황 제어기가 담당합니다.
        private Camera m_Viewer; private CrosswalkDesktopInput m_Input; private Canvas m_Screen, m_MarkerCanvas; private CanvasGroup m_Group; // 기존 카메라와 PC·VR 모드에 맞춰 안내만 표시합니다.
        private RectTransform m_Panel, m_Marker; private TMP_Text m_Title, m_Controls, m_Action; private CrosswalkInputGraphic m_Icon; // M 원본과 같은 제목·입력 아이콘·선택 표식을 보관합니다.
        private FirstSituationObservation m_Target; private bool m_Desktop; // 현재 실제로 선택할 수 있는 대상과 안내 모드입니다.
        public void Initialize(Camera viewer, CrosswalkDesktopInput input, TMP_FontAsset font, Material worldMaterial, Material textMaterial) // 기존 씬의 M 원본 자산만 전달받습니다.
        { // 안내는 World Canvas의 자식으로 만들지 않아 화면 좌표가 월드 패널 배치와 섞이지 않습니다.
            m_Viewer = viewer; m_Input = input; RectTransform screen = MakeRect("M 화면 고정 물체 안내", transform, new Vector2(1280f, 720f), Vector2.zero); m_Screen = screen.gameObject.AddComponent<Canvas>(); m_Screen.worldCamera = viewer; m_Screen.planeDistance = Mathf.Max(0.75f, viewer.nearClipPlane + 0.05f); m_Screen.sortingOrder = 220; // M 원본의 안내 깊이와 정렬 순서를 사용합니다.
            CanvasScaler scaler = screen.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 1f; // 스크린샷과 같은 1280×720 기준 화면 안내 크기를 유지합니다.
            m_Panel = MakeRect("TopRight_Instruction", screen, new Vector2(320f, 158f), new Vector2(-24f, -24f)); m_Panel.anchorMin = m_Panel.anchorMax = m_Panel.pivot = Vector2.one; m_Group = m_Panel.gameObject.AddComponent<CanvasGroup>(); m_Group.blocksRaycasts = m_Group.interactable = false; // 오른쪽 위 24 여백의 읽기 전용 안내입니다.
            m_Panel.gameObject.AddComponent<CrosswalkGuideSurface>().Configure(new Color(1f, 0.965f, 0.86f), 18f, 2.5f, worldMaterial); RectTransform badge = MakeRect("ControllerBadge", m_Panel, new Vector2(56f, 70f), new Vector2(-116f, 0f)); badge.gameObject.AddComponent<CrosswalkGuideSurface>().Configure(new Color(0.79f, 0.89f, 0.76f), 13f, 1f, worldMaterial); // M 원본의 안내 종이와 입력 아이콘 배경을 같은 표면 값으로 표시합니다.
            RectTransform icon = MakeRect("Select_Icon", badge, new Vector2(48f, 62f), Vector2.zero); m_Icon = icon.gameObject.AddComponent<CrosswalkInputGraphic>(); m_Icon.material = worldMaterial; m_Icon.raycastTarget = false; // M과 같은 마우스·검지 트리거 아이콘입니다.
            m_Title = MakeLabel("ObjectName", m_Panel, new Vector2(216f, 42f), 26f, new Color(0.16f, 0.48f, 0.44f), font, textMaterial); m_Title.rectTransform.anchoredPosition = new Vector2(34f, 54f); // 원본의 청록색 제목과 위치를 사용합니다.
            m_Controls = MakeLabel("Instruction", m_Panel, new Vector2(216f, 100f), 22f, new Color(0.28f, 0.19f, 0.14f), font, textMaterial); m_Controls.rectTransform.anchoredPosition = new Vector2(34f, -18f); // 본문 아래의 입력 설명을 같은 22 포인트로 표시합니다.
            m_Marker = MakeRect("Object_Marker", transform, new Vector2(92f, 44f), Vector2.zero); m_MarkerCanvas = m_Marker.gameObject.AddComponent<Canvas>(); m_MarkerCanvas.renderMode = RenderMode.WorldSpace; m_MarkerCanvas.worldCamera = viewer; m_MarkerCanvas.sortingOrder = 140; m_Marker.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false; // 실제 물체 옆의 작은 표식은 버튼처럼 클릭을 가로채지 않습니다.
            m_Marker.gameObject.AddComponent<CrosswalkGuideSurface>().Configure(new Color(0.79f, 0.89f, 0.76f), 13f, 2f, worldMaterial); m_Action = MakeLabel("ActionBadge", m_Marker, new Vector2(76f, 38f), 22f, new Color(0.28f, 0.19f, 0.14f), font, textMaterial); m_Action.alignment = TextAlignmentOptions.Center; // 원본과 같은 민트색 선택 표식과 중앙 글자를 사용합니다.
            RefreshMode(); Hide(); // 실제 과제가 열리기 전에는 화면 안내와 표식을 모두 숨깁니다.
        } // 별도 시작 화면이나 대사 다음 버튼은 만들지 않습니다.
        public void Show(FirstSituationObservation target) // 현재 상황에서 실제로 안내할 물체만 연결합니다.
        { // 대상이 바뀌면 이전 표식 위치나 문구를 가져오지 않습니다.
            m_Target = target; m_Group.alpha = 0f; m_Title.text = target.DisplayName; m_Action.text = target.GuideAction; m_Screen.enabled = true; // 실제 물체 이름과 행동을 원본 안내에 반영합니다.
        } // 선택 결과는 기존 Collider 입력이 처리합니다.
        public void Hide() // 컷씬·응답 선택·종료에서는 물체 안내를 닫습니다.
        { // 숨겨진 표식이 새 장면 위에 남지 않게 합니다.
            m_Target = null; if (m_Group != null) m_Group.alpha = 0f; if (m_Screen != null) m_Screen.enabled = false; if (m_MarkerCanvas != null) m_MarkerCanvas.enabled = false; // 화면 고정 안내와 월드 선택 표식을 함께 정리합니다.
        } // 안내는 입력 상태를 변경하지 않습니다.
        private void RefreshMode() // M처럼 PC는 화면 오버레이, VR은 카메라 화면에 안내를 표시합니다.
        { // 플레이어의 실제 시점은 기존 카메라가 담당합니다.
            m_Desktop = m_Input != null && m_Input.IsDesktopActive; FirstSituationUI.PlaceSharedScreenCanvas(m_Screen, m_Viewer, m_Desktop, transform); m_Icon.SetInputMode(!m_Desktop); m_Controls.text = m_Desktop ? "마우스로 둘러보고\n반짝이는 대상을 클릭" : "양손 레이로 가리키고\n검지 트리거로 선택"; // PC와 VR에서 같은 오른쪽 위 안내를 표시하고 실제 입력 안내만 맞춥니다.
        } // 입력 모드가 바뀌면 같은 안내 그림만 교체합니다.
        private void LateUpdate() // 최종 카메라 위치를 기준으로 화면 안내와 물체 옆 표식을 배치합니다.
        { // 현재 과제가 없으면 새 안내를 띄우지 않습니다.
            if (m_Target == null) return; bool desktop = m_Input != null && m_Input.IsDesktopActive; if (desktop != m_Desktop) RefreshMode(); m_Group.alpha = Mathf.MoveTowards(m_Group.alpha, 1f, Time.unscaledDeltaTime / 1.5f); Vector3 anchor = m_Target.GuidancePosition; Vector3 projected = m_Viewer.WorldToViewportPoint(anchor); // M 원본의 1.5초 안내 페이드와 실제 물체 위치를 사용합니다.
            bool visible = projected.z > m_Viewer.nearClipPlane && projected.x >= 0f && projected.x <= 1f && projected.y >= 0f && projected.y <= 1f; if (!visible) { ShowTargetDirection(projected); return; } m_Action.text = m_Target.GuideAction; m_Marker.sizeDelta = new Vector2(92f, 44f); m_Action.rectTransform.sizeDelta = new Vector2(76f, 38f); // 화면 밖의 대상도 방향을 알려 주고 화면 안에서는 실제 물체 옆의 짧은 행동 표식으로 돌아갑니다.
            Vector3 delta = anchor - m_Viewer.transform.position; float distance = delta.magnitude; if (Physics.Raycast(m_Viewer.transform.position, delta.normalized, out RaycastHit hit, distance + 0.05f, ~(1 << 2), QueryTriggerInteraction.Ignore) && !m_Target.OwnsCollider(hit.collider)) { m_MarkerCanvas.enabled = false; return; } // 실제 월드 물체에 가려진 대상의 표식을 벽 너머로 표시하지 않습니다.
            float unit = Mathf.Min(2f * distance * Mathf.Tan(m_Viewer.fieldOfView * 0.5f * Mathf.Deg2Rad) / 720f * 0.7f, 0.5f / 92f); m_Marker.localScale = Vector3.one * unit; m_Marker.SetPositionAndRotation(anchor + m_Viewer.transform.right * (92f * unit * 0.65f) - delta.normalized * 0.015f, m_Viewer.transform.rotation); m_MarkerCanvas.enabled = true; // M 원본의 화면 비율 배율과 최대 0.5m 폭으로 실제 대상 옆에 표식을 둡니다.
        } // 안내나 표식 자체를 눌러 상황을 넘기는 입력은 없습니다.
        private void ShowTargetDirection(Vector3 projected) // 선택할 물체가 보이지 않아도 어느 쪽인지 기존 표식으로 알려 줍니다.
        { // 화면 가운데에 맞추라는 지시 대신 실제 대상 방향을 표시합니다.
            Vector2 direction = new Vector2(projected.x - 0.5f, projected.y - 0.5f); if (projected.z < 0f) direction = -direction; if (direction.sqrMagnitude < 0.001f) direction = Vector2.left; Vector2 edge = Vector2.one * 0.5f + direction / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y)) * 0.38f; // 카메라 뒤쪽의 대상도 올바른 화면 가장자리 방향으로 바꿉니다.
            m_Action.text = m_Target.DisplayName + (direction.x < 0f ? " ◀" : " ▶"); float width = Mathf.Clamp(m_Action.GetPreferredValues(m_Action.text, float.PositiveInfinity, float.PositiveInfinity).x + 20f, 92f, 280f); m_Marker.sizeDelta = new Vector2(width, 44f); m_Action.rectTransform.sizeDelta = new Vector2(width - 16f, 38f); // 이름이 잘리지 않는 기존 민트색 표식에 방향을 붙입니다.
            const float depth = 1.2f; float unit = 2f * depth * Mathf.Tan(m_Viewer.fieldOfView * 0.5f * Mathf.Deg2Rad) / 720f * 0.7f; m_Marker.localScale = Vector3.one * unit; m_Marker.SetPositionAndRotation(m_Viewer.ViewportToWorldPoint(new Vector3(Mathf.Clamp(edge.x, 0.14f, 0.86f), Mathf.Clamp(edge.y, 0.14f, 0.86f), depth)), m_Viewer.transform.rotation); m_MarkerCanvas.enabled = true; // 둘러보면 찾을 수 있도록 화면 가장자리의 일정한 위치에 안내만 표시합니다.
        } // 표식 자체는 선택 버튼이 아니며 실제 물체를 눌러야 진행합니다.
        private static RectTransform MakeRect(string label, Transform parent, Vector2 size, Vector2 position) // M 원본과 같은 가운데 기준 RectTransform을 만듭니다.
        { // 화면 고정 안내와 월드 표식이 같은 기준 좌표를 사용합니다.
            RectTransform rect = (RectTransform)new GameObject(label, typeof(RectTransform)).transform; rect.gameObject.layer = 5; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.sizeDelta = size; rect.anchoredPosition = position; return rect; // 부모 배율을 유지하며 지정한 안내 크기와 위치를 적용합니다.
        } // 별도 이벤트나 Raycaster는 생성하지 않습니다.
        private static TMP_Text MakeLabel(string label, Transform parent, Vector2 size, float points, Color tint, TMP_FontAsset font, Material material) // M 안내의 동일한 글꼴·크기·굵기를 적용합니다.
        { // 설명을 읽는 클릭이 다른 입력으로 소비되지 않게 합니다.
            TMP_Text text = MakeRect(label, parent, size, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSharedMaterial = material; text.color = tint; text.fontSize = points; text.fontStyle = FontStyles.Bold; text.fontWeight = FontWeight.Bold; text.enableAutoSizing = false; text.extraPadding = true; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.alignment = TextAlignmentOptions.MidlineLeft; text.lineSpacing = 2f; text.margin = Vector4.zero; return text; // 원본의 제목 26·본문 22 포인트와 읽기 정렬을 그대로 사용합니다.
        } // 텍스트 렌더 순서는 전달받은 M의 4201 재질을 따릅니다.
    } // 세 상황 공통 물체 안내를 마칩니다.
} // 실제 이야기 진행은 원래 상황 제어기에 남깁니다.
