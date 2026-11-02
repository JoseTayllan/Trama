namespace Trama.Application.Knowledge;

public interface INoteReader
{
    /// <summary>Returns the note, or null only when the query succeeds without a matching ID.</summary>
    /// <exception cref="NotePersistenceException">Storage could not complete the query.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<NoteSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
