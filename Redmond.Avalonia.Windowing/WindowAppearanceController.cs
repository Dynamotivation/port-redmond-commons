using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Redmond.Avalonia.Windowing;

/// <summary>
/// Applies Redmond window materials and decoration choices to an Avalonia
/// window while keeping platform interop out of application code.
/// </summary>
public sealed class WindowAppearanceController : IDisposable
{
    private readonly Window _window;
    private readonly Border? _surface;
    private MacOSMaterialBackdrop? _materialBackdrop;
    private MacOSWindowControls? _macOSWindowControls;
    private WindowAppearanceOptions _options;
    private bool _isOpened;
    private bool _isDisposed;
    private int _presentationVersion;

    private WindowAppearanceController(
        Window window,
        Border? surface,
        WindowAppearanceOptions options)
    {
        _window = window;
        _surface = surface;
        _options = options;
        _window.Opened += OnOpened;
        _window.Closed += OnClosed;
        ApplyWindowDecorations(options);
        ApplySurface(options);
    }

    public WindowAppearanceOptions Options => _options;

    public bool UsesMacOSControls =>
        OperatingSystem.IsMacOS() && _options.ControlStyle == WindowControlStyle.MacOS;

    public bool UsesCustomResizeHandles =>
        !((OperatingSystem.IsMacOS() && _options.CornerStyle == WindowCornerStyle.MacOS)
          || (!OperatingSystem.IsMacOS() && _options.ControlStyle == WindowControlStyle.MacOS));

    public event EventHandler? PresentationChanged;

    public static WindowAppearanceController Attach(
        Window window,
        Border? surface = null,
        WindowAppearanceOptions? options = null) =>
        new(window, surface, options ?? new WindowAppearanceOptions());

    public void Apply(WindowAppearanceOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var previous = _options;
        var wasUsingNativeTitleBar = UsesFullNativeTitleBar(previous);
        var requiresStagedHostedControls = _isOpened
            && wasUsingNativeTitleBar
            && options.CornerStyle != WindowCornerStyle.MacOS
            && options.ControlStyle == WindowControlStyle.MacOS;
        var presentationVersion = ++_presentationVersion;

        _options = options;
        _macOSWindowControls?.Dispose();
        _macOSWindowControls = null;

        if (OperatingSystem.IsMacOS() && !UsesFullNativeTitleBar(options))
        {
            MacOSNativeTitleBar.Apply(_window, enabled: false);
        }

        if (requiresStagedHostedControls)
        {
            var teardown = options with
            {
                CornerStyle = WindowCornerStyle.MacOS,
                ControlStyle = WindowControlStyle.Windows11,
            };
            ApplyWindowDecorations(teardown);
            RefreshBackdrop(teardown);

            Dispatcher.UIThread.Post(() =>
            {
                if (!_isOpened || presentationVersion != _presentationVersion || _options != options)
                {
                    return;
                }

                ApplyAll(options);
            }, DispatcherPriority.Background);
            return;
        }

        ApplyAll(options, deferFullNativeTitleBar: true);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _window.Opened -= OnOpened;
        _window.Closed -= OnClosed;
        _materialBackdrop?.Dispose();
        _macOSWindowControls?.Dispose();
        _materialBackdrop = null;
        _macOSWindowControls = null;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _isOpened = true;
        RefreshBackdrop(_options);
        RefreshWindowPresentation(_options);
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _isOpened = false;
        Dispose();
    }

    private void ApplyAll(WindowAppearanceOptions options, bool deferFullNativeTitleBar = false)
    {
        ApplyWindowDecorations(options);
        ApplySurface(options);
        RefreshBackdrop(options);
        RefreshWindowPresentation(options, deferFullNativeTitleBar);
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyWindowDecorations(WindowAppearanceOptions options)
    {
        if (!OperatingSystem.IsMacOS())
        {
            var useNativeDecorations = options.ControlStyle == WindowControlStyle.MacOS;
            _window.ExtendClientAreaToDecorationsHint = false;
            _window.ExtendClientAreaTitleBarHeightHint = 42;
            _window.WindowDecorations = useNativeDecorations
                ? WindowDecorations.Full
                : WindowDecorations.None;
            return;
        }

        var usesNativeTitleBar = UsesFullNativeTitleBar(options);
        var usesNativeGeometry = options.CornerStyle == WindowCornerStyle.MacOS;
        _window.ExtendClientAreaToDecorationsHint = usesNativeGeometry;
        _window.ExtendClientAreaTitleBarHeightHint = 42;
        _window.WindowDecorations = usesNativeTitleBar
            ? WindowDecorations.Full
            : usesNativeGeometry
                ? WindowDecorations.BorderOnly
                : WindowDecorations.None;
    }

    private void ApplySurface(WindowAppearanceOptions options)
    {
        if (_surface is null)
        {
            return;
        }

        var usesNativeGeometry = OperatingSystem.IsMacOS()
            && options.CornerStyle == WindowCornerStyle.MacOS;
        _surface.CornerRadius = options.CornerStyle == WindowCornerStyle.Windows11
            ? new CornerRadius(8)
            : new CornerRadius(0);
        _surface.BorderThickness = usesNativeGeometry ? new Thickness(0) : new Thickness(1);
        _surface.ClipToBounds = true;
    }

    private void RefreshBackdrop(WindowAppearanceOptions options)
    {
        _materialBackdrop?.Dispose();
        _materialBackdrop = null;

        _window.Background = Brushes.Transparent;
        _window.TransparencyLevelHint = options.UseSystemBackdrop
            ? OperatingSystem.IsWindows()
                ?
                [
                    WindowTransparencyLevel.Mica,
                    WindowTransparencyLevel.AcrylicBlur,
                    WindowTransparencyLevel.Blur,
                    WindowTransparencyLevel.Transparent,
                ]
                : [WindowTransparencyLevel.Transparent]
            : [WindowTransparencyLevel.Transparent];

        if (_isOpened && options.UseSystemBackdrop && OperatingSystem.IsMacOS())
        {
            var cornerRadius = options.CornerStyle == WindowCornerStyle.Windows11 ? 8 : 0;
            _materialBackdrop = MacOSMaterialBackdrop.Attach(_window, cornerRadius);
        }

        MacOSMaterialBackdrop.InvalidateWindowShadow(_window);
    }

    private void RefreshWindowPresentation(
        WindowAppearanceOptions options,
        bool deferFullNativeTitleBar = false)
    {
        if (!_isOpened || !OperatingSystem.IsMacOS())
        {
            return;
        }

        if (UsesFullNativeTitleBar(options))
        {
            if (deferFullNativeTitleBar)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_isOpened && UsesFullNativeTitleBar(_options))
                    {
                        MacOSNativeTitleBar.Apply(_window, enabled: true);
                        MacOSMaterialBackdrop.InvalidateWindowShadow(_window);
                    }
                }, DispatcherPriority.Background);
            }
            else
            {
                MacOSNativeTitleBar.Apply(_window, enabled: true);
            }
        }
        else if (options.ControlStyle == WindowControlStyle.MacOS)
        {
            _macOSWindowControls = MacOSWindowControls.Attach(_window);
        }
    }

    private static bool UsesFullNativeTitleBar(WindowAppearanceOptions options) =>
        OperatingSystem.IsMacOS()
        && options.CornerStyle == WindowCornerStyle.MacOS
        && options.ControlStyle == WindowControlStyle.MacOS;
}
