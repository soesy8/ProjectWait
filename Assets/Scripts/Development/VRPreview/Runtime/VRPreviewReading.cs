#if UNITY_EDITOR
using System.Collections.Generic;
using ProjectWait.M.Elevator.DialoguePreview;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProjectWait.M.VRPreview
{
    // Replaces screen-fixed reading with event-placed world anchors.
    // Content owners, original world raycasters and live callbacks are retained.
    [DefaultExecutionOrder(-300)]
    public sealed class VRPreviewReading : MonoBehaviour, IXRSelectFilter
    {
        public Canvas ReadingCanvas { get; private set; }
        public RectTransform Column { get; private set; }
        public Button RecenterButton { get; private set; }
        public int RecenterCount { get; private set; }
        public float AppliedScale => 1;
        public bool Expanded { get; private set; }
        public bool PlacementValid { get; private set; }
        public string PlacementStatus { get; private set; } = "Waiting for dialogue";
        public Vector3 AnchorPosition => placement.position;
        public Quaternion AnchorRotation => placement.rotation;
        public Vector2 WorldSize { get; private set; }
        public Button CollapseButton { get; private set; }
        public Button AcknowledgeButton { get; private set; }
        public VRPreviewStagePolicy.Mode Mode { get; private set; }
        public bool NeedsReading => Mode == VRPreviewStagePolicy.Mode.RequiredReading || Mode == VRPreviewStagePolicy.Mode.Description;
        public bool Presented => contentKey != null && presentedKey == contentKey && contentKey == ContentKey();
        public bool InReadingView => InView();
        public bool canProcess => isActiveAndEnabled;
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable target) => CurrentMode() != VRPreviewStagePolicy.Mode.RequiredReading;
        Canvas outgoingCanvas;
        CanvasGroup outgoingGroup;
        RectTransform acknowledgeRect;
        NearFarInteractor[] interactors;
        VRPreviewWorldPlacement.Placement placement;
        Vector3 levelForward = Vector3.forward, attemptEye, attemptForward, stageForward;
        bool needsPlacement, manualRequest, stageReady, recoveryTried;
        int stageRun;
        ElevatorStep stageStep;
        int placementRun = -1;
        ElevatorStep placementStep;
        string contentKey, presentedKey, fullyPresentedKey, acknowledgedIntro;
        // The authored reader's full source is private; dialogueText changes on every page.
        static readonly System.Reflection.FieldInfo dialogueSourceField = typeof(ElevatorSpatialUI).GetField("dialogueSource",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        VRPreviewStageUX stageUX;
        readonly Dictionary<Button, bool> choiceStates = new Dictionary<Button, bool>();
        readonly List<XRSimpleInteractable> filtered = new List<XRSimpleInteractable>();
        struct WorldPose { public Vector3 center, scale; public Quaternion rotation; public Vector2 size; }
        VRPreviewTest owner;
        VRPreviewFraming framing;
        ElevatorLesson lesson;
        ElevatorView view;
        Camera camera;
        CanvasGroup group, sourceGroup;
        RectTransform reader;
        Local readerOrigin;
        ElevatorUIFocusWindow focus;
        float focusOpacity;
        Material controlInk;
        readonly List<Origin> origins = new List<Origin>();
        readonly Dictionary<TMP_Text, float> fonts = new Dictionary<TMP_Text, float>();
        readonly Dictionary<RectTransform, Local> stable = new Dictionary<RectTransform, Local>();
        readonly Dictionary<RectTransform, WorldPose> retired = new Dictionary<RectTransform, WorldPose>();
        readonly List<RectTransform> scratch = new List<RectTransform>();
        readonly Vector3[] corners = new Vector3[4];
        struct Origin { public RectTransform rect; public Transform parent; public int sibling; public Local local; }
        struct Local
        {
            public Vector2 min, max, pivot, size;
            public Vector3 position, scale;
            public Quaternion rotation;
            public static Local Read(RectTransform r) => new Local { min = r.anchorMin, max = r.anchorMax, pivot = r.pivot,
                size = r.sizeDelta, position = r.anchoredPosition3D, scale = r.localScale, rotation = r.localRotation };
            public void Apply(RectTransform r, bool restoreSize = true)
            {
                r.anchorMin = min; r.anchorMax = max; r.pivot = pivot;
                if (restoreSize) r.sizeDelta = size;
                r.anchoredPosition3D = position; r.localScale = scale; r.localRotation = rotation;
            }
        }

        public void Initialize(VRPreviewTest test, VRPreviewFraming frame, ElevatorLesson value, Camera target)
        {
            owner = test; framing = frame; lesson = value; view = value ? value.view : null; camera = target;
            interactors = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            levelForward = VRPreviewWorldPlacement.LevelForward(camera.transform.forward, camera.transform.parent.forward);
        }

        void OnEnable() { RenderPipelineManager.endCameraRendering += AfterCamera; }
        void Update()
        {
            if (!owner || !owner.Running || !view) return;
            RestoreFonts();
            if (ReadingCanvas) { Reconcile(); Canvas.ForceUpdateCanvases(); }
        }
        string IntroKey() => lesson.RunVersion + "|" + lesson.Flow.Step + "|" + lesson.Flow.DestinationSelected + "|" + lesson.Flow.BallReceived +
            "|" + view.spatial.speaker.text + "|" + dialogueSourceField.GetValue(view.spatial);
        VRPreviewStagePolicy.Mode CurrentMode()
        {
            if (!lesson || lesson.Flow == null) return VRPreviewStagePolicy.Mode.Cinematic;
            var mode = VRPreviewStagePolicy.Evaluate(lesson);
            return VRPreviewStagePolicy.NeedsAcknowledgment(lesson) && acknowledgedIntro == IntroKey() ? VRPreviewStagePolicy.Mode.Observe : mode;
        }
        string ContentKey()
        {
            var flow = lesson.Flow;
            var key = lesson.RunVersion + "|" + flow.Step + "|" + flow.Phase + "|" + flow.PreviewClue + "|" +
                flow.DestinationSelected + "|" + flow.BallReceived + "|" + view.spatial.DialoguePage + "|" + view.spatial.DialoguePageCount + "|" +
                view.spatial.speaker.text + "|" + view.dialogueText.text + "|" + dialogueSourceField.GetValue(view.spatial);
            foreach (var button in view.ChoiceButtons) if (button) key += "|" + button.GetComponentInChildren<TMP_Text>(true).text;
            return key;
        }
        void AfterCamera(ScriptableRenderContext context, Camera rendered)
        {
            if (rendered != camera || !isActiveAndEnabled || !NeedsReading || !Expanded || !PlacementValid ||
                lesson.IsPaused || lesson.CinematicBlocking || !group || group.alpha < .99f || contentKey == null || contentKey != ContentKey() || !InView()) return;
            var bodyAlpha = view.dialogueText.GetComponent<CanvasGroup>();
            var cardAlpha = view.dialoguePanel.GetComponent<CanvasGroup>();
            if (bodyAlpha && bodyAlpha.alpha < .05f || cardAlpha && cardAlpha.alpha < .05f) return;
            foreach (var button in view.ChoiceButtons) if (button)
            {
                var alpha = button.GetComponent<CanvasGroup>();
                if (alpha && alpha.alpha < .05f) return;
            }
            presentedKey = contentKey;
            fullyPresentedKey = view.spatial.DialogueFullyRevealed ? contentKey : null;
            if (view.spatial.DialogueFullyRevealed) stageUX.RememberPresented(contentKey, view.spatial.speaker.text, view.dialogueText.text);
        }
        void GateChoices()
        {
            foreach (var button in view.ChoiceButtons) if (button)
            {
                if (!choiceStates.ContainsKey(button)) choiceStates.Add(button, button.interactable);
                button.interactable = choiceStates[button] && (!NeedsReading || Presented);
            }
            if (AcknowledgeButton) AcknowledgeButton.interactable = Presented;
        }
        public void Acknowledge()
        {
            if (!Presented || !VRPreviewStagePolicy.NeedsAcknowledgment(lesson)) return;
            if (view.spatial.TryReadDialogue()) { presentedKey = fullyPresentedKey = null; return; }
            if (fullyPresentedKey != contentKey) return;
            acknowledgedIntro = IntroKey();
        }

        void Build()
        {
            ReadingCanvas = view.GetComponent<Canvas>(); sourceGroup = view.GetComponent<CanvasGroup>();
            Column = MakeRect("VR Preview World Reading Column", view.transform); Column.sizeDelta = new Vector2(660, 620);
            group = Column.gameObject.AddComponent<CanvasGroup>();
            Remember(view.dialoguePanel); Remember(view.actionPanel); Reconcile();
            outgoingCanvas = WorldCanvas("VR Preview World Outgoing", false); outgoingGroup = outgoingCanvas.GetComponent<CanvasGroup>();
            outgoingCanvas.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); WorldScale(outgoingCanvas.transform, 1);
            stageUX = new VRPreviewStageUX(); stageUX.Initialize(this, lesson, framing, transform);
            AcknowledgeButton = Control("ReadingAcknowledgment", Column, new Vector2(230, 54), "확인했어요", Acknowledge);
            acknowledgeRect = (RectTransform)AcknowledgeButton.transform;
            focus = view.spatial.focusWindow;
            if (focus) { focusOpacity = focus.centreOpacity; focus.centreOpacity = 1; }
            foreach (var observation in lesson.CollectObservations()) if (observation)
            {
                var target = observation.GetComponent<XRSimpleInteractable>();
                if (target) { target.selectFilters.Add(this); filtered.Add(target); }
            }
        }
        Canvas WorldCanvas(string name, bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup)); go.transform.SetParent(transform, false); go.layer = view.gameObject.layer;
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            canvas.sortingOrder = ReadingCanvas.sortingOrder; canvas.additionalShaderChannels = ReadingCanvas.additionalShaderChannels;
            var alpha = go.GetComponent<CanvasGroup>(); alpha.interactable = alpha.blocksRaycasts = interactive;
            if (interactive)
            {
                go.AddComponent<GraphicRaycaster>(); var ray = go.AddComponent<TrackedDeviceGraphicRaycaster>(); var source = view.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (source) { ray.ignoreReversedGraphics = source.ignoreReversedGraphics; ray.checkFor2DOcclusion = source.checkFor2DOcclusion; ray.checkFor3DOcclusion = source.checkFor3DOcclusion; ray.blockingMask = source.blockingMask; }
            }
            return canvas;
        }
        static void WorldScale(Transform target, float value)
        {
            var parent = target.parent ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(value / parent.x, value / parent.y, value / parent.z);
        }

        void Remember(RectTransform rect)
        {
            foreach (var item in origins) if (item.rect == rect) return;
            origins.Add(new Origin { rect = rect, parent = rect.parent, sibling = rect.GetSiblingIndex(), local = Local.Read(rect) });
        }
        static void Move(RectTransform rect, Transform parent)
        {
            if (rect.parent == parent) return;
            var local = Local.Read(rect); rect.SetParent(parent, false); local.Apply(rect);
        }
        void Reconcile()
        {
            if (!Column || !view.dialoguePanel || !view.actionPanel) return;
            Remember(view.dialoguePanel); Remember(view.actionPanel);
            if (lesson.Flow.Step == ElevatorStep.Complete)
            {
                // The authored completion book keeps its pages/history and buttons.
                var source = view.transform;
                if (view.dialoguePanel.parent == Column) Move(view.dialoguePanel, source);
                if (view.actionPanel.parent == Column) Move(view.actionPanel, view.spatial.rightPage);
                return;
            }
            Move(view.dialoguePanel, Column); Move(view.actionPanel, Column);
        }

        public void Sync()
        {
            if (!isActiveAndEnabled || !owner || !owner.Running || !view || lesson.Flow == null) return;
            if (!Column) Build();
            Reconcile(); Mode = CurrentMode();
            bool allowed = ReadingCanvas.enabled && sourceGroup.alpha > .01f && !lesson.IsPaused && !lesson.CinematicBlocking;
            if (focus) focus.centreOpacity = 1;
            levelForward = VRPreviewWorldPlacement.LevelForward(camera.transform.forward, levelForward);
            bool stageChanged = !stageReady || stageRun != lesson.RunVersion || stageStep != lesson.Flow.Step;
            if (stageChanged) { stageReady = true; stageRun = lesson.RunVersion; stageStep = lesson.Flow.Step; stageForward = levelForward; }
            bool wantsAcknowledgment = NeedsReading && VRPreviewStagePolicy.NeedsAcknowledgment(lesson);
            acknowledgeRect.gameObject.SetActive(wantsAcknowledgment);
            Expanded = NeedsReading;
            if (NeedsReading)
            {
                bool hovering = Hovering();
                PreparePanel(view.dialoguePanel); PreparePanel(view.actionPanel); PreparePanel(view.spatial.speakerTab);
                SetFont(view.dialogueText, owner.dialogueFontSize);
                foreach (var text in view.dialoguePanel.GetComponentsInChildren<TMP_Text>(true))
                    if (text.name == "TransitionText" && text.transform.parent == view.dialogueText.transform.parent) SetFont(text, owner.dialogueFontSize);
                foreach (var button in view.ChoiceButtons) if (button) SetFont(button.GetComponentInChildren<TMP_Text>(true), owner.choiceFontSize);
                Reflow(520);
                if (wantsAcknowledgment) acknowledgeRect.anchoredPosition = new Vector2(0, -view.dialoguePanel.rect.height * .5f - 42);
                foreach (var panel in new[] { view.dialoguePanel, view.actionPanel, view.spatial.speakerTab })
                    if (view.spatial.Transitions.IsVisible(panel)) stable[panel] = Local.Read(panel);
                var bounds = Bounds();
                var offset = hovering ? bounds.center + Column.anchoredPosition : Vector2.zero;
                var size = (bounds.size + new Vector2(24, 36) + new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) * 2) * owner.worldReadingScale;
                bool footprintChanged = (size - WorldSize).sqrMagnitude > .000001f;
                WorldSize = size;
                if (!hovering) Column.anchoredPosition = -bounds.center;
                string key = ContentKey();
                bool changed = contentKey != key;
                bool differentStage = placementRun != lesson.RunVersion || placementStep != lesson.Flow.Step;
                if (PlacementValid) { view.transform.SetPositionAndRotation(placement.position, placement.rotation); WorldScale(view.transform, owner.worldReadingScale); }
                if (changed)
                {
                    contentKey = key; presentedKey = fullyPresentedKey = null; recoveryTried = false;
                    if (differentStage || !PlacementValid) needsPlacement = true;
                    else if (!InView()) { needsPlacement = true; stageForward = levelForward; }
                }
                if (differentStage) PlacementValid = false;
                if (footprintChanged && PlacementValid)
                    PlacementValid = VRPreviewWorldPlacement.Validate(lesson, placement, WorldSize);
                if (!PlacementValid && (changed || footprintChanged || stageChanged)) needsPlacement = true;
                if (allowed && !PlacementValid && !Presented && !hovering && !recoveryTried && !needsPlacement &&
                    (Vector3.Distance(camera.transform.position, attemptEye) > .5f || Vector3.Angle(levelForward, attemptForward) > 60))
                { needsPlacement = true; stageForward = levelForward; recoveryTried = true; }
                if (allowed && (needsPlacement || manualRequest) && !hovering)
                {
                    PlacementValid = VRPreviewWorldPlacement.TryPlace(lesson, WorldSize, stageForward, owner.worldReadingDistance, true, out var next);
                    if (PlacementValid) { placement = next; placementRun = lesson.RunVersion; placementStep = lesson.Flow.Step; PlacementStatus = next.anchor; }
                    else PlacementStatus = "안전한 읽기 위치 확인 중";
                    attemptEye = camera.transform.position; attemptForward = levelForward;
                    needsPlacement = manualRequest = false;
                }
                if (PlacementValid) { view.transform.SetPositionAndRotation(placement.position, placement.rotation); WorldScale(view.transform, owner.worldReadingScale); }
                if (Expanded) CacheChoiceGeometry();
            }
            group.alpha = allowed && NeedsReading && PlacementValid ? 1 : 0;
            group.interactable = group.blocksRaycasts = group.alpha > .01f && sourceGroup.interactable;
            outgoingGroup.alpha = allowed && NeedsReading ? 1 : 0;
            BridgeRetired(); GateChoices();
            stageUX.Sync(Mode, Presented);
        }
        bool Hovering()
        {
            foreach (var hand in interactors) if (hand && hand.isActiveAndEnabled)
            {
                if (hand.uiPressInput.ReadIsPerformed()) return true;
                if (hand.TryGetCurrentUIRaycastResult(out var hit) && hit.gameObject && hit.gameObject.transform.IsChildOf(Column)) return true;
            }
            return false;
        }
        bool InView()
        {
            if (!PlacementValid || !ReadingCanvas || !framing) return false;
            var panels = new List<RectTransform> { view.dialoguePanel, view.spatial.speakerTab };
            foreach (var button in view.ChoiceButtons) if (button) panels.Add((RectTransform)button.transform);
            if (AcknowledgeButton && AcknowledgeButton.gameObject.activeSelf) panels.Add(acknowledgeRect);
            foreach (var panel in panels)
            {
                if (!panel.gameObject.activeInHierarchy) continue;
                panel.GetWorldCorners(corners);
                foreach (var corner in corners) if (!framing.IsWorldPointVisible(corner)) return false;
            }
            return true;
        }
        public void Collapse()
        {
            // Kept for old preview callers. Only the story phase removes reading.
            if (!NeedsReading && group) { Expanded = false; group.alpha = 0; group.interactable = group.blocksRaycasts = false; }
        }




        void PreparePanel(RectTransform panel)
        {
            if (!view.spatial.Transitions.IsVisible(panel) && stable.TryGetValue(panel, out var previous)) previous.Apply(panel);
            else
            {
                panel.localScale = Vector3.one; panel.localRotation = Quaternion.identity;
                stable[panel] = Local.Read(panel);
            }
        }
        void Reflow(float width)
        {
            // Grow live content at the display width. Outgoing content retains
            // its last displayed geometry until the original fade is finished.
            var card = view.dialoguePanel;
            if (view.spatial.Transitions.IsVisible(card))
            {
            float bodyWidth = Mathf.Max(200, width - 48);
            float bodyHeight = Mathf.Max(70, view.dialogueText.GetPreferredValues(view.dialogueText.text, bodyWidth, 0).y + 6);
            float height = bodyHeight + 46;
            card.sizeDelta = new Vector2(width, height); card.anchoredPosition = Vector2.zero;
            view.dialogueText.rectTransform.sizeDelta = new Vector2(bodyWidth, bodyHeight);
            view.dialogueText.rectTransform.anchoredPosition = Vector2.zero;
            var paper = card.Find("ReadingPaper") as RectTransform;
            if (paper) paper.sizeDelta = view.dialogueText.rectTransform.sizeDelta + new Vector2(24, 14);
            var tab = view.spatial.speakerTab;
            tab.localScale = Vector3.one; tab.localRotation = Quaternion.identity;
            tab.anchoredPosition = new Vector2(-width * .5f + (view.spatial.speaker.text == "친구" ? 88 : 30) + tab.rect.width * .5f, height * .5f - 1);
            var portrait = card.Find("FriendPortrait") as RectTransform;
            if (portrait) portrait.anchoredPosition = new Vector2(-width * .5f + 47, height * .5f - 9);
            var tail = card.Find("SpeechTail") as RectTransform;
            if (tail) tail.anchoredPosition = new Vector2(-width * .21f, -height * .5f - 12);
            var hit = card.Find("DialogueReader") as RectTransform;
            if (hit)
            {
                if (!reader) { reader = hit; readerOrigin = Local.Read(hit); }
                // A full-card hit surface would steal controller presses aimed
                // at world objects behind the reading card.
                hit.anchorMin = hit.anchorMax = hit.pivot = Vector2.one * .5f;
                hit.sizeDelta = new Vector2(Mathf.Max(180, width - 48), 40);
                hit.anchoredPosition = new Vector2(0, -height * .5f + 20);
            }
            var status = card.Find("ReadingStatus") as RectTransform;
            if (status) { status.sizeDelta = new Vector2(Mathf.Max(180, width - 48), 26); status.anchoredPosition = new Vector2(0, -height * .5f + 20); }
            }
            if (!view.spatial.Transitions.IsVisible(view.actionPanel)) return;
            float y = 0;
            foreach (var button in view.ChoiceButtons) if (button)
            {
                var rect = (RectTransform)button.transform; var label = button.GetComponentInChildren<TMP_Text>(true);
                float buttonWidth = button.name == "DialogueNext" ? Mathf.Min(190, width) : width;
                float textWidth = buttonWidth - (buttonWidth < 300 ? 38 : 64);
                float buttonHeight = Mathf.Max(52, label.GetPreferredValues(label.text, textWidth, 0).y + 18);
                rect.sizeDelta = new Vector2(buttonWidth, buttonHeight); rect.anchoredPosition = new Vector2(0, -y - buttonHeight * .5f);
                label.rectTransform.sizeDelta = new Vector2(textWidth, buttonHeight - 14);
                y += buttonHeight + 8;
            }
            view.actionPanel.sizeDelta = new Vector2(width, Mathf.Max(0, y - 8));
            view.actionPanel.anchoredPosition = new Vector2(0, -card.rect.height * .5f - 10);
        }
        Rect Bounds()
        {
            Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
            bool any = false;
            var panels = new List<RectTransform> { view.dialoguePanel, view.spatial.speakerTab };
            foreach (var button in view.ChoiceButtons) if (button) panels.Add((RectTransform)button.transform);
            if (AcknowledgeButton && AcknowledgeButton.gameObject.activeSelf) panels.Add(acknowledgeRect);
            foreach (var panel in panels)
            {
                if (!panel || !panel.gameObject.activeInHierarchy || !panel.IsChildOf(Column)) continue;
                panel.GetWorldCorners(corners);
                foreach (var p in corners) { var local = (Vector2)Column.InverseTransformPoint(p); min = Vector2.Min(min, local); max = Vector2.Max(max, local); }
                any = true;
            }
            return any ? Rect.MinMaxRect(min.x - 14, min.y - 18, max.x + 14, max.y + 10) : new Rect(-290, -100, 580, 200);
        }
        void SetFont(TMP_Text text, float size)
        {
            if (!text) return;
            if (!fonts.ContainsKey(text)) fonts.Add(text, text.fontSize);
            text.fontSize = size;
        }
        void RestoreFonts() { foreach (var pair in fonts) if (pair.Key) pair.Key.fontSize = pair.Value; }

        void CacheChoiceGeometry()
        {
            foreach (var button in view.ChoiceButtons) if (button)
            {
                var rect = (RectTransform)button.transform;
                retired[rect] = new WorldPose { center = rect.TransformPoint(rect.rect.center), scale = rect.lossyScale, rotation = rect.rotation, size = rect.sizeDelta };
            }
        }
        void BridgeRetired()
        {
            scratch.Clear();
            foreach (Transform parent in new[] { view.transform, Column, outgoingCanvas.transform })
                foreach (Transform child in parent) if (child is RectTransform rect && child.name.StartsWith("Retiring_")) scratch.Add(rect);
            foreach (var rect in scratch)
            {
                if (!retired.TryGetValue(rect, out var pose)) { rect.gameObject.SetActive(false); continue; }
                rect.SetParent(outgoingCanvas.transform, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.sizeDelta = pose.size; rect.SetPositionAndRotation(pose.center, pose.rotation); rect.localScale = pose.scale;
                SetFont(rect.GetComponentInChildren<TMP_Text>(true), owner.choiceFontSize);
            }
        }

        Button Control(string name, Transform parent, Vector2 size, string text, UnityEngine.Events.UnityAction callback)
        {
            var rect = MakeRect(name, parent); rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.79f, .89f, .76f);
            if (view.spatial.storybook) image.material = view.spatial.storybook.worldMaterial;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(callback);
            var labelRect = MakeRect("Label", rect); labelRect.sizeDelta = size - new Vector2(12, 4);
            stageUX.PrepareText(text);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = stageUX.LabelFont;
            controlInk = new Material(stageUX.LabelFont.material) { name = "VR preview reading-confirm ink", hideFlags = HideFlags.DontSave };
            if (controlInk.HasProperty("_OutlineWidth")) controlInk.SetFloat("_OutlineWidth", 0);
            label.fontSharedMaterial = controlInk;
            label.text = text; label.fontSize = 21; label.color = new Color(.28f, .19f, .14f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return button;
        }
        public void Recenter()
        {
            if (!isActiveAndEnabled || !lesson || lesson.Flow == null || lesson.IsPaused || lesson.CinematicBlocking) return;
            if (lesson.Flow.Step == ElevatorStep.Complete) return;
            RecenterCount++;
            manualRequest = true;
        }
        static RectTransform MakeRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; return rect;
        }

        void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= AfterCamera;
            if (AcknowledgeButton) DestroyImmediate(AcknowledgeButton.gameObject);
            stageUX?.Dispose(); stageUX = null;
            foreach (var target in filtered) if (target) target.selectFilters.Remove(this); filtered.Clear();
            foreach (var button in choiceStates) if (button.Key) button.Key.interactable = button.Value; choiceStates.Clear();
            foreach (var pair in fonts) if (pair.Key) pair.Key.fontSize = pair.Value; fonts.Clear();
            if (focus) focus.centreOpacity = focusOpacity;
            if (view && view.spatial) view.spatial.Transitions.Advance(1);
            foreach (var origin in origins) if (origin.rect)
            {
                Transform parent = origin.rect == view.actionPanel ? (lesson.Flow != null && lesson.Flow.Step == ElevatorStep.Complete ? view.spatial.rightPage : view.transform) : origin.parent;
                if (parent) { origin.rect.SetParent(parent, false); origin.local.Apply(origin.rect, false); origin.rect.SetSiblingIndex(origin.sibling); }
            }
            if (reader) readerOrigin.Apply(reader); reader = null;
            if (outgoingCanvas && view) foreach (var rect in outgoingCanvas.GetComponentsInChildren<RectTransform>(true)) if (rect.name.StartsWith("Retiring_")) rect.SetParent(view.transform, true);
            if (Column) DestroyImmediate(Column.gameObject);
            if (controlInk) DestroyImmediate(controlInk);
            controlInk = null;
            if (outgoingCanvas) DestroyImmediate(outgoingCanvas.gameObject);
            origins.Clear(); stable.Clear(); retired.Clear();
            Column = null; ReadingCanvas = null; Expanded = PlacementValid = false;
            needsPlacement = manualRequest = stageReady = recoveryTried = false;
            contentKey = presentedKey = fullyPresentedKey = acknowledgedIntro = null;
            placement = default; WorldSize = Vector2.zero;
            placementRun = -1;
            if (view && view.spatial && lesson.Flow != null) view.spatial.Layout();
        }
    }
}
#endif
