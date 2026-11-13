// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorStorybookUI.cs. Original is not modified.
using TMPro;
using UnityEngine;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator.DialoguePreview
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
        RectTransform speechTailRect;
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
            text.color = color;
            ElevatorTypography.Apply(text, size, bold);
            text.lineSpacing = 5; text.extraPadding = true; text.raycastTarget = false;
        }

        public void ApplyStatic()
        {
            if (!view || !view.spatial || !dialogueFrame) return;
            var spatial = view.spatial;
            Artwork(view.dialoguePanel, dialogueFrame);
            Artwork(spatial.speakerTab, speakerRibbon, false);
            Artwork(spatial.mission, dialogueFrame);
            Readable(view.dialogueText, Ink, ElevatorDialogueText.BodySize);
            view.dialogueText.lineSpacing = 9;
            view.dialogueText.alignment = TextAlignmentOptions.MidlineLeft;
            Readable(spatial.speaker, Ink, ElevatorTypography.SpeakerSize, true);
            Readable(view.thought, Ink, ElevatorTypography.ThoughtSize, true);
            Readable(spatial.thoughtHeading, Ink, 18, true);
            ApplyGuidanceTypography();
            foreach (var button in view.developerPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true)) SkinButton(button);
            foreach (var row in spatial.completionRows)
            {
                Artwork(row, secondaryButton);
                foreach (var text in row.GetComponentsInChildren<TMP_Text>(true)) text.color = Ink;
            }
        }

        void ApplyGuidanceTypography()
        {
            var spatial = view.spatial;
            Readable(view.title, Ink, ElevatorTypography.HeadingSize, true);
            Readable(view.subtitle, Ink, ElevatorTypography.ObjectiveSize, true);
            Readable(spatial.progress, Ink, ElevatorTypography.HintSize, true);
            Readable(spatial.controls, Ink, ElevatorTypography.ControlsSize, true);
            if (view.lesson && view.lesson.pauseMenu) SkinPause(view.lesson.pauseMenu);
            if (!spatial.notebook) return;
            foreach (var text in spatial.notebook.GetComponentsInChildren<TMP_Text>(true))
            {
                switch (text.name)
                {
                    case "NotebookTitle": Readable(text, Ink, 36, true); break;
                    case "NotebookEyebrow":
                        Readable(text, Ink, 20, true);
                        text.rectTransform.sizeDelta = new Vector2(420, 34);
                        break;
                    case "StepLabel": Readable(text, Ink, 24, true); break;
                    case "Done":
                        Readable(text, Ink, 22, true);
                        text.rectTransform.sizeDelta = new Vector2(120, 42);
                        ((RectTransform)text.transform.parent).sizeDelta = new Vector2(128, 50);
                        ((RectTransform)text.transform.parent).anchoredPosition = new Vector2(139, 0);
                        break;
                    case "NotebookFooter": Readable(text, Ink, 26, true); break;
                    case "MemoryTitle": Readable(text, Ink, 28, true); break;
                    case "MemoryDetail":
                        Readable(text, Ink, 24, true);
                        text.rectTransform.sizeDelta = new Vector2(426, 112);
                        text.rectTransform.anchoredPosition = new Vector2(0, -116);
                        break;
                }
            }
        }

        public void SkinButton(UnityEngine.UI.Button button)
        {
            if (!button) return;
            bool judgment = view && view.lesson && view.lesson.Flow != null && view.lesson.Flow.UsesObjectConfirmation;
            // Both replies have identical styling; only closing is secondary.
            bool primary = judgment ? view.lesson.Flow.CanConfirmObject && (button.name == "Choice0" || button.name == "Choice1") :
                button.name == "Choice0" || button.name == "DialogueNext" || button.name.Contains("Resume") || button.name.Contains("KeepMemory");
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
            bool developer = view && view.developerPanel && button.transform.IsChildOf(view.developerPanel);
            float size = developer ? 26 : button.name == "DialogueNext" ? ElevatorTypography.NextSize : ElevatorTypography.ChoiceSize;
            Readable(label, primary ? Paper : Ink, size, true);
            var oldDepth = button.GetComponent<CorridorButtonDepth>();
            if (oldDepth) oldDepth.enabled = false;
            var legacyFeedback=button.GetComponent<StorybookButtonFeedback>();
            if(legacyFeedback)legacyFeedback.enabled=false;
            var feedback = button.GetComponent<ElevatorSmoothButtonFeedback>();
            if (!feedback) feedback = button.gameObject.AddComponent<ElevatorSmoothButtonFeedback>();
            feedback.Configure(button, image.rectTransform, label ? label.rectTransform : null);
        }

        public void LayoutWorld() => LayoutWorld(UnityEngine.XR.XRSettings.isDeviceActive);

        public void LayoutWorld(bool vr)
        {
            if (!view || !view.spatial || !dialogueFrame) return;
            var spatial = view.spatial;
            var panel = view.dialoguePanel;
            bool friend = spatial.speaker.text == "친구";
            bool call = spatial.speaker.text.Contains("관리실") || spatial.speaker.text.Contains("119");
            var tag = Artwork(spatial.speakerTab, call ? thoughtRibbon : speakerRibbon, false);
            float tagWidth = Mathf.Clamp(spatial.speaker.GetPreferredValues().x + 54, 132, vr?panel.rect.width-60:292);
            spatial.speakerTab.sizeDelta = new Vector2(tagWidth, 58);
            spatial.speakerTab.anchoredPosition = new Vector2(-panel.rect.width * .5f + (friend ? 88 : 30) + tagWidth * .5f, panel.rect.height * .5f - 1);
            spatial.speaker.rectTransform.sizeDelta = new Vector2(tagWidth - 42, 46);
            spatial.speaker.rectTransform.anchoredPosition = new Vector2(-3, 1);
            var portrait = Decoration(panel, "FriendPortrait", friendPortrait, new Vector2(83, 83), new Vector2(-panel.rect.width * .5f + 47, panel.rect.height * .5f - 9));
            if (portrait) portrait.gameObject.SetActive(friend);
            var tail = Decoration(panel, "SpeechTail", speechTail, new Vector2(72, 42), new Vector2(-panel.rect.width * .21f, -panel.rect.height * .5f - 12));
            if (tail) tail.transform.SetSiblingIndex(0);
            if(tail)speechTailRect=tail.rectTransform;
            view.dialogueText.rectTransform.sizeDelta = new Vector2(panel.rect.width - 104, panel.rect.height - 80);
            view.dialogueText.rectTransform.anchoredPosition = new Vector2(0, 0);
            var paper = panel.Find("ReadingPaper") as RectTransform;
            if (!paper)
            {
                paper = (RectTransform)new GameObject("ReadingPaper", typeof(RectTransform)).transform;
                paper.SetParent(panel, false);
                var fill = paper.gameObject.AddComponent<UnityEngine.UI.Image>();
                fill.color = new Color(1,.975f,.89f,1);fill.raycastTarget=false;
            }
            paper.anchorMin=paper.anchorMax=paper.pivot=Vector2.one*.5f;
            paper.sizeDelta=view.dialogueText.rectTransform.sizeDelta+new Vector2(24,14);
            paper.anchoredPosition=Vector2.zero;
            int bodyIndex=view.dialogueText.transform.GetSiblingIndex();
            paper.SetSiblingIndex(Mathf.Max(0,bodyIndex-(paper.GetSiblingIndex()<bodyIndex?1:0)));
            paper.localPosition=new Vector3(paper.localPosition.x,paper.localPosition.y,.1f);
            foreach (var button in view.ChoiceButtons)
            {
                if (!button) continue;
                var label = button.GetComponentInChildren<TMP_Text>();
                var r = (RectTransform)button.transform;
                label.rectTransform.sizeDelta = r.sizeDelta - (r.rect.width < 300 ? new Vector2(38, 18) : new Vector2(64, 26));
                label.rectTransform.anchoredPosition = Vector2.zero;
                var feedback = button.GetComponent<ElevatorSmoothButtonFeedback>();
                if (feedback) feedback.CaptureRest();
            }
            spatial.speakerTab.SetAsLastSibling();
            if (portrait) portrait.transform.SetAsLastSibling();
        }

        public void PointSpeechTail(Vector3 source)
        {
            if(!speechTailRect||!view||!view.dialoguePanel)return;
            var panel=view.dialoguePanel;var local=panel.InverseTransformPoint(source);
            float x=local.x/Mathf.Max(1,panel.rect.width*.5f),y=local.y/Mathf.Max(1,panel.rect.height*.5f);
            bool horizontal=Mathf.Abs(x)>Mathf.Abs(y);
            if(horizontal)
            {
                speechTailRect.anchoredPosition=new Vector2(Mathf.Sign(x)*(panel.rect.width*.5f+10),Mathf.Clamp(local.y,-panel.rect.height*.30f,panel.rect.height*.30f));
                speechTailRect.localRotation=Quaternion.Euler(0,0,x>0?90:-90);
            }
            else
            {
                speechTailRect.anchoredPosition=new Vector2(Mathf.Clamp(local.x,-panel.rect.width*.35f,panel.rect.width*.35f),Mathf.Sign(y)*(panel.rect.height*.5f+10));
                speechTailRect.localRotation=Quaternion.Euler(0,0,y>0?180:0);
            }
        }

        public void LayoutThought(bool observation)
        {
            if(view)ElevatorThoughtHeaderPolicy.Hide(view.thoughtPanel);
            if (thoughtBubblePrefab && LayoutThoughtPrefab(observation)) return;
            if (!view || !view.spatial || !thoughtCloud) return;
            var panel = view.thoughtPanel;
            var spatial = view.spatial;
            float width = observation ? 320 : 360;
            if(UnityEngine.XR.XRSettings.isDeviceActive)width=Mathf.Min(width,VRUIReadingLayout.StatusWidth);
            float textWidth = width * (observation ? .76f : .68f);
            Readable(view.thought, Ink, ElevatorTypography.ThoughtSize, true);
            float body = Mathf.Max(56, view.thought.GetPreferredValues(view.thought.text, textWidth, 0).y + 10);
            float height = observation ? Mathf.Max(178, body + 100) : Mathf.Max(254, (body + 82) / .69f);
            panel.sizeDelta = new Vector2(width, height);
            Artwork(panel, observation ? observationNote : thoughtCloud, observation);
            view.thought.rectTransform.sizeDelta = new Vector2(textWidth, body);
            view.thought.rectTransform.anchoredPosition = new Vector2(0, observation ? -22 : 0);
            view.thought.alignment = observation ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            ElevatorThoughtHeaderPolicy.Hide(panel);
        }

        bool LayoutThoughtPrefab(bool observation)
        {
            if (!view || !view.spatial || !view.thoughtPanel) return false;
            var panel = view.thoughtPanel;
            if (!thoughtBubble)
            {
                // Reuse the current prefab's artwork and body typography, never its header.
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
            }
            // Enforce this on every content change, not just first instantiation.
            // Legacy Heading/ThoughtRibbon and any outgoing heading copy must stay gone.
            foreach (Transform child in panel)
                if(child!=thoughtBubble)child.gameObject.SetActive(false);
            var legacy=panel.GetComponent<CorridorBubbleGraphic>();if(legacy)legacy.enabled=false;
            ElevatorThoughtHeaderPolicy.Hide(panel);
            float width = panel.rect.width;
            float scale = width / 520f;
            // The shared prefab is scaled down in the HUD. Compensate locally so
            // its visible type is 27pt, without editing the shared font or prefab.
            bubbleThought.font = view.font;
            Readable(bubbleThought, Ink, ElevatorTypography.ThoughtSize / scale, true);
            bubbleThought.text = string.Join("\n", ElevatorDialogueText.Pages(bubbleThought, view.thought.text, 364));
            bubbleThought.maxVisibleCharacters = int.MaxValue;
            float body = Mathf.Max(110, bubbleThought.GetPreferredValues(bubbleThought.text, 364, 0).y + 12);
            float height = Mathf.Max(300, body + 180);
            thoughtBubble.anchorMin = thoughtBubble.anchorMax = thoughtBubble.pivot = Vector2.one * .5f;
            thoughtBubble.anchoredPosition = Vector2.zero;
            thoughtBubble.sizeDelta = new Vector2(520, height);
            thoughtBubble.localScale = Vector3.one * scale;
            panel.sizeDelta = new Vector2(width, height * scale);
            bubbleThought.rectTransform.sizeDelta = new Vector2(364, body);
            bubbleThought.rectTransform.anchoredPosition = new Vector2(0, 2);
            ElevatorThoughtHeaderPolicy.Hide(panel);
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
            foreach (var button in pause.Buttons)
                if (button) Readable(button.GetComponentInChildren<TMP_Text>(true), button == pause.resumeButton ? Paper : Ink, ElevatorTypography.PauseButtonSize, true);
            foreach (var text in pause.canvas.GetComponentsInChildren<TMP_Text>(true))
                if (!text.GetComponentInParent<UnityEngine.UI.Button>(true))
                {
                    Readable(text, Ink, 40, true);
                    text.rectTransform.sizeDelta = new Vector2(470, 66);
                }
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
