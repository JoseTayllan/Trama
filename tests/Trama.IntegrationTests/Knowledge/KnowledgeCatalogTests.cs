using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class KnowledgeCatalogTests
{
    [Fact]
    public async Task Migration_PreservesExistingNotesAndPagesAndMakesThemUnfiled()
    {
        using var workspace = new NotebookTestWorkspace();
        var legacy = new NoteSnapshot(Guid.NewGuid(), "  Nota antiga  ", "\r\n    Conteúdo 🧵\n");
        var page = Page("  Página em blocos  ");
        await using (var context = workspace.CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync("20261027234457_NotebookPages");
            await new SqliteNoteRepository(context).AddAsync(legacy);
            await new SqliteNotebookRepository(context).SaveAsync(page);
        }

        await using var upgraded = workspace.CreateContext();
        await upgraded.Database.MigrateAsync();
        Assert.True((await upgraded.Database.GetAppliedMigrationsAsync()).Count() >= 3,
            "Add a new migration; do not rewrite the two existing migrations.");
        var unfiled = await new SqliteKnowledgeCatalog(upgraded).ListUnfiledPagesAsync();
        Assert.Equal(2, unfiled.Count);
        Assert.Contains(new KnowledgePageSummary(legacy.Id, legacy.Title), unfiled);
        Assert.Contains(new KnowledgePageSummary(page.Id, page.Title), unfiled);
        Assert.Equal(legacy, await new SqliteNoteRepository(upgraded).GetByIdAsync(legacy.Id));
        var restored = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(upgraded).GetByIdAsync(page.Id));
        Assert.Equal(page.Blocks, restored.Blocks);
    }

    [Fact]
    public async Task SpacesAndNotebooks_ReopenWithIndependentIdentitiesAndExactTitles()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var course = new KnowledgeSpace(Guid.NewGuid(), "  Curso C# 🧵  ");
        var work = new KnowledgeSpace(Guid.NewGuid(), "Trabalho");
        var basics = new NotebookInfo(Guid.NewGuid(), course.Id, "Fundamentos");
        var projects = new NotebookInfo(Guid.NewGuid(), course.Id, "Projetos");
        var workBasics = new NotebookInfo(Guid.NewGuid(), work.Id, "Fundamentos");
        await using (var writer = workspace.CreateContext())
        {
            var catalog = new SqliteKnowledgeCatalog(writer);
            await catalog.AddSpaceAsync(course);
            await catalog.AddSpaceAsync(work);
            await catalog.AddNotebookAsync(basics);
            await catalog.AddNotebookAsync(projects);
            await catalog.AddNotebookAsync(workBasics);
        }

        await using var reader = workspace.CreateContext();
        var reopened = new SqliteKnowledgeCatalog(reader);
        var spaces = await reopened.ListSpacesAsync();
        Assert.Equal(2, spaces.Count);
        Assert.Contains(course, spaces);
        Assert.Contains(work, spaces);
        var courseBooks = await reopened.ListNotebooksAsync(course.Id);
        Assert.Equal(2, courseBooks.Count);
        Assert.Contains(basics, courseBooks);
        Assert.Contains(projects, courseBooks);
        Assert.Equal(workBasics, Assert.Single(await reopened.ListNotebooksAsync(work.Id)));
        Assert.Empty(await reopened.ListPagesAsync(basics.Id));
    }

    [Fact]
    public async Task Pages_AreListedOnlyInsideTheirOwnNotebook()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var (first, second) = await SeedNotebooksAsync(workspace);
        var variables = Page("Variáveis");
        var classes = Page("Classes");
        await using (var writer = workspace.CreateContext())
        {
            var pages = new SqliteNotebookRepository(writer);
            await pages.SaveAsync(variables);
            await pages.SaveAsync(classes);
            var catalog = new SqliteKnowledgeCatalog(writer);
            await catalog.PlacePageAsync(variables.Id, first.Id);
            await catalog.PlacePageAsync(classes.Id, second.Id);
        }

        await using var reader = workspace.CreateContext();
        var reopened = new SqliteKnowledgeCatalog(reader);
        Assert.Equal(new KnowledgePageSummary(variables.Id, variables.Title), Assert.Single(await reopened.ListPagesAsync(first.Id)));
        Assert.Equal(new KnowledgePageSummary(classes.Id, classes.Title), Assert.Single(await reopened.ListPagesAsync(second.Id)));
        Assert.Empty(await reopened.ListUnfiledPagesAsync());
    }

    [Fact]
    public async Task PlacePage_MovesItWithoutDuplicatingOrChangingItsBlocks()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var (first, second) = await SeedNotebooksAsync(workspace);
        var page = Page("Polimorfismo");
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNotebookRepository(writer).SaveAsync(page);
            var catalog = new SqliteKnowledgeCatalog(writer);
            await catalog.PlacePageAsync(page.Id, first.Id);
            await catalog.PlacePageAsync(page.Id, second.Id);
            await catalog.PlacePageAsync(page.Id, second.Id);
        }

        await using var reader = workspace.CreateContext();
        var reopened = new SqliteKnowledgeCatalog(reader);
        Assert.Empty(await reopened.ListPagesAsync(first.Id));
        Assert.Equal(new KnowledgePageSummary(page.Id, page.Title), Assert.Single(await reopened.ListPagesAsync(second.Id)));
        var restored = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(page.Id));
        Assert.Equal(page.Title, restored.Title);
        Assert.Equal(page.Blocks, restored.Blocks);
    }

    [Fact]
    public async Task UnfiledPages_DeduplicatesLegacyAndStructuredPageAndPrefersTheStructuredTitle()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var page = Page("Versão em blocos");
        var legacy = new NoteSnapshot(page.Id, "Título antigo", "Não reinterpretar este corpo");
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNoteRepository(writer).AddAsync(legacy);
            await new SqliteNotebookRepository(writer).SaveAsync(page);
        }

        await using var reader = workspace.CreateContext();
        Assert.Equal(new KnowledgePageSummary(page.Id, page.Title),
            Assert.Single(await new SqliteKnowledgeCatalog(reader).ListUnfiledPagesAsync()));
        Assert.Equal(legacy, await new SqliteNoteRepository(reader).GetByIdAsync(legacy.Id));
    }

    [Fact]
    public async Task PlacePage_CanOrganizeALegacyNoteWithoutRewritingItsBody()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var (notebook, _) = await SeedNotebooksAsync(workspace);
        var note = new NoteSnapshot(Guid.NewGuid(), "Nota antiga", "\r\n    print(42)\t🧵");
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNoteRepository(writer).AddAsync(note);
            await new SqliteKnowledgeCatalog(writer).PlacePageAsync(note.Id, notebook.Id);
        }

        await using var reader = workspace.CreateContext();
        Assert.Equal(new KnowledgePageSummary(note.Id, note.Title),
            Assert.Single(await new SqliteKnowledgeCatalog(reader).ListPagesAsync(notebook.Id)));
        Assert.Equal(note, await new SqliteNoteRepository(reader).GetByIdAsync(note.Id));
        Assert.Empty(await new SqliteKnowledgeCatalog(reader).ListUnfiledPagesAsync());
    }

    [Fact]
    public async Task SavePage_AfterOrganizingKeepsItsNotebookAndShowsTheUpdatedTitle()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var (notebook, _) = await SeedNotebooksAsync(workspace);
        var page = Page("Título inicial");
        var updated = page with { Title = "  Título revisado 🧵  " };
        await using (var writer = workspace.CreateContext())
        {
            var pages = new SqliteNotebookRepository(writer);
            await pages.SaveAsync(page);
            await new SqliteKnowledgeCatalog(writer).PlacePageAsync(page.Id, notebook.Id);
            await pages.SaveAsync(updated);
        }

        await using var reader = workspace.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(reader);
        Assert.Equal(new KnowledgePageSummary(updated.Id, updated.Title), Assert.Single(await catalog.ListPagesAsync(notebook.Id)));
        Assert.Empty(await catalog.ListUnfiledPagesAsync());
        var restored = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(page.Id));
        Assert.Equal(updated.Blocks, restored.Blocks);
    }

    [Fact]
    public async Task PlacePage_UnknownPageIsRejectedWithoutCreatingAPhantomEntry()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var (notebook, _) = await SeedNotebooksAsync(workspace);
        await using var context = workspace.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(context);

        await Assert.ThrowsAsync<ArgumentException>(() => catalog.PlacePageAsync(Guid.NewGuid(), notebook.Id));

        Assert.Empty(await catalog.ListPagesAsync(notebook.Id));
    }

    [Fact]
    public async Task PlacePage_UnknownDestinationPreservesItsPreviousNotebook()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var (notebook, _) = await SeedNotebooksAsync(workspace);
        var page = Page("Manter localização");
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNotebookRepository(writer).SaveAsync(page);
            await new SqliteKnowledgeCatalog(writer).PlacePageAsync(page.Id, notebook.Id);
        }

        await using (var writer = workspace.CreateContext())
        {
            await Assert.ThrowsAsync<NotePersistenceException>(() =>
                new SqliteKnowledgeCatalog(writer).PlacePageAsync(page.Id, Guid.NewGuid()));
        }

        await using var reader = workspace.CreateContext();
        Assert.Equal(new KnowledgePageSummary(page.Id, page.Title),
            Assert.Single(await new SqliteKnowledgeCatalog(reader).ListPagesAsync(notebook.Id)));
    }

    [Fact]
    public async Task AddNotebook_UnknownSpaceIsRejectedWithoutLeavingAnOrphan()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var missingSpace = Guid.NewGuid();
        await using (var writer = workspace.CreateContext())
        {
            await Assert.ThrowsAsync<NotePersistenceException>(() => new SqliteKnowledgeCatalog(writer)
                .AddNotebookAsync(new NotebookInfo(Guid.NewGuid(), missingSpace, "Órfão")));
        }

        await using var reader = workspace.CreateContext();
        Assert.Empty(await new SqliteKnowledgeCatalog(reader).ListNotebooksAsync(missingSpace));
    }

    [Fact]
    public async Task ListSpaces_MissingSchemaIsReportedAsFailureInsteadOfAnEmptyLibrary()
    {
        using var workspace = new NotebookTestWorkspace();
        await using var context = workspace.CreateContext();

        await Assert.ThrowsAsync<NotePersistenceException>(() => new SqliteKnowledgeCatalog(context).ListSpacesAsync());
    }

    [Fact]
    public async Task AddSpace_PreCancellationDoesNotWrite()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using (var writer = workspace.CreateContext())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SqliteKnowledgeCatalog(writer)
                .AddSpaceAsync(new KnowledgeSpace(Guid.NewGuid(), "Não gravar"), cancellation.Token));
        }

        await using var reader = workspace.CreateContext();
        Assert.Empty(await new SqliteKnowledgeCatalog(reader).ListSpacesAsync());
    }

    [Fact]
    public async Task AddSpace_ReadOnlyDatabaseReportsFailureWithoutWriting()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        await using (var writer = workspace.CreateContext(readOnly: true))
        {
            await Assert.ThrowsAsync<NotePersistenceException>(() => new SqliteKnowledgeCatalog(writer)
                .AddSpaceAsync(new KnowledgeSpace(Guid.NewGuid(), "Não gravar")));
        }

        await using var reader = workspace.CreateContext();
        Assert.Empty(await new SqliteKnowledgeCatalog(reader).ListSpacesAsync());
    }

    private static NotebookSnapshot Page(string title) => new(Guid.NewGuid(), title,
    [
        new NotebookTextBlock(Guid.NewGuid(), "  Explicação 🧵\r\n"),
        new NotebookCodeBlock(Guid.NewGuid(), "    Console.WriteLine(42);\t\n", "csharp"),
        new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Referência preservada")
    ]);

    private static async Task<(NotebookInfo First, NotebookInfo Second)> SeedNotebooksAsync(NotebookTestWorkspace workspace)
    {
        var space = new KnowledgeSpace(Guid.NewGuid(), "Curso C#");
        var first = new NotebookInfo(Guid.NewGuid(), space.Id, "Fundamentos");
        var second = new NotebookInfo(Guid.NewGuid(), space.Id, "Projetos");
        await using var context = workspace.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(context);
        await catalog.AddSpaceAsync(space);
        await catalog.AddNotebookAsync(first);
        await catalog.AddNotebookAsync(second);
        return (first, second);
    }
}
