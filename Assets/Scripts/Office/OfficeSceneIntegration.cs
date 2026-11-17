using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using ProjectWait.M.Shared;
using ProjectWait.M.SceneTransition;

namespace ProjectWait.YB.OfficeBlackout
{
    [DefaultExecutionOrder(-500)]
    public sealed class OfficeSceneIntegration : MonoBehaviour
    {
        public const string ScenePath = "Assets/Scenes/Office/OfficeScene.unity";
        public const string LobbyPath = "Assets/Scenes/Lobby/Wait_Robby.unity";
        public const string Context = "친구와 함께 선생님의 심부름으로,\n교무실에 서류를 가져다주러 왔어요.";
        public Camera viewer;
        public OfficeBlackoutPlayerMovement movement;
        public OfficeBlackoutDirector director;
        public OfficeBlackoutPresentation presentation;
        public Canvas menuCanvas;
        public UnityEngine.UI.Button resumeButton, restartButton, lobbyButton, quitButton;
        public TMP_Text statusText;
        public TMP_FontAsset transitionFont;
        public UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule uiModule;
        public Transform leftRay, rightRay;
        public static OfficeSceneIntegration Current { get; private set; }
        public static bool GameplayBlocked => OfficeStoryTime.Frozen || Current &&
            (Current.IsCompleted || Current.EscortMoving || Current.presentation && Current.presentation.DialogueVisible);
        public bool IsPaused { get; private set; }
        public bool IsCompleted => director && director.Flow != null && director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Complete;
        public bool EscortMoving { get; set; }
        public bool ClickReady => clickGate.Ready;
        readonly FreshPressGate clickGate = new FreshPressGate();
        readonly Dictionary<AudioSource, bool> pausedSources = new Dictionary<AudioSource, bool>();
        readonly Dictionary<AudioSource, float> pausedVolumes = new Dictionary<AudioSource, float>();
        readonly Dictionary<ParticleSystem, bool> particles = new Dictionary<ParticleSystem, bool>();
        InputAction pauseAction, uiAction;
        float savedScale = 1;
        double menuReady;
        UnityEngine.UI.Button hovered;
        bool cursorCaptured, oldVisible;
        CursorLockMode oldLock;

