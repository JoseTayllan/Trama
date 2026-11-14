namespace Trama.Infrastructure.Persistence;

internal sealed class AgendaTaskStartRecord
{
    public Guid TaskId { get; set; }
    public long StartedAtUtcTicks { get; set; }
}
