using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaServiceTests
{
    private static readonly DateOnly Day = new(2026, 11, 3);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative.db")]
    public void InvalidDatabasePath_IsRejected(string? path)
        => Assert.ThrowsAny<ArgumentException>(() => new SqliteAgendaActions(path!));

    [Fact]
    public void Construction_DoesNotCreateAProfileOrDatabase()
    {
        using var root = new NotebookTestWorkspace();
        var directory = Path.Combine(root.DirectoryPath, "not-created");
        _ = new SqliteAgendaActions(Path.Combine(directory, "trama.db"));
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task SavedTask_CanBeReadAndListedByANewServiceInstance()
    {
        using var root = new NotebookTestWorkspace();
        var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var task = Example();
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SqliteAgendaActions(path).SaveAsync(task)).Status);
        var reopened = new SqliteAgendaActions(path);
        Assert.Equal(task, await reopened.GetByIdAsync(task.Id));
        Assert.Equal(task, Assert.Single(await reopened.ListAsync(Day, Day)));
    }

    [Fact]
    public async Task InvalidTask_IsRejectedBeforeOpeningAMissingDatabase()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        var result = await new SqliteAgendaActions(path).SaveAsync(Example() with { Title = "   " });
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, result.Status);
        Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("save")]
    [InlineData("get")]
    [InlineData("list")]
    public async Task PreCancellation_DoesNotOpenOrCreateTheDatabase(string operation)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        var service = new SqliteAgendaActions(path); using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (operation == "save") await service.SaveAsync(Example(), source.Token);
            else if (operation == "get") await service.GetByIdAsync(Guid.NewGuid(), source.Token);
            else await service.ListAsync(Day, Day, source.Token);
        });
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task FailedWrite_DoesNotLeakAnUncommittedTaskIntoTheNextOperation()
    {
        using var root = new NotebookTestWorkspace();
        var service = new SqliteAgendaActions(await LocalDatabase.InitializeAsync(root.DirectoryPath));
        await using (var context = root.CreateContext())
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAgendaInsert BEFORE INSERT ON AgendaTasks BEGIN SELECT RAISE(ABORT, 'Synthetic failure'); END;");
        var failed = Example();
        Assert.Equal(AgendaTaskSaveStatus.PersistenceFailed, (await service.SaveAsync(failed)).Status);
        await using (var context = root.CreateContext())
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER FailAgendaInsert");
        var successful = Example() with { Title = "Gravação posterior" };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await service.SaveAsync(successful)).Status);
        Assert.Null(await service.GetByIdAsync(failed.Id));
        Assert.Equal(successful, Assert.Single(await service.ListAsync(Day, Day)));
    }

    [Fact]
    public async Task ConcurrentCalls_KeepIndependentTasksWithoutSharingATrackedContext()
    {
        using var root = new NotebookTestWorkspace();
        var service = new SqliteAgendaActions(await LocalDatabase.InitializeAsync(root.DirectoryPath));
        var tasks = Enumerable.Range(1, 6).Select(i => Example() with { Title = $"Tarefa {i}" }).ToArray();
        var results = await Task.WhenAll(tasks.Select(task => Task.Run(() => service.SaveAsync(task))));
        Assert.All(results, result => Assert.Equal(AgendaTaskSaveStatus.Saved, result.Status));
        var stored = await service.ListAsync(Day, Day);
        Assert.Equal(tasks.OrderBy(task => task.Title).ToArray(), stored);
    }

    [Fact]
    public async Task DifferentProfiles_DoNotShareTasks()
    {
        using var firstRoot = new NotebookTestWorkspace(); using var secondRoot = new NotebookTestWorkspace();
        var first = new SqliteAgendaActions(await LocalDatabase.InitializeAsync(firstRoot.DirectoryPath));
        var second = new SqliteAgendaActions(await LocalDatabase.InitializeAsync(secondRoot.DirectoryPath));
        var task = Example();
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await first.SaveAsync(task)).Status);
        Assert.Null(await second.GetByIdAsync(task.Id)); Assert.Empty(await second.ListAsync(Day, Day));
        Assert.Equal(task, await first.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task RepeatedCalls_SeeUpdatesFromAnotherService()
    {
        using var root = new NotebookTestWorkspace(); var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var first = new SqliteAgendaActions(path); var other = new SqliteAgendaActions(path); var task = Example();
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await first.SaveAsync(task)).Status);
        Assert.Equal(task, await first.GetByIdAsync(task.Id));
        var changed = task with { Status = AgendaTaskStatus.Completed };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await other.SaveAsync(changed)).Status);
        Assert.Equal(changed, await first.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task ReadingAMissingDatabase_ReportsFailureWithoutCreatingAnEmptyFile()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => new SqliteAgendaActions(path).GetByIdAsync(Guid.NewGuid()));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task DesktopComposition_ProvidesTheAgendaFacadeForTheSameLocalProfile()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var actions = services.GetService<IAgendaActions>();
        Assert.True(actions is not null, "A composição ainda não fornece IAgendaActions para a futura tela.");
        var task = Example();
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions!.SaveAsync(task)).Status);
        await using var context = root.CreateContext();
        Assert.Equal(task, await new SqliteAgendaTaskRepository(context).GetByIdAsync(task.Id));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Estudar interfaces", "Conteúdo fictício", Day,
        new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Study);
}
