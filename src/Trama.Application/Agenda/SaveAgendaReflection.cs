namespace Trama.Application.Agenda;

public sealed class SaveAgendaReflection
{
    private readonly IAgendaReflectionRepository _repository;
    private readonly TimeProvider _clock;

    public SaveAgendaReflection(IAgendaReflectionRepository repository, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(clock);
        _repository = repository;
        _clock = clock;
    }

    public async Task<AgendaReflectionSaveResult> ExecuteAsync(DateOnly weekStart, string? workedWell, string? adjustments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
            return new(AgendaReflectionSaveStatus.ValidationFailed);

        try
        {
            var hasClosure = await _repository.HasClosureAsync(weekStart, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!hasClosure) return new(AgendaReflectionSaveStatus.ClosureMissing);

            var reflection = new AgendaWeekReflection(weekStart, workedWell ?? string.Empty, adjustments ?? string.Empty,
                _clock.GetUtcNow().ToUniversalTime());
            cancellationToken.ThrowIfCancellationRequested();
            await _repository.SaveAsync(reflection, cancellationToken);
            return new(AgendaReflectionSaveStatus.Saved);
        }
        catch (AgendaPersistenceException)
        {
            return new(AgendaReflectionSaveStatus.PersistenceFailed);
        }
    }
}
