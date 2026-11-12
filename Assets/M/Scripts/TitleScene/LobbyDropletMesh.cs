using System.Collections.Generic;
using UnityEngine;

namespace ProjectWait.M.TitleScene
{
    // Built once per surface sample/configuration, never per animation frame.
    // Fixed low-cost spheres. The shader only animates their radii.
    public static class LobbyDropletMesh
    {
        public struct Seed { public Vector3 position; public Color color; }

        public sealed class Geometry
        {
            public readonly List<Vector3> centers;
            public readonly List<Vector3> radial;
            public readonly List<Color> colors;
            public readonly List<Vector2> styles;
            public readonly List<int> triangles;
            public Bounds bounds;

            public Geometry(int seedCount)
            {
                int vertices = seedCount * (2 + (CapRings - 1) * Sides) * 2;
                centers = new List<Vector3>(vertices); radial = new List<Vector3>(vertices);
                colors = new List<Color>(vertices); styles = new List<Vector2>(vertices);
                triangles = new List<int>(seedCount * (CapRings - 1) * Sides * 12);
            }
        }

        const int Sides = 12, CapRings = 8;

        // Same angular sphere resolution as the connected mode, but no necks,
        // partner metadata, seam duplicates or duplicated pole vertices.
        public static Geometry Build(IReadOnlyList<Seed> seeds, float maximumRadius)
        {
            var data = new Geometry(seeds.Count);
            var normals = new List<Vector3> { Vector3.up };
            var indices = new List<int>();
            for (int ring = 1; ring < CapRings; ring++)
            {
                float theta = ring * Mathf.PI / CapRings;
                for (int side = 0; side < Sides; side++)
                {
                    float phi = side * (2 * Mathf.PI / Sides);
                    normals.Add(new Vector3(Mathf.Cos(phi) * Mathf.Sin(theta), Mathf.Cos(theta), Mathf.Sin(phi) * Mathf.Sin(theta)));
                }
            }
            int bottom = normals.Count;
            normals.Add(Vector3.down);
            for (int side = 0; side < Sides; side++)
            {
                int next = (side + 1) % Sides;
                Triangle(0, 1 + next, 1 + side);
                for (int ring = 0; ring < CapRings - 2; ring++)
                {
                    int a = 1 + ring * Sides + side, c = 1 + ring * Sides + next;
                    Triangle(a, c, a + Sides); Triangle(c, c + Sides, a + Sides);
                }
                Triangle(bottom, bottom - Sides + side, bottom - Sides + next);
            }
            if (seeds.Count > 0) data.bounds = new Bounds(seeds[0].position, Vector3.zero);
            foreach (var seed in seeds)
            {
                data.bounds.Encapsulate(seed.position);
                int offset = data.centers.Count;
                for (int hull = 0; hull < 2; hull++)
                {
                    foreach (var normal in normals)
                    {
                        data.centers.Add(seed.position); data.radial.Add(normal); data.colors.Add(seed.color);
                        data.styles.Add(new Vector2(hull, 0));
                    }
                    int start = offset + hull * normals.Count;
                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        data.triangles.Add(start + indices[i]);
                        data.triangles.Add(start + indices[i + (hull == 0 ? 1 : 2)]);
                        data.triangles.Add(start + indices[i + (hull == 0 ? 2 : 1)]);
                    }
                }
            }
            data.bounds.Expand(maximumRadius * 2 + .1f);
            return data;
            void Triangle(int a, int b, int c) { indices.Add(a); indices.Add(b); indices.Add(c); }
        }

        public static float Radius(float keep, float maximumRadius) => maximumRadius * (1 - Smooth((keep - .72f) / .28f)) * Smooth(keep / .72f);
        static float Smooth(float value) { value = Mathf.Clamp01(value); return value * value * (3 - 2 * value); }
    }
}
