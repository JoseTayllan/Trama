using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}

public interface IUnsavedChangesPrompt
{
    Task<UnsavedChangesChoice> ChooseAsync();
}

public sealed record NotebookDestination(KnowledgeSpace Space, NotebookInfo Notebook)
{
    public string Label => $"{Space.Title} / {Notebook.Title}";
}

public sealed partial class LibraryViewModel : ObservableObject, IDisposable
{
    private readonly ILibraryActions _actions;
    private readonly ILibraryManagementActions? _management;
    private readonly IUnsavedChangesPrompt _prompt;
    private readonly ObservableCollection<KnowledgeSpace> _spaces = [];
    private readonly ObservableCollection<NotebookInfo> _notebooks = [];
    private readonly ObservableCollection<KnowledgePageSummary> _pages = [];
    private NotebookEditorViewModel _editor;
    private string _newSpaceTitle = string.Empty;
    private string _newNotebookTitle = string.Empty;
    private string _feedback = "Carregando biblioteca…";
    private Guid? _selectedSpaceId;
    private Guid? _selectedNotebookId;
    private bool _isBusy;
    private bool _initialized;
    private readonly TimeProvider? _autoSaveClock;
    private readonly IMarkdownPageExporter? _markdownExporter;

    public LibraryViewModel(ILibraryActions actions, IUnsavedChangesPrompt unsavedChangesPrompt,
        ILibraryManagementActions? management = null, IKnowledgeDiscovery? discovery = null)
        : this(actions, unsavedChangesPrompt, management, discovery, null)
    {
    }

    public LibraryViewModel(ILibraryActions actions, IUnsavedChangesPrompt unsavedChangesPrompt,
        ILibraryManagementActions? management, IKnowledgeDiscovery? discovery, TimeProvider? autoSaveClock)
        : this(actions, unsavedChangesPrompt, management, discovery, autoSaveClock, null)
    {
    }

    public LibraryViewModel(ILibraryActions actions, IUnsavedChangesPrompt unsavedChangesPrompt,
        ILibraryManagementActions? management, IKnowledgeDiscovery? discovery, TimeProvider? autoSaveClock, ILibraryTrashActions? trash)
        : this(actions, unsavedChangesPrompt, management, discovery, autoSaveClock, trash, null)
    {
    }

