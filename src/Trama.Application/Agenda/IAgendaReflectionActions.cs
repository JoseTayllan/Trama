namespace Trama.Application.Agenda;

public interface IAgendaReflectionActions : IAgendaReflectionReader
{
    Task<AgendaReflectionSaveResult> SaveAsync(DateOnly weekStart, string? workedWell, string? adjustments,
        CancellationToken cancellationToken = default);
}
