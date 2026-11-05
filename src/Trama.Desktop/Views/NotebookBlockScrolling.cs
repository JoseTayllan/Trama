using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit;

namespace Trama.Desktop.Views;

internal static class NotebookBlockScrolling
{
    public static void Attach(TextEditor editor)
    {
        editor.AddHandler(Control.RequestBringIntoViewEvent, (_, e) =>
        {
            if (!ReferenceEquals(e.TargetObject, editor.TextArea) ||
                editor.GetVisualAncestors().OfType<NotebookEditorView>().FirstOrDefault() is not { } page)
                return;

            // TextArea.ScrollToLine emits line numbers, not pixel coordinates.
            // They must not reach the physical ScrollViewer of the whole page.
            e.Handled = true;
            var scroll = page.FindControl<ScrollViewer>("PageScroll")!;
            var view = editor.TextArea.TextView;
            if (e.TargetRect.Width != 0 || e.TargetRect.Height != 1)
            {
                // Focusing a partly visible block must not reveal its entire height.
                var origin = editor.TranslatePoint(default, scroll);
                if (origin is null || (origin.Value.Y < scroll.Viewport.Height && origin.Value.Y + editor.Bounds.Height > 0))
                    return;
            }

            var caret = editor.TextArea.Caret.CalculateCaretRectangle();
            var rectangle = new Rect(caret.Position - view.ScrollOffset, caret.Size);
            view.BringIntoView(rectangle);
        }, RoutingStrategies.Bubble);
    }
}
