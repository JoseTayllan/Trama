using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LibraryManagementWorkflowTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Rename_ParentKeepsSelectionAndDirtyEditorWithoutPrompt(bool space)
    {
        var actions = new NavigationActions();
        var management = new ManagementActions(actions);
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, management, prompt);
        var editor = library.Editor;
        editor.Title = "Rascunho pendente";

        Assert.True(space ? await library.RenameSelectedSpaceAsync("Renomeado") : await library.RenameSelectedNotebookAsync("Renomeado"));

        Assert.Equal("Renomeado", space ? library.SelectedSpace?.Title : library.SelectedNotebook?.Title);
        Assert.Equal(actions.Course.Id, library.SelectedSpaceId);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Same(editor, library.Editor);
        Assert.Equal("Rascunho pendente", editor.Title);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(0, prompt.Calls);
        Assert.Empty(actions.Saved);
    }

    [Fact]
    public async Task Rename_PageUpdatesEditorAndListWithSameIdentity()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new ManagementActions(actions), new NavigationPrompt());

        Assert.True(await library.RenameCurrentPageAsync("Aula revisada"));

        Assert.Equal(actions.FirstPage.Id, library.Editor.PageId);
        Assert.Equal("Aula revisada", library.Editor.Title);
        Assert.Equal("Aula revisada", library.SelectedPage?.Title);
        Assert.False(library.Editor.HasUnsavedChanges);
        Assert.Equal(actions.FirstPage.Blocks, actions.Stored[actions.FirstPage.Id].Blocks);
        Assert.Empty(actions.Saved);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DirtyPage_CancelKeepsDraftAndDoesNotMutate(bool moving)
    {
        var actions = new NavigationActions();
        var management = new ManagementActions(actions);
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, management, prompt);
        var editor = library.Editor;
        editor.Title = "Pendente";

        Assert.False(moving ? await library.MoveCurrentPageAsync(actions.OtherNotebook.Id) : await library.RenameCurrentPageAsync("Novo título"));

        Assert.Same(editor, library.Editor);
        Assert.Equal("Pendente", editor.Title);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Equal(1, prompt.Calls);
        Assert.Equal(0, management.Calls);
        Assert.Empty(actions.Saved);
    }

    [Fact]
    public async Task Rename_SaveFirstPreservesEditedBlocks()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new ManagementActions(actions), new NavigationPrompt(UnsavedChangesChoice.Save));
        library.Editor.Blocks[0].Text = "Conteúdo novo";

        Assert.True(await library.RenameCurrentPageAsync("Novo título"));

        Assert.Single(actions.Saved);
        Assert.Equal("Conteúdo novo", Assert.IsType<NotebookTextBlock>(Assert.Single(actions.Stored[actions.FirstPage.Id].Blocks)).Text);
        Assert.Equal("Novo título", library.Editor.Title);
        Assert.False(library.Editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task Move_DiscardUsesStoredContentAndSubsequentSaveKeepsDestination()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new ManagementActions(actions), new NavigationPrompt(UnsavedChangesChoice.Discard));
        library.Editor.Blocks[0].Text = "Descartar";

        Assert.True(await library.MoveCurrentPageAsync(actions.OtherNotebook.Id));

        Assert.Equal(actions.OtherNotebook.Id, library.SelectedNotebookId);
        Assert.Equal(actions.FirstPage.Id, library.Editor.PageId);
        Assert.Equal(((NotebookTextBlock)actions.FirstPage.Blocks[0]).Text, library.Editor.Blocks[0].Text);
        Assert.False(library.Editor.HasUnsavedChanges);
        Assert.Empty(actions.Saved);
        library.Editor.Title = "Edição depois de mover";
        await library.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(actions.OtherNotebook.Id, Assert.Single(actions.Saved).NotebookId);
        Assert.Equal(actions.OtherNotebook.Id, actions.Locations[actions.FirstPage.Id]);
    }

    [Fact]
    public async Task Move_CrossSpaceOpensTheSamePageAtDestination()
    {
        var actions = new NavigationActions();
        var space = new KnowledgeSpace(Guid.NewGuid(), "Pesquisa");
        var book = new NotebookInfo(Guid.NewGuid(), space.Id, "Artigos");
        actions.Spaces.Add(space);
        actions.Notebooks.Add(book);
        var library = await ReadyAsync(actions, new ManagementActions(actions), new NavigationPrompt());

        Assert.True(await library.MoveCurrentPageAsync(book.Id));

        Assert.Equal(space.Id, library.SelectedSpaceId);
        Assert.Equal(book.Id, library.SelectedNotebookId);
        Assert.Equal(actions.FirstPage.Id, library.Editor.PageId);
        Assert.Equal(actions.FirstPage.Id, Assert.Single(library.Pages).Id);
        Assert.Equal(book.Id, actions.Locations[actions.FirstPage.Id]);
    }

    [Theory]
    [InlineData(NotebookSaveStatus.ValidationFailed)]
    [InlineData(NotebookSaveStatus.PersistenceFailed)]
    public async Task Move_FailedSavePreventsMutationAndKeepsDraft(NotebookSaveStatus status)
    {
        var actions = new NavigationActions { SaveResult = new NotebookSaveResult(status) };
        var management = new ManagementActions(actions);
        var library = await ReadyAsync(actions, management, new NavigationPrompt(UnsavedChangesChoice.Save));
        var editor = library.Editor;
        editor.Title = "Não perder";

        Assert.False(await library.MoveCurrentPageAsync(actions.OtherNotebook.Id));

        Assert.Same(editor, library.Editor);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Equal(0, management.Calls);
    }

    [Fact]
    public async Task Move_WaitsForSaveAndRejectsConcurrentOperation()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>();
        var actions = new NavigationActions { PendingSave = pending.Task };
        var management = new ManagementActions(actions);
        var library = await ReadyAsync(actions, management, new NavigationPrompt(UnsavedChangesChoice.Save));
        library.Editor.Title = "Salvar antes";
        var move = library.MoveCurrentPageAsync(actions.OtherNotebook.Id);
        try
        {
            Assert.False(move.IsCompleted);
            Assert.True(library.IsBusy);
            Assert.False(library.CanEdit);
            Assert.Equal(0, management.Calls);
            Assert.False(await library.RenameCurrentPageAsync("Concorrente"));
            pending.SetResult(new NotebookSaveResult(NotebookSaveStatus.Saved));
            Assert.True(await move);
            Assert.Equal(1, management.Calls);
            Assert.Equal(actions.OtherNotebook.Id, actions.Locations[actions.FirstPage.Id]);
        }
        finally
        {
            pending.TrySetResult(new NotebookSaveResult(NotebookSaveStatus.PersistenceFailed));
            await move;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MutationFailure_PreservesDraftAndSelectionAndHidesInternalDetails(bool moving)
    {
        var actions = new NavigationActions();
        var management = new ManagementActions(actions) { Fail = true };
        var library = await ReadyAsync(actions, management, new NavigationPrompt(UnsavedChangesChoice.Discard));
        var editor = library.Editor;
        editor.Title = "Ainda disponível";

        Assert.False(moving ? await library.MoveCurrentPageAsync(actions.OtherNotebook.Id) : await library.RenameCurrentPageAsync("Novo título"));

        Assert.Same(editor, library.Editor);
        Assert.Equal("Ainda disponível", editor.Title);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Equal(actions.FirstPage.Title, actions.Stored[actions.FirstPage.Id].Title);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", library.Feedback);
        Assert.False(string.IsNullOrWhiteSpace(library.Feedback));
        Assert.True(library.CanEdit);
    }

    [Fact]
    public async Task UnsavedNewPage_CannotBeRenamedOrMovedAsPersistedEntry()
    {
        var actions = new NavigationActions();
        var management = new ManagementActions(actions);
        var library = await ReadyAsync(actions, management, new NavigationPrompt());
        Assert.True(await library.NewPageAsync());
        library.Editor.Title = "Página nova";

        Assert.False(await library.RenameCurrentPageAsync("Outro título"));
        Assert.False(await library.MoveCurrentPageAsync(actions.OtherNotebook.Id));

        Assert.Equal("Página nova", library.Editor.Title);
        Assert.Equal(0, management.Calls);
        Assert.Empty(actions.Saved);
    }

    private static async Task<LibraryViewModel> ReadyAsync(NavigationActions actions, ManagementActions management, NavigationPrompt prompt)
    {
        var library = new LibraryViewModel(actions, prompt, management);
        await library.InitializeAsync();
        Assert.True(await library.SelectSpaceAsync(actions.Course.Id));
        Assert.True(await library.SelectNotebookAsync(actions.Notebook.Id));
        Assert.True(await library.OpenPageAsync(actions.FirstPage.Id));
        return library;
    }

    private sealed class ManagementActions(NavigationActions actions) : ILibraryManagementActions
    {
        public int Calls { get; private set; }
        public bool Fail { get; init; }

        public Task RenameAsync(LibraryEntryKind kind, Guid id, string title, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fail) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
            if (kind == LibraryEntryKind.Space)
            {
                var index = actions.Spaces.FindIndex(space => space.Id == id);
                actions.Spaces[index] = actions.Spaces[index] with { Title = title.Trim() };
            }
            else if (kind == LibraryEntryKind.Notebook)
            {
                var index = actions.Notebooks.FindIndex(book => book.Id == id);
                actions.Notebooks[index] = actions.Notebooks[index] with { Title = title.Trim() };
            }
            else
            {
                actions.Stored[id] = actions.Stored[id] with { Title = title.Trim() };
            }

            return Task.CompletedTask;
        }

        public Task MovePageAsync(Guid pageId, Guid notebookId, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fail) throw new NotePersistenceException("PRIVATE_TEST_DETAIL");
            actions.Locations[pageId] = notebookId;
            return Task.CompletedTask;
        }
    }
}
