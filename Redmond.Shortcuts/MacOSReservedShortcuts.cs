namespace Redmond.Shortcuts;

/// <summary>A physical macOS gesture that the system or the standard app menu already uses.</summary>
/// <param name="Gesture">The physical gesture (Command or Control, never Primary).</param>
/// <param name="Action">What macOS does with it, for diagnostics.</param>
/// <param name="IsConventionalCommand">
/// True for app commands every Mac app is expected to offer (New, Close, Settings…).
/// Binding them is fine for exactly that command, so they are reported as warnings;
/// everything else is an error.
/// </param>
public sealed record MacOSReservedShortcut(ShortcutGesture Gesture, string Action, bool IsConventionalCommand = false);

/// <summary>
/// The macOS system and app-menu shortcuts an application must not take over.
/// <see cref="ShortcutConventionValidator"/> checks every resolved macOS gesture
/// against this list, for both values of <see cref="MacPrimaryModifier"/>.
/// </summary>
/// <remarks>
/// A shortcut that collides here is changed on every platform, so the same key
/// works everywhere. Keys are compared by name, so <c>Named("H")</c> and
/// <c>Character('h')</c>, or <c>D3</c> and <c>3</c>, are the same key.
/// </remarks>
public static class MacOSReservedShortcuts
{
    private const ShortcutModifiers Cmd = ShortcutModifiers.Command;
    private const ShortcutModifiers Ctrl = ShortcutModifiers.Control;
    private const ShortcutModifiers Opt = ShortcutModifiers.Alt;
    private const ShortcutModifiers Shift = ShortcutModifiers.Shift;

    public static IReadOnlyList<MacOSReservedShortcut> All { get; } = Array.AsReadOnly(new MacOSReservedShortcut[]
    {
        // Application menu.
        new(Gesture("H", Cmd), "Hide the application"),
        new(Gesture("H", Opt | Cmd), "Hide other applications"),
        new(Gesture("Q", Cmd), "Quit the application"),
        new(Gesture(",", Cmd), "Open Settings", IsConventionalCommand: true),
        // File and Window menus.
        new(Gesture("N", Cmd), "Create a new document or window", IsConventionalCommand: true),
        new(Gesture("W", Cmd), "Close the window or tab", IsConventionalCommand: true),
        new(Gesture("M", Cmd), "Minimize the window"),
        new(Gesture("M", Opt | Cmd), "Minimize all windows"),
        new(Gesture("F", Ctrl | Cmd), "Toggle full screen"),
        new(Gesture("`", Cmd), "Cycle through the application's windows"),
        new(Gesture("`", Shift | Cmd), "Cycle backwards through the application's windows"),
        new(Gesture("/", Shift | Cmd), "Search the Help menu"),
        // System.
        new(Gesture("SPACE", Cmd), "Spotlight"),
        new(Gesture("SPACE", Opt | Cmd), "Finder search"),
        new(Gesture("SPACE", Ctrl), "Select the previous input source"),
        new(Gesture("SPACE", Ctrl | Opt), "Select the next input source"),
        new(Gesture("SPACE", Ctrl | Cmd), "Character Viewer"),
        new(Gesture("TAB", Cmd), "Switch applications"),
        new(Gesture("TAB", Shift | Cmd), "Switch applications backwards"),
        new(Gesture("ESC", Opt | Cmd), "Force Quit"),
        new(Gesture("Q", Ctrl | Cmd), "Lock the screen"),
        new(Gesture("Q", Shift | Cmd), "Log out"),
        new(Gesture("Q", Opt | Shift | Cmd), "Log out without confirming"),
        new(Gesture("D", Opt | Cmd), "Show or hide the Dock"),
        new(Gesture("D", Ctrl | Cmd), "Look up the selected word"),
        new(Gesture("3", Shift | Cmd), "Screenshot of the screen"),
        new(Gesture("4", Shift | Cmd), "Screenshot of a selection"),
        new(Gesture("5", Shift | Cmd), "Screenshot and recording options"),
        new(Gesture("3", Ctrl | Shift | Cmd), "Copy a screenshot of the screen"),
        new(Gesture("4", Ctrl | Shift | Cmd), "Copy a screenshot of a selection"),
        new(Gesture("UP", Ctrl), "Mission Control"),
        new(Gesture("DOWN", Ctrl), "Application windows"),
        new(Gesture("LEFT", Ctrl), "Move one space left"),
        new(Gesture("RIGHT", Ctrl), "Move one space right"),
        new(Gesture("F2", Ctrl), "Move focus to the menu bar"),
        new(Gesture("F3", Ctrl), "Move focus to the Dock"),
    });

    /// <summary>The reserved entry <paramref name="gesture"/> collides with, if any.</summary>
    public static bool TryFind(ShortcutGesture gesture, out MacOSReservedShortcut? reserved)
    {
        var key = CanonicalKey(gesture.Key);
        reserved = All.FirstOrDefault(item =>
            item.Gesture.Modifiers == gesture.Modifiers && CanonicalKey(item.Gesture.Key) == key);
        return reserved is not null;
    }

    private static ShortcutGesture Gesture(string key, ShortcutModifiers modifiers) =>
        new(ShortcutKey.Named(key), modifiers);

    private static string CanonicalKey(ShortcutKey key)
    {
        var value = key.Value.ToUpperInvariant();
        if (value.Length == 2 && value[0] == 'D' && char.IsAsciiDigit(value[1]))
        {
            return value[1..];
        }

        return value switch
        {
            "OEMCOMMA" => ",",
            "OEMTILDE" or "OEM3" or "GRAVE" => "`",
            "OEMQUESTION" or "OEM2" or "SLASH" => "/",
            "ESCAPE" => "ESC",
            " " => "SPACE",
            _ => value,
        };
    }
}
