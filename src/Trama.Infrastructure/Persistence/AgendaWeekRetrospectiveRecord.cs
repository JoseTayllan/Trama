namespace Trama.Infrastructure.Persistence;

internal sealed class AgendaWeekRetrospectiveRecord
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset ClosedAt { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string TimeZoneData { get; set; } = string.Empty;
    public int FormatVersion { get; set; }
    public string Content { get; set; } = string.Empty;
}
