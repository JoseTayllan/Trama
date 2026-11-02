namespace Trama.Application.Knowledge;

public sealed record LibraryTrashPreview(LibraryEntryKind Kind, Guid Id, string Title, int Spaces, int Notebooks, int Pages);

public sealed record LibraryTrashEntry(LibraryEntryKind Kind, Guid Id, string Title, DateTimeOffset DeletedAt, bool CanRestore);

public interface ILibraryTrashActions
{
    Task<LibraryTrashPreview> PreviewAsync(LibraryEntryKind kind, Guid id, CancellationToken cancellationToken = default);
    Task MoveToTrashAsync(LibraryEntryKind kind, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LibraryTrashEntry>> ListAsync(CancellationToken cancellationToken = default);
    Task RestoreAsync(LibraryEntryKind kind, Guid id, CancellationToken cancellationToken = default);
}
