using Trama.Application.Agenda;

namespace Trama.UnitTests.Agenda;

public sealed class AgendaWeekProjectionTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateOnly Sunday = Monday.AddDays(6);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void EveryReferenceDay_ProducesTheSameMondayToSundayWeek(int offset)
    {
        var week = Projector().Build(Monday.AddDays(offset), []);
        Assert.Equal(Monday, week.StartDate); Assert.Equal(Sunday, week.EndDate);
        Assert.Equal(Enumerable.Range(0, 7).Select(Monday.AddDays), week.Days.Select(day => day.Date));
    }

    [Fact]
    public void WeekAcrossNewYear_KeepsSevenOrderedCalendarDates()
    {
        var week = Projector().Build(new DateOnly(2026, 12, 31), []);
        Assert.Equal(new DateOnly(2026, 12, 28), week.StartDate); Assert.Equal(new DateOnly(2027, 1, 3), week.EndDate);
        Assert.Equal(7, week.Days.Count); Assert.Equal(7, week.Days.Select(day => day.Date).Distinct().Count());
    }

    [Fact]
    public void EmptyWeek_KeepsAllDaysAndZeroTasks()
    {
        var week = Projector().Build(Monday, []); Assert.Equal(7, week.Days.Count); Assert.Equal(0, week.UniqueTaskCount); Assert.All(week.Days, day => Assert.Empty(day.Entries));
    }

    [Fact]
    public void MultiDayTask_AppearsOnlyOnItsPlannedDatesWithItsOriginalIdentity()
    {
        var task = Example(Monday.AddDays(1)) with { PlannedEndDate = Monday.AddDays(3) };
        var week = Projector().Build(Monday, [task]); var entries = week.Days.SelectMany(day => day.Entries).ToArray();
        Assert.Equal(3, entries.Length); Assert.Equal(1, week.UniqueTaskCount); Assert.All(entries, entry => Assert.Equal(task, entry.Task));
        Assert.Equal(new[] { Monday.AddDays(1), Monday.AddDays(2), Monday.AddDays(3) }, entries.Select(entry => entry.Date));
        Assert.Empty(week.Days[0].Entries); Assert.Empty(week.Days[4].Entries);
    }

    [Fact]
    public void TaskSpanningBeyondBothWeekEdges_IsClippedWithoutRewritingItsPlanning()
    {
        var task = Example(Monday.AddDays(-3)) with { PlannedEndDate = Sunday.AddDays(2) };
        var week = Projector().Build(Monday, [task]); Assert.Equal(1, week.UniqueTaskCount);
        Assert.All(week.Days, day =>
        {
            var entry = Assert.Single(day.Entries); Assert.Equal(task, entry.Task); Assert.Equal(day.Date, entry.Date);
            Assert.True(entry.ContinuesFromPreviousDay); Assert.True(entry.ContinuesToNextDay); Assert.Null(entry.StartTime); Assert.Null(entry.EndTime);
        });
    }

    [Fact]
    public void TasksEntirelyOutsideTheWeek_AreNotProjected()
    {
        var week = Projector().Build(Monday, [Example(Monday.AddDays(-1)), Example(Sunday.AddDays(1))]);
        Assert.Equal(0, week.UniqueTaskCount); Assert.All(week.Days, day => Assert.Empty(day.Entries));
    }

    [Fact]
    public void OpenEndedPendingTask_StaysOnItsPlannedStartDay()
    {
        var task = Example(Monday) with { EndTime = null }; var week = Projector().Build(Monday, [task]);
        var entry = Assert.Single(week.Days[0].Entries); Assert.Equal(task, entry.Task); Assert.False(entry.ContinuesFromPreviousDay); Assert.False(entry.ContinuesToNextDay);
        Assert.Equal(task.StartTime, entry.StartTime); Assert.Null(entry.EndTime); Assert.All(week.Days.Skip(1), day => Assert.Empty(day.Entries));
    }

    [Fact]
    public void SingleDayTask_KeepsItsHoursAndHasNoContinuationMarkers()
    {
        var task = Example(Monday); var entry = Assert.Single(Projector().Build(Monday, [task]).Days[0].Entries);
        Assert.Equal(task.StartTime, entry.StartTime); Assert.Equal(task.EndTime, entry.EndTime); Assert.False(entry.ContinuesFromPreviousDay); Assert.False(entry.ContinuesToNextDay);
    }

    [Fact]
    public void ExplicitMidnightEnd_RemainsOnTheInclusiveFinalDate()
    {
        var task = Example(Monday) with { PlannedEndDate = Monday.AddDays(1), StartTime = new TimeOnly(22, 0), EndTime = TimeOnly.MinValue };
        var week = Projector().Build(Monday, [task]); Assert.Equal(1, week.UniqueTaskCount);
        Assert.Equal(new TimeOnly(22, 0), Assert.Single(week.Days[0].Entries).StartTime);
        Assert.Equal(TimeOnly.MinValue, Assert.Single(week.Days[1].Entries).EndTime); Assert.Empty(week.Days[2].Entries);
    }

    [Theory]
    [InlineData(AgendaTaskCategory.Personal)]
    [InlineData(AgendaTaskCategory.Work)]
    [InlineData(AgendaTaskCategory.Study)]
    public void CategoryFilter_DoesNotChangeTheSelectedTaskState(AgendaTaskCategory category)
    {
        var tasks = Enum.GetValues<AgendaTaskCategory>().Select(value => Example(Monday) with { Category = value, Status = AgendaTaskStatus.InProgress }).ToArray();
        var week = Projector().Build(Monday, tasks, category); Assert.Equal(1, week.UniqueTaskCount);
        Assert.Equal(tasks.Single(task => task.Category == category), Assert.Single(week.Days[0].Entries).Task);
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskStatus.Completed)]
    public void StateFilter_PreservesStateIndependentlyOfTheSameCategory(AgendaTaskStatus status)
    {
        var tasks = Enum.GetValues<AgendaTaskStatus>().Select(value => Example(Monday) with { Category = AgendaTaskCategory.Work, Status = value }).ToArray();
        var week = Projector().Build(Monday, tasks, status: status); Assert.Equal(1, week.UniqueTaskCount);
        var entry = Assert.Single(week.Days[0].Entries); Assert.Equal(tasks.Single(task => task.Status == status), entry.Task); Assert.Equal(AgendaTaskCategory.Work, entry.Task.Category);
    }

    [Fact]
    public void CategoryAndStateFilters_AreCombinedWithAnd()
    {
        var wanted = Example(Monday) with { Category = AgendaTaskCategory.Study, Status = AgendaTaskStatus.InProgress };
        var week = Projector().Build(Monday, [wanted, wanted with { Id = Guid.NewGuid(), Category = AgendaTaskCategory.Work }, wanted with { Id = Guid.NewGuid(), Status = AgendaTaskStatus.Completed }], AgendaTaskCategory.Study, AgendaTaskStatus.InProgress);
        Assert.Equal(1, week.UniqueTaskCount); Assert.Equal(wanted, Assert.Single(week.Days[0].Entries).Task);
    }

    [Fact]
    public void NoFilterMatches_KeepsSevenEmptyDaysAndTheSameWeekRange()
    {
        var week = Projector().Build(Monday.AddDays(3), [Example(Monday)], AgendaTaskCategory.Personal);
        Assert.Equal(Monday, week.StartDate); Assert.Equal(Sunday, week.EndDate); Assert.Equal(7, week.Days.Count); Assert.Equal(0, week.UniqueTaskCount); Assert.All(week.Days, day => Assert.Empty(day.Entries));
    }

    [Fact]
    public void UniqueCount_DoesNotCountMultiDayCardsAsDifferentTasks()
    {
        var first = Example(Monday) with { PlannedEndDate = Sunday }; var second = Example(Monday.AddDays(1));
        var week = Projector().Build(Monday, [first, second]); Assert.Equal(2, week.UniqueTaskCount); Assert.Equal(8, week.Days.Sum(day => day.Entries.Count));
    }

    [Fact]
    public void Ordering_KeepsPriorStartsThenTimeUntimedTitleAndIdentity()
    {
        var prior = Example(Monday.AddDays(-1)) with { PlannedEndDate = Monday };
        var first = Example(Monday) with { Title = "A", StartTime = new TimeOnly(9, 0), EndTime = null, Id = Guid.Parse("00000000-0000-0000-0000-000000000001") };
        var tied = first with { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") }; var title = first with { Id = Guid.NewGuid(), Title = "B" };
        var later = Example(Monday) with { StartTime = new TimeOnly(10, 0), EndTime = null }; var untimed = Example(Monday) with { StartTime = null, EndTime = null };
        var expected = new[] { prior, first, tied, title, later, untimed };
        Assert.Equal(expected, Projector().Build(Monday, expected.Reverse().ToArray()).Days[0].Entries.Select(entry => entry.Task));
    }

    [Fact]
    public void Projection_DoesNotMutateInputAndSurvivesClearingTheSourceCollection()
    {
        var task = Example(Monday) with { PlannedEndDate = Monday.AddDays(2) }; var source = new List<AgendaTaskSnapshot> { task };
        var week = Projector().Build(Monday, source); Assert.Equal(task, Assert.Single(source)); source.Clear();
        Assert.Equal(1, week.UniqueTaskCount); Assert.Equal(3, week.Days.Sum(day => day.Entries.Count)); Assert.All(week.Days.Take(3), day => Assert.Equal(task, Assert.Single(day.Entries).Task));
    }

    [Fact]
    public void NullInput_IsRejected()
    {
        var projector = Projector(); Assert.Throws<ArgumentNullException>(() => projector.Build(Monday, null!));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("title")]
    [InlineData("dates")]
    public void InvalidSnapshots_AreRejectedBeforeFiltering(string fault)
    {
        var task = Example(Monday.AddDays(-10));
        AgendaTaskSnapshot invalid = fault switch { "null" => null!, "title" => task with { Title = " " }, _ => task with { PlannedEndDate = task.PlannedDate.AddDays(-1) } };
        var projector = Projector(); Assert.Throws<ArgumentException>(() => projector.Build(Monday, [invalid], AgendaTaskCategory.Personal));
    }

    [Fact]
    public void DuplicateIdentities_AreRejectedRatherThanSilentlyOverwritingPlanning()
    {
        var task = Example(Monday); var projector = Projector(); Assert.Throws<ArgumentException>(() => projector.Build(Monday, [task, task with { Title = "Outro planejamento" }]));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UndefinedFilterValues_AreRejected(bool category)
    {
        var projector = Projector(); Assert.Throws<ArgumentOutOfRangeException>(() => projector.Build(Monday, [], category ? (AgendaTaskCategory)99 : null, category ? null : (AgendaTaskStatus)99));
    }

    [Fact]
    public void WeekBeyondDateOnlyMaximum_IsRejectedInsteadOfReturningAPartialWeek()
    {
        var projector = Projector(); Assert.Throws<ArgumentOutOfRangeException>(() => projector.Build(DateOnly.MaxValue, []));
    }

    [Fact]
    public void FirstRepresentableWeek_HasSevenDaysWithoutUnderflow()
    {
        var week = Projector().Build(DateOnly.MinValue, []); Assert.Equal(DateOnly.MinValue, week.StartDate); Assert.Equal(DateOnly.MinValue.AddDays(6), week.EndDate); Assert.Equal(7, week.Days.Count);
    }

    [Fact]
    public void DailyHours_AppearOnlyOnTheActualStartAndEndDates()
    {
        var task = Example(Monday) with { PlannedEndDate = Monday.AddDays(2), StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) };
        var week = Projector().Build(Monday, [task]); var first = Assert.Single(week.Days[0].Entries); var middle = Assert.Single(week.Days[1].Entries); var last = Assert.Single(week.Days[2].Entries);
        Assert.Equal(task.StartTime, first.StartTime); Assert.Null(first.EndTime); Assert.False(first.ContinuesFromPreviousDay); Assert.True(first.ContinuesToNextDay);
        Assert.Null(middle.StartTime); Assert.Null(middle.EndTime); Assert.True(middle.ContinuesFromPreviousDay); Assert.True(middle.ContinuesToNextDay);
        Assert.Null(last.StartTime); Assert.Equal(task.EndTime, last.EndTime); Assert.True(last.ContinuesFromPreviousDay); Assert.False(last.ContinuesToNextDay);
    }

    private static AgendaTaskSnapshot Example(DateOnly date) => new(Guid.NewGuid(), "Tarefa fictícia", "Planejamento preservado", date, new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Work);
    private static IAgendaWeekProjector Projector()
    {
        var type = typeof(IAgendaWeekProjector).Assembly.GetType("Trama.Application.Agenda.AgendaWeekProjector"); Assert.NotNull(type);
        return Assert.IsAssignableFrom<IAgendaWeekProjector>(Activator.CreateInstance(type));
    }
}
