namespace Redmond.Avalonia.Controls;

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

public enum TranslucentBackgroundMode
{
    Always,
    WhenSelected,
    Never,
}

public sealed record WindowAppearanceOptions(
    TranslucentBackgroundMode BackgroundMode = TranslucentBackgroundMode.WhenSelected,
    WindowCornerStyle CornerStyle = WindowCornerStyle.Windows11,
    WindowControlStyle ControlStyle = WindowControlStyle.Windows11)
{
    public bool UsesBackdrop(bool isWindowActive) => BackgroundMode switch
    {
        TranslucentBackgroundMode.Always => true,
        TranslucentBackgroundMode.WhenSelected => isWindowActive,
        _ => false,
    };
}
