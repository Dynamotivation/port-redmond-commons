namespace Redmond.Shortcuts;

public enum ShortcutDispatchPolicy
{
    FirstMatch,
    AllMatches,
}

public enum ShortcutInputHandling
{
    ObserveOnly,
    Handle,
    Consume,
}

public sealed class ShortcutServiceOptions
{
    public ShortcutDispatchPolicy DispatchPolicy { get; init; } = ShortcutDispatchPolicy.FirstMatch;

    /// <summary>
    /// Overrides the labels. When null, labels come from
    /// <see cref="ShortcutDisplayOptions.ForPlatform(ShortcutPlatform, System.Globalization.CultureInfo?)"/>
    /// for <see cref="LabelCulture"/>.
    /// </summary>
    public ShortcutDisplayOptions? DisplayOptions { get; init; }

    /// <summary>
    /// The language of modifier and key labels (for example <c>Strg</c> for German).
    /// Defaults to <see cref="ShortcutLabels.DetectCulture"/>.
    /// </summary>
    public System.Globalization.CultureInfo? LabelCulture { get; init; }

    /// <summary>The initial value of <see cref="IShortcutService.MacPrimaryModifier"/>.</summary>
    public MacPrimaryModifier MacPrimaryModifier { get; init; } = MacPrimaryModifier.Command;

    public string AlternativeDisplaySeparator { get; init; } = " / ";
}

public sealed record ShortcutMatch(
    string ShortcutId,
    ShortcutDefinition Definition,
    ShortcutInput Input,
    ShortcutGesture Gesture);

public sealed class ShortcutPressedEventArgs : EventArgs
{
    public ShortcutPressedEventArgs(ShortcutMatch match) => Match = match;

    public ShortcutMatch Match { get; }
}

public sealed record ShortcutConflict(
    ShortcutGesture Gesture,
    string FirstShortcutId,
    string SecondShortcutId,
    string? FirstScope,
    string? SecondScope);

public sealed class ShortcutProcessResult : IReadOnlyList<ShortcutMatch>
{
    private readonly IReadOnlyList<ShortcutMatch> _matches;

    internal ShortcutProcessResult(IReadOnlyList<ShortcutMatch> matches)
    {
        _matches = matches;
        WasMatched = matches.Count > 0;
        Handled = matches.Any(match => match.Definition.InputHandling != ShortcutInputHandling.ObserveOnly);
        ShouldConsume = matches.Any(match => match.Definition.InputHandling == ShortcutInputHandling.Consume);
    }

    public bool WasMatched { get; }

    public bool Handled { get; }

    public bool ShouldConsume { get; }

    public IReadOnlyList<ShortcutMatch> Matches => _matches;

    public int Count => _matches.Count;

    public ShortcutMatch this[int index] => _matches[index];

    public IEnumerator<ShortcutMatch> GetEnumerator() => _matches.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface IShortcutService
{
    event EventHandler<ShortcutPressedEventArgs>? ShortcutPressed;

    /// <summary>
    /// Raised after <see cref="MacPrimaryModifier"/> changes, so menus and tooltips
    /// can refresh their gesture text.
    /// </summary>
    event EventHandler? GesturesChanged;

    ShortcutPlatform Platform { get; }

    /// <summary>
    /// The key <see cref="ShortcutModifiers.Primary"/> means on macOS. Changing it
    /// takes effect immediately for matching and display; registrations stay.
    /// It has no effect on other platforms.
    /// </summary>
    MacPrimaryModifier MacPrimaryModifier { get; set; }

    /// <summary>The physical modifier Primary currently resolves to.</summary>
    ShortcutModifiers PrimaryModifier { get; }

    /// <summary>Formats a physical or neutral gesture with this service's labels.</summary>
    string FormatGesture(ShortcutGesture gesture);

    IDisposable Register(ShortcutDefinition definition, Action<ShortcutMatch>? handler = null);

    bool TryGetDefinition(string shortcutId, out ShortcutDefinition? definition);

    ShortcutGesture GetGesture(string shortcutId);

    string GetGestureDisplayText(string shortcutId);

    IReadOnlyList<string> GetGestureDisplayTexts(string shortcutId);

    bool IsMatch(string shortcutId, ShortcutInput input, IReadOnlySet<string>? activeScopes = null);

    ShortcutProcessResult Process(ShortcutInput input, IReadOnlySet<string>? activeScopes = null);

    IReadOnlyList<ShortcutConflict> GetConflicts();

    IReadOnlyList<ShortcutConventionIssue> ValidateConventions();

    string FormatText(string shortcutId, string localizedTemplate);

    string RewriteText(
        string shortcutId,
        string localizedText,
        ShortcutTextRewriteMode mode = ShortcutTextRewriteMode.TemplateOnly);
}

public sealed class ShortcutService : IShortcutService
{
    private readonly object _gate = new();
    private readonly List<Registration> _registrations = [];
    private readonly ShortcutDispatchPolicy _dispatchPolicy;
    private readonly ShortcutDisplayFormatter _displayFormatter;
    private readonly ShortcutTextFormatter _textFormatter = new();
    private readonly string _alternativeDisplaySeparator;
    private long _nextOrder;
    private volatile int _macPrimaryModifier;

