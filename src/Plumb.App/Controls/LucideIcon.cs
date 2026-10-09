using Avalonia;
using Avalonia.Controls.Primitives;

namespace Plumb.App.Controls;

/// <summary>
/// Draws a Lucide icon: stroke geometry in a 24x24 box, scaled to <see cref="Size"/>, in the current foreground.
/// </summary>
public sealed class LucideIcon : TemplatedControl
{
    public static readonly StyledProperty<Avalonia.Media.Geometry?> DataProperty = AvaloniaProperty.Register<LucideIcon, Avalonia.Media.Geometry?>(nameof(Data));

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<LucideIcon, double>(nameof(Size), 20);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<LucideIcon, double>(nameof(StrokeThickness), 2);

    public Avalonia.Media.Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
}
