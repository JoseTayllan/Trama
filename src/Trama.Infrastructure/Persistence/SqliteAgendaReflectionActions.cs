using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaReflectionActions : IAgendaReflectionActions
{
    private readonly DbContextOptions<TramaDbContext> _options;
    private readonly TimeProvider _clock;

    public SqliteAgendaReflectionActions(string databasePath, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(clock);
        if (!Path.IsPathFullyQualified(databasePath))
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));

        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
        _clock = clock;
    }

    public async Task<AgendaWeekReflection?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaReflectionRepository(context).GetAsync(weekStart, cancellationToken);
    }

    public async Task<AgendaReflectionSaveResult> SaveAsync(DateOnly weekStart, string? workedWell, string? adjustments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        var repository = new SqliteAgendaReflectionRepository(context);
        return await new SaveAgendaReflection(repository, _clock).ExecuteAsync(weekStart, workedWell, adjustments, cancellationToken);
    }
}
