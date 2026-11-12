namespace Trama.Infrastructure.Persistence;

internal sealed class AgendaWeekReflectionRecord
{
    public DateOnly WeekStart { get; set; }
    public string WorkedWell { get; set; } = string.Empty;
    public string Adjustments { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
