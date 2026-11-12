namespace Trama.Application.Agenda;

public enum AgendaComparisonStatus { Available, PreviousMissing, NoPercentageBase, DifferentTimeZones }

public sealed record AgendaRetrospectiveComparison(
    AgendaComparisonStatus Status,
    decimal? PercentagePointsDelta,
    int? PlannedDelta,
    int? CompletedDelta,
    int? OpenDelta,
    bool HasUndatedCompletions,
    bool HasOutsidePeriodCompletions,
    bool HasCarriedTasks);

public interface IAgendaRetrospectiveComparer
{
    AgendaRetrospectiveComparison Compare(AgendaWeekRetrospective current, AgendaWeekRetrospective? previous);
}
