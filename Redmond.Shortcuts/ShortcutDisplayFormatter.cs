using System.Collections.ObjectModel;

namespace Redmond.Shortcuts;

public enum ShortcutDisplayStyle
{
    Automatic,
    Text,
    Symbols,
}

public sealed class ShortcutDisplayOptions
{
    public ShortcutDisplayOptions(
        ShortcutDisplayStyle style = ShortcutDisplayStyle.Automatic,
        string separator = "+",
        bool concatenateSymbols = false,
        IReadOnlyDictionary<ShortcutModifiers, string>? modifierNames = null,
        IReadOnlyDictionary<string, string>? namedKeyNames = null)
    {
        ArgumentNullException.ThrowIfNull(separator);

        Style = style;
        Separator = separator;
        ConcatenateSymbols = concatenateSymbols;
        ModifierNames = new ReadOnlyDictionary<ShortcutModifiers, string>(
            modifierNames is null
                ? new Dictionary<ShortcutModifiers, string>()
                : new Dictionary<ShortcutModifiers, string>(modifierNames));
        NamedKeyNames = new ReadOnlyDictionary<string, string>(
            namedKeyNames is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(namedKeyNames, StringComparer.OrdinalIgnoreCase));
    }

    public ShortcutDisplayStyle Style { get; }

    public string Separator { get; }

    public bool ConcatenateSymbols { get; }

    public IReadOnlyDictionary<ShortcutModifiers, string> ModifierNames { get; }

    public IReadOnlyDictionary<string, string> NamedKeyNames { get; }

    /// <summary>
    /// The platform's display style with labels in <paramref name="culture"/>
    /// (default: <see cref="ShortcutLabels.DetectCulture"/>), for example
    /// <c>Strg+Umschalt+S</c> on German Windows and <c>⇧⌘S</c> on macOS.
    /// </summary>
    public static ShortcutDisplayOptions ForPlatform(
        ShortcutPlatform platform,
        System.Globalization.CultureInfo? culture = null) =>
        platform == ShortcutPlatform.MacOS
            ? new ShortcutDisplayOptions(
                ShortcutDisplayStyle.Symbols,
                separator: string.Empty,
                concatenateSymbols: true,
                modifierNames: ShortcutLabels.GetModifierNames(platform, culture),
                namedKeyNames: ShortcutLabels.GetKeyNames(platform, culture))
            : new ShortcutDisplayOptions(
                ShortcutDisplayStyle.Text,
                modifierNames: ShortcutLabels.GetModifierNames(platform, culture),
                namedKeyNames: ShortcutLabels.GetKeyNames(platform, culture));
}

public sealed class ShortcutDisplayFormatter
{
    private static readonly ShortcutModifiers[] ModifierOrder =
    [
        ShortcutModifiers.Control,
        ShortcutModifiers.Alt,
        ShortcutModifiers.Shift,
        ShortcutModifiers.Command,
        ShortcutModifiers.Super,
    ];

    private readonly ShortcutDisplayOptions _options;

    public ShortcutDisplayFormatter(ShortcutDisplayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Formats a physical gesture. Resolve <see cref="ShortcutModifiers.Primary"/> first
    /// (<see cref="IShortcutService.FormatGesture"/> does); an unresolved Primary is
    /// shown with the Control label.
    /// </summary>
    public string Format(ShortcutGesture gesture)
    {
        if (gesture.Modifiers.HasFlag(ShortcutModifiers.Primary))
        {
            gesture = new ShortcutGesture(
                gesture.Key,
                (gesture.Modifiers & ~ShortcutModifiers.Primary) | ShortcutModifiers.Control);
        }

        var parts = new List<string>();
        foreach (var modifier in ModifierOrder)
        {
            if (gesture.Modifiers.HasFlag(modifier))
            {
                parts.Add(GetModifierName(modifier));
            }
        }

        parts.Add(GetKeyName(gesture.Key));
        var separator = _options.Style == ShortcutDisplayStyle.Symbols && _options.ConcatenateSymbols
            ? string.Empty
            : _options.Separator;
        return string.Join(separator, parts);
    }

    private string GetModifierName(ShortcutModifiers modifier) =>
        _options.ModifierNames.TryGetValue(modifier, out var name)
            ? name
            : modifier.ToString();

    private string GetKeyName(ShortcutKey key) =>
        key.Kind == ShortcutKeyKind.Named && _options.NamedKeyNames.TryGetValue(key.Value, out var name)
            ? name
            : key.Value;
}
