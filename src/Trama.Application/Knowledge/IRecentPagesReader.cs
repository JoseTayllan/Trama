namespace Trama.Application.Knowledge;

public sealed record RecentPage(PageSearchResult Page, DateTimeOffset EditedAt);

public interface IRecentPagesReader
{
    Task<IReadOnlyList<RecentPage>> ListRecentPagesAsync(int limit, CancellationToken cancellationToken = default);
}
