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
    [RequireComponent(typeof(DialogueFocusWindow))]
    public sealed class OfficeBlackoutPresentation : MonoBehaviour
    {
        public Camera viewer;
        public OfficeBlackoutDirector director;
        public OfficeBlackoutCinematicOverlay cinematic;
        public Transform friend, teacher;
        public Canvas readingCanvas, promptCanvas;
        [Header("Fixed world reading stations (positive Z points away from reader)")]
        [SerializeField] Transform deskStation, friendStation, teacherStation, exitStation;
        [SerializeField, Min(.0001f)] float worldUnitsPerPoint = .002f;
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
        public bool ActionThoughtVisible { get; private set; }
        public string CurrentLine => dialogueText ? dialogueText.text : "";
        public UnityEngine.UI.Button[] CompletionButtons => new[] { restartButton, lobbyButton };
        CanvasGroup readingGroup, promptGroup, notebookGroup;
        bool menuVisible;
        const float DesktopScale = .82f;
        static readonly Color Ink = new Color32(58, 36, 24, 255);
        static readonly Color Paper = new Color32(255, 249, 231, 255);
        readonly List<string> pages = new List<string>();
        int pageIndex;
        string fullLine;
        bool departing, departureStory, departurePrompt, departureNotebook;
        public UnityEngine.UI.Button[] StoryButtons { get; private set; }
        UnityEngine.UI.Button otherButton;
        TMP_Text otherText;
        DialogueFocusWindow focusWindow;
        public Transform ReadingStation
        {
            get
            {
                var step = director && director.Flow != null ? director.Flow.CurrentStep : OfficeBlackoutFlow.Step.NotStarted;
                if (step == OfficeBlackoutFlow.Step.EscortToExit || step == OfficeBlackoutFlow.Step.Complete) return exitStation;
                if (step == OfficeBlackoutFlow.Step.TeacherAnnouncement || step == OfficeBlackoutFlow.Step.TeacherArrives) return teacherStation;
                return step == OfficeBlackoutFlow.Step.NotStarted || step == OfficeBlackoutFlow.Step.Errand ? deskStation : friendStation;
            }
        }

        void Awake()
        {
            readingGroup = readingCanvas.GetComponent<CanvasGroup>();
            promptGroup = promptCanvas.GetComponent<CanvasGroup>();
            notebookGroup = notebook.GetComponent<CanvasGroup>();
            storyCard.gameObject.SetActive(false); promptCard.gameObject.SetActive(false);
            restartButton.onClick.AddListener(() => OfficeSceneIntegration.Current.Restart());
            lobbyButton.onClick.AddListener(() => OfficeSceneIntegration.Current.ReturnToLobby());
            if (nextButton) nextButton.gameObject.SetActive(false);
            focusWindow = GetComponent<DialogueFocusWindow>();
            focusWindow.Bind(viewer, storyCard, dialogueText, speakerText);
            // Clone the existing storybook button before binding it: both replies share one style.
            otherButton = Instantiate(actionButton, actionButton.transform.parent);
            otherButton.name = "OtherReply";
            otherText = otherButton.GetComponentInChildren<TMP_Text>(true);
            otherButton.onClick.AddListener(() => SelectAnswer(false));
            actionButton.onClick.AddListener(() => SelectAnswer(true));
            StoryButtons = new[] { actionButton, otherButton };
            ExperiencePreferences.Changed += ApplySettings; ApplySettings();
        }
        void LateUpdate()
        {
            focusWindow.UpdateFocus(ActionThoughtVisible && DialogueVisible && !menuVisible && !departing && !OfficeStoryTime.Frozen,
                Time.unscaledDeltaTime);
            bool clickReady = OfficeSceneIntegration.Current && OfficeSceneIntegration.Current.ClickReady;
            bool ready = clickReady && !OfficeStoryTime.Frozen && director && director.ActionReady && !director.ConsequencePlaying;
            actionButton.interactable = ready && (director.Choosing || director.Failed);
            otherButton.interactable = ready && director.Choosing;
            if (progressText && director && director.Flow != null)
            {
                progressText.text = director.Flow.CurrentStep switch
                {
                    OfficeBlackoutFlow.Step.Errand => "01  책을 전해요",
                    OfficeBlackoutFlow.Step.BlackoutReaction or OfficeBlackoutFlow.Step.CalmFriend => "02  친구를 안심시켜요",
                    OfficeBlackoutFlow.Step.KeepFriendInPlace or OfficeBlackoutFlow.Step.WaitForTeacher or OfficeBlackoutFlow.Step.TeacherAnnouncement => "03  함께 기다려요",
                    _ => "04  선생님의 안내를 따라요"
                };
                if (progressCard) progressCard.gameObject.SetActive(!menuVisible && !OfficeStoryTime.Frozen && director.ActionReady);
            }
        }
        public IEnumerator ReadPages(float minimumSeconds)
        {
            while (pageIndex < pages.Count)
            {
                float elapsed = 0;
                while (elapsed < Mathf.Max(minimumSeconds, pages[pageIndex].Length * .12f) || OfficeStoryTime.Frozen)
                {
                    yield return null;
                    elapsed += OfficeStoryTime.Delta * ExperiencePreferences.Current.dialogue;
                }
                if (pageIndex + 1 >= pages.Count) yield break;
                ++pageIndex;
                dialogueText.text = pages[pageIndex];
                LayoutCards(); PlaceCard(readingCanvas, storyCard, 0);
            }
        }
        void SelectAnswer(bool safe)
        {
            if (!OfficeSceneIntegration.Current || !OfficeSceneIntegration.Current.ClickReady) return;
            if (director.Failed ? safe && director.RetryAnswer() : director.Answer(safe))
                OfficeSceneIntegration.Current.RequireFreshClick();
        }
        public void ApplySettings()
        {
            Readable(dialogueText, ElevatorDialogueText.BodySize, false);
            if (dialogueText) dialogueText.lineSpacing = 9;
            Readable(speakerText, ElevatorTypography.SpeakerSize);
            Readable(promptText, ElevatorTypography.ChoiceSize, true, true);
            Readable(otherText, ElevatorTypography.ChoiceSize, true, true);
            Readable(controlsText, ElevatorTypography.ControlsSize);
            Readable(progressText, ElevatorTypography.HintSize);
            if (lobbyButton) Readable(lobbyButton.GetComponentInChildren<TMP_Text>(true), ElevatorTypography.ChoiceSize, true, true);
            if (restartButton) Readable(restartButton.GetComponentInChildren<TMP_Text>(true), ElevatorTypography.ChoiceSize);
            LayoutCards();
            if (DialogueVisible && !string.IsNullOrEmpty(fullLine))
            {
                int previous = pageIndex;
                SetReading(fullLine, ActionThoughtVisible);
                pageIndex = Mathf.Min(previous, pages.Count - 1);
                dialogueText.text = pages[pageIndex];
                LayoutCards(); PlaceCard(readingCanvas, storyCard, 0);
            }
            PromptChanged();
            if (notebook && notebook.gameObject.activeSelf) PlaceCompletion();
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
            // Include the portrait, ribbon and tail in placement bounds.
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
            if (storyTail)
            {
                Size(storyTail, new Vector2(72, 42), new Vector2(-width * .21f, -height * .5f + 6));
                storyTail.gameObject.SetActive(speakerText.text != "내 생각");
            }
            if (friendPortrait)
            {
                Size((RectTransform)friendPortrait.transform, new Vector2(83, 83), new Vector2(-width * .5f + 47, height * .5f + 9));
                friendPortrait.SetActive(isFriend);
            }
            bool choose = director && director.Choosing, retry = director && director.Failed;
            actionButton.gameObject.SetActive(choose || retry);
            otherButton.gameObject.SetActive(choose);
            if (choose) otherText.text = director.OtherReply;
            if (controlsText) controlsText.text = director != null && director.ActionReady ? director.Instruction : "";
            float promptHeight = Mathf.Max(76, promptText.GetPreferredValues(promptText.text, width - 64, 0).y + 26);
            if (choose) promptHeight = Mathf.Max(promptHeight, otherText.GetPreferredValues(otherText.text, width - 64, 0).y + 26);
            float controlsHeight = controlsText ? controlsText.GetPreferredValues(controlsText.text, width - 32, 0).y + 20 : 40;
            float progressHeight = Mathf.Max(44, progressText ? progressText.GetPreferredValues("04  선생님의 안내를 따라요", width - 48, 0).y + 16 : 44);
            float buttonsHeight = choose ? promptHeight * 2 + 12 : retry ? promptHeight : 0;
            float actionHeight = buttonsHeight + controlsHeight + progressHeight + 48;
            Size(promptCard, new Vector2(width, actionHeight), Vector2.zero);
            float firstY = actionHeight * .5f - controlsHeight - promptHeight * .5f - 18;
            bool safeFirst = !choose || director.Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend;
            Size(promptPaper, new Vector2(width, promptHeight), new Vector2(0, safeFirst ? firstY : firstY - promptHeight - 12));
            Size(promptText.rectTransform, new Vector2(width - 64, promptHeight - 26), Vector2.zero);
            Size((RectTransform)otherButton.transform, new Vector2(width, promptHeight), new Vector2(0, safeFirst ? firstY - promptHeight - 12 : firstY));
            Size(otherText.rectTransform, new Vector2(width - 64, promptHeight - 26), Vector2.zero);
            if (controlsText)
            {
                Size(controlsPaper, new Vector2(width, controlsHeight),
                    new Vector2(0, actionHeight * .5f - 6 - controlsHeight * .5f));
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
        public void ShowLine(string line) => SetReading(line, false);
        void SetReading(string line, bool actionThought)
        {
            ActionThoughtVisible = actionThought;
            fullLine = line;
            int colon = line.IndexOf(':');
            string speaker = colon >= 0 ? line.Substring(0, colon).Trim() : "내 생각";
            string body = colon >= 0 ? line.Substring(colon + 1).Trim() : line;
            speakerText.text = speaker;
            // Paginate rather than shrink text or grow past the shared cinematic bands.
            float availableHeight = UnityEngine.XR.XRSettings.isDeviceActive ? 210 : (720 * .42f - 12) / DesktopScale - 200;
            pages.Clear(); pageIndex = 0;
            if (actionThought || director && director.Flow?.CurrentStep == OfficeBlackoutFlow.Step.Complete)
                pages.Add(body); // Persistent cards have no timed page turn.
            else foreach (string wrapped in ElevatorDialogueText.Pages(dialogueText, body))
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
            LayoutCards();
            PlaceCard(readingCanvas, storyCard, 0);
            storyCard.gameObject.SetActive(true);
        }
        void PlaceCard(Canvas canvas, RectTransform card, float verticalOffset)
        {
            var station = ReadingStation;
            if (!station || !canvas || !card) return;
            Transform root = canvas.transform;
            root.SetPositionAndRotation(station.position + Vector3.up * verticalOffset, station.rotation);
            root.localScale = Vector3.one * worldUnitsPerPoint;
            if (card != root)
            {
                card.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                card.localScale = Vector3.one;
            }
            canvas.worldCamera = viewer;
        }
        public void ClearLine() { if (storyCard) storyCard.gameObject.SetActive(false); ActionThoughtVisible = false; fullLine = null; }
        public void PromptChanged()
        {
            if (!promptCard || !promptCard.gameObject.activeSelf) return;
            if (director && director.ActionReady && director.Phase == OfficeBlackoutDirector.ActionPhase.Inspect)
            {
                promptCard.gameObject.SetActive(false);
                if (director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand || director.Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend)
                    LookAroundHint.ShowAt(viewer, ReadingStation, .59f, worldUnitsPerPoint);
                SetReading("내 생각: " + director.Instruction, true);
                return;
            }
            LayoutCards();
            // After the bump the friend stands beside the desk: leave their face visible above the retry card.
            bool byDesk = director.Failed && director.Flow.CurrentStep == OfficeBlackoutFlow.Step.CalmFriend;
            PlaceCard(promptCanvas, promptCard, byDesk ? -.48f : -.10f);
        }
        public void SetMenuVisible(bool value)
        {
            menuVisible = value;
            if (readingGroup) readingGroup.alpha = value ? 0 : 1;
            if (promptGroup) { promptGroup.alpha = value ? 0 : 1; promptGroup.blocksRaycasts = !value; }
            if (notebookGroup) { notebookGroup.alpha = value ? 0 : 1; notebookGroup.blocksRaycasts = !value; }
        }
        public void ShowCompletion()
        {
            promptCard.gameObject.SetActive(false);
            ShowLine("친구: 같이 있어 줘서 덜 무서웠어.");
            PlaceCompletion();
            notebook.gameObject.SetActive(true);
            SetMenuVisible(menuVisible);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = !UnityEngine.XR.XRSettings.isDeviceActive;
            OfficeSceneIntegration.Current.RequireFreshClick();
        }
        void PlaceCompletion()
        {
            // Stack both canvases at the authored exit, including room for the portrait and tail.
            const float speechOffset = -.12f;
            float separation = (storyCard.rect.height * .5f + notebook.rect.height * .5f) * worldUnitsPerPoint;
            PlaceCard(readingCanvas, storyCard, speechOffset);
            PlaceCard(notebook.GetComponentInParent<Canvas>(true), notebook, speechOffset - separation);
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
