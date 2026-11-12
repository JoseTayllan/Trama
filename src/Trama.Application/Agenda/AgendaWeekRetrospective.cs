namespace Trama.Application.Agenda;

public sealed record AgendaRetrospectiveTask(AgendaTaskSnapshot Task, DateTimeOffset? LastCompletedAt);
public sealed record AgendaRetrospectiveDay(DateOnly Date, int PlannedCount, int CompletedCount);
public sealed record AgendaRetrospectiveCategory(AgendaTaskCategory Category, int PlannedCount, int CompletedCount);

public sealed record AgendaWeekRetrospective(
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset ClosedAt,
    string TimeZoneId,
    int PlannedCount,
    int CompletedInWeekCount,
    int PendingCount,
    int InProgressCount,
    int CompletedWithoutDateCount,
    int CompletedOutsideWeekCount,
    int CarriedInCount,
    decimal? CompletionPercentage,
    IReadOnlyList<AgendaRetrospectiveTask> Tasks,
    IReadOnlyList<AgendaRetrospectiveDay> Days,
    IReadOnlyList<AgendaRetrospectiveCategory> Categories);
