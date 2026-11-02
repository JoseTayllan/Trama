using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteNotebookActions : INotebookActions
{
    private readonly DbContextOptions<TramaDbContext> _options;
    private readonly IImageAttachmentStore _images;

    public SqliteNotebookActions(string databasePath, IImageAttachmentStore images)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(images);
        if (!Path.IsPathFullyQualified(databasePath))
        {
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        }

        var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
        _images = images;
    }

    public async Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new SaveNotebook(new SqliteNotebookRepository(context)).ExecuteAsync(notebook, cancellationToken);
    }

    public async Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new SqliteNotebookRepository(context).GetByIdAsync(id, cancellationToken);
    }

    public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) =>
        _images.ImportAsync(source, cancellationToken);

    public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) =>
        _images.OpenReadAsync(id, cancellationToken);
}
