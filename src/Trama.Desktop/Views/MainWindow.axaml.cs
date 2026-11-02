using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class MainWindow : Window
{
    private bool _checkingClose;
    private bool _closeApproved;
    private Control? _disabledContent;
    private bool _contentWasEnabled;
    private bool _backupDialogOpen;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_backupDialogOpen || DataContext is MainWindowViewModel { Backup.IsBusy: true })
        {
            e.Cancel = true;
            base.OnClosing(e);
            return;
        }
        if (!_closeApproved && DataContext is MainWindowViewModel { Library: { } library })
        {
            e.Cancel = true;
            if (!_checkingClose)
            {
                _checkingClose = true;
                _disabledContent = Content as Control;
                if (_disabledContent is not null)
                {
                    _contentWasEnabled = _disabledContent.IsEnabled;
                    _disabledContent.SetCurrentValue(IsEnabledProperty, false);
                }

                _ = CheckCloseAsync(library);
            }
        }

        base.OnClosing(e);
    }

    private async void OpenBackupClicked(object? sender, RoutedEventArgs e) => await OpenBackupAsync();

    internal async Task OpenBackupAsync()
    {
        if (_backupDialogOpen || DataContext is not MainWindowViewModel { Backup: { } backup, Library: { CanNavigate: true } } viewModel) return;
        _backupDialogOpen = true;
        try { await new BackupDialog(backup).ShowDialog(this); }
        catch (Exception) { viewModel.Status = "Não foi possível abrir o painel de backup. Sua biblioteca foi mantida."; }
        finally { _backupDialogOpen = false; }
    }

    private async Task CheckCloseAsync(LibraryViewModel library)
    {
        bool approved;
        try
        {
            approved = await library.RequestCloseAsync();
        }
        catch (Exception)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Status = "Não foi possível fechar com segurança. Seu conteúdo foi mantido.";
            }

            RestoreAfterCloseRequest();
            return;
        }

        if (!approved || !IsVisible)
        {
            RestoreAfterCloseRequest();
            return;
        }

        // Finish the canceled Closing event before issuing the authorized close.
        // Keep editing blocked until this queued close actually runs.
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (IsVisible)
                {
                    _closeApproved = true;
                    Close();
                }
            }
            finally
            {
                _closeApproved = false;
                RestoreAfterCloseRequest();
            }
        });
    }

    private void RestoreAfterCloseRequest()
    {
        _disabledContent?.SetCurrentValue(IsEnabledProperty, _contentWasEnabled);
        _disabledContent = null;
        _checkingClose = false;
    }
}
