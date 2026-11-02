using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class RichTextStorageTests
{
    [Fact]
    public async Task FormattedPage_ReopensStylesAndPreservesExactTextCodeAndIdentity()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = FormattedPage();
        await using (var writer = data.CreateContext()) await new SqliteNotebookRepository(writer).SaveAsync(page);

        await using var reader = data.CreateContext();
        var restored = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(page.Id));

        Assert.Equal(page.Id, restored.Id);
        Assert.Equal(page.Title, restored.Title);
        var text = Assert.IsType<NotebookTextBlock>(restored.Blocks[0]);
        Assert.Equal(page.Blocks[0].Id, text.Id);
        Assert.Equal(((NotebookTextBlock)page.Blocks[0]).Text, text.Text);
        Assert.Equal(((NotebookTextBlock)page.Blocks[0]).Styles, text.Styles);
        Assert.Equal(page.Blocks[1], restored.Blocks[1]);
    }

    [Fact]
    public async Task Migration_AddsInlineStylesWithoutChangingOldTextOrLegacyNotes()
    {
        using var data = new NotebookTestWorkspace();
        var pageId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var note = new NoteSnapshot(Guid.NewGuid(), "Antiga", "\r\n    print(42)\t🧵");
        const string body = "  Texto antigo 🧵\r\n";
        await using (var old = data.CreateContext())
        {
            await old.GetService<IMigrator>().MigrateAsync("20261029011618_PageTags");
            await new SqliteNoteRepository(old).AddAsync(note);
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO NotebookPages(Id,Title,FormatVersion) VALUES ({pageId}, 'Página antiga', 1);");
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO NotebookBlocks(PageId,Id,Position,Kind,Content,Language) VALUES ({pageId},{blockId},0,'text',{body},'');");
        }

        await data.MigrateAsync();

        await using var reader = data.CreateContext();
        Assert.True((await reader.Database.GetAppliedMigrationsAsync()).Count() > 4, "Add a migration; preserve the four existing migrations.");
        var oldPage = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(pageId));
        var block = Assert.IsType<NotebookTextBlock>(Assert.Single(oldPage.Blocks));
        Assert.Equal(body, block.Text);
        Assert.Equal(blockId, block.Id);
        Assert.Empty(block.Styles);
        Assert.Equal(note, await new SqliteNoteRepository(reader).GetByIdAsync(note.Id));
    }

    [Fact]
    public async Task Backup_RoundTripRetainsInlineStylesAndSearchableText()
    {
        using var root = new NotebookTestWorkspace();
        var source = Path.Combine(root.DirectoryPath, "source");
        var database = await LocalDatabase.InitializeAsync(source);
        var actions = new SqliteNotebookActions(database, new LocalImageAttachmentStore(source));
        var page = FormattedPage();
        Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveAsync(page)).Status);
        var service = new LocalBackupService(source);
        var archive = Path.Combine(root.DirectoryPath, "formatted.trama-backup");
        var destination = Path.Combine(root.DirectoryPath, "restored");

        await service.CreateAsync(archive);
        await service.RestoreAsync(archive, destination);

        var restored = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookActions(Path.Combine(destination, "trama.db"),
            new LocalImageAttachmentStore(destination)).LoadAsync(page.Id));
        Assert.Equal(((NotebookTextBlock)page.Blocks[0]).Styles, ((NotebookTextBlock)restored.Blocks[0]).Styles);
        Assert.Equal(page.Id, Assert.Single(await new SqliteKnowledgeDiscovery(Path.Combine(destination, "trama.db")).SearchAsync("negrito")).PageId);
    }

    [Fact]
    public async Task PreviousBackupSchema_IsUpgradedInStagingAndKeepsLegacyBody()
    {
        using var data = new NotebookTestWorkspace();
        var note = new NoteSnapshot(Guid.NewGuid(), "Backup anterior", "  Corpo preservado 🧵\r\n");
        await using (var old = data.CreateContext())
        {
            await old.GetService<IMigrator>().MigrateAsync("20261029011618_PageTags");
            await new SqliteNoteRepository(old).AddAsync(note);
        }
        var archivePath = Path.Combine(data.DirectoryPath, "previous.trama-backup");
        var bytes = await File.ReadAllBytesAsync(Path.Combine(data.DirectoryPath, "trama.db"));
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            await using (var database = archive.CreateEntry("trama.db").Open()) await database.WriteAsync(bytes);
            await using var manifest = archive.CreateEntry("manifest.json").Open();
            await JsonSerializer.SerializeAsync(manifest, new BackupManifest(1,
                [new BackupFileEntry("trama.db", bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant())]),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        var destination = Path.Combine(data.DirectoryPath, "previous-restored");

        await new LocalBackupService(data.DirectoryPath).RestoreAsync(archivePath, destination);

        var restoredPath = Path.Combine(destination, "trama.db");
        var connection = new SqliteConnectionStringBuilder { DataSource = restoredPath, Pooling = false };
        await using var restoredContext = new TramaDbContext(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options);
        Assert.True((await restoredContext.Database.GetAppliedMigrationsAsync()).Count() > 4, "Upgrade the restored copy before publishing it.");
        Assert.Equal(note, await new SqliteNoteRepository(restoredContext).GetByIdAsync(note.Id));
        await using var untouched = data.CreateContext();
        Assert.Equal(4, (await untouched.Database.GetAppliedMigrationsAsync()).Count());
    }

    [Fact]
    public async Task InvalidStyles_AreRejectedBeforeOverwritingStoredContent()
    {
        using var data = new NotebookTestWorkspace();
        await data.MigrateAsync();
        var page = new NotebookSnapshot(Guid.NewGuid(), "Original", [new NotebookTextBlock(Guid.NewGuid(), "Texto")]);
        await using (var writer = data.CreateContext()) await new SqliteNotebookRepository(writer).SaveAsync(page);
        var invalid = page with
        {
            Title = "Não gravar",
            Blocks = [new NotebookTextBlock(page.Blocks[0].Id, "Texto")
            { Styles = [new TextStyleSpan(1, 100, InlineTextStyle.Bold)] }]
        };

        await using (var writer = data.CreateContext())
            await Assert.ThrowsAnyAsync<ArgumentException>(() => new SqliteNotebookRepository(writer).SaveAsync(invalid));

        await using var reader = data.CreateContext();
        var restored = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(page.Id));
        Assert.Equal(page.Title, restored.Title);
        Assert.Empty(Assert.IsType<NotebookTextBlock>(Assert.Single(restored.Blocks)).Styles);
    }

    private static NotebookSnapshot FormattedPage() => new(Guid.NewGuid(), "Aula formatada",
    [
        new NotebookTextBlock(Guid.NewGuid(), "Texto em negrito 🧵\r\n")
        {
            Styles = [new TextStyleSpan(0, 5, InlineTextStyle.Bold), new TextStyleSpan(9, 7, InlineTextStyle.Italic)]
        },
        new NotebookCodeBlock(Guid.NewGuid(), "    print(42)\t\n", "python")
    ]);
}
