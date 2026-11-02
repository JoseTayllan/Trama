using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class SearchTagsWorkflowTests
{
    [Fact]
    public async Task Search_PassesTextAndTagWithoutLeavingDirtyPageOrPrompting()
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, discovery, prompt);
        var editor = library.Editor;
        editor.Title = "Rascunho";

        Assert.True(await library.SearchAsync("variáveis", "c#"));

        Assert.Equal(("variáveis", "c#"), Assert.Single(discovery.Requests));
        Assert.Equal(discovery.Results, library.SearchResults);
        Assert.Same(editor, library.Editor);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(0, prompt.Calls);
        Assert.Empty(actions.Saved);
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel)]
    [InlineData(UnsavedChangesChoice.Discard)]
    [InlineData(UnsavedChangesChoice.Save)]
    public async Task OpenResult_ResolvesDirtyDraftAndOpensUnfiledPage(UnsavedChangesChoice choice)
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        var prompt = new NavigationPrompt(choice);
        var library = await ReadyAsync(actions, discovery, prompt);
        await library.SearchAsync("alvo");
        var editor = library.Editor;
        editor.Title = "Alteração pendente";

        var opened = await library.OpenSearchResultAsync(actions.UnfiledPage.Id);

        Assert.Equal(1, prompt.Calls);
        if (choice == UnsavedChangesChoice.Cancel)
        {
            Assert.False(opened);
            Assert.Same(editor, library.Editor);
            Assert.Equal("Alteração pendente", library.Editor.Title);
            Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
            Assert.Empty(actions.Saved);
        }
        else
        {
            Assert.True(opened);
            Assert.Equal(actions.UnfiledPage.Id, library.Editor.PageId);
            Assert.Null(library.SelectedSpaceId);
            Assert.Null(library.SelectedNotebookId);
            Assert.False(library.Editor.HasUnsavedChanges);
            if (choice == UnsavedChangesChoice.Save)
            {
                var saved = Assert.Single(actions.Saved);
                Assert.Equal(actions.Notebook.Id, saved.NotebookId);
                Assert.Equal("Alteração pendente", saved.Page.Title);
            }
            else Assert.Empty(actions.Saved);
        }
    }

    [Fact]
    public async Task SearchFailure_KeepsPreviousResultsAndHidesInternalDetails()
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        var library = await ReadyAsync(actions, discovery, new NavigationPrompt());
        Assert.True(await library.SearchAsync("primeira"));
        var results = library.SearchResults.ToArray();
        discovery.Fail = true;

        Assert.False(await library.SearchAsync("segunda"));

        Assert.Equal(results, library.SearchResults);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", library.Feedback);
        Assert.False(string.IsNullOrWhiteSpace(library.Feedback));
        Assert.True(library.CanNavigate);
    }

    [Fact]
    public async Task Search_PendingRequestBlocksConcurrentOperations()
    {
        var actions = new NavigationActions();
        var pending = new TaskCompletionSource<IReadOnlyList<PageSearchResult>>();
        var discovery = new DiscoveryActions(actions) { Pending = pending.Task };
        var library = await ReadyAsync(actions, discovery, new NavigationPrompt());
        var search = library.SearchAsync("primeira");
        try
        {
            Assert.False(search.IsCompleted);
            Assert.True(library.IsBusy);
            Assert.False(await library.SearchAsync("concorrente"));
            Assert.False(await library.NewPageAsync());
            Assert.Single(discovery.Requests);
            pending.SetResult(discovery.Results);
            Assert.True(await search);
            Assert.True(library.CanNavigate);
        }
        finally
        {
            pending.TrySetResult(Array.Empty<PageSearchResult>());
            await search;
        }
    }

    [Fact]
    public async Task Tags_ChangeMetadataWithoutSavingOrDiscardingDirtyContent()
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, discovery, prompt);
        var editor = library.Editor;
        editor.Blocks[0].Text = "Ainda em estudo";

        Assert.True(await library.SetCurrentPageTagsAsync(["C#", "revisão"]));

        Assert.Equal(actions.FirstPage.Id, discovery.LastTaggedPage);
        Assert.Equal(new[] { "c#", "revisão" }, library.CurrentPageTags);
        Assert.Same(editor, library.Editor);
        Assert.Equal("Ainda em estudo", editor.Blocks[0].Text);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Empty(actions.Saved);
        Assert.Equal(0, prompt.Calls);
    }

    [Fact]
    public async Task Tags_FailurePreservesPreviouslyLoadedTagsAndDraft()
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        discovery.Tags[actions.FirstPage.Id] = ["existente"];
        var library = await ReadyAsync(actions, discovery, new NavigationPrompt());
        Assert.True(await library.LoadCurrentPageTagsAsync());
        Assert.Equal(new[] { "existente" }, library.CurrentPageTags);
        library.Editor.Title = "Rascunho";
        discovery.Fail = true;

        Assert.False(await library.SetCurrentPageTagsAsync(["outra"]));

        Assert.Equal(new[] { "existente" }, library.CurrentPageTags);
        Assert.Equal("Rascunho", library.Editor.Title);
        Assert.True(library.Editor.HasUnsavedChanges);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", library.Feedback);
    }

    [Fact]
    public async Task Tags_NewUnsavedPageDoesNotCreateMetadata()
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        var library = await ReadyAsync(actions, discovery, new NavigationPrompt());
        Assert.True(await library.NewPageAsync());
        library.Editor.Title = "Nova página";

        Assert.False(await library.SetCurrentPageTagsAsync(["nova"]));

        Assert.Null(discovery.LastTaggedPage);
        Assert.Empty(actions.Saved);
        Assert.True(library.Editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task OpenResult_RefreshesMovedLocationBeforeOpeningAndSaving()
    {
        var actions = new NavigationActions();
        var discovery = new DiscoveryActions(actions);
        discovery.Results = [new PageSearchResult(actions.SecondPage.Id, actions.SecondPage.Title,
            actions.Course.Id, actions.Course.Title, actions.Notebook.Id, actions.Notebook.Title)];
        var library = await ReadyAsync(actions, discovery, new NavigationPrompt());
        await library.SearchAsync("resultado");
        actions.Locations[actions.SecondPage.Id] = actions.OtherNotebook.Id;

        Assert.True(await library.OpenSearchResultAsync(actions.SecondPage.Id));

        Assert.Equal(actions.OtherNotebook.Id, library.SelectedNotebookId);
        Assert.Equal(actions.SecondPage.Id, library.Editor.PageId);
        library.Editor.Title = "Editar no destino real";
        await library.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(actions.OtherNotebook.Id, Assert.Single(actions.Saved).NotebookId);
    }

    [Fact]
    public async Task OpenResult_FailedSaveKeepsDraftAndSelection()
    {
        var actions = new NavigationActions { SaveResult = new NotebookSaveResult(NotebookSaveStatus.PersistenceFailed) };
        var library = await ReadyAsync(actions, new DiscoveryActions(actions), new NavigationPrompt(UnsavedChangesChoice.Save));
        await library.SearchAsync("resultado");
        var editor = library.Editor;
        editor.Title = "Não perder";

        Assert.False(await library.OpenSearchResultAsync(actions.UnfiledPage.Id));

        Assert.Same(editor, library.Editor);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
    }

    private static async Task<LibraryViewModel> ReadyAsync(NavigationActions actions, DiscoveryActions discovery, NavigationPrompt prompt)
    {
        var library = new LibraryViewModel(actions, prompt, discovery: discovery);
        await library.InitializeAsync();
        Assert.True(await library.SelectSpaceAsync(actions.Course.Id));
        Assert.True(await library.SelectNotebookAsync(actions.Notebook.Id));
        Assert.True(await library.OpenPageAsync(actions.FirstPage.Id));
        return library;
    }

    private sealed class DiscoveryActions : IKnowledgeDiscovery
    {
        public DiscoveryActions(NavigationActions actions) => Results = [new PageSearchResult(actions.UnfiledPage.Id, actions.UnfiledPage.Title)];
        public List<(string Query, string? Tag)> Requests { get; } = [];
        public IReadOnlyList<PageSearchResult> Results { get; set; }
        public Dictionary<Guid, IReadOnlyList<string>> Tags { get; } = [];
        public Guid? LastTaggedPage { get; private set; }
        public bool Fail { get; set; }
        public Task<IReadOnlyList<PageSearchResult>>? Pending { get; init; }

        public async Task<IReadOnlyList<PageSearchResult>> SearchAsync(string query, string? tag = null, CancellationToken cancellationToken = default)
        {
            Requests.Add((query, tag));
            if (Fail) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
            return Pending is null ? Results : await Pending;
        }

        public Task<IReadOnlyList<string>> GetPageTagsAsync(Guid pageId, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
            return Task.FromResult(Tags.GetValueOrDefault(pageId) ?? Array.Empty<string>());
        }

        public Task SetPageTagsAsync(Guid pageId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
            LastTaggedPage = pageId;
            Tags[pageId] = tags.Select(tag => tag.Trim().ToLowerInvariant()).Distinct().Order(StringComparer.Ordinal).ToArray();
            return Task.CompletedTask;
        }
    }
}
