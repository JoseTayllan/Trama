using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LibraryManagementStorageTests
{
    [Theory]
    [InlineData(LibraryEntryKind.Space)]
    [InlineData(LibraryEntryKind.Notebook)]
    [InlineData(LibraryEntryKind.Page)]
    public async Task Rename_ReopensWithSameIdentityAndPreservesContentAndLocation(LibraryEntryKind kind)
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        var id = EntryId(seed, kind);

        await Management(data).RenameAsync(kind, id, "  Novo título 🧵  ");

        await using var reader = data.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(reader);
        Assert.Equal(new KnowledgeSpace(seed.Space.Id, kind == LibraryEntryKind.Space ? "Novo título 🧵" : seed.Space.Title),
            Assert.Single(await catalog.ListSpacesAsync()));
        Assert.Contains(seed.First with { Title = kind == LibraryEntryKind.Notebook ? "Novo título 🧵" : seed.First.Title },
            await catalog.ListNotebooksAsync(seed.Space.Id));
        var page = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(seed.Page.Id));
        Assert.Equal(seed.Page.Id, page.Id);
        Assert.Equal(seed.Page.Blocks, page.Blocks);
        Assert.Equal(kind == LibraryEntryKind.Page ? "Novo título 🧵" : seed.Page.Title, page.Title);
        Assert.Equal(new KnowledgePageSummary(page.Id, page.Title), Assert.Single(await catalog.ListPagesAsync(seed.First.Id)));
        Assert.Empty(await catalog.ListPagesAsync(seed.Second.Id));
    }

    [Theory]
    [InlineData(LibraryEntryKind.Space)]
    [InlineData(LibraryEntryKind.Notebook)]
    [InlineData(LibraryEntryKind.Page)]
    public async Task Rename_WhitespaceIsRejectedWithoutWriting(LibraryEntryKind kind)
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => Management(data).RenameAsync(kind, EntryId(seed, kind), " \r\n\t "));

        await AssertOriginalAsync(data, seed);
    }

    [Theory]
    [InlineData(LibraryEntryKind.Space)]
    [InlineData(LibraryEntryKind.Notebook)]
    [InlineData(LibraryEntryKind.Page)]
    public async Task Rename_MissingEntryDoesNotCreateAnything(LibraryEntryKind kind)
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => Management(data).RenameAsync(kind, Guid.NewGuid(), "Fantasma"));

        await AssertOriginalAsync(data, seed);
    }

    [Fact]
    public async Task Rename_LegacyNotePreservesItsExactBody()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var note = new NoteSnapshot(Guid.NewGuid(), "Antiga", "\r\n    print(42)\t🧵\n");
        await using (var writer = data.CreateContext())
            await new SqliteNoteRepository(writer).AddAsync(note);

        await Management(data).RenameAsync(LibraryEntryKind.Page, note.Id, "  Aula antiga  ");

        await using var reader = data.CreateContext();
        Assert.Equal(note with { Title = "Aula antiga" }, await new SqliteNoteRepository(reader).GetByIdAsync(note.Id));
        Assert.Equal(new KnowledgePageSummary(note.Id, "Aula antiga"),
            Assert.Single(await new SqliteKnowledgeCatalog(reader).ListUnfiledPagesAsync()));
    }

    [Fact]
    public async Task Rename_StructuredPageWinsOverLegacyWithSameId()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        var legacy = new NoteSnapshot(seed.Page.Id, "Histórico", "Corpo original");
        await using (var writer = data.CreateContext())
            await new SqliteNoteRepository(writer).AddAsync(legacy);

        await Management(data).RenameAsync(LibraryEntryKind.Page, seed.Page.Id, "Página atual");

        await using var reader = data.CreateContext();
        Assert.Equal(legacy, await new SqliteNoteRepository(reader).GetByIdAsync(legacy.Id));
        var page = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(legacy.Id));
        Assert.Equal("Página atual", page.Title);
        Assert.Equal(seed.Page.Blocks, page.Blocks);
    }

    [Fact]
    public async Task Move_CrossSpaceIsIdempotentAndPreservesBlocks()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        var otherSpace = new KnowledgeSpace(Guid.NewGuid(), "Trabalho");
        var destination = new NotebookInfo(Guid.NewGuid(), otherSpace.Id, "Pesquisa");
        await using (var writer = data.CreateContext())
        {
            var catalog = new SqliteKnowledgeCatalog(writer);
            await catalog.AddSpaceAsync(otherSpace);
            await catalog.AddNotebookAsync(destination);
        }

        await Management(data).MovePageAsync(seed.Page.Id, destination.Id);
        await Management(data).MovePageAsync(seed.Page.Id, destination.Id);

        await using var reader = data.CreateContext();
        var reopened = new SqliteKnowledgeCatalog(reader);
        Assert.Empty(await reopened.ListPagesAsync(seed.First.Id));
        Assert.Equal(new KnowledgePageSummary(seed.Page.Id, seed.Page.Title), Assert.Single(await reopened.ListPagesAsync(destination.Id)));
        Assert.Empty(await reopened.ListUnfiledPagesAsync());
        var page = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(seed.Page.Id));
        Assert.Equal(seed.Page.Blocks, page.Blocks);
    }

    [Fact]
    public async Task Move_UnknownDestinationKeepsOriginalLocation()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);

        await Assert.ThrowsAsync<NotePersistenceException>(() => Management(data).MovePageAsync(seed.Page.Id, Guid.NewGuid()));

        await AssertOriginalAsync(data, seed);
    }

    [Fact]
    public async Task Move_UnfiledLegacyNoteKeepsItsBody()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        var note = new NoteSnapshot(Guid.NewGuid(), "Anotação", "  Código\r\n\t🧵");
        await using (var writer = data.CreateContext())
            await new SqliteNoteRepository(writer).AddAsync(note);

        await Management(data).MovePageAsync(note.Id, seed.Second.Id);

        await using var reader = data.CreateContext();
        Assert.Equal(note, await new SqliteNoteRepository(reader).GetByIdAsync(note.Id));
        Assert.Equal(new KnowledgePageSummary(note.Id, note.Title),
            Assert.Single(await new SqliteKnowledgeCatalog(reader).ListPagesAsync(seed.Second.Id)));
        Assert.Empty(await new SqliteKnowledgeCatalog(reader).ListUnfiledPagesAsync());
    }

    [Fact]
    public async Task Operations_PreCancellationDoesNotWrite()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Management(data)
            .RenameAsync(LibraryEntryKind.Page, seed.Page.Id, "Não gravar", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Management(data)
            .MovePageAsync(seed.Page.Id, seed.Second.Id, cancellation.Token));

        await AssertOriginalAsync(data, seed);
    }

    private static SqliteLibraryManagement Management(NotebookTestWorkspace data) => new(Path.Combine(data.DirectoryPath, "trama.db"));

    private static Guid EntryId(Seed seed, LibraryEntryKind kind) => kind switch
    {
        LibraryEntryKind.Space => seed.Space.Id,
        LibraryEntryKind.Notebook => seed.First.Id,
        _ => seed.Page.Id
    };

    private static async Task<Seed> SeedAsync(NotebookTestWorkspace data)
    {
        await data.MigrateAsync();
        var seed = new Seed(new KnowledgeSpace(Guid.NewGuid(), "Curso C#"),
            new NotebookSnapshot(Guid.NewGuid(), "Variáveis",
            [
                new NotebookTextBlock(Guid.NewGuid(), "  Texto 🧵\r\n"),
                new NotebookCodeBlock(Guid.NewGuid(), "    print(42)\t\n", "python"),
                new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Print")
            ]));
        await using var writer = data.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(writer);
        await catalog.AddSpaceAsync(seed.Space);
        await catalog.AddNotebookAsync(seed.First);
        await catalog.AddNotebookAsync(seed.Second);
        await new SqliteNotebookRepository(writer).SaveAsync(seed.Page);
        await catalog.PlacePageAsync(seed.Page.Id, seed.First.Id);
        return seed;
    }

    private static async Task AssertOriginalAsync(NotebookTestWorkspace data, Seed seed)
    {
        await using var reader = data.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(reader);
        Assert.Equal(seed.Space, Assert.Single(await catalog.ListSpacesAsync()));
        Assert.Equal(2, (await catalog.ListNotebooksAsync(seed.Space.Id)).Count);
        Assert.Contains(seed.First, await catalog.ListNotebooksAsync(seed.Space.Id));
        Assert.Equal(new KnowledgePageSummary(seed.Page.Id, seed.Page.Title), Assert.Single(await catalog.ListPagesAsync(seed.First.Id)));
        Assert.Empty(await catalog.ListPagesAsync(seed.Second.Id));
        Assert.Empty(await catalog.ListUnfiledPagesAsync());
        var page = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(seed.Page.Id));
        Assert.Equal(seed.Page.Title, page.Title);
        Assert.Equal(seed.Page.Blocks, page.Blocks);
    }

    private sealed record Seed(KnowledgeSpace Space, NotebookSnapshot Page)
    {
        public NotebookInfo First { get; } = new(Guid.NewGuid(), Space.Id, "Fundamentos");
        public NotebookInfo Second { get; } = new(Guid.NewGuid(), Space.Id, "Projetos");
    }
}
