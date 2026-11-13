using TMPro;
using UnityEngine;
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

        [Header("Reusable visual assets")]
        [SerializeField] Material panelMaterial;
        [SerializeField] Material captionMaterial;
        [SerializeField] TMP_FontAsset captionFont;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] float letterboxDuration = 0.38f;
        [SerializeField, Min(0.01f)] float fadeDuration = 0.35f;
        [SerializeField] bool clearCaptionWhenHidden = true;

        RectTransform topBar;
        RectTransform bottomBar;
        RectTransform fadePanel;
        TMP_Text caption;
        float amount;
        float targetAmount;
        float fadeAlpha;
        float targetFadeAlpha;
        bool built;

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
        }

        void LateUpdate()
        {
            float delta = Time.unscaledDeltaTime;
            amount = Mathf.MoveTowards(amount, targetAmount, delta / Mathf.Max(0.01f, letterboxDuration));
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, targetFadeAlpha, delta / Mathf.Max(0.01f, fadeDuration));
            UpdateVisuals();
            PositionOverlay();
        }

        /// <summary>Show the top and bottom cinematic bars.</summary>
        public void ShowLetterbox()
        {
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

        /// <summary>Fade the camera view to black. Call FadeFromBlack to reveal it again.</summary>
        public void FadeToBlack()
        {
            targetFadeAlpha = 1f;
        }

        public void FadeFromBlack()
        {
            targetFadeAlpha = 0f;
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
