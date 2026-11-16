using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// Camera-locked letterbox, subtitle, and fade overlay for the office blackout scene.
    /// Add this to a top-level GameObject with a Canvas and RectTransform.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public sealed class OfficeBlackoutCinematicOverlay : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] Camera playerCamera;
        [SerializeField, Min(0.1f)] float distanceFromCamera = 0.45f;
        [SerializeField] OfficeBlackoutPlayerMovement playerMovement;

        [Header("Reusable visual assets")]
        [SerializeField] Material panelMaterial;
        [SerializeField] Material captionMaterial;
        [SerializeField] TMP_FontAsset captionFont;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] float letterboxDuration = 0.38f;
        [SerializeField, Min(0.01f)] float fadeDuration = 0.35f;
        [SerializeField] bool clearCaptionWhenHidden = true;

        [System.Serializable]
        public sealed class Dialogue
        {
            public string title;
            [TextArea(2, 4)] public string[] lines = System.Array.Empty<string>();
        }

        [System.Serializable]
        public sealed class DialogueIndexEvent : UnityEvent<int> { }

        [Header("Indexed dialogues (indices start at 1)")]
        [SerializeField] Dialogue[] dialogues = System.Array.Empty<Dialogue>();
        [SerializeField, Min(0.1f)] float captionIntervalSeconds = 2.5f;
        [SerializeField] bool showLetterboxForSequence = true;
        [SerializeField] bool hideLetterboxWhenSequenceEnds = true;
        [SerializeField] UnityEvent onCaptionSequenceCompleted = new UnityEvent();
        [SerializeField] UnityEvent onFadeToBlackCompleted = new UnityEvent();
        [SerializeField] DialogueIndexEvent onDialogueStarted = new DialogueIndexEvent();
        [SerializeField] DialogueIndexEvent onDialogueCompleted = new DialogueIndexEvent();

        RectTransform topBar;
        RectTransform bottomBar;
        RectTransform fadePanel;
        TMP_Text caption;
        float amount;
        float targetAmount;
        float fadeAlpha;
        float targetFadeAlpha;
        bool built;
        bool movementLocked;
        bool movementWasEnabled;
        Coroutine captionSequenceRoutine;
        bool fadeCompletionInvoked;

        public bool IsVisible => amount > 0.01f;
        public float Amount => amount;
        public float FadeAlpha => fadeAlpha;

        void OnEnable()
        {
            Build();
            Application.onBeforeRender += PositionOverlay;
            PositionOverlay();
        }

        void OnDisable()
        {
            Application.onBeforeRender -= PositionOverlay;
            captionSequenceRoutine = null;
            ReleaseMovementLock();
        }

        void LateUpdate()
        {
            float delta = Time.unscaledDeltaTime;
            amount = Mathf.MoveTowards(amount, targetAmount, delta / Mathf.Max(0.01f, letterboxDuration));
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, targetFadeAlpha, delta / Mathf.Max(0.01f, fadeDuration));
            UpdateVisuals();
            PositionOverlay();

            if (targetFadeAlpha >= 1f && fadeAlpha >= 0.999f && !fadeCompletionInvoked)
            {
                fadeCompletionInvoked = true;
                onFadeToBlackCompleted?.Invoke();
            }

            if (movementLocked && captionSequenceRoutine == null && targetAmount <= 0f && amount <= 0.01f &&
                targetFadeAlpha <= 0f && fadeAlpha <= 0.001f)
                ReleaseMovementLock();
        }

        /// <summary>Show the top and bottom cinematic bars.</summary>
        public void ShowLetterbox()
        {
            AcquireMovementLock();
            targetAmount = 1f;
        }

        /// <summary>Hide the cinematic bars.</summary>
        public void HideLetterbox()
        {
            targetAmount = 0f;
            if (clearCaptionWhenHidden) ClearCaption();
        }

        /// <summary>Set the subtitle shown within the bottom bar.</summary>
        public void SetCaption(string text)
        {
            Build();
            caption.text = text ?? string.Empty;
            caption.gameObject.SetActive(!string.IsNullOrWhiteSpace(caption.text));
        }

        public void ClearCaption()
        {
            if (!caption) return;
            caption.text = string.Empty;
            caption.gameObject.SetActive(false);
        }

        /// <summary>Plays a configured dialogue by its one-based Inspector index.</summary>
        public void PlayDialogue(int dialogueIndex)
        {
            if (dialogues == null || dialogueIndex < 1 || dialogueIndex > dialogues.Length)
            {
                Debug.LogWarning($"Dialogue index {dialogueIndex} is not configured on {name}.", this);
                return;
            }

            Dialogue dialogue = dialogues[dialogueIndex - 1];
            if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0) return;
            StartCaptionSequence(dialogueIndex, dialogue.lines);
        }

        void StartCaptionSequence(int dialogueIndex, string[] lines)
        {
            StopCaptionSequence();
            Build();
            AcquireMovementLock();
            onDialogueStarted?.Invoke(dialogueIndex);
            if (showLetterboxForSequence) ShowLetterbox();
            captionSequenceRoutine = StartCoroutine(RunCaptionSequence(dialogueIndex, lines));
        }

        void AcquireMovementLock()
        {
            if (movementLocked || !playerMovement) return;
            movementWasEnabled = playerMovement.IsMovementManuallyEnabled;
            movementLocked = true;
            playerMovement.SetMovementEnabled(false);
        }

        void ReleaseMovementLock()
        {
            if (!movementLocked) return;
            if (playerMovement) playerMovement.SetMovementEnabled(movementWasEnabled);
            movementLocked = false;
        }

        /// <summary>Stops timed captions and clears the current line.</summary>
        public void StopCaptionSequence()
        {
            if (captionSequenceRoutine != null)
            {
                StopCoroutine(captionSequenceRoutine);
                captionSequenceRoutine = null;
            }
            ClearCaption();
        }

        IEnumerator RunCaptionSequence(int dialogueIndex, string[] lines)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                SetCaption(line);
                yield return new WaitForSecondsRealtime(captionIntervalSeconds);
            }

            ClearCaption();
            if (hideLetterboxWhenSequenceEnds) targetAmount = 0f;
            while (hideLetterboxWhenSequenceEnds && amount > 0.01f)
                yield return null;

            captionSequenceRoutine = null;
            if ((hideLetterboxWhenSequenceEnds || targetAmount <= 0f) &&
                targetFadeAlpha <= 0f && fadeAlpha <= 0.001f)
                ReleaseMovementLock();
            onDialogueCompleted?.Invoke(dialogueIndex);
            onCaptionSequenceCompleted?.Invoke();
        }

        /// <summary>Fade the camera view to black. Call FadeFromBlack to reveal it again.</summary>
        public void FadeToBlack()
        {
            AcquireMovementLock();
            fadeCompletionInvoked = false;
            targetFadeAlpha = 1f;
        }

        public void FadeFromBlack()
        {
            targetFadeAlpha = 0f;
            fadeCompletionInvoked = false;
        }

        public void ToggleLetterbox()
        {
            if (targetAmount > 0f) HideLetterbox();
            else ShowLetterbox();
        }

        void Build()
        {
            if (built) return;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = playerCamera;
            canvas.sortingOrder = 100;

            var root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;

            topBar = CreatePanel("TopLetterbox", root);
            bottomBar = CreatePanel("BottomLetterbox", root);
            fadePanel = CreatePanel("XRComfortFade", root);

            var captionObject = new GameObject("CinematicSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var captionRect = (RectTransform)captionObject.transform;
            captionRect.SetParent(bottomBar, false);
            captionRect.anchorMin = captionRect.anchorMax = captionRect.pivot = new Vector2(0.5f, 0.5f);
            captionRect.anchoredPosition = new Vector2(0f, -2f);
            captionRect.sizeDelta = new Vector2(1100f, 76f);
            caption = captionObject.GetComponent<TextMeshProUGUI>();
            caption.font = captionFont;
            caption.fontSize = 34f;
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 22f;
            caption.fontSizeMax = 34f;
            caption.color = Color.white;
            caption.alignment = TextAlignmentOptions.Center;
            caption.textWrappingMode = TextWrappingModes.Normal;
            caption.raycastTarget = false;
            if (captionMaterial) caption.fontSharedMaterial = captionMaterial;

            built = true;
            UpdateVisuals();
        }

        RectTransform CreatePanel(string panelName, RectTransform parent)
        {
            var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)panelObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var image = panelObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            if (panelMaterial) image.material = panelMaterial;
            return rect;
        }

        void UpdateVisuals()
        {
            if (!built) return;
            bool barsVisible = amount > 0.001f;
            topBar.gameObject.SetActive(barsVisible);
            bottomBar.gameObject.SetActive(barsVisible);
            fadePanel.gameObject.SetActive(fadeAlpha > 0.001f);

            float width = playerCamera ? 720f * playerCamera.aspect : 1280f;
            topBar.sizeDelta = bottomBar.sizeDelta = new Vector2(width + 24f, 84f);
            float slide = Mathf.SmoothStep(0f, 1f, amount) * 84f;
            topBar.anchoredPosition = new Vector2(0f, 401f - slide);
            bottomBar.anchoredPosition = new Vector2(0f, -401f + slide);

            fadePanel.sizeDelta = XRSettings.isDeviceActive
                ? new Vector2(2880f, 2880f)
                : new Vector2(width + 24f, 744f);
            var fadeImage = fadePanel.GetComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, fadeAlpha);
        }

        void PositionOverlay()
        {
            if (!playerCamera) return;
            var canvas = GetComponent<Canvas>();
            if (canvas) canvas.worldCamera = playerCamera;

            float height = 2f * distanceFromCamera * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            transform.SetPositionAndRotation(
                playerCamera.transform.position + playerCamera.transform.forward * distanceFromCamera,
                playerCamera.transform.rotation);
            transform.localScale = Vector3.one * (height / 720f);

            var root = (RectTransform)transform;
            float width = 720f * playerCamera.aspect;
            root.sizeDelta = new Vector2(width, 720f);
            if (caption) caption.rectTransform.sizeDelta = new Vector2(Mathf.Min(1100f, width - 48f), 76f);
        }
    }
}
