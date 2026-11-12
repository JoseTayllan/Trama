using System.Globalization;
using Avalonia.Data.Converters;
using Trama.Application.Agenda;

namespace Trama.Desktop.Views;

public sealed class AgendaPresentationConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => parameter?.ToString() switch
    {
        "personal" => value is AgendaTaskCategory.Personal,
        "work" => value is AgendaTaskCategory.Work,
        "study" => value is AgendaTaskCategory.Study,
        "category" => value switch { AgendaTaskCategory.Work => "Trabalho", AgendaTaskCategory.Study => "Estudo", _ => "Pessoal" },
        "status" => StatusLabel(value),
        "closure-status" => $"No fechamento: {StatusLabel(value)}",
        "status-icon" => value switch { AgendaTaskStatus.Completed => "✓", AgendaTaskStatus.InProgress => "◐", AgendaTaskStatus.Pending => "○", _ => string.Empty },
        "week-day-name" => value is AgendaWeekDay day ? day.Date.ToString("ddd", CultureInfo.GetCultureInfo("pt-BR")).TrimEnd('.').ToUpperInvariant() : string.Empty,
        "week-day-number" => value is AgendaWeekDay numbered ? numbered.Date.ToString("dd", CultureInfo.InvariantCulture) : string.Empty,
        "week-create-name" => value is AgendaWeekDay create ? $"Criar tarefa em {create.Date:dd/MM/yyyy}" : string.Empty,
        "week-card-name" => value is AgendaWeekEntry card ? $"{card.Task.Title}, {card.Task.Status switch { AgendaTaskStatus.Completed => "Concluída", AgendaTaskStatus.InProgress => "Em andamento", _ => "Pendente" }}, {card.Date:dd/MM/yyyy}" : string.Empty,
        "week-continuation" => value is AgendaWeekEntry entry ? entry.ContinuesFromPreviousDay ? "Continuação" : entry.ContinuesToNextDay ? "Continua amanhã" : string.Empty : string.Empty,
        "week-time" => value is AgendaWeekEntry timed ? (timed.StartTime, timed.EndTime) switch
        {
            ({ } start, { } end) => $"{start:HH:mm} – {end:HH:mm}",
            ({ } start, null) => $"{start:HH:mm} · início",
            (null, { } end) => $"{end:HH:mm} · fim",
            _ => timed.ContinuesFromPreviousDay || timed.ContinuesToNextDay ? string.Empty : "Sem horário"
        } : string.Empty,
        "time" => TimeLabel(value),
        "planning" => value is AgendaTaskSnapshot task ? task.PlannedEndDate is { } endDate && endDate != task.PlannedDate
            ? $"Planejada: {TimeLabel(task)}" : $"Planejada: {task.PlannedDate:dd/MM/yyyy} · {TimeLabel(task)}" : string.Empty,
        "has-value" => value is not null,
        _ => string.Empty
    };

    internal static string StatusLabel(object? value) => value switch
    {
        AgendaTaskStatus.Completed => "Concluída",
        AgendaTaskStatus.InProgress => "Em andamento",
        _ => "Pendente"
    };

    private static string TimeLabel(object? value) => value switch
    {
        AgendaTaskSnapshot { PlannedEndDate: { } endDate } task when endDate != task.PlannedDate =>
            $"{task.PlannedDate:dd/MM/yyyy} {task.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "sem horário"} → {endDate:dd/MM/yyyy} {task.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "sem horário"}",
        AgendaTaskSnapshot { StartTime: { } start, EndTime: { } end } => $"{start:HH:mm} – {end:HH:mm}",
        AgendaTaskSnapshot { StartTime: { } start } => $"{start:HH:mm} · sem fim",
        _ => "Sem horário"
    };

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count == 2 && values[0] is Guid id && values[1] is IReadOnlySet<Guid> ids && ids.Contains(id);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
