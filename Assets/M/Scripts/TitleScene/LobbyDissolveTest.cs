using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using ProjectWait.M.SceneTransition;

namespace ProjectWait.M.TitleScene
{
    // Scene-local visual experiment. Does not load scenes or change shared assets.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class LobbyDissolveTest : MonoBehaviour
    {
        public enum FadeState { Visible, FadingOut, Hidden, FadingIn }

        [Serializable]
        public sealed class Panel
        {
            public Transform surface;
            public CanvasGroup content;
            public Collider[] selectionColliders = Array.Empty<Collider>();
        }

        [Header("Lobby references")]
        public Camera viewCamera;
        public Material sourceMaterial;
        public Shader dissolveShader;
        public Renderer[] targetRenderers = Array.Empty<Renderer>();
        public Panel[] panels = Array.Empty<Panel>();
        [Header("Additional M scenes (original lobby path stays unchanged)")]
        public bool useSceneMaterials;
        public Shader propDissolveShader;
        public Shader litDissolveShader;
        [Min(3)] public float sceneSampleDistance = 18;
        public bool enableKShortcut = true;

        [Header("K toggles after the current transition finishes")]
        [Min(.05f)] public float fadeOutSeconds = 3f;
        [Min(.05f)] public float fadeInSeconds = 3f;

        [Header("Fixed sphere fade (world metres)")]
        [Min(.05f)] public float boundaryWidth = 3f;
        [FormerlySerializedAs("lightweightDropletShader")] public Shader dropletShader;
        [FormerlySerializedAs("metaballSpacing"), Min(.12f)] public float dropletSpacing = .36f;
        [FormerlySerializedAs("metaballRadius"), Range(.35f, .75f)] public float dropletRadius = .43f;
        [FormerlySerializedAs("maximumMetaballs"), Range(128, 4000)] public int maximumDroplets = 1800;

        [Header("Repeated bubble pops during both fades")]
        public bool playDropletPops = true;
        public AudioClip dropletPopClip;
        [Range(0, 1)] public float popVolume = .28f;
        public Vector2 popPitchRange = new Vector2(.88f, 1.18f);
        [Tooltip("Seconds between pops. One event at most per frame; no catch-up bursts after a stall.")]
        public Vector2 popIntervalRange = new Vector2(.08f, .15f);

        [Header("Distance from the camera at the start of each transition")]
        public bool automaticDistanceRange = true;
        public Vector2 distanceRange = new Vector2(.5f, 13f);
        [Tooltip("Large background meshes still dissolve, but do not stretch the transition's distance range.")]
        [Min(1f)] public float maximumRangeSourceSize = 30f;

        public FadeState State { get; private set; } = FadeState.Visible;
        public bool IsTransitioning => State == FadeState.FadingOut || State == FadeState.FadingIn;
        public float Progress { get; private set; }
        public Vector3 TransitionOrigin { get; private set; }
        public Vector2 ActiveDistanceRange { get; private set; }
        public int RuntimeMaterialCount => (runtimeMaterial ? 1 : 0) + (sceneSurfaces?.MaterialCount ?? 0);
        public int DropletSeedCount => droplets?.SeedCount ?? 0;
        public int ActiveDropletCount => droplets?.ActiveCount ?? 0;
        public int DropletVertexCount => droplets?.VertexCount ?? 0;
        public float ActualDropletSpacing => droplets?.ActualSpacing ?? dropletSpacing;
        public int DropletTopologyBuildCount => droplets?.TopologyBuildCount ?? 0;
        public int CachedDropletVertexCount => droplets?.CachedVertexCount ?? 0;
        public double DropletRefreshMilliseconds => droplets?.LastRefreshMilliseconds ?? 0;
        public int PlayedPopCount => pops?.PlayedCount ?? 0;
        public int ActivePopVoices => pops?.ActiveVoices ?? 0;
        public float LastPopPitch => pops?.LastPitch ?? 1;
        public bool SceneTransitionControlled { get; private set; }

        static readonly int EnabledID = Shader.PropertyToID("_DissolveEnabled");
        static readonly int ProgressID = Shader.PropertyToID("_DissolveProgress");
        static readonly int RevealID = Shader.PropertyToID("_DissolveReveal");
        static readonly int OriginID = Shader.PropertyToID("_DissolveOrigin");
        static readonly int RangeID = Shader.PropertyToID("_DissolveRange");
        static readonly int CellID = Shader.PropertyToID("_DissolveCellSize");
        static readonly int WidthID = Shader.PropertyToID("_DissolveBandWidth");

        sealed class RendererState
        {
            public Renderer renderer;
            public Material[] materials;
        }

        sealed class PanelState
        {
            public Panel binding;
            public float alpha;
            public bool interactable, blocksRaycasts;
            public readonly List<ColliderState> colliders = new List<ColliderState>();
        }

        struct ColliderState
        {
            public Collider collider;
            public bool enabled;
        }

        readonly List<RendererState> rendererStates = new List<RendererState>();
        readonly List<PanelState> panelStates = new List<PanelState>();
        Material runtimeMaterial;
        bool initialized;
        LobbyDropletRenderer droplets;
        LobbyDropletPopAudio pops;
        float controlledSeconds;
        SceneMaterialDissolve sceneSurfaces;
        internal SceneMaterialDissolve SceneSurfaces => sceneSurfaces;

        void OnEnable()
        {
            if (Application.isPlaying && !useSceneMaterials) Initialize();
        }

        void Update()
        {
            if (SceneTransitionControlled) return;
            if (enableKShortcut && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ToggleFade();
            Advance(Time.unscaledDeltaTime);
        }

        [ContextMenu("Toggle fade (Play Mode)")]
        public void ToggleFromInspector() { ToggleFade(); }

        // Ignore presses during playback: reversing the timeline would reveal FAR objects first.
        public bool ToggleFade()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || SceneTransitionControlled || IsTransitioning || !Initialize()) return false;
            return StartFade(State == FadeState.Hidden);
        }

