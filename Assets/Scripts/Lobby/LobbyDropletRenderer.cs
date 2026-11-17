using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectWait.M.TitleScene
{
    // Owns one transient mesh/renderer, not one GameObject per droplet. Original
    // meshes, texture import settings and shared materials are never modified.
    sealed class LobbyDropletRenderer : IDisposable
    {
        struct Candidate { public Vector3 position, normal; public Color color; }
        readonly LobbyDissolveTest owner;
        readonly GameObject root;
        readonly Mesh mesh;
        readonly MeshRenderer renderer;
        readonly Material material;
        readonly List<LobbyDropletMesh.Seed> seeds = new List<LobbyDropletMesh.Seed>();
        Vector3 sampledOrigin;
        int sampledSignature;
        bool sampled, disposed;

        public int SeedCount => seeds.Count;
        // Diagnostic only: do not evaluate every seed on every animation frame.
        public int ActiveCount
        {
            get
            {
                if (disposed || !renderer.enabled) return 0;
                int count = 0;
                foreach (var seed in seeds)
                    if (LobbyDropletMesh.Radius(owner.VisibilityAt(seed.position), ActualSpacing * owner.dropletRadius) >= .004f) count++;
                return count;
            }
        }
        public int VertexCount => mesh && renderer.enabled ? mesh.vertexCount : 0;
        public int CachedVertexCount => mesh ? mesh.vertexCount : 0;
        public int TopologyBuildCount { get; private set; }
        public double LastRefreshMilliseconds { get; private set; }
        public double LastBuildMilliseconds { get; private set; }
        public float ActualSpacing { get; private set; }

        public LobbyDropletRenderer(LobbyDissolveTest owner)
        {
            this.owner = owner;
            material = new Material(owner.dropletShader) { name = "Lobby sphere fade material (runtime)", hideFlags = HideFlags.HideAndDontSave };
            material.CopyPropertiesFromMaterial(owner.sourceMaterial);
            mesh = new Mesh { name = "Lobby sphere fade mesh (runtime)", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.HideAndDontSave };
            root = new GameObject("Lobby sphere fade (runtime)", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.enabled = false;
        }

        public void Prepare(Vector3 origin)
        {
            renderer.enabled = false;
            int signature = Signature();
            bool changed = !sampled || signature != sampledSignature || Vector3.Distance(sampledOrigin, origin) > owner.dropletSpacing * .35f;
            if (changed)
            {
                SampleSurfaces(origin);
                sampledOrigin = origin; sampledSignature = signature; sampled = true;
            }
            if (changed) BuildTopology();
        }

        void BuildTopology()
        {
            var watch = Stopwatch.StartNew();
            // Radius affects only shader uniforms. Conservative Inspector-maximum
            // bounds avoid rebuilding geometry when its radius setting changes.
            var data = LobbyDropletMesh.Build(seeds, ActualSpacing * .75f);
            mesh.Clear(false);
            mesh.SetVertices(data.centers); mesh.SetNormals(data.radial); mesh.SetColors(data.colors); mesh.SetUVs(0, data.styles);
            mesh.SetTriangles(data.triangles, 0, false); mesh.bounds = data.bounds;
            TopologyBuildCount++;
            LastBuildMilliseconds = watch.Elapsed.TotalMilliseconds;
        }

        static readonly int ProgressID = Shader.PropertyToID("_DissolveProgress");
        static readonly int RevealID = Shader.PropertyToID("_DissolveReveal");
        static readonly int OriginID = Shader.PropertyToID("_DissolveOrigin");
        static readonly int RangeID = Shader.PropertyToID("_DissolveRange");
        static readonly int WidthID = Shader.PropertyToID("_DissolveBandWidth");
        static readonly int RadiusID = Shader.PropertyToID("_DropletMaximumRadius");

        public void Refresh()
        {
            if (disposed) return;
            long started = Stopwatch.GetTimestamp();
            bool active = owner.IsTransitioning;
            renderer.enabled = active && mesh.vertexCount > 0;
            if (active)
            {
                material.SetFloat(ProgressID, owner.Progress);
                material.SetFloat(RevealID, owner.State == LobbyDissolveTest.FadeState.FadingIn ? 1 : 0);
                material.SetVector(OriginID, owner.TransitionOrigin);
                material.SetVector(RangeID, new Vector4(owner.ActiveDistanceRange.x, owner.ActiveDistanceRange.y, 0, 0));
                material.SetFloat(WidthID, owner.boundaryWidth);
                material.SetFloat(RadiusID, ActualSpacing * owner.dropletRadius);
            }
            LastRefreshMilliseconds = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        }

        void SampleSurfaces(Vector3 origin)
        {
            if (owner.useSceneMaterials)
            {
                var sampled = owner.SceneSurfaces.Sample(origin);
                // Uniform deterministic reduction, not first-N: nearby and far surfaces stay represented.
                seeds.Clear();
                int sceneCap = Mathf.Clamp(owner.maximumDroplets,128,4000);
                int count = Mathf.Min(sceneCap,sampled.Count);
                for(int i=0;i<count;i++)seeds.Add(sampled[(int)((long)i*sampled.Count/Mathf.Max(1,count))]);
                ActualSpacing = owner.dropletSpacing;
                return;
            }
            var candidates = new List<Candidate>();
            var atlas = ReadAtlas(owner.sourceMaterial.mainTexture);
            try
            {
                foreach (var target in owner.targetRenderers)
                {
                    if (!target || !target.enabled || !target.gameObject.activeInHierarchy) continue;
                    var size = target.bounds.size;
                    if (Mathf.Max(size.x, Mathf.Max(size.y, size.z)) > owner.maximumRangeSourceSize) continue;
                    var filter = target.GetComponent<MeshFilter>();
                    if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) continue;
                    var source = filter.sharedMesh;
                    var vertices = source.vertices; var normals = source.normals; var uvs = source.uv; var style = source.colors32; var indices = source.triangles;
                    var matrix = target.localToWorldMatrix;
                    var normalMatrix = matrix.inverse.transpose;
                    float spacing = Mathf.Max(.12f, owner.dropletSpacing);
                    for (int tri = 0; tri < indices.Length; tri += 3)
                    {
                        int ia = indices[tri], ib = indices[tri + 1], ic = indices[tri + 2];
                        if (style.Length == vertices.Length && (style[ia].r > 127 || style[ib].r > 127 || style[ic].r > 127)) continue;
                        var a = matrix.MultiplyPoint3x4(vertices[ia]); var b = matrix.MultiplyPoint3x4(vertices[ib]); var c = matrix.MultiplyPoint3x4(vertices[ic]);
                        var cross = Vector3.Cross(b - a, c - a);
                        float area = cross.magnitude * .5f;
                        if (area < 1e-7f) continue;
                        var normal = normals.Length == vertices.Length ? normalMatrix.MultiplyVector(normals[ia] + normals[ib] + normals[ic]).normalized : cross.normalized;
                        // Sample all viewing directions around the captured centre, but
                        // not hidden back faces or the underside of the plaza floor.
                        if (Vector3.Dot(normal, origin - (a + b + c) / 3) < -.01f) continue;
                        int count = Mathf.Clamp(Mathf.CeilToInt(area / (spacing * spacing * .4f)), 1, 1024);
                        for (int i = 0; i < count; i++)
                        {
                            float u = Mathf.Sqrt((i + .5f) / count);
                            float v = Mathf.Repeat(i * .61803398875f + tri * .071f, 1);
                            float wa = 1 - u, wb = u * (1 - v), wc = u * v;
                            var uv = uvs.Length == vertices.Length ? uvs[ia] * wa + uvs[ib] * wb + uvs[ic] * wc : Vector2.zero;
                            var color = atlas ? atlas.GetPixelBilinear(uv.x, uv.y) : Color.white;
                            color.a = 1;
                            candidates.Add(new Candidate { position = a * wa + b * wb + c * wc, normal = normal, color = color });
                        }
                    }
                }
            }
            finally { Destroy(atlas); }

            float cellSize = Mathf.Max(.12f, owner.dropletSpacing);
            var cells = new Dictionary<Vector3Int, Candidate>();
            int cap = Mathf.Clamp(owner.maximumDroplets, 128, 4000);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                cells.Clear();
                foreach (var candidate in candidates)
                {
                    var p = candidate.position / cellSize;
                    var key = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
                    var centre = new Vector3(key.x + .5f, key.y + .5f, key.z + .5f) * cellSize;
                    if (!cells.TryGetValue(key, out var old) || (candidate.position - centre).sqrMagnitude < (old.position - centre).sqrMagnitude)
                        cells[key] = candidate;
                }
                if (cells.Count <= cap) break;
                cellSize *= Mathf.Max(1.05f, Mathf.Sqrt(cells.Count / (float)cap) * 1.03f);
            }
            seeds.Clear();
            var accepted = new Dictionary<Vector3Int, List<Vector3>>();
            float minimumSquared = cellSize * cellSize * .78f * .78f;
            foreach (var candidate in cells.Values)
            {
                if (seeds.Count >= cap) break;
                var point = candidate.position + candidate.normal * (cellSize * .045f);
                var p = point / cellSize;
                var key = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
                bool close = false;
                for (int z=-1; z<=1 && !close; z++)
                for (int y=-1; y<=1 && !close; y++)
                for (int x=-1; x<=1 && !close; x++)
                    if (accepted.TryGetValue(key + new Vector3Int(x,y,z), out var neighbours))
                        foreach (var neighbour in neighbours) if ((point-neighbour).sqrMagnitude < minimumSquared) { close=true; break; }
                if (close) continue;
                if (!accepted.TryGetValue(key, out var bucket)) accepted.Add(key,bucket=new List<Vector3>());
                bucket.Add(point);
                seeds.Add(new LobbyDropletMesh.Seed { position = point, color = candidate.color });
            }
            ActualSpacing = cellSize;
        }

        int Signature()
        {
            unchecked
            {
                int hash = owner.dropletSpacing.GetHashCode() ^ owner.maximumDroplets ^ owner.maximumRangeSourceSize.GetHashCode();
                if(owner.useSceneMaterials)hash^=owner.sceneSampleDistance.GetHashCode(); // Paused pose is reused for the reverse leg; a new lease samples anew.
                foreach (var target in owner.targetRenderers)
                    if (target)
                    {
                        hash = hash * 31 + target.localToWorldMatrix.GetHashCode();
                        hash = hash * 31 + (target.enabled && target.gameObject.activeInHierarchy ? 1 : 0);
                        var filter = target.GetComponent<MeshFilter>();
                        if (filter && filter.sharedMesh) hash = hash * 31 + filter.sharedMesh.GetInstanceID();
                    }
                return hash;
            }
        }

        static Texture2D ReadAtlas(Texture texture)
        {
            if (!texture) return null;
            var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D readback = null;
            try
            {
                Graphics.Blit(texture, rt); RenderTexture.active = rt;
                readback = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.HideAndDontSave };
                readback.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); readback.Apply();
                return readback;
            }
            catch { Destroy(readback); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (renderer) renderer.enabled = false;
            Destroy(root); Destroy(mesh); Destroy(material);
            seeds.Clear();
        }

        static void Destroy(Object value)
        {
            if (!value) return;
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
