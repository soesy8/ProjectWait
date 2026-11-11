#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using ProjectWait.M.Elevator.DialoguePreview;

namespace ProjectWait.M.VRPreview
{
    // Preview-only framing. Production story/UI code and provider projection stay untouched.
    // Fit geometry to the provider's actual mirror ROI AND the two eye frusta.
    [DefaultExecutionOrder(31000)]
    public sealed class VRPreviewFraming : MonoBehaviour
    {
        public bool SkipMirrorForTesting { get; set; }
        VRPreviewTest owner;
        Camera camera;
        XRDisplaySubsystem display;
        ElevatorLesson lesson;
        VRPreviewScreenSpace screenSpace;
        VRPreviewReading reading;
        readonly Dictionary<TMP_Text, float> originalFonts = new Dictionary<TMP_Text, float>();
        readonly Dictionary<Image, bool> barStates = new Dictionary<Image, bool>();
        RectTransform compactCaption;
        readonly Rect[] crops = new Rect[2];
        readonly Dictionary<RectTransform, State> original = new Dictionary<RectTransform, State>();
        struct State { public Vector2 position, size; public Vector3 scale; public bool sizeChanged; }
        public void Initialize(VRPreviewTest value, Camera view, XRDisplaySubsystem provider)
        {
            owner = value; camera = view; display = provider; lesson = FindFirstObjectByType<ElevatorLesson>();
            screenSpace = gameObject.AddComponent<VRPreviewScreenSpace>(); screenSpace.Initialize(lesson, camera);
            reading = gameObject.AddComponent<VRPreviewReading>(); reading.Initialize(owner, this, lesson, camera);
        }
        void OnEnable() { Application.onBeforeRender += Fit; if (screenSpace) screenSpace.enabled = true; if (reading) reading.enabled = true; }
        void LateUpdate() { Fit(); }
        [BeforeRenderOrder(1000)]
        void Fit()
        {
            if (!owner || !owner.Running || !camera || !camera.stereoEnabled || !lesson) return;
            screenSpace.Sync();
            int mode = owner.showBothEyes ? XRMirrorViewBlitMode.SideBySide : XRMirrorViewBlitMode.LeftEye;
            // Keep the provider's supported null-target query. Passing a Game-view
            // texture during XR startup can enter a native Mock HMD crash path.
            // Without mirror ROI data, use the actual full native-eye frusta.
            // World anchors and image-only bar suppression do not depend on it.
            crops[0] = crops[1] = new Rect(0, 0, 1, 1);
            if (!SkipMirrorForTesting && display != null && display.running &&
                display.GetMirrorViewBlitDesc(null, out var desc, mode) && desc.blitParamsCount > 0)
            {
                desc.GetBlitParameter(0, out var first); crops[0] = crops[1] = MirrorCrop(first.srcRect, first.srcTex, first.destRect);
                for (int i = 0; i < desc.blitParamsCount; i++)
                { desc.GetBlitParameter(i, out var p); if (p.srcTexArraySlice >= 0 && p.srcTexArraySlice < 2) crops[p.srcTexArraySlice] = MirrorCrop(p.srcRect, p.srcTex, p.destRect); }
            }
            var overlay = lesson.presentation.space.overlay;
            if (overlay)
            {
                var root = screenSpace.Cinematic ? screenSpace.Cinematic.transform as RectTransform : null;
                var top = root ? root.Find("TopLetterbox") as RectTransform : null;
                var bottom = root ? root.Find("BottomLetterbox") as RectTransform : null;
                foreach (var bar in new[] { top ? top.GetComponent<Image>() : null, bottom ? bottom.GetComponent<Image>() : null }) if (bar)
                {
                    if (!barStates.ContainsKey(bar)) barStates.Add(bar, bar.enabled);
                    bar.enabled = owner.showLetterbox && barStates[bar];
                }
                if (root && top && bottom && TryRects(root, out var left, out var right))
                {
                    Capture(top, true); Capture(bottom, true);
                    var caption = bottom.Find("CinematicSubtitle") as RectTransform;
                    var text = caption ? caption.GetComponent<TMP_Text>() : null;
                    float bandReference = 720 * owner.letterboxHeight;
                    if (text)
                    {
                        if (!originalFonts.ContainsKey(text)) originalFonts.Add(text, text.fontSize);
                        text.fontSize = owner.captionFontSize;
                        if (text.font) bandReference = Mathf.Max(bandReference,
                            text.font.faceInfo.lineHeight / Mathf.Max(1, text.font.faceInfo.pointSize) * text.fontSize * 2 + 16);
                    }
                    float height = Mathf.Min(left.height, right.height), band = height * (bandReference / 720);
                    float progress = Mathf.SmoothStep(0, 1, overlay.Amount);
                    float minX = Mathf.Min(left.xMin, right.xMin) - height * .02f, maxX = Mathf.Max(left.xMax, right.xMax) + height * .02f;
                    float topOuter = Mathf.Max(left.yMax, right.yMax) + height * .02f;
                    float topInner = Mathf.Min(left.yMax, right.yMax) - band * progress;
                    float bottomOuter = Mathf.Min(left.yMin, right.yMin) - height * .02f;
                    float bottomInner = Mathf.Max(left.yMin, right.yMin) + band * progress;
                    // Keep the original horizontal overscan: asymmetric eyes must not
                    // expose a sliver of scenery beside the black band in the mirror.
                    float bandWidth = Mathf.Max(maxX - minX, Mathf.Max(original[top].size.x, original[bottom].size.x));
                    top.sizeDelta = new Vector2(bandWidth, topOuter - topInner);
                    bottom.sizeDelta = new Vector2(bandWidth, bottomInner - bottomOuter);
                    top.anchoredPosition = new Vector2((minX + maxX) * .5f, (topOuter + topInner) * .5f);
                    bottom.anchoredPosition = new Vector2((minX + maxX) * .5f, (bottomInner + bottomOuter) * .5f);
                    if (caption && TryRects(bottom, out var cl, out var cr))
                    {
                        Capture(caption, true); float scale = height / 720;
                        float width = Mathf.Min(cl.xMax, cr.xMax) - Mathf.Max(cl.xMin, cr.xMin);
                        caption.localScale = original[caption].scale * scale;
                        caption.sizeDelta = new Vector2(Mathf.Max(1, Mathf.Min(760, width / scale - 48)), bandReference - 16);
                        // Fonts and geometry share one reference-height scale.
                        caption.anchoredPosition = new Vector2((Mathf.Max(cl.xMin, cr.xMin) + Mathf.Min(cl.xMax, cr.xMax)) * .5f,
                            (Mathf.Max(cl.yMin, cr.yMin) + bottom.rect.yMax) * .5f);
                        if (!compactCaption)
                        {
                            compactCaption = (RectTransform)new GameObject("VR Compact Subtitle Backdrop", typeof(RectTransform), typeof(Image)).transform;
                            compactCaption.SetParent(bottom, false); compactCaption.anchorMin = compactCaption.anchorMax = compactCaption.pivot = Vector2.one * .5f;
                            var image = compactCaption.GetComponent<Image>(); image.material = bottom.GetComponent<Image>().material; image.raycastTarget = false;
                            compactCaption.SetSiblingIndex(caption.GetSiblingIndex());
                        }
                        compactCaption.gameObject.SetActive(!owner.showLetterbox && text && text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text));
                        if (text)
                        {
                            var preferred = text.GetPreferredValues(text.text, caption.rect.width, 0);
                            compactCaption.sizeDelta = new Vector2(Mathf.Min(caption.rect.width, preferred.x) + 24, Mathf.Min(caption.rect.height, preferred.y) + 12);
                            compactCaption.localScale = caption.localScale; compactCaption.anchoredPosition = caption.anchoredPosition;
                            compactCaption.GetComponent<Image>().color = new Color(.28f, .19f, .14f, .8f * text.color.a);
                        }
                    }
                }
            }
            var spatial = lesson.view.spatial;
            if (spatial && spatial.hud && spatial.hud.enabled)
            {
                Place(spatial.mission, true, false);
                if (spatial.progress) Place(spatial.progress.rectTransform, true, true);
                Place(spatial.progressBackdrop, true, true);
                if (spatial.controls) Place(spatial.controls.rectTransform, false, false);
                Place(spatial.controlsBackdrop, false, false);
            }
            var tutorial = lesson.GetComponentInChildren<ElevatorTutorialPreview>(true);
            if (!tutorial) tutorial = FindFirstObjectByType<ElevatorTutorialPreview>(FindObjectsInactive.Include);
            if (tutorial)
            {
                var screen = screenSpace.Instructions ? screenSpace.Instructions.transform : null;
                if (screen && screen.GetComponent<Canvas>().enabled) Place(screen.Find("TopRight_Instruction") as RectTransform, true, true);
            }
            if (reading) reading.Sync();
        }
        void Capture(RectTransform rect, bool sizeChanged = false)
        {
            if (!original.ContainsKey(rect)) original.Add(rect, new State { position = rect.anchoredPosition, size = rect.sizeDelta, scale = rect.localScale, sizeChanged = sizeChanged });
        }
        Rect MirrorCrop(Rect crop, Texture texture, Rect destination)
        {
            // This session owns square Mock HMD eyes. Preserve its crop aperture
            // and fit it to the actual camera output/tile aspect, not the provider's
            // null-target window estimate. No render targets are allocated or bound.
            float sourceAspect = texture ? texture.width / (float)Mathf.Max(1, texture.height) : 1;
            float targetAspect = Mathf.Max(1, camera.pixelWidth) * destination.width /
                Mathf.Max(1, camera.pixelHeight * destination.height);
            float aperture = Mathf.Max(crop.width, crop.height);
            var size = new Vector2(aperture * Mathf.Min(1, targetAspect / sourceAspect),
                aperture * Mathf.Min(1, sourceAspect / targetAspect));
            return new Rect(crop.center - size * .5f, size);
        }
        void Place(RectTransform panel, bool right, bool top)
        {
            if (!panel || !panel.gameObject.activeInHierarchy || !(panel.parent is RectTransform parent) || !TryRects(parent, out var l, out var r)) return;
            float minX = Mathf.Max(l.xMin, r.xMin), maxX = Mathf.Min(l.xMax, r.xMax);
            float minY = Mathf.Max(l.yMin, r.yMin), maxY = Mathf.Min(l.yMax, r.yMax);
            float width = maxX - minX, height = maxY - minY;
            if (width <= 0 || height <= 0) return;
            Capture(panel);
            float scale = Mathf.Min(1, Mathf.Min(width / Mathf.Max(1, parent.rect.width), height / Mathf.Max(1, parent.rect.height)));
            panel.localScale = original[panel].scale * scale;
            // Keep readable UI away from the mirror edge, including the provider's
            // small ROI differences between update/render contexts and the two eyes.
            float paddingX = Mathf.Max(24 * scale, width * .06f), paddingY = Mathf.Max(24 * scale, height * .04f);
            Vector2 pivot = new Vector2(right ? maxX - paddingX : (minX + maxX) * .5f, top ? maxY - paddingY : minY + paddingY);
            Vector2 anchor = parent.rect.min + Vector2.Scale(parent.rect.size, (panel.anchorMin + panel.anchorMax) * .5f);
            Vector2 size = Vector2.Scale(panel.rect.size, new Vector2(panel.localScale.x, panel.localScale.y));
            if (!right) pivot.x += (panel.pivot.x - .5f) * size.x;
            if (top) pivot.y -= (1 - panel.pivot.y) * size.y; else pivot.y += panel.pivot.y * size.y;
            panel.anchoredPosition = pivot - anchor;
        }
        public bool IsWorldPointVisible(Vector3 worldPoint)
        {
            if (!camera || !camera.stereoEnabled) return false;
            var point = new Vector4(worldPoint.x, worldPoint.y, worldPoint.z, 1);
            var native = new Rect(0, 0, 1, 1);
            for (int i = 0; i < 2; i++)
            {
                var eye = i == 0 ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
                var eyePoint = camera.GetStereoViewMatrix(eye) * point;
                var clip = camera.GetStereoProjectionMatrix(eye) * eyePoint;
                float depth = -eyePoint.z;
                if (!(depth >= camera.nearClipPlane && depth <= camera.farClipPlane && clip.w > 0) || float.IsInfinity(clip.w)) return false;
                var viewport = new Vector2(clip.x / clip.w * .5f + .5f, clip.y / clip.w * .5f + .5f);
                if (!native.Contains(viewport) || !crops[i].Contains(viewport)) return false;
            }
            return true;
        }
        public bool TryVisibleArea(RectTransform plane, out Rect area)
        {
            area = default;
            if (!TryRects(plane, out var left, out var right)) return false;
            area = Rect.MinMaxRect(Mathf.Max(left.xMin, right.xMin), Mathf.Max(left.yMin, right.yMin),
                Mathf.Min(left.xMax, right.xMax), Mathf.Min(left.yMax, right.yMax));
            return area.width > 0 && area.height > 0;
        }
        bool TryRects(RectTransform plane, out Rect left, out Rect right)
        {
            left = EyeRect(plane, Camera.StereoscopicEye.Left, crops[0]);
            right = EyeRect(plane, Camera.StereoscopicEye.Right, crops[1]);
            return left.width > 0 && right.width > 0 && left.height > 0 && right.height > 0;
        }
        Rect EyeRect(RectTransform plane, Camera.StereoscopicEye eye, Rect crop)
        {
            Matrix4x4 inverse = (camera.GetStereoProjectionMatrix(eye) * camera.GetStereoViewMatrix(eye)).inverse;
            Matrix4x4 viewInverse = camera.GetStereoViewMatrix(eye).inverse;
            Vector3 origin = viewInverse.MultiplyPoint3x4(Vector3.zero);
            Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? crop.xMin : crop.xMax, y = (i & 2) == 0 ? crop.yMin : crop.yMax;
                Vector4 point = inverse * new Vector4(x * 2 - 1, y * 2 - 1, 1, 1);
                Vector3 direction = new Vector3(point.x, point.y, point.z) / point.w - origin;
                float denominator = Vector3.Dot(direction, plane.forward);
                if (Mathf.Abs(denominator) < .00001f) return default;
                float t = Vector3.Dot(plane.position - origin, plane.forward) / denominator;
                if (t <= 0) return default;
                Vector3 local = plane.InverseTransformPoint(origin + direction * t);
                min = Vector2.Min(min, local); max = Vector2.Max(max, local);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        void OnDisable()
        {
            Application.onBeforeRender -= Fit;
            if (reading) reading.enabled = false;
            foreach (var pair in original) if (pair.Key) { pair.Key.anchoredPosition = pair.Value.position; if (pair.Value.sizeChanged) pair.Key.sizeDelta = pair.Value.size; pair.Key.localScale = pair.Value.scale; }
            original.Clear();
            foreach (var pair in originalFonts) if (pair.Key) pair.Key.fontSize = pair.Value;
            originalFonts.Clear();
            foreach (var bar in barStates) if (bar.Key) bar.Key.enabled = bar.Value;
            barStates.Clear();
            if (compactCaption) DestroyImmediate(compactCaption.gameObject);
            if (screenSpace) screenSpace.enabled = false;
        }
    }
}
#endif
