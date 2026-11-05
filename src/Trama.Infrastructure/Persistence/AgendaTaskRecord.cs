using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

internal sealed class AgendaTaskRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly PlannedDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public AgendaTaskCategory Category { get; set; }
    public AgendaTaskStatus Status { get; set; }
    public DateTimeOffset? LastCompletedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
