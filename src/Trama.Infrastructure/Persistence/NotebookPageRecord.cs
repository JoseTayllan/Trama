namespace Trama.Infrastructure.Persistence;

internal sealed class NotebookPageRecord
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int FormatVersion { get; set; } = 1;

    public List<NotebookBlockRecord> Blocks { get; set; } = [];
}

internal sealed class NotebookBlockRecord
{
    public Guid PageId { get; set; }

    public Guid Id { get; set; }

    public int Position { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public Guid? AttachmentId { get; set; }
}
