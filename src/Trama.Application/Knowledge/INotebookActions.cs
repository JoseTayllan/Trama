namespace Trama.Application.Knowledge;

public enum NotebookSaveStatus
{
    Saved,
    ValidationFailed,
    PersistenceFailed
}

public sealed record NotebookSaveResult(NotebookSaveStatus Status);

public interface INotebookActions
{
    Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default);

    Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default);

    Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default);
}
