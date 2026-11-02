namespace Trama.Application.Knowledge;

public sealed record PageSearchResult(Guid PageId, string Title, Guid? SpaceId = null,
    string? SpaceTitle = null, Guid? NotebookId = null, string? NotebookTitle = null)
{
    public string LocationLabel => NotebookId is null ? "Não organizadas" : $"{SpaceTitle} / {NotebookTitle}";
}

public interface IKnowledgeDiscovery
{
    Task<IReadOnlyList<PageSearchResult>> SearchAsync(string query, string? tag = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPageTagsAsync(Guid pageId, CancellationToken cancellationToken = default);

    Task SetPageTagsAsync(Guid pageId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default);
}
