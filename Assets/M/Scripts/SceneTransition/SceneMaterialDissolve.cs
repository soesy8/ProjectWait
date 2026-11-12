using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ProjectWait.M.TitleScene;
using Object = UnityEngine.Object;

namespace ProjectWait.M.SceneTransition
{
    // Scene-only material leases. Normal rendering always uses the original materials.
    // Imported unreadable props get bounded box samples; readable meshes and skin use real surfaces.
    sealed class SceneMaterialDissolve : IDisposable
    {
        sealed class Binding { public Renderer renderer; public Material[] original, fading; }
        struct Hidden { public Renderer renderer; public bool enabled; }
        struct Hud { public Canvas canvas; public bool enabled; }
        readonly LobbyDissolveTest owner;
        readonly List<Binding> bindings = new List<Binding>();
        readonly List<Hidden> hidden = new List<Hidden>();
        readonly List<Hud> hud = new List<Hud>();
        readonly Dictionary<Material,Material> materials = new Dictionary<Material,Material>();
        bool fading;
        public int MaterialCount => materials.Count;
        public Renderer[] Renderers { get; }

        public SceneMaterialDissolve(LobbyDissolveTest owner)
        {
            this.owner = owner;
            Transform rig = owner.viewCamera.transform.root;
            foreach (var root in owner.gameObject.scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    hud.Add(new Hud { canvas = canvas, enabled = canvas.enabled });
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (!r.enabled || r.transform.IsChildOf(rig) || r is LineRenderer || r is ParticleSystemRenderer) continue;
                    var source = r.sharedMaterials;
                    bool supported = (r is MeshRenderer || r is SkinnedMeshRenderer) && source.Length > 0;
                    foreach (var m in source) if (!m || m.shader.name.Contains("TextMeshPro")) supported = false;
                    if (!supported) { hidden.Add(new Hidden { renderer = r, enabled = r.enabled }); continue; }
                    var replacement = new Material[source.Length];
                    for (int i = 0; i < source.Length; i++)
                    {
                        var original = source[i];
                        if (!materials.TryGetValue(original, out var clone))
                        {
                            clone = new Material(original) { name = original.name + " (Scene droplet lease)", hideFlags = HideFlags.HideAndDontSave };
                            if (!original.HasProperty("_DissolveEnabled"))
                            {
                                bool lit = original.shader.name == "Universal Render Pipeline/Lit" && owner.litDissolveShader;
                                clone.shader = lit ? owner.litDissolveShader : owner.propDissolveShader;
                                clone.shaderKeywords = original.shaderKeywords;
                                clone.renderQueue = original.renderQueue;
                                clone.SetColor("_BaseColor", original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white);
                                var texture = SurfaceTexture(original);
                                if (texture) clone.SetTexture("_BaseMap", texture);
                                if(!lit)clone.SetFloat("_Unlit", original.shader.name.Contains("/Lit") ? 0 : 1);
                                clone.SetShaderPassEnabled("ShadowCaster",original.FindPass("ShadowCaster")>=0&&original.GetShaderPassEnabled("ShadowCaster"));
                            }
                            materials.Add(original, clone);
                        }
                        replacement[i] = clone;
                    }
                    bindings.Add(new Binding { renderer = r, original = source, fading = replacement });
                }
            }
            Renderers = new Renderer[bindings.Count];
            for (int i = 0; i < bindings.Count; i++) Renderers[i] = bindings[i].renderer;
        }

        public void Apply(bool visible, bool reveal, float progress, Vector3 origin, Vector2 range, float spacing)
        {
            if (fading == visible)
            {
                fading = !visible;
                foreach (var b in bindings) if (b.renderer) b.renderer.sharedMaterials = fading ? b.fading : b.original;
            }
            if (!visible) foreach (var material in materials.Values)
            {
                material.SetFloat("_DissolveEnabled", 1);
                material.SetFloat("_DissolveReveal", reveal ? 1 : 0);
                material.SetFloat("_DissolveProgress", progress);
                material.SetVector("_DissolveOrigin", origin);
                material.SetVector("_DissolveRange", new Vector4(range.x,range.y,0,0));
                material.SetFloat("_DissolveCellSize", spacing);
                material.SetFloat("_DissolveBandWidth", owner.boundaryWidth);
            }
            EnforceSupplementals(visible);
        }

        public void EnforceSupplementals(bool visible)
        {
            foreach (var h in hidden) if (h.renderer) h.renderer.enabled = h.enabled && (visible || owner.VisibilityAt(h.renderer.bounds.center) >= .999f);
            foreach (var h in hud) if (h.canvas) h.canvas.enabled = visible && h.enabled;
        }

