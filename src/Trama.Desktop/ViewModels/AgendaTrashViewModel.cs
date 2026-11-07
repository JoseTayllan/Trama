using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed class AgendaTrashViewModel : ObservableObject
{
    private readonly AgendaViewModel _agenda;
    private AgendaTaskTrashEntry? _selectedEntry;
    private bool _isBusy;
    private bool _hasLoadError;
    private string _feedback = string.Empty;

    public AgendaTrashViewModel(AgendaViewModel agenda)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        _agenda = agenda;
        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync, () => CanRestore);
        Entries.CollectionChanged += (_, _) => NotifyState();
    }

    public ObservableCollection<AgendaTaskTrashEntry> Entries { get; } = [];
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand RestoreCommand { get; }
    public bool IsEmpty => !IsBusy && !HasLoadError && Entries.Count == 0;
    public bool CanRestore => !IsBusy && !HasLoadError && SelectedEntry is { } entry && Entries.Contains(entry);
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            NotifyState(); RefreshCommand.NotifyCanExecuteChanged();
        }
    }
    public bool HasLoadError
    {
        get => _hasLoadError;
        private set { if (SetProperty(ref _hasLoadError, value)) NotifyState(); }
    }
    public AgendaTaskTrashEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (!SetProperty(ref _selectedEntry, value)) return;
            NotifyState();
            if (!IsBusy && !HasLoadError)
                Feedback = value is null ? "Selecione uma tarefa para restaurar." : "A tarefa selecionada será restaurada com as datas e o estado originais.";
        }
    }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            Replace(await _agenda.ListTrashAsync()); HasLoadError = false;
            Feedback = Entries.Count == 0 ? "Nenhuma tarefa na lixeira." : "Selecione uma tarefa para restaurar.";
        }
        catch (Exception)
        {
            SelectedEntry = null; Entries.Clear(); HasLoadError = true;
            Feedback = "Não foi possível carregar a lixeira. Use Atualizar para tentar novamente.";
        }
        finally { IsBusy = false; }
    }

    private async Task RestoreAsync()
    {
        if (!CanRestore || SelectedEntry is not { } selected) return;
        IsBusy = true;
        var restored = false;
        try
        {
            restored = await _agenda.RestoreTrashEntryAsync(selected);
            if (!restored) { Feedback = _agenda.Feedback; return; }
            Replace(await _agenda.ListTrashAsync()); HasLoadError = false;
            Feedback = "Tarefa restaurada. Seu planejamento e estado foram preservados.";
        }
        catch (Exception)
        {
            if (restored)
            {
                SelectedEntry = null; HasLoadError = true;
                Feedback = "Tarefa restaurada; não foi possível atualizar a lixeira. Use Atualizar para consultar.";
            }
            else Feedback = "Não foi possível restaurar. O item foi mantido na lixeira.";
        }
        finally { IsBusy = false; }
    }

    private void Replace(IReadOnlyList<AgendaTaskTrashEntry> entries)
    {
        SelectedEntry = null; Entries.Clear();
        foreach (var entry in entries) Entries.Add(entry);
    }
    private void NotifyState()
    {
        OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(CanRestore)); RestoreCommand.NotifyCanExecuteChanged();
    }
}
