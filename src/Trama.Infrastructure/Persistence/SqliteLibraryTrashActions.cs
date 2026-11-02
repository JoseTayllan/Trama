using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteLibraryTrashActions : ILibraryTrashActions
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteLibraryTrashActions(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath)) throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
    }

    public async Task<LibraryTrashPreview> PreviewAsync(LibraryEntryKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        var entries = await CollectActiveAsync(context, kind, id, cancellationToken);
        var root = entries.Single(item => item.Kind == kind && item.EntryId == id);
        return new(kind, id, root.Title, entries.Count(item => item.Kind == LibraryEntryKind.Space),
            entries.Count(item => item.Kind == LibraryEntryKind.Notebook), entries.Count(item => item.Kind == LibraryEntryKind.Page));
    }

    public async Task MoveToTrashAsync(LibraryEntryKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        try
        {
            // A single transaction serializes the membership check and all tombstones.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var entries = await CollectActiveAsync(context, kind, id, cancellationToken);
            var batch = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in entries)
            {
                entry.BatchId = batch;
                entry.DeletedAt = now;
                entry.IsRoot = entry.Kind == kind && entry.EntryId == id;
            }
            context.Set<LibraryTrashRecord>().AddRange(entries);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("The entries could not be moved to the trash.", exception);
        }
    }

    public async Task<IReadOnlyList<LibraryTrashEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var roots = await context.Set<LibraryTrashRecord>().AsNoTracking().Where(item => item.IsRoot).ToArrayAsync(cancellationToken);
            var result = new List<LibraryTrashEntry>();
            foreach (var root in roots)
                result.Add(new(root.Kind, root.EntryId, root.Title, root.DeletedAt, await CanRestoreAsync(context, root, cancellationToken)));
            return result.OrderByDescending(item => item.DeletedAt).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Id).ToArray();
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("The trash could not be read.", exception);
        }
    }

    public async Task RestoreAsync(LibraryEntryKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var root = await context.Set<LibraryTrashRecord>().SingleOrDefaultAsync(item => item.Kind == kind && item.EntryId == id && item.IsRoot, cancellationToken)
                ?? throw new ArgumentException("The trash entry does not exist.", nameof(id));
            if (!await CanRestoreAsync(context, root, cancellationToken))
                throw new InvalidOperationException("Restore the parent before restoring this entry.");
            // Entries deleted earlier belong to different batches and remain in the trash.
            await context.Set<LibraryTrashRecord>().Where(item => item.BatchId == root.BatchId).ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("The entries could not be restored.", exception);
        }
    }

    private static async Task<bool> CanRestoreAsync(TramaDbContext context, LibraryTrashRecord root, CancellationToken cancellationToken)
    {
        if (root.Kind == LibraryEntryKind.Space)
            return await context.Set<KnowledgeSpaceRecord>().AnyAsync(item => item.Id == root.EntryId, cancellationToken);
        if (root.Kind == LibraryEntryKind.Notebook)
        {
            var book = await context.Set<NotebookRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == root.EntryId, cancellationToken);
            return book is not null && await context.Set<KnowledgeSpaceRecord>().AnyAsync(item => item.Id == book.SpaceId, cancellationToken)
                && !await LibraryTrashVisibility.IsHiddenAsync(context, LibraryEntryKind.Space, book.SpaceId, cancellationToken);
        }
        var location = await context.Set<PageLocationRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.PageId == root.EntryId, cancellationToken);
        if (location is null) return true;
        var parent = await context.Set<NotebookRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == location.NotebookId, cancellationToken);
        return parent is not null && !await LibraryTrashVisibility.IsHiddenAsync(context, LibraryEntryKind.Notebook, parent.Id, cancellationToken)
            && await context.Set<KnowledgeSpaceRecord>().AnyAsync(item => item.Id == parent.SpaceId, cancellationToken)
            && !await LibraryTrashVisibility.IsHiddenAsync(context, LibraryEntryKind.Space, parent.SpaceId, cancellationToken);
    }

    private static async Task<List<LibraryTrashRecord>> CollectActiveAsync(TramaDbContext context, LibraryEntryKind kind, Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || !Enum.IsDefined(kind)) throw new ArgumentException("Invalid library entry.", nameof(id));
        await LibraryTrashVisibility.RequireActiveAsync(context, kind, id, cancellationToken);
        var entries = new List<LibraryTrashRecord>();
        var books = new List<Guid>();
        var pages = new List<Guid>();
        if (kind == LibraryEntryKind.Space)
        {
            var space = await context.Set<KnowledgeSpaceRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new ArgumentException("The space does not exist.", nameof(id));
            entries.Add(new() { Kind = kind, EntryId = id, Title = space.Title });
            var children = await context.Set<NotebookRecord>().AsNoTracking().Where(item => item.SpaceId == id &&
                !context.Set<LibraryTrashRecord>().Any(trash => trash.Kind == LibraryEntryKind.Notebook && trash.EntryId == item.Id)).ToArrayAsync(cancellationToken);
            foreach (var child in children)
            {
                books.Add(child.Id);
                entries.Add(new() { Kind = LibraryEntryKind.Notebook, EntryId = child.Id, Title = child.Title });
            }
        }
        else if (kind == LibraryEntryKind.Notebook)
        {
            var book = await context.Set<NotebookRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new ArgumentException("The notebook does not exist.", nameof(id));
            await LibraryTrashVisibility.RequireActiveAsync(context, LibraryEntryKind.Space, book.SpaceId, cancellationToken);
            books.Add(id);
            entries.Add(new() { Kind = kind, EntryId = id, Title = book.Title });
        }
        if (kind == LibraryEntryKind.Page) pages.Add(id);
        else pages.AddRange(await context.Set<PageLocationRecord>().AsNoTracking().Where(item => books.Contains(item.NotebookId)).Select(item => item.PageId).ToArrayAsync(cancellationToken));
        var hidden = await context.Set<LibraryTrashRecord>().AsNoTracking().Where(item => item.Kind == LibraryEntryKind.Page).Select(item => item.EntryId).ToArrayAsync(cancellationToken);
        foreach (var pageId in pages.Except(hidden))
        {
            var title = await context.Set<NotebookPageRecord>().Where(item => item.Id == pageId).Select(item => item.Title).SingleOrDefaultAsync(cancellationToken)
                ?? await context.Set<NoteRecord>().Where(item => item.Id == pageId).Select(item => item.Title).SingleOrDefaultAsync(cancellationToken)
                ?? throw new ArgumentException("The page does not exist.", nameof(id));
            entries.Add(new() { Kind = LibraryEntryKind.Page, EntryId = pageId, Title = title });
        }
        return entries;
    }
}
