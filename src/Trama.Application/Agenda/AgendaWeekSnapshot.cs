namespace Trama.Application.Agenda;

public sealed record AgendaWeekEntry(AgendaTaskSnapshot Task, DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime,
    bool ContinuesFromPreviousDay, bool ContinuesToNextDay);

public sealed record AgendaWeekDay(DateOnly Date, IReadOnlyList<AgendaWeekEntry> Entries);

public sealed record AgendaWeekSnapshot(DateOnly StartDate, DateOnly EndDate, IReadOnlyList<AgendaWeekDay> Days, int UniqueTaskCount);
