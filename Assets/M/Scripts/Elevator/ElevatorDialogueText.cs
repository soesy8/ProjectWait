using System.Collections.Generic;
using System.Text;
using TMPro;

namespace ProjectWait.M.Elevator
{
    // Wrap whole words first, rather than leaving a Korean ending on the next line.
    public static class ElevatorDialogueText
    {
        public const float BodySize = 35;
        public const float PanelWidth = 580;
        public const float TextWidth = PanelWidth - 104;
        public const int LinesPerPage = 3;

        public static string[] Pages(TMP_Text measure, string value, float width = TextWidth)
        {
            var lines = new List<string>();
            foreach (string paragraph in (value ?? "").Replace("\r", "").Trim().Split('\n'))
            {
                var line = new StringBuilder();
                foreach (string word in paragraph.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && measure.GetPreferredValues(candidate, float.PositiveInfinity, float.PositiveInfinity).x > width)
                    {
                        lines.Add(line.ToString());line.Clear();
                    }
                    // Very long unbroken labels still fit without reducing the font size.
                    if (measure.GetPreferredValues(word, float.PositiveInfinity, float.PositiveInfinity).x > width)
                    {
                        foreach (char character in word)
                        {
                            if (line.Length > 0 && measure.GetPreferredValues(line.ToString() + character, float.PositiveInfinity, float.PositiveInfinity).x > width)
                            {lines.Add(line.ToString());line.Clear();}
                            line.Append(character);
                        }
                    }
                    else
                    {
                        if (line.Length > 0) line.Append(' ');
                        line.Append(word);
                    }
                }
                if (line.Length > 0) lines.Add(line.ToString());
            }
            if (lines.Count == 0) return new[] { "" };
            var pages = new List<string>();
            for (int first = 0; first < lines.Count;)
            {
                int remaining = lines.Count - first;
                int count = remaining == 4 ? 2 : System.Math.Min(LinesPerPage, remaining);
                pages.Add(string.Join("\n", lines.GetRange(first, count)));first += count;
            }
            return pages.ToArray();
        }
    }
}