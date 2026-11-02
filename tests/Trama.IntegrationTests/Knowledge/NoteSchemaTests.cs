using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NoteSchemaTests
{
    [Fact]
    public async Task MigrateAsync_OnNewDatabase_AppliesAProductionMigration()
    {
        using var database = new TemporaryDatabase();
        await using var context = database.CreateContext();

        await context.Database.MigrateAsync();

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task MigrateAsync_CreatesRequiredNoteColumnsAndPrimaryKey()
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();
        await using var connection = database.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Notes');";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new Dictionary<string, (string Type, bool Required, bool PrimaryKey)>();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1), (reader.GetString(2), reader.GetBoolean(3), reader.GetInt32(5) > 0));
        }

        Assert.True(columns.ContainsKey("Id"), "The production migration must create Notes.Id.");
        Assert.Equal(("TEXT", true, true), columns["Id"]);
        Assert.True(columns.ContainsKey("Title"), "The production migration must create Notes.Title.");
        Assert.Equal(("TEXT", true, false), columns["Title"]);
        Assert.True(columns.ContainsKey("Body"), "The production migration must create Notes.Body.");
        Assert.Equal(("TEXT", true, false), columns["Body"]);
    }

    [Theory]
    [InlineData("Estudar C#", "")]
    [InlineData("", "Uma ideia sem título")]
    [InlineData("  Pesquisa café 🧵  ", "\r\n```csharp\r\n    await SaveAsync();\r\n```\n\t 'texto' <tag>")]
    public async Task InsertNote_AfterClosingContext_CanBeReadFromANewConnection(string title, string body)
    {
        using var database = new TemporaryDatabase();
        var id = Guid.NewGuid();
        await database.MigrateAsync();
        await database.InsertAsync(id.ToString(), title, body);

        var saved = await database.ReadAsync(id);

        Assert.Equal(id, saved.Id);
        Assert.Equal(title, saved.Title);
        Assert.Equal(body, saved.Body);
    }

    [Fact]
    public async Task InsertNote_WithDuplicateId_RejectsDuplicateAndPreservesOriginal()
    {
        using var database = new TemporaryDatabase();
        var id = Guid.NewGuid();
        await database.MigrateAsync();
        await database.InsertAsync(id.ToString(), "Original", "Keep this content.");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => database.InsertAsync(id.ToString(), "Replacement", "Do not overwrite."));

        Assert.Equal(19, exception.SqliteErrorCode);
        Assert.Equal(1555, exception.SqliteExtendedErrorCode);
        var saved = await database.ReadAsync(id);
        Assert.Equal("Original", saved.Title);
        Assert.Equal("Keep this content.", saved.Body);
    }

    [Theory]
    [InlineData(null, "Title", "Body")]
    [InlineData("11111111-1111-1111-1111-111111111111", null, "Body")]
    [InlineData("11111111-1111-1111-1111-111111111111", "Title", null)]
    public async Task InsertNote_WithNullRequiredColumn_RejectsNull(string? id, string? title, string? body)
    {
        using var database = new TemporaryDatabase();
        await database.MigrateAsync();

        var exception = await Assert.ThrowsAsync<SqliteException>(() => database.InsertAsync(id, title, body));

        Assert.Equal(19, exception.SqliteErrorCode);
        Assert.Equal(1299, exception.SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task MigrateAsync_WhenRepeated_PreservesExistingNotes()
    {
        using var database = new TemporaryDatabase();
        var id = Guid.NewGuid();
        await database.MigrateAsync();
        await database.InsertAsync(id.ToString(), "Preserved", "Existing data.");

        await database.MigrateAsync();

        var saved = await database.ReadAsync(id);
        Assert.Equal("Preserved", saved.Title);
        Assert.Equal("Existing data.", saved.Body);
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("trama-notes-schema-");

        private string DatabasePath => Path.Combine(_directory.FullName, "notes.db");

        private string ConnectionString => new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Pooling = false
        }.ToString();

        public TramaDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(ConnectionString).Options);

        public SqliteConnection CreateConnection() => new(ConnectionString);

        public async Task MigrateAsync()
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task InsertAsync(string? id, string? title, string? body)
        {
            await using var context = CreateContext();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Notes (Id, Title, Body) VALUES ({id}, {title}, {body});");
        }

        public async Task<(Guid Id, string Title, string Body)> ReadAsync(Guid id)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Title, Body FROM Notes WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id.ToString());
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "The note must remain available in a new connection.");
            return (Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2));
        }

        public void Dispose()
        {
            // Delete only files explicitly owned by this test; never recurse through a configured path.
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            {
                File.Delete(DatabasePath + suffix);
            }

            _directory.Delete();
        }
    }
}
