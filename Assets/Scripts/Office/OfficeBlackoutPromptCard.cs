using TMPro;
using System;
using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// Displays the interaction prompt and hides its card when the prompt is empty.
    /// Place this on an always-active object, not on the card it controls.
    /// </summary>
    public sealed class OfficeBlackoutPromptCard : MonoBehaviour
    {
        [Serializable]
        public sealed class DialoguePromptBinding
        {
            [Min(1)] public int dialogueIndex = 1;
            [Min(1)] public int promptIndex = 1;
        }

        [SerializeField] GameObject cardRoot;
        [SerializeField] TMP_Text promptText;
        [Header("Prewritten prompts (indices start at 1)")]
        [SerializeField, TextArea(1, 3)] string[] presetPrompts = Array.Empty<string>();
        [SerializeField] DialoguePromptBinding[] promptsAfterDialogues = Array.Empty<DialoguePromptBinding>();

        int displayedPresetIndex = -1;

        public bool IsShowingPresetPrompt(int index) =>
            index > 0 && displayedPresetIndex == index && cardRoot && cardRoot.activeSelf;

        void Awake()
        {
            if (promptText)
                promptText.text = string.Empty;

            if (cardRoot)
                cardRoot.SetActive(false);
        }

        public void ShowPrompt(string value)
        {
            var presentation = OfficeSceneIntegration.Current ? OfficeSceneIntegration.Current.presentation : null;
            if (presentation && presentation.ActionThoughtVisible) presentation.ClearLine();
            displayedPresetIndex = -1;
            bool visible = !string.IsNullOrWhiteSpace(value);

            if (promptText)
                promptText.text = visible ? value : string.Empty;

            if (cardRoot && cardRoot.activeSelf != visible)
                cardRoot.SetActive(visible);
            if (visible && OfficeSceneIntegration.Current) OfficeSceneIntegration.Current.presentation.PromptChanged();
        }

        /// <summary>Displays a prewritten prompt using its one-based Inspector index.</summary>
        public void ShowPresetPrompt(int promptIndex)
        {
            if (presetPrompts == null || promptIndex < 1 || promptIndex > presetPrompts.Length)
            {
                Debug.LogWarning($"Prompt index {promptIndex} is not configured on {name}.", this);
                HidePrompt();
                return;
            }

            ShowPrompt(presetPrompts[promptIndex - 1]);
            if (!string.IsNullOrWhiteSpace(presetPrompts[promptIndex - 1]))
                displayedPresetIndex = promptIndex;
        }

        /// <summary>Shows the configured prompt when the matching dialogue completes.</summary>
        public void OnDialogueCompleted(int dialogueIndex)
        {
            if (promptsAfterDialogues == null) return;
            foreach (DialoguePromptBinding binding in promptsAfterDialogues)
            {
                if (binding != null && binding.dialogueIndex == dialogueIndex)
                {
                    ShowPresetPrompt(binding.promptIndex);
                    return;
                }
            }
        }

        public void HidePrompt() => ShowPrompt(string.Empty);
    }
}
