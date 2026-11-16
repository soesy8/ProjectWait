using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ProjectWait.M.SceneTransition;
using Y.Crosswalk;

namespace ProjectWait.M.Crosswalk
{
    // Scene-local bridge: reuse main's loading effect without changing the lesson flow.
    [DefaultExecutionOrder(20000)]
    public sealed class CrosswalkSceneIntegration : MonoBehaviour
    {
        public const string ScenePath = "Assets/Scenes/CrosswalkScene.unity";
        public const string LobbyPath = "Assets/M/Scene/Wait_Robby.unity";
        public const string Context = "학교를 마치고,\n친구와 함께 집으로 돌아가는 길이에요.";
        public Camera viewer;
        public CrosswalkDesktopInput input;
        public Canvas menuCanvas;
        public UnityEngine.UI.Button resumeButton, restartButton, lobbyButton, quitButton;
        public TMP_Text statusText;
        public static CrosswalkSceneIntegration Current { get; private set; }
        public static bool GameplayBlocked => Current && (Current.IsPaused || StorybookSceneTransition.IsBusy);
        public bool IsPaused { get; private set; }
        public bool QuitRequested { get; private set; }
        public bool IsTimeHeld => timeHeld;
        public bool IsLeaving { get; private set; }

        InputAction toggle;
        bool timeHeld, savedAudio;
        float savedTimeScale;
        readonly Dictionary<Canvas, bool> hiddenCanvases = new Dictionary<Canvas, bool>();
        readonly List<Canvas> canvasBuffer = new List<Canvas>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; }

        void Awake() { Current = this; }
        void OnEnable()
        {
            Application.onBeforeRender += EnforceHud;
            toggle = new InputAction("CrosswalkPause", InputActionType.Button);
            toggle.AddBinding("<Keyboard>/escape");
            toggle.AddBinding("<XRController>{LeftHand}/menuButton");
            toggle.AddBinding("<XRController>{RightHand}/secondaryButton");
            toggle.performed += OnToggle;
            toggle.Enable();
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(Restart);
            lobbyButton.onClick.AddListener(ReturnToLobby);
            quitButton.onClick.AddListener(Quit);
            menuCanvas.gameObject.SetActive(false);
        }

        void OnToggle(InputAction.CallbackContext _) { Toggle(); }
        public void Toggle() { if (IsPaused) Resume(); else Pause(); }
        public void Pause()
        {
            if (IsPaused || IsLeaving || StorybookSceneTransition.IsBusy || !viewer || !menuCanvas) return;
            IsPaused = true;
            ProjectWait.M.Shared.StoryMusic.SetMenuDucked(this, true);
            HoldTime();
            input.SetExternalMenu(true);
            menuCanvas.worldCamera = viewer;
            var rect = (RectTransform)menuCanvas.transform;
            Vector3 forward = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            rect.SetPositionAndRotation(viewer.transform.position + forward * 1.4f, Quaternion.LookRotation(forward));
            rect.localScale = Vector3.one * .0016f;
            menuCanvas.gameObject.SetActive(true);
            if (statusText) statusText.text = "이야기를 잠시 멈췄어요.\n준비되면 계속해요.";
            input.RequireFreshChoiceClick();
            HideSceneCanvases();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            if(input&&input.HeightCalibrationPending)input.RequestHeightCalibration();
            IsPaused = false;
            ProjectWait.M.Shared.StoryMusic.SetMenuDucked(this, false);
            menuCanvas.gameObject.SetActive(false);
            RestoreCanvases();
            input.SetExternalMenu(false);
            input.RequireFreshChoiceClick();
            if (!StorybookSceneTransition.IsBusy) ReleaseTime();
        }

        public void Restart() { Leave(ScenePath, "횡단보도", Context); }
        public void ReturnToLobby() { Leave(LobbyPath, "이야기 로비", "잠깐의 이야기를 마치고,\n다음 이야기를 만나러 가요."); }
        void Leave(string path, string title, string context)
        {
            if (IsLeaving || StorybookSceneTransition.IsBusy) return;
            var transition = StorybookSceneTransition.Current;
            // Keep the pause card open if setup is missing instead of silently unloading.
            if (!transition || !transition.TryLoad(path, title, context, viewer, success =>
            {
                if (!this) return;
                IsLeaving = false;
                if (!success) { ReleaseTime(); Pause(); if (statusText) statusText.text = "장면을 열지 못했어요.\n다시 시도해 주세요."; }
            }))
            {
                if (statusText) statusText.text = "장면을 준비하지 못했어요.\n잠시 후 다시 시도해 주세요.";
                return;
            }
            IsLeaving = true;
            Resume();
            HoldTime();
        }

        public void Quit()
        {
            QuitRequested = true;
            Resume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            if (IsPaused || StorybookSceneTransition.IsBusy) HoldTime();
            else ReleaseTime();
        }
        void LateUpdate() { if (IsPaused || StorybookSceneTransition.IsBusy) HideSceneCanvases(); }

        [BeforeRenderOrder(30000)]
        void EnforceHud() { if (IsPaused || StorybookSceneTransition.IsBusy) HideSceneCanvases(); }

        void HideSceneCanvases()
        {
            // Store only canvases actually suppressed; preserve authored disabled canvases.
            // Runtime world dialogue/choices may be created after Awake, so discover them here too.
            canvasBuffer.Clear();
            foreach (var root in gameObject.scene.GetRootGameObjects()) canvasBuffer.AddRange(root.GetComponentsInChildren<Canvas>(true));
            foreach (var canvas in canvasBuffer)
            {
                if (canvas == menuCanvas) continue;
                if (!hiddenCanvases.ContainsKey(canvas)) hiddenCanvases.Add(canvas, canvas.enabled);
                canvas.enabled = false;
            }
        }
        void RestoreCanvases()
        {
            foreach (var pair in hiddenCanvases) if (pair.Key) pair.Key.enabled = pair.Value;
            hiddenCanvases.Clear();
        }
        void HoldTime()
        {
            if (timeHeld) return;
            savedTimeScale = Time.timeScale; savedAudio = AudioListener.pause; timeHeld = true;
            Time.timeScale = 0; AudioListener.pause = true;
        }
        void ReleaseTime()
        {
            if (!timeHeld) return;
            timeHeld = false; Time.timeScale = savedTimeScale; AudioListener.pause = savedAudio;
            RestoreCanvases();
        }
        void OnDisable()
        {
            ProjectWait.M.Shared.StoryMusic.SetMenuDucked(this, false);
            Application.onBeforeRender -= EnforceHud;
            toggle?.Dispose(); toggle = null;
            resumeButton.onClick.RemoveListener(Resume);
            restartButton.onClick.RemoveListener(Restart);
            lobbyButton.onClick.RemoveListener(ReturnToLobby);
            quitButton.onClick.RemoveListener(Quit);
            IsPaused = false;
            if (menuCanvas) menuCanvas.gameObject.SetActive(false);
            if (input) input.SetExternalMenu(false);
            ReleaseTime(); RestoreCanvases();
            if (Current == this) Current = null;
        }
    }
}
