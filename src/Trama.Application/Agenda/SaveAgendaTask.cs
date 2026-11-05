namespace Trama.Application.Agenda;

public sealed class SaveAgendaTask
{
    private readonly IAgendaTaskRepository _repository;

    public SaveAgendaTask(IAgendaTaskRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<AgendaTaskSaveResult> ExecuteAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();

        if (!AgendaTaskValidation.IsValid(task))
        {
            return new AgendaTaskSaveResult(AgendaTaskSaveStatus.ValidationFailed);
        }

        try
        {
            await _repository.SaveAsync(task, cancellationToken);
            return new AgendaTaskSaveResult(AgendaTaskSaveStatus.Saved);
        }
        catch (AgendaPersistenceException)
        {
            return new AgendaTaskSaveResult(AgendaTaskSaveStatus.PersistenceFailed);
        }
    }
}
