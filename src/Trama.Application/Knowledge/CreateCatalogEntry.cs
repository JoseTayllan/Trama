namespace Trama.Application.Knowledge;

public sealed class CreateCatalogEntry(IKnowledgeCatalogRepository catalog)
{
    public async Task<KnowledgeSpace> CreateSpaceAsync(string title, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();
        var space = new KnowledgeSpace(Guid.NewGuid(), title.Trim());
        await catalog.AddSpaceAsync(space, cancellationToken);
        return space;
    }

    public async Task<NotebookInfo> CreateNotebookAsync(Guid spaceId, string title, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();
        if (spaceId == Guid.Empty)
        {
            throw new ArgumentException("A space identity is required.", nameof(spaceId));
        }

        var notebook = new NotebookInfo(Guid.NewGuid(), spaceId, title.Trim());
        await catalog.AddNotebookAsync(notebook, cancellationToken);
        return notebook;
    }
}