        // Borrow the approved renderer/audio without changing the K test's serialized settings.
        // The caller owns Advance while borrowed, so the same frame is never ticked twice.
        public bool BeginSceneTransition(float seconds)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || SceneTransitionControlled || IsTransitioning || !Initialize() || State != FadeState.Visible) return false;
            SceneTransitionControlled = true;
            controlledSeconds = Mathf.Max(.05f, seconds);
            return StartFade(false);
        }

        public bool RevealSceneTransition(float seconds)
        {
            if (!SceneTransitionControlled || State != FadeState.Hidden) return false;
            controlledSeconds = Mathf.Max(.05f, seconds);
            return StartFade(true);
        }

        public bool PrepareSceneArrival()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || SceneTransitionControlled || !Initialize()) return false;
            SceneTransitionControlled = true;
            TransitionOrigin = viewCamera.transform.position;
            ActiveDistanceRange = CalculateDistanceRange(TransitionOrigin);
            droplets.Prepare(TransitionOrigin);
            State = FadeState.Hidden;
            Progress = 1;
            Apply();
            return true;
        }

        public void EnforceSceneHud() { sceneSurfaces?.EnforceSupplementals(State == FadeState.Visible); }

        public void ReleaseSceneTransition()
        {
            if (!SceneTransitionControlled) return;
            SceneTransitionControlled = false;
            State = FadeState.Visible;
            Progress = 0;
            pops?.Begin();
            if (useSceneMaterials) { Restore(); return; }
            if (initialized) Apply();
        }

        bool StartFade(bool reveal)
        {
            TransitionOrigin = viewCamera.transform.position; // Shared centre-eye origin, never per-eye depth.
            ActiveDistanceRange = CalculateDistanceRange(TransitionOrigin);
            droplets.Prepare(TransitionOrigin);
            State = reveal ? FadeState.FadingIn : FadeState.FadingOut;
            Progress = 0;
            pops.Begin();
            Apply();
            return true;
        }

        // Public deterministic tick for the scene smoke check; normal playback uses unscaled time.
        public void Advance(float seconds)
        {
            if (!initialized || !IsTransitioning || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float duration = SceneTransitionControlled ? controlledSeconds : State == FadeState.FadingOut ? fadeOutSeconds : fadeInSeconds;
            Progress = Mathf.Clamp01(Progress + seconds / Mathf.Max(.05f, duration));
            if (Progress >= 1) State = State == FadeState.FadingOut ? FadeState.Hidden : FadeState.Visible;
            Apply();
            pops.Tick(seconds, IsTransitioning, Progress);
        }

        public float VisibilityAt(Vector3 worldPosition)
        {
            if (State == FadeState.Visible) return 1;
            if (State == FadeState.Hidden) return 0;
            float span = Mathf.Max(.1f, ActiveDistanceRange.y - ActiveDistanceRange.x);
            float distance = Mathf.Clamp01((Vector3.Distance(worldPosition, TransitionOrigin) - ActiveDistanceRange.x) / span);
            float band = Mathf.Clamp(boundaryWidth / span, .02f, 1);
            bool reveal = State == FadeState.FadingIn;
            float start = (reveal ? distance : 1 - distance) * (1 - band);
            float local = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Progress - start) / band));
            return reveal ? local : 1 - local;
        }

        bool Initialize()
        {
            if (initialized) return true;
            if (useSceneMaterials)
            {
                if (!viewCamera || !sourceMaterial || !propDissolveShader || !dropletShader) return false;
                sceneSurfaces = new SceneMaterialDissolve(this);
                targetRenderers = sceneSurfaces.Renderers;
                if (targetRenderers.Length == 0) { sceneSurfaces.Dispose();sceneSurfaces=null;return false; }
            }
            if (!viewCamera || !sourceMaterial || !dissolveShader || !dissolveShader.isSupported || !dropletShader || !dropletShader.isSupported ||
                targetRenderers == null || targetRenderers.Length == 0 || viewCamera.gameObject.scene != gameObject.scene)
            {
                Debug.LogError("LobbyDissolveTest needs its lobby camera, source material, supported fade/sphere shaders and renderers.", this);
                return false;
            }

            // Only one clone for the shared atlas, NOT Renderer.material (one clone per renderer).
            var material = useSceneMaterials ? null : new Material(sourceMaterial)
            {
                shader = dissolveShader,
                name = sourceMaterial.name + " (Lobby dissolve runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material) material.SetFloat(EnabledID, 0);
            var seen = new HashSet<Renderer>();
            foreach (var target in targetRenderers)
            {
                if (useSceneMaterials) break;
                if (!target || target.gameObject.scene != gameObject.scene || !seen.Add(target)) continue;
                var original = target.sharedMaterials;
                var replacement = (Material[])original.Clone();
                bool changed = false;
                for (int i = 0; i < replacement.Length; i++)
                    if (replacement[i] == sourceMaterial) { replacement[i] = material; changed = true; }
                if (!changed) continue;
                rendererStates.Add(new RendererState { renderer = target, materials = original });
                target.sharedMaterials = replacement;
            }
            if (!useSceneMaterials && rendererStates.Count == 0)
            {
                DestroyRuntimeMaterial(material);
                Debug.LogError("LobbyDissolveTest found no matching scene-local atlas renderers.", this);
                return false;
            }
            runtimeMaterial = material;

            if (panels != null) foreach (var panel in panels)
            {
                if (panel == null || !panel.surface || panel.surface.gameObject.scene != gameObject.scene) continue;
                var saved = new PanelState { binding = panel };
                if (panel.content)
                {
                    saved.alpha = panel.content.alpha;
                    saved.interactable = panel.content.interactable;
                    saved.blocksRaycasts = panel.content.blocksRaycasts;
                }
                if (panel.selectionColliders != null) foreach (var collider in panel.selectionColliders)
                    if (collider && collider.gameObject.scene == gameObject.scene)
                        saved.colliders.Add(new ColliderState { collider = collider, enabled = collider.enabled });
                panelStates.Add(saved);
            }
            initialized = true;
            State = FadeState.Visible;
            Progress = 0;
            TransitionOrigin = viewCamera.transform.position;
            ActiveDistanceRange = CalculateDistanceRange(TransitionOrigin);
            try
            {
                droplets = new LobbyDropletRenderer(this);
                droplets.Prepare(TransitionOrigin);
                pops = new LobbyDropletPopAudio(this);
            }
            catch (Exception ex)
            {
                Restore();
                Debug.LogError("Lobby sphere fade initialization failed: " + ex.Message, this);
                return false;
            }
            Apply();
            return true;
        }

        Vector2 CalculateDistanceRange(Vector3 origin)
        {
            float near = Mathf.Max(0, distanceRange.x), far = Mathf.Max(near + .1f, distanceRange.y);
            if (!automaticDistanceRange) return new Vector2(near, far);
            float closest = float.PositiveInfinity, furthest = 0;
            foreach (var target in targetRenderers)
            {
                if (!target || !target.enabled || !target.gameObject.activeInHierarchy) continue;
                var bounds = target.bounds;
                if (Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) > maximumRangeSourceSize) continue;
                closest = Mathf.Min(closest, Vector3.Distance(origin, bounds.ClosestPoint(origin)));
                for (int i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    furthest = Mathf.Max(furthest, Vector3.Distance(origin, corner));
                }
            }
            if (useSceneMaterials) return new Vector2(0,sceneSampleDistance);
            return float.IsInfinity(closest) ? new Vector2(near, far)
                : new Vector2(Mathf.Max(0, closest - .1f), Mathf.Max(closest + .1f, furthest + .1f));
        }

        void Apply()
        {
            bool visible = State == FadeState.Visible;
            bool reveal = State == FadeState.FadingIn;
            sceneSurfaces?.Apply(visible,reveal,State == FadeState.Hidden ? 1 : Progress,TransitionOrigin,ActiveDistanceRange,ActualDropletSpacing);
            if (runtimeMaterial)
            {
                var material = runtimeMaterial;
                material.SetFloat(EnabledID, visible ? 0 : 1);
                material.SetFloat(ProgressID, State == FadeState.Hidden ? 1 : Progress);
                material.SetFloat(RevealID, reveal ? 1 : 0);
                material.SetVector(OriginID, TransitionOrigin);
                material.SetVector(RangeID, new Vector4(ActiveDistanceRange.x, ActiveDistanceRange.y, 0, 0));
                material.SetFloat(CellID, ActualDropletSpacing);
                material.SetFloat(WidthID, Mathf.Max(.05f, boundaryWidth));
            }
            foreach (var saved in panelStates)
            {
                var panel = saved.binding;
                if (panel.content)
                {
                    // Clear the flat UI before the backing breaks into isolated dots.
                    float keep = panel.surface ? VisibilityAt(panel.surface.position) : (visible ? 1 : 0);
                    float alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.85f, 1, keep));
                    panel.content.alpha = saved.alpha * alpha;
                    panel.content.interactable = visible && saved.interactable;
                    panel.content.blocksRaycasts = visible && saved.blocksRaycasts;
                }
                // PC/VR selection also uses Physics.Raycast, so CanvasGroup alone is insufficient.
                foreach (var collider in saved.colliders)
                    if (collider.collider) collider.collider.enabled = visible && collider.enabled;
            }
            droplets.Refresh();
        }

        void OnDisable() { Restore(); }
        void OnDestroy() { Restore(); }

        void Restore()
        {
            pops?.Dispose(); pops = null;
            droplets?.Dispose(); droplets = null;
            sceneSurfaces?.Dispose(); sceneSurfaces = null;
            foreach (var saved in rendererStates)
                if (saved.renderer) saved.renderer.sharedMaterials = saved.materials;
            foreach (var saved in panelStates)
            {
                if (saved.binding.content)
                {
                    saved.binding.content.alpha = saved.alpha;
                    saved.binding.content.interactable = saved.interactable;
                    saved.binding.content.blocksRaycasts = saved.blocksRaycasts;
                }
                foreach (var collider in saved.colliders)
                    if (collider.collider) collider.collider.enabled = collider.enabled;
            }
            DestroyRuntimeMaterial(runtimeMaterial); runtimeMaterial = null;
            rendererStates.Clear();
            panelStates.Clear();
            initialized = false;
            SceneTransitionControlled = false;
            State = FadeState.Visible;
            Progress = 0;
        }

        static void DestroyRuntimeMaterial(Material material)
        {
            if (!material) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }

        void OnValidate()
        {
            fadeOutSeconds = Mathf.Max(.05f, fadeOutSeconds);
            fadeInSeconds = Mathf.Max(.05f, fadeInSeconds);
            boundaryWidth = Mathf.Max(.05f, boundaryWidth);
            dropletSpacing = Mathf.Max(.12f, dropletSpacing);
            dropletRadius = Mathf.Clamp(dropletRadius, .35f, .75f);
            maximumDroplets = Mathf.Clamp(maximumDroplets, 128, 4000);
            popPitchRange.x = Mathf.Clamp(popPitchRange.x, .5f, 2);
            popPitchRange.y = Mathf.Clamp(popPitchRange.y, popPitchRange.x, 2);
            popIntervalRange.x = Mathf.Clamp(popIntervalRange.x, .05f, 1);
            popIntervalRange.y = Mathf.Clamp(popIntervalRange.y, popIntervalRange.x, 1);
            maximumRangeSourceSize = Mathf.Max(1, maximumRangeSourceSize);
            distanceRange.x = Mathf.Max(0, distanceRange.x);
            distanceRange.y = Mathf.Max(distanceRange.x + .1f, distanceRange.y);
        }
    }
}