    public LibraryViewModel(ILibraryActions actions, IUnsavedChangesPrompt unsavedChangesPrompt,
        ILibraryManagementActions? management, IKnowledgeDiscovery? discovery, TimeProvider? autoSaveClock,
        ILibraryTrashActions? trash, IMarkdownPageExporter? markdownExporter)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(unsavedChangesPrompt);
        _actions = actions;
        _management = management;
        _discovery = discovery;
        _prompt = unsavedChangesPrompt;
        _autoSaveClock = autoSaveClock;
        _trash = trash;
        _markdownExporter = markdownExporter;
        _editor = CreateEditor(null);
        _editor.PropertyChanged += EditorChanged;
        Spaces = new ReadOnlyObservableCollection<KnowledgeSpace>(_spaces);
        Notebooks = new ReadOnlyObservableCollection<NotebookInfo>(_notebooks);
        Pages = new ReadOnlyObservableCollection<KnowledgePageSummary>(_pages);
        CreateSpaceCommand = new AsyncRelayCommand(CreateSpaceAsync, () => CanNavigate);
        CreateNotebookCommand = new AsyncRelayCommand(CreateNotebookAsync, () => CanNavigate && SelectedSpaceId.HasValue);
        NewPageCommand = new AsyncRelayCommand(async () => { await NewPageAsync(); }, () => CanNavigate && CanShowEditor);
        ShowUnfiledCommand = new AsyncRelayCommand(async () => { await ShowUnfiledAsync(); }, () => CanNavigate);
        RefreshCommand = new AsyncRelayCommand(InitializeAsync, () => CanNavigate);
        SearchCommand = new AsyncRelayCommand(async () => { await SearchAsync(SearchQuery, SearchTag); }, () => CanSearch);
        ClearSearchCommand = new RelayCommand(() => { IsSearchOpen = false; SearchResults = Array.Empty<PageSearchResult>(); }, () => CanNavigate);
    }

    public string NewSpaceTitle { get => _newSpaceTitle; set => SetProperty(ref _newSpaceTitle, value ?? string.Empty); }
    public string NewNotebookTitle { get => _newNotebookTitle; set => SetProperty(ref _newNotebookTitle, value ?? string.Empty); }
    public Guid? SelectedSpaceId
    {
        get => _selectedSpaceId;
        private set
        {
            SetProperty(ref _selectedSpaceId, value);
            NotifySelectionChanged();
        }
    }

    public Guid? SelectedNotebookId
    {
        get => _selectedNotebookId;
        private set
        {
            SetProperty(ref _selectedNotebookId, value);
            NotifySelectionChanged();
        }
    }

    public KnowledgeSpace? SelectedSpace => _spaces.FirstOrDefault(space => space.Id == SelectedSpaceId);
    public NotebookInfo? SelectedNotebook => _notebooks.FirstOrDefault(book => book.Id == SelectedNotebookId);
    public KnowledgePageSummary? SelectedPage => _pages.FirstOrDefault(page => page.Id == Editor.PageId);
    public string LocationLabel => SelectedNotebook?.Title ?? (SelectedSpaceId.HasValue ? "Escolha um caderno" : "Não organizadas");
    public string ContextSpaceTitle => SelectedSpace?.Title ?? "Não organizadas";
    public string ContextNotebookTitle => SelectedNotebook?.Title ?? (SelectedSpaceId.HasValue ? "Escolha um caderno" : "Sem caderno");
    public string ContextPageTitle => !CanShowEditor ? "Nenhuma página aberta" :
        string.IsNullOrWhiteSpace(Editor.Title) ? "Sem título" : Editor.Title;
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value)) NotifyCommands();
        }
    }

    public bool CanNavigate => !IsBusy && !Editor.IsBusy;
    public bool CanShowEditor => !SelectedSpaceId.HasValue || SelectedNotebookId.HasValue;
    public bool CanEdit => !IsBusy && CanShowEditor;
    public NotebookEditorViewModel Editor
    {
        get => _editor;
        private set
        {
            if (ReferenceEquals(_editor, value)) return;
            _editor.PropertyChanged -= EditorChanged;
            _editor.Dispose();
            SetProperty(ref _editor, value);
            _editor.PropertyChanged += EditorChanged;
            CurrentPageTags = Array.Empty<string>();
            NotifySelectionChanged();
        }
    }

    public ReadOnlyObservableCollection<KnowledgeSpace> Spaces { get; }
    public ReadOnlyObservableCollection<NotebookInfo> Notebooks { get; }
    public ReadOnlyObservableCollection<KnowledgePageSummary> Pages { get; }
    public IAsyncRelayCommand CreateSpaceCommand { get; }
    public IAsyncRelayCommand CreateNotebookCommand { get; }
    public IAsyncRelayCommand NewPageCommand { get; }
    public IAsyncRelayCommand ShowUnfiledCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public Task<bool> RequestCloseAsync() => NavigateAsync(() => Task.FromResult(true), closing: true);

    internal Task<bool> PrepareCloseAsync() => NavigateAsync(() => Task.FromResult(true), closing: true, disposeOnClose: false);

    public bool CanManageSpace => CanNavigate && _management is not null && SelectedSpace is not null;
    public bool CanManageNotebook => CanNavigate && _management is not null && SelectedNotebook is not null;
    public bool CanManagePage => CanNavigate && _management is not null && SelectedPage is not null;

    public Task<bool> RenameSelectedSpaceAsync(string title) =>
        RenameParentAsync(LibraryEntryKind.Space, SelectedSpaceId, title);

    public Task<bool> RenameSelectedNotebookAsync(string title) =>
        RenameParentAsync(LibraryEntryKind.Notebook, SelectedNotebookId, title);

    private Task<bool> RenameParentAsync(LibraryEntryKind kind, Guid? id, string title)
    {
        if (_management is null || id is null || !ValidTitle(title)) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var normalized = title.Trim();
            await _management.RenameAsync(kind, id.Value, normalized);
            if (kind == LibraryEntryKind.Space)
                Replace(_spaces, _spaces.Select(item => item.Id == id ? item with { Title = normalized } : item).ToArray());
            else
                Replace(_notebooks, _notebooks.Select(item => item.Id == id ? item with { Title = normalized } : item).ToArray());
            NotifySelectionChanged();
            Feedback = "Nome atualizado. Seu conteúdo foi mantido.";
            return true;
        }, resolveChanges: false);
    }

    public Task<bool> RenameCurrentPageAsync(string title)
    {
        if (_management is null || SelectedPage is null || !ValidTitle(title)) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var page = await _actions.LoadAsync(Editor.PageId);
            if (page is null) return false;
            var normalized = title.Trim();
            var candidate = CreateEditor(SelectedNotebookId);
            candidate.RestoreSnapshot(page with { Title = normalized });
            var tags = CurrentPageTags;
            await _management.RenameAsync(LibraryEntryKind.Page, page.Id, normalized);
            Replace(_pages, _pages.Select(item => item.Id == page.Id ? item with { Title = normalized } : item)
                .OrderBy(item => item.Title).ThenBy(item => item.Id).ToArray());
            Editor = candidate;
            CurrentPageTags = tags;
            Feedback = "Página renomeada.";
            return true;
        });
    }

    public Task<bool> MoveCurrentPageAsync(Guid notebookId)
    {
        if (_management is null || SelectedPage is null) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var spaces = await _actions.ListSpacesAsync();
            NotebookDestination? destination = null;
            IReadOnlyList<NotebookInfo> books = Array.Empty<NotebookInfo>();
            foreach (var space in spaces)
            {
                books = await _actions.ListNotebooksAsync(space.Id);
                if (books.FirstOrDefault(book => book.Id == notebookId) is { } book)
                {
                    destination = new NotebookDestination(space, book);
                    break;
                }
            }
            if (destination is null)
            {
                Feedback = "O caderno de destino não está disponível. Sua página foi mantida.";
                return false;
            }

            // Prepare everything before writing so a read failure cannot leave a stale editor location.
            var pages = await _actions.ListPagesAsync(notebookId);
            var page = await _actions.LoadAsync(Editor.PageId);
            if (page is null) return false;
            var candidate = CreateEditor(notebookId);
            candidate.RestoreSnapshot(page);
            var tags = CurrentPageTags;
            var updated = pages.Where(item => item.Id != page.Id).Append(new KnowledgePageSummary(page.Id, page.Title))
                .OrderBy(item => item.Title).ThenBy(item => item.Id).ToArray();
            await _management.MovePageAsync(page.Id, notebookId);
            Replace(_spaces, spaces);
            Replace(_notebooks, books);
            Replace(_pages, updated);
            SelectedSpaceId = destination.Space.Id;
            SelectedNotebookId = notebookId;
            Editor = candidate;
            CurrentPageTags = tags;
            Feedback = "Página movida. Você pode continuar escrevendo neste caderno.";
            return true;
        });
    }

    public async Task<IReadOnlyList<NotebookDestination>> ListMoveDestinationsAsync()
    {
        if (!CanManagePage) return Array.Empty<NotebookDestination>();
        IsBusy = true;
        try
        {
            var destinations = new List<NotebookDestination>();
            foreach (var space in await _actions.ListSpacesAsync())
                foreach (var book in await _actions.ListNotebooksAsync(space.Id))
                    if (book.Id != SelectedNotebookId) destinations.Add(new NotebookDestination(space, book));
            return destinations;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível carregar os destinos. Sua página foi mantida.";
            return Array.Empty<NotebookDestination>();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidTitle(string title)
    {
        if (!string.IsNullOrWhiteSpace(title)) return true;
        Feedback = "Informe um nome válido para continuar.";
        return false;
    }

    public void ReportManagementFailure() => Feedback = "Não foi possível abrir a ação. Seu conteúdo foi mantido.";

    public async Task InitializeAsync()
    {
        if (!CanNavigate) return;
        using var autoSavePause = Editor.PauseAutoSave();
        IsBusy = true;
        try
        {
            var spaces = await _actions.ListSpacesAsync();
            var notebooks = SelectedSpaceId is { } spaceId
                ? await _actions.ListNotebooksAsync(spaceId)
                : Array.Empty<NotebookInfo>();
            var pages = SelectedNotebookId is { } id
                ? await _actions.ListPagesAsync(id)
                : await _actions.ListUnfiledPagesAsync();
            Replace(_spaces, spaces);
            Replace(_notebooks, notebooks);
            if (!_initialized || CanShowEditor) Replace(_pages, pages);
            _initialized = true;
            NotifySelectionChanged();
            Feedback = "Escolha um espaço e um caderno, ou use Não organizadas.";
        }
        catch (Exception)
        {
            Feedback = "Não foi possível carregar a biblioteca. Use Atualizar para tentar novamente.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task<bool> SelectSpaceAsync(Guid id) => NavigateAsync(async () =>
    {
        if (!_spaces.Any(space => space.Id == id)) return false;
        var books = await _actions.ListNotebooksAsync(id);
        Replace(_notebooks, books);
        _pages.Clear();
        SelectedSpaceId = id;
        SelectedNotebookId = null;
        Editor = CreateEditor(null);
        Feedback = "Escolha ou crie um caderno para começar.";
        return true;
    });

    public Task<bool> SelectNotebookAsync(Guid id) => NavigateAsync(async () =>
    {
        if (!_notebooks.Any(book => book.Id == id && book.SpaceId == SelectedSpaceId)) return false;
        var pages = await _actions.ListPagesAsync(id);
        Replace(_pages, pages);
        SelectedNotebookId = id;
        Editor = CreateEditor(id);
        Feedback = "Abra uma página da lista ou crie uma nova.";
        return true;
    });

    public Task<bool> OpenPageAsync(Guid id)
    {
        if (!CanNavigate) return Task.FromResult(false);
        if (id == Editor.PageId) return Task.FromResult(true);
        return NavigateAsync(async () =>
        {
            if (!_pages.Any(page => page.Id == id))
            {
                Feedback = "Página indisponível. Seu rascunho foi mantido.";
                return false;
            }

            var candidate = CreateEditor(SelectedNotebookId);
            if (!await candidate.TryLoadAsync(id))
            {
                Feedback = "Não foi possível abrir a página. Seu rascunho foi mantido.";
                return false;
            }

            var tags = _discovery is null ? Array.Empty<string>() : await _discovery.GetPageTagsAsync(id);
            Editor = candidate;
            CurrentPageTags = tags;
            Feedback = "Página aberta.";
            return true;
        });
    }

    public Task<bool> ShowUnfiledAsync() => NavigateAsync(async () =>
    {
        var pages = await _actions.ListUnfiledPagesAsync();
        _notebooks.Clear();
        Replace(_pages, pages);
        SelectedSpaceId = null;
        SelectedNotebookId = null;
        Editor = CreateEditor(null);
        Feedback = "Páginas ainda sem caderno.";
        return true;
    });

    public Task<bool> NewPageAsync() => !CanShowEditor ? Task.FromResult(false) : NavigateAsync(() =>
    {
        Editor = CreateEditor(SelectedNotebookId);
        Feedback = "Nova página. Salve quando terminar de escrever.";
        return Task.FromResult(true);
    });

    private async Task CreateSpaceAsync() => await NavigateAsync(async () =>
    {
        var space = await _actions.CreateSpaceAsync(NewSpaceTitle);
        _spaces.Add(space);
        _notebooks.Clear();
        _pages.Clear();
        SelectedSpaceId = space.Id;
        SelectedNotebookId = null;
        Editor = CreateEditor(null);
        NewSpaceTitle = string.Empty;
        Feedback = "Espaço criado. Adicione seu primeiro caderno.";
        return true;
    });

    private async Task CreateNotebookAsync()
    {
        if (SelectedSpaceId is not { } spaceId) return;
        await NavigateAsync(async () =>
        {
            var book = await _actions.CreateNotebookAsync(spaceId, NewNotebookTitle);
            _notebooks.Add(book);
            _pages.Clear();
            SelectedNotebookId = book.Id;
            Editor = CreateEditor(book.Id);
            NewNotebookTitle = string.Empty;
            Feedback = "Caderno criado. Sua primeira página está pronta para escrever.";
            return true;
        });
    }

    private async Task<bool> NavigateAsync(Func<Task<bool>> navigate, bool closing = false, bool resolveChanges = true, bool disposeOnClose = true)
    {
        if (!CanNavigate) return false;
        using var autoSavePause = Editor.PauseAutoSave();
        IsBusy = true;
        try
        {
            if (resolveChanges && Editor.HasUnsavedChanges)
            {
                var choice = await _prompt.ChooseAsync();
                if (choice == UnsavedChangesChoice.Cancel) return false;
                if (choice == UnsavedChangesChoice.Save)
                {
                    await Editor.SaveCommand.ExecuteAsync(null);
                    if (Editor.LastSaveStatus != NotebookSaveStatus.Saved || Editor.HasUnsavedChanges)
                    {
                        Feedback = closing
                            ? "A página ainda tem alterações pendentes. O fechamento foi cancelado."
                            : "A página ainda tem alterações pendentes. A navegação foi cancelada.";
                        return false;
                    }
                }
                else if (choice != UnsavedChangesChoice.Discard)
                {
                    return false;
                }
            }

            var completed = await navigate();
            if (completed && closing && disposeOnClose) Editor.Dispose();
            return completed;
        }
        catch (ArgumentException)
        {
            Feedback = closing
                ? "Não foi possível concluir o fechamento. Seu conteúdo foi mantido."
                : "Informe um nome válido para continuar.";
            return false;
        }
        catch (Exception)
        {
            Feedback = closing
                ? "Não foi possível concluir o fechamento. Seu conteúdo foi mantido."
                : "Não foi possível concluir a operação. Seu conteúdo atual foi mantido.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private NotebookEditorViewModel CreateEditor(Guid? notebookId)
    {
        var actions = new LocatedEditorActions(_actions, notebookId, RefreshPagesAfterSaveAsync);
        if (_markdownExporter is not null) return new(actions, _markdownExporter, _autoSaveClock);
        return _autoSaveClock is null ? new(actions) : new(actions, _autoSaveClock);
    }

    public void Dispose()
    {
        _editor.PropertyChanged -= EditorChanged;
        _editor.Dispose();
    }

    private async Task RefreshPagesAfterSaveAsync()
    {
        try
        {
            var pages = SelectedNotebookId is { } id
                ? await _actions.ListPagesAsync(id)
                : await _actions.ListUnfiledPagesAsync();
            Replace(_pages, pages);
            OnPropertyChanged(nameof(SelectedPage));
            NotifyCommands();
        }
        catch (Exception)
        {
            Feedback = "Página salva, mas não foi possível atualizar a lista. Use Atualizar.";
        }
    }

    private void EditorChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotebookEditorViewModel.Title)) OnPropertyChanged(nameof(ContextPageTitle));
        NotifyCommands();
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedSpace));
        OnPropertyChanged(nameof(SelectedNotebook));
        OnPropertyChanged(nameof(SelectedPage));
        OnPropertyChanged(nameof(LocationLabel));
        OnPropertyChanged(nameof(ContextSpaceTitle));
        OnPropertyChanged(nameof(ContextNotebookTitle));
        OnPropertyChanged(nameof(ContextPageTitle));
        OnPropertyChanged(nameof(CanShowEditor));
        NotifyCommands();
    }

    private void NotifyCommands()
    {
        OnPropertyChanged(nameof(CanNavigate));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanManageSpace));
        OnPropertyChanged(nameof(CanManageNotebook));
        OnPropertyChanged(nameof(CanManagePage));
        OnPropertyChanged(nameof(CanSearch));
        OnPropertyChanged(nameof(CanEditTags));
        OnPropertyChanged(nameof(CanTrashSpace));
        OnPropertyChanged(nameof(CanTrashNotebook));
        OnPropertyChanged(nameof(CanTrashPage));
        CreateSpaceCommand.NotifyCanExecuteChanged();
        CreateNotebookCommand.NotifyCanExecuteChanged();
        NewPageCommand.NotifyCanExecuteChanged();
        ShowUnfiledCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        SearchCommand.NotifyCanExecuteChanged();
        ClearSearchCommand.NotifyCanExecuteChanged();
    }

    private static void Replace<T>(ObservableCollection<T> destination, IEnumerable<T> items)
    {
        destination.Clear();
        foreach (var item in items) destination.Add(item);
    }

    private sealed class LocatedEditorActions(ILibraryActions actions, Guid? notebookId, Func<Task> afterSave) : INotebookActions
    {
        public async Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            var result = await actions.SaveInNotebookAsync(notebook, notebookId, cancellationToken);
            if (result.Status == NotebookSaveStatus.Saved) await afterSave();
            return result;
        }

        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => actions.LoadAsync(id, cancellationToken);
        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => actions.ImportImageAsync(source, cancellationToken);
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => actions.OpenImageAsync(id, cancellationToken);
    }
}

internal sealed class CancelUnsavedChangesPrompt : IUnsavedChangesPrompt
{
    public Task<UnsavedChangesChoice> ChooseAsync() => Task.FromResult(UnsavedChangesChoice.Cancel);
}
