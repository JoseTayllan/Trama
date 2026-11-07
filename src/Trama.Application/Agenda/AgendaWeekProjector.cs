namespace Trama.Application.Agenda;

public sealed class AgendaWeekProjector : IAgendaWeekProjector
{
    public AgendaWeekSnapshot Build(DateOnly referenceDate, IReadOnlyList<AgendaTaskSnapshot> tasks,
        AgendaTaskCategory? category = null, AgendaTaskStatus? status = null)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (category.HasValue && !Enum.IsDefined(category.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        var start = referenceDate.AddDays(-(((int)referenceDate.DayOfWeek + 6) % 7));
        if (start.DayNumber > DateOnly.MaxValue.DayNumber - 6)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceDate), "A complete week exceeds the supported date range.");
        }
        var end = start.AddDays(6);
        var input = tasks.ToArray();
        var identities = new HashSet<Guid>();
        foreach (var task in input)
        {
            if (task is null || !AgendaTaskValidation.IsValid(task) || !identities.Add(task.Id))
            {
                throw new ArgumentException("Tasks must be valid and have distinct identities.", nameof(tasks));
            }
        }

        var selected = input.Where(task => task.PlannedDate <= end && (task.PlannedEndDate ?? task.PlannedDate) >= start &&
                (!category.HasValue || task.Category == category.Value) && (!status.HasValue || task.Status == status.Value))
            .OrderBy(task => task.PlannedDate)
            .ThenBy(task => task.StartTime is null).ThenBy(task => task.StartTime)
            .ThenBy(task => task.Title, StringComparer.Ordinal).ThenBy(task => task.Id)
            .ToArray();

        var days = new AgendaWeekDay[7];
        for (var offset = 0; offset < days.Length; offset++)
        {
            var date = start.AddDays(offset);
            var entries = selected.Where(task => task.PlannedDate <= date && (task.PlannedEndDate ?? task.PlannedDate) >= date)
                .Select(task => new AgendaWeekEntry(task, date,
                    date == task.PlannedDate ? task.StartTime : null,
                    date == (task.PlannedEndDate ?? task.PlannedDate) ? task.EndTime : null,
                    date > task.PlannedDate, date < (task.PlannedEndDate ?? task.PlannedDate)))
                .ToArray();
            days[offset] = new AgendaWeekDay(date, Array.AsReadOnly(entries));
        }

        return new AgendaWeekSnapshot(start, end, Array.AsReadOnly(days), selected.Length);
    }
}
