using System.Collections;
using TMPro;
using UnityEngine;
using ProjectWait.M.Shared;
using ProjectWait.M.Elevator.DialoguePreview;
using System.Collections.Generic;

namespace ProjectWait.YB.OfficeBlackout
{
    // Content-sized world cards retain the existing storybook artwork.
    // Speech belongs to its reading plane, never to the cinematic letterbox.
    public sealed class OfficeBlackoutPresentation : MonoBehaviour
    {
        public Camera viewer;
        public OfficeBlackoutDirector director;
        public OfficeBlackoutCinematicOverlay cinematic;
        public Transform friend, teacher;
        public Canvas readingCanvas, promptCanvas;
        public Canvas deskMarker;
        public RectTransform storyCard, promptCard, notebook;
        public TMP_Text dialogueText, speakerText, promptText;
        public GameObject friendPortrait;
        public UnityEngine.UI.Button restartButton, lobbyButton;
        public UnityEngine.UI.Button nextButton, actionButton;
        public RectTransform storyPaper, speakerRibbon, storyTail, promptPaper, readingPaper, controlsPaper;
        public TMP_Text controlsText, progressText;
        public RectTransform progressCard;
        public string introduction;
        public bool DialogueVisible => storyCard && storyCard.gameObject.activeSelf;
        public string CurrentLine => dialogueText ? dialogueText.text : "";
        public UnityEngine.UI.Button[] CompletionButtons => new[] { restartButton, lobbyButton };
        CanvasGroup readingGroup, promptGroup, notebookGroup;
        bool menuVisible;
        Transform lastSubject;
        readonly List<ElevatorUIPlacement.Region> regions = new List<ElevatorUIPlacement.Region>();
        const float DesktopScale = .82f;
        static readonly Color Ink = new Color32(58, 36, 24, 255);
        static readonly Color Paper = new Color32(255, 249, 231, 255);
        public bool LineAdvanced { get; private set; }
        double readReady;
        readonly List<string> pages = new List<string>();
        int pageIndex;
        string fullLine;
        bool departing, departureStory, departurePrompt, departureNotebook;
        public UnityEngine.UI.Button[] StoryButtons => new[] { nextButton, actionButton };

