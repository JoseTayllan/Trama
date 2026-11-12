using System.Data.Common;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Agenda;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveStorageTests
{
    private const string PreviousMigration = "20261105224911_AgendaTaskPlannedEndDate";
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("TramaRetrospectiveStorageTests", TimeSpan.FromHours(-3), "Test", "Test");
    private static readonly DateTimeOffset ClosedAt = new(2026, 11, 9, 12, 0, 0, TimeSpan.FromHours(-3));
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SavedCapture_ReopensInAnIndependentContextWithoutLosingFields()
    {
        using var f = await Fixture.CreateAsync(); var report = Report(); Assert.True(await f.AddAsync(report));
        await using var context = f.Root.CreateContext(); EqualReport(report, await Store(context).GetAsync(Monday));
    }

    [Fact]
    public async Task RepeatedWeek_KeepsTheFirstCaptureInsteadOfOverwritingIt()
    {
        using var f = await Fixture.CreateAsync(); var original = Report(); Assert.True(await f.AddAsync(original));
        var replacement = Report(title: "Outra captura", capturedAt: ClosedAt.AddHours(1)); Assert.False(await f.AddAsync(replacement));
        EqualReport(original, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task SameWeekInAnotherTimeZone_StillCannotReplaceTheOriginalCapture()
    {
        using var f = await Fixture.CreateAsync(); var original = Report(); await f.AddAsync(original);
        var other = TimeZoneInfo.CreateCustomTimeZone("AnotherReportZone", TimeSpan.FromHours(2), "Other", "Other");
        var replacement = new AgendaRetrospectiveBuilder().Build(Monday, [], ClosedAt, other);
        Assert.False(await f.AddAsync(replacement, other)); EqualReport(original, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task ConcurrentInsertions_ProduceOneWinnerAndOnePreservedCapture()
    {
        using var f = await Fixture.CreateAsync(); var first = Report(title: "Primeira"); var second = Report(title: "Segunda");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> Add(AgendaWeekRetrospective report) => Task.Run(async () =>
        {
            await using var context = f.Root.CreateContext(); var store = Store(context); await gate.Task; return await store.TryAddAsync(report, Zone);
        });
        var a = Add(first); var b = Add(second); gate.SetResult(); var results = await Task.WhenAll(a, b);
        Assert.Equal(1, results.Count(value => value)); EqualReport(results[0] ? first : second, await f.GetAsync(Monday));
        await using var read = f.Root.CreateContext(); Assert.Single(await Store(read).ListAsync(Monday, Monday));
    }

    [Fact]
    public async Task MissingCapture_ReturnsNullAndAnEmptyList()
    {
        using var f = await Fixture.CreateAsync(); Assert.Null(await f.GetAsync(Monday));
        await using var context = f.Root.CreateContext(); Assert.Empty(await Store(context).ListAsync(Monday, Monday.AddDays(6)));
    }

    [Fact]
    public async Task HistoryList_UsesInclusiveStartDatesAndReturnsNewestFirst()
    {
        using var f = await Fixture.CreateAsync(); var current = Report(); var prior = Report(Monday.AddDays(-7)); var older = Report(Monday.AddDays(-14));
        await f.AddAsync(prior); await f.AddAsync(current); await f.AddAsync(older);
        await using var context = f.Root.CreateContext(); var list = await Store(context).ListAsync(prior.StartDate, current.StartDate);
        Assert.Equal(new[] { current.StartDate, prior.StartDate }, list.Select(item => item.StartDate));
        Assert.Single(await Store(context).ListAsync(Monday, Monday));
    }

    [Fact]
    public async Task InvalidHistoryRangeOrNonMondayKey_IsRejected()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext(); var store = Store(context);
        await Assert.ThrowsAsync<ArgumentException>(() => store.ListAsync(Monday.AddDays(1), Monday));
        await Assert.ThrowsAsync<ArgumentException>(() => store.GetAsync(Monday.AddDays(1)));
    }

    [Fact]
    public async Task LiveTaskEditsReopeningAndTrash_DoNotChangeTheStoredReport()
    {
        using var f = await Fixture.CreateAsync(); var task = TaskOn(Monday); await f.SaveTaskAsync(task);
        await f.Completion.CompleteAsync([task], ClosedAt.AddDays(-1)); var completed = task with { Status = AgendaTaskStatus.Completed };
        var report = new AgendaRetrospectiveBuilder().Build(Monday, [new(completed, ClosedAt.AddDays(-1))], ClosedAt, Zone); await f.AddAsync(report);
        await f.SaveTaskAsync(task with { Title = "Editada", Description = "Outro texto", PlannedDate = Monday.AddDays(14), Status = AgendaTaskStatus.InProgress });
        await f.Trash.MoveToTrashAsync(task.Id, ClosedAt.AddHours(1)); EqualReport(report, await f.GetAsync(Monday));
        await f.Trash.RestoreAsync(task.Id); EqualReport(report, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task CaptureDoesNotRequireLiveTaskRowsOrCascadeWhenTheyDisappear()
    {
        using var f = await Fixture.CreateAsync(); var report = Report(); var task = Assert.Single(report.Tasks).Task; await f.SaveTaskAsync(task); await f.AddAsync(report);
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("DELETE FROM AgendaTasks");
        EqualReport(report, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task ProfilesWithTheSameWeek_AreIndependent()
    {
        using var first = await Fixture.CreateAsync(); using var second = await Fixture.CreateAsync(); var a = Report(title: "Perfil A"); var b = Report(title: "Perfil B");
        Assert.True(await first.AddAsync(a)); Assert.True(await second.AddAsync(b)); EqualReport(a, await first.GetAsync(Monday)); EqualReport(b, await second.GetAsync(Monday));
    }

    [Fact]
    public async Task StoredZoneRules_ArePortableWithoutLookingUpTheCustomZoneId()
    {
        using var f = await Fixture.CreateAsync(); var report = Report(); await f.AddAsync(report); EqualReport(report, await f.GetAsync(Monday));
        await using var context = f.Root.CreateContext(); await context.Database.OpenConnectionAsync(); await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT TimeZoneData FROM AgendaWeekRetrospectives"; var serialized = Assert.IsType<string>(await command.ExecuteScalarAsync());
        var restored = TimeZoneInfo.FromSerializedString(serialized); Assert.Equal(Zone.Id, restored.Id); Assert.Equal(Zone.GetUtcOffset(ClosedAt), restored.GetUtcOffset(ClosedAt));
    }

    [Theory]
    [InlineData("totals")]
    [InlineData("duplicate")]
    [InlineData("week")]
    [InlineData("open")]
    [InlineData("zone")]
    public async Task InvalidCapture_IsRejectedWithoutInsertingAPartialRow(string fault)
    {
        using var f = await Fixture.CreateAsync(); var report = Report();
        var invalid = fault switch
        {
            "totals" => report with { PlannedCount = report.PlannedCount + 1 },
            "duplicate" => report with { Tasks = [report.Tasks[0], report.Tasks[0]] },
            "week" => report with { StartDate = Monday.AddDays(1) },
            "open" => report with { ClosedAt = ClosedAt.AddDays(-1) },
            _ => report with { TimeZoneId = "DifferentZone" }
        };
        await Assert.ThrowsAsync<ArgumentException>(() => f.AddAsync(invalid)); Assert.Null(await f.GetAsync(Monday)); Assert.True(await f.AddAsync(report));
    }

    [Fact]
    public async Task PreCancelledOperations_DoNotWriteOrReturnData()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext(); var store = Store(context); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.TryAddAsync(Report(), Zone, cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAsync(Monday, cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.ListAsync(Monday, Monday, cancel.Token)); Assert.Null(await f.GetAsync(Monday));
    }

    [Fact]
    public async Task FailedInsert_LeavesNoCaptureAndAllowsRetry()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext(); await RequireTableAsync(context);
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailRetrospectiveInsert BEFORE INSERT ON AgendaWeekRetrospectives BEGIN SELECT RAISE(ABORT, 'synthetic write failure'); END;");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => Store(context).TryAddAsync(Report(), Zone)); Assert.Null(await f.GetAsync(Monday));
        await context.Database.ExecuteSqlRawAsync("DROP TRIGGER FailRetrospectiveInsert"); Assert.True(await f.AddAsync(Report()));
    }

    [Fact]
    public async Task EleventhMigration_PreservesExistingNotesTasksTrashAndHistory()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = TaskOn(Monday) with { PlannedEndDate = Monday.AddDays(2) }; await f.SaveTaskAsync(task);
        await f.Trash.MoveToTrashAsync(task.Id, ClosedAt); var note = new NoteSnapshot(Guid.NewGuid(), "Nota anterior", "Conteúdo fictício");
        await using var context = f.Root.CreateContext(); await new SqliteNoteRepository(context).AddAsync(note);
        var before = (await context.Database.GetAppliedMigrationsAsync()).ToArray(); Assert.Equal(10, before.Length);
        await context.Database.MigrateAsync(); var after = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.True(after.Length >= 11); Assert.Equal(before, after.Take(10)); await RequireTableAsync(context);
        Assert.Equal(note, await new SqliteNoteRepository(context).GetByIdAsync(note.Id)); Assert.Equal(task, Assert.Single(await f.Trash.ListAsync()).Task);
    }

    [Fact]
    public async Task Source_ReadsPlanningAndCompletionMetadataTogetherInOneDatabaseRead()
    {
        using var f = await Fixture.CreateAsync(); var task = TaskOn(Monday.AddDays(-1)) with { PlannedEndDate = Monday.AddDays(2) }; await f.SaveTaskAsync(task);
        var actual = ClosedAt.AddDays(-2); await f.Completion.CompleteAsync([task], actual);
        var counter = new ReadCounter(); await using var context = f.ContextAt(f.Root.DirectoryPath, counter);
        var item = Assert.Single(await Source(context).ListOverlappingAsync(Monday, Monday.AddDays(6)));
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, item.Task); Assert.Equal(actual, item.LastCompletedAt); Assert.Equal(1, counter.Reads);
    }

    [Fact]
    public async Task Source_PreservesOldTimestampOnReopenedTasksAndDoesNotInventLegacyDates()
    {
        using var f = await Fixture.CreateAsync(); var task = TaskOn(Monday); var legacy = TaskOn(Monday) with { Status = AgendaTaskStatus.Completed };
        await f.SaveTaskAsync(task); await f.SaveTaskAsync(legacy); var actual = ClosedAt.AddDays(-2); await f.Completion.CompleteAsync([task], actual); await f.SaveTaskAsync(task);
        await using var context = f.Root.CreateContext(); var items = await Source(context).ListOverlappingAsync(Monday, Monday.AddDays(6));
        Assert.Equal(actual, Assert.Single(items, item => item.Task.Id == task.Id).LastCompletedAt); Assert.Equal(AgendaTaskStatus.Pending, Assert.Single(items, item => item.Task.Id == task.Id).Task.Status);
        Assert.Null(Assert.Single(items, item => item.Task.Id == legacy.Id).LastCompletedAt);
    }

    [Fact]
    public async Task Source_UsesInclusiveOverlapAndExcludesTrashAndOutsidePlanning()
    {
        using var f = await Fixture.CreateAsync(); var selected = TaskOn(Monday.AddDays(-2)) with { PlannedEndDate = Monday }; var hidden = TaskOn(Monday); var outside = TaskOn(Monday.AddDays(7));
        foreach (var task in new[] { selected, hidden, outside }) await f.SaveTaskAsync(task);
        await f.Trash.MoveToTrashAsync(hidden.Id, ClosedAt); await using var context = f.Root.CreateContext();
        Assert.Equal(selected, Assert.Single(await Source(context).ListOverlappingAsync(Monday, Monday.AddDays(6))).Task);
    }

    [Fact]
    public async Task Source_RejectsInvalidRangeAndCancellationBeforeQuery()
    {
        using var f = await Fixture.CreateAsync(); var counter = new ReadCounter(); await using var context = f.ContextAt(f.Root.DirectoryPath, counter); var source = Source(context);
        await Assert.ThrowsAsync<ArgumentException>(() => source.ListOverlappingAsync(Monday.AddDays(1), Monday));
        using var cancel = new CancellationTokenSource(); cancel.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ListOverlappingAsync(Monday, Monday, cancel.Token)); Assert.Equal(0, counter.Reads);
    }

    [Fact]
    public async Task BackupVersionOne_RestoresFrozenCaptureAndZoneRules()
    {
        using var f = await Fixture.CreateAsync(); var report = Report(); await f.AddAsync(report); var backup = new LocalBackupService(f.Root.DirectoryPath); await backup.CreateAsync(f.Archive);
        using (var zip = ZipFile.OpenRead(f.Archive))
        {
            await using var stream = zip.GetEntry("manifest.json")!.Open(); Assert.Equal(1, (await JsonSerializer.DeserializeAsync<BackupManifest>(stream, Json))!.FormatVersion);
        }
        await backup.RestoreAsync(f.Archive, f.Restored); await using var restored = f.ContextAt(f.Restored); EqualReport(report, await Store(restored).GetAsync(Monday));
    }

    [Fact]
    public async Task LegacyBackup_UpgradesOnlyTheRestoredCopyAndCreatesNoInventedReport()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = TaskOn(Monday); await f.SaveTaskAsync(task);
        var original = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, original);
        await new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored); Assert.Equal(original, await File.ReadAllBytesAsync(f.Database));
        await using var restored = f.ContextAt(f.Restored); await RequireTableAsync(restored); Assert.Empty(await Store(restored).ListAsync(DateOnly.MinValue, DateOnly.MaxValue));
        Assert.Equal(task, await new SqliteAgendaActions(Path.Combine(f.Restored, "trama.db")).GetByIdAsync(task.Id));
    }

    [Theory]
    [InlineData("json")]
    [InlineData("totals")]
    [InlineData("version")]
    [InlineData("timezone")]
    public async Task CorruptCapture_IsRejectedByReadAndBackupCreation(string fault)
    {
        using var f = await Fixture.CreateAsync(); await f.AddAsync(Report()); await f.CorruptAsync(fault);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.GetAsync(Monday));
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).CreateAsync(f.Archive)); Assert.False(File.Exists(f.Archive));
    }

    [Theory]
    [InlineData("json")]
    [InlineData("totals")]
    [InlineData("version")]
    [InlineData("timezone")]
    public async Task CorruptCapture_RestorationIsRejectedWithoutPublishingADestination(string fault)
    {
        using var f = await Fixture.CreateAsync(); await f.AddAsync(Report()); await f.CorruptAsync(fault); var original = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, original);
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored));
        Assert.False(Directory.Exists(f.Restored)); Assert.Equal(original, await File.ReadAllBytesAsync(f.Database));
    }

    private static AgendaWeekRetrospective Report(DateOnly? start = null, string title = "Tarefa capturada", DateTimeOffset? capturedAt = null)
    {
        var day = start ?? Monday; var task = TaskOn(day) with { Title = title, PlannedEndDate = day.AddDays(2), Status = AgendaTaskStatus.Completed };
        return new AgendaRetrospectiveBuilder().Build(day, [new(task, new DateTimeOffset(day.AddDays(1).ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(-3)))], capturedAt ?? ClosedAt, Zone);
    }
    private static AgendaTaskSnapshot TaskOn(DateOnly day) => new(Guid.NewGuid(), "Tarefa fictícia", "Conteúdo preservado", day, new TimeOnly(9, 0), new TimeOnly(10, 0), AgendaTaskCategory.Study);
    private static void EqualReport(AgendaWeekRetrospective expected, AgendaWeekRetrospective? actual) { Assert.NotNull(actual); Assert.Equal(JsonSerializer.Serialize(expected, Json), JsonSerializer.Serialize(actual, Json)); }
    private static object Repository(TramaDbContext context)
    {
        var type = typeof(SqliteAgendaActions).Assembly.GetType("Trama.Infrastructure.Persistence.SqliteAgendaRetrospectiveRepository"); Assert.NotNull(type);
        return Assert.IsAssignableFrom<object>(Activator.CreateInstance(type, context));
    }
    private static IAgendaRetrospectiveStore Store(TramaDbContext context) => Assert.IsAssignableFrom<IAgendaRetrospectiveStore>(Repository(context));
    private static IAgendaRetrospectiveSource Source(TramaDbContext context) => Assert.IsAssignableFrom<IAgendaRetrospectiveSource>(Repository(context));
    private static async Task RequireTableAsync(TramaDbContext context)
    {
        await context.Database.OpenConnectionAsync(); await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'AgendaWeekRetrospectives'";
        Assert.Equal("AgendaWeekRetrospectives", await command.ExecuteScalarAsync());
    }
    private static async Task WriteArchiveAsync(string path, byte[] database)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create); await using (var stream = archive.CreateEntry("trama.db").Open()) await stream.WriteAsync(database);
        var hash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant(); await using var metadata = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(metadata, new BackupManifest(1, [new BackupFileEntry("trama.db", database.LongLength, hash)]), Json);
    }
    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Reads { get; private set; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) Reads++;
            return new ValueTask<DbDataReader>(result);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public string Archive => Path.Combine(Root.DirectoryPath, "retrospective.trama-backup");
        public string Restored => Path.Combine(Root.DirectoryPath, "restored");
        public SqliteAgendaActions Actions => new(Database);
        public SqliteAgendaCompletionActions Completion => new(Database);
        public SqliteAgendaTrashActions Trash => new(Database);
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture();
            try { await using var context = f.Root.CreateContext(); if (legacy) await context.GetService<IMigrator>().MigrateAsync(PreviousMigration); else await context.Database.MigrateAsync(); return f; }
            catch { f.Dispose(); throw; }
        }
        public async Task SaveTaskAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        public async Task<bool> AddAsync(AgendaWeekRetrospective report, TimeZoneInfo? zone = null) { await using var context = Root.CreateContext(); return await Store(context).TryAddAsync(report, zone ?? Zone); }
        public async Task<AgendaWeekRetrospective?> GetAsync(DateOnly start) { await using var context = Root.CreateContext(); return await Store(context).GetAsync(start); }
        public TramaDbContext ContextAt(string directory, DbCommandInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite($"Data Source={Path.Combine(directory, "trama.db")};Pooling=False");
            if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options);
        }
        public async Task CorruptAsync(string fault)
        {
            await using var context = Root.CreateContext(); await RequireTableAsync(context);
            var sql = fault switch
            {
                "json" => "UPDATE AgendaWeekRetrospectives SET Content = '{{invalid'",
                "totals" => "UPDATE AgendaWeekRetrospectives SET Content = json_set(Content, '$.plannedCount', 999)",
                "version" => "UPDATE AgendaWeekRetrospectives SET FormatVersion = 99",
                _ => "UPDATE AgendaWeekRetrospectives SET TimeZoneData = 'invalid-zone-data'"
            };
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        public void Dispose() => Root.Dispose();
    }
}
