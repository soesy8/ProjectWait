using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.SceneTransition
{
    // Rounded paper and dots without textures, temporary sprites or per-frame mesh generation.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TransitionShapeGraphic : MaskableGraphic
    {
        public float radius = 24;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float r = Mathf.Clamp(radius, 0, Mathf.Min(rect.width, rect.height) * .5f);
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = rect.center;
            vertex.uv0 = Vector2.one * .5f;
            mesh.AddVert(vertex);
            const int segments = 10;
            for (int corner = 0; corner < 4; corner++)
            {
                float cx = corner == 0 || corner == 3 ? rect.xMax - r : rect.xMin + r;
                float cy = corner < 2 ? rect.yMax - r : rect.yMin + r;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (corner * 90f + i * 90f / segments) * Mathf.Deg2Rad;
                    vertex.position = new Vector3(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
                    mesh.AddVert(vertex);
                }
            }
            int count = 4 * (segments + 1);
            for (int i = 0; i < count; i++) mesh.AddTriangle(0, i + 1, (i + 1) % count + 1);
        }
    }
}