        void Awake()
        {
            readingGroup = readingCanvas.GetComponent<CanvasGroup>();
            promptGroup = promptCanvas.GetComponent<CanvasGroup>();
            notebookGroup = notebook.GetComponent<CanvasGroup>();
            storyCard.gameObject.SetActive(false); promptCard.gameObject.SetActive(false);
            restartButton.onClick.AddListener(() => OfficeSceneIntegration.Current.Restart());
            lobbyButton.onClick.AddListener(() => OfficeSceneIntegration.Current.ReturnToLobby());
            if (nextButton) nextButton.onClick.AddListener(AdvanceLine);
            if (actionButton) actionButton.onClick.AddListener(ConfirmAction);
            ExperiencePreferences.Changed += ApplySettings; ApplySettings();
        }
        void LateUpdate()
        {
            bool clickReady = OfficeSceneIntegration.Current && OfficeSceneIntegration.Current.ClickReady;
            if (nextButton) nextButton.interactable = clickReady && !OfficeStoryTime.Frozen && Time.unscaledTimeAsDouble >= readReady;
            if (actionButton) actionButton.interactable = clickReady && !OfficeStoryTime.Frozen && director && director.ActionReady;
            if (progressText && director && director.Flow != null)
            {
                progressText.text = director.Flow.CurrentStep switch
                {
                    OfficeBlackoutFlow.Step.Errand => "01  서류를 전해요",
                    OfficeBlackoutFlow.Step.BlackoutReaction or OfficeBlackoutFlow.Step.CalmFriend => "02  친구를 안심시켜요",
                    OfficeBlackoutFlow.Step.KeepFriendInPlace or OfficeBlackoutFlow.Step.WaitForTeacher or OfficeBlackoutFlow.Step.TeacherAnnouncement => "03  함께 기다려요",
                    _ => "04  선생님의 안내를 따라요"
                };
                if (progressCard) progressCard.gameObject.SetActive(!menuVisible && !OfficeStoryTime.Frozen && director.ActionReady);
            }
            if (!deskMarker) return;
            bool visible = !menuVisible && !DialogueVisible && !OfficeStoryTime.Frozen && director && director.Flow != null && director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand;
            deskMarker.gameObject.SetActive(visible);
            if (visible) deskMarker.transform.rotation = Quaternion.LookRotation(deskMarker.transform.position - viewer.transform.position, Vector3.up);
        }
        public void BeginReading()
        {
            LineAdvanced = false;
            readReady = Time.unscaledTimeAsDouble + .35f / ExperiencePreferences.Current.dialogue;
            OfficeSceneIntegration.Current?.RequireFreshClick();
        }
        public void AdvanceLine()
        {
            if (OfficeStoryTime.Frozen || !DialogueVisible || Time.unscaledTimeAsDouble < readReady ||
                !OfficeSceneIntegration.Current.ClickReady) return;
            if (pageIndex + 1 < pages.Count)
            {
                pageIndex++;
                dialogueText.text = pages[pageIndex];
                LayoutCards(); PlaceReading(lastSubject); BeginReading();
                return;
            }
            LineAdvanced = true;
            OfficeSceneIntegration.Current.RequireFreshClick();
        }
        public void ConfirmAction()
        {
            if (!OfficeSceneIntegration.Current || !OfficeSceneIntegration.Current.ClickReady) return;
            if (director.PerformAction()) OfficeSceneIntegration.Current.RequireFreshClick();
        }
        public void ApplySettings()
        {
            Readable(dialogueText, ElevatorDialogueText.BodySize, false);
            if (dialogueText) dialogueText.lineSpacing = 9;
            Readable(speakerText, ElevatorTypography.SpeakerSize);
            Readable(promptText, ElevatorTypography.ChoiceSize, true, true);
            Readable(controlsText, ElevatorTypography.ControlsSize);
            Readable(progressText, ElevatorTypography.HintSize);
            if (nextButton) Readable(nextButton.GetComponentInChildren<TMP_Text>(true), ElevatorTypography.NextSize, true, true);
            if (lobbyButton) Readable(lobbyButton.GetComponentInChildren<TMP_Text>(true), ElevatorTypography.ChoiceSize, true, true);
            if (restartButton) Readable(restartButton.GetComponentInChildren<TMP_Text>(true), ElevatorTypography.ChoiceSize);
            if (deskMarker)
                foreach (var text in deskMarker.GetComponentsInChildren<TMP_Text>(true)) Readable(text, ElevatorTypography.ControlsSize);
            LayoutCards();
            if (DialogueVisible && !string.IsNullOrEmpty(fullLine)) ShowLine(fullLine);
            PromptChanged();
            if (notebook && notebook.gameObject.activeSelf) PlaceCard(notebook.GetComponentInParent<Canvas>(), notebook, new Vector2(.5f, .28f));
        }
        static void Readable(TMP_Text text, float size, bool bold = true, bool primary = false)
        {
            if (!text) return;
            ElevatorTypography.Apply(text, size, bold);
            text.color = primary ? Paper : Ink;
            text.lineSpacing = 5; text.extraPadding = true; text.raycastTarget = false;
        }
        static void Size(RectTransform rect, Vector2 size, Vector2 position)
        {
            if (!rect) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = Vector3.one;
        }
        public void LayoutCards()
        {
            if (!dialogueText || !promptText || !viewer) return;
            const float width = ElevatorDialogueText.PanelWidth;
            dialogueText.enableAutoSizing = promptText.enableAutoSizing = false;
            dialogueText.textWrappingMode = promptText.textWrappingMode = TextWrappingModes.Normal;
            float body = Mathf.Max(48, dialogueText.GetPreferredValues(dialogueText.text, ElevatorDialogueText.TextWidth, 0).y + 8);
            float height = body + 80;
            // The placement bounds include the portrait, name ribbon, tail and
            // manual Next button, not just the frame's inner reading rectangle.
            Size(storyCard, new Vector2(width + 80, height + 120), Vector2.zero);
            Size(storyPaper, new Vector2(width, height), new Vector2(0, 18));
            Size(dialogueText.rectTransform, new Vector2(ElevatorDialogueText.TextWidth, body), new Vector2(0, 18));
            Size(readingPaper, new Vector2(ElevatorDialogueText.TextWidth + 24, body + 14), new Vector2(0, 18));
            float tagHeight = Mathf.Max(58, speakerText.GetPreferredValues().y + 12);
            float tag = Mathf.Clamp(speakerText.GetPreferredValues().x + 54, 132, width - 100);
            bool isFriend = speakerText.text == "친구";
            Size(speakerRibbon, new Vector2(tag, tagHeight), new Vector2(-width * .5f + (isFriend ? 88 : 30) + tag * .5f, height * .5f + 17));
            if (speakerRibbon) Size(speakerRibbon.Find("StorybookArtwork") as RectTransform, speakerRibbon.sizeDelta, Vector2.zero);
            Size(speakerText.rectTransform, new Vector2(tag - 42, tagHeight - 12), new Vector2(-3, 1));
            if (nextButton)
            {
                var label = nextButton.GetComponentInChildren<TMP_Text>(true);
                float nextWidth = Mathf.Max(180, label.GetPreferredValues().x + 44);
                float nextHeight = Mathf.Max(64, label.GetPreferredValues().y + 22);
                Size((RectTransform)nextButton.transform, new Vector2(nextWidth, nextHeight), new Vector2(width * .5f - nextWidth * .5f, -height * .5f - 22));
                Size(label.rectTransform, new Vector2(nextWidth - 38, nextHeight - 18), Vector2.zero);
            }
            if (storyTail)
            {
                Size(storyTail, new Vector2(72, 42), new Vector2(-width * .21f, -height * .5f + 6));
                storyTail.gameObject.SetActive(true);
            }
            if (friendPortrait)
            {
                Size((RectTransform)friendPortrait.transform, new Vector2(83, 83), new Vector2(-width * .5f + 47, height * .5f + 9));
                friendPortrait.SetActive(isFriend);
            }
            float promptHeight = Mathf.Max(76, promptText.GetPreferredValues(promptText.text, width - 64, 0).y + 26);
            if (controlsText) controlsText.text = UnityEngine.XR.XRSettings.isDeviceActive ?
                "가리킨 뒤 검지 트리거로 선택" : "클릭 또는 E로 선택  ·  우클릭 드래그로 둘러보기";
            float controlsHeight = controlsText ? controlsText.GetPreferredValues(controlsText.text, width - 32, 0).y + 20 : 40;
            float progressHeight = Mathf.Max(44, progressText ? progressText.GetPreferredValues("04  선생님의 안내를 따라요", width - 48, 0).y + 16 : 44);
            float actionHeight = promptHeight + controlsHeight + progressHeight + 48;
            Size(promptCard, new Vector2(width, actionHeight), Vector2.zero);
            Size(promptPaper, new Vector2(width, promptHeight), new Vector2(0, actionHeight * .5f - promptHeight * .5f - 6));
            Size(promptText.rectTransform, new Vector2(width - 64, promptHeight - 26), Vector2.zero);
            if (controlsText)
            {
                Size(controlsPaper, new Vector2(width, controlsHeight),
                    new Vector2(0, actionHeight * .5f - promptHeight - 22 - controlsHeight * .5f));
                Size(controlsText.rectTransform, new Vector2(width - 32, controlsHeight - 16), Vector2.zero);
            }
            if (progressText && progressCard)
            {
                Vector2 pillSize = new Vector2(width, progressHeight);
                Size(progressCard, pillSize, new Vector2(0, -actionHeight * .5f + progressHeight * .5f + 6));
                Size(progressText.rectTransform, pillSize - new Vector2(24, 8), Vector2.zero);
            }
            if (notebook && lobbyButton && restartButton)
            {
                float buttonHeight = Mathf.Max(76, lobbyButton.GetComponentInChildren<TMP_Text>(true).GetPreferredValues().y + 26);
                Size(notebook, new Vector2(width, buttonHeight * 2 + 32), Vector2.zero);
                Size((RectTransform)lobbyButton.transform, new Vector2(width, buttonHeight), new Vector2(0, buttonHeight * .5f + 8));
                Size((RectTransform)restartButton.transform, new Vector2(width, buttonHeight), new Vector2(0, -buttonHeight * .5f - 8));
                foreach (var button in CompletionButtons)
                    Size(button.GetComponentInChildren<TMP_Text>(true).rectTransform, new Vector2(width - 64, buttonHeight - 26), Vector2.zero);
            }
        }
        public void ShowLine(string line)
        {
            fullLine = line;
            int colon = line.IndexOf(':');
            string speaker = colon >= 0 ? line.Substring(0, colon).Trim() : "안내";
            string body = colon >= 0 ? line.Substring(colon + 1).Trim() : line;
            speakerText.text = speaker;
            // Paginate rather than shrink text or grow past the shared cinematic bands.
            float availableHeight = UnityEngine.XR.XRSettings.isDeviceActive ? 210 : (720 * .42f - 12) / DesktopScale - 200;
            pages.Clear(); pageIndex = 0;
            foreach (string wrapped in ElevatorDialogueText.Pages(dialogueText, body))
            {
                string current = "";
                foreach (string row in wrapped.Split('\n'))
                {
                    string candidate = string.IsNullOrEmpty(current) ? row : current + "\n" + row;
                    if (current.Length > 0 && dialogueText.GetPreferredValues(candidate, ElevatorDialogueText.TextWidth, 0).y + 8 > availableHeight)
                    { pages.Add(current); current = row; }
                    else current = candidate;
                }
                if (!string.IsNullOrEmpty(current)) pages.Add(current);
            }
            if (pages.Count == 0) pages.Add("");
            dialogueText.text = pages[0];
            Transform subject = speaker == "선생님" && teacher && teacher.gameObject.activeInHierarchy ? teacher : friend;
            // Player responses remain next to the conversation partner, not a floating head HUD.
            LayoutCards();
            PlaceReading(subject);
            lastSubject = subject; storyCard.gameObject.SetActive(true);
            LineAdvanced = false;
        }
        void PlaceReading(Transform subject)
        {
            PlaceCard(readingCanvas, storyCard, cinematic && cinematic.LetterboxRequested ?
                new Vector2(.65f, .43f) : new Vector2(.65f, .25f));
        }
        void PlaceCard(Canvas canvas, RectTransform card, Vector2 preferred)
        {
            if (!viewer || !canvas || !card) return;
            Transform root = canvas.transform;
            const float distance = 2.15f;
            float unit = 2 * distance * Mathf.Tan(viewer.fieldOfView * .5f * Mathf.Deg2Rad) / 720;
            root.SetPositionAndRotation(viewer.transform.position + viewer.transform.forward * distance, viewer.transform.rotation);
            bool xr = UnityEngine.XR.XRSettings.isDeviceActive;
            root.localScale = Vector3.one * unit;
            card.localPosition = Vector3.zero; card.localRotation = Quaternion.identity;
            card.localScale = Vector3.one * (xr ? 1 : DesktopScale);
            regions.Clear();
            foreach (var subject in new[] { friend, teacher })
                if (subject && subject.gameObject.activeInHierarchy && ElevatorUIPlacement.Project(viewer,
                    new Bounds(subject.position + Vector3.up * (subject == teacher ? 1.52f : 1.18f), new Vector3(.65f, .55f, .5f)), out var face))
                    regions.Add(new ElevatorUIPlacement.Region(face, 80, "Face"));
            Vector2 size = new Vector2(card.rect.width * card.localScale.x / (720 * viewer.aspect), card.rect.height * card.localScale.y / 720);
            bool framed = cinematic && cinematic.LetterboxRequested;
            Vector2 center = ElevatorUIPlacement.Choose(size, regions, preferred, framed ? .29f : .06f, framed ? .71f : .94f, out _);
            card.anchoredPosition = new Vector2((center.x - .5f) * 720 * viewer.aspect, (center.y - .5f) * 720);
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                if (StereoUISafeArea.TryCapture(viewer, out var frame)) PlaceStereoCard(canvas, card, frame, center);
            }
            canvas.worldCamera = viewer;
        }
        public bool PlaceStereoCard(Canvas canvas, RectTransform card, StereoUISafeArea.Frame frame, Vector2 viewport)
        {
            Transform root = canvas.transform;
            VRUIReadingLayout.SetPlane(root, viewer.transform);
            card.localScale = Vector3.one;
            if (!VRUIReadingLayout.Area(frame, (RectTransform)root, out var area, out _) ||
                !frame.EyeVerticalRange(root, 0, out var left) || !frame.EyeVerticalRange(root, 1, out var right)) return false;
            // Keep the complete card between the native-eye 200/720 letterbox bands.
            if (cinematic && cinematic.LetterboxRequested)
            {
                const float band = 200f / 720f;
                area.yMin = Mathf.Max(area.yMin, Mathf.Max(Mathf.Lerp(left.x, left.y, band), Mathf.Lerp(right.x, right.y, band)));
                area.yMax = Mathf.Min(area.yMax, Mathf.Min(Mathf.Lerp(left.x, left.y, 1-band), Mathf.Lerp(right.x, right.y, 1-band)));
            }
            return VRUIReadingLayout.Place(card, root, area, (viewport.x - .5f) * 45, (viewport.y - .5f) * 40, out _);
        }
        public void ClearLine() { if (storyCard) storyCard.gameObject.SetActive(false); lastSubject = null; fullLine = null; }
        public void PromptChanged()
        {
            if (!promptCard || !promptCard.gameObject.activeSelf) return;
            LayoutCards();
            PlaceCard(promptCanvas, promptCard, new Vector2(.65f, .30f));
        }
        public void SetMenuVisible(bool value)
        {
            menuVisible = value;
            if (readingGroup) readingGroup.alpha = value ? 0 : 1;
            if (promptGroup) promptGroup.alpha = value ? 0 : 1;
            if (notebookGroup) { notebookGroup.alpha = value ? 0 : 1; notebookGroup.blocksRaycasts = !value; }
        }
        public void ShowCompletion()
        {
            ClearLine(); promptCard.gameObject.SetActive(false);
            LayoutCards();
            PlaceCard(notebook.GetComponentInParent<Canvas>(), notebook, new Vector2(.5f, .28f));
            notebook.gameObject.SetActive(true);
            SetMenuVisible(menuVisible);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = !UnityEngine.XR.XRSettings.isDeviceActive;
            OfficeSceneIntegration.Current.RequireFreshClick();
        }
        public void PrepareDeparture()
        {
            if (departing) return;
            departing = true;
            departureStory = DialogueVisible;
            departurePrompt = promptCard && promptCard.gameObject.activeSelf;
            departureNotebook = notebook && notebook.gameObject.activeSelf;
            if (storyCard) storyCard.gameObject.SetActive(false);
            if (promptCard) promptCard.gameObject.SetActive(false);
            if (notebook) notebook.gameObject.SetActive(false);
            if (deskMarker) deskMarker.gameObject.SetActive(false);
            if (progressCard) progressCard.gameObject.SetActive(false);
        }
        public void RestoreDeparture()
        {
            if (!departing) return;
            departing = false;
            if (storyCard) storyCard.gameObject.SetActive(departureStory);
            if (promptCard) promptCard.gameObject.SetActive(departurePrompt);
            if (notebook) notebook.gameObject.SetActive(departureNotebook);
            OfficeSceneIntegration.Current?.RequireFreshClick();
        }
        void OnDestroy() { ExperiencePreferences.Changed -= ApplySettings; }
    }
}
