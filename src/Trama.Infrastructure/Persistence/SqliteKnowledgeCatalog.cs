using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteKnowledgeCatalog : IKnowledgeCatalogRepository
{
    private readonly TramaDbContext _context;

    public SqliteKnowledgeCatalog(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AddSpaceAsync(KnowledgeSpace space, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(space);
        cancellationToken.ThrowIfCancellationRequested();
        _context.Set<KnowledgeSpaceRecord>().Add(new KnowledgeSpaceRecord { Id = space.Id, Title = space.Title });
        await PersistAsync(cancellationToken);
    }

    public async Task AddNotebookAsync(NotebookInfo notebook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notebook);
        cancellationToken.ThrowIfCancellationRequested();
        await LibraryTrashVisibility.RequireActiveAsync(_context, LibraryEntryKind.Space, notebook.SpaceId, cancellationToken);
        _context.Set<NotebookRecord>().Add(new NotebookRecord
        {
            Id = notebook.Id,
            SpaceId = notebook.SpaceId,
            Title = notebook.Title
        });
        await PersistAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeSpace>> ListSpacesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var query = _context.Set<KnowledgeSpaceRecord>().AsNoTracking();
            if (await LibraryTrashVisibility.HasTableAsync(_context, cancellationToken))
                query = query.Where(record => !_context.Set<LibraryTrashRecord>().Any(trash => trash.Kind == LibraryEntryKind.Space && trash.EntryId == record.Id));
            return await query
                .OrderBy(record => record.Title).ThenBy(record => record.Id)
                .Select(record => new KnowledgeSpace(record.Id, record.Title)).ToArrayAsync(cancellationToken);
        }
        catch (DbException exception)
        {
            throw new NotePersistenceException("The knowledge spaces could not be retrieved.", exception);
        }
    }

    public async Task<IReadOnlyList<NotebookInfo>> ListNotebooksAsync(Guid spaceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var query = _context.Set<NotebookRecord>().AsNoTracking().Where(record => record.SpaceId == spaceId);
            if (await LibraryTrashVisibility.HasTableAsync(_context, cancellationToken))
                query = query.Where(record => !_context.Set<LibraryTrashRecord>().Any(trash => (trash.Kind == LibraryEntryKind.Notebook && trash.EntryId == record.Id)
                    || (trash.Kind == LibraryEntryKind.Space && trash.EntryId == record.SpaceId)));
            return await query
                .OrderBy(record => record.Title).ThenBy(record => record.Id)
                .Select(record => new NotebookInfo(record.Id, record.SpaceId, record.Title)).ToArrayAsync(cancellationToken);
        }
        catch (DbException exception)
        {
            throw new NotePersistenceException("The notebooks could not be retrieved.", exception);
        }
    }

    public Task<IReadOnlyList<KnowledgePageSummary>> ListPagesAsync(Guid notebookId, CancellationToken cancellationToken = default) =>
        ReadPagesAsync(notebookId, cancellationToken);

    public Task<IReadOnlyList<KnowledgePageSummary>> ListUnfiledPagesAsync(CancellationToken cancellationToken = default) =>
        ReadPagesAsync(null, cancellationToken);

    public async Task PlacePageAsync(Guid pageId, Guid notebookId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await using var transaction = _context.Database.CurrentTransaction is null
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;
            await LibraryTrashVisibility.RequireActiveAsync(_context, LibraryEntryKind.Page, pageId, cancellationToken);
            await LibraryTrashVisibility.RequireActiveAsync(_context, LibraryEntryKind.Notebook, notebookId, cancellationToken);
            var parentSpace = await _context.Set<NotebookRecord>().Where(book => book.Id == notebookId).Select(book => book.SpaceId).SingleOrDefaultAsync(cancellationToken);
            await LibraryTrashVisibility.RequireActiveAsync(_context, LibraryEntryKind.Space, parentSpace, cancellationToken);
            var exists = await _context.Set<NotebookPageRecord>().AnyAsync(page => page.Id == pageId, cancellationToken)
                || await _context.Set<NoteRecord>().AnyAsync(note => note.Id == pageId, cancellationToken);
            if (!exists)
            {
                throw new ArgumentException("The page does not exist.", nameof(pageId));
            }

            var location = await _context.Set<PageLocationRecord>()
                .SingleOrDefaultAsync(record => record.PageId == pageId, cancellationToken);
            if (location is null)
            {
                _context.Set<PageLocationRecord>().Add(new PageLocationRecord { PageId = pageId, NotebookId = notebookId });
            }
            else
            {
                location.NotebookId = notebookId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new NotePersistenceException("The page could not be placed in the notebook.", exception);
        }
    }

    private async Task<IReadOnlyList<KnowledgePageSummary>> ReadPagesAsync(Guid? notebookId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var structured = _context.Set<NotebookPageRecord>().AsNoTracking();
            var legacy = _context.Set<NoteRecord>().AsNoTracking()
                .Where(note => !_context.Set<NotebookPageRecord>().Any(page => page.Id == note.Id));
            if (await LibraryTrashVisibility.HasTableAsync(_context, cancellationToken))
            {
                structured = structured.Where(page => !_context.Set<LibraryTrashRecord>().Any(trash => trash.Kind == LibraryEntryKind.Page && trash.EntryId == page.Id));
                legacy = legacy.Where(page => !_context.Set<LibraryTrashRecord>().Any(trash => trash.Kind == LibraryEntryKind.Page && trash.EntryId == page.Id));
            }
            var locations = _context.Set<PageLocationRecord>().AsNoTracking();
            if (notebookId is { } id)
            {
                var pageIds = locations.Where(location => location.NotebookId == id).Select(location => location.PageId);
                structured = structured.Where(page => pageIds.Contains(page.Id));
                legacy = legacy.Where(note => pageIds.Contains(note.Id));
            }
            else
            {
                var pageIds = locations.Select(location => location.PageId);
                structured = structured.Where(page => !pageIds.Contains(page.Id));
                legacy = legacy.Where(note => !pageIds.Contains(note.Id));
            }

            var rows = await structured.Select(page => new { page.Id, page.Title })
                .Concat(legacy.Select(note => new { note.Id, note.Title }))
                .OrderBy(page => page.Title).ThenBy(page => page.Id).ToArrayAsync(cancellationToken);
            return rows.Select(page => new KnowledgePageSummary(page.Id, page.Title)).ToArray();
        }
        catch (DbException exception)
        {
            throw new NotePersistenceException("The catalog pages could not be retrieved.", exception);
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new NotePersistenceException("The knowledge catalog could not be saved.", exception);
        }
    }
}
