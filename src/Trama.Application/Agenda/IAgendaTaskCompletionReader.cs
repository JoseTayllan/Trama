namespace Trama.Application.Agenda;

public interface IAgendaTaskCompletionReader
{
    Task<DateTimeOffset?> GetLastCompletedAtAsync(Guid taskId, CancellationToken cancellationToken = default);
}
