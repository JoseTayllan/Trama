using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NotebookStorageTests
{
    [Fact]
    public async Task Migrations_UpgradeAnExistingDatabaseWithoutChangingItsPlainNotes()
    {
        using var workspace = new NotebookTestWorkspace();
        var original = new NoteSnapshot(Guid.NewGuid(), "  Estudo antigo 🧵  ", "\r\n    Código e texto originais\n");
        await using (var context = workspace.CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync("20261027211013_InitialNotes");
            await new SqliteNoteRepository(context).AddAsync(original);
        }

        await using var upgraded = workspace.CreateContext();
        await upgraded.Database.MigrateAsync();

        Assert.Equal(original, await new SqliteNoteRepository(upgraded).GetByIdAsync(original.Id));
        Assert.True((await upgraded.Database.GetAppliedMigrationsAsync()).Count() >= 2,
            "A new migration must evolve the existing database; do not rewrite InitialNotes.");
    }

    [Theory]
    [InlineData("\r\n  Estudo antigo 🧵\n```python\n    print(1)\n```\t")]
    [InlineData("{\"version\":1,\"blocks\":[\"texto que não deve ser interpretado\"]}")]
    public async Task Read_LegacyNoteBecomesOneTextBlockWithoutReinterpretingItsBody(string body)
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var original = new NoteSnapshot(Guid.NewGuid(), "  Título antigo  ", body);
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNoteRepository(writer).AddAsync(original);
        }

        await using var reader = workspace.CreateContext();
        var notebook = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(original.Id));

        Assert.Equal(original.Id, notebook.Id);
        Assert.Equal(original.Title, notebook.Title);
        Assert.Equal(body, Assert.IsType<NotebookTextBlock>(Assert.Single(notebook.Blocks)).Text);
        Assert.Equal(original, await new SqliteNoteRepository(reader).GetByIdAsync(original.Id));
    }

    [Fact]
    public async Task Save_ReopenRestoresMixedBlocksAndManagedImageBytes()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var store = new LocalImageAttachmentStore(workspace.DirectoryPath);
        using var source = new MemoryStream(NotebookTestWorkspace.Png);
        var attachment = await store.ImportAsync(source);
        NotebookBlock[] blocks =
        [
            new NotebookTextBlock(Guid.NewGuid(), string.Concat(Enumerable.Repeat("  Explicação 🧵\r\n", 5000))),
            new NotebookCodeBlock(Guid.NewGuid(), "def dobro(x):\n\treturn x * 2\n", "python"),
            new NotebookImageBlock(Guid.NewGuid(), attachment.Id, "  Resultado  "),
            new NotebookTextBlock(Guid.NewGuid(), "Conclusão\n")
        ];
        var expected = new NotebookSnapshot(Guid.NewGuid(), "  Caderno de ML  ", blocks);
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNotebookRepository(writer).SaveAsync(expected);
        }

        await using var reader = workspace.CreateContext();
        var actual = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(expected.Id));
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Blocks, actual.Blocks);
        var reopenedStore = new LocalImageAttachmentStore(workspace.DirectoryPath);
        await using var image = await reopenedStore.OpenReadAsync(attachment.Id);
        Assert.NotNull(image);
        using var copy = new MemoryStream();
        await image.CopyToAsync(copy);
        Assert.Equal(NotebookTestWorkspace.Png, copy.ToArray());
    }

    [Fact]
    public async Task Save_ExistingPageReplacesTitleAndBlockOrderWithoutKeepingRemovedBlocks()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var first = new NotebookTextBlock(Guid.NewGuid(), "Introdução");
        var code = new NotebookCodeBlock(Guid.NewGuid(), "print(1)", "python");
        var original = new NotebookSnapshot(Guid.NewGuid(), "Original", [first, code]);
        var edited = original with
        {
            Title = "  Revisado  ",
            Blocks = [code with { Code = "Console.WriteLine(2);\r\n", Language = "csharp" }]
        };
        await using (var writer = workspace.CreateContext())
        {
            var repository = new SqliteNotebookRepository(writer);
            await repository.SaveAsync(original);
            await repository.SaveAsync(edited);
        }

        await using var reader = workspace.CreateContext();
        var actual = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(original.Id));
        Assert.Equal(edited.Title, actual.Title);
        Assert.Equal(edited.Blocks, actual.Blocks);
    }

    [Fact]
    public async Task Read_MissingPageReturnsNull()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        await using var reader = workspace.CreateContext();

        Assert.Null(await new SqliteNotebookRepository(reader).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Read_MissingSchemaIsReportedAsFailureInsteadOfAMissingPage()
    {
        using var workspace = new NotebookTestWorkspace();
        await using var reader = workspace.CreateContext();

        await Assert.ThrowsAsync<NotePersistenceException>(() =>
            new SqliteNotebookRepository(reader).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Save_UpdatingOnePagePreservesAnotherPage()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var first = new NotebookSnapshot(Guid.NewGuid(), "Primeira", [new NotebookTextBlock(Guid.NewGuid(), "Original")]);
        var second = new NotebookSnapshot(Guid.NewGuid(), "Outra página", [new NotebookTextBlock(Guid.NewGuid(), "Manter")]);
        await using (var writer = workspace.CreateContext())
        {
            var repository = new SqliteNotebookRepository(writer);
            await repository.SaveAsync(first);
            await repository.SaveAsync(second);
            await repository.SaveAsync(first with { Title = "Revisada", Blocks = [] });
        }

        await using var reader = workspace.CreateContext();
        var actual = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(second.Id));
        Assert.Equal(second.Title, actual.Title);
        Assert.Equal(second.Blocks, actual.Blocks);
    }

    [Fact]
    public async Task Save_PreCanceledUpdatePreservesThePreviousPage()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var original = new NotebookSnapshot(Guid.NewGuid(), "Original", [new NotebookTextBlock(Guid.NewGuid(), "Manter")]);
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNotebookRepository(writer).SaveAsync(original);
        }

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using (var writer = workspace.CreateContext())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new SqliteNotebookRepository(writer).SaveAsync(original with { Title = "Não salvar", Blocks = [] }, cancellation.Token));
        }

        await using var reader = workspace.CreateContext();
        var actual = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(original.Id));
        Assert.Equal(original.Title, actual.Title);
        Assert.Equal(original.Blocks, actual.Blocks);
    }

    [Fact]
    public async Task Save_ReadOnlyFailureIsReportedWithoutChangingThePage()
    {
        using var workspace = new NotebookTestWorkspace();
        await workspace.MigrateAsync();
        var original = new NotebookSnapshot(Guid.NewGuid(), "Original", [new NotebookTextBlock(Guid.NewGuid(), "Manter")]);
        await using (var writer = workspace.CreateContext())
        {
            await new SqliteNotebookRepository(writer).SaveAsync(original);
        }

        await using (var writer = workspace.CreateContext(readOnly: true))
        {
            await Assert.ThrowsAsync<NotePersistenceException>(() =>
                new SqliteNotebookRepository(writer).SaveAsync(original with { Title = "Não salvar", Blocks = [] }));
        }

        await using var reader = workspace.CreateContext();
        var actual = Assert.IsType<NotebookSnapshot>(await new SqliteNotebookRepository(reader).GetByIdAsync(original.Id));
        Assert.Equal(original.Title, actual.Title);
        Assert.Equal(original.Blocks, actual.Blocks);
    }
}

