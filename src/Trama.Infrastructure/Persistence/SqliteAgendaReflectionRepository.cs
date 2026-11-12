using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaReflectionRepository : IAgendaReflectionRepository, IAgendaReflectionReader
{
    private readonly TramaDbContext _context;

    public SqliteAgendaReflectionRepository(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<bool> HasClosureAsync(DateOnly weekStart, CancellationToken cancellationToken = default) =>
        await new SqliteAgendaRetrospectiveRepository(_context).GetAsync(weekStart, cancellationToken) is not null;

    public async Task SaveAsync(AgendaWeekReflection reflection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(reflection);
        try
        {
            if (!await HasClosureAsync(reflection.WeekStart, cancellationToken))
                throw new AgendaPersistenceException("A reflection requires an existing closure.");
            cancellationToken.ThrowIfCancellationRequested();
            // One statement preserves the complete record and does not flush other tracked entities.
            await _context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgendaWeekReflections (WeekStart, WorkedWell, Adjustments, UpdatedAt) VALUES ({reflection.WeekStart}, {reflection.WorkedWell}, {reflection.Adjustments}, {reflection.UpdatedAt}) ON CONFLICT (WeekStart) DO UPDATE SET WorkedWell = excluded.WorkedWell, Adjustments = excluded.Adjustments, UpdatedAt = excluded.UpdatedAt", cancellationToken);
        }
        catch (DbException)
        {
            throw new AgendaPersistenceException("The reflection could not be saved.");
        }
    }

    public async Task<AgendaWeekReflection?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(weekStart);
        try
        {
            var record = await _context.Set<AgendaWeekReflectionRecord>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.WeekStart == weekStart, cancellationToken);
            return record is null ? null : await ReadAsync(record, cancellationToken);
        }
        catch (Exception exception) when (InvalidStoredData(exception))
        {
            throw new AgendaPersistenceException("The stored reflection could not be read.");
        }
    }

    internal async Task ValidateForBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var records = await _context.Set<AgendaWeekReflectionRecord>().AsNoTracking().ToArrayAsync(cancellationToken);
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReadAsync(record, cancellationToken);
            }
        }
        catch (Exception exception) when (InvalidStoredData(exception))
        {
            throw new AgendaPersistenceException("The backup contains invalid reflections.");
        }
    }

    private async Task<AgendaWeekReflection> ReadAsync(AgendaWeekReflectionRecord record, CancellationToken cancellationToken)
    {
        var reflection = new AgendaWeekReflection(record.WeekStart, record.WorkedWell, record.Adjustments, record.UpdatedAt);
        Validate(reflection);
        if (!await HasClosureAsync(reflection.WeekStart, cancellationToken))
            throw new AgendaPersistenceException("The reflection refers to a missing closure.");
        return reflection;
    }

    private static void Validate(AgendaWeekReflection reflection)
    {
        ArgumentNullException.ThrowIfNull(reflection);
        ValidateKey(reflection.WeekStart);
        if (reflection.WorkedWell is null || reflection.Adjustments is null || reflection.UpdatedAt == default || reflection.UpdatedAt.Offset != TimeSpan.Zero)
            throw new ArgumentException("Reflection texts and UTC timestamp must be valid.", nameof(reflection));
    }

    private static void ValidateKey(DateOnly weekStart)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("A reflection key must be a Monday.", nameof(weekStart));
    }

    private static bool InvalidStoredData(Exception exception) => exception is
        DbException or FormatException or InvalidCastException or ArgumentException or InvalidOperationException;
}
