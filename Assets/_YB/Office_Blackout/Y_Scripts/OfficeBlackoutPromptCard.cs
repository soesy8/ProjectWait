using TMPro;
using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// Displays the interaction prompt and hides its card when the prompt is empty.
    /// Place this on an always-active object, not on the card it controls.
    /// </summary>
    public sealed class OfficeBlackoutPromptCard : MonoBehaviour
    {
        [SerializeField] GameObject cardRoot;
        [SerializeField] TMP_Text promptText;

        void Awake()
        {
            if (promptText)
                promptText.text = string.Empty;

            if (cardRoot)
                cardRoot.SetActive(false);
        }

        public void ShowPrompt(string value)
        {
            bool visible = !string.IsNullOrWhiteSpace(value);

            if (promptText)
                promptText.text = visible ? value : string.Empty;

            if (cardRoot && cardRoot.activeSelf != visible)
                cardRoot.SetActive(visible);
        }
    }
}
