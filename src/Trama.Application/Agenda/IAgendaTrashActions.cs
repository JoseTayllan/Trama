namespace Trama.Application.Agenda;

public sealed record AgendaTaskTrashEntry(AgendaTaskSnapshot Task, DateTimeOffset DeletedAt, DateTimeOffset? LastCompletedAt);

public interface IAgendaTrashActions
{
    Task MoveToTrashAsync(Guid taskId, DateTimeOffset deletedAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaTaskTrashEntry>> ListAsync(CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid taskId, CancellationToken cancellationToken = default);
}
