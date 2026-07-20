using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Redmond.Avalonia.Controls;

/// <summary>
/// A Fluent settings card whose content remains mounted while its height and
/// vertical offset animate. Copied and generalized from Redmond Calculator's
/// <c>../calculator/src/Frontends/Calculator.Avalonia/AnimatedSettingsExpander.cs</c>.
/// </summary>
public sealed class AnimatedSettingsExpander : Expander
{
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<AnimatedSettingsExpander, object?>(nameof(Icon));

    public static readonly StyledProperty<object?> DescriptionProperty =
        AvaloniaProperty.Register<AnimatedSettingsExpander, object?>(nameof(Description));

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<AnimatedSettingsExpander, double>(nameof(HeaderHeight), 72);

    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(200);

    private Border? _contentHost;
    private ToggleButton? _headerToggle;
    private TranslateTransform? _contentTransform;
    private CancellationTokenSource? _animationCancellation;
    private int _animationVersion;

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public double HeaderHeight
    {
        get => GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(AnimatedSettingsExpander);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_headerToggle is not null)
        {
            _headerToggle.Click -= HeaderToggleOnClick;
        }

        _headerToggle = e.NameScope.Find<ToggleButton>("ExpanderHeader");
        if (_headerToggle is not null)
        {
            _headerToggle.Click += HeaderToggleOnClick;
        }

        _contentHost = e.NameScope.Find<Border>("ExpanderContent");
        if (_contentHost is null)
        {
            return;
        }

        // Avalonia 12.1's stock Expander removes collapsed content from
        // layout. The owned template intentionally leaves visibility unbound
        // so collapse and rapid reversal can animate the existing visual.
        _contentHost.IsVisible = true;
        _contentHost.MinHeight = 0;
        _contentHost.ClipToBounds = true;
        _contentTransform = new TranslateTransform();
        if (_contentHost.Child is Visual contentVisual)
        {
            contentVisual.RenderTransform = _contentTransform;
        }

        if (IsExpanded)
        {
            _contentHost.ClearValue(Layoutable.HeightProperty);
        }
        else
        {
            _contentHost.Height = 0;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsExpandedProperty && _contentHost is not null)
        {
            StartHeightAnimation(IsExpanded);
        }
    }

    private void HeaderToggleOnClick(object? sender, RoutedEventArgs e) =>
        IsExpanded = !IsExpanded;

    private async void StartHeightAnimation(bool expanding)
    {
        _animationCancellation?.Cancel();
        _animationCancellation?.Dispose();
        _animationCancellation = new CancellationTokenSource();
        var cancellationToken = _animationCancellation.Token;
        var animationVersion = ++_animationVersion;
        var contentHost = _contentHost;

        if (contentHost is null)
        {
            return;
        }

        var currentHeight = contentHost.Bounds.Height;
        var targetHeight = 0d;
        var currentOffset = _contentTransform?.Y ?? 0;

        if (expanding)
        {
            contentHost.ClearValue(Layoutable.HeightProperty);
            contentHost.Measure(new Size(
                Math.Max(contentHost.Bounds.Width, contentHost.DesiredSize.Width),
                double.PositiveInfinity));
            targetHeight = contentHost.DesiredSize.Height;
            if (currentHeight <= 0 && Math.Abs(currentOffset) < 0.01)
            {
                currentOffset = -targetHeight;
                if (_contentTransform is not null)
                {
                    _contentTransform.Y = currentOffset;
                }
            }
        }

        var targetOffset = expanding ? 0 : -Math.Max(currentHeight, contentHost.DesiredSize.Height);
        contentHost.Height = currentHeight;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            while (stopwatch.Elapsed < AnimationDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var progress = Math.Clamp(
                    stopwatch.Elapsed.TotalMilliseconds / AnimationDuration.TotalMilliseconds,
                    0,
                    1);
                var easedProgress = 1 - Math.Pow(1 - progress, 3);
                var height = currentHeight + ((targetHeight - currentHeight) * easedProgress);
                var offset = currentOffset + ((targetOffset - currentOffset) * easedProgress);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    contentHost.Height = height;
                    if (_contentTransform is not null)
                    {
                        _contentTransform.Y = offset;
                    }
                });
                await Task.Delay(16, cancellationToken).ConfigureAwait(false);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (animationVersion != _animationVersion)
                {
                    return;
                }

                if (expanding)
                {
                    contentHost.ClearValue(Layoutable.HeightProperty);
                    if (_contentTransform is not null)
                    {
                        _contentTransform.Y = 0;
                    }
                }
                else
                {
                    contentHost.Height = 0;
                    if (_contentTransform is not null)
                    {
                        _contentTransform.Y = targetOffset;
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
            // A reversal deliberately continues from the rendered height.
        }
    }
}
