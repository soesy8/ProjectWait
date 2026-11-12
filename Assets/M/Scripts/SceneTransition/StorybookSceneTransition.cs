using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ProjectWait.M.TitleScene;

namespace ProjectWait.M.SceneTransition
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class StorybookSceneTransition : MonoBehaviour
    {
        public enum TransitionPhase { Idle, FadingOut, Holding, Activating, Revealing }
        public const string ElevatorContext = "친구와 농구를 마치고,\n이제 집으로 돌아가는 길이에요.";

        [Header("M-owned static Korean font and overlay material")]
        public TMP_FontAsset font;
        public Material overlayMaterial;
        public Shader skyBridgeShader;
        [Header("Reuse the approved lobby sphere fade, not a dark screen fade")]
        public LobbyDissolveTest lobbyFade;
        [Min(.05f)] public float fadeOutSeconds = 2.5f;
        [Min(0)] public float minimumHoldSeconds = 1.5f;
        [Tooltip("Same sphere effect in reverse for the no-load preview.")]
        [Min(.05f)] public float fadeInSeconds = 2.5f;
        [Tooltip("Fallback only for scenes without a registered droplet renderer.")]
        [Min(.05f)] public float arrivalSeconds = .5f;
        [Header("World-space loading card (exponential Lerp)")]
        [Min(.1f)] public float panelLerpSpeed = 12;
        [Min(.5f)] public float panelDistance = 2;
        [Min(.5f)] public float panelWidth = 1.65f;
        public bool revealOnStandaloneStart;
        [Header("F8 previews without leaving the lobby; K is unchanged")]
        public bool enablePreviewShortcut = true;
        public string previewTitle = "엘리베이터";
        [TextArea(2, 3)] public string previewContext = ElevatorContext;

        public static StorybookSceneTransition Current { get; private set; }
        public static bool IsBusy => Current && Current.Phase != TransitionPhase.Idle;
        public TransitionPhase Phase { get; private set; }
        public SceneTransitionView View { get; private set; }
        public bool IsPreview { get; private set; }
        public string LastError { get; private set; } = "";
        public float Elapsed { get; private set; }
        public float PhaseSeconds { get; private set; }
        public bool LoadStarted => loadRequested;
        public float MinimumSeconds => fadeOutSeconds + minimumHoldSeconds + fadeInSeconds;
        public float PreviewSeconds => fadeOutSeconds + minimumHoldSeconds + fadeInSeconds;
        public LobbyDissolveTest ActiveLobbyFade => borrowedFade;
        public LobbyDissolveTest ArrivingFade => arrivingFade;

        AsyncOperation operation;
        Action<bool> completed;
        string destination, homeScene;
        bool loadRequested, failed, sceneReady;
        int coveredFrame, activationFrame;
        double previousTick;
        LobbyDissolveTest borrowedFade;
        LobbyDissolveTest arrivingFade, activeFade;
        bool standaloneArrival;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; }

        void Awake()
        {
            if (Current && Current != this) { Destroy(gameObject); return; }
            Current = this;
            homeScene = gameObject.scene.path;
            activeFade = lobbyFade;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (Current != this || !revealOnStandaloneStart || IsBusy) return;
            activeFade = activeFade ? activeFade : FindEffect(SceneManager.GetActiveScene());
            if (!activeFade || !activeFade.useSceneMaterials || !font || !overlayMaterial || !skyBridgeShader) return;
            EnsureView();
            View.BeginPanel(); View.BindCamera(activeFade.viewCamera); View.SetSky(RenderSettings.skybox);
            View.SetContent(previewTitle,previewContext);
            standaloneArrival = true; IsPreview = false; sceneReady = true;
            Elapsed = PhaseSeconds = 0; previousTick = Time.realtimeSinceStartupAsDouble;
            activationFrame = Time.frameCount; Phase = TransitionPhase.Activating;
            Present();
        }

        void OnEnable() { SceneManager.sceneLoaded += SceneLoaded; }

        void Update()
        {
            if (enablePreviewShortcut && !IsBusy && activeFade && Keyboard.current != null && (activeFade.useSceneMaterials ? Keyboard.current.f9Key.wasPressedThisFrame : Keyboard.current.f8Key.wasPressedThisFrame))
                Preview();
            double now = Time.realtimeSinceStartupAsDouble;
            float seconds = (float)Math.Max(0, now - previousTick);
            previousTick = now;
            Advance(seconds);
        }

        void LateUpdate()
        {
            if (IsBusy) { borrowedFade?.EnforceSceneHud(); arrivingFade?.EnforceSceneHud(); }
        }

        static LobbyDissolveTest FindEffect(Scene scene)
        {
            foreach(var effect in FindObjectsByType<LobbyDissolveTest>(FindObjectsSortMode.None))
                if(effect.gameObject.scene==scene)return effect;
            return null;
        }

        void EnsureView()
        {
            if (!View)
            {
                var go = new GameObject("Story transition canvas (runtime)",typeof(RectTransform));go.layer=5;go.transform.SetParent(transform,false);
                View=go.AddComponent<SceneTransitionView>();View.Build(font,overlayMaterial,skyBridgeShader);
            }
            View.PanelLerpSpeed=panelLerpSpeed;View.PanelDistance=panelDistance;View.PanelWidth=panelWidth;
        }

        [ContextMenu("Preview story transition (Play Mode, no scene load)")]
        public void PreviewFromInspector() { Preview(); }

        public bool Preview()
        {
            return Begin(null, previewTitle, previewContext, Camera.main, null);
        }

        public bool TryLoad(string scenePath, string title, string context, Camera sourceCamera, Action<bool> onFinished = null)
        {
            if (string.IsNullOrEmpty(scenePath) || !CanLoad(scenePath)) return false;
            return Begin(scenePath, title, context, sourceCamera, onFinished);
        }

        static bool CanLoad(string scenePath)
        {
            if (Application.CanStreamedLevelBeLoaded(scenePath)) return true;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scenePath) != null;
