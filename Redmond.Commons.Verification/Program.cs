using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Redmond.Avalonia.Controls;
using Redmond.Shortcuts;

var english = CultureInfo.GetCultureInfo("en-US");
var german = CultureInfo.GetCultureInfo("de-DE");

var tests = new (string Name, Action Run)[]
{
    ("Primary resolves per platform and setting", PrimaryResolvesPerPlatform),
    ("Explicit Control means Control everywhere", ExplicitControlIsUnchanged),
    ("Neutral gestures parse and round-trip", GesturesParse),
    ("Keys missing on a Mac keyboard are dropped there", UnavailableKeysAreDropped),
    ("The service switches the macOS modifier live", ServiceSwitchesLive),
    ("English labels", EnglishLabels),
    ("German labels", GermanLabels),
    ("Labels fall back to the language, then English", LabelsFallBack),
    ("Validation sees the resolved modifier", ValidationUsesResolvedModifier),
    ("Reserved macOS shortcuts are flagged", ReservedMacShortcutsAreFlagged),
    ("Menu items show the shared label", MenuItemsShowSharedLabel),
};

foreach (var (name, run) in tests)
{
    run();
    Console.WriteLine($"PASS: {name}");
}

Console.WriteLine($"\nAll {tests.Length} Commons checks passed.");
return 0;

void PrimaryResolvesPerPlatform()
{
    var save = ShortcutGesture.Parse("Primary+Shift+S");
    Equal(ShortcutModifiers.Control | ShortcutModifiers.Shift,
        ShortcutPrimaryModifier.Resolve(save, ShortcutPlatform.Windows).Modifiers, "Windows");
    Equal(ShortcutModifiers.Control | ShortcutModifiers.Shift,
        ShortcutPrimaryModifier.Resolve(save, ShortcutPlatform.Linux).Modifiers, "Linux");
    Equal(ShortcutModifiers.Command | ShortcutModifiers.Shift,
        ShortcutPrimaryModifier.Resolve(save, ShortcutPlatform.MacOS).Modifiers, "macOS default");
    Equal(ShortcutModifiers.Control | ShortcutModifiers.Shift,
        ShortcutPrimaryModifier.Resolve(save, ShortcutPlatform.MacOS, MacPrimaryModifier.Control).Modifiers,
        "macOS with Control");
    Equal(ShortcutModifiers.Control,
        ShortcutPrimaryModifier.Resolve(ShortcutPlatform.Windows, MacPrimaryModifier.Command),
        "setting ignored on Windows");
    Throws<ArgumentOutOfRangeException>(
        () => ShortcutPrimaryModifier.Resolve(ShortcutPlatform.MacOS, (MacPrimaryModifier)7));
}

void ExplicitControlIsUnchanged()
{
    var gesture = ShortcutGesture.Parse("Ctrl+M");
    Equal(ShortcutModifiers.Control,
        ShortcutPrimaryModifier.Resolve(gesture, ShortcutPlatform.MacOS).Modifiers, "Ctrl+M");
    var f3 = ShortcutGesture.Parse("Shift+F3");
    Equal(f3, ShortcutPrimaryModifier.Resolve(f3, ShortcutPlatform.MacOS), "Shift+F3");
}

void GesturesParse()
{
    var gesture = ShortcutGesture.Parse("primary + option + s");
    Equal(ShortcutModifiers.Primary | ShortcutModifiers.Alt, gesture.Modifiers, "modifiers");
    Equal("S", gesture.Key.Value, "key");
    Equal("Primary+Alt+S", gesture.ToString(), "canonical text");
    Equal(gesture, ShortcutGesture.Parse(gesture.ToString()), "round trip");
    Equal(ShortcutModifiers.None, ShortcutGesture.Parse("F3").Modifiers, "bare key");
    Throws<FormatException>(() => ShortcutGesture.Parse("Hyper+S"));
    Throws<FormatException>(() => ShortcutGesture.Parse("Primary++"));
}

void UnavailableKeysAreDropped()
{
    var copy = new ShortcutDefinition(
        "copy",
        ShortcutGesture.Parse("Primary+C"),
        alternateGestures: [ShortcutGesture.Parse("Primary+Insert")]);
    Equal(2, copy.ResolveGestures(ShortcutPlatform.Windows).Count, "Windows keeps Insert");
    Equal(1, copy.ResolveGestures(ShortcutPlatform.MacOS).Count, "macOS drops Insert");
    var insertOnly = new ShortcutDefinition("paste-alt", ShortcutGesture.Parse("Shift+Insert"));
    Equal(0, insertOnly.ResolveGestures(ShortcutPlatform.MacOS).Count, "Insert-only shortcut");
    Throws<InvalidOperationException>(() => insertOnly.ResolveGesture(ShortcutPlatform.MacOS));
    True(new ShortcutConventionValidator().Validate([insertOnly], ShortcutPlatform.MacOS).Any(issue =>
            issue.Kind == ShortcutConventionIssueKind.UnavailableKey
            && issue.Severity == ShortcutConventionSeverity.Information),
        "validation reports the left-out gesture");
    True(!new ShortcutConventionValidator().Validate([insertOnly], ShortcutPlatform.Windows)
            .Any(issue => issue.Kind == ShortcutConventionIssueKind.UnavailableKey),
        "Windows has an Insert key");
}

