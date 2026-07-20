using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Redmond.Avalonia.Controls;

public partial class WindowAppearanceSettingsPanel : UserControl
{
    private WindowAppearanceOptions _options = new();
    private bool _synchronizing;

    public WindowAppearanceSettingsPanel()
    {
        InitializeComponent();
        SynchronizeControls();
    }

    public WindowAppearanceOptions Options => _options;

    public event EventHandler<WindowAppearanceOptions>? AppearanceChanged;

    public void SetOptions(WindowAppearanceOptions options)
    {
        _options = options;
        SynchronizeControls();
    }

    private void SynchronizeControls()
    {
        _synchronizing = true;
        BackdropAlways.IsChecked = _options.BackgroundMode == TranslucentBackgroundMode.Always;
        BackdropWhenSelected.IsChecked = _options.BackgroundMode == TranslucentBackgroundMode.WhenSelected;
        BackdropNever.IsChecked = _options.BackgroundMode == TranslucentBackgroundMode.Never;
        Windows10Corners.IsChecked = _options.CornerStyle == WindowCornerStyle.Windows10;
        Windows11Corners.IsChecked = _options.CornerStyle == WindowCornerStyle.Windows11;
        MacOSCorners.IsChecked = _options.CornerStyle == WindowCornerStyle.MacOS;
        Windows10Controls.IsChecked = _options.ControlStyle == WindowControlStyle.Windows10;
        Windows11Controls.IsChecked = _options.ControlStyle == WindowControlStyle.Windows11;
        MacOSControls.IsChecked = _options.ControlStyle == WindowControlStyle.MacOS;
        _synchronizing = false;
    }

    private void BackdropOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string value }
            && Enum.TryParse<TranslucentBackgroundMode>(value, out var mode))
        {
            Update(_options with { BackgroundMode = mode });
        }
    }

    private void CornerOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string value }
            && Enum.TryParse<WindowCornerStyle>(value, out var style))
        {
            Update(_options with { CornerStyle = style });
        }
    }

    private void ControlOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string value }
            && Enum.TryParse<WindowControlStyle>(value, out var style))
        {
            Update(_options with { ControlStyle = style });
        }
    }

    private void Update(WindowAppearanceOptions options)
    {
        if (_synchronizing || options == _options)
        {
            return;
        }

        _options = options;
        AppearanceChanged?.Invoke(this, options);
    }
}
