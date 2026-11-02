namespace Trama.Desktop.ViewModels;

public sealed partial class NotebookEditorViewModel
{
    private readonly TimeProvider? _autoSaveClock;
    private CancellationTokenSource? _autoSaveDelay;
    private bool _autoSaveDue;
    private bool _isAutomaticSave;
    private bool _disposed;
    private int _autoSavePauses;

    public bool IsAutoSaveEnabled => _autoSaveClock is not null;
    public bool CanEditContent => !_disposed && (!IsBusy || _isAutomaticSave);
    public string SaveModeLabel => IsAutoSaveEnabled ? "Salvamento automático · pausa de 2 s" : "Salvamento manual";

    private void ScheduleAutoSave()
    {
        if (_autoSaveClock is null || _disposed) return;
        CancelScheduledAutoSave();
        var delay = new CancellationTokenSource();
        _autoSaveDelay = delay;
        _ = WaitForAutoSaveAsync(delay);
    }

    private async Task WaitForAutoSaveAsync(CancellationTokenSource delay)
    {
        try
        {
            // Await captures the editor's UI context; the timer never edits controls directly.
            await Task.Delay(TimeSpan.FromSeconds(2), _autoSaveClock!, delay.Token);
            if (_disposed || !ReferenceEquals(_autoSaveDelay, delay) || !HasUnsavedChanges) return;
            _autoSaveDue = true;
            TrySaveWhenDue();
        }
        catch (OperationCanceledException) when (delay.IsCancellationRequested) { }
        finally
        {
            if (ReferenceEquals(_autoSaveDelay, delay)) _autoSaveDelay = null;
            delay.Dispose();
        }
    }

    private void TrySaveWhenDue()
    {
        if (!_autoSaveDue || _disposed || IsBusy || _autoSavePauses != 0 || !HasUnsavedChanges) return;
        _autoSaveDue = false;
        _ = SaveAutomaticallyAsync();
    }

    private async Task SaveAutomaticallyAsync()
    {
        _isAutomaticSave = true;
        OnPropertyChanged(nameof(CanEditContent));
        try { await SaveAsync(); }
        finally
        {
            _isAutomaticSave = false;
            OnPropertyChanged(nameof(CanEditContent));
            TrySaveWhenDue();
        }
    }

    private void CancelScheduledAutoSave()
    {
        _autoSaveDue = false;
        var delay = _autoSaveDelay;
        _autoSaveDelay = null;
        try { delay?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    internal IDisposable PauseAutoSave()
    {
        _autoSavePauses++;
        return new AutoSavePause(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelScheduledAutoSave();
    }

    private sealed class AutoSavePause(NotebookEditorViewModel editor) : IDisposable
    {
        private NotebookEditorViewModel? _editor = editor;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _editor, null);
            if (owner is null) return;
            owner._autoSavePauses--;
            owner.TrySaveWhenDue();
        }
    }
}
