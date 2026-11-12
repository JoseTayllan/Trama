namespace Trama.Application.Agenda;

public sealed record AgendaWeekReflection(DateOnly WeekStart, string WorkedWell, string Adjustments, DateTimeOffset UpdatedAt);

public enum AgendaReflectionSaveStatus { Saved, ValidationFailed, ClosureMissing, PersistenceFailed }
public sealed record AgendaReflectionSaveResult(AgendaReflectionSaveStatus Status);

public interface IAgendaReflectionRepository
{
    Task<bool> HasClosureAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
    Task SaveAsync(AgendaWeekReflection reflection, CancellationToken cancellationToken = default);
}
