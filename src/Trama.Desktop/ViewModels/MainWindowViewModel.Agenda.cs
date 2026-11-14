namespace Trama.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    private AgendaViewModel? _agenda;
    public AgendaViewModel? Agenda
    {
        get => _agenda;
        set
        {
            var previous = _agenda;
            if (!SetProperty(ref _agenda, value)) return;
            if (previous is not null) previous.PropertyChanged -= AgendaChanged;
            if (value is not null) value.PropertyChanged += AgendaChanged;
            if (value is null && _module == Module.Agenda) _module = Module.Home;
            NotifyModuleNavigation();
            _ = RefreshHomeAgendaAsync();
        }
    }
    internal async Task OpenAgendaLinkedPageAsync(Guid? pageId = null)
    {
        if (Agenda is not { } agenda || Library is not { } library || !CanNavigateModules ||
            (!IsAgendaActive && !(IsHomeActive && pageId.HasValue))) return;
        var opened = pageId is { } id
            ? await agenda.OpenLinkedPageAsync(id, OpenPageAsync)
            : await agenda.OpenLinkedPageAsync(OpenPageAsync);

        async Task<string?> OpenPageAsync(Guid target) =>
            await library.OpenAgendaLinkedPageAsync(target) ? null : library.Feedback;

        if (!ReferenceEquals(Agenda, agenda) || !ReferenceEquals(Library, library)) return;
        if (opened) SelectModule(Module.Notebooks);
        else if (IsHomeActive) Status = agenda.Feedback;
    }

    public bool HasAgenda => Agenda is not null;
    private void AgendaChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AgendaViewModel.IsBusy)) NotifyModuleNavigation();
    }
}
