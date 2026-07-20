using Avalonia;
using Avalonia.Controls;

namespace Redmond.Avalonia.Controls;

/// <summary>
/// A non-expanding Fluent settings row with an icon, title, optional
/// description, and a trailing action control.
/// </summary>
public sealed class SettingsCard : ContentControl
{
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<SettingsCard, object?>(nameof(Icon));

    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<SettingsCard, object?>(nameof(Header));

    public static readonly StyledProperty<object?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsCard, object?>(nameof(Description));

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(SettingsCard);
}