#else
            return false;
#endif
        }

        bool Begin(string scenePath, string title, string context, Camera sourceCamera, Action<bool> onFinished)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsBusy || !font || !overlayMaterial || !skyBridgeShader || !sourceCamera || !RenderSettings.skybox || !RenderSettings.skybox.HasProperty("_MainTex")) return false;
            var effect = activeFade && activeFade.gameObject.scene == sourceCamera.gameObject.scene ? activeFade : FindEffect(sourceCamera.gameObject.scene);
            if (!effect || effect.gameObject.scene != sourceCamera.gameObject.scene || !effect.BeginSceneTransition(fadeOutSeconds)) return false;
            borrowedFade = effect;
            EnsureView();
            View.BeginPanel();
            View.SetSky(RenderSettings.skybox);
            View.BindCamera(sourceCamera);
            View.SetContent(title, string.IsNullOrWhiteSpace(context) ? "새로운 이야기가 곧 시작돼요." : context);
            destination = scenePath;
            IsPreview = string.IsNullOrEmpty(scenePath);
            standaloneArrival = false; arrivingFade = null;
            completed = onFinished;
            failed = loadRequested = sceneReady = false;
            operation = null;
            LastError = "";
            Elapsed = PhaseSeconds = 0;
            previousTick = Time.realtimeSinceStartupAsDouble;
            Phase = TransitionPhase.FadingOut;
            Present();
            return true;
        }

        // Unscaled clock: an existing pause/timeScale=0 never stalls the transition or dots.
        // The existing sphere renderer and pop audio are ticked once here while borrowed.
        public void Advance(float seconds)
        {
            if (Phase == TransitionPhase.Idle || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            var previousPhase = Phase;
            Elapsed += seconds;
            PhaseSeconds += seconds;
            switch (Phase)
            {
                case TransitionPhase.FadingOut:
                    if (!borrowedFade || !borrowedFade.SceneTransitionControlled) { Finish(false); return; }
                    borrowedFade.Advance(seconds);
                    if (borrowedFade.State == LobbyDissolveTest.FadeState.Hidden)
                    { SetPhase(TransitionPhase.Holding); coveredFrame = Time.frameCount; }
                    break;
                case TransitionPhase.Holding:
                    // The world is already gone. Keep the original sky, then start loading behind it.
                    if (!IsPreview && !loadRequested && Time.frameCount > coveredFrame && PhaseSeconds >= .2f) StartLoading();
                    if (PhaseSeconds >= minimumHoldSeconds && (IsPreview || failed || operation != null && operation.progress >= .9f))
                    {
                        if (IsPreview || failed)
                        {
                            if (!borrowedFade || !borrowedFade.RevealSceneTransition(fadeInSeconds)) { Finish(false); return; }
                            SetPhase(TransitionPhase.Revealing);
                        }
                        else { SetPhase(TransitionPhase.Activating); operation.allowSceneActivation = true; }
                    }
                    break;
                case TransitionPhase.Activating:
                    if (sceneReady && (operation == null || operation.isDone) && Time.frameCount >= activationFrame + 2 && View.ViewCamera && (!standaloneArrival || PhaseSeconds >= minimumHoldSeconds))
                    {
                        if (activeFade && activeFade.useSceneMaterials)
                        {
                            if (!activeFade.PrepareSceneArrival() || !activeFade.RevealSceneTransition(fadeInSeconds)) { LastError="Incoming droplet renderer could not prepare."; Finish(false); return; }
                            arrivingFade=activeFade;
                        }
                        SetPhase(TransitionPhase.Revealing);
                    }
                    break;
                case TransitionPhase.Revealing:
                    if (IsPreview || failed)
                    {
                        if (!borrowedFade || !borrowedFade.SceneTransitionControlled) { Finish(false); return; }
                        borrowedFade.Advance(seconds);
                        if (borrowedFade.State == LobbyDissolveTest.FadeState.Visible) { Finish(!failed); return; }
                    }
                    else if(arrivingFade)
                    {
                        arrivingFade.Advance(seconds);
                        if(arrivingFade.State==LobbyDissolveTest.FadeState.Visible){Finish(true);return;}
                    }
                    else if (PhaseSeconds >= Mathf.Max(.05f, arrivalSeconds)) { Finish(true); return; }
                    break;
            }
            // A load/activation stall belongs to the phase it blocked, not the new panel exit.
            Present(Phase == previousPhase ? seconds : 0);
        }

        void StartLoading()
        {
            loadRequested = true;
            try
            {
#if UNITY_EDITOR
                if (!Application.CanStreamedLevelBeLoaded(destination))
                    operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(destination, new LoadSceneParameters(LoadSceneMode.Single));
                else
#endif
                    operation = SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Unity did not create a scene loading operation.");
                operation.allowSceneActivation = false;
            }
            catch (Exception error)
            {
                failed = true;
                LastError = error.Message;
                View.ContextText.text = "장면을 열지 못했어요.\n잠시 후 다시 시도해 주세요.";
                View.StatusText.text = "현재 장면으로 돌아가요";
                PhaseSeconds = 0;
                Debug.LogWarning("Story transition could not load " + destination + ": " + LastError, this);
            }
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            activeFade = FindEffect(scene);
            if (Phase != TransitionPhase.Activating) return;
            sceneReady = true;
            activationFrame = Time.frameCount;
            View.BindCamera(Camera.main);
        }

        void SetPhase(TransitionPhase phase) { Phase = phase; PhaseSeconds = 0; }

        void Present(float seconds = 0)
        {
            float cover = 0, panel = 1;
            if (Phase == TransitionPhase.FadingOut)
                panel = borrowedFade && borrowedFade.Progress >= .82f ? 1 : 0;
            else if (Phase == TransitionPhase.Holding || Phase == TransitionPhase.Activating) cover = 1;
            else if (Phase == TransitionPhase.Revealing)
            {
                cover = IsPreview || failed || arrivingFade ? 0 : 1 - Smooth(PhaseSeconds / Mathf.Max(.05f, arrivalSeconds));
                panel = 0;
            }
            View.Present(cover, panel, Elapsed);
            View.AdvancePanel(seconds);
        }

        static float Smooth(float value) { return Mathf.SmoothStep(0, 1, Mathf.Clamp01(value)); }

        void Finish(bool success)
        {
            Phase = TransitionPhase.Idle;
            if (View) View.Hide();
            if (borrowedFade) borrowedFade.ReleaseSceneTransition();
            if (arrivingFade) arrivingFade.ReleaseSceneTransition();
            borrowedFade = null;
            arrivingFade = null;
            operation = null;
            var callback = completed;
            completed = null;
            callback?.Invoke(success);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            // Unity cannot cancel an AsyncOperation; never strand its global loading queue at 0.9.
            if (operation != null && !operation.isDone) operation.allowSceneActivation = true;
            if (Phase != TransitionPhase.Idle) Finish(false);
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
