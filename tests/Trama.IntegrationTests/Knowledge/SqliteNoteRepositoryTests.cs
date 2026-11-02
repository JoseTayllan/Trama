using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class SqliteNoteRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsTheNoteForAnIndependentSqlReader()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        var expected = new NoteSnapshot(Guid.NewGuid(), "  Café 🧵  ", "\r\n    await SaveAsync();\n");
        await using (var context = database.CreateContext())
        {
            await new SqliteNoteRepository(context).AddAsync(expected);
        }

        Assert.Equal(expected, await database.ReadFromSqlAsync(expected.Id));
        Assert.Equal(1L, await database.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsANoteInsertedIndependentlyBySql()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        var expected = new NoteSnapshot(Guid.NewGuid(), "  Unicode ação 🧵  ", "\n    return value;\r\n");
        await using (var seedContext = database.CreateContext())
        {
            await seedContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Notes (Id, Title, Body) VALUES ({expected.Id}, {expected.Title}, {expected.Body});");
        }

        await using var readContext = database.CreateContext();
        Assert.Equal(expected, await new SqliteNoteRepository(readContext).GetByIdAsync(expected.Id));
    }

    [Theory]
    [InlineData("Só título", "")]
    [InlineData("", "Só conteúdo")]
    [InlineData("  Café 🧵  ", "\r\n```csharp\n    await SaveAsync();\n```\t 'texto'")]
    public async Task AddAsync_AfterReopeningContext_ReaderReturnsTheExactNote(string title, string body)
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        var expected = new NoteSnapshot(Guid.NewGuid(), title, body);
        await using (var writerContext = database.CreateContext())
        {
            INoteRepository writer = new SqliteNoteRepository(writerContext);
            await writer.AddAsync(expected);
        }

        await using var readerContext = database.CreateContext();
        INoteReader reader = new SqliteNoteRepository(readerContext);

        Assert.Equal(expected, await reader.GetByIdAsync(expected.Id));
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoteDoesNotExist_ReturnsNull()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        await using var context = database.CreateContext();
        INoteReader reader = new SqliteNoteRepository(context);

        Assert.Null(await reader.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ReportsPersistenceFailureAndPreservesOriginal()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        var original = new NoteSnapshot(Guid.NewGuid(), "Original", "Keep this content.");
        await using (var firstContext = database.CreateContext())
        {
            await new SqliteNoteRepository(firstContext).AddAsync(original);
        }

        await using (var duplicateContext = database.CreateContext())
        {
            var repository = new SqliteNoteRepository(duplicateContext);
            var duplicate = original with { Body = "Must not replace the original." };
            await Assert.ThrowsAsync<NotePersistenceException>(() => repository.AddAsync(duplicate));
        }

        await using var readContext = database.CreateContext();
        Assert.Equal(original, await new SqliteNoteRepository(readContext).GetByIdAsync(original.Id));
        Assert.Equal(1L, await database.CountAsync());
    }

    [Fact]
    public async Task CreateNote_WithReadOnlyDatabase_ReturnsPersistenceFailureWithoutSaving()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        await using var context = database.CreateContext(SqliteOpenMode.ReadOnly);
        var createNote = new CreateNote(new SqliteNoteRepository(context));

        var result = await createNote.ExecuteAsync(new CreateNoteRequest("Read-only test", "Must not be saved."));

        Assert.Equal(CreateNoteStatus.PersistenceFailed, result.Status);
        Assert.Null(result.Note);
        Assert.Equal(0L, await database.CountAsync());
    }

    [Fact]
    public async Task AddAsync_WithReadOnlyDatabase_PreservesTheCauseWithoutExposingNoteContent()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        await using var context = database.CreateContext(SqliteOpenMode.ReadOnly);
        var repository = new SqliteNoteRepository(context);
        var note = new NoteSnapshot(Guid.NewGuid(), "synthetic-private-title", "synthetic-private-body");

        var exception = await Assert.ThrowsAsync<NotePersistenceException>(() => repository.AddAsync(note));

        Assert.NotNull(exception.InnerException);
        Assert.DoesNotContain(note.Title, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(note.Body, exception.Message, StringComparison.Ordinal);
        Assert.Equal(0L, await database.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WhenStorageIsUnavailable_ThrowsPersistenceFailureInsteadOfReturningNull()
    {
        using var database = new TemporaryDatabase();
        // This isolated database has no schema: a query error must not look like a missing note.
        await using var context = database.CreateContext();
        INoteReader reader = new SqliteNoteRepository(context);

        await Assert.ThrowsAsync<NotePersistenceException>(() => reader.GetByIdAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Repository_WithPreCanceledToken_PropagatesCancellationWithoutSaving(bool write)
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        await using var context = database.CreateContext();
        var repository = new SqliteNoteRepository(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var note = new NoteSnapshot(Guid.NewGuid(), "Canceled", "No data should be saved.");

        if (write)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.AddAsync(note, cancellation.Token));
        }
        else
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.GetByIdAsync(note.Id, cancellation.Token));
        }

        Assert.Equal(0L, await database.CountAsync());
    }

    [Fact]
    public async Task CreateNote_WithSqliteRepository_CanBeRecoveredThroughANewReader()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        CreateNoteResult result;
        await using (var context = database.CreateContext())
        {
            var createNote = new CreateNote(new SqliteNoteRepository(context));
            result = await createNote.ExecuteAsync(new CreateNoteRequest("Study", "Keep my notes."));
        }

        Assert.Equal(CreateNoteStatus.Created, result.Status);
        var created = Assert.IsType<NoteSnapshot>(result.Note);
        Assert.NotEqual(Guid.Empty, created.Id);
        await using var readContext = database.CreateContext();
        Assert.Equal(created, await new SqliteNoteRepository(readContext).GetByIdAsync(created.Id));
        Assert.Equal(1L, await database.CountAsync());
    }

    [Fact]
    public async Task CreateNote_WithInvalidInput_DoesNotWriteToTheDatabase()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        await using var context = database.CreateContext();
        var createNote = new CreateNote(new SqliteNoteRepository(context));

        var result = await createNote.ExecuteAsync(new CreateNoteRequest(" \t", "\r\n"));

        Assert.Equal(CreateNoteStatus.ValidationFailed, result.Status);
        Assert.Null(result.Note);
        Assert.Equal(0L, await database.CountAsync());
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("trama-note-repository-");

        private string DatabasePath => Path.Combine(_directory.FullName, "notes.db");

        public TramaDbContext CreateContext(SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = mode,
                Pooling = false
            }.ToString();
            return new TramaDbContext(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connectionString).Options);
        }

        public async Task MigrateAsync()
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task<long> CountAsync()
        {
            await using var context = CreateContext();
            await context.Database.OpenConnectionAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Notes;";
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        public async Task<NoteSnapshot> ReadFromSqlAsync(Guid id)
        {
            await using var context = CreateContext();
            await context.Database.OpenConnectionAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT Id, Title, Body FROM Notes WHERE Id = $id;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$id";
            parameter.Value = id;
            command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "The repository must persist the note in SQLite.");
            return new NoteSnapshot(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2));
        }

        public void Dispose()
        {
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            {
                File.Delete(DatabasePath + suffix);
            }

            _directory.Delete();
        }
    }
}
