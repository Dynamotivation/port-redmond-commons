using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Redmond.Shortcuts;

/// <summary>
/// Localized modifier and key labels, such as <c>Strg</c> and <c>Entf</c> on German
/// Windows or <c>⌘</c> on macOS.
/// </summary>
/// <remarks>
/// The labels come from the embedded <c>ShortcutLabels.json</c>. Windows and Linux
/// read a language's <c>text</c> table plus its <c>windows</c> or <c>linux</c>
/// additions; macOS reads only the <c>macOS</c> table. Each label falls back from the
/// full culture name (<c>de-CH</c>) to the two-letter language (<c>de</c>) to English.
/// </remarks>
public static class ShortcutLabels
{
    private const string ResourceName = "Redmond.Shortcuts.ShortcutLabels.json";

    private static readonly Lazy<Dictionary<string, LanguageDocument>> Languages = new(Load);
    private static readonly ConcurrentDictionary<(ShortcutPlatform, string), LabelSet> Cache = new();

    /// <summary>The languages that have a label table, for example <c>en</c> and <c>de</c>.</summary>
    public static IReadOnlyCollection<string> AvailableLanguages => Languages.Value.Keys;

    /// <summary>
    /// The culture whose labels match the user's keyboard: the active keyboard layout's
    /// language on Windows, otherwise <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public static CultureInfo DetectCulture()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var languageId = (int)(GetKeyboardLayout(0) & 0xFFFF);
                if (languageId != 0)
                {
                    return CultureInfo.GetCultureInfo(languageId);
                }
            }
            catch (CultureNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
            catch (DllNotFoundException)
            {
            }
        }

        return CultureInfo.CurrentUICulture;
    }

    public static IReadOnlyDictionary<ShortcutModifiers, string> GetModifierNames(
        ShortcutPlatform platform,
        CultureInfo? culture = null) =>
        Get(platform, culture).Modifiers;

    public static IReadOnlyDictionary<string, string> GetKeyNames(
        ShortcutPlatform platform,
        CultureInfo? culture = null) =>
        Get(platform, culture).Keys;

    private static LabelSet Get(ShortcutPlatform platform, CultureInfo? culture)
    {
        culture ??= DetectCulture();
        return Cache.GetOrAdd((platform, culture.Name), key => Build(key.Item1, culture));
    }

    private static LabelSet Build(ShortcutPlatform platform, CultureInfo culture)
    {
        var modifiers = new Dictionary<ShortcutModifiers, string>();
        var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var digit = 0; digit <= 9; digit++)
        {
            keys[$"D{digit}"] = digit.ToString(CultureInfo.InvariantCulture);
            keys[$"NUMPAD{digit}"] = platform == ShortcutPlatform.MacOS ? digit.ToString(CultureInfo.InvariantCulture) : $"Num {digit}";
        }

        foreach (var name in new[] { "en", culture.TwoLetterISOLanguageName, culture.Name }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Languages.Value.TryGetValue(name, out var language))
            {
                continue;
            }

            var tables = platform switch
            {
                ShortcutPlatform.MacOS => new[] { language.MacOS },
                ShortcutPlatform.Linux => [language.Text, language.Linux],
                _ => [language.Text, language.Windows],
            };
            foreach (var table in tables.OfType<TableDocument>())
            {
                foreach (var pair in table.Modifiers ?? [])
                {
                    modifiers[ParseModifier(pair.Key)] = pair.Value;
                }

                foreach (var pair in table.Keys ?? [])
                {
                    keys[pair.Key] = pair.Value;
                }
            }
        }

        return new LabelSet(
            new ReadOnlyDictionary<ShortcutModifiers, string>(modifiers),
            new ReadOnlyDictionary<string, string>(keys));
    }

    private static ShortcutModifiers ParseModifier(string name) =>
        Enum.TryParse<ShortcutModifiers>(name, ignoreCase: false, out var modifier) &&
        modifier is not (ShortcutModifiers.None or ShortcutModifiers.Primary) &&
        Enum.IsDefined(modifier)
            ? modifier
            : throw new InvalidOperationException($"ShortcutLabels.json names an unknown modifier '{name}'.");

    private static Dictionary<string, LanguageDocument> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName) ??
            throw new InvalidOperationException("The embedded shortcut label table is missing.");
        using var document = JsonDocument.Parse(stream);
        var languages = new Dictionary<string, LanguageDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                languages[property.Name] = property.Value.Deserialize<LanguageDocument>(JsonOptions)!;
            }
        }

        return languages;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint threadId);

    private sealed record LabelSet(
        IReadOnlyDictionary<ShortcutModifiers, string> Modifiers,
        IReadOnlyDictionary<string, string> Keys);

    private sealed class LanguageDocument
    {
        public TableDocument? Text { get; set; }
        public TableDocument? Windows { get; set; }
        public TableDocument? Linux { get; set; }
        public TableDocument? MacOS { get; set; }
    }

    private sealed class TableDocument
    {
        public Dictionary<string, string>? Modifiers { get; set; }
        public Dictionary<string, string>? Keys { get; set; }
    }
}
