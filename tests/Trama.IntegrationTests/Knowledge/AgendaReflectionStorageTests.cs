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

public sealed class AgendaReflectionStorageTests
{
    private const string PreviousMigration = "20261107143302_AgendaWeekRetrospectives";
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 11, 10, 18, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Reflection_ReopensWithTextAndUtcTimestampIntact()
    {
        using var f = await Fixture.CreateAsync(); var expected = Example() with { WorkedWell = "  Avancei no C#\nFoco ✓  ", Adjustments = "Revisar\r\nreferências 📚" };
        await f.SaveAsync(expected); Assert.Equal(expected, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task UpdatingOneWeek_DoesNotCreateDuplicateRows()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); var updated = Example() with { WorkedWell = "Editada", UpdatedAt = UpdatedAt.AddHours(1) };
        await f.SaveAsync(updated); Assert.Equal(updated, await f.GetAsync(Monday));
        await using var context = f.Root.CreateContext(); await context.Database.OpenConnectionAsync(); await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM AgendaWeekReflections"; Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task ClearingBothFields_ReplacesTheSavedText()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); var empty = Example() with { WorkedWell = "", Adjustments = "" };
        await f.SaveAsync(empty); Assert.Equal(empty, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task LongText_IsNotTruncatedByStorage()
    {
        using var f = await Fixture.CreateAsync(); var expected = Example() with { WorkedWell = new string('á', 20_000), Adjustments = "  \t\n  " };
        await f.SaveAsync(expected); Assert.Equal(expected, await f.GetAsync(Monday));
    }

    [Fact]
    public async Task DifferentWeeks_AreIndependent()
    {
        using var f = await Fixture.CreateAsync(); await f.AddClosureAsync(Monday.AddDays(-7)); var other = Example() with { WeekStart = Monday.AddDays(-7), WorkedWell = "Outra semana" };
        await f.SaveAsync(Example()); await f.SaveAsync(other); Assert.Equal(Example(), await f.GetAsync(Monday)); Assert.Equal(other, await f.GetAsync(other.WeekStart));
    }

    [Fact]
    public async Task SameWeekInTwoProfiles_IsIsolated()
    {
        using var a = await Fixture.CreateAsync(); using var b = await Fixture.CreateAsync(); var other = Example() with { WorkedWell = "Perfil B" };
        await a.SaveAsync(Example()); await b.SaveAsync(other); Assert.Equal(Example(), await a.GetAsync(Monday)); Assert.Equal(other, await b.GetAsync(Monday));
    }

    [Fact]
    public async Task ReflectionUpdates_LeaveCapturedReportUnchanged()
    {
        using var f = await Fixture.CreateAsync(); var before = await f.CaptureContentAsync(); await f.SaveAsync(Example());
        await f.SaveAsync(Example() with { Adjustments = "Revisão posterior" }); Assert.Equal(before, await f.CaptureContentAsync());
    }

    [Fact]
    public async Task MissingReflection_ReturnsNullAndClosureQueryDoesNotCreateIt()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext(); var repository = Repository(context);
        Assert.True(await repository.HasClosureAsync(Monday)); Assert.False(await repository.HasClosureAsync(Monday.AddDays(-7)));
        Assert.Null(await Reader(context).GetAsync(Monday)); Assert.Null(await Reader(context).GetAsync(Monday.AddDays(-7)));
    }

    [Fact]
    public async Task InvalidReaderAndClosureKeys_AreRejected()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext();
        await Assert.ThrowsAsync<ArgumentException>(() => Reader(context).GetAsync(Monday.AddDays(1)));
        await Assert.ThrowsAsync<ArgumentException>(() => Repository(context).HasClosureAsync(Monday.AddDays(1)));
    }

    [Theory]
    [InlineData("week")]
    [InlineData("worked-null")]
    [InlineData("adjust-null")]
    [InlineData("offset")]
    [InlineData("timestamp")]
    public async Task InvalidReflection_IsRejectedBeforeWriting(string fault)
    {
        using var f = await Fixture.CreateAsync(); var value = fault switch
        {
            "week" => Example() with { WeekStart = Monday.AddDays(1) },
            "worked-null" => Example() with { WorkedWell = null! },
            "adjust-null" => Example() with { Adjustments = null! },
            "offset" => Example() with { UpdatedAt = UpdatedAt.ToOffset(TimeSpan.FromHours(3)) },
            _ => Example() with { UpdatedAt = default }
        };
        await Assert.ThrowsAsync<ArgumentException>(() => f.SaveAsync(value)); Assert.Null(await f.GetAsync(Monday));
    }