public sealed class ImageAttachmentStorageTests
{
    [Theory]
    [InlineData(false, "image/png")]
    [InlineData(true, "image/jpeg")]
    public async Task Import_PngAndJpegAreCopiedAndCanBeReadByANewStore(bool jpeg, string mediaType)
    {
        using var workspace = new NotebookTestWorkspace();
        var bytes = jpeg ? NotebookTestWorkspace.Jpeg : NotebookTestWorkspace.Png;
        ImageAttachment attachment;
        using (var source = new MemoryStream(bytes))
        {
            attachment = await new LocalImageAttachmentStore(workspace.DirectoryPath).ImportAsync(source);
            Assert.True(source.CanRead);
        }

        Assert.NotEqual(Guid.Empty, attachment.Id);
        Assert.Equal(mediaType, attachment.MediaType);
        await using var saved = await new LocalImageAttachmentStore(workspace.DirectoryPath).OpenReadAsync(attachment.Id);
        Assert.NotNull(saved);
        using var copy = new MemoryStream();
        await saved.CopyToAsync(copy);
        Assert.Equal(bytes, copy.ToArray());
        Assert.Single(workspace.StoredFiles());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_UnsupportedOrTruncatedHeaderIsRejectedWithoutLeavingFiles(bool truncated)
    {
        using var workspace = new NotebookTestWorkspace();
        var bytes = truncated ? NotebookTestWorkspace.Png[..4] : Encoding.UTF8.GetBytes("not an image");
        using var source = new MemoryStream(bytes);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new LocalImageAttachmentStore(workspace.DirectoryPath).ImportAsync(source));

        Assert.Empty(workspace.StoredFiles());
    }

