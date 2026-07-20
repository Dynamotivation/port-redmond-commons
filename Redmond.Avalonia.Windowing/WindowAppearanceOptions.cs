namespace Redmond.Avalonia.Windowing;

public enum WindowCornerStyle
{
    Windows10,
    Windows11,
    MacOS,
}

public enum WindowControlStyle
{
    Windows11,
    Windows10,
    MacOS,
}

public sealed record WindowAppearanceOptions(
    bool UseSystemBackdrop = true,
    WindowCornerStyle CornerStyle = WindowCornerStyle.Windows11,
    WindowControlStyle ControlStyle = WindowControlStyle.Windows11);
