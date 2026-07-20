using Avalonia.Controls;
using Avalonia.Input;

namespace Redmond.Avalonia.Windowing;

public partial class WindowResizeHandles : UserControl
{
    public WindowResizeHandles() => InitializeComponent();

    private void ResizeOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { Tag: string edgeName }
            || !Enum.TryParse<WindowEdge>(edgeName, out var edge)
            || TopLevel.GetTopLevel(this) is not Window window
            || !e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            return;
        }

        window.BeginResizeDrag(edge, e);
    }
}
