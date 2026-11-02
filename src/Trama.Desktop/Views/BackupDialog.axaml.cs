using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class BackupDialog : Window
{
    private bool _choosing;
    private static readonly FilePickerFileType BackupType = new("Backup do Trama") { Patterns = ["*.trama-backup"] };
    public BackupDialog() => InitializeComponent();
    public BackupDialog(BackupViewModel viewModel) : this() => DataContext = viewModel;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_choosing || DataContext is BackupViewModel { IsBusy: true }) e.Cancel = true;
        base.OnClosing(e);
    }

    private void CloseClicked(object? sender, RoutedEventArgs e) => Close();
    private async void CreateClicked(object? sender, RoutedEventArgs e)
    {
        if (!_choosing && DataContext is BackupViewModel viewModel)
            await viewModel.CreateAsync((ArchiveOutputInput.Text ?? string.Empty).Trim());
    }
    private async void RestoreClicked(object? sender, RoutedEventArgs e)
    {
        if (!_choosing && DataContext is BackupViewModel viewModel)
            await viewModel.RestoreAsync((RestoreArchiveInput.Text ?? string.Empty).Trim(), (RestoreDirectoryInput.Text ?? string.Empty).Trim());
    }

    private async void ChooseOutputClicked(object? sender, RoutedEventArgs e) => await ChooseAsync(async () =>
    {
        if (!StorageProvider.CanSave) throw new IOException("File selection is unavailable.");
        using var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Criar novo backup",
            SuggestedFileName = $"Trama-{DateTime.Now:yyyyMMdd-HHmmss}.trama-backup",
            DefaultExtension = "trama-backup",
            FileTypeChoices = [BackupType],
            ShowOverwritePrompt = false
        });
        if (file is not null) ArchiveOutputInput.Text = file.TryGetLocalPath() ?? throw new IOException("A local file is required.");
    });

    private async void ChooseArchiveClicked(object? sender, RoutedEventArgs e) => await ChooseAsync(async () =>
    {
        if (!StorageProvider.CanOpen) throw new IOException("File selection is unavailable.");
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar backup",
            AllowMultiple = false,
            FileTypeFilter = [BackupType]
        });
        if (files.Count == 0) return;
        using var file = files[0];
        RestoreArchiveInput.Text = file.TryGetLocalPath() ?? throw new IOException("A local file is required.");
    });

    private async void ChooseFolderClicked(object? sender, RoutedEventArgs e) => await ChooseAsync(async () =>
    {
        if (!StorageProvider.CanPickFolder) throw new IOException("Folder selection is unavailable.");
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Escolha onde criar a pasta restaurada", AllowMultiple = false });
        if (folders.Count == 0) return;
        using var folder = folders[0];
        var parent = folder.TryGetLocalPath() ?? throw new IOException("A local directory is required.");
        RestoreDirectoryInput.Text = Path.Combine(parent, $"Trama-Restaurado-{Guid.NewGuid().ToString("N")[..8]}");
    });

    private async Task ChooseAsync(Func<Task> choose)
    {
        if (_choosing || DataContext is not BackupViewModel viewModel || viewModel.IsBusy) return;
        _choosing = true;
        try { await choose(); }
        catch (OperationCanceledException) { /* Native picker cancellation leaves the fields unchanged. */ }
        catch (Exception) { viewModel.ReportSelectionFailure(); }
        finally { _choosing = false; }
    }
}
