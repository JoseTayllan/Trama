using System.Globalization;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookProseEditor
{
    private ListMargin? _listMargin;

    private void InitializeLists()
    {
        _listMargin = new ListMargin(this);
        TextArea.LeftMargins.Add(_listMargin);
        TextArea.TextView.VisualLinesChanged += (_, _) => _listMargin.InvalidateVisual();
    }

    private void RefreshListMargin()
    {
        if (_listMargin is null) return;
        _listMargin.IsVisible = _block?.Lists.Count > 0;
        _listMargin.InvalidateMeasure();
        _listMargin.InvalidateVisual();
    }

    public bool ApplyList(string kind)
    {
        if (_block is null || Owner is not { } owner) return false;
        var before = _block.Lists.ToArray();
        var styles = _block.Styles.ToArray();
        var start = SelectionStart;
        var length = SelectionLength;
        var changed = owner.FormatParagraphList(_block.Id, start, length, kind);
        if (changed)
            Document.UndoStack.Push(new StyleHistoryOperation(this, _block,
                styles, styles, before, _block.Lists.ToArray(), textChange: false, start, length));
        Select(start, length);
        TextArea.Focus();
        FormattingStateChanged?.Invoke(this, EventArgs.Empty);
        return changed;
    }

    private sealed class ListMargin(NotebookProseEditor editor) : AbstractMargin
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var digits = Math.Max(2, (editor._block?.Lists.Count ?? 0).ToString(CultureInfo.InvariantCulture).Length);
            var size = Math.Max(editor.FontSize, editor._block?.Styles.Max(span => (double?)span.FontSize) ?? editor.FontSize);
            return new Size(digits * size * 0.7 + 18, 0);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (editor._block is not NotebookBlockEditor block || TextView is not { VisualLinesValid: true } view) return;
            var items = block.Lists.ToDictionary(item => item.Offset);
            var labels = new Dictionary<int, string>();
            var number = 0;
            foreach (var line in editor.Document.Lines)
            {
                if (!items.TryGetValue(line.Offset, out var item)) { number = 0; continue; }
                labels[line.Offset] = item.Kind == "numbered" ? $"{++number}." : "•";
                if (item.Kind != "numbered") number = 0;
            }
            foreach (var line in view.VisualLines)
            {
                if (!labels.TryGetValue(line.FirstDocumentLine.Offset, out var label)) continue;
                var offset = line.FirstDocumentLine.Offset;
                var span = block.Styles.FirstOrDefault(style => style.Offset <= offset && style.Offset + style.Length > offset);
                var size = span?.FontSize ?? editor.FontSize;
                var text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(editor.FontFamily), size, editor.Foreground);
                var top = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.Baseline)
                    - view.VerticalOffset - text.Baseline;
                context.DrawText(text, new Point(Math.Max(0, Bounds.Width - text.Width - 10), top));
            }
        }
    }
}
