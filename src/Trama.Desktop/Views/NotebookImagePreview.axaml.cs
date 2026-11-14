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

    public Task PendingLoads { get; private set; } = Task.CompletedTask;

    public NotebookImagePreview() => InitializeComponent();

    public Guid? AttachmentId { get => GetValue(AttachmentIdProperty); set => SetValue(AttachmentIdProperty, value); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        QueuePreviewLoad();
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
            QueuePreviewLoad();
        }
    }

    private void QueuePreviewLoad()
    {
        var current = LoadPreviewAsync();
        PendingLoads = PendingLoads.IsCompleted ? current : Task.WhenAll(PendingLoads, current);
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
        var token = loading.Token;
        Bitmap? decoded = null;
        try
        {
            await using (var stream = await editor.OpenImageAsync(id, token))
            {
                token.ThrowIfCancellationRequested();
                if (stream is null)
                {
                    ImageMessage.Text = "Imagem indisponível. A referência foi mantida na página.";
                    return;
                }

                decoded = await Task.Run(() => Bitmap.DecodeToWidth(stream, 1200), token);
            }

            token.ThrowIfCancellationRequested();
            _bitmap = decoded;
            decoded = null;
            PreviewImage.Source = _bitmap;
            ImageMessage.IsVisible = false;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer load or detachment owns the visual state now.
        }
        catch (Exception)
        {
            if (!token.IsCancellationRequested)
            {
                ImageMessage.Text = "Não foi possível exibir esta imagem. A referência foi mantida.";
            }
        }
        finally
        {
            decoded?.Dispose();
            if (ReferenceEquals(_loading, loading)) _loading = null;
            loading.Dispose();
        }
    }

    private void CancelLoading()
    {
        var loading = _loading;
        _loading = null;
        loading?.Cancel();
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
