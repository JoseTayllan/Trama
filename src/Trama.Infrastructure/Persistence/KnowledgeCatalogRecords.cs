namespace Trama.Infrastructure.Persistence;

internal sealed class KnowledgeSpaceRecord
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
}

internal sealed class NotebookRecord
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public string Title { get; set; } = string.Empty;
}

internal sealed class PageLocationRecord
{
    public Guid PageId { get; set; }

    public Guid NotebookId { get; set; }
}
