using UnityEngine; // M 원본 조준점과 같은 원·중앙 점의 정점을 계산합니다.
using UnityEngine.UI; // 기존 PC HUD Canvas 안에 조준 메시만 그립니다.

namespace Y.Crosswalk // 횡단보도 씬의 기존 입력과 연결합니다.
{ // M의 ElevatorReticleGraphic과 동일한 도형·색·반경을 사용합니다.
    [RequireComponent(typeof(CanvasRenderer))] // 기존 HUD의 Canvas 렌더러로 표시합니다.
    public sealed class CrosswalkReticleGraphic : MaskableGraphic // 조준 표시는 세계나 UI 선택을 가로채지 않습니다.
    { // 실제 선택 대상 여부만 기존 입력기가 전달합니다.
        private bool m_Focused; // 선택 가능한 물체나 행동 버튼을 중앙에 맞춘 상태입니다.
        public void SetFocused(bool value) // M처럼 선택 가능 여부가 바뀔 때만 메시를 갱신합니다.
        { // 같은 상태에서 매 프레임 새 메시를 만들지 않습니다.
            if (m_Focused == value) return; m_Focused = value; SetVerticesDirty(); // 원의 색과 반경을 현재 조준 상태에 맞게 다시 그립니다.
        } // 카메라 위치나 물체의 강조 상태는 변경하지 않습니다.
        protected override void OnPopulateMesh(VertexHelper helper) // M 원본의 32 조각 원과 중앙 점을 그대로 그립니다.
        { // 좌표는 기존 HUD의 화면 포인트 단위입니다.
            helper.Clear(); Color tint = m_Focused ? new Color(0.38f, 0.87f, 1f, 1f) : new Color(1f, 0.98f, 0.87f, 0.95f); const int segments = 32; float radius = m_Focused ? 7f : 5.5f; // 원본과 같은 선택색·기본색과 반경을 적용합니다.
            for (int i = 0; i < segments; ++i) // 얇은 원을 동일한 32개 사각형으로 나눠 그립니다.
            { // 각 조각의 시작과 끝 방향을 원 둘레에서 구합니다.
                float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments; Vector2 first = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), next = new Vector2(Mathf.Cos(b), Mathf.Sin(b)); int index = helper.currentVertCount; // 이 조각이 사용할 네 정점의 시작 번호입니다.
                helper.AddVert(first * (radius - 1f), tint, Vector2.zero); helper.AddVert(first * (radius + 1f), tint, Vector2.zero); helper.AddVert(next * (radius + 1f), tint, Vector2.zero); helper.AddVert(next * (radius - 1f), tint, Vector2.zero); helper.AddTriangle(index, index + 1, index + 2); helper.AddTriangle(index, index + 2, index + 3); // 원본과 같은 두께 2의 원 조각을 만듭니다.
            } // 원 둘레를 모두 채웁니다.
            int dot = helper.currentVertCount; helper.AddVert(new Vector2(-1f, -1f), tint, Vector2.zero); helper.AddVert(new Vector2(-1f, 1f), tint, Vector2.zero); helper.AddVert(new Vector2(1f, 1f), tint, Vector2.zero); helper.AddVert(new Vector2(1f, -1f), tint, Vector2.zero); helper.AddTriangle(dot, dot + 1, dot + 2); helper.AddTriangle(dot, dot + 2, dot + 3); // M 원본처럼 가운데에 2×2 크기의 작은 점을 표시합니다.
        } // 조준 가능한 대상의 판정은 기존 입력 코드가 담당합니다.
    } // 원본 M 조준점의 표시 규칙을 마칩니다.
} // 다른 씬의 입력이나 HUD에는 자동 적용하지 않습니다.
