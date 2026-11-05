namespace Trama.Application.Agenda;

public interface IAgendaActions : IAgendaTaskReader
{
    Task<AgendaTaskSaveResult> SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default);
}