        void Awake()
        {
            Current = this;
            pauseAction = new InputAction("OfficePause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<XRController>{LeftHand}/menuButton");
            pauseAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            uiAction = new InputAction("OfficeMenuTrigger", InputActionType.Button);
            uiAction.AddBinding("<XRController>/{TriggerButton}");
            uiAction.AddBinding("<Keyboard>/e");
            uiAction.AddBinding("<Keyboard>/space");
            uiAction.performed += OnUITrigger; uiAction.Enable(); pauseAction.Enable();
            resumeButton.onClick.AddListener(Resume); restartButton.onClick.AddListener(Restart);
            lobbyButton.onClick.AddListener(ReturnToLobby); quitButton.onClick.AddListener(Quit);
            menuCanvas.gameObject.SetActive(false);
            RequireFreshClick();
        }
        void Update()
        {
            clickGate.Observe(Time.unscaledTimeAsDouble, SelectHeld());
            if (uiModule) uiModule.enableMouseInput = !XRSettings.isDeviceActive &&
                (!(IsPaused || IsCompleted) || clickGate.Ready && Time.unscaledTimeAsDouble >= menuReady);
            if (pauseAction.WasPressedThisFrame() && !StorybookSceneTransition.IsBusy)
            { if (IsPaused) Resume(); else Pause(); }
            if (XRSettings.isDeviceActive && !StorybookSceneTransition.IsBusy) SetHover(HitUI(DefaultRay()));
            if (!IsPaused) return;
            foreach (var pair in pausedSources) if (pair.Key && !pair.Key.GetComponent<StoryMusic>()) pair.Key.volume = 0;
            if (XRSettings.isDeviceActive) SetHover(HitUI(DefaultRay()));
        }
        public static bool SelectHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed || Keyboard.current != null &&
                (Keyboard.current.eKey.isPressed || Keyboard.current.spaceKey.isPressed)) return true;
            foreach (var device in InputSystem.devices) if (device is UnityEngine.InputSystem.XR.XRController)
                foreach (var control in device.allControls) if (control is ButtonControl button && button.isPressed &&
                    (control.name == "trigger" || control.name == "triggerPressed" || control.name == "triggerButton")) return true;
            return false;
        }
        public void RequireFreshClick() { clickGate.Arm(Time.unscaledTimeAsDouble); }
        public Ray AimRay(InputControl control = null)
        {
            if (XRSettings.isDeviceActive)
            {
                bool left = control != null && control.device.usages.Contains(UnityEngine.InputSystem.CommonUsages.LeftHand);
                Transform hand = left ? leftRay : rightRay;
                if (hand) return new Ray(hand.position, hand.forward);
            }
            Vector2 point = Mouse.current != null && Cursor.lockState != CursorLockMode.Locked ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * .5f, Screen.height * .5f);
            return viewer.ScreenPointToRay(point);
        }
        public Ray DefaultRay() => AimRay();
        void PlaceMenu()
        {
            Vector3 forward = XRSettings.isDeviceActive ? Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized : viewer.transform.forward;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            menuCanvas.transform.SetPositionAndRotation(viewer.transform.position + forward * 1.4f, Quaternion.LookRotation(forward, Vector3.up));
            menuCanvas.transform.localScale = Vector3.one * .0016f;
            menuCanvas.worldCamera = viewer;
        }
        public void Pause()
        {
            if (IsPaused || StorybookSceneTransition.IsBusy) return;
            IsPaused = true; savedScale = Time.timeScale; Time.timeScale = 0;
            oldLock = Cursor.lockState; oldVisible = Cursor.visible; cursorCaptured = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = !XRSettings.isDeviceActive;
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source.GetComponent<StoryMusic>() || source.gameObject.scene != gameObject.scene) continue;
                pausedSources[source] = source.isPlaying; pausedVolumes[source] = source.volume;
                if (source.isPlaying) source.Pause(); source.volume = 0;
            }
            foreach (var system in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            { particles[system] = system.isPlaying; if (system.isPlaying) system.Pause(false); }
            StoryMusic.SetMenuDucked(this, true);
            presentation.SetMenuVisible(true); PlaceMenu(); menuCanvas.gameObject.SetActive(true);
            var settings = menuCanvas.GetComponent<ExperienceSettingsMenu>(); if (settings) settings.Close();
            if (statusText) statusText.text = "이야기를 잠시 멈췄어요.";
            menuReady = Time.unscaledTimeAsDouble + .25; RequireFreshClick();
        }
        public void Resume()
        {
            if (!IsPaused || StorybookSceneTransition.IsBusy) return;
            IsPaused = false; Time.timeScale = savedScale;
            foreach (var pair in pausedSources) if (pair.Key)
            { if (pausedVolumes.TryGetValue(pair.Key, out float volume)) pair.Key.volume = volume; if (pair.Value) pair.Key.UnPause(); }
            pausedSources.Clear(); pausedVolumes.Clear();
            foreach (var pair in particles) if (pair.Key && pair.Value) pair.Key.Play(false); particles.Clear();
            StoryMusic.SetMenuDucked(this, false);
            presentation.SetMenuVisible(false); menuCanvas.gameObject.SetActive(false); SetHover(null);
            if (cursorCaptured) { Cursor.lockState = oldLock; Cursor.visible = oldVisible; cursorCaptured = false; }
            movement.SyncCursor(); RequireFreshClick();
        }
        public void Restart() => Load(ScenePath, "교무실 정전", Context);
        public void ReturnToLobby() => Load(LobbyPath, "선택 광장", "다른 이야기도 함께 만나 볼까요?");
        void Load(string path, string title, string context)
        {
            if (StorybookSceneTransition.IsBusy) return;
            if (IsPaused) Resume();
            // Release the local comfort cover; scene departures use only the approved droplets.
            var cover = ComfortScreenFade.For(viewer); if (cover) cover.SetOpacity(0);
            var transition = StorybookSceneTransition.Current;
            if (!transition || !transition.TryLoad(path, title, context, viewer, success =>
                { if (!success && this) { presentation.RestoreDeparture(); Pause(); if (statusText) statusText.text = "장면을 열지 못했어요. 다시 시도해 주세요."; } }, transitionFont))
            { Pause(); if (statusText) statusText.text = "장면을 열지 못했어요. 다시 시도해 주세요."; }
            else presentation.PrepareDeparture();
        }
        public void Quit()
        {
#if UNITY_EDITOR
            Resume(); UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnUITrigger(InputAction.CallbackContext context)
        {
            if (!clickGate.Ready || Time.unscaledTimeAsDouble < menuReady || StorybookSceneTransition.IsBusy) return;
            if (context.control.device is Keyboard && !IsPaused && !IsCompleted)
            {
                if (presentation.DialogueVisible) presentation.AdvanceLine();
                else presentation.ConfirmAction();
                return;
            }
            if (!XRSettings.isDeviceActive) return;
            var button = HitUI(AimRay(context.control));
            // Listeners consume and re-arm the gate after accepting an action.
            if (button) { button.onClick.Invoke(); RequireFreshClick(); }
        }
        UnityEngine.UI.Button HitUI(Ray ray)
        {
            UnityEngine.UI.Button best = null; float distance = float.MaxValue;
            var buttons = IsPaused ? menuCanvas.GetComponentsInChildren<UnityEngine.UI.Button>() :
                IsCompleted ? presentation.CompletionButtons : presentation.StoryButtons;
            foreach (var button in buttons)
            {
                if (!button || !button.isActiveAndEnabled || !button.IsInteractable()) continue;
                var rect = (RectTransform)button.transform;
                if (!new Plane(rect.forward, rect.position).Raycast(ray, out float hit) || hit < 0 || hit >= distance) continue;
                if (!rect.rect.Contains(rect.InverseTransformPoint(ray.GetPoint(hit)))) continue;
                best = button; distance = hit;
            }
            return best;
        }
        void SetHover(UnityEngine.UI.Button value)
        {
            if (hovered == value) return;
            var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            if (hovered) hovered.OnPointerExit(data); hovered = value; if (hovered) hovered.OnPointerEnter(data);
        }
        void OnDestroy()
        {
            StoryMusic.SetMenuDucked(this, false);
            pauseAction?.Dispose(); uiAction?.Dispose();
            if (IsPaused) Time.timeScale = savedScale;
            if (Current == this) Current = null;
        }
    }
}
