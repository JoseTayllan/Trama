using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteLibraryManagement : ILibraryManagementActions
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteLibraryManagement(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath))
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
    }

    public async Task RenameAsync(LibraryEntryKind kind, Guid id, string title, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        title = title.Trim();
        await using var context = new TramaDbContext(_options);
        try
        {
            await LibraryTrashVisibility.RequireActiveAsync(context, kind, id, cancellationToken);
            switch (kind)
            {
                case LibraryEntryKind.Space:
                    var space = await context.Set<KnowledgeSpaceRecord>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                        ?? throw new ArgumentException("The space does not exist.", nameof(id));
                    space.Title = title;
                    break;
                case LibraryEntryKind.Notebook:
                    var book = await context.Set<NotebookRecord>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                        ?? throw new ArgumentException("The notebook does not exist.", nameof(id));
                    book.Title = title;
                    break;
                case LibraryEntryKind.Page:
                    var page = await context.Set<NotebookPageRecord>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
                    if (page is not null) page.Title = title;
                    else
                    {
                        var note = await context.Set<NoteRecord>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                            ?? throw new ArgumentException("The page does not exist.", nameof(id));
                        note.Title = title;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("The library entry could not be renamed.", exception);
        }
    }

    public async Task MovePageAsync(Guid pageId, Guid notebookId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        await new SqliteKnowledgeCatalog(context).PlacePageAsync(pageId, notebookId, cancellationToken);
    }
}
