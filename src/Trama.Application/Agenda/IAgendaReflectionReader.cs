namespace Trama.Application.Agenda;

public interface IAgendaReflectionReader
{
    Task<AgendaWeekReflection?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
}
