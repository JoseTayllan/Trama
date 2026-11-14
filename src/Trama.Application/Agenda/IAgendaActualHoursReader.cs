namespace Trama.Application.Agenda;

public interface IAgendaActualHoursReader
{
    Task<TimeSpan?> ReadActualHoursAsync(DateTimeOffset from, DateTimeOffset until,
        CancellationToken cancellationToken = default);
}
