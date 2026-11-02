namespace Trama.Application.Knowledge;

public sealed record KnowledgeSpace(Guid Id, string Title);

public sealed record NotebookInfo(Guid Id, Guid SpaceId, string Title);

public sealed record KnowledgePageSummary(Guid Id, string Title);

public interface IKnowledgeCatalogRepository
{
    Task AddSpaceAsync(KnowledgeSpace space, CancellationToken cancellationToken = default);

    Task AddNotebookAsync(NotebookInfo notebook, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSpace>> ListSpacesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotebookInfo>> ListNotebooksAsync(Guid spaceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePageSummary>> ListPagesAsync(Guid notebookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgePageSummary>> ListUnfiledPagesAsync(CancellationToken cancellationToken = default);

    Task PlacePageAsync(Guid pageId, Guid notebookId, CancellationToken cancellationToken = default);
}
