using System.Globalization;
using Avalonia.Data.Converters;
using Trama.Application.Agenda;

namespace Trama.Desktop.Views;

public sealed class AgendaPresentationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => parameter?.ToString() switch
    {
        "personal" => value is AgendaTaskCategory.Personal,
        "work" => value is AgendaTaskCategory.Work,
        "study" => value is AgendaTaskCategory.Study,
        "category" => value switch { AgendaTaskCategory.Work => "Trabalho", AgendaTaskCategory.Study => "Estudo", _ => "Pessoal" },
        "status" => value switch { AgendaTaskStatus.Completed => "Concluída", AgendaTaskStatus.InProgress => "Em andamento", _ => "Pendente" },
        "time" => value switch
        {
            AgendaTaskSnapshot { PlannedEndDate: { } endDate } task when endDate != task.PlannedDate =>
                $"{task.PlannedDate:dd/MM/yyyy} {task.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "sem horário"} → {endDate:dd/MM/yyyy} {task.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "sem horário"}",
            AgendaTaskSnapshot { StartTime: { } start, EndTime: { } end } => $"{start:HH:mm} – {end:HH:mm}",
            AgendaTaskSnapshot { StartTime: { } start } => $"{start:HH:mm} · sem fim",
            _ => "Sem horário"
        },
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
