namespace Trama.Application.Agenda;

public sealed record AgendaPageLink(Guid PageId, string? Title);

public interface IAgendaPageLinks
{
    Task<IReadOnlySet<Guid>> ListTasksWithPageLinksAsync(IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaPageLink>> ListPageLinksAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task AddPageLinkAsync(Guid taskId, Guid pageId, CancellationToken cancellationToken = default);
    Task RemovePageLinkAsync(Guid taskId, Guid pageId, CancellationToken cancellationToken = default);
}
