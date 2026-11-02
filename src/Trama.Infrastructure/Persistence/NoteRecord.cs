namespace Trama.Infrastructure.Persistence;

internal sealed class NoteRecord
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}
