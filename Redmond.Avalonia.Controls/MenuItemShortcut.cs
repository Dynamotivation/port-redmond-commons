using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Redmond.Avalonia.Controls;

/// <summary>
/// Shows a menu item's shortcut with the application's own label, such as
/// <c>Strg+Umschalt+S</c> from <c>Redmond.Shortcuts</c>, instead of Avalonia's
/// fixed English gesture text.
/// </summary>
/// <remarks>
/// The Fluent menu item template renders <see cref="MenuItem.InputGesture"/> in its
/// <c>PART_InputGestureText</c> block. This attached property replaces that text
/// whenever the template is applied. Keep setting <c>InputGesture</c> as well so
/// accessibility and the template's layout still see a gesture. Native macOS menu
/// items render their key equivalent through AppKit and need no label.
/// </remarks>
public static class MenuItemShortcut
{
    private const string GestureTextPartName = "PART_InputGestureText";

    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, string?>("Text", typeof(MenuItemShortcut));

    static MenuItemShortcut()
    {
        TextProperty.Changed.AddClassHandler<MenuItem>((item, _) =>
        {
            item.TemplateApplied -= OnTemplateApplied;
            item.TemplateApplied += OnTemplateApplied;
            Apply(item, FindGestureText(item));
        });
    }

    public static string? GetText(MenuItem item) => item.GetValue(TextProperty);

    public static void SetText(MenuItem item, string? value) => item.SetValue(TextProperty, value);

    private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            Apply(item, e.NameScope.Find<TextBlock>(GestureTextPartName));
        }
    }

    private static TextBlock? FindGestureText(MenuItem item) =>
        item.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(block => block.Name == GestureTextPartName);

    private static void Apply(MenuItem item, TextBlock? gestureText)
    {
        if (gestureText is null)
        {
            return;
        }

        if (GetText(item) is { } text)
        {
            gestureText.Text = text;
        }
        else
        {
            gestureText.ClearValue(TextBlock.TextProperty);
        }
    }
}