    public ShortcutService(ShortcutPlatform platform, ShortcutServiceOptions? options = null)
    {
        Platform = platform;
        options ??= new ShortcutServiceOptions();
        _dispatchPolicy = options.DispatchPolicy;
        _displayFormatter = new ShortcutDisplayFormatter(
            options.DisplayOptions ?? ShortcutDisplayOptions.ForPlatform(platform, options.LabelCulture));
        MacPrimaryModifier = options.MacPrimaryModifier;
        _alternativeDisplaySeparator = options.AlternativeDisplaySeparator ??
            throw new ArgumentNullException(nameof(options.AlternativeDisplaySeparator));
    }

    public event EventHandler<ShortcutPressedEventArgs>? ShortcutPressed;

    public event EventHandler? GesturesChanged;

    public ShortcutPlatform Platform { get; }

    public MacPrimaryModifier MacPrimaryModifier
    {
        get => (MacPrimaryModifier)_macPrimaryModifier;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown macOS primary modifier.");
            }

            if (Interlocked.Exchange(ref _macPrimaryModifier, (int)value) != (int)value)
            {
                GesturesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ShortcutModifiers PrimaryModifier => ShortcutPrimaryModifier.Resolve(Platform, MacPrimaryModifier);

    public string FormatGesture(ShortcutGesture gesture) =>
        _displayFormatter.Format(ShortcutPrimaryModifier.Resolve(gesture, Platform, MacPrimaryModifier));

    public IDisposable Register(ShortcutDefinition definition, Action<ShortcutMatch>? handler = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var token = Guid.NewGuid();
        lock (_gate)
        {
            if (_registrations.Any(item => string.Equals(item.Definition.Id, definition.Id, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"A shortcut with the id '{definition.Id}' is already registered.");
            }

            _registrations.Add(new Registration(token, definition, handler, _nextOrder++));
        }

        return new RegistrationHandle(this, token);
    }

    public bool TryGetDefinition(string shortcutId, out ShortcutDefinition? definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortcutId);
        lock (_gate)
        {
            definition = _registrations
                .FirstOrDefault(item => string.Equals(item.Definition.Id, shortcutId, StringComparison.Ordinal))
                ?.Definition;
            return definition is not null;
        }
    }

    public ShortcutGesture GetGesture(string shortcutId) =>
        GetRequiredDefinition(shortcutId).ResolveGesture(Platform, MacPrimaryModifier);

    public string GetGestureDisplayText(string shortcutId) => _displayFormatter.Format(GetGesture(shortcutId));

    public IReadOnlyList<string> GetGestureDisplayTexts(string shortcutId) =>
        GetRequiredDefinition(shortcutId)
            .ResolveGestures(Platform, MacPrimaryModifier)
            .Select(_displayFormatter.Format)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public bool IsMatch(string shortcutId, ShortcutInput input, IReadOnlySet<string>? activeScopes = null)
    {
        var definition = GetRequiredDefinition(shortcutId);
        return Matches(definition, input, activeScopes);
    }

    public ShortcutProcessResult Process(ShortcutInput input, IReadOnlySet<string>? activeScopes = null)
    {
        Registration[] snapshot;
        lock (_gate)
        {
            snapshot = [.. _registrations];
        }

        var matching = snapshot
            .Where(item => Matches(item.Definition, input, activeScopes))
            .OrderByDescending(item => item.Definition.Priority)
            .ThenBy(item => item.Order);

        if (_dispatchPolicy == ShortcutDispatchPolicy.FirstMatch)
        {
            matching = matching.Take(1).OrderByDescending(item => item.Definition.Priority).ThenBy(item => item.Order);
        }

        var registrations = matching.ToArray();
        var matches = new List<ShortcutMatch>(registrations.Length);
        foreach (var registration in registrations)
        {
            var match = new ShortcutMatch(
                registration.Definition.Id,
                registration.Definition,
                input,
                input.Gesture);
            matches.Add(match);
            registration.Handler?.Invoke(match);
            ShortcutPressed?.Invoke(this, new ShortcutPressedEventArgs(match));
        }

        return new ShortcutProcessResult(matches.AsReadOnly());
    }

    public IReadOnlyList<ShortcutConflict> GetConflicts()
    {
        Registration[] snapshot;
        lock (_gate)
        {
            snapshot = [.. _registrations];
        }

        var conflicts = new List<ShortcutConflict>();
        for (var firstIndex = 0; firstIndex < snapshot.Length; firstIndex++)
        {
            var first = snapshot[firstIndex].Definition;
            for (var secondIndex = firstIndex + 1; secondIndex < snapshot.Length; secondIndex++)
            {
                var second = snapshot[secondIndex].Definition;
                var sharedGestures = first.ResolveGestures(Platform, MacPrimaryModifier)
                    .Intersect(second.ResolveGestures(Platform, MacPrimaryModifier))
                    .ToArray();
                if (sharedGestures.Length > 0 && ScopesOverlap(first.Scope, second.Scope))
                {
                    conflicts.AddRange(sharedGestures.Select(gesture => new ShortcutConflict(
                        gesture,
                        first.Id,
                        second.Id,
                        first.Scope,
                        second.Scope)));
                }
            }
        }

        return conflicts;
    }

    public IReadOnlyList<ShortcutConventionIssue> ValidateConventions()
    {
        ShortcutDefinition[] definitions;
        lock (_gate)
        {
            definitions = _registrations.Select(item => item.Definition).ToArray();
        }

        return new ShortcutConventionValidator().Validate(definitions, Platform, MacPrimaryModifier);
    }

    public string FormatText(string shortcutId, string localizedTemplate) =>
        _textFormatter.FormatTemplate(
            localizedTemplate,
            GetGestureDisplayText(shortcutId),
            string.Join(_alternativeDisplaySeparator, GetGestureDisplayTexts(shortcutId)));

    public string RewriteText(
        string shortcutId,
        string localizedText,
        ShortcutTextRewriteMode mode = ShortcutTextRewriteMode.TemplateOnly) =>
        _textFormatter.Rewrite(
            localizedText,
            GetGestureDisplayText(shortcutId),
            mode,
            string.Join(_alternativeDisplaySeparator, GetGestureDisplayTexts(shortcutId)));

    private ShortcutDefinition GetRequiredDefinition(string shortcutId)
    {
        if (TryGetDefinition(shortcutId, out var definition))
        {
            return definition!;
        }

        throw new KeyNotFoundException($"No shortcut with the id '{shortcutId}' is registered.");
    }

    private bool Matches(
        ShortcutDefinition definition,
        ShortcutInput input,
        IReadOnlySet<string>? activeScopes)
    {
        if (input.IsRepeat && !definition.AllowRepeat)
        {
            return false;
        }

        if (!definition.IsCurrentlyEnabled() || !ScopeIsActive(definition.Scope, activeScopes))
        {
            return false;
        }

        return definition.ResolveGestures(Platform, MacPrimaryModifier).Contains(input.Gesture);
    }

    private static bool ScopeIsActive(string? scope, IReadOnlySet<string>? activeScopes) =>
        scope is null || activeScopes?.Contains(scope) == true;

    private static bool ScopesOverlap(string? first, string? second) =>
        first is null || second is null || string.Equals(first, second, StringComparison.Ordinal);

    private void Unregister(Guid token)
    {
        lock (_gate)
        {
            _registrations.RemoveAll(item => item.Token == token);
        }
    }

    private sealed record Registration(
        Guid Token,
        ShortcutDefinition Definition,
        Action<ShortcutMatch>? Handler,
        long Order);

    private sealed class RegistrationHandle : IDisposable
    {
        private ShortcutService? _owner;
        private readonly Guid _token;

        public RegistrationHandle(ShortcutService owner, Guid token)
        {
            _owner = owner;
            _token = token;
        }

        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Unregister(_token);
    }
}

public static class ShortcutServices
{
    public static IShortcutService Create(
        ShortcutPlatform platform,
        ShortcutServiceOptions? options = null) =>
        new ShortcutService(platform, options);

    public static IShortcutService CreateForCurrentPlatform(ShortcutServiceOptions? options = null) =>
        Create(DetectCurrentPlatform(), options);

    public static ShortcutPlatform DetectCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return ShortcutPlatform.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return ShortcutPlatform.MacOS;
        }

        if (OperatingSystem.IsLinux())
        {
            return ShortcutPlatform.Linux;
        }

        return ShortcutPlatform.Unknown;
    }
}
