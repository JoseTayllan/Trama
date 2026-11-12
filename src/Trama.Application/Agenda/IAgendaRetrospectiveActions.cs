namespace Trama.Application.Agenda;

public interface IAgendaRetrospectiveActions
{
    Task<AgendaWeekRetrospective> GetOrCloseAsync(DateOnly referenceDate, CancellationToken cancellationToken = default);
    Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default);
}
