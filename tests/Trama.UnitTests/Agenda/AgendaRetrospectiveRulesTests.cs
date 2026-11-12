using Trama.Application.Agenda;

namespace Trama.UnitTests.Agenda;

public sealed class AgendaRetrospectiveRulesTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateOnly Sunday = Monday.AddDays(6);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("RetrospectiveTests", TimeSpan.FromHours(-3), "Test", "Test");
    private static readonly DateTimeOffset ClosedAt = At(Monday.AddDays(7), 12);

    [Fact]
    public void ClosedReport_IdentifiesTheWeekCaptureInstantAndExplicitTimeZone()
    {
        var report = Builder().Build(Monday.AddDays(3), [], ClosedAt, Zone);
        Assert.Equal(Monday, report.StartDate); Assert.Equal(Sunday, report.EndDate); Assert.Equal(ClosedAt, report.ClosedAt); Assert.Equal(Zone.Id, report.TimeZoneId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    public void WeekStillInProgress_CannotBeClosed(int hour)
    {
        var builder = Builder(); Assert.Throws<ArgumentException>(() => builder.Build(Monday, [], At(Sunday, hour), Zone));
    }

    [Fact]
    public void NextMondayAtLocalMidnight_AllowsClosingThePreviousWeek()
    {
        var report = Builder().Build(Monday, [], At(Monday.AddDays(7), 0), Zone); Assert.Equal(Sunday, report.EndDate);
    }

    [Fact]
    public void EmptyReport_HasSevenDaysAllCategoriesAndNoInventedPercentage()
    {
        var report = Build(); Assert.Equal(0, report.PlannedCount); Assert.Equal(0, report.CompletedInWeekCount);
        Assert.Null(report.CompletionPercentage); Assert.Empty(report.Tasks); Assert.Equal(7, report.Days.Count);
        Assert.Equal(Enumerable.Range(0, 7).Select(Monday.AddDays), report.Days.Select(day => day.Date));
        Assert.All(report.Days, day => { Assert.Equal(0, day.PlannedCount); Assert.Equal(0, day.CompletedCount); });
        Assert.Equal(Enum.GetValues<AgendaTaskCategory>(), report.Categories.Select(c => c.Category));
        Assert.All(report.Categories, c => { Assert.Equal(0, c.PlannedCount); Assert.Equal(0, c.CompletedCount); });
    }

    [Fact]
    public void MultiDayTask_IsCountedOnceAndPlannedOnlyOnItsStartingDay()
    {
        var item = Item() with { Task = TaskOn(Monday) with { PlannedEndDate = Sunday } }; var report = Build(item);
        Assert.Equal(1, report.PlannedCount); Assert.Single(report.Tasks); Assert.Equal(1, report.Days[0].PlannedCount);
        Assert.Equal(1, report.Days.Sum(day => day.PlannedCount)); Assert.Equal(0, report.CarriedInCount);
    }

    [Fact]
    public void PriorStart_IsIncludedOnceAsCarriedInWithoutInventingAStartInsideTheWeek()
    {
        var task = TaskOn(Monday.AddDays(-2)) with { PlannedEndDate = Monday.AddDays(2), Status = AgendaTaskStatus.Completed };
        var report = Build(new AgendaRetrospectiveTask(task, At(Monday.AddDays(1), 10)));
        Assert.Equal(1, report.PlannedCount); Assert.Equal(1, report.CarriedInCount); Assert.Equal(1, report.CompletedInWeekCount);
        Assert.Equal(0, report.Days.Sum(day => day.PlannedCount)); Assert.Equal(1, report.Days[1].CompletedCount); Assert.Equal(100m, report.CompletionPercentage);
    }

    [Fact]
    public void PlanningEntirelyOutsideTheWeek_IsExcludedFromThisCohort()
    {
        var selected = Item(); var old = new AgendaRetrospectiveTask(TaskOn(Monday.AddDays(-1)), null); var future = new AgendaRetrospectiveTask(TaskOn(Sunday.AddDays(1)), null);
        var report = Build(old, selected, future); Assert.Equal(selected, Assert.Single(report.Tasks)); Assert.Equal(1, report.PlannedCount);
    }

    [Fact]
    public void ActualCompletion_IsGroupedOnItsRealDayInsteadOfThePlannedEnd()
    {
        var task = TaskOn(Monday) with { PlannedEndDate = Sunday, Status = AgendaTaskStatus.Completed };
        var report = Build(new AgendaRetrospectiveTask(task, At(Monday.AddDays(2), 16)));
        Assert.Equal(1, report.Days[0].PlannedCount); Assert.Equal(1, report.Days[2].CompletedCount); Assert.Equal(0, report.Days[6].CompletedCount);
        Assert.Equal(1, report.CompletedInWeekCount); Assert.Equal(1, report.Days.Sum(day => day.CompletedCount));
    }

    [Theory]
    [InlineData(2, 30, 1)]
    [InlineData(3, 0, 0)]
    public void CompletionAtUtcWeekBoundary_IsClassifiedUsingTheReportTimeZone(int utcHour, int minute, int expected)
    {
        var completedAt = new DateTimeOffset(2026, 11, 9, utcHour, minute, 0, TimeSpan.Zero);
        var report = Build(new AgendaRetrospectiveTask(TaskOn(Monday) with { Status = AgendaTaskStatus.Completed }, completedAt));
        Assert.Equal(expected, report.CompletedInWeekCount); Assert.Equal(expected, report.Days[6].CompletedCount);
        Assert.Equal(1 - expected, report.CompletedOutsideWeekCount);
    }

    [Fact]
    public void LegacyCompletionWithoutTimestamp_IsExplicitAndDoesNotBorrowPlannedHours()
    {
        var task = TaskOn(Monday) with { PlannedEndDate = Sunday, Status = AgendaTaskStatus.Completed }; var report = Build(new AgendaRetrospectiveTask(task, null));
        Assert.Equal(1, report.CompletedWithoutDateCount); Assert.Equal(0, report.CompletedInWeekCount); Assert.Equal(0m, report.CompletionPercentage);
        Assert.Equal(0, report.Days.Sum(day => day.CompletedCount)); Assert.Equal(0, report.PendingCount); Assert.Equal(0, report.InProgressCount);
        Assert.Equal(task, Assert.Single(report.Tasks).Task);
    }

    [Fact]
    public void CompletedBeforeTheWeek_IsSeparateFromCompletionsWithinTheWeek()
    {
        var task = TaskOn(Monday.AddDays(-2)) with { PlannedEndDate = Monday, Status = AgendaTaskStatus.Completed };
        var report = Build(new AgendaRetrospectiveTask(task, At(Monday.AddDays(-1), 10)));
        Assert.Equal(1, report.CompletedOutsideWeekCount); Assert.Equal(0, report.CompletedInWeekCount); Assert.Equal(0, report.CompletedWithoutDateCount);
    }

    [Fact]
    public void CompletedAfterSundayButBeforeCapture_DoesNotRewriteTheWeekAsCompleted()
    {
        var report = Build(new AgendaRetrospectiveTask(TaskOn(Monday) with { Status = AgendaTaskStatus.Completed }, At(Monday.AddDays(7), 9)));
        Assert.Equal(1, report.CompletedOutsideWeekCount); Assert.Equal(0, report.CompletedInWeekCount);
        Assert.Equal(0, report.PendingCount + report.InProgressCount); Assert.Equal(0, report.Days.Sum(day => day.CompletedCount));
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    public void ReopenedTask_OldCompletionDoesNotCountAsItsCurrentConclusion(AgendaTaskStatus status)
    {
        var report = Build(new AgendaRetrospectiveTask(TaskOn(Monday) with { Status = status }, At(Monday.AddDays(1), 10)));
        Assert.Equal(0, report.CompletedInWeekCount); Assert.Equal(0, report.CompletedOutsideWeekCount); Assert.Equal(0, report.CompletedWithoutDateCount);
        Assert.Equal(status == AgendaTaskStatus.Pending ? 1 : 0, report.PendingCount); Assert.Equal(status == AgendaTaskStatus.InProgress ? 1 : 0, report.InProgressCount);
    }

    [Fact]
    public void Categories_UseTheSameUniqueCohortAndCompletionCriteria()
    {
        var workDone = Item(AgendaTaskCategory.Work, AgendaTaskStatus.Completed, At(Monday, 10));
        var studyDone = Item(AgendaTaskCategory.Study, AgendaTaskStatus.Completed, At(Monday, 11));
        var report = Build(workDone, Item(AgendaTaskCategory.Work), studyDone, Item(AgendaTaskCategory.Personal, AgendaTaskStatus.InProgress));
        Assert.Equal(4, report.PlannedCount); Assert.Equal(2, report.CompletedInWeekCount); Assert.Equal(50m, report.CompletionPercentage);
        Assert.Equal(new AgendaRetrospectiveCategory(AgendaTaskCategory.Work, 2, 1), Assert.Single(report.Categories, c => c.Category == AgendaTaskCategory.Work));
        Assert.Equal(new AgendaRetrospectiveCategory(AgendaTaskCategory.Study, 1, 1), Assert.Single(report.Categories, c => c.Category == AgendaTaskCategory.Study));
        Assert.Equal(new AgendaRetrospectiveCategory(AgendaTaskCategory.Personal, 1, 0), Assert.Single(report.Categories, c => c.Category == AgendaTaskCategory.Personal));
    }

    [Theory]
    [InlineData(1, "33.3")]
    [InlineData(2, "66.7")]
    public void CompletionPercentage_IsRoundedToOneDecimal(int completed, string percentage)
    {
        var items = Enumerable.Range(0, 3).Select(index => index < completed ? Item(status: AgendaTaskStatus.Completed, at: At(Monday, 10)) : Item()).ToArray();
        Assert.Equal(decimal.Parse(percentage, System.Globalization.CultureInfo.InvariantCulture), Build(items).CompletionPercentage);
    }

    [Fact]
    public void ReportCapture_IsIndependentOfLaterSourceEditsOrClearing()
    {
        var original = Item(); var source = new List<AgendaRetrospectiveTask> { original };
        var report = Builder().Build(Monday, source, ClosedAt, Zone);
        source[0] = original with { Task = original.Task with { Title = "Depois", Status = AgendaTaskStatus.Completed } }; source.Clear();
        Assert.Equal(original, Assert.Single(report.Tasks)); Assert.Equal(1, report.PendingCount); Assert.Equal(0, report.CompletedInWeekCount);
    }

    [Fact]
    public void EveryTask_IsAccountedForWithoutHidingUnknownOrOutsideDates()
    {
        var report = Build(Item(), Item(), Item(status: AgendaTaskStatus.InProgress), Item(status: AgendaTaskStatus.Completed, at: At(Monday, 10)),
            Item(status: AgendaTaskStatus.Completed), Item(status: AgendaTaskStatus.Completed, at: At(Monday.AddDays(7), 10)));
        Assert.Equal(6, report.PlannedCount); Assert.Equal(2, report.PendingCount); Assert.Equal(1, report.InProgressCount);
        Assert.Equal(1, report.CompletedInWeekCount); Assert.Equal(1, report.CompletedWithoutDateCount); Assert.Equal(1, report.CompletedOutsideWeekCount);
        Assert.Equal(report.PlannedCount, report.PendingCount + report.InProgressCount + report.CompletedInWeekCount + report.CompletedWithoutDateCount + report.CompletedOutsideWeekCount);
        Assert.Equal(report.PlannedCount, report.Days.Sum(day => day.PlannedCount) + report.CarriedInCount);
        Assert.Equal(report.PlannedCount, report.Categories.Sum(c => c.PlannedCount)); Assert.Equal(report.CompletedInWeekCount, report.Categories.Sum(c => c.CompletedCount));
    }

    [Fact]
    public void ClosingReport_DoesNotModifyPlanningOrCompleteOpenTasks()
    {
        var original = Item() with { Task = TaskOn(Monday) with { PlannedEndDate = Sunday, StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) } };
        var report = Build(original); Assert.Equal(original, Assert.Single(report.Tasks)); Assert.Equal(AgendaTaskStatus.Pending, original.Task.Status);
        Assert.Null(original.LastCompletedAt); Assert.Equal(1, report.PendingCount);
    }

    [Fact]
    public void NullCollectionOrTimeZone_IsRejected()
    {
        var builder = Builder(); Assert.Throws<ArgumentNullException>(() => builder.Build(Monday, null!, ClosedAt, Zone));
        Assert.Throws<ArgumentNullException>(() => builder.Build(Monday, [], ClosedAt, null!));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("snapshot")]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("dates")]
    [InlineData("category")]
    public void InvalidInputs_AreRejectedEvenOutsideTheSelectedWeek(string fault)
    {
        var task = TaskOn(Monday.AddDays(-10));
        AgendaRetrospectiveTask item = fault switch
        {
            "null" => null!,
            "snapshot" => new(null!, null),
            "title" => new(task with { Title = " " }, null),
            "description" => new(task with { Description = null! }, null),
            "dates" => new(task with { PlannedEndDate = task.PlannedDate.AddDays(-1) }, null),
            _ => new(task with { Category = (AgendaTaskCategory)99 }, null)
        };
        var builder = Builder(); Assert.Throws<ArgumentException>(() => builder.Build(Monday, [item], ClosedAt, Zone));
    }

    [Fact]
    public void DuplicateIdentity_IsRejectedInsteadOfInflatingTheIndicators()
    {
        var item = Item(); var builder = Builder(); Assert.Throws<ArgumentException>(() => builder.Build(Monday, [item, item with { Task = item.Task with { Title = "Duplicada" } }], ClosedAt, Zone));
    }

    [Fact]
    public void CompletionAfterCapture_IsRejectedRatherThanPresentedAsKnownData()
    {
        var builder = Builder(); Assert.Throws<ArgumentException>(() => builder.Build(Monday, [Item(status: AgendaTaskStatus.Completed, at: ClosedAt.AddMinutes(1))], ClosedAt, Zone));
    }

    [Fact]
    public void UnrepresentableWeek_IsRejectedWithoutReturningAPartialReport()
    {
        var builder = Builder(); Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(DateOnly.MaxValue, [], ClosedAt, Zone));
    }

    private static AgendaWeekRetrospective Build(params AgendaRetrospectiveTask[] items) => Builder().Build(Monday, items, ClosedAt, Zone);
    private static DateTimeOffset At(DateOnly date, int hour) => new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.FromHours(-3));
    private static AgendaTaskSnapshot TaskOn(DateOnly date) => new(Guid.NewGuid(), "Tarefa fictícia", "Planejamento original", date, new TimeOnly(9, 0), new TimeOnly(10, 0), AgendaTaskCategory.Work);
    private static AgendaRetrospectiveTask Item(AgendaTaskCategory category = AgendaTaskCategory.Work, AgendaTaskStatus status = AgendaTaskStatus.Pending, DateTimeOffset? at = null) => new(TaskOn(Monday) with { Category = category, Status = status }, at);
    private static IAgendaRetrospectiveBuilder Builder()
    {
        var type = typeof(IAgendaRetrospectiveBuilder).Assembly.GetType("Trama.Application.Agenda.AgendaRetrospectiveBuilder"); Assert.NotNull(type);
        return Assert.IsAssignableFrom<IAgendaRetrospectiveBuilder>(Activator.CreateInstance(type));
    }
}
