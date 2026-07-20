using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Redmond.Avalonia.Controls;

/// <summary>
/// A title-bar surface that moves its owning window from its non-interactive
/// area. Interactive children handle their pointer press before it reaches the
/// region, matching Calculator's custom title-bar behavior.
/// </summary>
public sealed class WindowDragRegion : Border
{
    public WindowDragRegion()
    {
        // A null background is not hit-testable in Avalonia. Transparent paints
        // no pixels but makes the complete title-bar rectangle receive pointer
        // input; child buttons remain above this background layer.
        Background = Brushes.Transparent;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Copied from Calculator's TitleBar_OnPointerPressed behavior and made
        // reusable for any Avalonia window/title-bar surface.
        if (TopLevel.GetTopLevel(this) is Window window
            && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            window.BeginMoveDrag(e);
        }
    }
}
