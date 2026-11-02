using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LibraryWorkspaceTests
{
    [AvaloniaFact]
    public async Task View_BindsLibraryListsCreationCommandsAndTheCurrentEditor()
    {
        var library = new LibraryViewModel(new NavigationActions(), new NavigationPrompt());
        await library.InitializeAsync();
        var view = new LibraryWorkspaceView(library);
        var window = new Window { Width = 1100, Height = 780, Content = view };
        try
        {
            window.Show();
            var spaceTitle = Assert.IsType<TextBox>(view.FindControl<TextBox>("NewSpaceTitleInput"));
            var notebookTitle = Assert.IsType<TextBox>(view.FindControl<TextBox>("NewNotebookTitleInput"));
            spaceTitle.Text = "Novo espaço";
            notebookTitle.Text = "Novo caderno";
            Assert.Equal(spaceTitle.Text, library.NewSpaceTitle);
            Assert.Equal(notebookTitle.Text, library.NewNotebookTitle);
            Assert.Same(library.CreateSpaceCommand, Assert.IsType<Button>(view.FindControl<Button>("CreateSpaceButton")).Command);
            Assert.Same(library.CreateNotebookCommand, Assert.IsType<Button>(view.FindControl<Button>("CreateNotebookButton")).Command);
            Assert.Same(library.NewPageCommand, Assert.IsType<Button>(view.FindControl<Button>("NewPageButton")).Command);
            Assert.Same(library.Spaces, Assert.IsType<ListBox>(view.FindControl<ListBox>("SpaceList")).ItemsSource);
            Assert.Same(library.Notebooks, Assert.IsType<ListBox>(view.FindControl<ListBox>("NotebookList")).ItemsSource);
            Assert.Same(library.Pages, Assert.IsType<ListBox>(view.FindControl<ListBox>("PageList")).ItemsSource);
            Assert.Same(library.Editor, Assert.IsType<NotebookEditorView>(view.FindControl<NotebookEditorView>("CurrentPageEditor")).DataContext);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task Composition_CreatesARealLibraryAndReopensThePageInAnotherSession()
    {
        using var workspace = new NotebookTestWorkspace();
        Guid spaceId;
        Guid notebookId;
        Guid pageId;
        using (var services = await DesktopComposition.CreateAsync(workspace.DirectoryPath,
            unsavedChangesPrompt: new NavigationPrompt()))
        {
            var library = services.GetRequiredService<LibraryViewModel>();
            await library.InitializeAsync();
            library.NewSpaceTitle = "Curso C#";
            await library.CreateSpaceCommand.ExecuteAsync(null);
            spaceId = Assert.Single(library.Spaces).Id;
            library.NewNotebookTitle = "Fundamentos";
            await library.CreateNotebookCommand.ExecuteAsync(null);
            notebookId = Assert.Single(library.Notebooks).Id;
            Assert.True(await library.NewPageAsync());
            library.Editor.Title = "Variáveis";
            library.Editor.Blocks[0].Text = "  Conteúdo real 🧵\r\n";
            await library.Editor.SaveCommand.ExecuteAsync(null);
            Assert.Equal(NotebookSaveStatus.Saved, library.Editor.LastSaveStatus);
            pageId = library.Editor.PageId;
            Assert.Equal(new KnowledgePageSummary(pageId, "Variáveis"), Assert.Single(library.Pages));
        }

        using var reopened = await DesktopComposition.CreateAsync(workspace.DirectoryPath,
            unsavedChangesPrompt: new NavigationPrompt());
        var session = reopened.GetRequiredService<LibraryViewModel>();
        await session.InitializeAsync();
        Assert.True(await session.SelectSpaceAsync(spaceId));
        Assert.True(await session.SelectNotebookAsync(notebookId));
        Assert.True(await session.OpenPageAsync(pageId));
        Assert.Equal("Variáveis", session.Editor.Title);
        Assert.Equal("  Conteúdo real 🧵\r\n", session.Editor.Blocks[0].Text);
        Assert.False(session.Editor.HasUnsavedChanges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAndPlacement_InvalidNotebookRollsBackContentAndLocation(bool existingPage)
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var actions = Actions(workspace);
        var space = await actions.CreateSpaceAsync("Curso");
        var notebook = await actions.CreateNotebookAsync(space.Id, "Caderno");
        var original = new NotebookSnapshot(Guid.NewGuid(), "Original", [new NotebookTextBlock(Guid.NewGuid(), "Manter")]);
        if (existingPage)
        {
            Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(original, notebook.Id)).Status);
        }

        var result = await actions.SaveInNotebookAsync(original with { Title = "Não confirmar", Blocks = [] }, Guid.NewGuid());

        Assert.Equal(NotebookSaveStatus.PersistenceFailed, result.Status);
        await using var reader = workspace.CreateContext();
        var page = await new SqliteNotebookRepository(reader).GetByIdAsync(original.Id);
        var catalog = new SqliteKnowledgeCatalog(reader);
        if (existingPage)
        {
            Assert.NotNull(page);
            Assert.Equal(original.Title, page.Title);
            Assert.Equal(original.Blocks, page.Blocks);
            Assert.Equal(new KnowledgePageSummary(original.Id, original.Title), Assert.Single(await catalog.ListPagesAsync(notebook.Id)));
        }
        else
        {
            Assert.Null(page);
            Assert.Empty(await catalog.ListPagesAsync(notebook.Id));
        }

        Assert.Empty(await catalog.ListUnfiledPagesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatingAnEntry_RejectsBlankNamesWithoutPersistingThem(bool notebook)
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var actions = Actions(workspace);
        if (notebook)
        {
            var space = await actions.CreateSpaceAsync("Curso");
            await Assert.ThrowsAsync<ArgumentException>(() => actions.CreateNotebookAsync(space.Id, " \t\n"));
            Assert.Empty(await actions.ListNotebooksAsync(space.Id));
        }
        else
        {
            await Assert.ThrowsAsync<ArgumentException>(() => actions.CreateSpaceAsync(" \t\n"));
            Assert.Empty(await actions.ListSpacesAsync());
        }
    }

    private static SqliteLibraryActions Actions(NotebookTestWorkspace workspace) =>
        new(Path.Combine(workspace.DirectoryPath, "trama.db"), new LocalImageAttachmentStore(workspace.DirectoryPath));
}
