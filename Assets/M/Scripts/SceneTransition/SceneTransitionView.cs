using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ProjectWait.M.SceneTransition
{
    [DefaultExecutionOrder(30000)]
    public sealed class SceneTransitionView : MonoBehaviour
    {
        public static readonly Color Ink = new Color(.28f, .19f, .14f);
        public static readonly Color Paper = new Color(1, .965f, .86f);
        public static readonly Color Teal = new Color(.16f, .48f, .44f);
        public static readonly Color Mint = new Color(.79f, .89f, .76f);

        public Canvas Canvas { get; private set; }
        public CanvasGroup CardGroup { get; private set; }
        public TMP_Text ContextText { get; private set; }
        public TMP_Text HeadingText { get; private set; }
        public TMP_Text StatusText { get; private set; }
        public float SkyCoverAlpha { get; private set; }
        public float CardAlpha => CardGroup ? CardGroup.alpha : 0;
        public Camera ViewCamera { get; private set; }
        public RectTransform[] Dots { get; private set; }
        public Vector3 PanelAnchor => anchorPosition;
        public Transform PanelTransform => card;
        public float PanelLerpSpeed { get; set; } = 12;
        public float PanelDistance { get; set; } = 2;
        public float PanelWidth { get; set; } = 1.65f;

        RectTransform root, card;
        Image skyBridge;
        Material skyMaterial;
        Material uiMaterial;
        TMP_FontAsset font;
        Canvas skyCanvas;
        RectTransform skyRoot;
        Vector3 anchorPosition, lastEye;
        Quaternion anchorRotation, lastEyeRotation;
        bool anchored, hasLastEye;
        float panelTarget;

        public void Build(TMP_FontAsset textFont, Material overlayMaterial, Shader skyShader)
        {
            if (Canvas) return;
            font = textFont;
            uiMaterial = overlayMaterial;
            root = (RectTransform)transform;
            root.sizeDelta = new Vector2(1280, 720);
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.WorldSpace;
            Canvas.sortingOrder = 32760;
            Canvas.overrideSorting = true;
            var group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = true;

            skyRoot = Rect("Sky-only camera follower", transform.parent, new Vector2(1280,720), Vector2.zero);
            skyCanvas = skyRoot.gameObject.AddComponent<Canvas>();
            skyCanvas.renderMode = RenderMode.WorldSpace;
            skyCanvas.overrideSorting = true;
            skyCanvas.sortingOrder = 32750;
            var back = Rect("Unchanged sky bridge (no dark veil)", skyRoot, new Vector2(12000, 12000), Vector2.zero);
            skyMaterial = new Material(skyShader) { name = "Story transition sky bridge (runtime)", hideFlags = HideFlags.HideAndDontSave };
            skyBridge = back.gameObject.AddComponent<Image>();
            skyBridge.material = skyMaterial;
            skyBridge.raycastTarget = true;
            card = Rect("Story card", root, new Vector2(840, 368), Vector2.zero);
            CardGroup = card.gameObject.AddComponent<CanvasGroup>();
            Shape("Step shadow", card, new Vector2(850, 376), new Vector2(0, -12), 35, new Color(.10f, .08f, .06f, .35f));
            Shape("Brown paper edge", card, new Vector2(840, 368), Vector2.zero, 32, Ink);
            Shape("Paper bevel", card, new Vector2(832, 360), new Vector2(0, 1), 29, new Color(.87f, .79f, .62f));
            Shape("Cream paper face", card, new Vector2(828, 350), new Vector2(0, 5), 27, Paper);
            Shape("Mint chapter tab", card, new Vector2(360, 46), new Vector2(0, 124), 23, Mint);
            HeadingText = Text("Chapter", card, new Vector2(600, 42), new Vector2(0, 124), 24, Teal);
            ContextText = Text("Situation, not a solution", card, new Vector2(744, 124), new Vector2(0, 28), 36, Ink);
            ContextText.lineSpacing = 14;
            Shape("Short teal divider", card, new Vector2(52, 3), new Vector2(0, -55), 1.5f, new Color(.48f, .65f, .54f));
            StatusText = Text("Preparing next scene", card, new Vector2(650, 36), new Vector2(0, -86), 22, Teal);
            StatusText.text = "다음 장면을 준비하고 있어요";
            Dots = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 32;
                Shape("Dot ground shadow " + i, card, new Vector2(15, 3), new Vector2(x, -140), 1.5f, new Color(.28f, .19f, .14f, .14f));
                Dots[i] = Rect("Jumping dot " + (i + 1), card, new Vector2(18, 18), new Vector2(x, -128));
                Shape("Ink edge", Dots[i], new Vector2(18, 18), Vector2.zero, 9, Ink);
                Shape("Teal face", Dots[i], new Vector2(13, 13), new Vector2(0, 1), 6.5f, Teal);
                Shape("Mint glint", Dots[i], new Vector2(5, 3), new Vector2(-2, 3), 1.5f, Mint);
            }
            Hide();
        }

        public void SetContent(string title, string context)
        {
            HeadingText.text = "다음 이야기  ·  " + title;
            ContextText.text = context;
            StatusText.text = "다음 장면을 준비하고 있어요";
        }

        public void BindCamera(Camera camera)
        {
            if (camera && anchored && hasLastEye)
            {
                Quaternion delta = camera.transform.rotation * Quaternion.Inverse(lastEyeRotation);
                anchorPosition = camera.transform.position + delta * (anchorPosition - lastEye);
                anchorRotation = delta * anchorRotation;
                ApplyPanelPose();
            }
            ViewCamera = camera;
            if (Canvas) Canvas.worldCamera = camera;
            if (skyCanvas) skyCanvas.worldCamera = camera;
            Position();
        }

        public void BeginPanel()
        {
            anchored = false;
            panelTarget = 0;
            CardGroup.alpha = 0;
            Canvas.enabled = false;
        }

        public void SetSky(Material sourceSky)
        {
            skyMaterial.SetTexture("_SkyTex", sourceSky.GetTexture("_MainTex"));
            skyMaterial.SetColor("_Tint", sourceSky.GetColor("_Tint"));
            skyMaterial.SetFloat("_Exposure", sourceSky.GetFloat("_Exposure"));
            skyMaterial.SetFloat("_Rotation", sourceSky.GetFloat("_Rotation"));
        }

        public void Present(float skyCoverAlpha, float panelAlpha, float animationSeconds)
        {
            if (!Canvas) return;
            SkyCoverAlpha = Mathf.Clamp01(skyCoverAlpha);
            skyBridge.color = new Color(1, 1, 1, SkyCoverAlpha);
            panelTarget = Mathf.Clamp01(panelAlpha);
            if (!anchored && panelTarget > 0) AnchorPanel();
            skyCanvas.enabled = SkyCoverAlpha > 0;
            for (int i = 0; i < Dots.Length; i++)
            {
                float jump = DotJump(animationSeconds, i);
                Dots[i].anchoredPosition = new Vector2((i - 1) * 32, -128 + jump * 15);
                Dots[i].localScale = new Vector3(1 - jump * .07f, 1 + jump * .1f, 1);
            }
            Position();
        }

        // Exponential Lerp is frame-rate independent and needs no tweens, coroutines or allocations.
        public void AdvancePanel(float seconds)
        {
            if (!Canvas || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float amount = 1 - Mathf.Exp(-Mathf.Max(.1f, PanelLerpSpeed) * seconds);
            float alpha = Mathf.Lerp(CardGroup.alpha, panelTarget, amount);
            if (Mathf.Abs(alpha-panelTarget) < .002f) alpha = panelTarget;
            CardGroup.alpha = alpha;
            Canvas.enabled = alpha > .001f || panelTarget > 0;
            ApplyPanelPose();
        }

        void AnchorPanel()
        {
            if (!ViewCamera) return;
            Vector3 forward = ViewCamera.transform.forward;
            float pitch = Mathf.Asin(Mathf.Clamp(forward.y,-1,1)) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(forward.x,forward.z) * Mathf.Rad2Deg;
            anchorRotation = Quaternion.Euler(-Mathf.Clamp(pitch,-15,15),yaw,0);
            anchorPosition = ViewCamera.transform.position + anchorRotation * Vector3.forward * PanelDistance;
            anchored = true;
            transform.localScale = Vector3.one * (PanelWidth / 840);
            ApplyPanelPose();
        }

        void ApplyPanelPose()
        {
            if (!anchored || !CardGroup) return;
            float alpha = CardGroup.alpha;
            transform.position = Vector3.Lerp(anchorPosition + Vector3.down * .12f, anchorPosition, alpha);
            transform.rotation = Quaternion.Slerp(anchorRotation * Quaternion.Euler(5,0,0), anchorRotation, alpha);
            card.localScale = Vector3.one * Mathf.Lerp(.96f,1,alpha);
        }

        public static float DotJump(float seconds, int index)
        {
            float local = Mathf.Repeat(seconds - index * .14f, 1.05f);
            return local < .38f ? Mathf.Sin(local / .38f * Mathf.PI) : 0;
        }

        public void Hide()
        {
            if (!Canvas) return;
            SkyCoverAlpha = 0;
            CardGroup.alpha = 0;
            panelTarget = 0;
            Canvas.enabled = false;
            if (skyCanvas) skyCanvas.enabled = false;
        }

        void OnEnable() { Application.onBeforeRender += Position; }
        void OnDisable() { Application.onBeforeRender -= Position; }
        void OnDestroy()
        {
            if (skyMaterial) { if (Application.isPlaying) Destroy(skyMaterial); else DestroyImmediate(skyMaterial); }
            if (skyRoot) { if (Application.isPlaying) Destroy(skyRoot.gameObject); else DestroyImmediate(skyRoot.gameObject); }
        }
        void LateUpdate() { if (skyCanvas && skyCanvas.enabled) Position(); }

        public void Position()
        {
            if (!ViewCamera || !root) return;
            const float distance = 1.5f;
            bool xr = XRSettings.isDeviceActive && ViewCamera.stereoTargetEye != StereoTargetEyeMask.None;
            float height = xr ? 1.55f : ViewCamera.orthographic ? ViewCamera.orthographicSize * 2 : 2 * distance * Mathf.Tan(ViewCamera.fieldOfView * .5f * Mathf.Deg2Rad);
            skyRoot.SetPositionAndRotation(ViewCamera.transform.position + ViewCamera.transform.forward * distance, ViewCamera.transform.rotation);
            skyRoot.localScale = Vector3.one * (height / 720);
            lastEye = ViewCamera.transform.position;
            lastEyeRotation = ViewCamera.transform.rotation;
            hasLastEye = true;
        }

        TransitionShapeGraphic Shape(string name, Transform parent, Vector2 size, Vector2 position, float radius, Color color)
        {
            var rect = Rect(name, parent, size, position);
            var graphic = rect.gameObject.AddComponent<TransitionShapeGraphic>();
            graphic.radius = radius;
            graphic.color = color;
            graphic.material = uiMaterial;
            graphic.raycastTarget = false;
            return graphic;
        }

        TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position, float fontSize, Color color)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.richText = false;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
