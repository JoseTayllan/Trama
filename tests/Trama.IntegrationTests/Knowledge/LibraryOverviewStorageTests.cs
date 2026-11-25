using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LibraryOverviewStorageTests
{
    [Fact]
    public async Task EmptyLibrary_ReturnsZeroCountsAndEmptyCollections()
    {
        using var f = await Fixture.CreateAsync();
        var result = await Reader(f.Discovery).ReadOverviewAsync();
        Counts(result, 0, 0, 0, 0);
        Assert.Empty(result.Spaces);
        Assert.Empty(result.TopTags);
    }

    [Fact]
    public async Task Counts_AreGlobalAndIncludeUnfiledPages()
    {
        using var f = await Fixture.CreateAsync();
        var first = await f.Library.CreateSpaceAsync("Estudos");
        var second = await f.Library.CreateSpaceAsync("Trabalho");
        var a = await f.Library.CreateNotebookAsync(first.Id, "C#");
        var b = await f.Library.CreateNotebookAsync(first.Id, "Python");
        var c = await f.Library.CreateNotebookAsync(second.Id, "Projeto");
        await f.SaveAsync("A", a.Id);
        await f.SaveAsync("B", a.Id);
        await f.SaveAsync("C", b.Id);
        await f.SaveAsync("D", c.Id);
        await f.SaveAsync("Solta");

        var result = await Reader(f.Discovery).ReadOverviewAsync();
        Counts(result, 2, 3, 5, 1);
        var study = Assert.Single(result.Spaces, row => row.Space.Id == first.Id);
        Assert.Equal(first, study.Space);
        Assert.Equal(2, study.NotebookCount);
        Assert.Equal(3, study.PageCount);
        var work = Assert.Single(result.Spaces, row => row.Space.Id == second.Id);
        Assert.Equal(1, work.NotebookCount);
        Assert.Equal(1, work.PageCount);
    }

    [Fact]
    public async Task LegacyNotes_AreUnfiledAndSharedIdentitiesAreNotCountedTwice()
    {
        using var f = await Fixture.CreateAsync();
        var structured = await f.SaveAsync("Página atual");
        await f.LegacyAsync(new NoteSnapshot(structured.Id, "Título antigo", "Texto antigo"));
        await f.LegacyAsync(new NoteSnapshot(Guid.NewGuid(), "Nota antiga", "Conteúdo"));

        Counts(await Reader(f.Discovery).ReadOverviewAsync(), 0, 0, 2, 2);
    }

    [Theory]
    [InlineData(LibraryEntryKind.Page, 1, 1)]
    [InlineData(LibraryEntryKind.Notebook, 1, 0)]
    [InlineData(LibraryEntryKind.Space, 0, 0)]
    public async Task Trash_ExcludesAnEntryAndItsDescendants(LibraryEntryKind kind, int spaces, int notebooks)
    {
        using var f = await Fixture.CreateAsync();
        var seed = await f.SeedAsync();
        await f.Trash.MoveToTrashAsync(kind, EntryId(seed, kind));

        var result = await Reader(f.Discovery).ReadOverviewAsync();
        Counts(result, spaces, notebooks, 0, 0);
        Assert.Equal(spaces, result.Spaces.Count);
        Assert.All(result.Spaces, row => Assert.Equal(0, row.PageCount));
    }

    [Fact]
    public async Task EmptySpaces_AppearWithZeroCounts()
    {
        using var f = await Fixture.CreateAsync();
        var space = await f.Library.CreateSpaceAsync("Espaço vazio");
        var result = await Reader(f.Discovery).ReadOverviewAsync();
        Counts(result, 1, 0, 0, 0);
        Assert.Equal(new LibrarySpaceOverview(space, 0, 0), Assert.Single(result.Spaces));
    }

    [Fact]
    public async Task EqualTitles_DoNotMergeDifferentSpacesOrPages()
    {
        using var f = await Fixture.CreateAsync();
        var a = await f.Library.CreateSpaceAsync("Mesmo nome");
        var b = await f.Library.CreateSpaceAsync("Mesmo nome");
        var notebook = await f.Library.CreateNotebookAsync(a.Id, "Curso");
        await f.SaveAsync("Anotação", notebook.Id);
        await f.SaveAsync("Anotação");

        var result = await Reader(f.Discovery).ReadOverviewAsync();
        Counts(result, 2, 1, 2, 1);
        Assert.Equal(2, result.Spaces.Select(row => row.Space.Id).Distinct().Count());
        Assert.Contains(result.Spaces, row => row.Space.Id == b.Id && row.PageCount == 0);
    }

    [Fact]
    public async Task TopTags_ReturnsFiveOrderedByPageCountThenOrdinalName()
    {
        using var f = await Fixture.CreateAsync();
        var first = await f.SaveAsync("A");
        var second = await f.SaveAsync("B");
        await f.Discovery.SetPageTagsAsync(first.Id, ["z", "b", "a", "c", "d", "e", "f"]);
        await f.Discovery.SetPageTagsAsync(second.Id, ["z", "b"]);

        var result = await Reader(f.Discovery).ReadOverviewAsync();
        Assert.Equal(new[] { new LibraryTagUsage("b", 2), new("z", 2), new("a", 1), new("c", 1), new("d", 1) }, result.TopTags);
    }

    [Fact]
    public async Task Tags_CountDistinctPagesWithTheExistingNormalization()
    {
        using var f = await Fixture.CreateAsync();
        var first = await f.SaveAsync("A");
        var second = await f.SaveAsync("B");
        await f.Discovery.SetPageTagsAsync(first.Id, ["CSharp", "csharp", " CSHARP "]);
        await f.Discovery.SetPageTagsAsync(second.Id, ["CSHARP"]);

        Assert.Equal(new LibraryTagUsage("csharp", 2), Assert.Single((await Reader(f.Discovery).ReadOverviewAsync()).TopTags));
    }

    [Theory]
    [InlineData(LibraryEntryKind.Page)]
    [InlineData(LibraryEntryKind.Notebook)]
    [InlineData(LibraryEntryKind.Space)]
    public async Task Tags_FromHiddenPagesDoNotContribute(LibraryEntryKind kind)
    {
        using var f = await Fixture.CreateAsync();
        var seed = await f.SeedAsync();
        var visible = await f.SaveAsync("Solta");
        await f.Discovery.SetPageTagsAsync(seed.Page.Id, ["oculta", "comum"]);
        await f.Discovery.SetPageTagsAsync(visible.Id, ["comum"]);
        await f.Trash.MoveToTrashAsync(kind, EntryId(seed, kind));

        Assert.Equal(new LibraryTagUsage("comum", 1), Assert.Single((await Reader(f.Discovery).ReadOverviewAsync()).TopTags));
    }

    [Fact]
    public async Task RepeatedReads_ReflectSavingDeletingAndRestoring()
    {
        using var f = await Fixture.CreateAsync();
        var reader = Reader(f.Discovery);
        Counts(await reader.ReadOverviewAsync(), 0, 0, 0, 0);
        var page = await f.SaveAsync("Página");
        Counts(await reader.ReadOverviewAsync(), 0, 0, 1, 1);
        await f.Trash.MoveToTrashAsync(LibraryEntryKind.Page, page.Id);
        Counts(await reader.ReadOverviewAsync(), 0, 0, 0, 0);
        await f.Trash.RestoreAsync(LibraryEntryKind.Page, page.Id);
        Counts(await reader.ReadOverviewAsync(), 0, 0, 1, 1);
    }

    [Fact]
    public async Task SeparateProfiles_KeepTheirOwnCountsAndTags()
    {
        using var first = await Fixture.CreateAsync();
        using var second = await Fixture.CreateAsync();
        var page = await first.SaveAsync("Primeiro perfil");
        await first.Discovery.SetPageTagsAsync(page.Id, ["privada"]);

        Counts(await Reader(first.Discovery).ReadOverviewAsync(), 0, 0, 1, 1);
        var empty = await Reader(second.Discovery).ReadOverviewAsync();
        Counts(empty, 0, 0, 0, 0);
        Assert.Empty(empty.TopTags);
    }

    [Fact]
    public async Task Cancellation_IsPropagatedAndTheReaderCanBeUsedAgain()
    {
        using var f = await Fixture.CreateAsync();
        var reader = Reader(f.Discovery);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadOverviewAsync(cancellation.Token));
        Counts(await reader.ReadOverviewAsync(), 0, 0, 0, 0);
    }

    [Fact]
    public async Task MissingDatabase_IsReportedWithoutCreatingAnEmptyFile()
    {
        using var root = new NotebookTestWorkspace();
        var path = Path.Combine(root.DirectoryPath, "missing.db");
        var reader = Reader(new SqliteKnowledgeDiscovery(path));
        await Assert.ThrowsAsync<NotePersistenceException>(() => reader.ReadOverviewAsync());
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ReadingTheOverview_DoesNotChangeTheDatabaseOrInventAnEditDate()
    {
        using var f = await Fixture.CreateAsync();
        await f.LegacyAsync(new NoteSnapshot(Guid.NewGuid(), "Antiga", "Texto sem data"));
        var before = await File.ReadAllBytesAsync(f.DatabasePath);

        Counts(await Reader(f.Discovery).ReadOverviewAsync(), 0, 0, 1, 1);
        Assert.Empty(await f.Discovery.ListRecentPagesAsync(5));
        Assert.Equal(before, await File.ReadAllBytesAsync(f.DatabasePath));
    }

    [Fact]
    public async Task Composition_RegistersTheReaderForTheSelectedProfile()
    {
        using var root = new NotebookTestWorkspace();
        using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var reader = services.GetService<ILibraryOverviewReader>();
        Assert.NotNull(reader);
        var library = services.GetRequiredService<ILibraryActions>();
        await library.CreateSpaceAsync("Perfil escolhido");

        var result = await reader.ReadOverviewAsync();
        Counts(result, 1, 0, 0, 0);
        Assert.Equal("Perfil escolhido", Assert.Single(result.Spaces).Space.Title);
    }

    private static ILibraryOverviewReader Reader(object source) => Assert.IsAssignableFrom<ILibraryOverviewReader>(source);

    private static void Counts(LibraryOverview result, int spaces, int notebooks, int pages, int unfiled)
    {
        Assert.Equal(spaces, result.SpaceCount);
        Assert.Equal(notebooks, result.NotebookCount);
        Assert.Equal(pages, result.PageCount);
        Assert.Equal(unfiled, result.UnfiledPageCount);
    }

    private static Guid EntryId(Seed seed, LibraryEntryKind kind) => kind switch
    {
        LibraryEntryKind.Space => seed.Space.Id,
        LibraryEntryKind.Notebook => seed.Notebook.Id,
        LibraryEntryKind.Page => seed.Page.Id,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed record Seed(KnowledgeSpace Space, NotebookInfo Notebook, NotebookSnapshot Page);

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string DatabasePath => Path.Combine(Root.DirectoryPath, "trama.db");
        public SqliteLibraryActions Library { get; private set; } = null!;
        public SqliteKnowledgeDiscovery Discovery { get; private set; } = null!;
        public SqliteLibraryTrashActions Trash { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            try
            {
                await fixture.Root.MigrateAsync();
                fixture.Library = new SqliteLibraryActions(fixture.DatabasePath, new LocalImageAttachmentStore(fixture.Root.DirectoryPath));
                fixture.Discovery = new SqliteKnowledgeDiscovery(fixture.DatabasePath);
                fixture.Trash = new SqliteLibraryTrashActions(fixture.DatabasePath);
                return fixture;
            }
            catch
            {
                fixture.Dispose();
                throw;
            }
        }

        public async Task<NotebookSnapshot> SaveAsync(string title, Guid? notebookId = null)
        {
            var page = new NotebookSnapshot(Guid.NewGuid(), title, [new NotebookTextBlock(Guid.NewGuid(), "Texto fictício")]);
            Assert.Equal(NotebookSaveStatus.Saved, (await Library.SaveInNotebookAsync(page, notebookId)).Status);
            return page;
        }

        public async Task LegacyAsync(NoteSnapshot note)
        {
            await using var context = Root.CreateContext();
            await new SqliteNoteRepository(context).AddAsync(note);
        }

        public async Task<Seed> SeedAsync()
        {
            var space = await Library.CreateSpaceAsync("Estudos");
            var notebook = await Library.CreateNotebookAsync(space.Id, "Curso C#");
            return new Seed(space, notebook, await SaveAsync("Interfaces", notebook.Id));
        }

        public void Dispose() => Root.Dispose();
    }
}
