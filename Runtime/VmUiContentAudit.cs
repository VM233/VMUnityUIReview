using System;

namespace VMUnityAutomation.Runtime
{
    public static class VmUiContentAudit
    {
        public const int MaximumTextLength = 65536;
        private static readonly string[] vagueReasons = { "放不稳", "条件不足", "不满足条件", "原因不明", "无法进行此操作",
            "cannot place securely", "requirements not met", "conditions not met", "unknown reason", "cannot do that" };

        public static string ReasonIssue(string text)
        {
            RequireText(text);
            foreach (string vague in vagueReasons)
                if (text.IndexOf(vague, StringComparison.OrdinalIgnoreCase) >= 0) return "vague-rejection-reason";
            return null;
        }

        public static bool IsFlatBlock(string text)
        {
            RequireText(text);
            int lines = 0, listLines = 0;
            int start = 0;
            for (int index = 0; index <= text.Length; index++)
            {
                if (index < text.Length && text[index] != '\n') continue;
                int first = start;
                while (first < index && char.IsWhiteSpace(text[first])) first++;
                if (first < index)
                {
                    bool heading = text.IndexOf("<b>", first, index - first, StringComparison.OrdinalIgnoreCase) == first ||
                        text.IndexOf("<size=", first, index - first, StringComparison.OrdinalIgnoreCase) == first;
                    if (heading)
                    {
                        if (lines >= 5 && listLines < lines - 1) return true;
                        lines = listLines = 0;
                        start = index + 1;
                        continue;
                    }
                    lines++;
                    char prefix = text[first];
                    if (prefix == '-' || prefix == '\u2022' || prefix >= '0' && prefix <= '9' &&
                        first + 1 < index && (text[first + 1] == '.' || text[first + 1] == ')')) listLines++;
                }
                start = index + 1;
            }
            return lines >= 5 && listLines < lines - 1;
        }

        private static void RequireText(string text)
        {
            if (text == null || text.Length > MaximumTextLength)
                throw new ArgumentException("Content review requires a non-null text of at most65536 UTF-16 units.");
        }
    }
}
