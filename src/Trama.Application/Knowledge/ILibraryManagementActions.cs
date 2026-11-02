namespace Trama.Application.Knowledge;

public enum LibraryEntryKind
{
    Space,
    Notebook,
    Page
}

public interface ILibraryManagementActions
{
    Task RenameAsync(LibraryEntryKind kind, Guid id, string title, CancellationToken cancellationToken = default);

    Task MovePageAsync(Guid pageId, Guid notebookId, CancellationToken cancellationToken = default);
}
