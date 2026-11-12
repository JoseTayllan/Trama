namespace Trama.Application.Agenda;

public interface IAgendaRetrospectiveSource
{
    Task<IReadOnlyList<AgendaRetrospectiveTask>> ListOverlappingAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default);
}
