using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed partial class SqliteAgendaActions : IAgendaActions, IAgendaPeriodReader, IAgendaPageLinks, IAgendaActualHoursReader
{
    private readonly DbContextOptions<TramaDbContext> _options;

    private readonly TimeProvider? _completionClock;

    public SqliteAgendaActions(string databasePath) : this(databasePath, null)
    {
    }

    public SqliteAgendaActions(string databasePath, TimeProvider? completionClock)
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
        _completionClock = completionClock;
    }

    public async Task<AgendaTaskSaveResult> SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SaveAgendaTask(new SqliteAgendaTaskRepository(context, _completionClock)).ExecuteAsync(task, cancellationToken);
    }

    public async Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaTaskRepository(context).GetByIdAsync(id, cancellationToken);
    }

    public async Task<TimeSpan?> ReadActualHoursAsync(DateTimeOffset from, DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var firstTick = from.UtcTicks;
        var endTick = until.UtcTicks;
        await using var context = new TramaDbContext(_options);
        var recorded = await (from task in context.Set<AgendaTaskRecord>().AsNoTracking()
                              join start in context.Set<AgendaTaskStartRecord>() on task.Id equals start.TaskId
                              where task.DeletedAt == null && task.Status == AgendaTaskStatus.Completed &&
                                  task.LastCompletedAt != null && start.StartedAtUtcTicks < endTick
                              select new { start.StartedAtUtcTicks, task.LastCompletedAt }).ToArrayAsync(cancellationToken);

        TimeSpan? total = null;
        foreach (var interval in recorded)
        {
            var started = Math.Max(firstTick, interval.StartedAtUtcTicks);
            var completed = Math.Min(endTick, interval.LastCompletedAt!.Value.UtcTicks);
            if (completed < started || completed <= firstTick) continue;

            total = (total ?? TimeSpan.Zero) + TimeSpan.FromTicks(completed - started);
        }
        return total;
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
