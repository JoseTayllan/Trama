using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookImagePreview : UserControl
{
    public static readonly StyledProperty<Guid?> AttachmentIdProperty =
        AvaloniaProperty.Register<NotebookImagePreview, Guid?>(nameof(AttachmentId));
    private CancellationTokenSource? _loading;
    private Bitmap? _bitmap;

    public NotebookImagePreview() => InitializeComponent();

    public Guid? AttachmentId { get => GetValue(AttachmentIdProperty); set => SetValue(AttachmentIdProperty, value); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _ = LoadPreviewAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelLoading();
        ClearBitmap();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AttachmentIdProperty && this.IsAttachedToVisualTree())
        {
            _ = LoadPreviewAsync();
        }
    }

    private async Task LoadPreviewAsync()
    {
        CancelLoading();
        ClearBitmap();
        if (AttachmentId is not { } id || this.FindAncestorOfType<NotebookEditorView>()?.DataContext is not NotebookEditorViewModel editor)
        {
            return;
        }

        var loading = new CancellationTokenSource();
        _loading = loading;
        ImageMessage.Text = "Abrindo imagem…";
        ImageMessage.IsVisible = true;
        try
        {
            await using var stream = await editor.OpenImageAsync(id, loading.Token);
            loading.Token.ThrowIfCancellationRequested();
            if (stream is null)
            {
                ImageMessage.Text = "Imagem indisponível. A referência foi mantida na página.";
                return;
            }

            var bitmap = await Task.Run(() => Bitmap.DecodeToWidth(stream, 1200), loading.Token);
            if (loading.IsCancellationRequested)
            {
                bitmap.Dispose();
                return;
            }

            _bitmap = bitmap;
            PreviewImage.Source = bitmap;
            ImageMessage.IsVisible = false;
        }
        catch (OperationCanceledException) when (loading.IsCancellationRequested)
        {
            // A newer load or detachment owns the visual state now.
        }
        catch (Exception)
        {
            if (!loading.IsCancellationRequested)
            {
                ImageMessage.Text = "Não foi possível exibir esta imagem. A referência foi mantida.";
            }
        }
    }

    private void CancelLoading()
    {
        _loading?.Cancel();
        _loading?.Dispose();
        _loading = null;
    }

    private void ClearBitmap()
    {
        if (PreviewImage is not null)
        {
            PreviewImage.Source = null;
        }

        _bitmap?.Dispose();
        _bitmap = null;
    }
}
