using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaPeriodQueryTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateOnly Sunday = Monday.AddDays(6);

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(-3, 7)]
    [InlineData(2, 3)]
    [InlineData(6, 8)]
    public async Task IntersectingPlanning_IsReturnedWithItsOriginalDates(int start, int end)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(Monday.AddDays(start)) with { PlannedEndDate = Monday.AddDays(end) }; await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday)));
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task BothDateBoundaries_AreInclusive(int offset)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(Monday.AddDays(offset)); await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday)));
    }

    [Theory]
    [InlineData(-3, -1)]
    [InlineData(7, 9)]
    public async Task PlanningEntirelyOutsideThePeriod_IsExcluded(int start, int end)
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example(Monday.AddDays(start)) with { PlannedEndDate = Monday.AddDays(end) });
        Assert.Empty(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday));
    }

    [Fact]
    public async Task MissingPlannedEnd_MeansTheStartDayEvenWhenTheTaskIsStillPending()
    {
        using var f = await Fixture.CreateAsync(); var today = Example(Monday) with { EndTime = null }; var old = Example(Monday.AddDays(-1)) with { EndTime = null };
        await f.SaveAsync(today); await f.SaveAsync(old);
        Assert.Equal(today, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday)));
    }

    [Fact]
    public async Task SingleDateQuery_FindsATaskThatStartedOnAnotherDay()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(Monday.AddDays(-2)) with { PlannedEndDate = Monday.AddDays(2) }; await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Monday)));
    }

    [Fact]
    public async Task DateOnlyInterval_IsReturnedWithoutInventingHours()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(Monday.AddDays(-1)) with { PlannedEndDate = Sunday, StartTime = null, EndTime = null }; await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday)));
    }

    [Fact]
    public async Task ExistingDailyQuery_KeepsFilteringOnlyByThePlannedStartDate()
    {
        using var f = await Fixture.CreateAsync(); var spanning = Example(Monday.AddDays(-1)) with { PlannedEndDate = Sunday }; var startsToday = Example(Monday);
        await f.SaveAsync(spanning); await f.SaveAsync(startsToday);
        Assert.Equal(startsToday, Assert.Single(await f.Actions.ListAsync(Monday, Sunday)));
    }

    [Fact]
    public async Task EqualTitlesAndMultiDayIntervals_AreReturnedOncePerIdentity()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(Monday.AddDays(-5)) with { PlannedEndDate = Sunday.AddDays(5) }; var second = first with { Id = Guid.NewGuid(), Description = "Outra tarefa" };
        await f.SaveAsync(first); await f.SaveAsync(second);
        var rows = await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday);
        Assert.Equal(2, rows.Count); Assert.Equal(2, rows.Select(task => task.Id).Distinct().Count()); Assert.Contains(first, rows); Assert.Contains(second, rows);
    }

    [Fact]
    public async Task Ordering_KeepsDayTimeUntimedTitleAndIdentityCriteria()
    {
        using var f = await Fixture.CreateAsync();
        var prior = Example(Monday.AddDays(-1)) with { PlannedEndDate = Monday };
        var first = Example(Monday) with { Title = "A", StartTime = new TimeOnly(9, 0), EndTime = null, Id = Guid.Parse("00000000-0000-0000-0000-000000000001") };
        var tied = first with { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") };
        var title = first with { Id = Guid.NewGuid(), Title = "B" };
        var later = Example(Monday) with { Title = "A", StartTime = new TimeOnly(10, 0), EndTime = null };
        var untimed = Example(Monday) with { Title = "A", StartTime = null, EndTime = null };
        var expected = new[] { prior, first, tied, title, later, untimed };
        foreach (var task in expected.Reverse()) await f.SaveAsync(task);
        Assert.Equal(expected, await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday));
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskStatus.Completed)]
    public async Task EveryActiveState_IsIncludedWithoutChangingIt(AgendaTaskStatus status)
    {
        using var f = await Fixture.CreateAsync(); var task = Example(Monday) with { Status = status }; await f.SaveAsync(task);
        Assert.Equal(task, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday))); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task TrashedTasks_AreHiddenAndReappearAfterRestoration()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(Monday.AddDays(-1)) with { PlannedEndDate = Sunday }; await f.SaveAsync(task);
        await f.Trash.MoveToTrashAsync(task.Id, new DateTimeOffset(2026, 11, 6, 20, 0, 0, TimeSpan.FromHours(-3)));
        Assert.Empty(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday)); await f.Trash.RestoreAsync(task.Id);
        Assert.Equal(task, Assert.Single(await Reader(f.Actions).ListOverlappingAsync(Monday, Sunday)));
    }

    [Fact]
    public async Task ReversedPeriod_IsRejectedBeforeOpeningAnAbsentDatabase()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db"); var reader = Reader(new SqliteAgendaActions(path));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => reader.ListOverlappingAsync(Sunday, Monday)); Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task PreCancellation_DoesNotOpenOrCreateAnAbsentDatabase()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db"); var reader = Reader(new SqliteAgendaActions(path));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ListOverlappingAsync(Monday, Sunday, cancel.Token)); Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task AbsentDatabase_IsReportedWithoutCreatingAnEmptyProfile()
    {
        using var root = new NotebookTestWorkspace(); var path = Path.Combine(root.DirectoryPath, "missing.db"); var reader = Reader(new SqliteAgendaActions(path));
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => reader.ListOverlappingAsync(Monday, Sunday)); Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-11-02")]
    public async Task SelectedMalformedOrReversedPlanning_IsRejected(string end)
    {
        using var f = await Fixture.CreateAsync(); await f.SaveAsync(Example(Monday.AddDays(3)));
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE AgendaTasks SET PlannedEndDate = {end}");
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => Reader(f.Actions).ListOverlappingAsync(Monday, Sunday));
    }

    [Fact]
    public async Task ProfilesWithTheSameTaskIdentity_AreIsolated()
    {
        using var first = await Fixture.CreateAsync(); using var second = await Fixture.CreateAsync(); var task = Example(Monday.AddDays(-1)) with { PlannedEndDate = Sunday };
        await first.SaveAsync(task); await second.SaveAsync(task with { Description = "Segundo perfil" });
        Assert.Equal(task, Assert.Single(await Reader(first.Actions).ListOverlappingAsync(Monday, Sunday)));
        Assert.Equal(task with { Description = "Segundo perfil" }, Assert.Single(await Reader(second.Actions).ListOverlappingAsync(Monday, Sunday)));
    }

    [Fact]
    public async Task Composition_ProvidesThePeriodReaderForTheSameProfile()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var reader = services.GetService<IAgendaPeriodReader>(); Assert.NotNull(reader);
        var actions = services.GetRequiredService<IAgendaActions>(); var task = Example(Monday.AddDays(-1)) with { PlannedEndDate = Sunday };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions.SaveAsync(task)).Status);
        Assert.Equal(task, Assert.Single(await reader.ListOverlappingAsync(Monday, Sunday)));
    }

    private static IAgendaPeriodReader Reader(object source) => Assert.IsAssignableFrom<IAgendaPeriodReader>(source);
    private static AgendaTaskSnapshot Example(DateOnly date) => new(Guid.NewGuid(), "Tarefa fictícia", "Planejamento preservado", date, new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Study);
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public SqliteAgendaActions Actions { get; private set; } = null!;
        public SqliteAgendaTrashActions Trash { get; private set; } = null!;
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            try
            {
                await using var context = f.Root.CreateContext(); await context.Database.MigrateAsync(); var path = Path.Combine(f.Root.DirectoryPath, "trama.db"); f.Actions = new(path); f.Trash = new(path); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public async Task SaveAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        public void Dispose() => Root.Dispose();
    }
}
