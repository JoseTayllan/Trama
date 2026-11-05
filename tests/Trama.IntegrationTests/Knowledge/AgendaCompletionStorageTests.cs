using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Agenda;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaCompletionStorageTests
{
    private const string PreviousMigration = "20261103015956_AgendaTasks";
    private static readonly DateOnly Day = new(2026, 11, 3);
    private static readonly DateTimeOffset ClosedAt = new(2026, 11, 4, 0, 30, 0, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    public async Task Completion_ReopensWithActualTimestampAndUnchangedPlanning(AgendaTaskStatus status)
    {
        using var f = await Fixture.CreateAsync();
        var task = Example() with { Status = status }; await f.SaveAsync(task);
        await using (var context = f.Root.CreateContext())
        {
            await new SqliteAgendaTaskCompletionRepository(context).CompleteAsync([task.Id], ClosedAt);
            Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
        }
        await using var reader = f.Root.CreateContext();
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await new SqliteAgendaTaskRepository(reader).GetByIdAsync(task.Id));
        var actual = await new SqliteAgendaTaskCompletionRepository(reader).GetLastCompletedAtAsync(task.Id);
        Assert.Equal(ClosedAt, actual); Assert.Equal(ClosedAt.Offset, actual!.Value.Offset);
    }

    [Fact]
    public async Task Batch_OnlyCompletesTheSelectedIdentities()
    {
        using var f = await Fixture.CreateAsync();
        var first = Example(); var second = Example(); var other = Example();
        foreach (var task in new[] { first, second, other }) await f.SaveAsync(task);
        await f.CompleteAsync(first.Id, second.Id);
        Assert.Equal(first with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(first.Id));
        Assert.Equal(second with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(second.Id));
        Assert.Equal(other, await f.ReadAsync(other.Id));
        Assert.Null(await f.ReadActualAsync(other.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyOrDuplicateSelection_IsRejectedWithoutWriting(bool duplicate)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await using var context = f.Root.CreateContext();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new SqliteAgendaTaskCompletionRepository(context).CompleteAsync(duplicate ? [task.Id, task.Id] : [], ClosedAt));
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.ReadActualAsync(task.Id));
    }

    [Fact]
    public async Task MissingTask_PreventsTheWholeBatchFromBeingWritten()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.CompleteAsync(task.Id, Guid.NewGuid()));
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.ReadActualAsync(task.Id));
    }

    [Fact]
    public async Task TaskAlreadyCompletedAfterSelection_IsRevalidatedBeforeWriting()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var changed = Example();
        await f.SaveAsync(first); await f.SaveAsync(changed);
        await f.CompleteAsync(changed.Id);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.CompleteAsync(first.Id, changed.Id));
        Assert.Equal(first, await f.ReadAsync(first.Id)); Assert.Null(await f.ReadActualAsync(first.Id));
        Assert.Equal(ClosedAt, await f.ReadActualAsync(changed.Id));
    }

    [Fact]
    public async Task TaskRescheduledIntoTheFuture_IsRevalidatedBeforeWriting()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var changed = Example();
        await f.SaveAsync(first); await f.SaveAsync(changed);
        changed = changed with { PlannedDate = new DateOnly(2026, 11, 5) }; await f.SaveAsync(changed);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.CompleteAsync(first.Id, changed.Id));
        Assert.Equal(first, await f.ReadAsync(first.Id)); Assert.Equal(changed, await f.ReadAsync(changed.Id));
        Assert.Null(await f.ReadActualAsync(first.Id)); Assert.Null(await f.ReadActualAsync(changed.Id));
    }

    [Fact]
    public async Task DatabaseFailure_RollsBackStatusAndActualTimeForTheWholeBatch()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var second = Example();
        await f.SaveAsync(first); await f.SaveAsync(second);
        await using (var context = f.Root.CreateContext())
        {
            await context.Database.OpenConnectionAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand();
            // SQLite trigger definitions cannot bind parameters; this literal comes only from a generated Guid.
            command.CommandText = $"CREATE TRIGGER SyntheticCompletionFailure BEFORE UPDATE OF Status ON AgendaTasks WHEN upper(NEW.Id) = '{second.Id.ToString().ToUpperInvariant()}' BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;";
            await command.ExecuteNonQueryAsync();
        }
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.CompleteAsync(first.Id, second.Id));
        Assert.Equal(first, await f.ReadAsync(first.Id)); Assert.Equal(second, await f.ReadAsync(second.Id));
        Assert.Null(await f.ReadActualAsync(first.Id)); Assert.Null(await f.ReadActualAsync(second.Id));
    }

    [Fact]
    public async Task PreCancellation_DoesNotWrite()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await using var context = f.Root.CreateContext();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SqliteAgendaTaskCompletionRepository(context).CompleteAsync([task.Id], ClosedAt, cancellation.Token));
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.ReadActualAsync(task.Id));
    }

    [Fact]
    public void MissingContext_IsRejected() => Assert.Throws<ArgumentNullException>(() => new SqliteAgendaTaskCompletionRepository(null!));

    [Fact]
    public async Task MissingTask_HasNoInventedCompletionTimestamp()
    {
        using var f = await Fixture.CreateAsync(); Assert.Null(await f.ReadActualAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task LegacyCompletedTask_HasNoInventedActualTimestamp()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { Status = AgendaTaskStatus.Completed }; await f.SaveAsync(task);
        Assert.Null(await f.ReadActualAsync(task.Id)); Assert.Equal(task, await f.ReadAsync(task.Id));
    }

    [Fact]
    public async Task Migration_PreservesTheSevenPreviousMigrationsTasksAndNotebookData()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = Example(); var note = new NoteSnapshot(Guid.NewGuid(), "Nota fictícia", "Conteúdo anterior");
        await f.SaveAsync(task);
        await using (var context = f.Root.CreateContext())
        {
            await new SqliteNoteRepository(context).AddAsync(note);
            var before = (await context.Database.GetAppliedMigrationsAsync()).ToArray(); Assert.Equal(7, before.Length);
            await context.Database.MigrateAsync();
            var after = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.True(after.Length >= 8); Assert.Equal(before, after.Take(7));
            Assert.Equal(note, await new SqliteNoteRepository(context).GetByIdAsync(note.Id));
        }
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.ReadActualAsync(task.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditingOrReopening_PreservesTheLastActualCompletion(bool reopen)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CompleteAsync(task.Id);
        var edited = task with { Description = "Revisada", Status = reopen ? AgendaTaskStatus.Pending : AgendaTaskStatus.Completed };
        await f.SaveAsync(edited);
        Assert.Equal(edited, await f.ReadAsync(task.Id)); Assert.Equal(ClosedAt, await f.ReadActualAsync(task.Id));
    }

    [Fact]
    public async Task CompletingAgainAfterReopening_UpdatesTheLastCompletionWithoutChangingPlanning()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CompleteAsync(task.Id);
        await f.SaveAsync(task);
        var next = ClosedAt.AddDays(1);
        await using (var context = f.Root.CreateContext()) await new SqliteAgendaTaskCompletionRepository(context).CompleteAsync([task.Id], next);
        Assert.Equal(next, await f.ReadActualAsync(task.Id));
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(task.Id));
    }

    [Fact]
    public async Task BackupAndRestore_PreserveActualTimeOffsetAndPlanningWithPackageVersionOne()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CompleteAsync(task.Id);
        var backup = new LocalBackupService(f.Root.DirectoryPath); await backup.CreateAsync(f.Archive);
        using (var archive = ZipFile.OpenRead(f.Archive))
        {
            await using var metadata = archive.GetEntry("manifest.json")!.Open();
            var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.Equal(1, manifest!.FormatVersion);
        }
        await backup.RestoreAsync(f.Archive, f.Restored);
        await using var context = f.ContextAt(f.Restored);
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await new SqliteAgendaTaskRepository(context).GetByIdAsync(task.Id));
        var actual = await new SqliteAgendaTaskCompletionRepository(context).GetLastCompletedAtAsync(task.Id);
        Assert.Equal(ClosedAt, actual); Assert.Equal(ClosedAt.Offset, actual!.Value.Offset);
    }

    [Fact]
    public async Task PreviousBackup_RestoresWithNullActualTimeAndWithoutChangingTheSource()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = Example(); await f.SaveAsync(task);
        var source = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, source);
        await new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored);
        Assert.Equal(source, await File.ReadAllBytesAsync(f.Database));
        await using var context = f.ContextAt(f.Restored);
        Assert.Equal(task, await new SqliteAgendaTaskRepository(context).GetByIdAsync(task.Id));
        Assert.Null(await new SqliteAgendaTaskCompletionRepository(context).GetLastCompletedAtAsync(task.Id));
        Assert.True((await context.Database.GetAppliedMigrationsAsync()).Count() >= 8);
    }

    [Fact]
    public async Task MalformedActualTime_IsRejectedWhenReading()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CompleteAsync(task.Id); await f.CorruptActualAsync();
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.ReadActualAsync(task.Id));
    }

    [Fact]
    public async Task BackupCreation_RejectsMalformedActualTimeEvenOutsideTheSelectedDay()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedDate = new DateOnly(2000, 1, 1) };
        await f.SaveAsync(task); await f.CompleteAsync(task.Id); await f.CorruptActualAsync();
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).CreateAsync(f.Archive));
        Assert.False(File.Exists(f.Archive));
    }

    [Fact]
    public async Task Restore_RejectsMalformedActualTimeDespiteValidHashes()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CompleteAsync(task.Id); await f.CorruptActualAsync();
        var source = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, source);
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored));
        Assert.False(Directory.Exists(f.Restored)); Assert.Equal(source, await File.ReadAllBytesAsync(f.Database));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Planejamento preservado", Day,
        new TimeOnly(23, 50), null, AgendaTaskCategory.Work, AgendaTaskStatus.Pending);

    private static async Task WriteArchiveAsync(string path, byte[] database)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        await using (var entry = archive.CreateEntry("trama.db").Open()) await entry.WriteAsync(database);
        var hash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant();
        await using var metadata = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(metadata, new BackupManifest(1, [new BackupFileEntry("trama.db", database.LongLength, hash)]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed class Fixture : IDisposable
    {
        private bool _legacy;
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public string Archive => Path.Combine(Root.DirectoryPath, "completion.trama-backup");
        public string Restored => Path.Combine(Root.DirectoryPath, "restored");
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture { _legacy = legacy }; await using var context = f.Root.CreateContext();
            if (legacy) await context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            else await context.Database.MigrateAsync();
            return f;
        }
        public async Task SaveAsync(AgendaTaskSnapshot task)
        {
            await using var context = Root.CreateContext();
            if (_legacy)
            {
                // Seed the old schema without using a future EF model that includes the new column.
                await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgendaTasks (Id, Title, Description, PlannedDate, StartTime, EndTime, Category, Status) VALUES ({task.Id}, {task.Title}, {task.Description}, {task.PlannedDate}, {task.StartTime}, {task.EndTime}, {(int)task.Category}, {(int)task.Status})");
            }
            else await new SqliteAgendaTaskRepository(context).SaveAsync(task);
        }
        public async Task<AgendaTaskSnapshot?> ReadAsync(Guid id)
        {
            await using var context = Root.CreateContext(); return await new SqliteAgendaTaskRepository(context).GetByIdAsync(id);
        }
        public async Task<DateTimeOffset?> ReadActualAsync(Guid id)
        {
            await using var context = Root.CreateContext(); return await new SqliteAgendaTaskCompletionRepository(context).GetLastCompletedAtAsync(id);
        }
        public async Task CompleteAsync(params Guid[] ids)
        {
            await using var context = Root.CreateContext(); await new SqliteAgendaTaskCompletionRepository(context).CompleteAsync(ids, ClosedAt);
        }
        public async Task CorruptActualAsync()
        {
            await using var context = Root.CreateContext(); await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET LastCompletedAt = 'not-a-timestamp'");
        }
        public TramaDbContext ContextAt(string path) => new(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(path, "trama.db"), Pooling = false }.ToString()).Options);
        public void Dispose() => Root.Dispose();
    }
}
