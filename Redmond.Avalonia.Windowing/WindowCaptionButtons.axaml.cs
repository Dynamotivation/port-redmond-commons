using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Redmond.Avalonia.Windowing;

public partial class WindowCaptionButtons : UserControl
{
    public static readonly StyledProperty<WindowControlStyle> ControlStyleProperty =
        AvaloniaProperty.Register<WindowCaptionButtons, WindowControlStyle>(
            nameof(ControlStyle),
            WindowControlStyle.Windows11);

    public WindowCaptionButtons()
    {
        InitializeComponent();
        UpdateStyle(ControlStyle);
    }

    public WindowControlStyle ControlStyle
    {
        get => GetValue(ControlStyleProperty);
        set => SetValue(ControlStyleProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ControlStyleProperty)
        {
            UpdateStyle(change.GetNewValue<WindowControlStyle>());
        }
    }

    private void UpdateStyle(WindowControlStyle style)
    {
        var windows10 = style == WindowControlStyle.Windows10;
        MinimizeButton?.Classes.Set("windows10", windows10);
        MaximizeButton?.Classes.Set("windows10", windows10);
        CloseButton?.Classes.Set("windows10", windows10);
        if (Windows10Minimize is null)
        {
            return;
        }

        Windows10Minimize.IsVisible = windows10;
        Windows10Maximize.IsVisible = windows10;
        Windows10Close.IsVisible = windows10;
        Windows11Minimize.IsVisible = !windows10;
        Windows11Maximize.IsVisible = !windows10;
        Windows11Close.IsVisible = !windows10;
    }

    private Window? OwnerWindow => TopLevel.GetTopLevel(this) as Window;

    private void MinimizeOnClick(object? sender, RoutedEventArgs e)
    {
        if (OwnerWindow is { } window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void MaximizeOnClick(object? sender, RoutedEventArgs e)
    {
        if (OwnerWindow is { } window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    private void CloseOnClick(object? sender, RoutedEventArgs e) => OwnerWindow?.Close();
}
