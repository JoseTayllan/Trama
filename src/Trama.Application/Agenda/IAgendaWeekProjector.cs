namespace Trama.Application.Agenda;

public interface IAgendaWeekProjector
{
    AgendaWeekSnapshot Build(DateOnly referenceDate, IReadOnlyList<AgendaTaskSnapshot> tasks,
        AgendaTaskCategory? category = null, AgendaTaskStatus? status = null);
}
