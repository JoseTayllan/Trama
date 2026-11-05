using System.Text.Json.Serialization;

namespace Trama.Application.Agenda;

public enum AgendaTaskCategory { Personal, Work, Study }
public enum AgendaTaskStatus { Pending, InProgress, Completed }

public sealed record AgendaTaskSnapshot(
    Guid Id,
    string Title,
    string Description,
    DateOnly PlannedDate,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    AgendaTaskCategory Category = AgendaTaskCategory.Personal,
    AgendaTaskStatus Status = AgendaTaskStatus.Pending)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? PlannedEndDate { get; init; }
}

public enum AgendaTaskSaveStatus { Saved, ValidationFailed, PersistenceFailed }
public sealed record AgendaTaskSaveResult(AgendaTaskSaveStatus Status);