    [Fact]
    public async Task NullReflection_IsRejected()
    {
        using var f = await Fixture.CreateAsync(); await Assert.ThrowsAsync<ArgumentNullException>(() => f.SaveAsync(null!));
    }

    [Fact]
    public async Task SavingWithoutAClosure_IsRejectedWithoutCreatingParentOrChild()
    {
        using var f = await Fixture.CreateAsync(withClosure: false); await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.SaveAsync(Example()));
        Assert.Null(await f.GetAsync(Monday)); await using var context = f.Root.CreateContext(); Assert.Null(await new SqliteAgendaRetrospectiveRepository(context).GetAsync(Monday));
    }

    [Fact]
    public async Task ForeignKey_RejectsRawOrphanInsertAndParentDeletion()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await using var context = f.Root.CreateContext();
        await Assert.ThrowsAnyAsync<DbException>(() => context.Database.ExecuteSqlRawAsync("DELETE FROM AgendaWeekRetrospectives"));
        var absent = Monday.AddDays(-7);
        await Assert.ThrowsAnyAsync<DbException>(() => context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgendaWeekReflections (WeekStart, WorkedWell, Adjustments, UpdatedAt) VALUES ({absent}, {"Órfã"}, {""}, {UpdatedAt})"));
        Assert.Equal(Example(), await f.GetAsync(Monday));
    }

    [Fact]
    public async Task CallerTransaction_RollbackRestoresPreviousReflection()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await using var context = f.Root.CreateContext();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await Repository(context).SaveAsync(Example() with { WorkedWell = "Não confirmar" }); await transaction.RollbackAsync();
        }
        Assert.Equal(Example(), await f.GetAsync(Monday)); Assert.Null(await Reader(context).GetAsync(Monday.AddDays(-7)));
    }

    [Fact]
    public async Task SameContext_ReadsTheLatestSavedValueWithoutTrackingStaleData()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext(); var repository = Repository(context); var reader = Reader(context);
        await repository.SaveAsync(Example()); Assert.Equal(Example(), await reader.GetAsync(Monday)); var next = Example() with { WorkedWell = "Texto novo" };
        await repository.SaveAsync(next); Assert.Equal(next, await reader.GetAsync(Monday));
    }

    [Fact]
    public async Task PreCancelledOperations_DoNotReadOrWrite()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext(); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Repository(context).SaveAsync(Example(), cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Repository(context).HasClosureAsync(Monday, cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Reader(context).GetAsync(Monday, cts.Token)); Assert.Null(await f.GetAsync(Monday));
    }

    [Fact]
    public async Task CancellationAtInsert_PreventsCommitAndAllowsRetry()
    {
        using var f = await Fixture.CreateAsync(); using var cts = new CancellationTokenSource(); var probe = new CancelInsert(cts);
        await using (var context = f.ContextAt(f.Root.DirectoryPath, probe))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Repository(context).SaveAsync(Example(), cts.Token));
        Assert.True(probe.Seen); Assert.Null(await f.GetAsync(Monday)); await f.SaveAsync(Example()); Assert.Equal(Example(), await f.GetAsync(Monday));
    }

    [Fact]
    public async Task ConcurrentWritesToOneWeek_KeepOneWholeRecord()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var second = Example() with { WorkedWell = "Segundo texto", Adjustments = "Segundo ajuste", UpdatedAt = UpdatedAt.AddHours(1) };
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task Write(AgendaWeekReflection value) => Task.Run(async () => { await gate.Task; await f.SaveAsync(value); });
        var a = Write(first); var b = Write(second); gate.SetResult(); await Task.WhenAll(a, b);
        Assert.Contains(await f.GetAsync(Monday), new[] { first, second });
    }

    [Fact]
    public async Task ConcurrentWritesToDifferentWeeks_PreserveBoth()
    {
        using var f = await Fixture.CreateAsync(); await f.AddClosureAsync(Monday.AddDays(-7)); var other = Example() with { WeekStart = Monday.AddDays(-7), WorkedWell = "Anterior" };
        await Task.WhenAll(Task.Run(() => f.SaveAsync(Example())), Task.Run(() => f.SaveAsync(other)));
        Assert.Equal(Example(), await f.GetAsync(Monday)); Assert.Equal(other, await f.GetAsync(other.WeekStart));
    }

    [Fact]
    public async Task TwelfthMigration_PreservesNotesTasksTrashClosuresAndHistory()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = new AgendaTaskSnapshot(Guid.NewGuid(), "Tarefa anterior", "Preservada", Monday);
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SqliteAgendaActions(f.Database).SaveAsync(task)).Status);
        await new SqliteAgendaTrashActions(f.Database).MoveToTrashAsync(task.Id, UpdatedAt);
        var note = new NoteSnapshot(Guid.NewGuid(), "Nota anterior", "Conteúdo preservado");
        await using var context = f.Root.CreateContext(); await new SqliteNoteRepository(context).AddAsync(note); var before = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(11, before.Length); var capture = await f.CaptureContentAsync(); await context.Database.MigrateAsync();
        var after = (await context.Database.GetAppliedMigrationsAsync()).ToArray(); Assert.True(after.Length >= 12); Assert.Equal(before, after.Take(11));
        Assert.Equal(note, await new SqliteNoteRepository(context).GetByIdAsync(note.Id)); Assert.Equal(task, Assert.Single(await new SqliteAgendaTrashActions(f.Database).ListAsync()).Task);
        Assert.Equal(capture, await f.CaptureContentAsync()); Assert.Null(await Reader(context).GetAsync(Monday)); await f.SaveAsync(Example());
    }

    [Fact]
    public async Task BackupVersionOne_RestoresReflectionAndPreservedCapture()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); var capture = await f.CaptureContentAsync(); var backup = new LocalBackupService(f.Root.DirectoryPath);
        await backup.CreateAsync(f.Archive); using (var zip = ZipFile.OpenRead(f.Archive))
        {
            await using var stream = zip.GetEntry("manifest.json")!.Open(); var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, Json); Assert.NotNull(manifest); Assert.Equal(1, manifest.FormatVersion);
        }
        await backup.RestoreAsync(f.Archive, f.Restored); await using var context = f.ContextAt(f.Restored);
        Assert.Equal(Example(), await Reader(context).GetAsync(Monday)); var actual = await new SqliteAgendaRetrospectiveRepository(context).GetAsync(Monday);
        Assert.NotNull(actual); Assert.Equal(capture, JsonSerializer.Serialize(actual, Json));
    }

    [Fact]
    public async Task LegacyBackupWithoutReflections_RestoresAnEmptyReflectionTable()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var original = await File.ReadAllBytesAsync(f.Database); await WriteArchive(f.Archive, original);
        await new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored);
        await using var context = f.ContextAt(f.Restored); Assert.Null(await Reader(context).GetAsync(Monday)); Assert.NotNull(await new SqliteAgendaRetrospectiveRepository(context).GetAsync(Monday));
        Assert.Equal(original, await File.ReadAllBytesAsync(f.Database));
    }

    [Theory]
    [InlineData("timestamp")]
    [InlineData("offset")]
    [InlineData("orphan")]
    public async Task CorruptReflection_ReadAndBackupCreationAreRejected(string fault)
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await f.CorruptAsync(fault);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.GetAsync(Monday));
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).CreateAsync(f.Archive)); Assert.False(File.Exists(f.Archive));
    }

    [Theory]
    [InlineData("timestamp")]
    [InlineData("offset")]
    [InlineData("orphan")]
    public async Task CorruptReflection_RestoreDoesNotPublishDestination(string fault)
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await f.CorruptAsync(fault); var bytes = await File.ReadAllBytesAsync(f.Database);
        await WriteArchive(f.Archive, bytes); await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored));
        Assert.False(Directory.Exists(f.Restored)); Assert.Equal(bytes, await File.ReadAllBytesAsync(f.Database));
    }

    [Fact]
    public async Task CorruptParent_IsRejectedInsteadOfReturningAUsableReflection()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await using var context = f.Root.CreateContext();
        await context.Database.ExecuteSqlRawAsync("UPDATE AgendaWeekRetrospectives SET FormatVersion = 99");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.GetAsync(Monday));
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => Repository(context).HasClosureAsync(Monday));
    }

    private static AgendaWeekReflection Example() => new(Monday, "Funcionou bem", "Ajustar prioridades", UpdatedAt);
    private static object Store(TramaDbContext context)
    {
        var type = typeof(SqliteAgendaActions).Assembly.GetType("Trama.Infrastructure.Persistence.SqliteAgendaReflectionRepository"); Assert.NotNull(type);
        var value = Activator.CreateInstance(type, context); Assert.NotNull(value); return value;
    }
    private static IAgendaReflectionRepository Repository(TramaDbContext context) => Assert.IsAssignableFrom<IAgendaReflectionRepository>(Store(context));
    private static IAgendaReflectionReader Reader(TramaDbContext context) => Assert.IsAssignableFrom<IAgendaReflectionReader>(Store(context));
    private static async Task WriteArchive(string path, byte[] bytes)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create); await using (var stream = archive.CreateEntry("trama.db").Open()) await stream.WriteAsync(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(); await using var metadata = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(metadata, new BackupManifest(1, [new BackupFileEntry("trama.db", bytes.LongLength, hash)]), Json);
    }
    private sealed class CancelInsert(CancellationTokenSource cts) : DbCommandInterceptor
    {
        public bool Seen { get; private set; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO AgendaWeekReflections", StringComparison.OrdinalIgnoreCase)) { Seen = true; cts.Cancel(); cancellationToken.ThrowIfCancellationRequested(); }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public string Archive => Path.Combine(Root.DirectoryPath, "reflection.trama-backup");
        public string Restored => Path.Combine(Root.DirectoryPath, "restored");
        public static async Task<Fixture> CreateAsync(bool withClosure = true, bool legacy = false)
        {
            var f = new Fixture();
            try
            {
                await using var context = f.Root.CreateContext(); if (legacy) await context.GetService<IMigrator>().MigrateAsync(PreviousMigration); else await context.Database.MigrateAsync();
                _ = Repository(context); if (withClosure) await f.AddClosureAsync(Monday); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public async Task AddClosureAsync(DateOnly start)
        {
            await using var context = Root.CreateContext(); var report = new AgendaRetrospectiveBuilder().Build(start, [], UpdatedAt, TimeZoneInfo.Utc);
            Assert.True(await new SqliteAgendaRetrospectiveRepository(context).TryAddAsync(report, TimeZoneInfo.Utc));
        }
        public async Task SaveAsync(AgendaWeekReflection value) { await using var context = Root.CreateContext(); await Repository(context).SaveAsync(value); }
        public async Task<AgendaWeekReflection?> GetAsync(DateOnly start) { await using var context = Root.CreateContext(); return await Reader(context).GetAsync(start); }
        public async Task<string> CaptureContentAsync()
        {
            await using var context = Root.CreateContext(); var report = await new SqliteAgendaRetrospectiveRepository(context).GetAsync(Monday); Assert.NotNull(report); return JsonSerializer.Serialize(report, Json);
        }
        public TramaDbContext ContextAt(string directory, DbCommandInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite($"Data Source={Path.Combine(directory, "trama.db")};Pooling=False");
            if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options);
        }
        public async Task CorruptAsync(string fault)
        {
            await using var context = Root.CreateContext(); var sql = fault switch
            {
                "timestamp" => "UPDATE AgendaWeekReflections SET UpdatedAt = 'invalid-time'",
                "offset" => "UPDATE AgendaWeekReflections SET UpdatedAt = '2026-11-10 18:00:00+03:00'",
                _ => "PRAGMA foreign_keys = OFF; DELETE FROM AgendaWeekRetrospectives; PRAGMA foreign_keys = ON;"
            };
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        public void Dispose() => Root.Dispose();
    }
}
