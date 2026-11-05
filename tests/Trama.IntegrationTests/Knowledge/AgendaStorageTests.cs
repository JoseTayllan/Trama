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

public sealed class AgendaStorageTests
{
    private static readonly DateOnly Day = new(2026, 11, 2);
    private const string PreviousMigration = "20261031134505_LibraryTrash";

    [Theory]
    [InlineData(AgendaTaskCategory.Personal, AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskCategory.Work, AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskCategory.Study, AgendaTaskStatus.Completed)]
    public async Task SaveAndReopen_PreserveEveryFieldAndTimePrecision(AgendaTaskCategory category, AgendaTaskStatus status)
    {
        using var f = await Fixture.CreateAsync();
        var task = Example() with
        {
            Title = "  Estudar C# 🧵  ",
            Description = "\r\n    exemplo #estudo\t",
            Category = category,
            Status = status,
            StartTime = new TimeOnly(18, 15, 20).Add(TimeSpan.FromTicks(1234))
        };
        await f.SaveAsync(task);
        await using var reopened = f.Root.CreateContext();
        Assert.Equal(task, await new SqliteAgendaTaskRepository(reopened).GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task UpdateSameIdentity_ReplacesFieldsWithoutDuplicatingOrChangingAnotherTask()
    {
        using var f = await Fixture.CreateAsync();
        var task = Example(); var other = Example() with { Title = "Outra tarefa" };
        await f.SaveAsync(task); await f.SaveAsync(other);
        var changed = task with
        {
            Title = "Reagendada",
            Description = "Descrição revista",
            PlannedDate = Day.AddDays(1),
            StartTime = null,
            EndTime = null,
            Category = AgendaTaskCategory.Work,
            Status = AgendaTaskStatus.Completed
        };
        await f.SaveAsync(changed);
        await using var reader = f.Root.CreateContext(); var repository = new SqliteAgendaTaskRepository(reader);
        var tasks = await repository.ListAsync(Day, Day.AddDays(1));
        Assert.Equal(2, tasks.Count); Assert.Contains(changed, tasks); Assert.Contains(other, tasks);
        Assert.Equal(changed, await repository.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task DateWithoutTime_ReopensWithoutInventedHours()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { StartTime = null, EndTime = null };
        await f.SaveAsync(task);
        await using var context = f.Root.CreateContext();
        Assert.Equal(task, await new SqliteAgendaTaskRepository(context).GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task RangeQuery_IncludesBothDatesAndExcludesOutsideTasks()
    {
        using var f = await Fixture.CreateAsync();
        var first = Example(); var last = Example() with { PlannedDate = Day.AddDays(6) };
        foreach (var task in new[] { first, last, Example() with { PlannedDate = Day.AddDays(-1) }, Example() with { PlannedDate = Day.AddDays(7) } })
            await f.SaveAsync(task);
        await using var context = f.Root.CreateContext();
        Assert.Equal(new[] { first, last }, await new SqliteAgendaTaskRepository(context).ListAsync(Day, Day.AddDays(6)));
    }

    [Fact]
    public async Task RangeQuery_OrdersByDayTimeThenUntimedWithStableTitleAndIdentityTies()
    {
        using var f = await Fixture.CreateAsync();
        var early = Example() with { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(9, 0) };
        var lateA = Example() with { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "A" };
        var lateB = lateA with { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") };
        var lateZ = Example() with { Title = "Z" };
        var untimed = Example() with { StartTime = null, EndTime = null, Title = "A" };
        var tomorrow = early with { Id = Guid.NewGuid(), PlannedDate = Day.AddDays(1) };
        foreach (var task in new[] { tomorrow, untimed, lateZ, lateB, early, lateA }) await f.SaveAsync(task);
        await using var context = f.Root.CreateContext();
        Assert.Equal(new[] { early, lateA, lateB, lateZ, untimed, tomorrow },
            await new SqliteAgendaTaskRepository(context).ListAsync(Day, Day.AddDays(1)));
    }

    [Fact]
    public async Task MissingIdentity_ReturnsNull()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext();
        Assert.Null(await new SqliteAgendaTaskRepository(context).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task EmptyPeriod_ReturnsAnEmptyCollection()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext();
        Assert.Empty(await new SqliteAgendaTaskRepository(context).ListAsync(Day, Day));
    }

    [Fact]
    public async Task ReversedPeriod_IsRejected()
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext();
        await Assert.ThrowsAsync<ArgumentException>(() => new SqliteAgendaTaskRepository(context).ListAsync(Day.AddDays(1), Day));
    }

    [Theory]
    [InlineData("save")]
    [InlineData("get")]
    [InlineData("list")]
    public async Task PreCancellation_IsPropagated(string operation)
    {
        using var f = await Fixture.CreateAsync(); await using var context = f.Root.CreateContext();
        var repository = new SqliteAgendaTaskRepository(context); using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (operation == "save") await repository.SaveAsync(Example(), source.Token);
            else if (operation == "get") await repository.GetByIdAsync(Guid.NewGuid(), source.Token);
            else await repository.ListAsync(Day, Day, source.Token);
        });
    }

    [Fact]
    public async Task ReadOnlyWrite_ReportsPersistenceFailureAndKeepsTheOriginal()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await using (var readOnly = f.Root.CreateContext(readOnly: true))
        {
            var result = await new SaveAgendaTask(new SqliteAgendaTaskRepository(readOnly)).ExecuteAsync(task with { Title = "Não gravar" });
            Assert.Equal(AgendaTaskSaveStatus.PersistenceFailed, result.Status);
        }
        await using var reader = f.Root.CreateContext();
        Assert.Equal(task, await new SqliteAgendaTaskRepository(reader).GetByIdAsync(task.Id));
    }

    [Theory]
    [InlineData("category")]
    [InlineData("status")]
    [InlineData("title")]
    public async Task CorruptStoredTask_IsRejectedOnRead(string fault)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await f.CorruptAsync(fault);
        await using var reader = f.Root.CreateContext();
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => new SqliteAgendaTaskRepository(reader).GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task RepeatedRead_ReflectsCommittedChangesFromAnotherContext()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await using var reader = f.Root.CreateContext(); var repository = new SqliteAgendaTaskRepository(reader);
        Assert.Equal(task, await repository.GetByIdAsync(task.Id));
        var changed = task with { Title = "Atualizada em outro contexto" }; await f.SaveAsync(changed);
        Assert.Equal(changed, await repository.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task NewMigration_PreservesExistingNotebookAndAddsAgendaStorage()
    {
        using var f = await Fixture.CreateAsync(legacy: true);
        var page = new NotebookSnapshot(Guid.NewGuid(), "Caderno existente", [new NotebookTextBlock(Guid.NewGuid(), "Conteúdo a preservar")]);
        await using (var old = f.Root.CreateContext()) await new SqliteNotebookRepository(old).SaveAsync(page);
        await f.Root.MigrateAsync();
        await using var context = f.Root.CreateContext();
        AssertPage(page, await new SqliteNotebookRepository(context).GetByIdAsync(page.Id));
        Assert.True((await context.Database.GetAppliedMigrationsAsync()).Count() > 6, "A Agenda requer migração nova; preservar as seis anteriores.");
        Assert.Empty(await new SqliteAgendaTaskRepository(context).ListAsync(Day, Day));
    }

    [Fact]
    public async Task BackupAndRestore_KeepAgendaAndNotebookWithPackageVersionOne()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        var page = new NotebookSnapshot(Guid.NewGuid(), "Anotação", [new NotebookTextBlock(Guid.NewGuid(), "Caderno e agenda independentes")]);
        await using (var context = f.Root.CreateContext()) await new SqliteNotebookRepository(context).SaveAsync(page);
        var backup = new LocalBackupService(f.Root.DirectoryPath); await backup.CreateAsync(f.Archive);
        using (var archive = ZipFile.OpenRead(f.Archive))
        {
            await using var metadata = archive.GetEntry("manifest.json")!.Open();
            Assert.Equal(1, (await JsonSerializer.DeserializeAsync<BackupManifest>(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)))!.FormatVersion);
        }
        await backup.RestoreAsync(f.Archive, f.Restored);
        await using var restored = f.RestoredContext();
        Assert.Equal(task, await new SqliteAgendaTaskRepository(restored).GetByIdAsync(task.Id));
        AssertPage(page, await new SqliteNotebookRepository(restored).GetByIdAsync(page.Id));
    }

    [Fact]
    public async Task LegacyBackup_RestoresAndUpgradesWithoutChangingTheSource()
    {
        using var f = await Fixture.CreateAsync(legacy: true);
        var note = new NoteSnapshot(Guid.NewGuid(), "Nota antiga", "Texto anterior à Agenda");
        await using (var context = f.Root.CreateContext()) await new SqliteNoteRepository(context).AddAsync(note);
        var bytes = await File.ReadAllBytesAsync(f.Database);
        await WriteArchiveAsync(f.Archive, bytes);
        await new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(f.Database));
        await using var restored = f.RestoredContext();
        Assert.Equal(note, await new SqliteNoteRepository(restored).GetByIdAsync(note.Id));
        Assert.True((await restored.Database.GetAppliedMigrationsAsync()).Count() > 6);
        Assert.Empty(await new SqliteAgendaTaskRepository(restored).ListAsync(Day, Day));
    }