        public List<LobbyDropletMesh.Seed> Sample(Vector3 origin)
        {
            var seeds = new List<LobbyDropletMesh.Seed>();
            var cells = new HashSet<Vector3Int>();
            var textures = new Dictionary<Texture,Texture2D>();
            float spacing = Mathf.Max(.12f, owner.dropletSpacing);
            try
            {
                foreach (var b in bindings)
                {
                    var r = b.renderer;
                    if (!r || !r.gameObject.activeInHierarchy || Vector3.Distance(origin,r.bounds.ClosestPoint(origin)) > owner.sceneSampleDistance) continue;
                    Mesh baked = null;
                    var filter = r.GetComponent<MeshFilter>();
                    Mesh mesh = filter ? filter.sharedMesh : null;
                    if (r is SkinnedMeshRenderer skin) { baked = new Mesh(); skin.BakeMesh(baked); mesh = baked; }
                    try
                    {
                        if (!mesh || !mesh.isReadable) { Box(r.bounds, BaseColor(b.original[0])); continue; }
                        var vertices = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv; var colors = mesh.colors;
                        bool palette = b.original[0].shader.name.Contains("Vertex Palette") || b.original[0].shader.name.Contains("Ball Handoff");
                        var matrix = r.localToWorldMatrix; var normalMatrix = matrix.inverse.transpose;
                        for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        {
                            var mat = b.original[Mathf.Min(sub,b.original.Length-1)]; var tex = SurfaceTexture(mat);
                            Texture2D image = null;
                            if (tex && !textures.TryGetValue(tex,out image)) { image = ReadTexture(tex); textures.Add(tex,image); }
                            var triangles = mesh.GetTriangles(sub);
                            for (int t = 0; t < triangles.Length; t += 3)
                            {
                                int ia=triangles[t],ib=triangles[t+1],ic=triangles[t+2];
                                Vector3 a=matrix.MultiplyPoint3x4(vertices[ia]),c=matrix.MultiplyPoint3x4(vertices[ic]),bb=matrix.MultiplyPoint3x4(vertices[ib]);
                                Vector3 center=(a+bb+c)/3;
                                if (Vector3.Distance(origin,center)>owner.sceneSampleDistance) continue;
                                var cross=Vector3.Cross(bb-a,c-a); float area=cross.magnitude*.5f;
                                if (area<1e-7f) continue;
                                Vector3 n=normals.Length==vertices.Length?normalMatrix.MultiplyVector(normals[ia]+normals[ib]+normals[ic]).normalized:cross.normalized;
                                if (Vector3.Dot(n,origin-center)<-.01f) continue;
                                int count=Mathf.Clamp(Mathf.CeilToInt(area/(spacing*spacing)),1,512);
                                for (int i=0;i<count;i++)
                                {
                                    float u=Mathf.Sqrt((i+.5f)/count),v=Mathf.Repeat(i*.61803398875f+t*.071f,1),wa=1-u,wb=u*(1-v),wc=u*v;
                                    Color color=BaseColor(mat);
                                    if (palette && colors.Length==vertices.Length) color*=colors[ia]*wa+colors[ib]*wb+colors[ic]*wc;
                                    else if(image && uv.Length==vertices.Length) { var p=uv[ia]*wa+uv[ib]*wb+uv[ic]*wc; color*=image.GetPixelBilinear(p.x,p.y); }
                                    Add(a*wa+bb*wb+c*wc+n*spacing*.04f,color);
                                }
                            }
                        }
                    }
                    finally { if (baked) Destroy(baked); }
                }
                return seeds;
            }
            finally { foreach (var image in textures.Values) if (image) Destroy(image); }

            void Add(Vector3 point,Color color)
            {
                if (Vector3.Distance(point,origin)>owner.sceneSampleDistance) return;
                var p=point/spacing; var cell=new Vector3Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z));
                if (!cells.Add(cell)) return;
                color.a=1; seeds.Add(new LobbyDropletMesh.Seed{position=point,color=color});
            }
            void Box(Bounds bounds,Color color)
            {
                for(int axis=0;axis<3;axis++) for(int side=-1;side<=1;side+=2)
                {
                    int u=(axis+1)%3,v=(axis+2)%3;
                    int nu=Mathf.Clamp(Mathf.CeilToInt(bounds.size[u]/spacing),1,32),nv=Mathf.Clamp(Mathf.CeilToInt(bounds.size[v]/spacing),1,32);
                    for(int a=0;a<=nu;a++)for(int b=0;b<=nv;b++) { var p=bounds.center;p[axis]+=bounds.extents[axis]*side;p[u]+=Mathf.Lerp(-bounds.extents[u],bounds.extents[u],a/(float)nu);p[v]+=Mathf.Lerp(-bounds.extents[v],bounds.extents[v],b/(float)nv);Add(p,color); }
                }
            }
        }

        static Color BaseColor(Material mat) => mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        static Texture SurfaceTexture(Material mat) => mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        static Texture2D ReadTexture(Texture source)
        {
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            try { Graphics.Blit(source,rt);RenderTexture.active=rt;var image=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,true);image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();return image; }
            finally { RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt); }
        }
        public void Dispose()
        {
            foreach(var b in bindings)if(b.renderer)b.renderer.sharedMaterials=b.original;
            EnforceSupplementals(true);
            foreach(var mat in materials.Values)Destroy(mat);
            bindings.Clear();materials.Clear();hidden.Clear();hud.Clear();
        }
        static void Destroy(Object value) { if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value); }
    }
}
