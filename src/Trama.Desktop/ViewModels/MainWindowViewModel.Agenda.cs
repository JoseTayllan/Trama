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
        }
    }
    internal async Task OpenAgendaLinkedPageAsync(Guid? pageId = null)
    {
        if (Agenda is not { } agenda || Library is not { } library || !IsAgendaActive || !CanNavigateModules) return;
        var opened = pageId is { } id
            ? await agenda.OpenLinkedPageAsync(id, OpenPageAsync)
            : await agenda.OpenLinkedPageAsync(OpenPageAsync);

        async Task<string?> OpenPageAsync(Guid target) =>
            await library.OpenAgendaLinkedPageAsync(target) ? null : library.Feedback;

        if (opened && ReferenceEquals(Agenda, agenda) && ReferenceEquals(Library, library)) SelectModule(Module.Notebooks);
    }

    public bool HasAgenda => Agenda is not null;
    private void AgendaChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AgendaViewModel.IsBusy)) NotifyModuleNavigation();
    }
}
