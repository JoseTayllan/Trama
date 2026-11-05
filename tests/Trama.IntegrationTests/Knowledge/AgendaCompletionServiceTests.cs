using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaCompletionServiceTests
{
    private static readonly DateOnly Day = new(2026, 11, 4);
    private static readonly DateTimeOffset ClosedAt = new(2026, 11, 4, 20, 0, 0, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative.db")]
    public void InvalidDatabasePath_IsRejected(string? path)
        => Assert.ThrowsAny<ArgumentException>(() => new SqliteAgendaCompletionActions(path!));

    [Fact]
    public void Construction_DoesNotCreateAProfileOrDatabase()
    {
        using var root = new NotebookTestWorkspace(); var directory = Path.Combine(root.DirectoryPath, "not-created");
        _ = new SqliteAgendaCompletionActions(Path.Combine(directory, "trama.db"));
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task Candidates_IncludeOpenTasksFromEarlierDaysAndExcludeFutureAndCompletedTasks()
    {
        using var f = await Fixture.CreateAsync();
        var earlier = Example() with { PlannedDate = new DateOnly(2000, 1, 1) };
        var today = Example() with { Status = AgendaTaskStatus.InProgress };
        foreach (var task in new[] { earlier, today, Example() with { PlannedDate = Day.AddDays(1) }, Example() with { Status = AgendaTaskStatus.Completed } }) await f.SaveAsync(task);
        var candidates = await f.Actions.ListCandidatesAsync(Day);
        Assert.Equal(new[] { earlier, today }, candidates);
        Assert.Equal(4, (await new SqliteAgendaActions(f.Database).ListAsync(new DateOnly(2000, 1, 1), Day.AddDays(1))).Count);
    }

    [Fact]
    public async Task Candidates_KeepTheExistingDayTimeUntimedTitleAndIdentityOrdering()
    {
        using var f = await Fixture.CreateAsync();
        var yesterday = Example() with { PlannedDate = Day.AddDays(-1) };
        var morning = Example() with { StartTime = new TimeOnly(8, 0) };
        var a = Example() with { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "A" };
        var b = a with { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") };
        var z = Example() with { Title = "Z" };
        foreach (var task in new[] { z, b, morning, yesterday, a }) await f.SaveAsync(task);
        Assert.Equal(new[] { yesterday, morning, a, b, z }, await f.Actions.ListCandidatesAsync(Day));
    }

    [Fact]
    public async Task ExternalCompletionAndReopening_AreVisibleWithoutRebuildingTheService()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await f.Actions.ListCandidatesAsync(Day)));
        await using (var context = f.Root.CreateContext()) await new SqliteAgendaTaskCompletionRepository(context).CompleteAsync([task.Id], ClosedAt);
        Assert.Empty(await f.Actions.ListCandidatesAsync(Day));
        Assert.Equal(ClosedAt, await f.Actions.GetLastCompletedAtAsync(task.Id));
        await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await f.Actions.ListCandidatesAsync(Day)));
        Assert.Equal(ClosedAt, await f.Actions.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task CompletingTheSelection_PreservesTheOtherTasksAndThePlanning()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var second = Example();
        await f.SaveAsync(first); await f.SaveAsync(second);
        var result = await f.Actions.CompleteAsync([first], ClosedAt);
        Assert.Equal(AgendaTaskCompletionStatus.Completed, result.Status);
        Assert.Equal(first with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(first.Id));
        Assert.Equal(second, await f.ReadAsync(second.Id));
        var reopened = new SqliteAgendaCompletionActions(f.Database);
        var actual = await reopened.GetLastCompletedAtAsync(first.Id);
        Assert.Equal(ClosedAt, actual); Assert.Equal(ClosedAt.Offset, actual!.Value.Offset);
        Assert.Null(await reopened.GetLastCompletedAtAsync(second.Id));
    }

    [Fact]
    public async Task ProfilesWithTheSameIdentity_AreIsolated()
    {
        using var first = await Fixture.CreateAsync(); using var second = await Fixture.CreateAsync();
        var task = Example(); await first.SaveAsync(task); await second.SaveAsync(task);
        Assert.Equal(AgendaTaskCompletionStatus.Completed, (await first.Actions.CompleteAsync([task], ClosedAt)).Status);
        Assert.Equal(task, await second.ReadAsync(task.Id));
        Assert.Equal(task, Assert.Single(await second.Actions.ListCandidatesAsync(Day)));
        Assert.Null(await second.Actions.GetLastCompletedAtAsync(task.Id));
    }

    [Theory]
    [InlineData("candidates")]
    [InlineData("complete")]
    [InlineData("read")]
    public async Task PreCancellation_DoesNotOpenOrCreateTheDatabase(string operation)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        var actions = new SqliteAgendaCompletionActions(path); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation switch
        {
            "candidates" => (Task)actions.ListCandidatesAsync(Day, cancellation.Token),
            "complete" => actions.CompleteAsync([Example()], ClosedAt, cancellation.Token),
            _ => actions.GetLastCompletedAtAsync(Guid.NewGuid(), cancellation.Token)
        });
        Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("duplicates")]
    [InlineData("future")]
    [InlineData("completed")]
    public async Task InvalidSelection_IsRejectedBeforeOpeningAMissingDatabase(string scenario)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        var task = Example();
        AgendaTaskSnapshot[] selection = scenario switch
        {
            "empty" => [],
            "duplicates" => [task, task],
            "future" => [task with { PlannedDate = Day.AddDays(1) }],
            _ => [task with { Status = AgendaTaskStatus.Completed }]
        };
        Assert.Equal(AgendaTaskCompletionStatus.ValidationFailed, (await new SqliteAgendaCompletionActions(path).CompleteAsync(selection, ClosedAt)).Status);
        Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingDatabaseReads_ReportFailureWithoutCreatingAnEmptyFile(bool candidates)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db"); var actions = new SqliteAgendaCompletionActions(path);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => candidates ? (Task)actions.ListCandidatesAsync(Day) : actions.GetLastCompletedAtAsync(Guid.NewGuid()));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task FailureThenRetry_DoesNotLeakFailedChangesAcrossOperations()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        await using (var context = f.Root.CreateContext())
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailCompletionService BEFORE UPDATE OF Status ON AgendaTasks BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        Assert.Equal(AgendaTaskCompletionStatus.PersistenceFailed, (await f.Actions.CompleteAsync([task], ClosedAt)).Status);
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.Actions.GetLastCompletedAtAsync(task.Id));
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("DROP TRIGGER FailCompletionService");
        Assert.Equal(AgendaTaskCompletionStatus.Completed, (await f.Actions.CompleteAsync([task], ClosedAt)).Status);
        Assert.Equal(ClosedAt, await f.Actions.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task StaleSelection_IsRevalidatedAndDoesNotPartiallyComplete()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var changed = Example();
        await f.SaveAsync(first); await f.SaveAsync(changed);
        var stale = await f.Actions.ListCandidatesAsync(Day);
        changed = changed with { PlannedDate = Day.AddDays(2) }; await f.SaveAsync(changed);
        Assert.Equal(AgendaTaskCompletionStatus.PersistenceFailed, (await f.Actions.CompleteAsync(stale, ClosedAt)).Status);
        Assert.Equal(first, await f.ReadAsync(first.Id)); Assert.Equal(changed, await f.ReadAsync(changed.Id));
        Assert.Null(await f.Actions.GetLastCompletedAtAsync(first.Id));
    }

    [Fact]
    public async Task ConcurrentCallsForDistinctTasks_UseIndependentOperations()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var second = Example();
        await f.SaveAsync(first); await f.SaveAsync(second);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var one = Task.Run(async () => { await start.Task; return await f.Actions.CompleteAsync([first], ClosedAt); });
        var two = Task.Run(async () => { await start.Task; return await f.Actions.CompleteAsync([second], ClosedAt); });
        start.SetResult();
        var results = await Task.WhenAll(one, two);
        Assert.All(results, result => Assert.Equal(AgendaTaskCompletionStatus.Completed, result.Status));
        Assert.Empty(await f.Actions.ListCandidatesAsync(Day));
        Assert.Equal(ClosedAt, await f.Actions.GetLastCompletedAtAsync(first.Id)); Assert.Equal(ClosedAt, await f.Actions.GetLastCompletedAtAsync(second.Id));
    }

    [Fact]
    public async Task DesktopComposition_ProvidesTheServiceForTheSameLocalProfile()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var actions = services.GetService<IAgendaCompletionActions>();
        Assert.True(actions is not null, "A composição ainda não fornece o serviço de encerramento.");
        var task = Example(); Assert.Equal(AgendaTaskSaveStatus.Saved, (await services.GetRequiredService<IAgendaActions>().SaveAsync(task)).Status);
        Assert.Equal(task, Assert.Single(await actions!.ListCandidatesAsync(Day)));
        Assert.Equal(AgendaTaskCompletionStatus.Completed, (await actions.CompleteAsync([task], ClosedAt)).Status);
        await using var context = root.CreateContext();
        Assert.Equal(ClosedAt, await new SqliteAgendaTaskCompletionRepository(context).GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task MissingSelection_IsRejectedBeforeOpeningAMissingDatabase()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db");
        await Assert.ThrowsAsync<ArgumentNullException>(() => new SqliteAgendaCompletionActions(path).CompleteAsync(null!, ClosedAt));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task LegacyCompletionAndMissingTask_DoNotInventAnActualTimestamp()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { Status = AgendaTaskStatus.Completed }; await f.SaveAsync(task);
        Assert.Null(await f.Actions.GetLastCompletedAtAsync(task.Id));
        Assert.Null(await f.Actions.GetLastCompletedAtAsync(Guid.NewGuid()));
        Assert.Equal(task, await f.ReadAsync(task.Id));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Dados de teste", Day);

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public SqliteAgendaCompletionActions Actions { get; private set; } = null!;
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture(); await LocalDatabase.InitializeAsync(f.Root.DirectoryPath); f.Actions = new SqliteAgendaCompletionActions(f.Database); return f;
        }
        public async Task SaveAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SqliteAgendaActions(Database).SaveAsync(task)).Status);
        public Task<AgendaTaskSnapshot?> ReadAsync(Guid id) => new SqliteAgendaActions(Database).GetByIdAsync(id);
        public void Dispose() => Root.Dispose();
    }
}
