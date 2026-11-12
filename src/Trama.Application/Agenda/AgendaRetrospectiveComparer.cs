namespace Trama.Application.Agenda;

public sealed class AgendaRetrospectiveComparer : IAgendaRetrospectiveComparer
{
    public AgendaRetrospectiveComparison Compare(AgendaWeekRetrospective current, AgendaWeekRetrospective? previous)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (previous is not null && current.StartDate.DayNumber - previous.StartDate.DayNumber != 7)
            throw new ArgumentException("O fechamento deve ser da semana imediatamente anterior.", nameof(previous));

        var undated = current.CompletedWithoutDateCount > 0 || previous?.CompletedWithoutDateCount > 0;
        var outside = current.CompletedOutsideWeekCount > 0 || previous?.CompletedOutsideWeekCount > 0;
        var carried = current.CarriedInCount > 0 || previous?.CarriedInCount > 0;
        if (previous is null)
            return new(AgendaComparisonStatus.PreviousMissing, null, null, null, null, undated, outside, carried);
        if (!string.Equals(current.TimeZoneId, previous.TimeZoneId, StringComparison.Ordinal))
            return new(AgendaComparisonStatus.DifferentTimeZones, null, null, null, null, undated, outside, carried);

        var delta = current.CompletionPercentage - previous.CompletionPercentage;
        return new(delta.HasValue ? AgendaComparisonStatus.Available : AgendaComparisonStatus.NoPercentageBase,
            delta, current.PlannedCount - previous.PlannedCount,
            current.CompletedInWeekCount - previous.CompletedInWeekCount,
            current.PendingCount + current.InProgressCount - previous.PendingCount - previous.InProgressCount,
            undated, outside, carried);
    }
}
