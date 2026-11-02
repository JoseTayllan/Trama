namespace Trama.Application.Knowledge;

public sealed record NotebookSnapshot(Guid Id, string Title, IReadOnlyList<NotebookBlock> Blocks);

public interface INotebookRepository
{
    Task SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default);

    Task<NotebookSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
