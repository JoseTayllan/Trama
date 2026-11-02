using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class BackupStorageTests
{
    [Fact]
    public async Task RoundTrip_ReopensDatabaseTagsAndImagesWithoutOriginalProfile()
    {
        using var data = new BackupWorkspace();
        var seed = await SeedAsync(data);
        var receipt = await Service(data).CreateAsync(data.Archive);
        Directory.Move(data.Source, Path.Combine(data.Root, "original-unavailable"));

        var restored = await Service(data).RestoreAsync(data.Archive, data.Destination);

        Assert.Equal(data.Archive, receipt.ArchivePath);
        Assert.Equal(2, receipt.AttachmentCount);
        Assert.Equal(data.Destination, restored.DataDirectory);
        using var services = await DesktopComposition.CreateAsync(restored.DataDirectory);
        var library = services.GetRequiredService<ILibraryActions>();
        Assert.Equal(seed.Space, Assert.Single(await library.ListSpacesAsync()));
        Assert.Equal(seed.Book, Assert.Single(await library.ListNotebooksAsync(seed.Space.Id)));
        Assert.Equal(seed.Page.Id, Assert.Single(await library.ListPagesAsync(seed.Book.Id)).Id);
        var page = Assert.IsType<NotebookSnapshot>(await library.LoadAsync(seed.Page.Id));
        Assert.Equal(seed.Page.Title, page.Title);
        Assert.Equal(seed.Page.Blocks, page.Blocks);
        Assert.Equal(new[] { "c#", "revisão" }, await services.GetRequiredService<IKnowledgeDiscovery>().GetPageTagsAsync(page.Id));
        await using var reader = data.Context(data.Destination);
        Assert.Equal(seed.Legacy, await new SqliteNoteRepository(reader).GetByIdAsync(seed.Legacy.Id));
        foreach (var image in seed.Images)
        {
            await using var stream = await library.OpenImageAsync(image.Key);
            Assert.NotNull(stream);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            Assert.Equal(image.Value, buffer.ToArray());
        }
    }

    [Fact]
    public async Task Archive_ManifestDescribesExactPayloadAndExcludesUnreferencedFiles()
    {
        using var data = new BackupWorkspace();
        var seed = await SeedAsync(data);
        await File.WriteAllTextAsync(Path.Combine(data.Source, "private-unrelated.txt"), "Outside backup scope");
        await File.WriteAllTextAsync(Path.Combine(data.Source, "attachments", $"{Guid.NewGuid():N}.image"), "Unreferenced");
        await Service(data).CreateAsync(data.Archive);

        using var archive = ZipFile.OpenRead(data.Archive);
        await using var manifestStream = Assert.Single(archive.Entries, item => item.FullName == "manifest.json").Open();
        var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(manifest);
        Assert.Equal(1, manifest.FormatVersion);
        Assert.Equal(3, manifest.Files.Count);
        Assert.Equal(4, archive.Entries.Count);
        Assert.Contains(manifest.Files, file => file.Path == "trama.db");
        foreach (var file in manifest.Files)
        {
            var entry = archive.GetEntry(file.Path);
            Assert.NotNull(entry);
            await using var stream = entry.Open();
            using var bytes = new MemoryStream();
            await stream.CopyToAsync(bytes);
            Assert.Equal(file.Length, bytes.Length);
            Assert.Equal(file.Sha256, Hash(bytes.ToArray()));
        }
        foreach (var id in seed.Images.Keys) Assert.Contains(manifest.Files, item => item.Path == $"attachments/{id:N}.image");
    }

    [Fact]
    public async Task Backup_UsesCommittedWalSnapshotWhileWriterIsOpen()
    {
        using var data = new BackupWorkspace();
        var seed = await SeedAsync(data);
        await using var writer = new SqliteConnection($"Data Source={Path.Combine(data.Source, "trama.db")};Pooling=False");
        await writer.OpenAsync();
        await using (var command = writer.CreateCommand())
        {
            command.CommandText = "PRAGMA journal_mode=WAL;";
            Assert.Equal("wal", await command.ExecuteScalarAsync());
            command.CommandText = "PRAGMA wal_autocheckpoint=0; UPDATE Notes SET Body = 'Committed WAL text';";
            await command.ExecuteNonQueryAsync();
        }
        await using var transaction = await writer.BeginTransactionAsync();
        await using (var command = writer.CreateCommand())
        {
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = "UPDATE Notes SET Body = 'Uncommitted text';";
            await command.ExecuteNonQueryAsync();
        }

        await Service(data).CreateAsync(data.Archive);
        await transaction.RollbackAsync();
        await Service(data).RestoreAsync(data.Archive, data.Destination);

        await using var reader = data.Context(data.Destination);
        Assert.Equal(seed.Legacy with { Body = "Committed WAL text" },
            await new SqliteNoteRepository(reader).GetByIdAsync(seed.Legacy.Id));
    }

    [Fact]
    public async Task Backup_MissingReferencedImageDoesNotPublishOrLeaveTemporaryFiles()
    {
        using var data = new BackupWorkspace();
        var seed = await SeedAsync(data);
        File.Delete(Path.Combine(data.Source, "attachments", $"{seed.Images.Keys.First():N}.image"));
        var before = data.Paths();

        await Assert.ThrowsAsync<InvalidDataException>(() => Service(data).CreateAsync(data.Archive));

        Assert.Equal(before, data.Paths());
    }

    [Fact]
    public async Task Backup_ExistingOutputIsNeverOverwritten()
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        await File.WriteAllTextAsync(data.Archive, "Keep existing backup");

        await Assert.ThrowsAsync<IOException>(() => Service(data).CreateAsync(data.Archive));

        Assert.Equal("Keep existing backup", await File.ReadAllTextAsync(data.Archive));
    }

    [Fact]
    public async Task Restore_ExistingDestinationIsNeverMergedOrOverwritten()
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        await Service(data).CreateAsync(data.Archive);
        Directory.CreateDirectory(data.Destination);
        var marker = Path.Combine(data.Destination, "preserve.txt");
        await File.WriteAllTextAsync(marker, "Preserve destination");

        await Assert.ThrowsAsync<IOException>(() => Service(data).RestoreAsync(data.Archive, data.Destination));

        Assert.Equal("Preserve destination", await File.ReadAllTextAsync(marker));
        Assert.Single(Directory.GetFiles(data.Destination));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("attachments/../../escape.txt")]
    [InlineData("/absolute.txt")]
    [InlineData("C:/absolute.txt")]
    public async Task Restore_RejectsUnsafeArchivePathsWithoutCreatingFiles(string unsafePath)
    {
        using var data = new BackupWorkspace();
        await WriteArchiveAsync(data.Archive, new Dictionary<string, byte[]> { ["trama.db"] = [1], [unsafePath] = [2] });
        await RejectRestoreAsync(data);
    }

    [Theory]
    [InlineData("hash")]
    [InlineData("missing-manifest")]
    [InlineData("missing-database")]
    [InlineData("unknown-format")]
    [InlineData("duplicate")]
    [InlineData("extra-entry")]
    public async Task Restore_RejectsInvalidPackageAndCleansStaging(string fault)
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        await WriteArchiveAsync(data.Archive, await PayloadAsync(data), fault == "unknown-format" ? 99 : 1);
        using (var archive = ZipFile.Open(data.Archive, ZipArchiveMode.Update))
        {
            if (fault == "missing-manifest") archive.GetEntry("manifest.json")!.Delete();
            if (fault == "missing-database") archive.GetEntry("trama.db")!.Delete();
            if (fault == "hash")
            {
                archive.GetEntry("trama.db")!.Delete();
                await using var stream = archive.CreateEntry("trama.db").Open();
                await stream.WriteAsync(new byte[] { 1, 2, 3 });
            }
            if (fault == "duplicate") archive.CreateEntry("trama.db");
            if (fault == "extra-entry") archive.CreateEntry("unlisted.txt");
        }
        await RejectRestoreAsync(data);
    }

    [Fact]
    public async Task Restore_MissingReferencedImageIsRejectedEvenWithValidManifest()
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        var payload = await PayloadAsync(data);
        payload.Remove(payload.Keys.First(key => key.StartsWith("attachments/", StringComparison.Ordinal)));
        await WriteArchiveAsync(data.Archive, payload);
        await RejectRestoreAsync(data);
    }

    [Fact]
    public async Task Restore_InvalidDatabaseIsRejectedEvenWithValidChecksum()
    {
        using var data = new BackupWorkspace();
        await WriteArchiveAsync(data.Archive, new Dictionary<string, byte[]> { ["trama.db"] = Encoding.UTF8.GetBytes("Not a SQLite database") });
        await RejectRestoreAsync(data);
    }

    [Fact]
    public async Task Restore_FutureDatabaseSchemaIsRejectedBeforePublishing()
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        await using (var context = data.Context(data.Source))
            await context.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsHistory(MigrationId, ProductVersion) VALUES ('99999999999999_Future', '99.0.0');");
        await WriteArchiveAsync(data.Archive, await PayloadAsync(data));
        await RejectRestoreAsync(data);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Restore_EnforcesExpandedSizeAndEntryBudgets(bool sizeLimit)
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        await WriteArchiveAsync(data.Archive, await PayloadAsync(data));
        var before = data.Paths();
        var limits = sizeLimit ? new BackupArchiveLimits(MaximumExpandedBytes: 32) : new BackupArchiveLimits(MaximumEntries: 1);

        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(data.Source, limits).RestoreAsync(data.Archive, data.Destination));

        Assert.Equal(before, data.Paths());
    }

    [Fact]
    public async Task PreCancellation_DoesNotCreateArchiveOrDestination()
    {
        using var data = new BackupWorkspace();
        await SeedAsync(data);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var before = data.Paths();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(data).CreateAsync(data.Archive, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(data).RestoreAsync(data.Archive, data.Destination, cancellation.Token));

        Assert.Equal(before, data.Paths());
    }

    private static async Task RejectRestoreAsync(BackupWorkspace data)
    {
        var before = data.Paths();
        await Assert.ThrowsAsync<InvalidDataException>(() => Service(data).RestoreAsync(data.Archive, data.Destination));
        Assert.Equal(before, data.Paths());
    }

    private static LocalBackupService Service(BackupWorkspace data) => new(data.Source);
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static async Task<BackupSeed> SeedAsync(BackupWorkspace data)
    {
        var database = await LocalDatabase.InitializeAsync(data.Source);
        var images = new LocalImageAttachmentStore(data.Source);
        var png = await images.ImportAsync(new MemoryStream(NotebookTestWorkspace.Png));
        var jpeg = await images.ImportAsync(new MemoryStream(NotebookTestWorkspace.Jpeg));
        var actions = new SqliteLibraryActions(database, images);
        var space = await actions.CreateSpaceAsync("Curso C#");
        var book = await actions.CreateNotebookAsync(space.Id, "Fundamentos");
        var page = new NotebookSnapshot(Guid.NewGuid(), "  Aula 🧵  ",
        [
            new NotebookTextBlock(Guid.NewGuid(), "  Texto original\r\n"),
            new NotebookCodeBlock(Guid.NewGuid(), "    Console.WriteLine(42);\t\n", "csharp"),
            new NotebookImageBlock(Guid.NewGuid(), png.Id, "Print PNG"),
            new NotebookImageBlock(Guid.NewGuid(), jpeg.Id, "Print JPEG"),
            new NotebookImageBlock(Guid.NewGuid(), png.Id, "Mesma imagem, sem duplicar arquivo")
        ]);
        Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(page, book.Id)).Status);
        await new SqliteKnowledgeDiscovery(database).SetPageTagsAsync(page.Id, ["c#", "revisão"]);
        var legacy = new NoteSnapshot(Guid.NewGuid(), "Nota antiga", "\r\n    print(42)\t🧵");
        await using var writer = data.Context(data.Source);
        await new SqliteNoteRepository(writer).AddAsync(legacy);
        return new BackupSeed(space, book, page, legacy, new Dictionary<Guid, byte[]> { [png.Id] = NotebookTestWorkspace.Png, [jpeg.Id] = NotebookTestWorkspace.Jpeg });
    }

    private static async Task<Dictionary<string, byte[]>> PayloadAsync(BackupWorkspace data)
    {
        var payload = new Dictionary<string, byte[]> { ["trama.db"] = await File.ReadAllBytesAsync(Path.Combine(data.Source, "trama.db")) };
        foreach (var file in Directory.GetFiles(Path.Combine(data.Source, "attachments"), "*.image"))
            payload["attachments/" + Path.GetFileName(file)] = await File.ReadAllBytesAsync(file);
        return payload;
    }

    private static async Task WriteArchiveAsync(string path, Dictionary<string, byte[]> payload, int formatVersion = 1)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var files = new List<BackupFileEntry>();
        foreach (var (name, bytes) in payload)
        {
            await using var stream = archive.CreateEntry(name).Open();
            await stream.WriteAsync(bytes);
            files.Add(new BackupFileEntry(name, bytes.LongLength, Hash(bytes)));
        }
        await using var manifest = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(manifest, new BackupManifest(formatVersion, files), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed record BackupSeed(KnowledgeSpace Space, NotebookInfo Book, NotebookSnapshot Page, NoteSnapshot Legacy, Dictionary<Guid, byte[]> Images);

    private sealed class BackupWorkspace : IDisposable
    {
        private readonly NotebookTestWorkspace _workspace = new();
        public string Root => _workspace.DirectoryPath;
        public string Source => Path.Combine(Root, "source");
        public string Destination => Path.Combine(Root, "restored");
        public string Archive => Path.Combine(Root, "copy.trama-backup");
        public string[] Paths() => Directory.GetFileSystemEntries(Root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        public TramaDbContext Context(string directory) => new(new DbContextOptionsBuilder<TramaDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "trama.db"), Pooling = false }.ToString()).Options);
        public void Dispose() => _workspace.Dispose();
    }
}