    [Fact]
    public async Task Import_OverTenMebibytesIsRejectedWithoutLeavingFiles()
    {
        using var workspace = new NotebookTestWorkspace();
        var bytes = new byte[(10 * 1024 * 1024) + 1];
        NotebookTestWorkspace.Png.CopyTo(bytes, 0);
        using var source = new MemoryStream(bytes);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new LocalImageAttachmentStore(workspace.DirectoryPath).ImportAsync(source));

        Assert.Empty(workspace.StoredFiles());
    }

    [Fact]
    public async Task Import_ReadFailureDoesNotPublishAnIncompleteAttachment()
    {
        using var workspace = new NotebookTestWorkspace();
        using var source = new FailingImageStream();

        await Assert.ThrowsAnyAsync<IOException>(() =>
            new LocalImageAttachmentStore(workspace.DirectoryPath).ImportAsync(source));

        Assert.Empty(workspace.StoredFiles());
    }

    [Fact]
    public async Task Import_PreCancellationDoesNotWriteFilesOrDisposeTheSource()
    {
        using var workspace = new NotebookTestWorkspace();
        using var source = new MemoryStream(NotebookTestWorkspace.Png);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new LocalImageAttachmentStore(workspace.DirectoryPath).ImportAsync(source, cancellation.Token));

        Assert.Empty(workspace.StoredFiles());
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task Read_MissingAttachmentReturnsNull()
    {
        using var workspace = new NotebookTestWorkspace();

        Assert.Null(await new LocalImageAttachmentStore(workspace.DirectoryPath).OpenReadAsync(Guid.NewGuid()));
        Assert.Empty(workspace.StoredFiles());
    }

    private sealed class FailingImageStream() : MemoryStream(NotebookTestWorkspace.Png)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= 32)
            {
                throw new IOException("Synthetic read failure.");
            }

            return base.Read(buffer, offset, Math.Min(count, 32));
        }

        public override int Read(Span<byte> buffer)
        {
            if (Position >= 32)
            {
                throw new IOException("Synthetic read failure.");
            }

            return base.Read(buffer[..Math.Min(buffer.Length, 32)]);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Read(buffer.Span));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromResult(Read(buffer, offset, count));

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            throw new IOException("Synthetic copy failure.");
    }
}

internal sealed class NotebookTestWorkspace : IDisposable
{
    public static byte[] Png => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGOQS9kCAAHaATfV0vUwAAAAAElFTkSuQmCC");

    public static byte[] Jpeg => Convert.FromBase64String(
        "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDl6KKK+uPmj//Z");

    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "TramaNotebookTests", Guid.NewGuid().ToString("N"));

    public NotebookTestWorkspace()
    {
        Directory.CreateDirectory(DirectoryPath);
    }

    public TramaDbContext CreateContext(bool readOnly = false)
    {
        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(DirectoryPath, "trama.db"),
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        return new TramaDbContext(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options);
    }

    public async Task MigrateAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public string[] StoredFiles() => Directory.GetFiles(DirectoryPath, "*", SearchOption.AllDirectories);

    public void Dispose()
    {
        Directory.Delete(DirectoryPath, recursive: true);
    }
}
