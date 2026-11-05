namespace Trama.Application.Agenda;

public interface IAgendaTaskReader
{
    Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaTaskSnapshot>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default);
}
