namespace Trama.Application.Agenda;

public sealed class AgendaRetrospectiveBuilder : IAgendaRetrospectiveBuilder
{
    public AgendaWeekRetrospective Build(DateOnly referenceDate, IReadOnlyList<AgendaRetrospectiveTask> tasks,
        DateTimeOffset closedAt, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(timeZone);
        // Keep the same calendar boundaries and representable-week limits as the agenda.
        var week = new AgendaWeekProjector().Build(referenceDate, Array.Empty<AgendaTaskSnapshot>());
        var captureDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(closedAt, timeZone).DateTime);
        if (captureDate <= week.EndDate)
            throw new ArgumentException("The week must have ended before capturing its retrospective.", nameof(closedAt));

        var input = tasks.ToArray();
        var identities = new HashSet<Guid>();
        foreach (var item in input)
        {
            if (item?.Task is not { } task || !AgendaTaskValidation.IsValid(task) || task.Description is null ||
                !identities.Add(task.Id) || item.LastCompletedAt > closedAt)
                throw new ArgumentException("Retrospective tasks must be valid, distinct and known at capture time.", nameof(tasks));
        }

        var captured = input.Where(item => item.Task.PlannedDate <= week.EndDate &&
                (item.Task.PlannedEndDate ?? item.Task.PlannedDate) >= week.StartDate)
            .OrderBy(item => item.Task.PlannedDate)
            .ThenBy(item => item.Task.StartTime is null).ThenBy(item => item.Task.StartTime)
            .ThenBy(item => item.Task.Title, StringComparer.Ordinal).ThenBy(item => item.Task.Id)
            .ToArray();
        var plannedByDay = new int[7];
        var completedByDay = new int[7];
        var categories = Enum.GetValues<AgendaTaskCategory>();
        var plannedByCategory = categories.ToDictionary(category => category, _ => 0);
        var completedByCategory = categories.ToDictionary(category => category, _ => 0);
        var carriedIn = 0;
        var pending = 0;
        var inProgress = 0;
        var completed = 0;
        var withoutDate = 0;
        var outsideWeek = 0;

        foreach (var item in captured)
        {
            var task = item.Task;
            plannedByCategory[task.Category]++;
            if (task.PlannedDate < week.StartDate) carriedIn++;
            else plannedByDay[task.PlannedDate.DayNumber - week.StartDate.DayNumber]++;

            if (task.Status == AgendaTaskStatus.Pending) { pending++; continue; }
            if (task.Status == AgendaTaskStatus.InProgress) { inProgress++; continue; }
            if (item.LastCompletedAt is not { } completedAt) { withoutDate++; continue; }

            var completedDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(completedAt, timeZone).DateTime);
            if (completedDate < week.StartDate || completedDate > week.EndDate) { outsideWeek++; continue; }
            completed++;
            completedByDay[completedDate.DayNumber - week.StartDate.DayNumber]++;
            completedByCategory[task.Category]++;
        }

        var days = Enumerable.Range(0, 7).Select(offset => new AgendaRetrospectiveDay(
            week.StartDate.AddDays(offset), plannedByDay[offset], completedByDay[offset])).ToArray();
        var categoryTotals = categories.Select(category => new AgendaRetrospectiveCategory(
            category, plannedByCategory[category], completedByCategory[category])).ToArray();
        decimal? percentage = captured.Length == 0 ? null : decimal.Round(completed * 100m / captured.Length, 1, MidpointRounding.AwayFromZero);

        return new AgendaWeekRetrospective(week.StartDate, week.EndDate, closedAt, timeZone.Id,
            captured.Length, completed, pending, inProgress, withoutDate, outsideWeek, carriedIn, percentage,
            Array.AsReadOnly(captured), Array.AsReadOnly(days), Array.AsReadOnly(categoryTotals));
    }
}
