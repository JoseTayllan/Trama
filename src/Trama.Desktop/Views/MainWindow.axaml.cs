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
        SizeChanged += (_, _) => Classes.Set("compact-home", Bounds.Width < 1080);
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
        if (!_closeApproved && DataContext is MainWindowViewModel viewModel && (viewModel.Library is not null || viewModel.Agenda is not null))
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

                _ = CheckCloseAsync(viewModel);
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

    private async Task CheckCloseAsync(MainWindowViewModel model)
    {
        bool approved;
        try
        {
            if (model.Closing is { } closing)
            {
                approved = await closing.RequestCloseAsync();
                if (!approved && !string.IsNullOrWhiteSpace(closing.Feedback)) model.Status = closing.Feedback;
            }
            else
            {
                approved = (model.Agenda is null || await model.Agenda.RequestCloseAsync()) &&
                    (model.Library is null || await model.Library.RequestCloseAsync());
            }
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
        if (IsVisible && DataContext is MainWindowViewModel model) model.Closing?.CancelPendingClose();
        _disabledContent?.SetCurrentValue(IsEnabledProperty, _contentWasEnabled);
        _disabledContent = null;
        _checkingClose = false;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel { Closing: { } closing } model)
        {
            model.Library?.Dispose();
            closing.Dispose();
        }
        base.OnClosed(e);
    }
}