    [Fact]
    public async Task BackupCreation_RejectsInvalidAgendaDataWithoutPublishingAnArchive()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await f.CorruptAsync("category");
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).CreateAsync(f.Archive));
        Assert.False(File.Exists(f.Archive));
    }

    [Fact]
    public async Task Restore_RejectsInvalidAgendaDespiteValidHashesAndKeepsSourceUntouched()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example()); await f.CorruptAsync("status");
        var bytes = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, bytes);
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored));
        Assert.False(Directory.Exists(f.Restored));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(f.Database));
    }

    [Fact]
    public void NullContext_IsRejected() => Assert.Throws<ArgumentNullException>(() => new SqliteAgendaTaskRepository(null!));

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Estudar interfaces", "Revisar exemplos de C#", Day,
        new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Study);

    private static void AssertPage(NotebookSnapshot expected, NotebookSnapshot? actual)
    {
        Assert.NotNull(actual); Assert.Equal(expected.Id, actual.Id); Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(Assert.IsType<NotebookTextBlock>(Assert.Single(expected.Blocks)).Text,
            Assert.IsType<NotebookTextBlock>(Assert.Single(actual.Blocks)).Text);
    }

    private static async Task WriteArchiveAsync(string path, byte[] database)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        await using (var entry = archive.CreateEntry("trama.db").Open()) await entry.WriteAsync(database);
        var hash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant();
        await using var metadata = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(metadata, new BackupManifest(1, [new BackupFileEntry("trama.db", database.LongLength, hash)]),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public string Archive => Path.Combine(Root.DirectoryPath, "agenda.trama-backup");
        public string Restored => Path.Combine(Root.DirectoryPath, "restored");
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture();
            if (legacy)
            {
                await using var context = f.Root.CreateContext();
                await context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            }
            else await f.Root.MigrateAsync();
            return f;
        }
        public async Task SaveAsync(AgendaTaskSnapshot task)
        {
            await using var context = Root.CreateContext();
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SaveAgendaTask(new SqliteAgendaTaskRepository(context)).ExecuteAsync(task)).Status);
        }
        public async Task CorruptAsync(string fault)
        {
            await using var context = Root.CreateContext();
            await context.Database.OpenConnectionAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA ignore_check_constraints = ON");
            if (fault == "category") await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET Category = 999");
            else if (fault == "status") await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET Status = 999");
            else await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET Title = '   '");
        }
        public TramaDbContext RestoredContext() => new(new DbContextOptionsBuilder<TramaDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(Restored, "trama.db"), Pooling = false }.ToString()).Options);
        public void Dispose() => Root.Dispose();
    }
}
