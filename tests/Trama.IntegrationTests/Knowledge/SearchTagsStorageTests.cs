using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class SearchTagsStorageTests
{
    [Theory]
    [InlineData("VARIAVEIS")]
    [InlineData("  explicacao  ")]
    [InlineData("Console.WriteLine")]
    [InlineData("diagrama")]
    public async Task Search_FindsTitleTextCodeAndImageCaptionWithoutCaseOrAccentDistinction(string query)
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);

        var result = Assert.Single(await Discovery(data).SearchAsync(query));

        Assert.Equal(seed.Page.Id, result.PageId);
        Assert.Equal(seed.Page.Title, result.Title);
        Assert.Equal(seed.Space.Id, result.SpaceId);
        Assert.Equal(seed.Space.Title, result.SpaceTitle);
        Assert.Equal(seed.Book.Id, result.NotebookId);
        Assert.Equal(seed.Book.Title, result.NotebookTitle);
    }

    [Theory]
    [InlineData("100%_feito")]
    [InlineData("' OR 1=1 --")]
    public async Task Search_TreatsSqlPunctuationAsLiteralText(string query)
    {
        using var data = new NotebookTestWorkspace();
        await SeedAsync(data);
        var exact = new NoteSnapshot(Guid.NewGuid(), "Literal", query);
        await using (var context = data.CreateContext()) await new SqliteNoteRepository(context).AddAsync(exact);

        Assert.Equal(exact.Id, Assert.Single(await Discovery(data).SearchAsync(query)).PageId);
    }

    [Fact]
    public async Task Search_GlobalDeduplicatesMatchesAndIncludesUnfiledLegacyNotes()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        var legacy = new NoteSnapshot(Guid.NewGuid(), "Explicação antiga", "Explicação também no corpo");
        await using (var context = data.CreateContext()) await new SqliteNoteRepository(context).AddAsync(legacy);

        var results = await Discovery(data).SearchAsync("explicacao");

        Assert.Equal(2, results.Count);
        Assert.Single(results, item => item.PageId == seed.Page.Id);
        var old = Assert.Single(results, item => item.PageId == legacy.Id);
        Assert.Null(old.SpaceId);
        Assert.Null(old.NotebookId);
    }

    [Fact]
    public async Task Search_StructuredPageShadowsLegacyBodyWithSameIdentity()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        await using (var context = data.CreateContext())
            await new SqliteNoteRepository(context).AddAsync(new NoteSnapshot(seed.Page.Id, "Histórico", "somente-no-legado"));

        Assert.Empty(await Discovery(data).SearchAsync("somente-no-legado"));
        Assert.Equal(seed.Page.Id, Assert.Single(await Discovery(data).SearchAsync("variaveis")).PageId);
    }

    [Fact]
    public async Task Search_EmptyQueryListsPagesAndUnknownTextReturnsEmpty()
    {
        using var data = new NotebookTestWorkspace();
        await SeedAsync(data);

        Assert.Equal(2, (await Discovery(data).SearchAsync(" \t ")).Count);
        Assert.Empty(await Discovery(data).SearchAsync("conteudo-inexistente"));
    }

    [Fact]
    public async Task Tags_NormalizeDeduplicateAndReopenWithoutChangingContent()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);

        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["  C#  ", "c#", "Revisão", "REVISA\u0303O"]);

        Assert.Equal(new[] { "c#", "revisão" }, await Discovery(data).GetPageTagsAsync(seed.Page.Id));
        await using var reader = data.CreateContext();
        var page = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(seed.Page.Id));
        Assert.Equal(seed.Page.Title, page.Title);
        Assert.Equal(seed.Page.Blocks, page.Blocks);
        Assert.Equal(seed.Page.Id, Assert.Single(await new SqliteKnowledgeCatalog(reader).ListPagesAsync(seed.Book.Id)).Id);
    }

    [Fact]
    public async Task Tags_ReplacementRemovalAndPageIsolationAreDurable()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["c#", "estudo"]);
        await Discovery(data).SetPageTagsAsync(seed.Other.Id, ["trabalho"]);
        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["revisão"]);
        Assert.Equal(new[] { "revisão" }, await Discovery(data).GetPageTagsAsync(seed.Page.Id));

        await Discovery(data).SetPageTagsAsync(seed.Page.Id, []);

        Assert.Empty(await Discovery(data).GetPageTagsAsync(seed.Page.Id));
        Assert.Equal(new[] { "trabalho" }, await Discovery(data).GetPageTagsAsync(seed.Other.Id));
    }

    [Fact]
    public async Task Tags_InvalidReplacementKeepsTheWholePreviousSet()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["existente"]);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => Discovery(data).SetPageTagsAsync(seed.Page.Id, ["nova", " \t "]));

        Assert.Equal(new[] { "existente" }, await Discovery(data).GetPageTagsAsync(seed.Page.Id));
    }

    [Fact]
    public async Task Tags_UnknownPageCannotCreateAnOrphan()
    {
        using var data = new NotebookTestWorkspace();
        await SeedAsync(data);
        var missing = Guid.NewGuid();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => Discovery(data).SetPageTagsAsync(missing, ["órfã"]));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Discovery(data).GetPageTagsAsync(missing));
    }

    [Fact]
    public async Task Search_TagFilterIsExactAndCombinedWithText()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["c#"]);
        await Discovery(data).SetPageTagsAsync(seed.Other.Id, ["c#-avançado"]);

        Assert.Equal(seed.Page.Id, Assert.Single(await Discovery(data).SearchAsync("", " C# ")).PageId);
        Assert.Equal(seed.Page.Id, Assert.Single(await Discovery(data).SearchAsync("explicacao", "c#")).PageId);
        Assert.Empty(await Discovery(data).SearchAsync("reunião", "c#"));
        Assert.Empty(await Discovery(data).SearchAsync("", "desconhecida"));
        Assert.Equal(2, (await Discovery(data).SearchAsync("", " \t ")).Count);
    }

    [Fact]
    public async Task Tags_LegacyNoteSupportsTagsWithoutRewritingItsBody()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var note = new NoteSnapshot(Guid.NewGuid(), "Antiga", "\r\n    print(42)\t🧵");
        await using (var context = data.CreateContext()) await new SqliteNoteRepository(context).AddAsync(note);

        await Discovery(data).SetPageTagsAsync(note.Id, ["python"]);

        Assert.Equal(note.Id, Assert.Single(await Discovery(data).SearchAsync("print", "python")).PageId);
        await using var reader = data.CreateContext();
        Assert.Equal(note, await new SqliteNoteRepository(reader).GetByIdAsync(note.Id));
    }

    [Fact]
    public async Task Search_ReflectsEditsRenameAndMoveWhileTagsStayAttached()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["estudo"]);
        var destination = new NotebookInfo(Guid.NewGuid(), seed.Space.Id, "Revisões");
        await using (var context = data.CreateContext())
        {
            await new SqliteKnowledgeCatalog(context).AddNotebookAsync(destination);
            await new SqliteNotebookRepository(context).SaveAsync(seed.Page with
            {
                Blocks = [new NotebookTextBlock(Guid.NewGuid(), "Texto substituído")]
            });
        }
        var management = new SqliteLibraryManagement(Path.Combine(data.DirectoryPath, "trama.db"));
        await management.RenameAsync(LibraryEntryKind.Page, seed.Page.Id, "Aula nova");
        await management.MovePageAsync(seed.Page.Id, destination.Id);

        Assert.Empty(await Discovery(data).SearchAsync("explicacao"));
        var result = Assert.Single(await Discovery(data).SearchAsync("substituido", "estudo"));
        Assert.Equal("Aula nova", result.Title);
        Assert.Equal(destination.Id, result.NotebookId);
        Assert.Equal(destination.Title, result.NotebookTitle);
    }

    [Fact]
    public async Task Discovery_PreCancellationDoesNotReadOrReplaceTags()
    {
        using var data = new NotebookTestWorkspace();
        var seed = await SeedAsync(data);
        await Discovery(data).SetPageTagsAsync(seed.Page.Id, ["preservar"]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Discovery(data).SearchAsync("", cancellationToken: cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Discovery(data).GetPageTagsAsync(seed.Page.Id, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Discovery(data).SetPageTagsAsync(seed.Page.Id, ["outra"], cancellation.Token));
        Assert.Equal(new[] { "preservar" }, await Discovery(data).GetPageTagsAsync(seed.Page.Id));
    }

    [Fact]
    public async Task Search_MissingSchemaReportsFailureInsteadOfEmptyResults()
    {
        using var data = new NotebookTestWorkspace();

        await Assert.ThrowsAsync<NotePersistenceException>(() => Discovery(data).SearchAsync("aula"));
    }

    [Fact]
    public async Task Migration_AddsTagsWithoutRewritingTheExistingThreeMigrations()
    {
        using var data = new NotebookTestWorkspace();
        var note = new NoteSnapshot(Guid.NewGuid(), "Anterior", "Conteúdo original");
        await using (var old = data.CreateContext())
        {
            await old.GetService<IMigrator>().MigrateAsync("20261028003249_KnowledgeCatalog");
            await new SqliteNoteRepository(old).AddAsync(note);
        }
        await data.MigrateAsync();
        await using var reader = data.CreateContext();
        Assert.True((await reader.Database.GetAppliedMigrationsAsync()).Count() > 3, "Add a new migration for tags; preserve the existing migrations.");
        Assert.Equal(note, await new SqliteNoteRepository(reader).GetByIdAsync(note.Id));
        Assert.Empty(await Discovery(data).GetPageTagsAsync(note.Id));
    }

    private static SqliteKnowledgeDiscovery Discovery(NotebookTestWorkspace data) => new(Path.Combine(data.DirectoryPath, "trama.db"));

    private static async Task<Seed> SeedAsync(NotebookTestWorkspace data)
    {
        await data.MigrateAsync();
        var space = new KnowledgeSpace(Guid.NewGuid(), "Curso C#");
        var book = new NotebookInfo(Guid.NewGuid(), space.Id, "Fundamentos");
        var page = new NotebookSnapshot(Guid.NewGuid(), "Variáveis",
        [
            new NotebookTextBlock(Guid.NewGuid(), "  Explicação sobre variáveis\r\n"),
            new NotebookCodeBlock(Guid.NewGuid(), "    Console.WriteLine(42);\n", "csharp"),
            new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Diagrama da memória")
        ]);
        var other = new NoteSnapshot(Guid.NewGuid(), "Reunião", "Ata de trabalho");
        await using var context = data.CreateContext();
        var catalog = new SqliteKnowledgeCatalog(context);
        await catalog.AddSpaceAsync(space);
        await catalog.AddNotebookAsync(book);
        await new SqliteNotebookRepository(context).SaveAsync(page);
        await catalog.PlacePageAsync(page.Id, book.Id);
        await new SqliteNoteRepository(context).AddAsync(other);
        return new Seed(space, book, page, other);
    }

    private sealed record Seed(KnowledgeSpace Space, NotebookInfo Book, NotebookSnapshot Page, NoteSnapshot Other);
}
