using TMPro;
using UnityEngine;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator
{
    // Artwork is independent of dialogue content and the scenario's interaction rules.
    [DisallowMultipleComponent]
    public sealed class ElevatorStorybookUI : MonoBehaviour
    {
        public ElevatorView view;
        public Sprite dialogueFrame, thoughtCloud, primaryButton, secondaryButton;
        public Sprite speakerRibbon, thoughtRibbon, observationNote, friendPortrait, speechTail;
        public GameObject thoughtBubblePrefab;
        public Material worldMaterial;
        RectTransform thoughtBubble;
        TMP_Text bubbleThought, bubbleHeading;
        static readonly Color Ink = new Color32(58, 36, 24, 255);
        static readonly Color Paper = new Color32(255, 249, 231, 255);

        void Awake()
        {
            if (!view) view = GetComponent<ElevatorView>();
            ApplyStatic();
        }

        public UnityEngine.UI.Image Artwork(RectTransform parent, Sprite sprite, bool sliced = true, string name = "StorybookArtwork")
        {
            if (!parent || !sprite) return null;
            var old = parent.GetComponent<CorridorBubbleGraphic>();
            if (old) old.enabled = false;
            var child = parent.Find(name) as RectTransform;
            if (!child)
            {
                child = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
                child.SetParent(parent, false);
            }
            child.SetSiblingIndex(0);
            child.anchorMin = Vector2.zero; child.anchorMax = Vector2.one;
            child.offsetMin = child.offsetMax = Vector2.zero;
            var image = child.GetComponent<UnityEngine.UI.Image>();
            if (!image) image = child.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.type = sliced && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.pixelsPerUnitMultiplier = sprite == primaryButton || sprite == secondaryButton ? 6 : 4;
            image.color = Color.white; image.raycastTarget = false;
            image.material = worldMaterial;
            return image;
        }

        UnityEngine.UI.Image Decoration(RectTransform parent, string name, Sprite sprite, Vector2 size, Vector2 position)
        {
            if (!sprite) return null;
            var r = parent.Find(name) as RectTransform;
            if (!r)
            {
                r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
                r.SetParent(parent, false);
                r.gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
            r.sizeDelta = size; r.anchoredPosition = position;
            var image = r.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.color = Color.white;
            image.raycastTarget = false; image.material = worldMaterial;
            return image;
        }

        static void Readable(TMP_Text text, Color color, float size, bool bold = false)
        {
            if (!text) return;
            text.color = color; text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 5; text.extraPadding = true; text.raycastTarget = false;
        }

        public void ApplyStatic()
        {
            if (!view || !view.spatial || !dialogueFrame) return;
            var spatial = view.spatial;
            Artwork(view.dialoguePanel, dialogueFrame);
            Artwork(spatial.speakerTab, speakerRibbon, false);
            Artwork(spatial.mission, dialogueFrame);
            Readable(view.dialogueText, Ink, 29);
            Readable(spatial.speaker, Ink, 24, true);
            Readable(view.thought, Ink, 23);
            Readable(spatial.thoughtHeading, Ink, 18, true);
            foreach (var button in view.developerPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true)) SkinButton(button);
            foreach (var row in spatial.completionRows)
            {
                Artwork(row, secondaryButton);
                foreach (var text in row.GetComponentsInChildren<TMP_Text>(true)) text.color = Ink;
            }
        }

        public void SkinButton(UnityEngine.UI.Button button)
        {
            if (!button) return;
            bool primary = button.name == "Choice0" || button.name == "DialogueNext" || button.name.Contains("Resume") || button.name.Contains("KeepMemory");
            var image = Artwork((RectTransform)button.transform, primary ? primaryButton : secondaryButton);
            if (!image) return;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 1, .94f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.78f, .84f, .77f);
            colors.disabledColor = new Color(.58f, .60f, .56f, .8f); colors.fadeDuration = .10f;
            button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            Readable(label, primary ? Paper : Ink, 26, true);
            var oldDepth = button.GetComponent<CorridorButtonDepth>();
            if (oldDepth) oldDepth.enabled = false;
            var feedback = button.GetComponent<StorybookButtonFeedback>();
            if (!feedback) feedback = button.gameObject.AddComponent<StorybookButtonFeedback>();
            feedback.Configure(button, image.rectTransform, label ? label.rectTransform : null);
        }

        public void LayoutWorld()
        {
            if (!view || !view.spatial || !dialogueFrame) return;
            var spatial = view.spatial;
            var panel = view.dialoguePanel;
            bool friend = spatial.speaker.text == "친구";
            bool call = spatial.speaker.text.Contains("관리실") || spatial.speaker.text.Contains("119");
            var tag = Artwork(spatial.speakerTab, call ? thoughtRibbon : speakerRibbon, false);
            float tagWidth = Mathf.Clamp(spatial.speaker.GetPreferredValues().x + 54, 132, 292);
            spatial.speakerTab.sizeDelta = new Vector2(tagWidth, 49);
            spatial.speakerTab.anchoredPosition = new Vector2(-panel.rect.width * .5f + (friend ? 88 : 30) + tagWidth * .5f, panel.rect.height * .5f - 1);
            spatial.speaker.rectTransform.sizeDelta = new Vector2(tagWidth - 42, 36);
            spatial.speaker.rectTransform.anchoredPosition = new Vector2(-3, 1);
            var portrait = Decoration(panel, "FriendPortrait", friendPortrait, new Vector2(83, 83), new Vector2(-panel.rect.width * .5f + 47, panel.rect.height * .5f - 9));
            if (portrait) portrait.gameObject.SetActive(friend);
            var tail = Decoration(panel, "SpeechTail", speechTail, new Vector2(72, 42), new Vector2(-panel.rect.width * .21f, -panel.rect.height * .5f - 12));
            if (tail) tail.transform.SetSiblingIndex(0);
            view.dialogueText.rectTransform.sizeDelta = new Vector2(panel.rect.width - 92, panel.rect.height - 78);
            view.dialogueText.rectTransform.anchoredPosition = new Vector2(0, -7);
            foreach (var button in view.ChoiceButtons)
            {
                if (!button) continue;
                var label = button.GetComponentInChildren<TMP_Text>();
                var r = (RectTransform)button.transform;
                label.rectTransform.sizeDelta = r.sizeDelta - (r.rect.width < 300 ? new Vector2(38, 18) : new Vector2(80, 26));
                label.rectTransform.anchoredPosition = Vector2.zero;
                var feedback = button.GetComponent<StorybookButtonFeedback>();
                if (feedback) feedback.CaptureRest();
            }
            spatial.speakerTab.SetAsLastSibling();
            if (portrait) portrait.transform.SetAsLastSibling();
        }

        public void LayoutThought(bool observation)
        {
            if (thoughtBubblePrefab && LayoutThoughtPrefab(observation)) return;
            if (!view || !view.spatial || !thoughtCloud) return;
            var panel = view.thoughtPanel;
            var spatial = view.spatial;
            float width = observation ? 320 : 360;
            float textWidth = width * (observation ? .76f : .68f);
            Readable(view.thought, Ink, 23);
            float body = Mathf.Max(56, view.thought.GetPreferredValues(view.thought.text, textWidth, 0).y + 10);
            float height = observation ? Mathf.Max(178, body + 100) : Mathf.Max(254, (body + 82) / .69f);
            panel.sizeDelta = new Vector2(width, height);
            Artwork(panel, observation ? observationNote : thoughtCloud, observation);
            view.thought.rectTransform.sizeDelta = new Vector2(textWidth, body);
            view.thought.rectTransform.anchoredPosition = new Vector2(0, observation ? -22 : 0);
            view.thought.alignment = observation ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            var ribbon = Decoration(panel, "ThoughtRibbon", thoughtRibbon, new Vector2(160, 43), new Vector2(0, height * .5f - 55));
            if (ribbon) ribbon.gameObject.SetActive(!observation);
            Readable(spatial.thoughtHeading, Ink, 18, true);
            spatial.thoughtHeading.text = observation ? "살펴보니" : "나의 생각";
            spatial.thoughtHeading.gameObject.SetActive(!observation);
            spatial.thoughtHeading.rectTransform.sizeDelta = new Vector2(observation ? 90 : 140, 32);
            spatial.thoughtHeading.rectTransform.anchoredPosition = observation ? new Vector2(147 - width * .5f, height * .5f - 40) : new Vector2(0, height * .5f - 55);
            spatial.thoughtHeading.alignment = TextAlignmentOptions.Center;
            spatial.thoughtHeading.transform.SetAsLastSibling();
        }

        bool LayoutThoughtPrefab(bool observation)
        {
            if (!view || !view.spatial || !view.thoughtPanel) return false;
            var panel = view.thoughtPanel;
            if (!thoughtBubble)
            {
                // Reuse the current prefab's complete artwork, ribbon and typography.
                // Its outer world-space canvas is deliberately not nested in the HUD.
                var template = thoughtBubblePrefab.transform.Find("ThoughtCloud") as RectTransform;
                if (!template) return false;
                thoughtBubble = Instantiate(template, panel, false);
                thoughtBubble.name = "CurrentThoughtBubble";
                bubbleThought = thoughtBubble.Find("Thought")?.GetComponent<TMP_Text>();
                bubbleHeading = thoughtBubble.Find("Heading")?.GetComponent<TMP_Text>();
                if (!bubbleThought || !bubbleHeading)
                {
                    Destroy(thoughtBubble.gameObject);thoughtBubble=null;return false;
                }
                foreach (Transform child in panel)
                    if (child != thoughtBubble) child.gameObject.SetActive(false);
                var legacy = panel.GetComponent<CorridorBubbleGraphic>();
                if (legacy) legacy.enabled = false;
            }
            bubbleThought.text = view.thought.text;
            bubbleThought.maxVisibleCharacters = int.MaxValue;
            bubbleHeading.text = observation ? "살펴보니" : "나의 생각";
            bubbleHeading.gameObject.SetActive(!observation);
            float width = panel.rect.width;
            float scale = width / 520f;
            float body = Mathf.Max(110, bubbleThought.GetPreferredValues(bubbleThought.text, 364, 0).y + 12);
            float height = observation ? Mathf.Max(300, body + 180) : Mathf.Max(368, body + 258);
            thoughtBubble.anchorMin = thoughtBubble.anchorMax = thoughtBubble.pivot = Vector2.one * .5f;
            thoughtBubble.anchoredPosition = Vector2.zero;
            thoughtBubble.sizeDelta = new Vector2(520, height);
            thoughtBubble.localScale = Vector3.one * scale;
            panel.sizeDelta = new Vector2(width, height * scale);
            bubbleThought.rectTransform.sizeDelta = new Vector2(364, body);
            bubbleThought.rectTransform.anchoredPosition = new Vector2(0, 2);
            bubbleHeading.rectTransform.anchoredPosition = new Vector2(0, height * .5f - 72);
            var ribbon = thoughtBubble.Find("ThoughtRibbon") as RectTransform;
            if (ribbon){ribbon.gameObject.SetActive(!observation);ribbon.anchoredPosition = new Vector2(0, height * .5f - 72);}
            return true;
        }

        public void SkinPause(ElevatorPauseMenu pause)
        {
            if (!pause || !pause.canvas) return;
            foreach (var graphic in pause.canvas.GetComponentsInChildren<CorridorBubbleGraphic>(true))
            {
                if (graphic.GetComponent<UnityEngine.UI.Button>()) continue;
                Artwork(graphic.rectTransform, dialogueFrame);
            }
            foreach (var button in pause.Buttons) SkinButton(button);
            foreach (var text in pause.canvas.GetComponentsInChildren<TMP_Text>(true))
                if (!text.GetComponentInParent<UnityEngine.UI.Button>(true)) text.color = Ink;
        }

        public void HighlightHistory(int selected)
        {
            if (!view || !view.spatial) return;
            for (int i = 0; i < view.spatial.completionRows.Length; i++)
            {
                var image = view.spatial.completionRows[i].Find("StorybookArtwork")?.GetComponent<UnityEngine.UI.Image>();
                if (image) image.color = i == selected ? new Color(.88f, 1, .83f) : Color.white;
            }
        }
    }
}
