namespace Trama.Application.Agenda;

// The adapter must complete the selection atomically, preserving planned fields.
public interface IAgendaTaskCompletionRepository
{
    Task CompleteAsync(IReadOnlyList<Guid> taskIds, DateTimeOffset completedAt, CancellationToken cancellationToken = default);
}

public enum AgendaTaskCompletionStatus { Completed, ValidationFailed, PersistenceFailed }
public sealed record AgendaTaskCompletionResult(AgendaTaskCompletionStatus Status);

public sealed class CompleteAgendaTasks
{
    private readonly IAgendaTaskCompletionRepository _repository;

    public CompleteAgendaTasks(IAgendaTaskCompletionRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<AgendaTaskCompletionResult> ExecuteAsync(IReadOnlyList<AgendaTaskSnapshot> selectedTasks,
        DateTimeOffset completedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedTasks);
        cancellationToken.ThrowIfCancellationRequested();

        var tasks = selectedTasks.ToArray();
        var localDay = DateOnly.FromDateTime(completedAt.DateTime);
        if (tasks.Length == 0 || tasks.Any(task => task is null || !AgendaTaskValidation.IsValid(task) ||
            task.Status == AgendaTaskStatus.Completed || task.PlannedDate > localDay) ||
            tasks.Select(task => task.Id).Distinct().Count() != tasks.Length)
        {
            return new AgendaTaskCompletionResult(AgendaTaskCompletionStatus.ValidationFailed);
        }

        try
        {
            await _repository.CompleteAsync(tasks.Select(task => task.Id).ToArray(), completedAt, cancellationToken);
            return new AgendaTaskCompletionResult(AgendaTaskCompletionStatus.Completed);
        }
        catch (AgendaPersistenceException)
        {
            return new AgendaTaskCompletionResult(AgendaTaskCompletionStatus.PersistenceFailed);
        }
    }
}
