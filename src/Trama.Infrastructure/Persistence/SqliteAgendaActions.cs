using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaActions : IAgendaActions, IAgendaPeriodReader
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteAgendaActions(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath))
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));

        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
    }

    public async Task<AgendaTaskSaveResult> SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SaveAgendaTask(new SqliteAgendaTaskRepository(context)).ExecuteAsync(task, cancellationToken);
    }

    public async Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaTaskRepository(context).GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<AgendaTaskSnapshot>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaTaskRepository(context).ListAsync(from, through, cancellationToken);
    }

    public async Task<IReadOnlyList<AgendaTaskSnapshot>> ListOverlappingAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaTaskRepository(context).ListOverlappingAsync(from, through, cancellationToken);
    }
}
