using System.Globalization;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveComparisonTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("RetrospectiveComparisonTests", TimeSpan.FromHours(-3), "Test", "Test");

    [Fact]
    public void PercentAndCountDeltas_AreSeparateAndUseSavedReports()
    {
        var current = Report(Monday, 4, 3); var previous = Report(Monday.AddDays(-7), 8, 4);
        var comparison = Comparer().Compare(current, previous);
        Assert.Equal(AgendaComparisonStatus.Available, comparison.Status); Assert.Equal(25m, comparison.PercentagePointsDelta);
        Assert.Equal(-4, comparison.PlannedDelta); Assert.Equal(-1, comparison.CompletedDelta); Assert.Equal(-3, comparison.OpenDelta);
    }

    [Theory]
    [InlineData(1, 3, "-50")]
    [InlineData(2, 2, "0")]
    [InlineData(3, 1, "50")]
    public void PercentagePoints_PreserveNegativeNeutralAndPositiveValues(int currentDone, int previousDone, string delta)
    {
        var comparison = Comparer().Compare(Report(Monday, 4, currentDone), Report(Monday.AddDays(-7), 4, previousDone));
        Assert.Equal(decimal.Parse(delta, CultureInfo.InvariantCulture), comparison.PercentagePointsDelta);
    }

    [Fact]
    public void Delta_UsesTheDisplayedStoredPercentagesInsteadOfRecomputingTheirRatios()
    {
        var current = Report(Monday, 3, 2); var previous = Report(Monday.AddDays(-7), 3, 1);
        Assert.Equal(66.7m, current.CompletionPercentage); Assert.Equal(33.3m, previous.CompletionPercentage);
        Assert.Equal(33.4m, Comparer().Compare(current, previous).PercentagePointsDelta);
    }

    [Fact]
    public void MissingPreviousReport_IsNotTreatedAsZeroPercent()
    {
        var comparison = Comparer().Compare(Report(Monday, 4, 3), null);
        Assert.Equal(AgendaComparisonStatus.PreviousMissing, comparison.Status); Assert.Null(comparison.PercentagePointsDelta);
        Assert.Null(comparison.PlannedDelta); Assert.Null(comparison.CompletedDelta); Assert.Null(comparison.OpenDelta);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 0)]
    [InlineData(0, 0)]
    public void EmptyWeeks_HaveNoPercentageDeltaButStillAllowCountComparison(int currentCount, int previousCount)
    {
        var comparison = Comparer().Compare(Report(Monday, currentCount, 0), Report(Monday.AddDays(-7), previousCount, 0));
        Assert.Equal(AgendaComparisonStatus.NoPercentageBase, comparison.Status); Assert.Null(comparison.PercentagePointsDelta);
        Assert.Equal(currentCount - previousCount, comparison.PlannedDelta); Assert.Equal(0, comparison.CompletedDelta);
        Assert.Equal(currentCount - previousCount, comparison.OpenDelta);
    }

    [Fact]
    public void DifferentRecordedTimeZoneIds_DisableDirectComparison()
    {
        var other = TimeZoneInfo.CreateCustomTimeZone("AnotherRecordedZone", TimeSpan.FromHours(2), "Other", "Other");
        var comparison = Comparer().Compare(Report(Monday, 4, 3), Report(Monday.AddDays(-7), 4, 2, zone: other));
        Assert.Equal(AgendaComparisonStatus.DifferentTimeZones, comparison.Status); Assert.Null(comparison.PercentagePointsDelta);
        Assert.Null(comparison.PlannedDelta); Assert.Null(comparison.CompletedDelta); Assert.Null(comparison.OpenDelta);
    }

    [Fact]
    public void DifferentCaptureOffsets_DoNotDisableComparisonWhenRecordedZoneIdMatches()
    {
        var current = Report(Monday, 4, 3); var previous = Report(Monday.AddDays(-7), 4, 2);
        previous = previous with { ClosedAt = previous.ClosedAt.ToUniversalTime() };
        Assert.Equal(AgendaComparisonStatus.Available, Comparer().Compare(current, previous).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-14)]
    [InlineData(7)]
    public void OnlyTheImmediatelyPreviousWeek_IsAccepted(int offset)
    {
        var comparer = Comparer(); Assert.Throws<ArgumentException>(() => comparer.Compare(Report(Monday, 1, 0), Report(Monday.AddDays(offset), 1, 0)));
    }

    [Fact]
    public void NullCurrentReport_IsRejected()
    {
        var comparer = Comparer(); Assert.Throws<ArgumentNullException>(() => comparer.Compare(null!, null));
    }

    [Fact]
    public void FirstRepresentableWeek_WithNoPreviousReport_DoesNotUnderflow()
    {
        var comparison = Comparer().Compare(Report(DateOnly.MinValue, 0, 0), null); Assert.Equal(AgendaComparisonStatus.PreviousMissing, comparison.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingDataInEitherWeek_IsFlaggedWithoutInventingCompletionDates(bool previousOnly)
    {
        var current = Report(Monday, 3, 1, undated: previousOnly ? 0 : 1);
        var previous = Report(Monday.AddDays(-7), 3, 1, undated: previousOnly ? 1 : 0);
        var comparison = Comparer().Compare(current, previous); Assert.True(comparison.HasUndatedCompletions);
        Assert.Equal(0m, comparison.PercentagePointsDelta); Assert.Equal(AgendaComparisonStatus.Available, comparison.Status);
    }

    [Fact]
    public void OutsideDatesAndContinuations_AreFlaggedFromBothReports()
    {
        var current = Report(Monday, 3, 1, outside: 1); var previous = Report(Monday.AddDays(-7), 3, 1, carried: 1);
        var comparison = Comparer().Compare(current, previous); Assert.True(comparison.HasOutsidePeriodCompletions); Assert.True(comparison.HasCarriedTasks);
    }

    [Fact]
    public void MissingPreviousReport_StillPreservesCurrentDataQualityFlags()
    {
        var comparison = Comparer().Compare(Report(Monday, 4, 1, undated: 1, outside: 1, carried: 1), null);
        Assert.True(comparison.HasUndatedCompletions); Assert.True(comparison.HasOutsidePeriodCompletions); Assert.True(comparison.HasCarriedTasks);
    }

    [Theory]
    [InlineData("8.3", "+8,3 p.p.")]
    [InlineData("-8.3", "-8,3 p.p.")]
    [InlineData("0", "Sem alteração")]
    public void Summary_FormatsPercentagePointsWithSignAndPortugueseDecimal(string delta, string expected)
    {
        var comparison = new AgendaRetrospectiveComparison(AgendaComparisonStatus.Available, decimal.Parse(delta, CultureInfo.InvariantCulture), 0, 0, 0, false, false, false);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US"); var summary = Summary(Report(Monday, 4, 3), comparison);
            Assert.Contains(expected, Get<string>(summary, "ComparisonLabel"), StringComparison.OrdinalIgnoreCase);
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    [Theory]
    [InlineData(AgendaComparisonStatus.PreviousMissing, "Sem fechamento da semana anterior")]
    [InlineData(AgendaComparisonStatus.NoPercentageBase, "Sem base percentual")]
    [InlineData(AgendaComparisonStatus.DifferentTimeZones, "Fusos registrados diferentes")]
    public void Summary_ExplainsUnavailableComparisonWithoutShowingAFalseZero(AgendaComparisonStatus status, string expected)
    {
        var comparison = new AgendaRetrospectiveComparison(status, null, null, null, null, false, false, false);
        var summary = Summary(Report(Monday, 4, 3), comparison); var text = Get<string>(summary, "ComparisonLabel");
        Assert.Contains(expected, text, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("0 p.p.", text);
    }

    [Fact]
    public void Summary_SeparatesOpenTasksFromUndatedAndOutsideCompletions()
    {
        var report = Report(Monday, 8, 2, undated: 1, outside: 1, inProgress: 2); var summary = Summary(report, Comparer().Compare(report, null));
        Assert.Equal(4, Get<int>(summary, "OpenCount")); Assert.Equal(2, Get<int>(summary, "CompletedCount"));
        Assert.Equal(4, Get<IReadOnlyList<AgendaRetrospectiveTask>>(summary, "OpenTasks").Count);
        Assert.All(Get<IReadOnlyList<AgendaRetrospectiveTask>>(summary, "OpenTasks"), item => Assert.NotEqual(AgendaTaskStatus.Completed, item.Task.Status));
        Assert.Contains("2 pendentes", Get<string>(summary, "OpenBreakdownLabel")); Assert.Contains("2 em andamento", Get<string>(summary, "OpenBreakdownLabel"));
    }

    [Fact]
    public void Summary_ExplainsAllCurrentDataQualityCounters()
    {
        var report = Report(Monday, 4, 1, undated: 1, outside: 1, carried: 1); var summary = Summary(report, Comparer().Compare(report, null));
        var messages = Get<IReadOnlyList<string>>(summary, "DataQualityMessages");
        Assert.Contains(messages, text => text.Contains("sem data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, text => text.Contains("fora", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, text => text.Contains("antes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Summary_PreviousOnlyMissingData_IsNotPresentedAsCurrentMissingTasks()
    {
        var current = Report(Monday, 3, 1); var previous = Report(Monday.AddDays(-7), 3, 1, undated: 1);
        var summary = Summary(current, Comparer().Compare(current, previous));
        Assert.Contains(Get<IReadOnlyList<string>>(summary, "DataQualityMessages"), text => text.Contains("anterior", StringComparison.OrdinalIgnoreCase) && text.Contains("sem data", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, Get<int>(summary, "OpenCount"));
    }

    [Theory]
    [InlineData(0, 0, "Sem tarefas")]
    [InlineData(4, 3, "75%")]
    [InlineData(3, 2, "66,7%")]
    public void Summary_DistinguishesEmptyFromNumericPercentages(int planned, int completed, string expected)
    {
        var report = Report(Monday, planned, completed); var summary = Summary(report, Comparer().Compare(report, null)); Assert.Equal(expected, Get<string>(summary, "CompletionLabel"));
    }

    [Fact]
    public void Summary_PreservesCapturedDaysCategoriesAndOpenTaskIdentities()
    {
        var report = Report(Monday, 4, 1, carried: 1); var before = report.Tasks.ToArray(); var summary = Summary(report, Comparer().Compare(report, null));
        Assert.Equal(report.Days, Get<IReadOnlyList<AgendaRetrospectiveDay>>(summary, "Days")); Assert.Equal(report.Categories, Get<IReadOnlyList<AgendaRetrospectiveCategory>>(summary, "Categories"));
        Assert.Equal(before.Where(item => item.Task.Status != AgendaTaskStatus.Completed), Get<IReadOnlyList<AgendaRetrospectiveTask>>(summary, "OpenTasks"));
        Assert.Equal(before, report.Tasks);
    }

    private static AgendaWeekRetrospective Report(DateOnly start, int planned, int completed, int undated = 0, int outside = 0, int carried = 0, int inProgress = 0, TimeZoneInfo? zone = null)
    {
        zone ??= Zone; var items = new List<AgendaRetrospectiveTask>();
        for (var index = 0; index < planned; index++)
        {
            var status = index < completed + undated + outside ? AgendaTaskStatus.Completed : index < completed + undated + outside + inProgress ? AgendaTaskStatus.InProgress : AgendaTaskStatus.Pending;
            var isCarried = index >= planned - carried; var date = isCarried ? start.AddDays(-1) : start;
            var task = new AgendaTaskSnapshot(Guid.NewGuid(), "Tarefa fictícia", "Captura preservada", date, Category: (AgendaTaskCategory)(index % 3), Status: status)
            { PlannedEndDate = isCarried ? start.AddDays(2) : null };
            DateTimeOffset? actual = index < completed ? At(start.AddDays(1), 10, zone) : index >= completed + undated && index < completed + undated + outside ? At(start.AddDays(7), 9, zone) : null;
            items.Add(new(task, actual));
        }
        return new AgendaRetrospectiveBuilder().Build(start, items, At(start.AddDays(7), 12, zone), zone);
    }
    private static DateTimeOffset At(DateOnly date, int hour, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(new TimeOnly(hour, 0)); return new(local, zone.GetUtcOffset(local));
    }
    private static IAgendaRetrospectiveComparer Comparer()
    {
        var type = typeof(IAgendaRetrospectiveComparer).Assembly.GetType("Trama.Application.Agenda.AgendaRetrospectiveComparer"); Assert.NotNull(type);
        return Assert.IsAssignableFrom<IAgendaRetrospectiveComparer>(Activator.CreateInstance(type));
    }
    private static object Summary(AgendaWeekRetrospective report, AgendaRetrospectiveComparison comparison)
    {
        var type = typeof(AgendaViewModel).Assembly.GetType("Trama.Desktop.ViewModels.AgendaRetrospectiveSummaryViewModel"); Assert.NotNull(type);
        var instance = Activator.CreateInstance(type, report, comparison); Assert.NotNull(instance); return instance;
    }
    private static T Get<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(name); Assert.NotNull(property); return Assert.IsAssignableFrom<T>(property.GetValue(source));
    }
}
