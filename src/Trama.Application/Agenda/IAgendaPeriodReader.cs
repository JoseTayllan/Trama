namespace Trama.Application.Agenda;

public interface IAgendaPeriodReader
{
    Task<IReadOnlyList<AgendaTaskSnapshot>> ListOverlappingAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default);
}
