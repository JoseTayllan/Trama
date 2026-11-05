using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Agenda;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaEndDateStorageTests
{
    private const string PreviousMigration = "20261105220851_AgendaTaskTrash";
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly DateTimeOffset Actual = new(2026, 11, 7, 3, 0, 0, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task Reopening_PreservesExplicitEndDateAndOptionalHours(int days, bool dateOnly)
    {
        using var f = await Fixture.CreateAsync();
        var task = Example() with { PlannedEndDate = Day.AddDays(days), StartTime = dateOnly ? null : new TimeOnly(22, 0), EndTime = dateOnly ? null : new TimeOnly(days == 0 ? 23 : 2, 0) };
        await f.SaveAsync(task); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task Editing_ExtendsTheDateWithoutChangingIdentity()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        var edited = task with { PlannedEndDate = Day.AddDays(3), Description = "Novo planejamento" };
        await f.SaveAsync(edited); Assert.Equal(edited, await f.Actions.GetByIdAsync(task.Id));
        Assert.Single(await f.Actions.ListAsync(Day, Day));
    }

    [Fact]
    public async Task Editing_CanClearAnExistingEndDate()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
        var cleared = task with { PlannedEndDate = null }; await f.SaveAsync(cleared);
        Assert.Equal(cleared, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task PeriodQuery_PreservesEndDatesAndKeepsItsStartDayFilter()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(2) }; await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await f.Actions.ListAsync(Day, Day)));
        Assert.Empty(await f.Actions.ListAsync(Day.AddDays(1), Day.AddDays(2)));
    }

    [Fact]
    public async Task Completion_PreservesPlanningAndRecordsTheActualTimeSeparately()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task);
        await f.CompleteAsync(task.Id);
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await f.Actions.GetByIdAsync(task.Id));
        Assert.Equal(Actual, await f.Completion.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task ReopeningACompletedTask_PreservesItsPlannedEndAndLastActualTime()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task); await f.CompleteAsync(task.Id);
        await f.SaveAsync(task with { Status = AgendaTaskStatus.InProgress });
        Assert.Equal(task with { Status = AgendaTaskStatus.InProgress }, await f.Actions.GetByIdAsync(task.Id));
        Assert.Equal(Actual, await f.Completion.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task TrashAndRestoration_PreserveTheEndDateAndCompletionMetadata()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task); await f.CompleteAsync(task.Id);
        await f.Trash.MoveToTrashAsync(task.Id, Actual.AddHours(1));
        var entry = Assert.Single(await f.Trash.ListAsync()); Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, entry.Task); Assert.Equal(Actual, entry.LastCompletedAt);
        await f.Trash.RestoreAsync(task.Id); Assert.Equal(entry.Task, await f.Actions.GetByIdAsync(task.Id));
        Assert.Equal(Actual, await f.Completion.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task BackupVersionOne_PreservesActiveAndTrashedEndDates()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) };
        var hidden = task with { Id = Guid.NewGuid(), PlannedEndDate = Day.AddDays(2) }; await f.SaveAsync(task); await f.SaveAsync(hidden);
        await f.CompleteAsync(hidden.Id); await f.Trash.MoveToTrashAsync(hidden.Id, Actual.AddHours(1));
        var backup = new LocalBackupService(f.Root.DirectoryPath); await backup.CreateAsync(f.Archive);
        using (var zip = ZipFile.OpenRead(f.Archive))
        {
            await using var stream = zip.GetEntry("manifest.json")!.Open();
            Assert.Equal(1, (await JsonSerializer.DeserializeAsync<BackupManifest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web)))!.FormatVersion);
        }
        await backup.RestoreAsync(f.Archive, f.Restored); var database = Path.Combine(f.Restored, "trama.db");
        Assert.Equal(task, await new SqliteAgendaActions(database).GetByIdAsync(task.Id));
        var entry = Assert.Single(await new SqliteAgendaTrashActions(database).ListAsync());
        Assert.Equal(hidden with { Status = AgendaTaskStatus.Completed }, entry.Task); Assert.Equal(Actual, entry.LastCompletedAt);
    }

    [Fact]
    public async Task LegacyBackup_MigratesTheCopyWithoutChangingTheSourceOrInventingAnEndDate()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = Example(); await f.SeedLegacyAsync(task);
        var bytes = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, bytes);
        await new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(f.Database));
        var database = Path.Combine(f.Restored, "trama.db"); Assert.Equal(task, await new SqliteAgendaActions(database).GetByIdAsync(task.Id));
        await using var context = f.ContextAt(f.Restored); await RequireColumnAsync(context);
        Assert.True((await context.Database.GetAppliedMigrationsAsync()).Count() >= 10);
    }

    [Fact]
    public async Task TenthMigration_PreservesNinePreviousMigrationsNotesAndTrashMetadata()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = Example() with { Status = AgendaTaskStatus.Completed };
        await f.SeedLegacyAsync(task, Actual, Actual.AddHours(1)); var note = new NoteSnapshot(Guid.NewGuid(), "Nota fictícia", "Conteúdo anterior");
        await using (var context = f.Root.CreateContext())
        {
            await new SqliteNoteRepository(context).AddAsync(note);
            var before = (await context.Database.GetAppliedMigrationsAsync()).ToArray(); Assert.Equal(9, before.Length);
            await context.Database.MigrateAsync(); var after = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.True(after.Length >= 10); Assert.Equal(before, after.Take(9)); await RequireColumnAsync(context);
            Assert.Equal(note, await new SqliteNoteRepository(context).GetByIdAsync(note.Id));
        }
        var entry = Assert.Single(await f.Trash.ListAsync()); Assert.Equal(task, entry.Task); Assert.Null(entry.Task.PlannedEndDate);
        Assert.Equal(Actual, entry.LastCompletedAt); Assert.Equal(Actual.AddHours(1), entry.DeletedAt);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("reversed")]
    public async Task Reading_RejectsMalformedOrReversedStoredEndDates(string fault)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CorruptAsync(fault);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Actions.GetByIdAsync(task.Id));
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Actions.ListAsync(Day, Day));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("reversed")]
    public async Task Backup_RejectsInvalidEndDatesEvenInTheTrash(string fault)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, Actual);
        await f.CorruptAsync(fault);
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).CreateAsync(f.Archive)); Assert.False(File.Exists(f.Archive));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("reversed")]
    public async Task Restore_RejectsInvalidDatesDespiteValidArchiveHashes(string fault)
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await f.CorruptAsync(fault);
        var bytes = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, bytes);
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored));
        Assert.False(Directory.Exists(f.Restored)); Assert.Equal(bytes, await File.ReadAllBytesAsync(f.Database));
    }

    [Fact]
    public async Task SavingAnInvalidDate_DoesNotOverwriteTheExistingTask()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, (await f.Actions.SaveAsync(task with { PlannedEndDate = Day.AddDays(-1) })).Status);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task Cancellation_DoesNotChangeThePersistedEndDate()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Actions.SaveAsync(task with { PlannedEndDate = Day.AddDays(2) }, cancel.Token));
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task FailedDateUpdate_PreservesThePreviousTask()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task);
        await using (var context = f.Root.CreateContext())
        {
            await RequireColumnAsync(context);
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailPlannedEnd BEFORE UPDATE OF PlannedEndDate ON AgendaTasks BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        }
        Assert.Equal(AgendaTaskSaveStatus.PersistenceFailed, (await f.Actions.SaveAsync(task with { PlannedEndDate = Day.AddDays(2), Description = "Tentativa" })).Status);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task ProfilesWithTheSameIdentity_KeepDifferentEndDates()
    {
        using var first = await Fixture.CreateAsync(); using var second = await Fixture.CreateAsync();
        var task = Example() with { PlannedEndDate = Day.AddDays(1) }; var other = task with { PlannedEndDate = Day.AddDays(3) };
        await first.SaveAsync(task); await second.SaveAsync(other);
        Assert.Equal(task, await first.Actions.GetByIdAsync(task.Id)); Assert.Equal(other, await second.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task MissingEndDate_RetainsLegacySameDaySemantics()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, (await f.Actions.SaveAsync(task with { StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) })).Status);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task Completion_RevalidatesAnInvalidStoredEndDateBeforeWritingTheBatch()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var other = Example(); await f.SaveAsync(first); await f.SaveAsync(other);
        await f.CorruptAsync("reversed", other.Id);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.CompleteAsync(first.Id, other.Id));
        Assert.Equal(first, await f.Actions.GetByIdAsync(first.Id)); Assert.Null(await f.Completion.GetLastCompletedAtAsync(first.Id));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Planejamento preservado", Day, new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Work);

    private static async Task RequireColumnAsync(TramaDbContext context)
    {
        await context.Database.OpenConnectionAsync(); await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA table_info('AgendaTasks')"; await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<string>(); while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
        Assert.Contains("PlannedEndDate", columns);
    }

    private static async Task WriteArchiveAsync(string path, byte[] database)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        await using (var stream = archive.CreateEntry("trama.db").Open()) await stream.WriteAsync(database);
        var hash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant(); await using var metadata = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(metadata, new BackupManifest(1, [new BackupFileEntry("trama.db", database.LongLength, hash)]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public string Archive => Path.Combine(Root.DirectoryPath, "dates.trama-backup");
        public string Restored => Path.Combine(Root.DirectoryPath, "restored");
        public SqliteAgendaActions Actions => new(Database);
        public SqliteAgendaCompletionActions Completion => new(Database);
        public SqliteAgendaTrashActions Trash => new(Database);
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture(); await using var context = f.Root.CreateContext();
            if (legacy) await context.GetService<IMigrator>().MigrateAsync(PreviousMigration); else await context.Database.MigrateAsync();
            return f;
        }
        public async Task SaveAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        public async Task CompleteAsync(params Guid[] ids)
        {
            await using var context = Root.CreateContext(); await new SqliteAgendaTaskCompletionRepository(context).CompleteAsync(ids, Actual);
        }
        public async Task SeedLegacyAsync(AgendaTaskSnapshot task, DateTimeOffset? actual = null, DateTimeOffset? deleted = null)
        {
            await using var context = Root.CreateContext();
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgendaTasks (Id, Title, Description, PlannedDate, StartTime, EndTime, Category, Status, LastCompletedAt, DeletedAt) VALUES ({task.Id}, {task.Title}, {task.Description}, {task.PlannedDate}, {task.StartTime}, {task.EndTime}, {(int)task.Category}, {(int)task.Status}, {actual}, {deleted})");
        }
        public async Task CorruptAsync(string fault, Guid? id = null)
        {
            await using var context = Root.CreateContext(); await RequireColumnAsync(context);
            var value = fault == "malformed" ? "not-a-date" : "2026-11-04";
            if (id is { } selected) await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE AgendaTasks SET PlannedEndDate = {value} WHERE Id = {selected}");
            else await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE AgendaTasks SET PlannedEndDate = {value}");
        }
        public TramaDbContext ContextAt(string directory) => new(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite($"Data Source={Path.Combine(directory, "trama.db")};Pooling=False").Options);
        public void Dispose() => Root.Dispose();
    }
}
