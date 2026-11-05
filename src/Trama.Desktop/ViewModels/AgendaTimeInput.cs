using System.Globalization;

namespace Trama.Desktop.ViewModels;

internal static class AgendaTimeInput
{
    public static bool TryParse(string? text, out TimeSpan? time)
    {
        time = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!TimeOnly.TryParseExact(text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        time = parsed.ToTimeSpan();
        return true;
    }

    public static string Format(TimeSpan? time) => time?.ToString(@"hh\:mm", CultureInfo.InvariantCulture) ?? string.Empty;
}
