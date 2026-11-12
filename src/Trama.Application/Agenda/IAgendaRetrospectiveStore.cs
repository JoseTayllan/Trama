namespace Trama.Application.Agenda;

public interface IAgendaRetrospectiveStore
{
    Task<bool> TryAddAsync(AgendaWeekRetrospective report, TimeZoneInfo timeZone, CancellationToken cancellationToken = default);
    Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default);
}
