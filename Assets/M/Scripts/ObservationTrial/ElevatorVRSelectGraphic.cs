using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.Elevator.ObservationTrial
{
    // Generic controller and ray. The upper-front control marks the index trigger.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ElevatorVRSelectGraphic : MaskableGraphic
    {
        static readonly Color Ink = new Color(.28f, .19f, .14f);
        static readonly Color Paper = new Color(1, .965f, .86f);
        static readonly Color Teal = new Color(.16f, .48f, .44f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Polygon(vh, Ink, new[] { new Vector2(-15, 16), new Vector2(12, 16), new Vector2(16, 5),
                new Vector2(9, -26), new Vector2(-4, -29), new Vector2(-12, -18), new Vector2(-17, 4) });
            Polygon(vh, Paper, new[] { new Vector2(-12, 13), new Vector2(10, 13), new Vector2(13, 4),
                new Vector2(7, -23), new Vector2(-3, -26), new Vector2(-9, -17), new Vector2(-14, 4) });
            Circle(vh, new Vector2(-2, 16), 15, Ink);
            Circle(vh, new Vector2(-2, 16), 11, Paper);
            Circle(vh, new Vector2(-2, 16), 6, Teal);
            Polygon(vh, Teal, new[] { new Vector2(5, 10), new Vector2(15, 8), new Vector2(18, 1), new Vector2(9, 0) });
            Polygon(vh, Teal, new[] { new Vector2(-3, 30), new Vector2(-1, 29), new Vector2(10, 42), new Vector2(8, 43) });
            Circle(vh, new Vector2(11, 44), 2.5f, Teal);
        }

        static void Circle(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            var points = new Vector2[24];
            for (int i = 0; i < points.Length; i++)
            {
                float a = i * Mathf.PI * 2 / points.Length;
                points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            Polygon(vh, tint, points);
        }

        static void Polygon(VertexHelper vh, Color tint, Vector2[] points)
        {
            int first = vh.currentVertCount;
            foreach (var p in points) vh.AddVert(p, tint, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
