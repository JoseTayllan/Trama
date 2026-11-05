using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaCompletionActions : IAgendaCompletionActions
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteAgendaCompletionActions(string databasePath)
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

    public async Task<IReadOnlyList<AgendaTaskSnapshot>> ListCandidatesAsync(DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        var tasks = await new SqliteAgendaTaskRepository(context).ListAsync(DateOnly.MinValue, through, cancellationToken);
        return tasks.Where(task => task.Status is AgendaTaskStatus.Pending or AgendaTaskStatus.InProgress).ToArray();
    }

    public async Task<AgendaTaskCompletionResult> CompleteAsync(IReadOnlyList<AgendaTaskSnapshot> selectedTasks,
        DateTimeOffset completedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedTasks);
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new CompleteAgendaTasks(new SqliteAgendaTaskCompletionRepository(context))
            .ExecuteAsync(selectedTasks, completedAt, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLastCompletedAtAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        return await new SqliteAgendaTaskCompletionRepository(context).GetLastCompletedAtAsync(taskId, cancellationToken);
    }
}
