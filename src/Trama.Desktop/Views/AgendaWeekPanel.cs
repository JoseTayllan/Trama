using Avalonia;
using Avalonia.Controls;

namespace Trama.Desktop.Views;

public sealed class AgendaWeekPanel : Panel
{
    private const double Gap = 8;
    private static int Columns(double width) => width >= 980 ? 7 : 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        var columns = Columns(availableSize.Width);
        var width = Math.Max(0, (availableSize.Width - Gap * (columns - 1)) / columns);
        var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(width, double.PositiveInfinity));
            height = columns == 7 ? Math.Max(height, child.DesiredSize.Height) : height + child.DesiredSize.Height;
        }
        if (columns == 1) height += Gap * Math.Max(0, Children.Count - 1);
        return new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 0, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Columns(finalSize.Width);
        var width = Math.Max(0, (finalSize.Width - Gap * (columns - 1)) / columns);
        var top = 0d;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            child.Arrange(columns == 7 ? new Rect(index * (width + Gap), 0, width, finalSize.Height)
                : new Rect(0, top, width, child.DesiredSize.Height));
            top += child.DesiredSize.Height + Gap;
        }
        return finalSize;
    }
}
