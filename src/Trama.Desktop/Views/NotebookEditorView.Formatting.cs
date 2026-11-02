using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookEditorView
{
    private NotebookProseEditor? _activeProse;
    private bool _updatingFormattingControls;

    private void FormattingFocusChanged(object? sender, GotFocusEventArgs e)
    {
        if (e.Source is not Control source) return;
        var ancestors = source.GetVisualAncestors().Prepend(source).ToArray();
        if (ancestors.OfType<NotebookProseEditor>().FirstOrDefault() is { } prose) SetActiveProse(prose);
        // A selector popup has a separate visual root; it still belongs to this
        // formatting interaction and must not discard the prose target.
        else if (ancestors.Contains(this) && !ancestors.OfType<Control>().Any(control => control.Name == "ProseFormattingBar")) SetActiveProse(null);
    }

    private void SetActiveProse(NotebookProseEditor? prose)
    {
        if (!ReferenceEquals(_activeProse, prose))
        {
            if (_activeProse is not null) _activeProse.FormattingStateChanged -= ActiveProseChanged;
            _activeProse = prose;
            if (_activeProse is not null) _activeProse.FormattingStateChanged += ActiveProseChanged;
        }
        SynchronizeFormattingControls();
    }

    private void ActiveProseChanged(object? sender, EventArgs e) => SynchronizeFormattingControls();

    private void SynchronizeFormattingControls()
    {
        if (_updatingFormattingControls) return;
        if (_activeProse is not null && (_activeProse.DataContext is not NotebookBlockEditor block ||
            DataContext is not NotebookEditorViewModel editor || !editor.Blocks.Contains(block) ||
            !_activeProse.GetVisualAncestors().Contains(this)))
        {
            SetActiveProse(null);
            return;
        }
        _updatingFormattingControls = true;
        try
        {
            ProseFormattingTools.IsEnabled = _activeProse is not null;
            UndoProseButton.IsEnabled = _activeProse?.CanUndo == true;
            RedoProseButton.IsEnabled = _activeProse?.CanRedo == true;
            SelectChoice(ProseFontSizePicker, SelectedValue(span => span?.FontSize?.ToString(CultureInfo.InvariantCulture) ?? "default"));
            SelectChoice(ProseForegroundPicker, SelectedValue(span => span?.Foreground ?? "default"));
            SelectChoice(ProseHighlightPicker, SelectedValue(span => span?.Highlight ?? "none"));
            SelectChoice(ProseListPicker, SelectedListKind());
        }
        finally { _updatingFormattingControls = false; }
    }

    private string? SelectedValue(Func<TextStyleSpan?, string> value)
    {
        if (_activeProse?.DataContext is not NotebookBlockEditor block) return null;
        var start = _activeProse.SelectionStart;
        var end = start + _activeProse.SelectionLength;
        if (start == end)
        {
            var position = start == block.Text.Length ? start - 1 : start;
            return value(block.Styles.FirstOrDefault(span => span.Offset <= position && span.Offset + span.Length > position));
        }
        var values = new HashSet<string>(StringComparer.Ordinal);
        var positionInSelection = start;
        foreach (var span in block.Styles.Where(span => span.Offset < end && span.Offset + span.Length > start))
        {
            if (span.Offset > positionInSelection) values.Add(value(null));
            values.Add(value(span));
            positionInSelection = Math.Min(end, span.Offset + span.Length);
        }
        if (positionInSelection < end) values.Add(value(null));
        return values.Count == 1 ? values.Single() : null;
    }

    private string? SelectedListKind()
    {
        if (_activeProse?.DataContext is not NotebookBlockEditor block) return null;
        var document = new ParagraphListDocument(block.Text, block.Lists);
        var values = document.SelectedStarts(_activeProse.SelectionStart, _activeProse.SelectionLength)
            .Select(start => block.Lists.FirstOrDefault(item => item.Offset == start)?.Kind ?? "none").Distinct().ToArray();
        return values.Length == 1 ? values[0] : null;
    }

    private static void SelectChoice(ComboBox picker, string? value)
    {
        picker.PlaceholderText = "Misto";
        picker.SelectedItem = picker.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag?.ToString() == value);
    }

    private void FormattingChoiceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingFormattingControls || _activeProse is null || DataContext is not NotebookEditorViewModel { CanEditContent: true } ||
            sender is not ComboBox { SelectedItem: ComboBoxItem { Tag: string value } } picker) return;
        if (ReferenceEquals(picker, ProseFontSizePicker))
            _activeProse.ApplyFontSize(value == "default" ? null : int.Parse(value, CultureInfo.InvariantCulture));
        else if (ReferenceEquals(picker, ProseForegroundPicker))
            _activeProse.ApplyForeground(value == "default" ? null : value);
        else if (ReferenceEquals(picker, ProseHighlightPicker))
            _activeProse.ApplyHighlight(value == "none" ? null : value);
        else if (ReferenceEquals(picker, ProseListPicker))
            _activeProse.ApplyList(value);
        SynchronizeFormattingControls();
    }

    private void ClearProseFormattingClicked(object? sender, RoutedEventArgs e) => _activeProse?.ClearFormatting();
}
