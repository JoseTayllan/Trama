using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaTaskRepository : IAgendaTaskRepository, IAgendaTaskReader
{
    private readonly TramaDbContext _context;

    public SqliteAgendaTaskRepository(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();
        if (!AgendaTaskValidation.IsValid(task) || task.Description is null)
            throw new ArgumentException("Invalid agenda task.", nameof(task));
        try
        {
            await using var transaction = _context.Database.CurrentTransaction is null
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            // Check persisted visibility even if the caller's context still tracks an older version.
            var current = await _context.Set<AgendaTaskRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == task.Id, cancellationToken);
            if (current?.DeletedAt is not null)
                throw new AgendaPersistenceException("The task is in the trash; restore it before saving.");
            var record = await _context.Set<AgendaTaskRecord>().SingleOrDefaultAsync(item => item.Id == task.Id, cancellationToken);
            if (record is null)
            {
                record = new AgendaTaskRecord { Id = task.Id };
                _context.Set<AgendaTaskRecord>().Add(record);
            }
            record.Title = task.Title;
            record.Description = task.Description;
            record.PlannedDate = task.PlannedDate;
            record.PlannedEndDate = task.PlannedEndDate;
            record.StartTime = task.StartTime;
            record.EndTime = task.EndTime;
            record.Category = task.Category;
            record.Status = task.Status;
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new AgendaPersistenceException("The agenda task could not be saved.");
        }
    }

    public async Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var record = await _context.Set<AgendaTaskRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id && item.DeletedAt == null, cancellationToken);
            return record is null ? null : Read(record);
        }
        catch (Exception exception) when (exception is DbException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The stored agenda task could not be read.");
        }
    }

    public async Task<IReadOnlyList<AgendaTaskSnapshot>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (from > through) throw new ArgumentException("The period must start before it ends.", nameof(through));
        try
        {
            var records = await _context.Set<AgendaTaskRecord>().AsNoTracking()
                .Where(item => item.DeletedAt == null && item.PlannedDate >= from && item.PlannedDate <= through)
                .OrderBy(item => item.PlannedDate).ThenBy(item => item.StartTime == null).ThenBy(item => item.StartTime)
                .ThenBy(item => item.Title).ThenBy(item => item.Id).ToArrayAsync(cancellationToken);
            return records.Select(Read).ToArray();
        }
        catch (Exception exception) when (exception is DbException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The agenda period could not be read.");
        }
    }

    internal async Task ValidateForBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Validate all rows, including records that a date filter could exclude.
            var records = await _context.Set<AgendaTaskRecord>().AsNoTracking().ToArrayAsync(cancellationToken);
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Read(record);
            }
        }
        catch (Exception exception) when (exception is DbException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The stored agenda could not be validated.");
        }
    }

    internal static AgendaTaskSnapshot Read(AgendaTaskRecord record)
    {
        var task = new AgendaTaskSnapshot(record.Id, record.Title, record.Description, record.PlannedDate,
            record.StartTime, record.EndTime, record.Category, record.Status)
        { PlannedEndDate = record.PlannedEndDate };
        if (!AgendaTaskValidation.IsValid(task) || task.Description is null)
            throw new AgendaPersistenceException("The stored agenda contains invalid task data.");
        return task;
    }
}
