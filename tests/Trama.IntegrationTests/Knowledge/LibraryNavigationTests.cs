using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LibraryNavigationTests
{
    [Fact]
    public async Task InitializeAndCreate_LoadsUnfiledPagesAndSelectsNewSpacesAndNotebooks()
    {
        var actions = new NavigationActions();
        var library = new LibraryViewModel(actions, new NavigationPrompt());
        await library.InitializeAsync();
        Assert.Equal(actions.Course, Assert.Single(library.Spaces));
        Assert.Contains(new KnowledgePageSummary(actions.UnfiledPage.Id, actions.UnfiledPage.Title), library.Pages);
        library.NewSpaceTitle = "  Curso de Rust  ";

        await library.CreateSpaceCommand.ExecuteAsync(null);

        var created = Assert.Single(library.Spaces, space => space.Title == "Curso de Rust");
        Assert.Equal(created.Id, library.SelectedSpaceId);
        Assert.Equal(string.Empty, library.NewSpaceTitle);
        library.NewNotebookTitle = "  Exercícios  ";
        await library.CreateNotebookCommand.ExecuteAsync(null);
        var notebook = Assert.Single(library.Notebooks);
        Assert.Equal(created.Id, notebook.SpaceId);
        Assert.Equal("Exercícios", notebook.Title);
        Assert.Equal(notebook.Id, library.SelectedNotebookId);
        Assert.Equal(string.Empty, library.NewNotebookTitle);
        Assert.Empty(library.Pages);
    }

    [Fact]
    public async Task NewPage_SaveUsesItsSelectedNotebookAndRefreshesTheVisiblePageList()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt());
        Assert.True(await library.NewPageAsync());
        library.Editor.Title = "Minha nova aula";

        await library.Editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal(actions.Notebook.Id, Assert.Single(actions.Saved).NotebookId);
        Assert.Contains(new KnowledgePageSummary(library.Editor.PageId, "Minha nova aula"), library.Pages);
        Assert.False(library.Editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task OpenPage_LoadsItsExistingContentIntoTheEditor()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt());

        Assert.True(await library.OpenPageAsync(actions.FirstPage.Id));

        Assert.Equal(actions.FirstPage.Id, library.Editor.PageId);
        Assert.Equal(actions.FirstPage.Title, library.Editor.Title);
        Assert.Equal("  Conteúdo 🧵\r\n", library.Editor.Blocks[0].Text);
        Assert.False(library.Editor.HasUnsavedChanges);
    }

    [Theory]
    [InlineData("page")]
    [InlineData("notebook")]
    [InlineData("new")]
    [InlineData("unfiled")]
    public async Task CancelLeavingDirtyPage_PreservesEditorSelectionAndDoesNotLoadOrSave(string destination)
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, prompt);
        var original = library.Editor;
        original.Title = "Rascunho que deve ficar";

        var changed = destination switch
        {
            "page" => await library.OpenPageAsync(actions.SecondPage.Id),
            "notebook" => await library.SelectNotebookAsync(actions.OtherNotebook.Id),
            "new" => await library.NewPageAsync(),
            _ => await library.ShowUnfiledAsync()
        };

        Assert.False(changed);
        Assert.Same(original, library.Editor);
        Assert.Equal("Rascunho que deve ficar", library.Editor.Title);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Equal(1, prompt.Calls);
        Assert.Empty(actions.Saved);
        Assert.Equal(0, actions.LoadCalls);
    }

    [Fact]
    public async Task DiscardChoice_OpensTheRequestedPageWithoutSavingTheOldDraft()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt(UnsavedChangesChoice.Discard);
        var library = await ReadyAsync(actions, prompt);
        library.Editor.Title = "Descartar esta alteração";

        Assert.True(await library.OpenPageAsync(actions.SecondPage.Id));

        Assert.Equal(actions.SecondPage.Id, library.Editor.PageId);
        Assert.Empty(actions.Saved);
        Assert.Equal(1, prompt.Calls);
    }

    [Fact]
    public async Task SaveChoice_WhenPersistenceFails_DoesNotLeaveTheDraft()
    {
        var actions = new NavigationActions { SaveResult = new(NotebookSaveStatus.PersistenceFailed) };
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        var original = library.Editor;
        original.Title = "Manter no erro";

        Assert.False(await library.OpenPageAsync(actions.SecondPage.Id));

        Assert.Same(original, library.Editor);
        Assert.True(original.HasUnsavedChanges);
        Assert.Equal(0, actions.LoadCalls);
    }

    [Fact]
    public async Task SaveChoice_WaitsForSuccessfulSaveBeforeOpeningTheRequestedPage()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        var originalId = library.Editor.PageId;
        library.Editor.Title = "Salvar antes de sair";

        Assert.True(await library.OpenPageAsync(actions.SecondPage.Id));

        Assert.Equal(originalId, Assert.Single(actions.Saved).Page.Id);
        Assert.Equal(actions.Notebook.Id, actions.Saved[0].NotebookId);
        Assert.Equal(actions.SecondPage.Id, library.Editor.PageId);
    }

    [Fact]
    public async Task SaveChoice_PendingSaveDoesNotLoadTheDestinationEarly()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        actions.PendingSave = pending.Task;
        library.Editor.Title = "Aguardar gravação";
        var navigation = library.OpenPageAsync(actions.SecondPage.Id);
        var changed = false;
        try
        {
            Assert.False(navigation.IsCompleted);
            Assert.Equal(0, actions.LoadCalls);
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            changed = await navigation;
        }

        Assert.True(changed);
        Assert.Equal(actions.SecondPage.Id, library.Editor.PageId);
    }

    [Fact]
    public async Task OpenMissingPage_AfterDiscardChoiceStillKeepsTheOriginalDraftWhenOpeningFails()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Discard));
        var original = library.Editor;
        original.Title = "Preservar se a abertura falhar";

        Assert.False(await library.OpenPageAsync(Guid.NewGuid()));

        Assert.Same(original, library.Editor);
        Assert.Equal("Preservar se a abertura falhar", original.Title);
        Assert.True(original.HasUnsavedChanges);
    }

    [Fact]
    public async Task CatalogFailure_PreservesTheCurrentNotebookAndEditorWithoutExposingDetails()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt());
        var original = library.Editor;
        actions.FailCatalogRead = true;

        Assert.False(await library.SelectNotebookAsync(actions.OtherNotebook.Id));

        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Same(original, library.Editor);
        Assert.False(string.IsNullOrWhiteSpace(library.Feedback));
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", library.Feedback);
        Assert.False(library.IsBusy);
    }

    [Fact]
    public async Task SavingPage_BlocksNavigationBeforePromptingOrLoading()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt(UnsavedChangesChoice.Discard);
        var library = await ReadyAsync(actions, prompt);
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        actions.PendingSave = pending.Task;
        library.Editor.Title = "Salvando";
        var save = library.Editor.SaveCommand.ExecuteAsync(null);
        try
        {
            Assert.False(await library.OpenPageAsync(actions.SecondPage.Id));
            Assert.Equal(0, prompt.Calls);
            Assert.Equal(0, actions.LoadCalls);
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            await save;
        }
    }

    [Fact]
    public async Task CreationFailure_KeepsTheInputAndCurrentEditor()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt());
        var original = library.Editor;
        actions.FailCreation = true;
        library.NewSpaceTitle = "  Manter este nome  ";

        await library.CreateSpaceCommand.ExecuteAsync(null);

        Assert.Equal("  Manter este nome  ", library.NewSpaceTitle);
        Assert.Same(original, library.Editor);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", library.Feedback);
        Assert.False(library.IsBusy);
    }

    private static async Task<LibraryViewModel> ReadyAsync(NavigationActions actions, NavigationPrompt prompt)
    {
        var library = new LibraryViewModel(actions, prompt);
        await library.InitializeAsync();
        Assert.True(await library.SelectSpaceAsync(actions.Course.Id));
        Assert.True(await library.SelectNotebookAsync(actions.Notebook.Id));
        return library;
    }
}
