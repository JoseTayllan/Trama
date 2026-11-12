using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaRetrospectiveActions : IAgendaRetrospectiveActions
{
    private readonly DbContextOptions<TramaDbContext> _options;
    private readonly TimeProvider _clock;

    public SqliteAgendaRetrospectiveActions(string databasePath, TimeProvider clock)
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

    public async Task<AgendaWeekRetrospective> GetOrCloseAsync(DateOnly referenceDate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var week = new AgendaWeekProjector().Build(referenceDate, Array.Empty<AgendaTaskSnapshot>());
        await using var context = new TramaDbContext(_options);
        var repository = new SqliteAgendaRetrospectiveRepository(context);
        try
        {
            // An existing capture depends only on its stored rules, not today's clock or live tasks.
            var existing = await repository.GetAsync(week.StartDate, cancellationToken);
            if (existing is not null) return existing;

            // SQLite's serializable transaction reserves the writer before reading the capture source.
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            existing = await repository.GetAsync(week.StartDate, cancellationToken);
            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            var timeZone = _clock.LocalTimeZone;
            var closedAt = TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), timeZone);
            var builder = new AgendaRetrospectiveBuilder();
            // Validate the requested week before reading live data; an open week is an argument error.
            builder.Build(week.StartDate, Array.Empty<AgendaRetrospectiveTask>(), closedAt, timeZone);
            var tasks = await repository.ListOverlappingAsync(week.StartDate, week.EndDate, cancellationToken);
            AgendaWeekRetrospective capture;
            try { capture = builder.Build(week.StartDate, tasks, closedAt, timeZone); }
            catch (ArgumentException)
            {
                throw new AgendaPersistenceException("The stored agenda cannot produce a valid retrospective.");
            }

            var inserted = await repository.TryAddAsync(capture, timeZone, cancellationToken);
            var result = inserted ? capture : await repository.GetAsync(week.StartDate, cancellationToken)
                ?? throw new AgendaPersistenceException("The winning retrospective could not be read.");
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbException)
        {
            throw new AgendaPersistenceException("The retrospective could not be closed.");
        }
    }

    public async Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("A retrospective key must be a Monday.", nameof(weekStart));
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaRetrospectiveRepository(context).GetAsync(weekStart, cancellationToken);
    }

    public async Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (from > through) throw new ArgumentException("The period must start before it ends.", nameof(through));
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaRetrospectiveRepository(context).ListAsync(from, through, cancellationToken);
    }
}
