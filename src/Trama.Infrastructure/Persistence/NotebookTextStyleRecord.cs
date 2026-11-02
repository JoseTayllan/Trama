namespace Trama.Infrastructure.Persistence;

internal sealed class NotebookTextStyleRecord
{
    public Guid PageId { get; set; }
    public Guid BlockId { get; set; }
    public string Styles { get; set; } = "[]";
}
