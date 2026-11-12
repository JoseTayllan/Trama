using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveNavigationTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("RetrospectiveNavigationTests", TimeSpan.FromHours(-3), "Test", "Test");

    [Fact]
    public void InitialState_DoesNotQueryOrCreateAnyClosure()
    {
        var f = new Fixture(); var vm = f.Create();
        Assert.Null(Get<AgendaWeekRetrospective?>(vm, "Report")); Assert.Null(Get<AgendaRetrospectiveSummaryViewModel?>(vm, "Summary"));
        Assert.False(Get<bool>(vm, "IsBusy")); Assert.False(Get<bool>(vm, "HasError")); Assert.Empty(Get<string>(vm, "Feedback"));
        Assert.Empty(f.Actions.Closed); Assert.Empty(f.Actions.Read); Assert.Equal(0, f.Comparer.Calls);
    }

    [Fact]
    public async Task Open_LoadsCurrentClosureAndReadsOnlyTheImmediatelyPreviousWeek()
    {
        var f = new Fixture(); var vm = f.Create(); Assert.True(await Open(vm, Monday));
        Assert.Equal(Monday, Assert.Single(f.Actions.Closed)); Assert.Equal(Monday.AddDays(-7), Assert.Single(f.Actions.Read));
        Assert.Same(f.Actions.Current, Get<AgendaWeekRetrospective>(vm, "Report"));
        Assert.Equal(1, f.Comparer.Calls); Assert.Same(f.Actions.Current, f.Comparer.Current); Assert.Same(f.Actions.Previous, f.Comparer.Previous);
        Assert.Equal("50%", Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary").CompletionLabel);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public async Task ReferenceDay_IsNormalizedToMonday(int offset)
    {
        var f = new Fixture(); var vm = f.Create(); Assert.True(await Open(vm, Monday.AddDays(offset)));
        Assert.Equal(Monday, Assert.Single(f.Actions.Closed)); Assert.Equal(Monday.AddDays(-7), Assert.Single(f.Actions.Read));
    }

    [Fact]
    public async Task MissingPreviousClosure_IsPassedAsMissingWithoutCreatingIt()
    {
        var f = new Fixture(); f.Actions.Previous = null; var vm = f.Create(); Assert.True(await Open(vm, Monday));
        Assert.Single(f.Actions.Closed); Assert.Null(f.Comparer.Previous);
        Assert.Equal("Sem fechamento da semana anterior", Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary").ComparisonLabel);
        Assert.False(Get<bool>(vm, "HasError"));
    }

    [Fact]
    public async Task EmptySavedClosure_IsAValidResult()
    {
        var f = new Fixture(); f.Actions.Current = Report(Monday, false); var vm = f.Create();
        Assert.True(await Open(vm, Monday)); Assert.Equal("Sem tarefas", Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary").CompletionLabel);
        Assert.False(Get<bool>(vm, "HasError"));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(14)]
    public async Task CurrentOrFutureWeek_IsRejectedBeforeAnyStorageCall(int offset)
    {
        var f = new Fixture(); var vm = f.Create(); Assert.False(await Open(vm, Monday.AddDays(offset)));
        Assert.Empty(f.Actions.Closed); Assert.Empty(f.Actions.Read); Assert.NotEmpty(Get<string>(vm, "Feedback"));
        Assert.Null(Get<AgendaWeekRetrospective?>(vm, "Report")); Assert.False(Get<bool>(vm, "IsBusy"));
    }

    [Fact]
    public async Task ClosureEligibility_UsesInjectedLocalDateRatherThanUtcDate()
    {
        var f = new Fixture(); f.Clock.UtcNow = new(2026, 11, 9, 1, 0, 0, TimeSpan.Zero); var vm = f.Create();
        Assert.False(await Open(vm, Monday)); Assert.Empty(f.Actions.Closed);
        f.Clock.UtcNow = new(2026, 11, 9, 3, 0, 0, TimeSpan.Zero);
        Assert.True(await Open(vm, Monday)); Assert.Single(f.Actions.Closed);
    }

    [Fact]
    public async Task FirstRepresentableWeek_SkipsUnrepresentablePreviousWeek()
    {
        var f = new Fixture(); f.Actions.Current = Report(DateOnly.MinValue, false); var vm = f.Create();
        Assert.True(await Open(vm, DateOnly.MinValue)); Assert.Empty(f.Actions.Read); Assert.Null(f.Comparer.Previous);
        Assert.Equal(DateOnly.MinValue, Assert.Single(f.Actions.Closed));
    }

    [Fact]
    public async Task FirstLoadFailure_ShowsErrorAndAllowsRetry()
    {
        var f = new Fixture(); f.Actions.FailClose = true; var vm = f.Create(); Assert.False(await Open(vm, Monday));
        Assert.Null(Get<AgendaWeekRetrospective?>(vm, "Report")); Assert.Null(Get<AgendaRetrospectiveSummaryViewModel?>(vm, "Summary"));
        Assert.True(Get<bool>(vm, "HasError")); Assert.False(Get<bool>(vm, "IsBusy")); Assert.Empty(f.Actions.Read);
        f.Actions.FailClose = false; Assert.True(await Open(vm, Monday)); Assert.False(Get<bool>(vm, "HasError"));
    }

    [Fact]
    public async Task PreviousReadFailure_IsNotPresentedAsMissingHistory()
    {
        var f = new Fixture(); f.Actions.FailRead = true; var vm = f.Create(); Assert.False(await Open(vm, Monday));
        Assert.True(Get<bool>(vm, "HasError")); Assert.Null(Get<AgendaRetrospectiveSummaryViewModel?>(vm, "Summary")); Assert.Equal(0, f.Comparer.Calls);
        f.Actions.FailRead = false; Assert.True(await Open(vm, Monday));
    }

    [Fact]
    public async Task FailedNavigation_KeepsBothPreviouslyPublishedObjects()
    {
        var f = new Fixture(); var vm = f.Create(); Assert.True(await Open(vm, Monday));
        var report = Get<AgendaWeekRetrospective>(vm, "Report"); var summary = Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary");
        f.Actions.Current = Report(Monday.AddDays(-7)); f.Actions.FailRead = true;
        Assert.False(await Open(vm, Monday.AddDays(-7))); Assert.Same(report, Get<AgendaWeekRetrospective>(vm, "Report"));
        Assert.Same(summary, Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary")); Assert.True(Get<bool>(vm, "HasError"));
    }

    [Fact]
    public async Task SuccessfulNavigation_ReplacesReportAndSummaryTogether()
    {
        var f = new Fixture(); var vm = f.Create(); Assert.True(await Open(vm, Monday));
        var before = Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary"); f.Actions.Current = Report(Monday.AddDays(-7), false); f.Actions.Previous = null;
        Assert.True(await Open(vm, Monday.AddDays(-7))); Assert.Same(f.Actions.Current, Get<AgendaWeekRetrospective>(vm, "Report"));
        Assert.NotSame(before, Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary")); Assert.Equal("Sem tarefas", Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary").CompletionLabel);
    }

    [Fact]
    public async Task ComparerFailure_DoesNotPublishPartialDataAndAllowsRetry()
    {
        var f = new Fixture(); f.Comparer.Fail = true; var vm = f.Create(); Assert.False(await Open(vm, Monday));
        Assert.Null(Get<AgendaWeekRetrospective?>(vm, "Report")); Assert.True(Get<bool>(vm, "HasError")); Assert.False(Get<bool>(vm, "IsBusy"));
        f.Comparer.Fail = false; Assert.True(await Open(vm, Monday));
    }

    [Fact]
    public async Task Feedback_DoesNotExposeStorageExceptionDetails()
    {
        var f = new Fixture(); f.Actions.FailClose = true; var vm = f.Create(); Assert.False(await Open(vm, Monday));
        Assert.NotEmpty(Get<string>(vm, "Feedback")); Assert.DoesNotContain("private-path-test", Get<string>(vm, "Feedback"));
    }

    [Fact]
    public async Task BusyLoad_RejectsConcurrentNavigationWithoutAnotherCall()
    {
        var f = new Fixture(); f.Actions.CloseGate = new(TaskCreationOptions.RunContinuationsAsynchronously); var vm = f.Create();
        var pending = Open(vm, Monday);
        try
        {
            Assert.True(Get<bool>(vm, "IsBusy")); Assert.False(await Open(vm, Monday.AddDays(-7))); Assert.Single(f.Actions.Closed);
        }
        finally { f.Actions.CloseGate.TrySetResult(); }
        Assert.True(await pending); Assert.False(Get<bool>(vm, "IsBusy"));
    }

    [Fact]
    public async Task BusyState_CoversThePreviousWeekReadToo()
    {
        var f = new Fixture(); f.Actions.ReadGate = new(TaskCreationOptions.RunContinuationsAsynchronously); var vm = f.Create();
        var pending = Open(vm, Monday);
        try { Assert.True(Get<bool>(vm, "IsBusy")); Assert.False(await Invoke(vm, "RefreshAsync", CancellationToken.None)); Assert.Single(f.Actions.Read); }
        finally { f.Actions.ReadGate.TrySetResult(); }
        Assert.True(await pending); Assert.False(Get<bool>(vm, "IsBusy"));
    }

    [Fact]
    public async Task PreCancelledRequest_DoesNotReadClockOrStorage()
    {
        var f = new Fixture(); f.Clock.ThrowOnRead = true; var vm = f.Create(); using var cts = new CancellationTokenSource(); cts.Cancel();
        Assert.False(await Open(vm, Monday, cts.Token)); Assert.Empty(f.Actions.Closed); Assert.Empty(f.Actions.Read); Assert.False(Get<bool>(vm, "IsBusy"));
        Assert.False(Get<bool>(vm, "HasError"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAtEitherRead_PreservesPreviousResultAndAllowsRetry(bool duringPrevious)
    {
        var f = new Fixture(); var vm = f.Create(); Assert.True(await Open(vm, Monday));
        var report = Get<AgendaWeekRetrospective>(vm, "Report"); var summary = Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary");
        using var cts = new CancellationTokenSource();
        if (duringPrevious) f.Actions.ReadGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        else f.Actions.CloseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = Open(vm, Monday, cts.Token); cts.Cancel(); Assert.False(await pending);
        Assert.Same(report, Get<AgendaWeekRetrospective>(vm, "Report")); Assert.Same(summary, Get<AgendaRetrospectiveSummaryViewModel>(vm, "Summary"));
        Assert.False(Get<bool>(vm, "IsBusy")); Assert.False(Get<bool>(vm, "HasError"));
        f.Actions.ReadGate = null; f.Actions.CloseGate = null; Assert.True(await Open(vm, Monday));
    }

    [Fact]
    public async Task CancellationToken_IsForwardedToBothServiceCalls()
    {
        var f = new Fixture(); var vm = f.Create(); using var cts = new CancellationTokenSource(); Assert.True(await Open(vm, Monday, cts.Token));
        Assert.Equal(cts.Token, f.Actions.CloseToken); Assert.Equal(cts.Token, f.Actions.ReadToken);
    }

    [Fact]
    public async Task RefreshBeforeOpening_DoesNotCreateOrChooseAWeek()
    {
        var f = new Fixture(); var vm = f.Create(); Assert.False(await Invoke(vm, "RefreshAsync", CancellationToken.None));
        Assert.Empty(f.Actions.Closed); Assert.Empty(f.Actions.Read);
    }

    [Fact]
    public async Task Refresh_UsesLastSuccessfulWeekEvenAfterFailedNavigation()
    {
        var f = new Fixture(); var vm = f.Create(); Assert.True(await Open(vm, Monday)); f.Actions.FailClose = true;
        Assert.False(await Open(vm, Monday.AddDays(-7))); f.Actions.FailClose = false;
        Assert.True(await Invoke(vm, "RefreshAsync", CancellationToken.None)); Assert.Equal(Monday, f.Actions.Closed[^1]);
    }

    [Fact]
    public async Task ObservableState_NotifiesBindingsAboutLoadAndPublication()
    {
        var f = new Fixture(); var vm = f.Create(); var observable = Assert.IsAssignableFrom<INotifyPropertyChanged>(vm); var names = new List<string?>();
        observable.PropertyChanged += (_, e) => names.Add(e.PropertyName); Assert.True(await Open(vm, Monday));
        Assert.Contains("Report", names); Assert.Contains("Summary", names); Assert.True(names.Count(name => name == "IsBusy") >= 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RequiredDependencies_AreRejectedWhenNull(int index)
    {
        var f = new Fixture(); object?[] args = [f.Actions, f.Comparer, f.Clock]; args[index] = null;
        Assert.Throws<ArgumentNullException>(() => Create(args));
    }

    private static Task<bool> Open(object vm, DateOnly day, CancellationToken token = default) => Invoke(vm, "OpenAsync", day, token);
    private static async Task<bool> Invoke(object vm, string name, params object[] args)
    {
        var method = vm.GetType().GetMethod(name); Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task<bool>>(method.Invoke(vm, args)); return await task;
    }
    private static T Get<T>(object vm, string name)
    {
        var property = vm.GetType().GetProperty(name); Assert.NotNull(property); return (T)property.GetValue(vm)!;
    }
    private static object Create(params object?[] args)
    {
        var type = typeof(AgendaViewModel).Assembly.GetType("Trama.Desktop.ViewModels.AgendaRetrospectiveViewModel"); Assert.NotNull(type);
        try { var value = Activator.CreateInstance(type, args); Assert.NotNull(value); return value; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static AgendaWeekRetrospective Report(DateOnly start, bool populated = true)
    {
        var local = start.AddDays(7).ToDateTime(new TimeOnly(12, 0));
        var tasks = populated ? new[] {
            new AgendaRetrospectiveTask(new(Guid.NewGuid(), "Planejada", "Fictícia", start), null),
            new AgendaRetrospectiveTask(new(Guid.NewGuid(), "Realizada", "Fictícia", start, Status: AgendaTaskStatus.Completed), new DateTimeOffset(start.AddDays(1).ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(-3)))
        } : Array.Empty<AgendaRetrospectiveTask>();
        return new AgendaRetrospectiveBuilder().Build(start, tasks, new(local, TimeSpan.FromHours(-3)), Zone);
    }
    private sealed class Fixture
    {
        public Actions Actions { get; } = new();
        public Comparer Comparer { get; } = new();
        public Clock Clock { get; } = new();
        public object Create() => AgendaRetrospectiveNavigationTests.Create(Actions, Comparer, Clock);
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 9, 15, 0, 0, TimeSpan.Zero);
        public bool ThrowOnRead { get; set; }
        public override DateTimeOffset GetUtcNow() => ThrowOnRead ? throw new InvalidOperationException("Clock must not be read.") : UtcNow;
        public override TimeZoneInfo LocalTimeZone => Zone;
    }
    private sealed class Comparer : IAgendaRetrospectiveComparer
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public AgendaWeekRetrospective? Current { get; private set; }
        public AgendaWeekRetrospective? Previous { get; private set; }
        public AgendaRetrospectiveComparison Compare(AgendaWeekRetrospective current, AgendaWeekRetrospective? previous)
        {
            Calls++; Current = current; Previous = previous;
            if (Fail) throw new InvalidOperationException("Test comparison failure.");
            return new AgendaRetrospectiveComparer().Compare(current, previous);
        }
    }
    private sealed class Actions : IAgendaRetrospectiveActions
    {
        public AgendaWeekRetrospective Current { get; set; } = Report(Monday);
        public AgendaWeekRetrospective? Previous { get; set; } = Report(Monday.AddDays(-7));
        public List<DateOnly> Closed { get; } = [];
        public List<DateOnly> Read { get; } = [];
        public bool FailClose { get; set; }
        public bool FailRead { get; set; }
        public TaskCompletionSource? CloseGate { get; set; }
        public TaskCompletionSource? ReadGate { get; set; }
        public CancellationToken CloseToken { get; private set; }
        public CancellationToken ReadToken { get; private set; }
        public async Task<AgendaWeekRetrospective> GetOrCloseAsync(DateOnly referenceDate, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Closed.Add(referenceDate); CloseToken = cancellationToken;
            if (CloseGate is { } gate) await gate.Task.WaitAsync(cancellationToken);
            if (FailClose) throw new InvalidOperationException("private-path-test"); return Current;
        }
        public async Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Read.Add(weekStart); ReadToken = cancellationToken;
            if (ReadGate is { } gate) await gate.Task.WaitAsync(cancellationToken);
            if (FailRead) throw new InvalidOperationException("private-path-test"); return Previous;
        }
        public Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Navigation must not scan the closure history.");
    }
}
