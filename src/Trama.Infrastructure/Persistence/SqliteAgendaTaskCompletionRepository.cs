using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaTaskCompletionRepository : IAgendaTaskCompletionRepository, IAgendaTaskCompletionReader
{
    private readonly TramaDbContext _context;

    public SqliteAgendaTaskCompletionRepository(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task CompleteAsync(IReadOnlyList<Guid> taskIds, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskIds);
        cancellationToken.ThrowIfCancellationRequested();
        var ids = taskIds.ToArray();
        if (ids.Length == 0 || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("Select distinct, valid task identities.", nameof(taskIds));

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var records = await _context.Set<AgendaTaskRecord>().AsNoTracking()
                .Where(record => record.DeletedAt == null && ids.Contains(record.Id)).ToArrayAsync(cancellationToken);
            if (records.Length != ids.Length)
                throw new AgendaPersistenceException("The selected tasks are no longer available.");

            var localDay = DateOnly.FromDateTime(completedAt.DateTime);
            foreach (var record in records)
            {
                var task = SqliteAgendaTaskRepository.Read(record);
                if (task.Status == AgendaTaskStatus.Completed || task.PlannedDate > localDay)
                    throw new AgendaPersistenceException("The selected tasks are no longer eligible for completion.");
            }

            var byId = records.ToDictionary(record => record.Id);
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var original = byId[id];
                // Update only completion fields; never flush unrelated tracked changes in the caller's context.
                var changed = await _context.Set<AgendaTaskRecord>()
                    .Where(record => record.Id == id && record.DeletedAt == null && record.Status == original.Status && record.PlannedDate == original.PlannedDate)
                    .ExecuteUpdateAsync(update => update.SetProperty(record => record.Status, AgendaTaskStatus.Completed)
                        .SetProperty(record => record.LastCompletedAt, (DateTimeOffset?)completedAt), cancellationToken);
                if (changed != 1)
                    throw new AgendaPersistenceException("The selected tasks changed during completion.");
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The selected tasks could not be completed.");
        }
    }

    public async Task<DateTimeOffset?> GetLastCompletedAtAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await _context.Set<AgendaTaskRecord>().AsNoTracking().Where(record => record.Id == taskId && record.DeletedAt == null)
                .Select(record => record.LastCompletedAt).SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The actual completion time could not be read.");
        }
    }
}
