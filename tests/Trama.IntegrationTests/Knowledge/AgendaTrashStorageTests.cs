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

public sealed class AgendaTrashStorageTests
{
    private const string PreviousMigration = "20261104234835_AgendaTaskLastCompletion";
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly DateTimeOffset DeletedAt = new(2026, 11, 5, 14, 15, 0, TimeSpan.FromHours(-3));
    private static readonly DateTimeOffset CompletedAt = DeletedAt.AddHours(-1);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative.db")]
    public void InvalidPath_IsRejected(string? path) => Assert.ThrowsAny<ArgumentException>(() => new SqliteAgendaTrashActions(path!));

    [Fact]
    public void Construction_DoesNotCreateProfileOrDatabase()
    {
        using var root = new NotebookTestWorkspace(); var directory = Path.Combine(root.DirectoryPath, "missing");
        _ = new SqliteAgendaTrashActions(Path.Combine(directory, "trama.db")); Assert.False(Directory.Exists(directory));
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskStatus.Completed)]
    public async Task TrashAndRestore_PreserveIdentityPlanningContentStatusAndActualTime(AgendaTaskStatus status)
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { Status = status };
        await f.SaveAsync(task with { Status = status == AgendaTaskStatus.Completed ? AgendaTaskStatus.Pending : status });
        if (status == AgendaTaskStatus.Completed) await f.CompleteAsync(task.Id);
        await f.Trash.MoveToTrashAsync(task.Id, DeletedAt);
        Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await f.Actions.ListAsync(Day, Day));
        Assert.Empty(await f.Completion.ListCandidatesAsync(Day)); Assert.Null(await f.Completion.GetLastCompletedAtAsync(task.Id));
        Assert.Equal(1, await f.PhysicalCountAsync());
        var entry = Assert.Single(await new SqliteAgendaTrashActions(f.Database).ListAsync());
        Assert.Equal(task, entry.Task); Assert.Equal(DeletedAt, entry.DeletedAt); Assert.Equal(DeletedAt.Offset, entry.DeletedAt.Offset);
        Assert.Equal(status == AgendaTaskStatus.Completed ? CompletedAt : (DateTimeOffset?)null, entry.LastCompletedAt);
        await f.Trash.RestoreAsync(task.Id);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await f.Trash.ListAsync());
        Assert.Equal(status == AgendaTaskStatus.Completed ? CompletedAt : (DateTimeOffset?)null, await f.Completion.GetLastCompletedAtAsync(task.Id));
        Assert.Equal(status == AgendaTaskStatus.Completed ? 0 : 1, (await f.Completion.ListCandidatesAsync(Day)).Count);
    }

    [Fact]
    public async Task TrashList_OrdersByDeletionTimeThenTitleIgnoringCaseAndIdentity()
    {
        using var f = await Fixture.CreateAsync(); var earlier = Example() with { Title = "B" };
        var a = Example() with { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "A" };
        var b = a with { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Title = "a" };
        foreach (var task in new[] { earlier, b, a }) await f.SaveAsync(task);
        await f.Trash.MoveToTrashAsync(earlier.Id, DeletedAt.AddMinutes(-1)); await f.Trash.MoveToTrashAsync(b.Id, DeletedAt); await f.Trash.MoveToTrashAsync(a.Id, DeletedAt);
        Assert.Equal(new[] { a.Id, b.Id, earlier.Id }, (await f.Trash.ListAsync()).Select(entry => entry.Task.Id));
    }

    [Fact]
    public async Task SameTitleAndCategory_DoNotCascadeToAnotherIdentity()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var second = first with { Id = Guid.NewGuid(), Description = "Outra descrição" };
        await f.SaveAsync(first); await f.SaveAsync(second); await f.Trash.MoveToTrashAsync(first.Id, DeletedAt);
        Assert.Equal(second, Assert.Single(await f.Actions.ListAsync(Day, Day))); Assert.Equal(first, Assert.Single(await f.Trash.ListAsync()).Task);
    }

    [Fact]
    public async Task TrashedTaskInAStaleClosingSelection_CancelsTheWholeCompletion()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var second = Example(); await f.SaveAsync(first); await f.SaveAsync(second);
        var stale = await f.Completion.ListCandidatesAsync(Day); await f.Trash.MoveToTrashAsync(second.Id, DeletedAt);
        Assert.Equal(AgendaTaskCompletionStatus.PersistenceFailed, (await f.Completion.CompleteAsync(stale, DeletedAt)).Status);
        Assert.Equal(first, await f.Actions.GetByIdAsync(first.Id)); Assert.Null(await f.Completion.GetLastCompletedAtAsync(first.Id));
        Assert.Equal(second, Assert.Single(await f.Trash.ListAsync()).Task);
    }

    [Fact]
    public async Task ADelayedSave_CannotResurrectOrOverwriteTheTrashedTask()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, DeletedAt);
        Assert.Equal(AgendaTaskSaveStatus.PersistenceFailed, (await f.Actions.SaveAsync(task with { Description = "Gravação atrasada" })).Status);
        Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Equal(task, Assert.Single(await f.Trash.ListAsync()).Task);
        await f.Trash.RestoreAsync(task.Id); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task RepeatedDeletion_IsRejectedWithoutReplacingTheOriginalTimestamp()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, DeletedAt);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Trash.MoveToTrashAsync(task.Id, DeletedAt.AddDays(1)));
        Assert.Equal(DeletedAt, Assert.Single(await f.Trash.ListAsync()).DeletedAt);
    }

    [Fact]
    public async Task RestoringAnActiveTask_IsRejectedWithoutChangingIt()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Trash.RestoreAsync(task.Id)); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingIdentity_IsRejectedWithoutInventingATask(bool restore)
    {
        using var f = await Fixture.CreateAsync(); var id = Guid.NewGuid();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => restore ? f.Trash.RestoreAsync(id) : f.Trash.MoveToTrashAsync(id, DeletedAt));
        Assert.Equal(0, await f.PhysicalCountAsync()); Assert.Empty(await f.Trash.ListAsync());
    }

    [Theory]
    [InlineData("move")]
    [InlineData("list")]
    [InlineData("restore")]
    public async Task PreCancellation_DoesNotOpenOrCreateTheDatabase(string operation)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        var trash = new SqliteAgendaTrashActions(path); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation switch
        {
            "move" => trash.MoveToTrashAsync(Guid.NewGuid(), DeletedAt, cancellation.Token),
            "restore" => trash.RestoreAsync(Guid.NewGuid(), cancellation.Token),
            _ => (Task)trash.ListAsync(cancellation.Token)
        });
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task InvalidDeletionMetadata_IsRejectedWhenListingTheTrash()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, DeletedAt);
        await f.CorruptAsync("date"); await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Trash.ListAsync());
    }

    [Fact]
    public async Task BackupCreation_ValidatesHiddenContentOutsideTheCurrentDay()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedDate = Day.AddYears(-20) }; await f.SaveAsync(task);
        await f.Trash.MoveToTrashAsync(task.Id, DeletedAt); await f.CorruptAsync("title");
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).CreateAsync(f.Archive)); Assert.False(File.Exists(f.Archive));
    }

    [Fact]
    public async Task Restore_RejectsMalformedDeletionMetadataDespiteValidHashes()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, DeletedAt); await f.CorruptAsync("date");
        var source = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, source);
        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored));
        Assert.False(Directory.Exists(f.Restored)); Assert.Equal(source, await File.ReadAllBytesAsync(f.Database));
    }

    [Fact]
    public async Task BackupAndRestore_PreserveTrashAndActualCompletionWithPackageVersionOne()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.CompleteAsync(task.Id);
        await f.Trash.MoveToTrashAsync(task.Id, DeletedAt); var backup = new LocalBackupService(f.Root.DirectoryPath); await backup.CreateAsync(f.Archive);
        using (var archive = ZipFile.OpenRead(f.Archive))
        {
            await using var stream = archive.GetEntry("manifest.json")!.Open();
            var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web)); Assert.Equal(1, manifest!.FormatVersion);
        }
        await backup.RestoreAsync(f.Archive, f.Restored);
        var path = Path.Combine(f.Restored, "trama.db"); var restoredTrash = new SqliteAgendaTrashActions(path);
        Assert.Null(await new SqliteAgendaActions(path).GetByIdAsync(task.Id));
        var entry = Assert.Single(await restoredTrash.ListAsync()); Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, entry.Task);
        Assert.Equal(DeletedAt, entry.DeletedAt); Assert.Equal(DeletedAt.Offset, entry.DeletedAt.Offset); Assert.Equal(CompletedAt, entry.LastCompletedAt);
        await restoredTrash.RestoreAsync(task.Id);
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await new SqliteAgendaActions(path).GetByIdAsync(task.Id));
        Assert.Equal(CompletedAt, await new SqliteAgendaCompletionActions(path).GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task BackupFromSchemaEight_RestoresAllTasksAsActiveWithoutChangingTheSource()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = Example() with { Status = AgendaTaskStatus.Completed }; await f.SaveAsync(task, CompletedAt);
        var source = await File.ReadAllBytesAsync(f.Database); await WriteArchiveAsync(f.Archive, source);
        await new LocalBackupService(f.Root.DirectoryPath).RestoreAsync(f.Archive, f.Restored);
        Assert.Equal(source, await File.ReadAllBytesAsync(f.Database)); var path = Path.Combine(f.Restored, "trama.db");
        Assert.Equal(task, await new SqliteAgendaActions(path).GetByIdAsync(task.Id)); Assert.Empty(await new SqliteAgendaTrashActions(path).ListAsync());
        Assert.Equal(CompletedAt, await new SqliteAgendaCompletionActions(path).GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task Migration_PreservesEightHistoricalMigrationsTasksActualTimeAndNotebookNotes()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var task = Example() with { Status = AgendaTaskStatus.Completed };
        await f.SaveAsync(task, CompletedAt); var note = new NoteSnapshot(Guid.NewGuid(), "Nota fictícia", "Conteúdo preservado");
        await using (var context = f.Root.CreateContext())
        {
            await new SqliteNoteRepository(context).AddAsync(note); var before = (await context.Database.GetAppliedMigrationsAsync()).ToArray(); Assert.Equal(8, before.Length);
            await context.Database.MigrateAsync(); var after = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.True(after.Length >= 9); Assert.Equal(before, after.Take(8)); Assert.Equal(note, await new SqliteNoteRepository(context).GetByIdAsync(note.Id));
        }
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Equal(CompletedAt, await f.Completion.GetLastCompletedAtAsync(task.Id)); Assert.Empty(await f.Trash.ListAsync());
    }

    [Fact]
    public async Task ProfilesWithTheSameTaskIdentity_AreIsolated()
    {
        using var first = await Fixture.CreateAsync(); using var second = await Fixture.CreateAsync(); var task = Example();
        await first.SaveAsync(task); await second.SaveAsync(task); await first.Trash.MoveToTrashAsync(task.Id, DeletedAt);
        Assert.Equal(task, await second.Actions.GetByIdAsync(task.Id)); Assert.Empty(await second.Trash.ListAsync()); Assert.Null(await first.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task FailedDeletion_DoesNotPartiallyHideOrChangeTheTask()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAgendaTrash BEFORE UPDATE OF DeletedAt ON AgendaTasks BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Trash.MoveToTrashAsync(task.Id, DeletedAt));
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await f.Trash.ListAsync());
    }

    [Fact]
    public async Task FailedRestoration_PreservesTheTrashEntryAndItsTimestamp()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, DeletedAt);
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAgendaRestore BEFORE UPDATE OF DeletedAt ON AgendaTasks WHEN NEW.DeletedAt IS NULL BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Trash.RestoreAsync(task.Id));
        Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Equal(DeletedAt, Assert.Single(await f.Trash.ListAsync()).DeletedAt);
    }

    [Fact]
    public async Task MissingDatabaseRead_DoesNotCreateAnEmptyDatabase()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => new SqliteAgendaTrashActions(path).ListAsync()); Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyIdentity_IsRejectedBeforeOpeningAMissingDatabase(bool restore)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db"); var trash = new SqliteAgendaTrashActions(path);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => restore ? trash.RestoreAsync(Guid.Empty) : trash.MoveToTrashAsync(Guid.Empty, DeletedAt)); Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task EmptyTrash_ReturnsAnEmptyList()
    {
        using var f = await Fixture.CreateAsync(); Assert.Empty(await f.Trash.ListAsync());
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "  Tarefa C# 💻  ", "Descrição preservada\nCódigo e estudo", Day,
        new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Work);

    private static async Task WriteArchiveAsync(string path, byte[] database)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        await using (var stream = archive.CreateEntry("trama.db").Open()) await stream.WriteAsync(database);
        var hash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant(); await using var metadata = archive.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(metadata, new BackupManifest(1, [new BackupFileEntry("trama.db", database.LongLength, hash)]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed class Fixture : IDisposable
    {
        private bool _legacy;
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public string Archive => Path.Combine(Root.DirectoryPath, "trash.trama-backup");
        public string Restored => Path.Combine(Root.DirectoryPath, "restored");
        public SqliteAgendaActions Actions => new(Database);
        public SqliteAgendaCompletionActions Completion => new(Database);
        public SqliteAgendaTrashActions Trash => new(Database);
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture { _legacy = legacy }; await using var context = f.Root.CreateContext();
            if (legacy) await context.GetService<IMigrator>().MigrateAsync(PreviousMigration); else await context.Database.MigrateAsync();
            return f;
        }
        public async Task SaveAsync(AgendaTaskSnapshot task, DateTimeOffset? actual = null)
        {
            if (_legacy)
            {
                await using var context = Root.CreateContext();
                await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgendaTasks (Id, Title, Description, PlannedDate, StartTime, EndTime, Category, Status, LastCompletedAt) VALUES ({task.Id}, {task.Title}, {task.Description}, {task.PlannedDate}, {task.StartTime}, {task.EndTime}, {(int)task.Category}, {(int)task.Status}, {actual})");
            }
            else Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        }
        public async Task CompleteAsync(Guid id)
        {
            await using var context = Root.CreateContext(); await new SqliteAgendaTaskCompletionRepository(context).CompleteAsync([id], CompletedAt);
        }
        public async Task<int> PhysicalCountAsync()
        {
            await using var context = Root.CreateContext(); await context.Database.OpenConnectionAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM AgendaTasks";
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        public async Task CorruptAsync(string fault)
        {
            await using var context = Root.CreateContext();
            if (fault == "date") await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET DeletedAt = 'not-a-timestamp'");
            else await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET Title = '   '");
        }
        public void Dispose() => Root.Dispose();
    }
}
