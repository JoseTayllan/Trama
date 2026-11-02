using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookEditorView : UserControl
{
    private bool _pickingImage;
    public NotebookEditorView()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, ImageDragOver);
        AddHandler(DragDrop.DropEvent, ImagesDropped);
        AddHandler(KeyDownEvent, PasteImageKeyDown, RoutingStrategies.Tunnel);
        AddHandler(GotFocusEvent, FormattingFocusChanged, RoutingStrategies.Bubble, handledEventsToo: true);
        DataContextChanged += (_, _) => { SetActiveProse(null); PageScroll.Offset = default; ObserveBlockMarkers(); };
        AttachedToVisualTree += (_, _) => ObserveBlockMarkers();
        SizeChanged += (_, _) => UpdateFormattingLayout();
        DetachedFromVisualTree += (_, _) => { SetActiveProse(null); ObserveBlockMarkers(detach: true); };
    }

    public NotebookEditorView(NotebookEditorViewModel viewModel) : this() => DataContext = viewModel;

    private void BoldClicked(object? sender, RoutedEventArgs e) => _activeProse?.ApplyStyle(InlineTextStyle.Bold);
    private void ItalicClicked(object? sender, RoutedEventArgs e) => _activeProse?.ApplyStyle(InlineTextStyle.Italic);

    private async void PasteImageClicked(object? sender, RoutedEventArgs e) => await PasteImageAsync();
    private async void PasteImageKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.V || e.KeyModifiers != (KeyModifiers.Control | KeyModifiers.Shift)) return;
        e.Handled = true;
        await PasteImageAsync();
    }
    private async Task PasteImageAsync()
    {
        if (!_pickingImage && DataContext is NotebookEditorViewModel editor)
            await editor.PasteImageAsync(new AvaloniaClipboardImageSource(TopLevel.GetTopLevel(this)?.Clipboard));
    }
    private void ImageDragOver(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Formats.Contains(DataFormat.File)) return;
        e.DragEffects = DataContext is NotebookEditorViewModel { IsBusy: false }
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void ImagesDropped(object? sender, DragEventArgs e)
    {
        if (_pickingImage || DataContext is not NotebookEditorViewModel { IsBusy: false } editor) return;
        var files = e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>().ToArray() ?? [];
        if (files.Length == 0) return;
        e.Handled = true;
        try
        {
            await editor.ImportDroppedImagesAsync(files.Select(file => new EditorImageInput(file.Name, _ => file.OpenReadAsync())).ToArray());
        }
        finally { foreach (var file in files) file.Dispose(); }
    }

    private async void ImportImageClicked(object? sender, RoutedEventArgs e)
    {
        if (_pickingImage || DataContext is not NotebookEditorViewModel editor || editor.IsBusy)
        {
            return;
        }

        _pickingImage = true;
        try
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (provider is null || !provider.CanOpen)
            {
                editor.ReportImageSelectionFailure();
                return;
            }

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Adicionar imagem à página",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("PNG ou JPEG")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg"], MimeTypes = ["image/png", "image/jpeg"]
                }]
            });
            if (files.Count == 0)
            {
                return;
            }

            using var file = files[0];
            await using var stream = await file.OpenReadAsync();
            await editor.ImportImageAsync(stream);
        }
        catch (OperationCanceledException)
        {
            // Closing the native picker does not change the page.
        }
        catch (Exception)
        {
            editor.ReportImageSelectionFailure();
        }
        finally
        {
            _pickingImage = false;
        }
    }
}
