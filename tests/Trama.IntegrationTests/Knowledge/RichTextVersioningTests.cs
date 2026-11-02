using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class RichTextVersioningTests
{
    [Fact]
    public async Task SavingEnrichedPage_DeclaresVersionThreeAndRetainsExactPayload()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = RichPage();

        await SaveAsync(data, page);

        Assert.Equal(3, await VersionAsync(data, page.Id));
        await AssertPageAsync(data.DirectoryPath, page);
    }

    [Fact]
    public async Task ReadingVersionThree_ReopensEveryAttributeInANewContext()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = RichPage();
        await SeedAsync(data, page, 3);

        await AssertPageAsync(data.DirectoryPath, page);
    }

    [Fact]
    public async Task UpdatingVersionThree_KeepsIdentityAndReplacesThePreviousStyleRows()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var original = RichPage();
        await SeedAsync(data, original, 3);
        var text = (NotebookTextBlock)original.Blocks[0];
        var updated = original with
        {
            Title = "Revisão da aula",
            Blocks = [text with { Styles = [new TextStyleSpan(1, 4, InlineTextStyle.Italic) { FontSize = 32, Foreground = "coral" }] }, original.Blocks[1]]
        };

        await SaveAsync(data, updated);

        Assert.Equal(3, await VersionAsync(data, original.Id));
        await AssertPageAsync(data.DirectoryPath, updated);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task RemovingEnrichedAttributes_UsesTheRemainingContentVersion(bool keepBold, int expectedVersion)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var original = RichPage();
        await SeedAsync(data, original, 3);
        var updated = original with
        {
            Blocks = [((NotebookTextBlock)original.Blocks[0]) with
            {
                Styles = keepBold ? [new TextStyleSpan(0, 4, InlineTextStyle.Bold)] : []
            }, original.Blocks[1]]
        };

        await SaveAsync(data, updated);

        Assert.Equal(expectedVersion, await VersionAsync(data, updated.Id));
        await AssertPageAsync(data.DirectoryPath, updated);
        if (!keepBold)
        {
            await using var context = data.CreateContext();
            Assert.Equal(0, await context.Database.SqlQuery<int>(
                $"SELECT COUNT(*) AS Value FROM NotebookTextStyles WHERE PageId={updated.Id}").SingleAsync());
        }
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task PreviousPlainAndBoldFormats_LoadAndSaveWithoutPromotion(bool bold, int version)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = LegacyPage(bold);
        await SeedAsync(data, page, version);

        await AssertPageAsync(data.DirectoryPath, page);
        var updated = page with { Title = "Título atualizado" };
        await SaveAsync(data, updated);

        Assert.Equal(version, await VersionAsync(data, page.Id));
        await AssertPageAsync(data.DirectoryPath, updated);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task EnrichedAttributesLabeledAsLegacyVersion_AreRejectedWithoutChangingStoredData(int version)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = RichPage();
        await SeedAsync(data, page, version);
        var before = await StoredStateAsync(data, page.Id);

        await using (var context = data.CreateContext())
            await Assert.ThrowsAsync<NotePersistenceException>(() => new SqliteNotebookRepository(context).GetByIdAsync(page.Id));

        Assert.Equal(before, await StoredStateAsync(data, page.Id));
    }

    [Theory]
    [InlineData("size")]
    [InlineData("foreground")]
    [InlineData("highlight")]
    public async Task SavingInvalidAttributes_DoesNotOverwriteTheExistingPage(string fault)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var original = LegacyPage(true);
        await SaveAsync(data, original);
        var before = await StoredStateAsync(data, original.Id);
        var invalidSpan = fault switch
        {
            "size" => new TextStyleSpan(0, 4, InlineTextStyle.Bold) { FontSize = 0 },
            "foreground" => new TextStyleSpan(0, 4, InlineTextStyle.Bold) { Foreground = "unknown-color" },
            _ => new TextStyleSpan(0, 4, InlineTextStyle.Bold) { Highlight = "unknown-color" }
        };
        var invalid = original with
        {
            Title = "Não sobrescrever",
            Blocks = [((NotebookTextBlock)original.Blocks[0]) with { Styles = [invalidSpan] }]
        };

        await Assert.ThrowsAnyAsync<ArgumentException>(() => SaveAsync(data, invalid));

        Assert.Equal(before, await StoredStateAsync(data, original.Id));
        await AssertPageAsync(data.DirectoryPath, original);
    }

    [Fact]
    public async Task FuturePageVersion_IsNeitherReadNorOverwritten()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = LegacyPage(true);
        await SeedAsync(data, page, 99);
        var before = await StoredStateAsync(data, page.Id);

        await using (var reader = data.CreateContext())
            await Assert.ThrowsAsync<NotePersistenceException>(() => new SqliteNotebookRepository(reader).GetByIdAsync(page.Id));
        await Assert.ThrowsAsync<NotePersistenceException>(() => SaveAsync(data, page with { Title = "Não gravar" }));

        Assert.Equal(before, await StoredStateAsync(data, page.Id));
    }

    [Fact]
    public async Task BackupOfVersionThree_RestoresAttributesCodeAndImageWithoutChangingTheSource()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = await RichPageWithImageAsync(data);
        await SeedAsync(data, page, 3);
        var before = await StoredStateAsync(data, page.Id);
        var archive = Path.Combine(data.DirectoryPath, "rich.trama-backup");
        var destination = Path.Combine(data.DirectoryPath, "restored");
        var service = new LocalBackupService(data.DirectoryPath);

        await service.CreateAsync(archive);
        await service.RestoreAsync(archive, destination);

        await AssertPageAsync(destination, page);
        Assert.Equal(3, await VersionInDirectoryAsync(destination, page.Id));
        var image = (NotebookImageBlock)page.Blocks[2];
        Assert.Equal(NotebookTestWorkspace.Png, await File.ReadAllBytesAsync(
            Path.Combine(destination, "attachments", $"{image.AttachmentId:N}.image")));
        Assert.Equal(before, await StoredStateAsync(data, page.Id));
        Assert.Equal(1, await PackageVersionAsync(archive));
    }

    [Fact]
    public async Task RestoringIndependentVersionThreePackage_PreservesAllAttributes()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = RichPage();
        await SeedAsync(data, page, 3);
        var archive = Path.Combine(data.DirectoryPath, "external.trama-backup");
        await WritePackageAsync(data, archive);
        var destination = Path.Combine(data.DirectoryPath, "external-restored");

        await new LocalBackupService(data.DirectoryPath).RestoreAsync(archive, destination);

        await AssertPageAsync(destination, page);
        Assert.Equal(3, await VersionInDirectoryAsync(destination, page.Id));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task RestoringPreviousPageFormats_KeepsTheirVersionAndContents(bool bold, int version)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = LegacyPage(bold);
        await SeedAsync(data, page, version);
        var archive = Path.Combine(data.DirectoryPath, "legacy.trama-backup");
        await WritePackageAsync(data, archive);
        var destination = Path.Combine(data.DirectoryPath, "legacy-restored");

        await new LocalBackupService(data.DirectoryPath).RestoreAsync(archive, destination);

        await AssertPageAsync(destination, page);
        Assert.Equal(version, await VersionInDirectoryAsync(destination, page.Id));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task BackupRejectsEnrichedAttributesWithLegacyVersion_WithoutPublishingOrLeavingStaging(int version)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = RichPage();
        await SeedAsync(data, page, version);
        var archive = Path.Combine(data.DirectoryPath, "invalid.trama-backup");
        var before = Paths(data);
        var original = await StoredStateAsync(data, page.Id);

        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(data.DirectoryPath).CreateAsync(archive));

        Assert.Equal(before, Paths(data));
        Assert.Equal(original, await StoredStateAsync(data, page.Id));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RestoreRejectsEnrichedAttributesWithLegacyVersion_WithoutPublishingOrLeavingStaging(int version)
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = RichPage();
        await SeedAsync(data, page, version);
        var archive = Path.Combine(data.DirectoryPath, "invalid.trama-backup");
        await WritePackageAsync(data, archive);
        var originalArchive = await File.ReadAllBytesAsync(archive);
        var before = Paths(data);

        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(data.DirectoryPath).RestoreAsync(
            archive, Path.Combine(data.DirectoryPath, "invalid-restored")));

        Assert.Equal(before, Paths(data));
        Assert.Equal(originalArchive, await File.ReadAllBytesAsync(archive));
    }

    private static NotebookSnapshot RichPage() => new(Guid.NewGuid(), "  Aula 🧵  ",
    [
        new NotebookTextBlock(Guid.NewGuid(), "Texto 🧵 em destaque\r\n")
        {
            Styles =
            [
                new TextStyleSpan(0, 5, InlineTextStyle.Bold | InlineTextStyle.Italic)
                { FontSize = 24, Foreground = "teal", Highlight = "amber" },
                new TextStyleSpan(9, 2, InlineTextStyle.None) { Foreground = "blue", Highlight = "violet" }
            ]
        },
        new NotebookCodeBlock(Guid.NewGuid(), "    Console.WriteLine(42);\t\n", "csharp")
    ]);

    private static NotebookSnapshot LegacyPage(bool bold) => new(Guid.NewGuid(), "Antiga",
    [new NotebookTextBlock(Guid.NewGuid(), "Texto antigo 🧵\r\n")
    { Styles = bold ? [new TextStyleSpan(0, 5, InlineTextStyle.Bold)] : [] }]);

    private static async Task<NotebookSnapshot> RichPageWithImageAsync(NotebookTestWorkspace data)
    {
        using var source = new MemoryStream(NotebookTestWorkspace.Png);
        var image = await new LocalImageAttachmentStore(data.DirectoryPath).ImportAsync(source);
        var page = RichPage();
        return page with { Blocks = [.. page.Blocks, new NotebookImageBlock(Guid.NewGuid(), image.Id, "Print da aula")] };
    }

    private static async Task SaveAsync(NotebookTestWorkspace data, NotebookSnapshot page)
    {
        await using var context = data.CreateContext();
        await new SqliteNotebookRepository(context).SaveAsync(page);
    }

    private static async Task SeedAsync(NotebookTestWorkspace data, NotebookSnapshot page, int version)
    {
        await SaveAsync(data, page);
        await using var context = data.CreateContext();
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE NotebookPages SET FormatVersion={version} WHERE Id={page.Id}");
    }

    private static async Task AssertPageAsync(string directory, NotebookSnapshot expected)
    {
        await using var context = Context(directory);
        var actual = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(context).GetByIdAsync(expected.Id));
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Blocks.Count, actual.Blocks.Count);
        for (var index = 0; index < expected.Blocks.Count; index++)
        {
            if (expected.Blocks[index] is NotebookTextBlock text)
            {
                var restored = Assert.IsType<NotebookTextBlock>(actual.Blocks[index]);
                Assert.Equal(text.Id, restored.Id);
                Assert.Equal(text.Text, restored.Text);
                Assert.Equal(text.Styles, restored.Styles);
            }
            else Assert.Equal(expected.Blocks[index], actual.Blocks[index]);
        }
    }

    private static Task<int> VersionAsync(NotebookTestWorkspace data, Guid id) => VersionInDirectoryAsync(data.DirectoryPath, id);

    private static async Task<int> VersionInDirectoryAsync(string directory, Guid id)
    {
        await using var context = Context(directory);
        return await context.Database.SqlQuery<int>($"SELECT FormatVersion AS Value FROM NotebookPages WHERE Id={id}").SingleAsync();
    }

    private static async Task<string> StoredStateAsync(NotebookTestWorkspace data, Guid id)
    {
        await using var context = data.CreateContext();
        var version = await context.Database.SqlQuery<int>($"SELECT FormatVersion AS Value FROM NotebookPages WHERE Id={id}").SingleAsync();
        var title = await context.Database.SqlQuery<string>($"SELECT Title AS Value FROM NotebookPages WHERE Id={id}").SingleAsync();
        var blocks = await context.Database.SqlQuery<string>($"SELECT Content AS Value FROM NotebookBlocks WHERE PageId={id} ORDER BY Position").ToArrayAsync();
        var styles = await context.Database.SqlQuery<string>($"SELECT Styles AS Value FROM NotebookTextStyles WHERE PageId={id} ORDER BY BlockId").ToArrayAsync();
        return JsonSerializer.Serialize(new { version, title, blocks, styles });
    }

    private static TramaDbContext Context(string directory) => new(new DbContextOptionsBuilder<TramaDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "trama.db"), Pooling = false }.ToString()).Options);

    private static string[] Paths(NotebookTestWorkspace data) => Directory.GetFileSystemEntries(
        data.DirectoryPath, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();

    private static async Task WritePackageAsync(NotebookTestWorkspace data, string path)
    {
        // Build a valid package independently of CreateAsync, so restore acceptance
        // cannot be hidden by a writer that accidentally labels new content as v2.
        var snapshotPath = Path.Combine(data.DirectoryPath, $"snapshot-{Guid.NewGuid():N}.db");
        await using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = Path.Combine(data.DirectoryPath, "trama.db"), Pooling = false }.ToString()))
        await using (var target = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = snapshotPath, Pooling = false }.ToString()))
        {
            await source.OpenAsync();
            await target.OpenAsync();
            source.BackupDatabase(target);
        }
        var bytes = await File.ReadAllBytesAsync(snapshotPath);
        File.Delete(snapshotPath);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        await using (var database = archive.CreateEntry("trama.db").Open()) await database.WriteAsync(bytes);
        await using var manifest = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(manifest, new BackupManifest(1,
            [new BackupFileEntry("trama.db", bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant())]),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static async Task<int> PackageVersionAsync(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        await using var stream = archive.GetEntry("manifest.json")!.Open();
        var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Assert.IsType<BackupManifest>(manifest).FormatVersion;
    }
}
