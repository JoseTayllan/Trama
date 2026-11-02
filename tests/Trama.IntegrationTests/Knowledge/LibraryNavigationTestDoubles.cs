using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

internal sealed class NavigationPrompt(UnsavedChangesChoice choice = UnsavedChangesChoice.Cancel) : IUnsavedChangesPrompt
{
    public int Calls { get; private set; }
    public Task<UnsavedChangesChoice> ChooseAsync()
    {
        Calls++;
        return Task.FromResult(choice);
    }
}

internal sealed class NavigationActions : ILibraryActions
{
    public KnowledgeSpace Course { get; } = new(Guid.NewGuid(), "Curso C#");
    public NotebookInfo Notebook { get; }
    public NotebookInfo OtherNotebook { get; }
    public NotebookSnapshot FirstPage { get; } = Page("Variáveis");
    public NotebookSnapshot SecondPage { get; } = Page("Tipos");
    public NotebookSnapshot UnfiledPage { get; } = Page("Anotação solta");
    public List<KnowledgeSpace> Spaces { get; } = [];
    public List<NotebookInfo> Notebooks { get; } = [];
    public Dictionary<Guid, NotebookSnapshot> Stored { get; } = [];
    public Dictionary<Guid, Guid?> Locations { get; } = [];
    public List<(NotebookSnapshot Page, Guid? NotebookId)> Saved { get; } = [];
    public NotebookSaveResult SaveResult { get; set; } = new(NotebookSaveStatus.Saved);
    public Task<NotebookSaveResult>? PendingSave { get; set; }
    public bool FailCatalogRead { get; set; }
    public bool FailCreation { get; set; }
    public int LoadCalls { get; private set; }

    public NavigationActions()
    {
        Notebook = new NotebookInfo(Guid.NewGuid(), Course.Id, "Fundamentos");
        OtherNotebook = new NotebookInfo(Guid.NewGuid(), Course.Id, "Projetos");
        Spaces.Add(Course);
        Notebooks.AddRange([Notebook, OtherNotebook]);
        foreach (var page in new[] { FirstPage, SecondPage, UnfiledPage })
        {
            Stored.Add(page.Id, page);
            Locations.Add(page.Id, page == UnfiledPage ? null : Notebook.Id);
        }
    }

    public Task<KnowledgeSpace> CreateSpaceAsync(string title, CancellationToken cancellationToken = default)
    {
        if (FailCreation) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
        var space = new KnowledgeSpace(Guid.NewGuid(), title.Trim());
        Spaces.Add(space);
        return Task.FromResult(space);
    }

    public Task<NotebookInfo> CreateNotebookAsync(Guid spaceId, string title, CancellationToken cancellationToken = default)
    {
        if (FailCreation) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
        var notebook = new NotebookInfo(Guid.NewGuid(), spaceId, title.Trim());
        Notebooks.Add(notebook);
        return Task.FromResult(notebook);
    }

    public Task<IReadOnlyList<KnowledgeSpace>> ListSpacesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<KnowledgeSpace>>(Spaces.ToArray());
    public Task<IReadOnlyList<NotebookInfo>> ListNotebooksAsync(Guid spaceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotebookInfo>>(Notebooks.Where(book => book.SpaceId == spaceId).ToArray());

    public Task<IReadOnlyList<KnowledgePageSummary>> ListPagesAsync(Guid notebookId, CancellationToken cancellationToken = default) => ListAsync(notebookId);
    public Task<IReadOnlyList<KnowledgePageSummary>> ListUnfiledPagesAsync(CancellationToken cancellationToken = default) => ListAsync(null);

    private Task<IReadOnlyList<KnowledgePageSummary>> ListAsync(Guid? notebookId) => FailCatalogRead
        ? Task.FromException<IReadOnlyList<KnowledgePageSummary>>(new NotePersistenceException("PRIVATE_TEST_DETAIL"))
        : Task.FromResult<IReadOnlyList<KnowledgePageSummary>>(Stored.Values.Where(page => Locations[page.Id] == notebookId)
            .Select(page => new KnowledgePageSummary(page.Id, page.Title)).ToArray());

    public async Task<NotebookSaveResult> SaveInNotebookAsync(NotebookSnapshot page, Guid? notebookId, CancellationToken cancellationToken = default)
    {
        Saved.Add((page, notebookId));
        var result = PendingSave is null ? SaveResult : await PendingSave;
        if (result.Status == NotebookSaveStatus.Saved)
        {
            Stored[page.Id] = page;
            Locations[page.Id] = notebookId ?? Locations.GetValueOrDefault(page.Id);
        }

        return result;
    }

    public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default) => SaveInNotebookAsync(notebook, null, cancellationToken);

    public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        LoadCalls++;
        return Task.FromResult(Stored.GetValueOrDefault(id));
    }

    public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => Task.FromResult(new ImageAttachment(Guid.NewGuid(), "image/png"));
    public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);

    private static NotebookSnapshot Page(string title) => new(Guid.NewGuid(), title, [new NotebookTextBlock(Guid.NewGuid(), "  Conteúdo 🧵\r\n")]);
}
