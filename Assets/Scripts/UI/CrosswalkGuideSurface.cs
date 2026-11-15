using System.Collections.Generic; // M 원본 안내 배경의 둥근 외곽과 색 띠 정점을 보관합니다.
using UnityEngine; // M의 동일한 윤곽·음영·그림자 계산을 사용합니다.
using UnityEngine.UI; // 기존 Canvas에 안내 표면만 그립니다.

namespace Y.Crosswalk // 현재 횡단보도 씬의 M 방식 안내에 연결합니다.
{ // M이 사용하는 CorridorBubbleGraphic의 일반 사각형 표면 경로를 재사용합니다.
    [RequireComponent(typeof(CanvasRenderer))] // M 원본과 같이 Canvas 메시를 그릴 렌더러를 함께 연결합니다.
    public sealed class CrosswalkGuideSurface : MaskableGraphic // 말풍선이나 버튼 입력 없이 안내 배경만 표시합니다.
    { // 안내 패널과 작은 선택 표식이 같은 표면 계산을 사용합니다.
        private float m_Radius, m_Border; // M 안내에 지정된 모서리 반경과 테두리 두께입니다.
        public void Configure(Color fill, float radius, float border, Material overlay) // M TutorialPreview.Surface의 실제 값을 전달받습니다.
        { // 그림의 형태와 입력 판정을 분리합니다.
            color = fill; m_Radius = radius; m_Border = border; material = overlay; raycastTarget = false; SetVerticesDirty(); // 기존 원본 재질에 같은 색·테두리·반경을 적용하고 클릭은 통과시킵니다.
        } // 크기는 호출한 안내 RectTransform이 결정합니다.
        protected override void OnPopulateMesh(VertexHelper helper) // M의 일반 안내 표면과 같은 외곽·음영·입체 테두리를 만듭니다.
        { // 모든 음영은 UI 평면 안에 있어 양쪽 눈에서 같은 형태로 보입니다.
            helper.Clear(); Rect rect = rectTransform.rect; if (rect.width <= 0f || rect.height <= 0f) return; List<Vector2> outer = Contour(rect, m_Radius); Vector2 center = rect.center; float depth = m_Border > 1f ? 3f : 1f; Color ink = new Color(0.28f, 0.19f, 0.14f); // M 원본의 표면 반경·잉크색과 안내 전용 깊이를 사용합니다.
            Polygon(helper, Offset(outer, new Vector2(2f, -4f)), new Color(0.2f, 0.12f, 0.08f, 0.12f * color.a)); Color side = Color.Lerp(color, new Color(0.42f, 0.29f, 0.2f, color.a), 0.37f); // 원본과 같은 그림자 위치·투명도와 측면 색을 만듭니다.
            Polygon(helper, Offset(outer, Vector2.down * depth), ink); List<Vector2> face = Expand(outer, center, -m_Border); Polygon(helper, Offset(face, Vector2.down * depth), side); Polygon(helper, outer, ink); Polygon(helper, face, color); // M의 바깥선과 아래쪽 두께 및 본문 종이를 같은 순서로 그립니다.
            float low = float.MaxValue, high = float.MinValue; foreach (Vector2 point in face) { low = Mathf.Min(low, point.y); high = Mathf.Max(high, point.y); } // 위아래 색 띠를 자를 실제 표면 높이입니다.
            Polygon(helper, ClipY(face, Mathf.Lerp(low, high, 0.28f), false), Color.Lerp(color, side, 0.4f)); Polygon(helper, ClipY(face, Mathf.Lerp(low, high, 0.8f), true), Color.Lerp(color, Color.white, 0.32f)); List<Vector2> inner = Expand(face, center, -Mathf.Min(2f, (high - low) * 0.12f)); // 원본의 위쪽 밝은 띠와 아래쪽 음영 및 2 포인트 입체 가장자리를 적용합니다.
            for (int i = 0; i < face.Count; ++i) // M의 같은 광원 방향으로 가장자리 밝기를 결정합니다.
            { // 정점 위치를 바꾸지 않고 색만 평면에 그립니다.
                int next = (i + 1) % face.Count; Vector2 normal = ((face[i] + face[next]) * 0.5f - center).normalized; float light = Vector2.Dot(normal, new Vector2(-0.48f, 0.88f)); Color rim = light > 0.2f ? Color.Lerp(color, Color.white, 0.7f) : light < -0.2f ? Color.Lerp(color, side, 0.78f) : Color.Lerp(color, side, 0.2f); // M 원본의 세 단계 테두리 명암을 사용합니다.
                Quad(helper, face[i], face[next], inner[next], inner[i], rim); // 해당 가장자리 한 조각을 같은 색으로 채웁니다.
            } // 안내 표면의 모든 가장자리를 완성합니다.
        } // 선택 강조는 실제 물체의 기존 강조 컴포넌트가 담당합니다.
        private static List<Vector2> Contour(Rect rect, float radius) // M과 같은 모서리당 10 조각으로 둥근 사각형을 만듭니다.
        { // 안내 배경에는 생각 구름이나 말풍선 꼬리를 추가하지 않습니다.
            List<Vector2> points = new List<Vector2>(); float rounded = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f); // 작은 안내 표식도 모서리가 뒤집히지 않는 반경을 사용합니다.
            for (int corner = 0; corner < 4; ++corner) { Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - rounded : rect.xMin + rounded, corner < 2 ? rect.yMax - rounded : rect.yMin + rounded); for (int i = 0; i <= 10; ++i) { float angle = (corner * 90f + i * 9f) * Mathf.Deg2Rad; points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rounded); } } // 원본과 같은 각도 간격으로 사각형 둘레를 잇습니다.
            return points; // 테두리와 종이 모두 동일한 외곽을 사용합니다.
        } // 외곽 생성에서 실제 UI 크기는 변경하지 않습니다.
        private static List<Vector2> Offset(List<Vector2> points, Vector2 offset) // M의 그림자와 측면에 같은 외곽을 평행 이동합니다.
        { // 원본 외곽을 수정하지 않습니다.
            List<Vector2> moved = new List<Vector2>(points.Count); foreach (Vector2 point in points) moved.Add(point + offset); return moved; // 필요한 색 띠만 별도의 좌표로 그립니다.
        } // 그림자의 위치는 입력 영역에 영향을 주지 않습니다.
        private static List<Vector2> Expand(List<Vector2> points, Vector2 center, float amount) // M 원본의 축별 비율로 테두리 안쪽 외곽을 구합니다.
        { // 둥근 모서리의 원래 점 개수를 유지합니다.
            float x = 0f, y = 0f; foreach (Vector2 point in points) { x = Mathf.Max(x, Mathf.Abs(point.x - center.x)); y = Mathf.Max(y, Mathf.Abs(point.y - center.y)); } Vector2 scale = new Vector2(Mathf.Max(0.1f, (x + amount) / Mathf.Max(x, 0.1f)), Mathf.Max(0.1f, (y + amount) / Mathf.Max(y, 0.1f))); // 원본과 같은 안쪽 축척을 계산합니다.
            List<Vector2> inner = new List<Vector2>(points.Count); foreach (Vector2 point in points) inner.Add(center + Vector2.Scale(point - center, scale)); return inner; // 일정한 테두리 폭을 가진 내부 표면을 반환합니다.
        } // 테두리 계산은 UI 평면 안에서 끝납니다.
        private static List<Vector2> ClipY(List<Vector2> source, float height, bool above) // 원본의 수평 명암 띠만 남기도록 표면을 자릅니다.
        { // 화면 마스크나 클릭 판정에는 사용하지 않습니다.
            List<Vector2> result = new List<Vector2>(); if (source.Count < 3) return result; Vector2 previous = source[source.Count - 1]; bool wasInside = above ? previous.y >= height : previous.y <= height; // 마지막 변에서 시작해 닫힌 외곽을 순회합니다.
            foreach (Vector2 current in source) { bool inside = above ? current.y >= height : current.y <= height; if (inside != wasInside) result.Add(Vector2.LerpUnclamped(previous, current, (height - previous.y) / (current.y - previous.y))); if (inside) result.Add(current); previous = current; wasInside = inside; } return result; // 원본과 같은 교점 계산으로 필요한 색 띠의 다각형을 구합니다.
        } // 잘린 띠는 나중에 원래 표면 위에 그립니다.
        private static void Polygon(VertexHelper helper, List<Vector2> points, Color tint) // M의 중심 삼각형 방식으로 표면을 채웁니다.
        { // 색 띠마다 같은 메시 생성 규칙을 사용합니다.
            if (points.Count < 3) return; Vector2 center = Vector2.zero; foreach (Vector2 point in points) center += point; center /= points.Count; int first = helper.currentVertCount; helper.AddVert(center, tint, Vector2.zero); foreach (Vector2 point in points) helper.AddVert(point, tint, Vector2.zero); // 표면의 중심과 외곽 정점을 추가합니다.
            for (int i = 0; i < points.Count; ++i) helper.AddTriangle(first, first + i + 1, first + (i + 1) % points.Count + 1); // 원본의 삼각형 순서로 닫힌 면을 만듭니다.
        } // 텍스처 없이 같은 M 색상만 표시합니다.
        private static void Quad(VertexHelper helper, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint) // 입체 가장자리 한 조각을 그립니다.
        { // M과 같은 두 삼각형 사각형입니다.
            int first = helper.currentVertCount; helper.AddVert(a, tint, Vector2.zero); helper.AddVert(b, tint, Vector2.zero); helper.AddVert(c, tint, Vector2.zero); helper.AddVert(d, tint, Vector2.zero); helper.AddTriangle(first, first + 1, first + 2); helper.AddTriangle(first, first + 2, first + 3); // 가장자리의 안쪽과 바깥쪽을 연결합니다.
        } // 안내 표면 메시 생성을 마칩니다.
    } // M 안내용 일반 표면 계산을 마칩니다.
} // 다른 씬의 그래픽에는 자동 적용하지 않습니다.
