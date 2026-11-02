using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteNoteService : ICreateNote
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteNoteService(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath))
        {
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        }

        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connectionString).Options;
    }

    public async Task<CreateNoteResult> ExecuteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        await using var context = new TramaDbContext(_options);
        return await new CreateNote(new SqliteNoteRepository(context)).ExecuteAsync(request, cancellationToken);
    }
}
