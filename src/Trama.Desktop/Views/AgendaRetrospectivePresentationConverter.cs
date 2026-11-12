using System.Globalization;
using Avalonia.Data.Converters;
using Trama.Application.Agenda;

namespace Trama.Desktop.Views;

public sealed class AgendaRetrospectivePresentationConverter : IValueConverter, IMultiValueConverter
{
    private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value, parameter) switch
    {
        (AgendaRetrospectiveDay day, "day") => day.Date.ToString("ddd dd", Portuguese),
        (AgendaRetrospectiveDay day, "day-counts") => $"{day.PlannedCount} / {day.CompletedCount}",
        (AgendaRetrospectiveDay day, "planned-name") => $"{day.Date:dd/MM}: {day.PlannedCount} planejadas",
        (AgendaRetrospectiveDay day, "completed-name") => $"{day.Date:dd/MM}: {day.CompletedCount} concluídas",
        (AgendaRetrospectiveCategory category, "category-counts") => $"{category.CompletedCount} de {category.PlannedCount} concluídas",
        (AgendaRetrospectiveCategory category, "category-max") => (double)Math.Max(1, category.PlannedCount),
        _ => null
    };
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2 && values[0] is Guid id &&
            values[1] is IReadOnlyDictionary<Guid, AgendaTaskStatus?> statuses &&
            statuses.TryGetValue(id, out var status))
            return status is { } current ? $"Atual: {AgendaPresentationConverter.StatusLabel(current)}" : "Atual: Indisponível";

        return "Atual: Não consultado";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
