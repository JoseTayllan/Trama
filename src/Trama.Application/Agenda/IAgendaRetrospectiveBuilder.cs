namespace Trama.Application.Agenda;

public interface IAgendaRetrospectiveBuilder
{
    AgendaWeekRetrospective Build(DateOnly referenceDate, IReadOnlyList<AgendaRetrospectiveTask> tasks,
        DateTimeOffset closedAt, TimeZoneInfo timeZone);
}
