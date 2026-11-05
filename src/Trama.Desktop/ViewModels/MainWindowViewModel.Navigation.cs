using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Trama.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    private enum Module { Home, Notebooks, Agenda }
    private Module _module = Module.Home;

    public MainWindowViewModel()
    {
        ShowHomeCommand = new RelayCommand(() => SelectModule(Module.Home), () => CanNavigateModules);
        ShowNotebooksCommand = new RelayCommand(() => SelectModule(Module.Notebooks), () => CanNavigateModules);
        ShowAgendaCommand = new RelayCommand(() => SelectModule(Module.Agenda), () => CanOpenAgenda);
    }

    public IRelayCommand ShowHomeCommand { get; }
    public IRelayCommand ShowNotebooksCommand { get; }
    public IRelayCommand ShowAgendaCommand { get; }
    public bool CanNavigateModules => Library?.CanNavigate == true && Agenda?.IsBusy != true;
    public bool CanOpenAgenda => HasAgenda && CanNavigateModules;
    public bool IsHomeActive => HasLibrary && _module == Module.Home;
    public bool IsNotebooksActive => HasLibrary && _module == Module.Notebooks;
    public bool IsAgendaActive => HasAgenda && _module == Module.Agenda;
    public bool IsWorkspaceVisible => !HasLibrary || IsNotebooksActive;
    public bool HasNotebookToContinue => Library is { } library &&
        (library.SelectedPage is not null || library.Editor.HasUnsavedChanges);
    public string ContinueNotebookTitle => Library?.ContextPageTitle ?? "Sem título";
    public string ContinueNotebookLocation => Library is { } library
        ? $"{library.ContextSpaceTitle} / {library.ContextNotebookTitle}" : string.Empty;
    public string ContinueNotebookStatus => Library?.Editor.HasUnsavedChanges == true
        ? "Rascunho em andamento" : "Página aberta";

    private void SelectModule(Module module)
    {
        if (!CanNavigateModules || _module == module || (module == Module.Agenda && !HasAgenda)) return;
        _module = module;
        NotifyModuleNavigation();
    }

    private void ObserveLibraryNavigation(LibraryViewModel? previous, LibraryViewModel? current)
    {
        if (previous is not null) previous.PropertyChanged -= LibraryNavigationChanged;
        if (current is not null) current.PropertyChanged += LibraryNavigationChanged;
        _module = Module.Home;
        NotifyModuleNavigation();
    }

    private void LibraryNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LibraryViewModel.CanNavigate) or nameof(LibraryViewModel.ContextPageTitle)
            or nameof(LibraryViewModel.SelectedPage) or nameof(LibraryViewModel.Editor)) NotifyModuleNavigation();
    }

    private void NotifyModuleNavigation()
    {
        OnPropertyChanged(nameof(CanNavigateModules));
        OnPropertyChanged(nameof(IsHomeActive));
        OnPropertyChanged(nameof(IsNotebooksActive));
        OnPropertyChanged(nameof(IsAgendaActive));
        OnPropertyChanged(nameof(CanOpenAgenda));
        OnPropertyChanged(nameof(HasAgenda));
        OnPropertyChanged(nameof(IsWorkspaceVisible));
        OnPropertyChanged(nameof(HasNotebookToContinue));
        OnPropertyChanged(nameof(ContinueNotebookTitle));
        OnPropertyChanged(nameof(ContinueNotebookLocation));
        OnPropertyChanged(nameof(ContinueNotebookStatus));
        ShowHomeCommand.NotifyCanExecuteChanged();
        ShowNotebooksCommand.NotifyCanExecuteChanged();
        ShowAgendaCommand.NotifyCanExecuteChanged();
    }
}
