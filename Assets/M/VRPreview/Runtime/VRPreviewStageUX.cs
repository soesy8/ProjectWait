#if UNITY_EDITOR
using System.Collections.Generic;
using DwtD.TestScene;
using ProjectWait.M.Elevator;
using ProjectWait.M.Elevator.DialoguePreview;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProjectWait.M.VRPreview
{
    // Session-only presentation. The caller synchronizes after source LateUpdate
    // and reading/framing layout, including onBeforeRender; this owns no clock/input.
    public sealed class VRPreviewStageUX
    {
        public Button ReplayButton { get; private set; }
        public TMP_FontAsset LabelFont { get; private set; }
        readonly HashSet<uint> warmedCharacters = new HashSet<uint>();

        const int MaximumPages = 12;
        static readonly Color Paper = new Color(1, .965f, .86f);
        static readonly Color Ink = new Color(.28f, .19f, .14f);
        static readonly Color Mint = new Color(.79f, .89f, .76f);
        static readonly Color Teal = new Color(.16f, .48f, .44f);
        readonly List<Page> pages = new List<Page>();
        readonly Dictionary<Graphic, float> rendererAlpha = new Dictionary<Graphic, float>();
        readonly Dictionary<RectTransform, RectState> geometry = new Dictionary<RectTransform, RectState>();
        readonly Dictionary<ElevatorOutlineTarget, OutlineState> outlines = new Dictionary<ElevatorOutlineTarget, OutlineState>();
        ElevatorLesson lesson;
        ElevatorView view;
        VRPreviewFraming framing;
        Transform owner;
        ElevatorTutorialPreview tutorial;
        ElevatorObservation[] observations;
        int observationsFrame = -1, runVersion = -1, pageIndex;
        RectTransform guide, menuCard, transcript;
        CanvasGroup guideVisibility;
        TMP_Text guideLabel, transcriptText, speakerLabel, pageLabel;
        readonly List<Material> labelInks = new List<Material>();
        Button previousButton, nextButton;
        ScrollRect scroll;
        CanvasGroup menuGroup;
        GroupState menuGroupState;
        bool ownsMenuGroup, rereadOpen, disposed, transcriptDirty = true;

        sealed class Page { public string key, speaker, text; }
        struct RectState
        {
            public Vector2 size, position;
            public bool sizeChanged, positionChanged;
            public void Restore(RectTransform rect)
            {
                if (sizeChanged) rect.sizeDelta = size;
                if (positionChanged) rect.anchoredPosition = position;
            }
        }
        struct GroupState
        {
            public float alpha;
            public bool interactable, raycasts, ignoreParents;
            public static GroupState Read(CanvasGroup group) => new GroupState
            {
                alpha = group.alpha, interactable = group.interactable,
                raycasts = group.blocksRaycasts, ignoreParents = group.ignoreParentGroups
            };
            public void Restore(CanvasGroup group)
            {
                group.alpha = alpha; group.interactable = interactable;
                group.blocksRaycasts = raycasts; group.ignoreParentGroups = ignoreParents;
            }
        }
        struct OutlineState
        {
            public Color idle, hover;
            public float idleWidth, hoverWidth, idleHalo, hoverHalo;
            public bool visible, hovered, inRange;
            public static OutlineState Read(ElevatorOutlineTarget target) => new OutlineState
            {
                idle = target.idleColor, hover = target.hoverColor,
                idleWidth = target.idleWidth, hoverWidth = target.hoverWidth,
                idleHalo = target.idleHalo, hoverHalo = target.hoverHalo,
                visible = target.Visible, hovered = target.Hovered, inRange = target.InRange
            };
            public void Restore(ElevatorOutlineTarget target)
            {
                target.idleColor = idle; target.hoverColor = hover;
                target.idleWidth = idleWidth; target.hoverWidth = hoverWidth;
                target.idleHalo = idleHalo; target.hoverHalo = hoverHalo;
                target.SetPresentation(visible, hovered, inRange);
            }
        }

        public void Initialize(VRPreviewReading reading, ElevatorLesson lesson, VRPreviewFraming framing, Transform owner)
        {
            Dispose();
            disposed = false;
            this.lesson = lesson; view = lesson ? lesson.view : null;
            this.framing = framing; this.owner = owner ? owner : reading ? reading.transform : null;
            CreateLabelFont();
            runVersion = lesson ? lesson.RunVersion : -1;
        }

        void CreateLabelFont()
        {
            Font source = view.font.sourceFontFile;
            if (!source) foreach (var fallback in view.font.fallbackFontAssetTable)
                if (fallback && fallback.sourceFontFile) { source = fallback.sourceFontFile; break; }
            if (!source) throw new System.InvalidOperationException("미리보기 한글 폰트의 원본 글꼴을 찾을 수 없습니다.");
            LabelFont = TMP_FontAsset.CreateFontAsset(source, 48, 5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
            LabelFont.name = "VR preview Noto runtime"; LabelFont.hideFlags = HideFlags.DontSave;
            LabelFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            var material = LabelFont.material; material.name = "VR preview runtime atlas ink"; material.hideFlags = HideFlags.DontSave;
            material.shader = view.dialogueText.fontSharedMaterial.shader;
            material.renderQueue = view.dialogueText.fontSharedMaterial.renderQueue;
            foreach (var texture in LabelFont.atlasTextures) if (texture) texture.hideFlags = HideFlags.DontSave;
            var seed = new HashSet<uint>();
            for (uint c = 32; c < 127; c++) seed.Add(c);
            foreach (var character in view.font.characterTable) if (character.unicode >= 32) seed.Add(character.unicode);
            foreach (var fallback in view.font.fallbackFontAssetTable) if (fallback)
                foreach (var character in fallback.characterTable) if (character.unicode >= 32) seed.Add(character.unicode);
            var codes = new uint[seed.Count]; seed.CopyTo(codes); LabelFont.TryAddCharacters(codes);
            foreach (var c in seed) warmedCharacters.Add(c);
            PrepareText("대화 다시 보기 아직 화면에 표시된 대화가 없어요. 일시정지 중 이전 기록 다음 기록 메뉴로 돌아가기 확인했어요");
        }

        public void PrepareText(string text)
        {
            if (!LabelFont || string.IsNullOrEmpty(text)) return;
            var pending = new List<uint>();
            foreach (var c in text) if (!char.IsControl(c) && warmedCharacters.Add(c)) pending.Add(c);
            if (pending.Count > 0) LabelFont.TryAddCharacters(pending.ToArray());
        }

        public void Sync(VRPreviewStagePolicy.Mode mode, bool presented)
        {
            if (disposed || !lesson || !view || !view.spatial) return;
            CheckRun();
            PrunePages();
            EnsurePauseUI();
            // The source rewrites its four-row layout in every LateUpdate.
            // Use absolute positions, never additive growth in either render pass.
            LayoutPauseMenu();
            if (rereadOpen && (!lesson.IsPaused || !lesson.pauseMenu || !lesson.pauseMenu.canvas ||
                !lesson.pauseMenu.canvas.gameObject.activeInHierarchy)) CloseReread();
            if (ReplayButton) ReplayButton.interactable = lesson.IsPaused;
            if (rereadOpen)
            {
                HideMenu();
                if (transcriptDirty) RefreshTranscript();
            }

            // Only artwork is suppressed: source activation, text and progress run normally.
            Suppress(view.spatial.mission);
            if (!tutorial) tutorial = lesson.GetComponentInChildren<ElevatorTutorialPreview>(true);
            if (!tutorial) tutorial = Object.FindFirstObjectByType<ElevatorTutorialPreview>(FindObjectsInactive.Include);
            if (tutorial)
            {
                Suppress(tutorial.InstructionPanel);
                Suppress(tutorial.Tip);
            }

            bool observe = mode == VRPreviewStagePolicy.Mode.Observe;
            bool pending = !presented && (mode == VRPreviewStagePolicy.Mode.RequiredReading || mode == VRPreviewStagePolicy.Mode.Description);
            bool allowed = lesson.Flow != null && !lesson.IsPaused && !lesson.CinematicBlocking;
            SyncGuide(allowed && (observe || pending), pending && mode == VRPreviewStagePolicy.Mode.RequiredReading);
            SyncWorldTarget(allowed && observe);
        }

        // The reading owner calls this only for a page actually presented to the player.
        // Deliberately never reads Flow.History, descriptions or future dialogue.
        public void RememberPresented(string key, string speaker, string text)
        {
            if (disposed || string.IsNullOrWhiteSpace(text)) return;
            CheckRun();
            PrunePages();
            speaker = speaker ?? string.Empty;
            key = key ?? string.Empty;
            PrepareText(speaker + text);
            if (pages.Count > 0)
            {
                var last = pages[pages.Count - 1];
                if (last.key == key && last.speaker == speaker && last.text == text) return;
            }
            pages.Add(new Page { key = key, speaker = speaker, text = text });
            if (pages.Count > MaximumPages)
            {
                pages.RemoveAt(0);
                pageIndex = Mathf.Max(0, pageIndex - 1);
            }
            if (!rereadOpen) pageIndex = pages.Count - 1;
            transcriptDirty = true;
        }

        void CheckRun()
        {
            if (!lesson || runVersion == lesson.RunVersion) return;
            CloseReread(); pages.Clear(); pageIndex = 0;
            runVersion = lesson.RunVersion; observationsFrame = -1; transcriptDirty = true;
        }
        void PrunePages()
        {
            int removed = pages.RemoveAll(page => page == null || string.IsNullOrWhiteSpace(page.text));
            if (removed > 0) transcriptDirty = true;
            while (pages.Count > MaximumPages) { pages.RemoveAt(0); transcriptDirty = true; }
            pageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, pages.Count - 1));
        }

        void Suppress(RectTransform source)
        {
            if (!source) return;
            foreach (var graphic in source.GetComponentsInChildren<Graphic>(true))
            {
                if (!rendererAlpha.ContainsKey(graphic)) rendererAlpha.Add(graphic, graphic.canvasRenderer.GetAlpha());
                graphic.canvasRenderer.SetAlpha(0);
            }
        }

        void SyncGuide(bool wanted, bool pending)
        {
            var mission = view.spatial.mission;
            var parent = mission ? mission.parent as RectTransform : null;
            if (!parent || !framing || !framing.TryVisibleArea(parent, out var area))
            { if (guideVisibility) guideVisibility.alpha = 0; return; }
            if (!guide)
            {
                guide = Rect("VR Preview Next Action", parent, new Vector2(500, 76));
                // Stage visibility owns this cue, not the source HUD's cinematic fade.
                // A nested Canvas also avoids the parent's alpha-zero batch culling.
                var guideCanvas = guide.gameObject.AddComponent<Canvas>();
                guideCanvas.additionalShaderChannels = parent.GetComponentInParent<Canvas>().additionalShaderChannels;
                guideVisibility = guide.gameObject.AddComponent<CanvasGroup>();
                guideVisibility.ignoreParentGroups = true;
                guideVisibility.interactable = guideVisibility.blocksRaycasts = false;
                Surface(guide, Paper, false);
                guideLabel = Label("Next Action", guide, new Vector2(460, 60), 24, Ink);
            }
            if (guide.parent != parent) guide.SetParent(parent, false);
            // Keep TMP's fallback submesh lifetime intact across phase changes.
            guideVisibility.alpha = wanted ? 1 : 0;
            if (!wanted) return;
            string instruction = pending ? "안내를 읽고 다음 행동을 골라 주세요." : VRPreviewStagePolicy.Cue(lesson);
            PrepareText(instruction); guideLabel.text = instruction;
            // The two-eye/native/mirror intersection is supplied by the framing owner.
            float scale = Mathf.Min(1, Mathf.Min(area.width / 600, area.height / 720));
            if (scale <= 0) { guideVisibility.alpha = 0; return; }
            float height = Mathf.Max(76, guideLabel.GetPreferredValues(guideLabel.text, 460, 0).y + 26);
            guide.sizeDelta = new Vector2(500, height);
            guideLabel.rectTransform.sizeDelta = new Vector2(460, height - 20);
            guide.localScale = Vector3.one * scale;
            float paddingX = Mathf.Max(24 * scale, area.width * .06f);
            float paddingY = Mathf.Max(24 * scale, area.height * .04f);
            guide.anchoredPosition = new Vector2(area.xMax - paddingX - 250 * scale,
                area.yMin + paddingY + height * scale * .5f) - parent.rect.center;

        }

        void SyncWorldTarget(bool wanted)
        {
            if (observationsFrame != Time.frameCount)
            {
                observations = lesson.CollectObservations(); observationsFrame = Time.frameCount;
            }
            ElevatorObservation selected = null;
            if (wanted && lesson.scenario)
            {
                // Refresh the semantic target without changing the source delay or timer.
                lesson.scenario.AdvanceGuidance(0);
                var clue = lesson.scenario.GuidanceTarget;
                if (clue.HasValue)
                {
                    // The two floor panels are equivalent: emphasize the closest one.
                    // Prefer actual identity/ball geometry over a secondary hint/friend.
                    // Never use IsGuidanceTarget: it broadens to many unrelated props.
                    selected = Closest(clue.Value, clue.Value == ElevatorClue.FloorButtons);
                    if (!selected) selected = Closest(clue.Value, true);
                }
            }
            var target = selected ? selected.outlineTarget : null;
            // A held/reparented prop can leave CollectObservations; do not leave its
            // previous automatic emphasis behind when the next target changes.
            foreach (var saved in outlines) if (saved.Key) saved.Key.SetPresentation(false, false);
            if (observations == null) return;
            foreach (var observation in observations)
            {
                if (!observation || !observation.outlineTarget) continue;
                var outline = observation.outlineTarget;
                if (!outlines.ContainsKey(outline)) outlines.Add(outline, OutlineState.Read(outline));
                // No persistent orange ring on every selectable prop, nor a second
                // automatic hint from the original 40-second guidance mechanism.
                if (outline != target)
                {
                    bool hovered = observation.IsHovered && observation.CanActivate;
                    outline.SetPresentation(hovered, hovered, false); continue;
                }
                outline.idleColor = outline.hoverColor = new Color(1, .55f, .18f);
                outline.idleWidth = outline.hoverWidth = 4;
                outline.idleHalo = outline.hoverHalo = .05f;
                outline.SetPresentation(true, selected.IsHovered, false);
            }
        }
        ElevatorObservation Closest(ElevatorClue clue, bool equivalents)
        {
            ElevatorObservation best = null;
            float distance = float.PositiveInfinity;
            foreach (var candidate in observations)
            {
                if (!candidate || !candidate.CanActivate || !candidate.outlineTarget || !candidate.outlineTarget.isActiveAndEnabled) continue;
                bool matches = candidate.clue == clue;
                if (equivalents)
                {
                    if (clue == ElevatorClue.FloorButtons) matches |= candidate.clue == ElevatorClue.FrontFloorButtons;
                    if (clue == ElevatorClue.Ball) matches |= candidate.clue == ElevatorClue.Friend;
                    if (clue == ElevatorClue.Identity && lesson.Flow.SearchingIdentity)
                        matches |= candidate.clue == ElevatorClue.Notice || candidate.clue == ElevatorClue.FacilityGuide;
                }
                if (!matches) continue;
                float next = candidate.DistanceToPlayer;
                if (next < distance) { distance = next; best = candidate; }
            }
            // This is emphasis only. Both boarding actions and all valid optional
            // inspections keep their original CanActivate/filter/callback behavior.
            return best;
        }

        void EnsurePauseUI()
        {
            var pause = lesson.pauseMenu;
            if (ReplayButton || !pause || !pause.canvas || !pause.resumeButton) return;
            menuCard = pause.resumeButton.transform.parent as RectTransform;
            if (!menuCard) return;
            // Capture authored geometry BEFORE adding a child or applying any layout.
            Capture(menuCard, true, false);
            Capture(pause.resumeButton.transform as RectTransform, false, true);
            Capture(pause.restartButton ? pause.restartButton.transform as RectTransform : null, false, true);
            Capture(pause.titleButton ? pause.titleButton.transform as RectTransform : null, false, true);
            Capture(pause.quitButton ? pause.quitButton.transform as RectTransform : null, false, true);
            Capture(menuCard.Find("Heading") as RectTransform, false, true);
            Capture(menuCard.Find("HeightStatus") as RectTransform, false, true);
            ReplayButton = Control("VR Preview Reread", menuCard, new Vector2(450, 70), "대화 다시 보기", OpenReread);
            BuildTranscript(pause.canvas.transform);
        }
        void Capture(RectTransform rect, bool size, bool position)
        {
            if (!rect || geometry.ContainsKey(rect)) return;
            geometry.Add(rect, new RectState { size = rect.sizeDelta, position = rect.anchoredPosition,
                sizeChanged = size, positionChanged = position });
        }
        void LayoutPauseMenu()
        {
            if (!menuCard || !ReplayButton || !lesson.pauseMenu) return;
            var pause = lesson.pauseMenu;
            menuCard.sizeDelta = new Vector2(550, 650);
            SetRow(pause.resumeButton ? pause.resumeButton.transform as RectTransform : null, 170);
            SetRow(pause.restartButton ? pause.restartButton.transform as RectTransform : null, 77);
            SetRow(pause.titleButton ? pause.titleButton.transform as RectTransform : null, -16);
            ((RectTransform)ReplayButton.transform).anchoredPosition = new Vector2(0, -109);
            SetRow(pause.quitButton ? pause.quitButton.transform as RectTransform : null, -202);
            SetRow(menuCard.Find("Heading") as RectTransform, 265);
            SetRow(menuCard.Find("HeightStatus") as RectTransform, -284);
            // Source rows are 70 high with 93 centre spacing. Heading is 66 high,
            // status 64 high: minimum vertical gap is 15 and all fit inside +/-325.
        }
        void SetRow(RectTransform rect, float y)
        {
            if (!rect) return;
            Capture(rect, false, true); // HeightStatus may be created by a later source LateUpdate.
            rect.anchoredPosition = new Vector2(0, y);
        }

        void BuildTranscript(Transform parent)
        {
            transcript = Rect("VR Preview Paused Conversation", parent, new Vector2(680, 650));
            Surface(transcript, Paper, true);
            var title = Label("Heading", transcript, new Vector2(600, 48), 32, Ink);
            title.text = "대화 다시 보기"; title.rectTransform.anchoredPosition = new Vector2(0, 274);
            speakerLabel = Label("Speaker", transcript, new Vector2(590, 38), 26, Teal);
            speakerLabel.rectTransform.anchoredPosition = new Vector2(0, 220);
            var viewport = Rect("Read Only Viewport", transcript, new Vector2(570, 340));
            viewport.anchoredPosition = new Vector2(-8, 18);
            viewport.gameObject.AddComponent<RectMask2D>();
            var background = viewport.gameObject.AddComponent<Image>();
            background.color = Paper; background.material = WorldMaterial; background.raycastTarget = true;
            transcriptText = Label("Presented Text", viewport, new Vector2(550, 340), 27, Ink);
            var body = transcriptText.rectTransform;
            body.anchorMin = body.anchorMax = body.pivot = new Vector2(.5f, 1);
            transcriptText.alignment = TextAlignmentOptions.TopLeft;
            transcriptText.richText = view.dialogueText && view.dialogueText.richText;
            transcriptText.overflowMode = TextOverflowModes.Overflow;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = body;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = false; scroll.scrollSensitivity = 28;
            scroll.verticalScrollbar = MakeScrollbar(transcript);
            scroll.verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            pageLabel = Label("Page", transcript, new Vector2(590, 36), 21, Teal);
            pageLabel.rectTransform.anchoredPosition = new Vector2(0, -175);
            previousButton = Control("Previous Presented Page", transcript, new Vector2(240, 56), "이전 기록", () => ChangePage(-1));
            ((RectTransform)previousButton.transform).anchoredPosition = new Vector2(-132, -224);
            nextButton = Control("Next Presented Page", transcript, new Vector2(240, 56), "다음 기록", () => ChangePage(1));
            ((RectTransform)nextButton.transform).anchoredPosition = new Vector2(132, -224);
            var back = Control("Back To Pause Menu", transcript, new Vector2(504, 56), "메뉴로 돌아가기", CloseReread);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(0, -289);
            transcript.gameObject.SetActive(false);
        }
        Scrollbar MakeScrollbar(Transform parent)
        {
            var track = Rect("Text Scrollbar", parent, new Vector2(18, 340));
            track.anchoredPosition = new Vector2(295, 18);
            Surface(track, Mint, true);
            var handle = Rect("Handle", track, Vector2.zero);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one;
            handle.offsetMin = new Vector2(2, 2); handle.offsetMax = new Vector2(-2, -2);
            var graphic = Surface(handle, Teal, true);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle; scrollbar.targetGraphic = graphic;
            return scrollbar;
        }
        void OpenReread()
        {
            if (disposed || !lesson || !lesson.IsPaused || !transcript || !menuCard) return;
            if (!rereadOpen)
            {
                menuGroup = menuCard.GetComponent<CanvasGroup>();
                if (!menuGroup)
                {
                    menuGroup = menuCard.gameObject.AddComponent<CanvasGroup>();
                    menuGroup.hideFlags = HideFlags.DontSave; ownsMenuGroup = true;
                }
                menuGroupState = GroupState.Read(menuGroup);
            }
            PrunePages(); pageIndex = Mathf.Max(0, pages.Count - 1); rereadOpen = true;
            HideMenu(); transcript.gameObject.SetActive(true); RefreshTranscript();
        }
        void HideMenu()
        {
            if (!menuGroup) return;
            menuGroup.alpha = 0; menuGroup.interactable = false; menuGroup.blocksRaycasts = false;
        }
        void CloseReread()
        {
            if (rereadOpen && menuGroup) menuGroupState.Restore(menuGroup);
            rereadOpen = false;
            if (transcript) transcript.gameObject.SetActive(false);
            // No Resume, Recenter, lesson selection or flow callback here.
        }
        void ChangePage(int delta)
        {
            if (!rereadOpen || !lesson || !lesson.IsPaused) return;
            PrunePages(); pageIndex = Mathf.Clamp(pageIndex + delta, 0, Mathf.Max(0, pages.Count - 1)); RefreshTranscript();
        }
        void RefreshTranscript()
        {
            if (!transcriptText) return;
            PrunePages();
            var page = pages.Count > 0 ? pages[pageIndex] : null;
            speakerLabel.text = page == null ? string.Empty : page.speaker;
            transcriptText.text = page == null ? "아직 화면에 표시된 대화가 없어요." : page.text;
            pageLabel.text = page == null ? "일시정지 중" : (pageIndex + 1) + " / " + pages.Count + "  ·  일시정지 중";
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pages.Count;
            var body = transcriptText.rectTransform;
            float height = Mathf.Max(scroll.viewport.rect.height, transcriptText.GetPreferredValues(transcriptText.text, body.rect.width, 0).y + 12);
            body.sizeDelta = new Vector2(550, height); body.anchoredPosition = Vector2.zero;
            scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
            transcriptDirty = false;
        }

        Material WorldMaterial => view && view.spatial && view.spatial.storybook ? view.spatial.storybook.worldMaterial : null;
        RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)) { hideFlags = HideFlags.DontSave };
            go.layer = parent ? parent.gameObject.layer : owner ? owner.gameObject.layer : 0;
            var rect = (RectTransform)go.transform; rect.SetParent(parent ? parent : owner, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.sizeDelta = size;
            return rect;
        }
        CorridorBubbleGraphic Surface(RectTransform rect, Color color, bool raycast)
        {
            var graphic = rect.gameObject.AddComponent<CorridorBubbleGraphic>();
            graphic.color = color; graphic.borderColor = Ink; graphic.borderWidth = 2;
            graphic.cornerRadius = 20; graphic.extrusion = 4; graphic.bevelWidth = 3;
            graphic.shadowOffset = new Vector2(3, -5); graphic.shadowOpacity = .10f;
            graphic.material = WorldMaterial; graphic.raycastTarget = raycast;
            return graphic;
        }
        TMP_Text Label(string name, Transform parent, Vector2 size, float fontSize, Color color)
        {
            var label = Rect(name, parent, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = LabelFont;
            if (LabelFont)
            {
                // Masked scroll text and the screen HUD must not share mutable TMP presets.
                var labelInk = new Material(LabelFont.material) { name = "VR preview small-label ink: " + name, hideFlags = HideFlags.DontSave };
                if (labelInk.HasProperty("_OutlineWidth")) labelInk.SetFloat("_OutlineWidth", 0);
                labelInks.Add(labelInk); label.fontSharedMaterial = labelInk;
            }
            label.fontSize = fontSize; label.color = color; label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold; label.fontWeight = FontWeight.Bold; label.extraPadding = true;
            label.UpdateMeshPadding();
            label.enableAutoSizing = false; label.raycastTarget = false;
            return label;
        }
        Button Control(string name, Transform parent, Vector2 size, string text, UnityAction callback)
        {
            var rect = Rect(name, parent, size);
            var graphic = Surface(rect, Mint, true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 1, .94f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.78f, .84f, .77f);
            colors.disabledColor = new Color(.58f, .60f, .56f, .8f); colors.fadeDuration = .10f;
            button.colors = colors; button.onClick.AddListener(callback);
            PrepareText(text);
            var label = Label("Label", rect, size - new Vector2(28, 8), 25, Ink); label.text = text;
            return button;
        }

        public void Dispose()
        {
            CloseReread();
            foreach (var pair in geometry) if (pair.Key) pair.Value.Restore(pair.Key);
            foreach (var pair in rendererAlpha) if (pair.Key) pair.Key.canvasRenderer.SetAlpha(pair.Value);
            foreach (var pair in outlines) if (pair.Key) pair.Value.Restore(pair.Key);
            if (ReplayButton) Object.DestroyImmediate(ReplayButton.gameObject);
            if (transcript) Object.DestroyImmediate(transcript.gameObject);
            if (guide) Object.DestroyImmediate(guide.gameObject);
            foreach (var ink in labelInks) if (ink) Object.DestroyImmediate(ink);
            labelInks.Clear();
            if (LabelFont) Object.DestroyImmediate(LabelFont);
            LabelFont = null; warmedCharacters.Clear();
            if (ownsMenuGroup && menuGroup) Object.DestroyImmediate(menuGroup);
            geometry.Clear(); rendererAlpha.Clear(); outlines.Clear(); pages.Clear();
            ReplayButton = previousButton = nextButton = null;
            guide = menuCard = transcript = null; guideLabel = transcriptText = speakerLabel = pageLabel = null;
            guideVisibility = null;
            menuGroup = null; scroll = null; tutorial = null; observations = null;
            lesson = null; view = null; framing = null; owner = null;
            ownsMenuGroup = rereadOpen = false; disposed = true; transcriptDirty = true;
            observationsFrame = runVersion = -1; pageIndex = 0;
        }
    }
}
#endif
