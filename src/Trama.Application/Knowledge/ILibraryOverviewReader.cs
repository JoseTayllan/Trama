namespace Trama.Application.Knowledge;

public sealed record LibrarySpaceOverview(KnowledgeSpace Space, int NotebookCount, int PageCount);

public sealed record LibraryTagUsage(string Tag, int PageCount);

public sealed record LibraryOverview(int SpaceCount, int NotebookCount, int PageCount,
    int UnfiledPageCount, IReadOnlyList<LibrarySpaceOverview> Spaces, IReadOnlyList<LibraryTagUsage> TopTags);

public interface ILibraryOverviewReader
{
    Task<LibraryOverview> ReadOverviewAsync(CancellationToken cancellationToken = default);
}