void ServiceSwitchesLive()
{
    var service = new ShortcutService(
        ShortcutPlatform.MacOS,
        new ShortcutServiceOptions { LabelCulture = english });
    service.Register(new ShortcutDefinition("save", ShortcutGesture.Parse("Primary+S")));
    var changes = 0;
    service.GesturesChanged += (_, _) => changes++;
    Equal("⌘S", service.GetGestureDisplayText("save"), "Command label");
    True(service.IsMatch("save", Input("S", ShortcutModifiers.Command)), "Command matches");

    service.MacPrimaryModifier = MacPrimaryModifier.Control;
    service.MacPrimaryModifier = MacPrimaryModifier.Control;
    Equal(1, changes, "one change event");
    Equal("⌃S", service.GetGestureDisplayText("save"), "Control label");
    True(service.IsMatch("save", Input("S", ShortcutModifiers.Control)), "Control matches");
    True(!service.IsMatch("save", Input("S", ShortcutModifiers.Command)), "Command no longer matches");
    Equal(ShortcutModifiers.Control, service.PrimaryModifier, "resolved primary");

    var windows = new ShortcutService(
        ShortcutPlatform.Windows,
        new ShortcutServiceOptions { MacPrimaryModifier = MacPrimaryModifier.Command, LabelCulture = english });
    windows.Register(new ShortcutDefinition("save", ShortcutGesture.Parse("Primary+S")));
    Equal("Ctrl+S", windows.GetGestureDisplayText("save"), "Windows ignores the macOS setting");
    Equal("Ctrl+Shift+N", windows.FormatGesture(ShortcutGesture.Parse("Primary+Shift+N")), "FormatGesture resolves");
}

void EnglishLabels()
{
    Equal("Ctrl+Shift+S", Format(ShortcutPlatform.Windows, english, "Primary+Shift+S"), "Windows");
    Equal("Ctrl+Alt+S", Format(ShortcutPlatform.Linux, english, "Primary+Alt+S"), "Linux");
    Equal("⌥⇧⌘S", Format(ShortcutPlatform.MacOS, english, "Primary+Alt+Shift+S"), "macOS");
    Equal("Delete", Format(ShortcutPlatform.Windows, english, "Delete"), "Delete");
    Equal("Ctrl+Plus", Format(ShortcutPlatform.Windows, english, "Primary+OemPlus"), "Plus");
    Equal("Ctrl+0", Format(ShortcutPlatform.Windows, english, "Primary+D0"), "digit");
    Equal("⌘0", Format(ShortcutPlatform.MacOS, english, "Primary+D0"), "macOS digit");
    Equal("Win+E", Format(ShortcutPlatform.Windows, english, "Super+E"), "Windows key");
    Equal("Super+E", Format(ShortcutPlatform.Linux, english, "Super+E"), "Super key");
}

void GermanLabels()
{
    Equal("Strg+Umschalt+S", Format(ShortcutPlatform.Windows, german, "Primary+Shift+S"), "Windows");
    Equal("Strg+Alt+S", Format(ShortcutPlatform.Linux, german, "Primary+Alt+S"), "Linux");
    Equal("Entf", Format(ShortcutPlatform.Windows, german, "Delete"), "Delete");
    Equal("Umschalt+Einfg", Format(ShortcutPlatform.Windows, german, "Shift+Insert"), "Insert");
    Equal("Strg+Pos1", Format(ShortcutPlatform.Windows, german, "Primary+Home"), "Home");
    Equal("⇧⌘S", Format(ShortcutPlatform.MacOS, german, "Primary+Shift+S"), "macOS symbols");
    Equal("⌦", Format(ShortcutPlatform.MacOS, german, "Delete"), "macOS keeps its symbol");
}

void LabelsFallBack()
{
    True(ShortcutLabels.AvailableLanguages.Contains("en") && ShortcutLabels.AvailableLanguages.Contains("de"),
        "en and de tables");
    Equal("Strg+S", Format(ShortcutPlatform.Windows, CultureInfo.GetCultureInfo("de-CH"), "Primary+S"), "de-CH → de");
    Equal("Strg+F3", Format(ShortcutPlatform.Windows, CultureInfo.GetCultureInfo("de-AT"), "Ctrl+F3"), "de-AT → de");
    Equal("Ctrl+Esc", Format(ShortcutPlatform.Windows, CultureInfo.GetCultureInfo("ja-JP"), "Ctrl+Escape"), "ja → en");
    Equal("Strg+Esc", Format(ShortcutPlatform.Windows, german, "Ctrl+Escape"), "missing German key uses English");
    _ = ShortcutLabels.DetectCulture();
}

