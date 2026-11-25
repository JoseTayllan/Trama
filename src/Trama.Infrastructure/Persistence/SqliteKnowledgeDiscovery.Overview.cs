using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed partial class SqliteKnowledgeDiscovery
{
    public async Task<LibraryOverview> ReadOverviewAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_overviewOptions);
        try
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            // A deferred read transaction keeps all counters on the same snapshot without reserving a writer.
            await using var transaction = connection.BeginTransaction(deferred: true);
            await using var enlistment = await context.Database.UseTransactionAsync(transaction, cancellationToken);
            var trash = context.Set<LibraryTrashRecord>().AsNoTracking();
            var spaces = context.Set<KnowledgeSpaceRecord>().AsNoTracking()
                .Where(space => !trash.Any(item => item.Kind == LibraryEntryKind.Space && item.EntryId == space.Id));
            var notebooks = context.Set<NotebookRecord>().AsNoTracking()
                .Where(book => spaces.Any(space => space.Id == book.SpaceId) &&
                    !trash.Any(item => item.Kind == LibraryEntryKind.Notebook && item.EntryId == book.Id));

            var pageIds = context.Set<NotebookPageRecord>().Select(page => page.Id)
                .Union(context.Set<NoteRecord>().Select(note => note.Id));
            var pages = from id in pageIds
                        join location in context.Set<PageLocationRecord>() on id equals location.PageId into locations
                        from location in locations.DefaultIfEmpty()
                        join book in context.Set<NotebookRecord>() on location.NotebookId equals book.Id into books
                        from book in books.DefaultIfEmpty()
                        where !trash.Any(item =>
                            (item.Kind == LibraryEntryKind.Page && item.EntryId == id) ||
                            (item.Kind == LibraryEntryKind.Notebook && item.EntryId == location.NotebookId) ||
                            (item.Kind == LibraryEntryKind.Space && item.EntryId == book.SpaceId))
                        select new { Id = id, SpaceId = (Guid?)book.SpaceId };

            var spaceRows = await spaces.Select(space => new KnowledgeSpace(space.Id, space.Title))
                .ToArrayAsync(cancellationToken);
            var notebookCounts = await notebooks.GroupBy(book => book.SpaceId)
                .Select(group => new { SpaceId = group.Key, Count = group.Count() }).ToArrayAsync(cancellationToken);
            var pageCounts = await pages.GroupBy(page => page.SpaceId)
                .Select(group => new { SpaceId = group.Key, Count = group.Count() }).ToArrayAsync(cancellationToken);
            var tags = await context.Set<PageTagRecord>().AsNoTracking()
                .Where(tag => pages.Any(page => page.Id == tag.PageId))
                .GroupBy(tag => tag.Name)
                .Select(group => new LibraryTagUsage(group.Key, group.Select(tag => tag.PageId).Distinct().Count()))
                .ToArrayAsync(cancellationToken);

            var notebookCountsBySpace = notebookCounts.ToDictionary(row => row.SpaceId, row => row.Count);
            var pageCountsBySpace = pageCounts.Where(row => row.SpaceId.HasValue)
                .ToDictionary(row => row.SpaceId!.Value, row => row.Count);
            var rows = spaceRows.OrderBy(space => space.Title, StringComparer.Ordinal).ThenBy(space => space.Id)
                .Select(space => new LibrarySpaceOverview(space,
                    notebookCountsBySpace.GetValueOrDefault(space.Id), pageCountsBySpace.GetValueOrDefault(space.Id))).ToArray();
            var topTags = tags.OrderByDescending(tag => tag.PageCount).ThenBy(tag => tag.Tag, StringComparer.Ordinal).Take(5).ToArray();
            var result = new LibraryOverview(spaceRows.Length, notebookCounts.Sum(row => row.Count),
                pageCounts.Sum(row => row.Count), pageCounts.Where(row => row.SpaceId is null).Sum(row => row.Count), rows, topTags);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("The library overview could not be loaded.", exception);
        }
    }
}
