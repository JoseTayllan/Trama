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
    public bool HasAgenda => Agenda is not null;
    private void AgendaChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AgendaViewModel.IsBusy)) NotifyModuleNavigation();
    }
}
