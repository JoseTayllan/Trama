using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveServiceTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("RetrospectiveServiceTests", TimeSpan.FromHours(-3), "Test", "Test");
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 15, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task FirstClosure_CapturesPlanningCompletionLegacyAndContinuationsWithoutTrash()
    {
        using var f = await Fixture.CreateAsync(); var pending = Example(); var progress = Example() with { Status = AgendaTaskStatus.InProgress };
        var completed = Example(); var legacy = Example() with { Status = AgendaTaskStatus.Completed }; var hidden = Example();
        var carry = Example() with { PlannedDate = Monday.AddDays(-1), PlannedEndDate = Monday.AddDays(2) };
        foreach (var task in new[] { pending, progress, completed, legacy, hidden, carry }) await f.SaveTaskAsync(task);
        var actual = Now.AddDays(-2); await f.Completion.CompleteAsync([completed], actual); await f.Trash.MoveToTrashAsync(hidden.Id, Now);
        var report = await f.Service.GetOrCloseAsync(Monday);
        var expected = new AgendaRetrospectiveBuilder().Build(Monday,
            [new(pending, null), new(progress, null), new(completed with { Status = AgendaTaskStatus.Completed }, actual), new(legacy, null), new(carry, null)], f.Clock.GetLocalNow(), Zone);
        Equal(expected, report); Equal(report, await f.StoredAsync(Monday)); Assert.Equal(5, report.PlannedCount); Assert.Equal(1, report.CarriedInCount);
    }

    [Fact]
    public async Task ExistingCapture_DoesNotReadClockOrReloadEditedLiveTasks()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveTaskAsync(task); var original = await f.Service.GetOrCloseAsync(Monday);
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET Title = ' '");
        f.Clock.ThrowOnRead = true; Equal(original, await f.Service.GetOrCloseAsync(Monday.AddDays(5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    public async Task ReferenceDays_ResolveToOnePreservedWeeklyCapture(int offset)
    {
        using var f = await Fixture.CreateAsync(); var report = await f.Service.GetOrCloseAsync(Monday.AddDays(offset));
        Assert.Equal(Monday, report.StartDate); Assert.Equal(Monday.AddDays(6), report.EndDate); Assert.Single(await f.Service.ListAsync(Monday, Monday));
        f.Clock.UtcNow = Now.AddDays(1); Equal(report, await f.Service.GetOrCloseAsync(Monday));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task CurrentWeek_IsRejectedAccordingToLocalDateEvenWhenUtcIsAlreadyMonday(int utcHour)
    {
        using var f = await Fixture.CreateAsync(); f.Clock.UtcNow = new DateTimeOffset(2026, 11, 9, utcHour, 0, 0, TimeSpan.Zero);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service.GetOrCloseAsync(Monday)); Assert.Null(await f.StoredAsync(Monday));
    }

    [Fact]
    public async Task FirstLocalInstantAfterSunday_AllowsClosureWithExplicitZoneAndOffset()
    {
        using var f = await Fixture.CreateAsync(); f.Clock.UtcNow = new DateTimeOffset(2026, 11, 9, 3, 0, 0, TimeSpan.Zero);
        var report = await f.Service.GetOrCloseAsync(Monday); Assert.Equal(Zone.Id, report.TimeZoneId);
        Assert.Equal(TimeSpan.FromHours(-3), report.ClosedAt.Offset); Assert.Equal(Monday.AddDays(7), DateOnly.FromDateTime(report.ClosedAt.DateTime));
    }

    [Fact]
    public async Task EmptyClosedWeek_IsStoredWithoutPercentageOrInventedTasks()
    {
        using var f = await Fixture.CreateAsync(); var report = await f.Service.GetOrCloseAsync(Monday);
        Assert.Equal(0, report.PlannedCount); Assert.Null(report.CompletionPercentage); Assert.Empty(report.Tasks); Assert.Equal(7, report.Days.Count);
        Equal(report, await f.Service.GetAsync(Monday)); Assert.Single(await f.Service.ListAsync(Monday, Monday));
    }

    [Fact]
    public async Task Closing_DoesNotChangeTaskStatusPlanningOrCompletionMetadata()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Monday.AddDays(3) }; await f.SaveTaskAsync(task);
        await f.Service.GetOrCloseAsync(Monday); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Null(await f.Completion.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task SameService_ReopensIndependentOperationsAndReadsNewHistory()
    {
        using var f = await Fixture.CreateAsync(); Assert.Null(await f.Service.GetAsync(Monday)); var report = await f.Service.GetOrCloseAsync(Monday);
        var other = Create(f.Database, f.Clock); Equal(report, await other.GetAsync(Monday));
        await other.GetOrCloseAsync(Monday.AddDays(-7)); Assert.Equal(2, (await f.Service.ListAsync(Monday.AddDays(-7), Monday)).Count);
    }

    [Fact]
    public async Task SameWeekInDifferentProfiles_RemainsIsolated()
    {
        using var first = await Fixture.CreateAsync(); using var second = await Fixture.CreateAsync(); var task = Example(); await first.SaveTaskAsync(task);
        var a = await first.Service.GetOrCloseAsync(Monday); var b = await second.Service.GetOrCloseAsync(Monday);
        Assert.Equal(1, a.PlannedCount); Assert.Equal(0, b.PlannedCount); Equal(a, await first.Service.GetAsync(Monday)); Equal(b, await second.Service.GetAsync(Monday));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("relative.db")]
    public void Constructor_RejectsInvalidDatabasePaths(string? path)
    {
        var constructor = Constructor(); Assert.ThrowsAny<ArgumentException>(() => Construct(constructor, path, new Clock()));
    }

    [Fact]
    public void Constructor_RejectsNullClock()
    {
        var constructor = Constructor(); using var root = new NotebookTestWorkspace(); Assert.Throws<ArgumentNullException>(() => Construct(constructor, Path.Combine(root.DirectoryPath, "trama.db"), null));
    }

    [Theory]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("close")]
    public async Task AbsentProfile_IsReportedWithoutCreatingAnEmptyDatabase(string operation)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "absent.db"); var service = Create(path, new Clock());
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => Operate(service, operation)); Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("close")]
    public async Task PreCancellation_DoesNotOpenTheAbsentProfileOrReadClock(string operation)
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "absent.db"); var service = Create(path, new Clock { ThrowOnRead = true });
        using var cancel = new CancellationTokenSource(); cancel.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Operate(service, operation, cancel.Token)); Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task InvalidCalendarArguments_AreRejectedBeforeOpeningAnAbsentDatabase()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "absent.db"); var service = Create(path, new Clock { ThrowOnRead = true });
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetOrCloseAsync(DateOnly.MaxValue));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(Monday.AddDays(1)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ListAsync(Monday.AddDays(1), Monday)); Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task History_IsInclusiveNewestFirstAndDoesNotCreateUnrequestedWeeks()
    {
        using var f = await Fixture.CreateAsync(); var prior = await f.Service.GetOrCloseAsync(Monday.AddDays(-7)); var current = await f.Service.GetOrCloseAsync(Monday);
        var list = await f.Service.ListAsync(prior.StartDate, current.StartDate); Assert.Equal(new[] { current.StartDate, prior.StartDate }, list.Select(r => r.StartDate));
        Assert.Single(await f.Service.ListAsync(Monday, Monday)); Assert.Null(await f.Service.GetAsync(Monday.AddDays(-14)));
    }

    [Fact]
    public async Task InvalidLiveData_DoesNotSaveAPartialReportAndRetryRecovers()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveTaskAsync(task);
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("UPDATE AgendaTasks SET Title = ' '");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Service.GetOrCloseAsync(Monday)); Assert.Null(await f.StoredAsync(Monday));
        await f.SaveTaskAsync(task); Assert.Equal(task, Assert.Single((await f.Service.GetOrCloseAsync(Monday)).Tasks).Task);
    }

    [Fact]
    public async Task CompletionBeyondCapture_IsReportedAsInvalidStoredDataWithoutSaving()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveTaskAsync(task); await f.Completion.CompleteAsync([task], Now.AddHours(1));
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Service.GetOrCloseAsync(Monday)); Assert.Null(await f.StoredAsync(Monday));
    }

    [Fact]
    public async Task FailedInsert_RollsBackAndTheSameServiceCanRetry()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveTaskAsync(Example());
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailServiceClosure BEFORE INSERT ON AgendaWeekRetrospectives BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Service.GetOrCloseAsync(Monday)); Assert.Null(await f.StoredAsync(Monday));
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("DROP TRIGGER FailServiceClosure");
        Assert.Equal(1, (await f.Service.GetOrCloseAsync(Monday)).PlannedCount);
    }

    [Fact]
    public async Task CancellationAtInsert_RollsBackAndDoesNotPoisonTheNextOperation()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveTaskAsync(Example()); using var cancel = new CancellationTokenSource();
        using (var probe = new InsertProbe(f.Database, cancel.Cancel))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Service.GetOrCloseAsync(Monday, cancel.Token));
        Assert.Null(await f.StoredAsync(Monday)); Assert.Equal(1, (await f.Service.GetOrCloseAsync(Monday)).PlannedCount);
    }

    [Fact]
    public async Task ConcurrentClosuresOfTheSameWeek_ReturnTheSameWinningSnapshot()
    {
        using var f = await Fixture.CreateAsync(); await f.SaveTaskAsync(Example()); var other = Create(f.Database, new Clock { UtcNow = Now.AddHours(1) });
        var reports = await Task.WhenAll(Task.Run(() => f.Service.GetOrCloseAsync(Monday)), Task.Run(() => other.GetOrCloseAsync(Monday.AddDays(3))));
        Equal(reports[0], reports[1]); Equal(reports[0], await f.StoredAsync(Monday)); Assert.Single(await f.Service.ListAsync(Monday, Monday));
    }

    [Fact]
    public async Task ConcurrentClosuresOfDifferentWeeks_KeepBothReports()
    {
        using var f = await Fixture.CreateAsync(); var other = Create(f.Database, new Clock());
        await Task.WhenAll(Task.Run(() => f.Service.GetOrCloseAsync(Monday)), Task.Run(() => other.GetOrCloseAsync(Monday.AddDays(-7))));
        Assert.Equal(new[] { Monday, Monday.AddDays(-7) }, (await f.Service.ListAsync(Monday.AddDays(-7), Monday)).Select(r => r.StartDate));
    }

    [Fact]
    public async Task CaptureAndInsert_HoldOneTransactionAgainstConcurrentTaskEdits()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveTaskAsync(task);
        using var release = new ManualResetEventSlim(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var probe = new InsertProbe(f.Database, () => { entered.TrySetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(15)), "A captura não foi liberada."); });
        var capture = Task.Run(() => f.Service.GetOrCloseAsync(Monday)); Task? writer = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)); var writerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            writer = Task.Run(async () => { writerStarted.SetResult(); await f.SaveTaskAsync(task with { Title = "Editada depois" }); });
            await writerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)); await Task.Delay(200);
            Assert.False(writer.IsCompleted, "A edição concorrente não deve atravessar a transação de captura antes da inserção.");
            release.Set(); var report = await capture.WaitAsync(TimeSpan.FromSeconds(10)); await writer.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(task, Assert.Single(report.Tasks).Task); Assert.Equal("Editada depois", (await f.Actions.GetByIdAsync(task.Id))!.Title); Equal(report, await f.StoredAsync(Monday));
        }
        finally
        {
            release.Set(); await capture.WaitAsync(TimeSpan.FromSeconds(15)); if (writer is not null) await writer.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    [Fact]
    public async Task CorruptExistingCapture_IsNotReplacedAndReadsRecoverAfterRepair()
    {
        using var f = await Fixture.CreateAsync(); var original = await f.Service.GetOrCloseAsync(Monday);
        var content = await f.ReadContentAsync(); await f.SetContentAsync("{invalid");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Service.GetOrCloseAsync(Monday));
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => f.Service.GetAsync(Monday));
        await f.SetContentAsync(content); Equal(original, await f.Service.GetOrCloseAsync(Monday)); Assert.Single(await f.Service.ListAsync(Monday, Monday));
    }

    [Fact]
    public async Task Composition_ResolvesTheServiceForTheSameProfile()
    {
        using var f = await Fixture.CreateAsync(createService: false); var report = new AgendaRetrospectiveBuilder().Build(Monday, [], Now, Zone);
        await using (var context = f.Root.CreateContext()) Assert.True(await new SqliteAgendaRetrospectiveRepository(context).TryAddAsync(report, Zone));
        using var services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath); var service = services.GetService<IAgendaRetrospectiveActions>(); Assert.NotNull(service);
        Equal(report, await service.GetOrCloseAsync(Monday)); Equal(report, await service.GetAsync(Monday));
    }

    [Fact]
    public async Task ExistingDailyActions_RemainAvailableBeforeServiceIntegration()
    {
        using var f = await Fixture.CreateAsync(createService: false); var task = Example(); await f.SaveTaskAsync(task);
        Assert.Equal(task, Assert.Single(await f.Actions.ListAsync(Monday, Monday)));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Conteúdo preservado", Monday, new TimeOnly(9, 0), new TimeOnly(10, 0), AgendaTaskCategory.Work);
    private static void Equal(AgendaWeekRetrospective expected, AgendaWeekRetrospective? actual) { Assert.NotNull(actual); Assert.Equal(JsonSerializer.Serialize(expected, Json), JsonSerializer.Serialize(actual, Json)); }
    private static async Task Operate(IAgendaRetrospectiveActions service, string operation, CancellationToken cancellationToken = default)
    {
        if (operation == "get") await service.GetAsync(Monday, cancellationToken);
        else if (operation == "list") await service.ListAsync(Monday, Monday, cancellationToken);
        else await service.GetOrCloseAsync(Monday, cancellationToken);
    }
    private static ConstructorInfo Constructor()
    {
        var type = typeof(SqliteAgendaActions).Assembly.GetType("Trama.Infrastructure.Persistence.SqliteAgendaRetrospectiveActions"); Assert.NotNull(type);
        var constructor = type.GetConstructor([typeof(string), typeof(TimeProvider)]); Assert.NotNull(constructor); return constructor;
    }
    private static IAgendaRetrospectiveActions Create(string path, TimeProvider clock) => Construct(Constructor(), path, clock);
    private static IAgendaRetrospectiveActions Construct(ConstructorInfo constructor, string? path, TimeProvider? clock)
    {
        try { return Assert.IsAssignableFrom<IAgendaRetrospectiveActions>(constructor.Invoke([path, clock])); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null) { ExceptionDispatchInfo.Capture(exception.InnerException).Throw(); throw; }
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = Now;
        public bool ThrowOnRead { get; set; }
        public override DateTimeOffset GetUtcNow() => ThrowOnRead ? throw new InvalidOperationException("Clock must not be read.") : UtcNow;
        public override TimeZoneInfo LocalTimeZone => ThrowOnRead ? throw new InvalidOperationException("Clock must not be read.") : Zone;
    }
    private sealed class InsertProbe : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly string _database;
        private readonly Action _onInsert;
        private readonly IDisposable _all;
        private readonly List<IDisposable> _subscriptions = [];
        private int _seen;
        public InsertProbe(string database, Action onInsert) { _database = Path.GetFullPath(database); _onInsert = onInsert; _all = DiagnosticListener.AllListeners.Subscribe(this); }
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore") lock (_subscriptions) _subscriptions.Add(listener.Subscribe(this));
        }
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (!value.Key.EndsWith(".CommandExecuting", StringComparison.Ordinal) || value.Value is not CommandEventData data ||
                data.Command.Connection is not { } connection || !string.Equals(Path.GetFullPath(connection.DataSource), _database, StringComparison.OrdinalIgnoreCase) ||
                !data.Command.CommandText.Replace("\"", string.Empty, StringComparison.Ordinal).TrimStart().StartsWith("INSERT INTO AgendaWeekRetrospectives", StringComparison.OrdinalIgnoreCase)) return;
            if (Interlocked.Exchange(ref _seen, 1) == 0) _onInsert();
        }
        public void OnError(Exception error) { }
        public void OnCompleted() { }
        public void Dispose() { _all.Dispose(); lock (_subscriptions) foreach (var subscription in _subscriptions) subscription.Dispose(); }
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Database => Path.Combine(Root.DirectoryPath, "trama.db");
        public Clock Clock { get; } = new();
        public IAgendaRetrospectiveActions Service { get; private set; } = null!;
        public SqliteAgendaActions Actions => new(Database);
        public SqliteAgendaCompletionActions Completion => new(Database);
        public SqliteAgendaTrashActions Trash => new(Database);
        public static async Task<Fixture> CreateAsync(bool createService = true)
        {
            var f = new Fixture();
            try { await using var context = f.Root.CreateContext(); await context.Database.MigrateAsync(); if (createService) f.Service = Create(f.Database, f.Clock); return f; }
            catch { f.Dispose(); throw; }
        }
        public async Task SaveTaskAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        public async Task<AgendaWeekRetrospective?> StoredAsync(DateOnly start) { await using var context = Root.CreateContext(); return await new SqliteAgendaRetrospectiveRepository(context).GetAsync(start); }
        public async Task<string> ReadContentAsync()
        {
            await using var context = Root.CreateContext(); await context.Database.OpenConnectionAsync(); await using var command = context.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT Content FROM AgendaWeekRetrospectives";
            return Assert.IsType<string>(await command.ExecuteScalarAsync());
        }
        public async Task SetContentAsync(string content) { await using var context = Root.CreateContext(); await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE AgendaWeekRetrospectives SET Content = {content}"); }
        public void Dispose() => Root.Dispose();
    }
}
