namespace Trama.Application.Agenda;

public interface IAgendaCompletionActions : IAgendaTaskCompletionReader
{
    Task<IReadOnlyList<AgendaTaskSnapshot>> ListCandidatesAsync(DateOnly through, CancellationToken cancellationToken = default);
    Task<AgendaTaskCompletionResult> CompleteAsync(IReadOnlyList<AgendaTaskSnapshot> selectedTasks,
        DateTimeOffset completedAt, CancellationToken cancellationToken = default);
}
