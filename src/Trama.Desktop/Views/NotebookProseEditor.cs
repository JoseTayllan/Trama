using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookProseEditor : TextEditor
{
    protected override Type StyleKeyOverride => typeof(TextEditor);
    private NotebookBlockEditor? _block;
    private NotebookBlockEditor? _historyBlock;
    private PendingStyles? _pendingHistoryStyles;
    private bool _synchronizing;
    private bool _observingFormattingResources;
    private readonly HashSet<AvaloniaObject> _formattingBrushes = [];
    public event EventHandler? FormattingStateChanged;
    public NotebookProseEditor()
    {
        WordWrap = true;
        ShowLineNumbers = false;
        FontSize = 16;
        MinHeight = 60;
        Padding = new Thickness(10, 8);
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        TextArea.TextView.LineTransformers.Add(new StyleTransformer(this));
        InitializeLists();
        Document.UndoStack.SizeLimit = 200;
        Document.UndoStack.PropertyChanged += (_, _) => FormattingStateChanged?.Invoke(this, EventArgs.Empty);
        TextChanged += TextEdited;
        TextArea.SelectionChanged += (_, _) => FormattingStateChanged?.Invoke(this, EventArgs.Empty);
        ResourcesChanged += (_, _) =>
        {
            if (!_observingFormattingResources) return;
            ObserveFormattingBrushes();
            TextArea.TextView.Redraw();
        };
        LayoutUpdated += (_, _) =>
        {
            var height = Math.Max(60, TextArea.TextView.DocumentHeight + Padding.Top + Padding.Bottom + 8);
            if (Math.Abs(Height - height) > 1 || double.IsNaN(Height)) Height = height;
        };
        DataContextChanged += (_, _) => ObserveBlock();
        AddHandler(KeyDownEvent, FormatKeyDown, RoutingStrategies.Tunnel);
    }

    private NotebookEditorViewModel? Owner => this.GetVisualAncestors().OfType<NotebookEditorView>().FirstOrDefault()?.DataContext as NotebookEditorViewModel;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _observingFormattingResources = true;
        ObserveFormattingBrushes();
        ObserveBlock();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_block is not null) _block.PropertyChanged -= BlockChanged;
        _block = null;
        _observingFormattingResources = false;
        ObserveFormattingBrushes();
        FormattingStateChanged?.Invoke(this, EventArgs.Empty);
        base.OnDetachedFromVisualTree(e);
    }

    private void ObserveBlock()
    {
        if (_block is not null) _block.PropertyChanged -= BlockChanged;
        _block = DataContext as NotebookBlockEditor;
        if (!ReferenceEquals(_historyBlock, _block))
        {
            Document.UndoStack.ClearAll();
            _pendingHistoryStyles = null;
            _historyBlock = _block;
        }
        if (_block is not null) _block.PropertyChanged += BlockChanged;
        Synchronize();
    }

    private void BlockChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_synchronizing) Synchronize();
    }

    private void Synchronize()
    {
        if (_block is null) return;
        _synchronizing = true;
        try { if (Text != _block.Text) Text = _block.Text; }
        finally { _synchronizing = false; }
        TextArea.TextView.Redraw();
        RefreshListMargin();
        Height = Math.Max(60, (Document?.LineCount ?? 1) * 26 + 24);
        InvalidateMeasure();
        FormattingStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TextEdited(object? sender, EventArgs e)
    {
        if (_synchronizing || _block is null || (Text == _block.Text && _pendingHistoryStyles is null)) return;
        var old = _block.Text;
        var updated = Text;
        var pending = _pendingHistoryStyles;
        _pendingHistoryStyles = null;
        var beforeStyles = _block.Styles.ToArray();
        var beforeLists = _block.Lists.ToArray();
        var prefix = 0;
        while (prefix < old.Length && prefix < updated.Length && old[prefix] == updated[prefix]) prefix++;
        var suffix = 0;
        while (suffix < old.Length - prefix && suffix < updated.Length - prefix && old[old.Length - suffix - 1] == updated[updated.Length - suffix - 1]) suffix++;
        _synchronizing = true;
        try
        {
            if (pending is not null && ReferenceEquals(pending.Block, _block))
            {
                _block.Text = updated;
                _block.Styles = pending.Styles.ToArray();
                _block.Lists = pending.Lists.ToArray();
            }
            else if (Owner?.EditText(_block.Id, prefix, old.Length - prefix - suffix, updated.Substring(prefix, updated.Length - prefix - suffix)) == true)
            {
                // TextChanged runs inside the document's undo group. The native text
                // operation and this metadata operation must travel through history together.
                Document.UndoStack.PushOptional(new StyleHistoryOperation(this, _block,
                    beforeStyles, _block.Styles.ToArray(), beforeLists, _block.Lists.ToArray(), textChange: true, SelectionStart, SelectionLength));
            }
        }
        finally { _synchronizing = false; }
        Synchronize();
    }

    public bool ApplyStyle(InlineTextStyle style)
        => ApplyFormatting((owner, id, offset, length) => owner.FormatTextSelection(id, offset, length, style));

    public bool ApplyFontSize(int? size)
        => ApplyFormatting((owner, id, offset, length) => owner.FormatTextSize(id, offset, length, size));

    public bool ApplyForeground(string? color)
        => ApplyFormatting((owner, id, offset, length) => owner.FormatTextForeground(id, offset, length, color));

    public bool ApplyHighlight(string? color)
        => ApplyFormatting((owner, id, offset, length) => owner.FormatTextHighlight(id, offset, length, color));

    public bool ClearFormatting()
        => ApplyFormatting((owner, id, offset, length) => owner.ClearTextFormatting(id, offset, length));

    private bool ApplyFormatting(Func<NotebookEditorViewModel, Guid, int, int, bool> change)
    {
        if (_block is null || Owner is not { } owner) return false;
        var before = _block.Styles.ToArray();
        var start = SelectionStart;
        var length = SelectionLength;
        var applied = change(owner, _block.Id, start, length);
        if (applied && !before.SequenceEqual(_block.Styles))
            Document.UndoStack.Push(new StyleHistoryOperation(this, _block,
                before, _block.Styles.ToArray(), _block.Lists.ToArray(), _block.Lists.ToArray(), textChange: false, start, length));
        Select(start, length);
        TextArea.Focus();
        FormattingStateChanged?.Invoke(this, EventArgs.Empty);
        return applied;
    }

    private void FormatKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && SelectionLength == 0 &&
            Owner?.CanEditContent == true && _block is not null)
        {
            var line = Document.GetLineByOffset(CaretOffset);
            if (line.Length == 0 && _block.Lists.Any(item => item.Offset == line.Offset))
            {
                e.Handled = true;
                ApplyList("none");
                return;
            }
        }
        if ((e.KeyModifiers == KeyModifiers.Control && e.Key is Key.Z or Key.Y) ||
            (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.Z))
        {
            e.Handled = true;
            if (Owner?.CanEditContent == true)
            {
                if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control) Undo();
                else Redo();
            }
            return;
        }
        if (e.KeyModifiers != KeyModifiers.Control || e.Key is not (Key.B or Key.I)) return;
        e.Handled = true;
        ApplyStyle(e.Key == Key.B ? InlineTextStyle.Bold : InlineTextStyle.Italic);
    }

    private sealed record PendingStyles(NotebookBlockEditor Block, IReadOnlyList<TextStyleSpan> Styles, IReadOnlyList<ParagraphListItem> Lists);

    private sealed class StyleHistoryOperation(NotebookProseEditor editor, NotebookBlockEditor block,
        IReadOnlyList<TextStyleSpan> before, IReadOnlyList<TextStyleSpan> after,
        IReadOnlyList<ParagraphListItem> beforeLists, IReadOnlyList<ParagraphListItem> afterLists, bool textChange,
        int selectionStart, int selectionLength) : IUndoableOperation
    {
        public void Undo() => Apply(before, beforeLists);
        public void Redo() => Apply(after, afterLists);

        private void Apply(IReadOnlyList<TextStyleSpan> styles, IReadOnlyList<ParagraphListItem> lists)
        {
            if (!ReferenceEquals(editor._block, block)) return;
            if (textChange)
            {
                // Native undo defers TextChanged until all grouped text operations finish.
                editor._pendingHistoryStyles = new PendingStyles(block, styles, lists);
                return;
            }

            editor._synchronizing = true;
            try { block.Styles = styles.ToArray(); block.Lists = lists.ToArray(); }
            finally { editor._synchronizing = false; }
            editor.Synchronize();
            var start = Math.Min(selectionStart, editor.Text.Length);
            editor.Select(start, Math.Min(selectionLength, editor.Text.Length - start));
        }
    }

    private sealed class StyleTransformer(NotebookProseEditor editor) : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var span in editor._block?.Styles ?? Array.Empty<TextStyleSpan>())
            {
                var start = Math.Max(line.Offset, span.Offset);
                var end = Math.Min(line.EndOffset, span.Offset + span.Length);
                if (start >= end) continue;
                ChangeLinePart(start, end, element =>
                {
                    element.TextRunProperties.SetTypeface(new Typeface(editor.FontFamily,
                        span.Style.HasFlag(InlineTextStyle.Italic) ? FontStyle.Italic : FontStyle.Normal,
                        span.Style.HasFlag(InlineTextStyle.Bold) ? FontWeight.Bold : FontWeight.Normal));
                    if (span.FontSize is { } size) element.TextRunProperties.SetFontRenderingEmSize(size);
                    if (span.Foreground is { } foreground && editor.TryFindResource(BrushKey("Foreground", foreground), out var textBrush) && textBrush is IBrush brush)
                        element.TextRunProperties.SetForegroundBrush(brush);
                    if (span.Highlight is { } highlight && editor.TryFindResource(BrushKey("Highlight", highlight), out var background) && background is IBrush backgroundBrush)
                        element.TextRunProperties.SetBackgroundBrush(backgroundBrush);
                });
            }
        }
    }

    private static string BrushKey(string role, string color) =>
        $"TramaProse{role}{char.ToUpperInvariant(color[0])}{color[1..]}Brush";

    private void ObserveFormattingBrushes()
    {
        foreach (var brush in _formattingBrushes) brush.PropertyChanged -= FormattingBrushChanged;
        _formattingBrushes.Clear();
        if (!_observingFormattingResources) return;
        foreach (var (role, colors) in new[]
        {
            ("Foreground", new[] { "teal", "blue", "violet", "amber", "coral" }),
            ("Highlight", new[] { "amber", "green", "blue", "violet" })
        })
        {
            foreach (var color in colors)
                if (this.TryFindResource(BrushKey(role, color), out var resource) && resource is AvaloniaObject brush && _formattingBrushes.Add(brush))
                    brush.PropertyChanged += FormattingBrushChanged;
        }
    }

    private void FormattingBrushChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => TextArea.TextView.Redraw();
}
