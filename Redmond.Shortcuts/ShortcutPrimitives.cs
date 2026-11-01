namespace Redmond.Shortcuts;

public enum ShortcutPlatform
{
    Unknown,
    Windows,
    MacOS,
    Linux,
}

[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2,
    Command = 1 << 3,
    Super = 1 << 4,

    /// <summary>
    /// The platform's application-command modifier: Control on Windows and Linux,
    /// Command on macOS unless the user chose Control (<see cref="MacPrimaryModifier"/>).
    /// Shortcut definitions use it instead of naming Control or Command, so one
    /// definition serves every platform. <see cref="ShortcutPrimaryModifier.Resolve(ShortcutGesture, ShortcutPlatform, MacPrimaryModifier)"/>
    /// replaces it with the physical modifier before matching or display.
    /// </summary>
    Primary = 1 << 5,
}

/// <summary>
/// The physical key that <see cref="ShortcutModifiers.Primary"/> stands for on macOS.
/// Windows and Linux always use Control.
/// </summary>
/// <remarks>
/// Applications persist this value. Command stays 0 so a missing or older
/// setting keeps the native macOS default.
/// </remarks>
public enum MacPrimaryModifier
{
    Command = 0,
    Control = 1,
}

public enum ShortcutKeyKind
{
    Named,
    Character,
}

public readonly record struct ShortcutKey
{
    private ShortcutKey(string value, ShortcutKeyKind kind)
    {
        Value = value;
        Kind = kind;
    }

    public string Value { get; }

    public ShortcutKeyKind Kind { get; }

    public static ShortcutKey Named(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ShortcutKey(value.Trim().ToUpperInvariant(), ShortcutKeyKind.Named);
    }

    public static ShortcutKey Character(char value) =>
        new(char.ToUpperInvariant(value).ToString(), ShortcutKeyKind.Character);

    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct ShortcutGesture
{
    public ShortcutGesture(ShortcutKey key, ShortcutModifiers modifiers = ShortcutModifiers.None)
    {
        if (string.IsNullOrWhiteSpace(key.Value))
        {
            throw new ArgumentException("A shortcut gesture must have a key.", nameof(key));
        }

        Key = key;
        Modifiers = modifiers;
    }

    public ShortcutKey Key { get; }

    public ShortcutModifiers Modifiers { get; }

    /// <summary>
    /// Parses a platform-neutral gesture such as <c>Primary+N</c>,
    /// <c>Primary+Shift+S</c> or <c>F3</c>. The last segment is a named key;
    /// the others are <see cref="ShortcutModifiers"/> names (<c>Ctrl</c> and
    /// <c>Option</c> are accepted as aliases for Control and Alt).
    /// </summary>
    public static ShortcutGesture Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Any(string.IsNullOrEmpty))
        {
            throw new FormatException($"The shortcut '{text}' has an empty segment.");
        }

        var modifiers = ShortcutModifiers.None;
        foreach (var part in parts[..^1])
        {
            modifiers |= part.ToUpperInvariant() switch
            {
                "CTRL" => ShortcutModifiers.Control,
                "OPTION" => ShortcutModifiers.Alt,
                _ when Enum.TryParse<ShortcutModifiers>(part, ignoreCase: true, out var modifier) &&
                    modifier != ShortcutModifiers.None &&
                    Enum.IsDefined(modifier) => modifier,
                _ => throw new FormatException($"The shortcut '{text}' has an unknown modifier '{part}'."),
            };
        }

        return new ShortcutGesture(ShortcutKey.Named(parts[^1]), modifiers);
    }

    /// <summary>The neutral form accepted by <see cref="Parse"/>, for example <c>Primary+Shift+S</c>.</summary>
    public override string ToString()
    {
        var parts = new List<string>();
        foreach (var modifier in ShortcutPrimaryModifier.CanonicalOrder)
        {
            if (Modifiers.HasFlag(modifier))
            {
                parts.Add(modifier.ToString());
            }
        }

        parts.Add(Key.Value);
        return string.Join('+', parts);
    }
}

public readonly record struct ShortcutInput(ShortcutGesture Gesture, bool IsRepeat = false);
