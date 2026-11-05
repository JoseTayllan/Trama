namespace Trama.Application.Agenda;

public static class AgendaTaskValidation
{
    public static bool IsValid(AgendaTaskSnapshot task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var endDate = task.PlannedEndDate ?? task.PlannedDate;
        return task.Id != Guid.Empty && !string.IsNullOrWhiteSpace(task.Title) &&
            Enum.IsDefined(task.Category) && Enum.IsDefined(task.Status) &&
            endDate >= task.PlannedDate &&
            (!task.EndTime.HasValue || task.StartTime.HasValue) &&
            !(endDate == task.PlannedDate && task.StartTime is { } start && task.EndTime is { } end && end <= start);
    }
}