void ValidationUsesResolvedModifier()
{
    var definitions = new[] { new ShortcutDefinition("open", ShortcutGesture.Parse("Primary+O")) };
    var validator = new ShortcutConventionValidator();
    True(!validator.Validate(definitions, ShortcutPlatform.MacOS)
        .Any(issue => issue.Kind == ShortcutConventionIssueKind.NonIdiomaticModifier), "Command is idiomatic");
    True(validator.Validate(definitions, ShortcutPlatform.MacOS, MacPrimaryModifier.Control)
        .Any(issue => issue.Kind == ShortcutConventionIssueKind.NonIdiomaticModifier), "Control is flagged");
}

void ReservedMacShortcutsAreFlagged()
{
    IReadOnlyList<ShortcutConventionIssue> Collisions(
        string gesture,
        MacPrimaryModifier modifier = MacPrimaryModifier.Command) =>
        new ShortcutConventionValidator()
            .Validate([new ShortcutDefinition("probe", ShortcutGesture.Parse(gesture), scope: "probe")],
                ShortcutPlatform.MacOS,
                modifier)
            .Where(issue => issue.IsCollision)
            .ToArray();

    foreach (var gesture in new[] { "Primary+H", "Primary+M", "Primary+Q", "Primary+Alt+H", "Primary+Space",
                 "Primary+Shift+D3", "Primary+Tab", "Primary+Ctrl+F", "Alt+Up" })
    {
        True(Collisions(gesture).Any(issue => issue.Severity == ShortcutConventionSeverity.Error),
            $"{gesture} collides on macOS");
    }

    True(Collisions("Primary+Space", MacPrimaryModifier.Control).Any(), "Control+Space selects the input source");
    True(Collisions("Primary+Up", MacPrimaryModifier.Control).Any(), "Control+Up opens Mission Control");
    True(!Collisions("Primary+Up").Any(), "Command+Up is free");

    foreach (var gesture in new[] { "Primary+N", "Primary+W", "Primary+OemComma" })
    {
        var issues = Collisions(gesture);
        True(issues.Count == 1 && issues[0].Severity == ShortcutConventionSeverity.Warning,
            $"{gesture} is a conventional command, reported as a warning");
    }

    foreach (var gesture in new[] { "Primary+Shift+H", "Primary+Shift+M", "Primary+Shift+P", "Primary+Alt+Up",
                 "Primary+S", "Primary+D0", "F3" })
    {
        True(!Collisions(gesture).Any(), $"{gesture} is free on macOS");
        True(!Collisions(gesture, MacPrimaryModifier.Control).Any(), $"{gesture} is free with Control");
    }

    var character = new ShortcutDefinition("probe", new ShortcutGesture(ShortcutKey.Character('h'), ShortcutModifiers.Primary));
    True(new ShortcutConventionValidator().Validate([character], ShortcutPlatform.MacOS).Any(issue => issue.IsCollision),
        "character keys are compared by name");
    True(!new ShortcutConventionValidator().Validate(
                [new ShortcutDefinition("probe", ShortcutGesture.Parse("Primary+H"))], ShortcutPlatform.Windows)
            .Any(issue => issue.IsCollision),
        "the macOS list does not apply to Windows");
}

void MenuItemsShowSharedLabel()
{
    using var session = HeadlessUnitTestSession.StartNew(typeof(VerificationApp));
    session.Dispatch(() =>
    {
        var item = new MenuItem
        {
            Header = "Save as",
            InputGesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift),
        };
        var late = new MenuItem { Header = "Open", InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) };
        MenuItemShortcut.SetText(late, "Strg+O");
        var panel = new StackPanel { Children = { item, late } };
        var window = new Window { Content = panel };
        window.Show();
        item.ApplyTemplate();
        late.ApplyTemplate();
        var original = FindGestureText(item)?.Text;
        True(!string.IsNullOrEmpty(original), "the template shows Avalonia's gesture text");
        Equal("Strg+O", FindGestureText(late)?.Text, "label set before the template is applied");

        MenuItemShortcut.SetText(item, "Strg+Umschalt+S");
        Equal("Strg+Umschalt+S", FindGestureText(item)?.Text, "label replaces Avalonia's text");

        MenuItemShortcut.SetText(item, null);
        Equal(original, FindGestureText(item)?.Text, "clearing restores Avalonia's text");
        window.Close();
    }, CancellationToken.None).GetAwaiter().GetResult();
}

static TextBlock? FindGestureText(MenuItem item) =>
    item.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(block => block.Name == "PART_InputGestureText");

static string Format(ShortcutPlatform platform, CultureInfo culture, string gesture) =>
    new ShortcutService(platform, new ShortcutServiceOptions { LabelCulture = culture })
        .FormatGesture(ShortcutGesture.Parse(gesture));

static ShortcutInput Input(string key, ShortcutModifiers modifiers) =>
    new(new ShortcutGesture(ShortcutKey.Named(key), modifiers));

static void Equal<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected '{expected}', got '{actual}'.");
    }
}

static void True(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException(label);
    }
}

static void Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

internal sealed class VerificationApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<VerificationApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
