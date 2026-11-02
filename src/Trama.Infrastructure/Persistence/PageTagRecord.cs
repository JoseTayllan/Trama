namespace Trama.Infrastructure.Persistence;

internal sealed class PageTagRecord
{
    public Guid PageId { get; set; }
    public string Name { get; set; } = string.Empty;
}
