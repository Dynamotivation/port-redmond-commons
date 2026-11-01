namespace Redmond.Shortcuts;

/// <summary>
/// Turns platform-neutral gestures into the physical gestures of one platform.
/// </summary>
/// <remarks>
/// This is the only place that knows which key <see cref="ShortcutModifiers.Primary"/>
/// means. Applications define each shortcut once with Primary and never keep
/// per-platform gesture tables.
/// </remarks>
public static class ShortcutPrimaryModifier
{
    internal static readonly ShortcutModifiers[] CanonicalOrder =
    [
        ShortcutModifiers.Primary,
        ShortcutModifiers.Control,
        ShortcutModifiers.Alt,
        ShortcutModifiers.Shift,
        ShortcutModifiers.Command,
        ShortcutModifiers.Super,
    ];

    /// <summary>
    /// The physical modifier behind Primary: Command on macOS when
    /// <paramref name="macModifier"/> is Command, Control everywhere else.
    /// </summary>
    public static ShortcutModifiers Resolve(
        ShortcutPlatform platform,
        MacPrimaryModifier macModifier = MacPrimaryModifier.Command)
    {
        if (!Enum.IsDefined(macModifier))
        {
            throw new ArgumentOutOfRangeException(nameof(macModifier), macModifier, "Unknown macOS primary modifier.");
        }

        return platform == ShortcutPlatform.MacOS && macModifier == MacPrimaryModifier.Command
            ? ShortcutModifiers.Command
            : ShortcutModifiers.Control;
    }

    /// <summary>
    /// Returns <paramref name="gesture"/> with Primary replaced by the platform's
    /// physical modifier. Gestures without Primary are returned unchanged, so a
    /// gesture that names Control explicitly means Control on every platform.
    /// </summary>
    public static ShortcutGesture Resolve(
        ShortcutGesture gesture,
        ShortcutPlatform platform,
        MacPrimaryModifier macModifier = MacPrimaryModifier.Command)
    {
        var primary = Resolve(platform, macModifier);
        return gesture.Modifiers.HasFlag(ShortcutModifiers.Primary)
            ? new ShortcutGesture(gesture.Key, (gesture.Modifiers & ~ShortcutModifiers.Primary) | primary)
            : gesture;
    }

    /// <summary>
    /// False when the key does not exist on the platform's standard keyboard
    /// (Insert on a Mac). Such gestures are dropped during resolution instead
    /// of being replaced with a platform-specific binding.
    /// </summary>
    public static bool IsKeyAvailable(ShortcutKey key, ShortcutPlatform platform) =>
        !(platform == ShortcutPlatform.MacOS && key.Kind == ShortcutKeyKind.Named && key.Value == "INSERT");
}
