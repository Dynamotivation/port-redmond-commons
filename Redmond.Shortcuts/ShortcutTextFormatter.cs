namespace Redmond.Shortcuts;

public enum ShortcutTextRewriteMode
{
    TemplateOnly,
    ReplaceTrailingParenthetical,
    ReplaceOrAppend,
}

public sealed class ShortcutTextFormatter
{
    public const string Placeholder = "{shortcut}";

    public const string AlternativesPlaceholder = "{shortcuts}";

    public string FormatTemplate(
        string localizedTemplate,
        string shortcutDisplayText,
        string? allShortcutDisplayText = null)
    {
        ArgumentNullException.ThrowIfNull(localizedTemplate);
        ArgumentNullException.ThrowIfNull(shortcutDisplayText);
        return localizedTemplate
            .Replace(AlternativesPlaceholder, allShortcutDisplayText ?? shortcutDisplayText, StringComparison.Ordinal)
            .Replace(Placeholder, shortcutDisplayText, StringComparison.Ordinal);
    }

    public string Rewrite(
        string localizedText,
        string shortcutDisplayText,
        ShortcutTextRewriteMode mode = ShortcutTextRewriteMode.TemplateOnly,
        string? allShortcutDisplayText = null)
    {
        ArgumentNullException.ThrowIfNull(localizedText);
        ArgumentNullException.ThrowIfNull(shortcutDisplayText);

        if (localizedText.Contains(Placeholder, StringComparison.Ordinal) ||
            localizedText.Contains(AlternativesPlaceholder, StringComparison.Ordinal))
        {
            return FormatTemplate(localizedText, shortcutDisplayText, allShortcutDisplayText);
        }

        if (mode == ShortcutTextRewriteMode.TemplateOnly)
        {
            return localizedText;
        }

        if (TryFindTrailingParenthetical(localizedText, out var start, out var opening))
        {
            var closing = opening == '（' ? '）' : ')';
            return localizedText[..start] + opening + shortcutDisplayText + closing;
        }

        return mode == ShortcutTextRewriteMode.ReplaceOrAppend
            ? $"{localizedText} ({shortcutDisplayText})"
            : localizedText;
    }

    private static bool TryFindTrailingParenthetical(
        string text,
        out int start,
        out char opening)
    {
        var index = text.Length - 1;
        while (index >= 0 && char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        if (index < 0 || text[index] is not (')' or '）'))
        {
            start = -1;
            opening = default;
            return false;
        }

        var depth = 0;
        for (; index >= 0; index--)
        {
            switch (text[index])
            {
                case ')':
                case '）':
                    depth++;
                    break;
                case '(':
                case '（':
                    depth--;
                    if (depth == 0)
                    {
                        start = index;
                        opening = text[index];
                        return true;
                    }
                    break;
            }
        }

        start = -1;
        opening = default;
        return false;
    }
}
