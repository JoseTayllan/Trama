using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookEditorView
{
    public static readonly StyledProperty<bool> UseHostHeaderProperty =
        AvaloniaProperty.Register<NotebookEditorView, bool>(nameof(UseHostHeader));
    public static readonly StyledProperty<string> SectionCaptionProperty =
        AvaloniaProperty.Register<NotebookEditorView, string>(nameof(SectionCaption), string.Empty);
    public static readonly StyledProperty<string> TagSummaryProperty =
        AvaloniaProperty.Register<NotebookEditorView, string>(nameof(TagSummary), string.Empty);

    private INotifyCollectionChanged? _markerChanges;
    private bool? _compactFormatting;
    public bool UseHostHeader { get => GetValue(UseHostHeaderProperty); set => SetValue(UseHostHeaderProperty, value); }
    public string SectionCaption { get => GetValue(SectionCaptionProperty); set => SetValue(SectionCaptionProperty, value); }
    public string TagSummary { get => GetValue(TagSummaryProperty); set => SetValue(TagSummaryProperty, value); }

    private void ToggleFormattingClicked(object? sender, RoutedEventArgs e) => ShowFormatting(!ProseFormattingTools.IsVisible);

    private void UpdateFormattingLayout()
    {
        if (Bounds.Width <= 0) return;
        var compact = Bounds.Width < 900;
        if (_compactFormatting == compact) return;
        _compactFormatting = compact;
        ShowFormatting(!compact);
    }

    private void ShowFormatting(bool visible)
    {
        ProseFormattingTools.IsVisible = visible;
        ToggleFormattingButton.Content = visible ? "Texto −" : "Texto · formatação";
    }

    private void BlockActionsAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not NotebookBlockEditor block ||
            DataContext is not NotebookEditorViewModel editor) return;
        button.Flyout = new MenuFlyout
        {
            Items =
            {
                new MenuItem { Header = "Antecipar bloco", Command = editor.MoveBlockUpCommand, CommandParameter = block.Id },
                new MenuItem { Header = "Adiar bloco", Command = editor.MoveBlockDownCommand, CommandParameter = block.Id },
                new MenuItem { Header = "Remover bloco", Command = editor.RemoveBlockCommand, CommandParameter = block.Id }
            }
        };
    }

    private void UndoProseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NotebookEditorViewModel { CanEditContent: true } && _activeProse?.Undo() == true)
            _activeProse.TextArea.Focus();
        SynchronizeFormattingControls();
    }

    private void RedoProseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NotebookEditorViewModel { CanEditContent: true } && _activeProse?.Redo() == true)
            _activeProse.TextArea.Focus();
        SynchronizeFormattingControls();
    }

    private void ObserveBlockMarkers(bool detach = false)
    {
        if (_markerChanges is not null) _markerChanges.CollectionChanged -= BlockMarkerCollectionChanged;
        _markerChanges = !detach && DataContext is NotebookEditorViewModel editor ? editor.Blocks : null;
        if (_markerChanges is not null) _markerChanges.CollectionChanged += BlockMarkerCollectionChanged;
        if (!detach) Dispatcher.UIThread.Post(UpdateBlockMarkers, DispatcherPriority.Loaded);
    }

    private void BlockMarkerCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.UIThread.Post(UpdateBlockMarkers, DispatcherPriority.Loaded);

    private void UpdateBlockMarkers()
    {
        if (DataContext is not NotebookEditorViewModel editor) return;
        foreach (var label in this.GetVisualDescendants().OfType<TextBlock>().Where(label => label.Name == "BlockOrderLabel"))
        {
            if (label.DataContext is not NotebookBlockEditor block) continue;
            var index = editor.Blocks.IndexOf(block);
            if (index >= 0) label.Text = (index + 1).ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
