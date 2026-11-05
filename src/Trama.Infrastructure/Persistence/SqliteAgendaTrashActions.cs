using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaTrashActions : IAgendaTrashActions
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteAgendaTrashActions(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath)) throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
    }

    public async Task MoveToTrashAsync(Guid taskId, DateTimeOffset deletedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (taskId == Guid.Empty) throw new ArgumentException("A task identity is required.", nameof(taskId));
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var task = await context.Set<AgendaTaskRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == taskId, cancellationToken)
                ?? throw new ArgumentException("The task does not exist.", nameof(taskId));
            if (task.DeletedAt is not null) throw new InvalidOperationException("The task is already in the trash.");
            SqliteAgendaTaskRepository.Read(task);
            var changed = await context.Set<AgendaTaskRecord>().Where(item => item.Id == taskId && item.DeletedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.DeletedAt, (DateTimeOffset?)deletedAt), cancellationToken);
            if (changed != 1) throw new AgendaPersistenceException("The task changed while moving to the trash.");
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The task could not be moved to the trash.");
        }
    }

    public async Task<IReadOnlyList<AgendaTaskTrashEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        try
        {
            var records = await context.Set<AgendaTaskRecord>().AsNoTracking().Where(item => item.DeletedAt != null).ToArrayAsync(cancellationToken);
            return records.Select(item => new AgendaTaskTrashEntry(SqliteAgendaTaskRepository.Read(item), item.DeletedAt!.Value, item.LastCompletedAt))
                .OrderByDescending(item => item.DeletedAt).ThenBy(item => item.Task.Title, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Task.Id).ToArray();
        }
        catch (Exception exception) when (exception is DbException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The agenda trash could not be read.");
        }
    }

    public async Task RestoreAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (taskId == Guid.Empty) throw new ArgumentException("A task identity is required.", nameof(taskId));
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var task = await context.Set<AgendaTaskRecord>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == taskId, cancellationToken)
                ?? throw new ArgumentException("The task does not exist.", nameof(taskId));
            if (task.DeletedAt is null) throw new InvalidOperationException("The task is not in the trash.");
            SqliteAgendaTaskRepository.Read(task);
            var changed = await context.Set<AgendaTaskRecord>().Where(item => item.Id == taskId && item.DeletedAt != null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.DeletedAt, (DateTimeOffset?)null), cancellationToken);
            if (changed != 1) throw new AgendaPersistenceException("The task changed while restoring.");
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The task could not be restored.");
        }
    }
}
