using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LocalDatabaseTests
{
    [Fact]
    public void DefaultDirectory_UsesTheCurrentUsersLocalApplicationData()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Trama");

        Assert.Equal(expected, LocalDatabase.DefaultDirectory);
        Assert.True(Path.IsPathFullyQualified(LocalDatabase.DefaultDirectory));
    }

    [Fact]
    public async Task InitializeAsync_CreatesDirectoryAndAppliesProductionMigrations()
    {
        using var storage = new TemporaryStorage();

        var path = await LocalDatabase.InitializeAsync(storage.DataDirectory);

        Assert.Equal(Path.Combine(storage.DataDirectory, "trama.db"), path);
        Assert.True(File.Exists(path));
        await using var context = CreateContext(path);
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task InitializeAsync_WhenRepeated_PreservesNotesInANewContext()
    {
        using var storage = new TemporaryStorage();
        var path = await LocalDatabase.InitializeAsync(storage.DataDirectory);
        NoteSnapshot note;
        await using (var context = CreateContext(path))
        {
            var result = await new CreateNote(new SqliteNoteRepository(context))
                .ExecuteAsync(new CreateNoteRequest("Study", "Preserve after initialization."));
            Assert.Equal(CreateNoteStatus.Created, result.Status);
            note = Assert.IsType<NoteSnapshot>(result.Note);
        }

        Assert.Equal(path, await LocalDatabase.InitializeAsync(storage.DataDirectory));

        await using var readerContext = CreateContext(path);
        Assert.Equal(note, await new SqliteNoteRepository(readerContext).GetByIdAsync(note.Id));
    }

    [Theory]
    [InlineData("data")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task InitializeAsync_WithNonAbsoluteDirectory_RejectsThePath(string directory)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => LocalDatabase.InitializeAsync(directory));
    }

    [Fact]
    public async Task InitializeAsync_WithPreCanceledToken_DoesNotCreateDirectory()
    {
        using var storage = new TemporaryStorage();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => LocalDatabase.InitializeAsync(storage.DataDirectory, cancellation.Token));

        Assert.False(Directory.Exists(storage.DataDirectory));
    }

    [Fact]
    public async Task InitializeAsync_WhenAFileBlocksTheDirectory_ReportsAnIoFailure()
    {
        using var storage = new TemporaryStorage();
        File.WriteAllText(storage.BlockingFile, "synthetic test data");

        await Assert.ThrowsAsync<IOException>(() => LocalDatabase.InitializeAsync(Path.Combine(storage.BlockingFile, "data")));

        Assert.Equal("synthetic test data", File.ReadAllText(storage.BlockingFile));
    }

    private static TramaDbContext CreateContext(string path)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        return new TramaDbContext(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connectionString).Options);
    }

    private sealed class TemporaryStorage : IDisposable
    {
        private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("trama-local-storage-");

        public string DataDirectory => Path.Combine(_root.FullName, "data");
        public string BlockingFile => Path.Combine(_root.FullName, "block");

        public void Dispose()
        {
            if (Directory.Exists(DataDirectory))
            {
                foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
                {
                    File.Delete(Path.Combine(DataDirectory, "trama.db") + suffix);
                }

                Directory.Delete(DataDirectory);
            }

            File.Delete(BlockingFile);
            _root.Delete();
        }
    }
}
