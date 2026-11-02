using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed record TrashItemViewModel(LibraryTrashEntry Entry)
{
    public string Title => Entry.Title;
    public string KindLabel => Entry.Kind switch { LibraryEntryKind.Space => "Espaço", LibraryEntryKind.Notebook => "Caderno", _ => "Página" };
    public string DeletedLabel => Entry.DeletedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.CurrentCulture);
}

public sealed class TrashViewModel : ObservableObject
{
    private readonly LibraryViewModel _library;
    private TrashItemViewModel? _selected;
    private bool _busy;
    private string _feedback = "Selecione um item para restaurar. Nada será apagado definitivamente.";

    public TrashViewModel(LibraryViewModel library)
    {
        _library = library;
        RestoreCommand = new AsyncRelayCommand(RestoreAsync, () => CanRestore);
    }

    public ObservableCollection<TrashItemViewModel> Entries { get; } = [];
    public IAsyncRelayCommand RestoreCommand { get; }
    public bool CanRestore => !IsBusy && SelectedEntry?.Entry.CanRestore == true;
    public bool IsBusy
    {
        get => _busy;
        private set
        {
            if (!SetProperty(ref _busy, value)) return;
            OnPropertyChanged(nameof(CanRestore));
            RestoreCommand.NotifyCanExecuteChanged();
        }
    }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }
    public TrashItemViewModel? SelectedEntry
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;
            Feedback = value is null ? "Selecione um item para restaurar." : value.Entry.CanRestore
                ? "O item voltará ao seu local original." : "Restaure primeiro o espaço ou caderno de origem.";
            OnPropertyChanged(nameof(CanRestore));
            RestoreCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var entries = await _library.ListTrashAsync();
            Entries.Clear();
            foreach (var entry in entries) Entries.Add(new(entry));
            SelectedEntry = null;
            if (entries.Count == 0) Feedback = "A lixeira está vazia.";
        }
        catch (Exception) { Feedback = "Não foi possível carregar a lixeira. Tente abrir novamente."; }
        finally { IsBusy = false; }
    }

    private async Task RestoreAsync()
    {
        if (SelectedEntry is not { } selected) return;
        IsBusy = true;
        try
        {
            if (await _library.RestoreTrashEntryAsync(selected.Entry))
            {
                await LoadAsync();
                Feedback = "Item restaurado. Você pode abri-lo pela biblioteca.";
            }
            else Feedback = "Não foi possível restaurar. Verifique se a origem já foi restaurada.";
        }
        catch (Exception) { Feedback = "Não foi possível restaurar. O item foi mantido na lixeira."; }
        finally { IsBusy = false; }
    }
}
