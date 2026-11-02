using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class MarkdownExportDialog : Window
{
    private CancellationTokenSource? _exportCancellation;
    private bool _closeRequested;
    private bool _choosing;
    public MarkdownExportDialog() => InitializeComponent();
    public MarkdownExportDialog(MarkdownExportViewModel viewModel) : this() => DataContext = viewModel;

    private async void ConfirmExportClicked(object? sender, RoutedEventArgs e)
    {
        if (_choosing || _exportCancellation is not null || DataContext is not MarkdownExportViewModel viewModel) return;
        using var cancellation = new CancellationTokenSource();
        _exportCancellation = cancellation;
        try { await viewModel.ExportAsync(cancellation.Token); }
        finally
        {
            _exportCancellation = null;
            if (_closeRequested) Close();
        }
    }

    private void CancelExportClicked(object? sender, RoutedEventArgs e)
    {
        if (_choosing) return;
        if (_exportCancellation is null) Close();
        else { _closeRequested = true; _exportCancellation.Cancel(); }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_choosing || _exportCancellation is not null)
        {
            e.Cancel = true;
            if (_exportCancellation is not null) { _closeRequested = true; _exportCancellation.Cancel(); }
        }
        base.OnClosing(e);
    }

    private async void ChooseDestinationClicked(object? sender, RoutedEventArgs e)
    {
        if (_choosing || _exportCancellation is not null || DataContext is not MarkdownExportViewModel viewModel) return;
        _choosing = true;
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Onde criar a pasta da página?", AllowMultiple = false });
            foreach (var folder in folders)
            {
                using (folder)
                {
                    if (folder.TryGetLocalPath() is { } path)
                        viewModel.Destination = Path.Combine(path, "Pagina-Trama-" + Guid.NewGuid().ToString("N")[..8]);
                }
            }
        }
        catch (OperationCanceledException) { /* Keep the chosen destination unchanged. */ }
        catch (Exception) { viewModel.ReportSelectionFailure(); }
        finally { _choosing = false; }
    }
}
