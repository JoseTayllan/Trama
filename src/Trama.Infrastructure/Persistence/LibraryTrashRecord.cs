using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class LibraryTrashRecord
{
    public LibraryEntryKind Kind { get; set; }
    public Guid EntryId { get; set; }
    public Guid BatchId { get; set; }
    public bool IsRoot { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset DeletedAt { get; set; }
}
