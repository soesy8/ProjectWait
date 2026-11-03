using TMPro;

namespace ProjectWait.M.Elevator
{
    // Sizes are canvas points, not world metres. Layout grows around the text;
    // auto-shrinking would undo the readability improvement, especially in XR.
    public static class ElevatorTypography
    {
        public const float ChoiceSize = 34;
        public const float NextSize = 32;
        public const float NotebookButtonSize = 27;
        public const float PauseButtonSize = 32;
        public const float SpeakerSize = 28;
        public const float ThoughtSize = 27;
        public const float HeadingSize = 20;
        public const float ObjectiveSize = 26;
        public const float HintSize = 22;
        public const float ControlsSize = 20;
        public const float CaptionSize = 28;

        public static void Apply(TMP_Text text, float size, bool bold = true)
        {
            if (!text) return;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.fontWeight = bold ? FontWeight.Bold : FontWeight.Regular;
            text.enableAutoSizing = false;
            text.extraPadding = true;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }
    }
}