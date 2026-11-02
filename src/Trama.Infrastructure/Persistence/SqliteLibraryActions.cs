using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteLibraryActions : ILibraryActions
{
    private readonly DbContextOptions<TramaDbContext> _options;
    private readonly SqliteNotebookActions _pages;

    public SqliteLibraryActions(string databasePath, IImageAttachmentStore images)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(images);
        if (!Path.IsPathFullyQualified(databasePath))
        {
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        }

        var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
        _pages = new SqliteNotebookActions(databasePath, images);
    }

    public async Task<KnowledgeSpace> CreateSpaceAsync(string title, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new CreateCatalogEntry(new SqliteKnowledgeCatalog(context)).CreateSpaceAsync(title, cancellationToken);
    }

    public async Task<NotebookInfo> CreateNotebookAsync(Guid spaceId, string title, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new CreateCatalogEntry(new SqliteKnowledgeCatalog(context)).CreateNotebookAsync(spaceId, title, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeSpace>> ListSpacesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new SqliteKnowledgeCatalog(context).ListSpacesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotebookInfo>> ListNotebooksAsync(Guid spaceId, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new SqliteKnowledgeCatalog(context).ListNotebooksAsync(spaceId, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePageSummary>> ListPagesAsync(Guid notebookId, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new SqliteKnowledgeCatalog(context).ListPagesAsync(notebookId, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePageSummary>> ListUnfiledPagesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new SqliteKnowledgeCatalog(context).ListUnfiledPagesAsync(cancellationToken);
    }

    public async Task<NotebookSaveResult> SaveInNotebookAsync(NotebookSnapshot page, Guid? notebookId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var result = await new SaveNotebook(new SqliteNotebookRepository(context)).ExecuteAsync(page, cancellationToken);
            if (result.Status != NotebookSaveStatus.Saved)
            {
                return result;
            }

            if (notebookId is { } id)
            {
                await new SqliteKnowledgeCatalog(context).PlacePageAsync(page.Id, id, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception) when (exception is NotePersistenceException or DbException or DbUpdateException)
        {
            return new NotebookSaveResult(NotebookSaveStatus.PersistenceFailed);
        }
    }

    public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default) =>
        SaveInNotebookAsync(notebook, null, cancellationToken);
    public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => _pages.LoadAsync(id, cancellationToken);
    public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => _pages.ImportImageAsync(source, cancellationToken);
    public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => _pages.OpenImageAsync(id, cancellationToken);
}
