using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaEdit;

namespace Trama.Desktop.Views;

public sealed partial class NotebookEditorView
{
    private void ScrollOverBlock(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None || e.Delta.Y == 0 || Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y) ||
            e.Source is not Control source || !source.GetVisualAncestors().Prepend(source).OfType<TextEditor>().Any())
            return;

        // Expanded blocks have no vertical viewport of their own. Route the wheel
        // to the page before their internal ScrollViewer can consume it.
        PageScroll.Offset = new Vector(PageScroll.Offset.X, PageScroll.Offset.Y - e.Delta.Y * 50);
        e.Handled = true;
    }
}
