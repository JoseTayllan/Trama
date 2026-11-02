namespace Trama.Application.Knowledge;

public interface ILibraryActions : INotebookActions
{
    Task<KnowledgeSpace> CreateSpaceAsync(string title, CancellationToken cancellationToken = default);

    Task<NotebookInfo> CreateNotebookAsync(Guid spaceId, string title, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSpace>> ListSpacesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotebookInfo>> ListNotebooksAsync(Guid spaceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePageSummary>> ListPagesAsync(Guid notebookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePageSummary>> ListUnfiledPagesAsync(CancellationToken cancellationToken = default);

    Task<NotebookSaveResult> SaveInNotebookAsync(NotebookSnapshot page, Guid? notebookId, CancellationToken cancellationToken = default);
}
