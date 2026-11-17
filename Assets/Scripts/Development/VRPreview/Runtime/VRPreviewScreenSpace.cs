#if UNITY_EDITOR
using System.Collections.Generic;
using ProjectWait.M.Elevator.DialoguePreview;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.VRPreview
{
    // Render the existing, live presentation in camera-space Canvases. The story's
    // empty source roots may still follow the camera, but never drive rendered UI.
    public sealed class VRPreviewScreenSpace : MonoBehaviour
    {
        public Canvas Cinematic { get; private set; }
        public Canvas Hud { get; private set; }
        public Canvas Instructions { get; private set; }
        ElevatorLesson lesson;
        Camera camera;
        readonly List<Binding> bindings = new List<Binding>();
        readonly List<Moved> moved = new List<Moved>();
        struct Binding { public Canvas source, screen; public CanvasGroup sourceGroup, screenGroup; }
        struct Moved { public RectTransform rect; public Transform parent; public int sibling; }

        public void Initialize(ElevatorLesson value, Camera view) { lesson = value; camera = view; }

        public void Sync()
        {
            if (!lesson || !camera) return;
            var overlay = lesson.presentation.space.overlay;
            if (!Cinematic && overlay) Cinematic = Bind(overlay.GetComponent<Canvas>(), "Cinematic");
            if (!Hud && lesson.view.spatial) Hud = Bind(lesson.view.spatial.hud, "HUD");
            var tutorial = lesson.GetComponentInChildren<ElevatorTutorialPreview>(true);
            if (!tutorial) tutorial = FindFirstObjectByType<ElevatorTutorialPreview>(FindObjectsInactive.Include);
            if (!Instructions && tutorial && tutorial.InstructionCanvas) Instructions = Bind(tutorial.InstructionCanvas, "Instructions");
            foreach (var binding in bindings)
            {
                binding.screen.enabled = binding.source && binding.source.enabled && binding.source.gameObject.activeInHierarchy;
                if (binding.sourceGroup) binding.screenGroup.alpha = binding.sourceGroup.alpha;
            }
            // Let Unity place camera-space roots against the current tracked camera
            // before framing. No transform follow, interpolation or world-pose copy.
            Canvas.ForceUpdateCanvases();
        }

        Canvas Bind(Canvas source, string suffix)
        {
            if (!source) return null;
            var go = new GameObject("VR Preview Screen " + suffix, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var screen = go.GetComponent<Canvas>();
            screen.renderMode = RenderMode.ScreenSpaceCamera;
            screen.worldCamera = camera;
            screen.planeDistance = Mathf.Max(.5f, camera.nearClipPlane + .1f);
            screen.sortingOrder = source.sortingOrder;
            screen.additionalShaderChannels = source.additionalShaderChannels;
            screen.targetDisplay = source.targetDisplay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            var group = go.GetComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            bindings.Add(new Binding { source = source, screen = screen, sourceGroup = source.GetComponent<CanvasGroup>(), screenGroup = group });
            var children = new List<RectTransform>();
            foreach (Transform child in source.transform) if (child is RectTransform rect) children.Add(rect);
            // Snapshot all indices before removing the first child; removals shift
            // the remaining siblings and would otherwise reverse the draw order.
            foreach (var rect in children)
                moved.Add(new Moved { rect = rect, parent = source.transform, sibling = rect.GetSiblingIndex() });
            foreach (var rect in children) Reparent(rect, screen.transform);
            return screen;
        }

        static void Reparent(RectTransform rect, Transform parent)
        {
            var position = rect.anchoredPosition3D;
            var scale = rect.localScale;
            var rotation = rect.localRotation;
            var size = rect.sizeDelta;
            rect.SetParent(parent, false);
            rect.anchoredPosition3D = position;
            rect.localScale = scale;
            rect.localRotation = rotation;
            rect.sizeDelta = size;
        }

        void OnDisable()
        {
            // Reattach live controls before the session destroys its temporary roots.
            // Preserve current dynamic content sizes rather than restoring stale ones.
            foreach (var item in moved) if (item.rect && item.parent)
            {
                Reparent(item.rect, item.parent);
                item.rect.SetSiblingIndex(item.sibling);
            }
            moved.Clear();
            foreach (var binding in bindings) if (binding.screen) DestroyImmediate(binding.screen.gameObject);
            bindings.Clear();
            Cinematic = Hud = Instructions = null;
        }
    }
}
#endif
