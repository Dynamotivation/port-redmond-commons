namespace Redmond.Shortcuts;

/// <summary>
/// One shortcut, defined once for every platform.
/// </summary>
/// <remarks>
/// Gestures are platform-neutral: use <see cref="ShortcutModifiers.Primary"/> for the
/// application-command modifier. There are deliberately no per-platform gesture
/// overrides. A gesture whose key is missing on a platform's keyboard is left out
/// there (see <see cref="ShortcutPrimaryModifier.IsKeyAvailable"/>).
/// </remarks>
public sealed class ShortcutDefinition
{
    public ShortcutDefinition(
        string id,
        ShortcutGesture gesture,
        string? scope = null,
        int priority = 0,
        bool allowRepeat = false,
        Func<bool>? isEnabled = null,
        ShortcutInputHandling inputHandling = ShortcutInputHandling.Consume,
        IReadOnlyList<ShortcutGesture>? alternateGestures = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        Gesture = gesture;
        Scope = string.IsNullOrWhiteSpace(scope) ? null : scope.Trim();
        Priority = priority;
        AllowRepeat = allowRepeat;
        IsEnabled = isEnabled;
        InputHandling = inputHandling;
        AlternateGestures = Array.AsReadOnly(alternateGestures?.ToArray() ?? []);
    }

    public string Id { get; }

    /// <summary>The primary platform-neutral gesture.</summary>
    public ShortcutGesture Gesture { get; }

    public string? Scope { get; }

    public int Priority { get; }

    public bool AllowRepeat { get; }

    public Func<bool>? IsEnabled { get; }

    public ShortcutInputHandling InputHandling { get; }

    /// <summary>Further platform-neutral gestures that trigger the same shortcut.</summary>
    public IReadOnlyList<ShortcutGesture> AlternateGestures { get; }

    /// <summary>
    /// The first physical gesture on <paramref name="platform"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No gesture's key exists on the platform.</exception>
    public ShortcutGesture ResolveGesture(
        ShortcutPlatform platform,
        MacPrimaryModifier macModifier = MacPrimaryModifier.Command)
    {
        var gestures = ResolveGestures(platform, macModifier);
        return gestures.Count > 0
            ? gestures[0]
            : throw new InvalidOperationException($"The shortcut '{Id}' has no key available on {platform}.");
    }

    /// <summary>
    /// Every physical gesture on <paramref name="platform"/>, primary first, without
    /// duplicates. Empty when none of the keys exists on the platform.
    /// </summary>
    public IReadOnlyList<ShortcutGesture> ResolveGestures(
        ShortcutPlatform platform,
        MacPrimaryModifier macModifier = MacPrimaryModifier.Command) =>
        new[] { Gesture }
            .Concat(AlternateGestures)
            .Where(gesture => ShortcutPrimaryModifier.IsKeyAvailable(gesture.Key, platform))
            .Select(gesture => ShortcutPrimaryModifier.Resolve(gesture, platform, macModifier))
            .Distinct()
            .ToArray();

    internal bool IsCurrentlyEnabled() => IsEnabled?.Invoke() ?? true;
}
