using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed partial class SqliteAgendaActions
{
    public async Task<IReadOnlySet<Guid>> ListTasksWithPageLinksAsync(IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (taskIds.Count == 0) return new HashSet<Guid>();

        await using var context = new TramaDbContext(_options);
        var ids = await context.Set<AgendaPageLinkRecord>().AsNoTracking()
            .Where(link => taskIds.Contains(link.TaskId))
            .Select(link => link.TaskId).Distinct().ToArrayAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<AgendaPageLink>> ListPageLinksAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await context.Set<AgendaPageLinkRecord>().AsNoTracking()
            .Where(link => link.TaskId == taskId &&
                context.Set<AgendaTaskRecord>().Any(task => task.Id == taskId && task.DeletedAt == null))
            .OrderBy(link => link.PageId)
            .Select(link => new AgendaPageLink(link.PageId,
                context.Set<NotebookPageRecord>().Where(page => page.Id == link.PageId).Select(page => page.Title).FirstOrDefault() ??
                context.Set<NoteRecord>().Where(page => page.Id == link.PageId).Select(page => page.Title).FirstOrDefault()))
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddPageLinkAsync(Guid taskId, Guid pageId, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await RequireActiveTaskAsync(context, taskId, cancellationToken);
        if (pageId == Guid.Empty ||
            await LibraryTrashVisibility.IsHiddenAsync(context, LibraryEntryKind.Page, pageId, cancellationToken) ||
            (!await context.Set<NotebookPageRecord>().AnyAsync(page => page.Id == pageId, cancellationToken) &&
             !await context.Set<NoteRecord>().AnyAsync(page => page.Id == pageId, cancellationToken)))
            throw new AgendaPersistenceException("The page is unavailable.");

        if (!await context.Set<AgendaPageLinkRecord>().AnyAsync(link => link.TaskId == taskId && link.PageId == pageId, cancellationToken))
        {
            context.Add(new AgendaPageLinkRecord { TaskId = taskId, PageId = pageId });
            await context.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemovePageLinkAsync(Guid taskId, Guid pageId, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await RequireActiveTaskAsync(context, taskId, cancellationToken);
        await context.Set<AgendaPageLinkRecord>().Where(link => link.TaskId == taskId && link.PageId == pageId)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task RequireActiveTaskAsync(TramaDbContext context, Guid taskId, CancellationToken cancellationToken)
    {
        if (taskId == Guid.Empty || !await context.Set<AgendaTaskRecord>()
            .AnyAsync(task => task.Id == taskId && task.DeletedAt == null, cancellationToken))
            throw new AgendaPersistenceException("The task is unavailable.");
    }
}
