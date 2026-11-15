using UnityEngine; // M 원본의 마우스와 검지 트리거 아이콘 좌표를 사용합니다.
using UnityEngine.UI; // 안내 카드 안에 입력 장치 아이콘만 그립니다.

namespace Y.Crosswalk // 세 상황이 공유하는 물체 안내에 연결합니다.
{ // M ElevatorVRSelectGraphic의 같은 좌표와 색을 사용합니다.
    [RequireComponent(typeof(CanvasRenderer))] // M 원본과 같이 Canvas 메시를 그릴 렌더러를 함께 연결합니다.
    public sealed class CrosswalkInputGraphic : MaskableGraphic // 아이콘은 클릭 대상이 아닌 조작 설명입니다.
    { // 실제 입력은 기존 PC·XR 코드가 처리합니다.
        private bool m_Controller = true; // 현재 원본처럼 컨트롤러 아이콘을 사용할지 기억합니다.
        public void SetInputMode(bool controller) { if (m_Controller == controller) return; m_Controller = controller; SetVerticesDirty(); } // 실제 PC·VR 모드가 바뀔 때만 표시 메시를 바꿉니다.
        protected override void OnPopulateMesh(VertexHelper helper) // M의 마우스·검지 트리거 아이콘을 같은 도형으로 그립니다.
        { // 원본의 종이색·잉크색·청록색을 유지합니다.
            helper.Clear(); Color ink = new Color(0.28f, 0.19f, 0.14f), paper = new Color(1f, 0.965f, 0.86f), teal = new Color(0.16f, 0.48f, 0.44f); // 배경 안내와 같은 M 팔레트입니다.
            if (!m_Controller) // PC에서는 M 스크린샷의 왼쪽 버튼이 강조된 마우스를 표시합니다.
            { // 외곽과 내부를 같은 원·사각형 조합으로 만듭니다.
                Circle(helper, new Vector2(0f, 5f), 19f, ink); Circle(helper, new Vector2(0f, -12f), 19f, ink); Polygon(helper, ink, new[] { new Vector2(-19f, 5f), new Vector2(19f, 5f), new Vector2(19f, -12f), new Vector2(-19f, -12f) }); // 원본 마우스의 잉크색 테두리입니다.
                Circle(helper, new Vector2(0f, 5f), 16f, paper); Circle(helper, new Vector2(0f, -12f), 16f, paper); Polygon(helper, paper, new[] { new Vector2(-16f, 5f), new Vector2(16f, 5f), new Vector2(16f, -12f), new Vector2(-16f, -12f) }); // 원본의 종이색 마우스 안쪽을 채웁니다.
                Polygon(helper, teal, new[] { new Vector2(-14f, 5f), new Vector2(-12f, 15f), new Vector2(-2f, 19f), new Vector2(-2f, 1f), new Vector2(-14f, 1f) }); Polygon(helper, ink, new[] { new Vector2(-1f, 20f), new Vector2(1f, 20f), new Vector2(1f, 0f), new Vector2(-1f, 0f) }); return; // M과 같은 왼쪽 버튼 강조와 가운데 구분선을 표시합니다.
            } // VR에서는 같은 원본의 검지 트리거 표시를 사용합니다.
            Polygon(helper, ink, new[] { new Vector2(-15f, 16f), new Vector2(12f, 16f), new Vector2(16f, 5f), new Vector2(9f, -26f), new Vector2(-4f, -29f), new Vector2(-12f, -18f), new Vector2(-17f, 4f) }); // 컨트롤러 외곽을 원본 좌표로 그립니다.
            Polygon(helper, paper, new[] { new Vector2(-12f, 13f), new Vector2(10f, 13f), new Vector2(13f, 4f), new Vector2(7f, -23f), new Vector2(-3f, -26f), new Vector2(-9f, -17f), new Vector2(-14f, 4f) }); // 컨트롤러 안쪽 종이색입니다.
            Circle(helper, new Vector2(-2f, 16f), 15f, ink); Circle(helper, new Vector2(-2f, 16f), 11f, paper); Circle(helper, new Vector2(-2f, 16f), 6f, teal); // 컨트롤러 상단의 원형 조작부를 표시합니다.
            Polygon(helper, teal, new[] { new Vector2(5f, 10f), new Vector2(15f, 8f), new Vector2(18f, 1f), new Vector2(9f, 0f) }); Polygon(helper, teal, new[] { new Vector2(-3f, 30f), new Vector2(-1f, 29f), new Vector2(10f, 42f), new Vector2(8f, 43f) }); Circle(helper, new Vector2(11f, 44f), 2.5f, teal); // M과 같은 검지 트리거와 선택 광선을 강조합니다.
        } // 입력 장치 표시는 현재 모드에 따라 바뀝니다.
        private static void Circle(VertexHelper helper, Vector2 center, float radius, Color tint) // 원본의 24 조각 원을 만듭니다.
        { // 마우스와 컨트롤러의 둥근 부분이 같은 해상도를 사용합니다.
            Vector2[] points = new Vector2[24]; for (int i = 0; i < points.Length; ++i) { float angle = i * Mathf.PI * 2f / points.Length; points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; } Polygon(helper, tint, points); // M 원본과 같은 원 둘레를 채웁니다.
        } // 실제 아이콘 크기는 원본 48×62 영역으로 배치합니다.
        private static void Polygon(VertexHelper helper, Color tint, Vector2[] points) // M 원본과 같은 정점 연결 순서로 아이콘을 채웁니다.
        { // 새로운 아이콘 모양이나 애니메이션은 추가하지 않습니다.
            int first = helper.currentVertCount; foreach (Vector2 point in points) helper.AddVert(point, tint, Vector2.zero); for (int i = 1; i < points.Length - 1; ++i) helper.AddTriangle(first, first + i, first + i + 1); // 원본의 첫 정점 기준 삼각형을 사용합니다.
        } // 아이콘 메시 생성의 끝입니다.
    } // M 입력 장치 아이콘을 마칩니다.
} // 다른 씬의 입력 동작에는 관여하지 않습니다.
