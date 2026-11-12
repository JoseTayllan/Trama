using System.Data.Common;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteAgendaRetrospectiveRepository : IAgendaRetrospectiveStore, IAgendaRetrospectiveSource
{
    private const int FormatVersion = 1;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly TramaDbContext _context;

    public SqliteAgendaRetrospectiveRepository(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<bool> TryAddAsync(AgendaWeekRetrospective report, TimeZoneInfo timeZone, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var capture = Validate(report, timeZone);
        var content = JsonSerializer.Serialize(capture, Json);
        var zoneData = timeZone.ToSerializedString();
        try
        {
            // A single insert with a unique week key makes concurrent attempts preserve the first capture.
            // Do not flush unrelated tracked entities from the caller's context.
            var inserted = await _context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgendaWeekRetrospectives (StartDate, EndDate, ClosedAt, TimeZoneId, TimeZoneData, FormatVersion, Content) VALUES ({capture.StartDate}, {capture.EndDate}, {capture.ClosedAt}, {capture.TimeZoneId}, {zoneData}, {FormatVersion}, {content}) ON CONFLICT (StartDate) DO NOTHING", cancellationToken);
            return inserted == 1;
        }
        catch (DbException)
        {
            throw new AgendaPersistenceException("The retrospective could not be saved.");
        }
    }

    public async Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("A retrospective key must be a Monday.", nameof(weekStart));
        try
        {
            var record = await _context.Set<AgendaWeekRetrospectiveRecord>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.StartDate == weekStart, cancellationToken);
            return record is null ? null : Read(record);
        }
        catch (Exception exception) when (InvalidStoredData(exception))
        {
            throw new AgendaPersistenceException("The stored retrospective could not be read.");
        }
    }

    public async Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (from > through) throw new ArgumentException("The period must start before it ends.", nameof(through));
        try
        {
            var records = await _context.Set<AgendaWeekRetrospectiveRecord>().AsNoTracking()
                .Where(item => item.StartDate >= from && item.StartDate <= through)
                .OrderByDescending(item => item.StartDate).ToArrayAsync(cancellationToken);
            var reports = new AgendaWeekRetrospective[records.Length];
            for (var index = 0; index < records.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                reports[index] = Read(records[index]);
            }
            return Array.AsReadOnly(reports);
        }
        catch (Exception exception) when (InvalidStoredData(exception))
        {
            throw new AgendaPersistenceException("The retrospective history could not be read.");
        }
    }

    public async Task<IReadOnlyList<AgendaRetrospectiveTask>> ListOverlappingAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (from > through) throw new ArgumentException("The period must start before it ends.", nameof(through));
        try
        {
            var records = await _context.Set<AgendaTaskRecord>().AsNoTracking()
                .Where(item => item.DeletedAt == null && item.PlannedDate <= through && (item.PlannedEndDate ?? item.PlannedDate) >= from)
                .OrderBy(item => item.PlannedDate).ThenBy(item => item.StartTime == null).ThenBy(item => item.StartTime)
                .ThenBy(item => item.Title).ThenBy(item => item.Id).ToArrayAsync(cancellationToken);
            return Array.AsReadOnly(records.Select(record => new AgendaRetrospectiveTask(
                SqliteAgendaTaskRepository.Read(record), record.LastCompletedAt)).ToArray());
        }
        catch (Exception exception) when (exception is DbException or FormatException or InvalidCastException)
        {
            throw new AgendaPersistenceException("The retrospective source could not be read.");
        }
    }

    internal async Task ValidateForBackupAsync(CancellationToken cancellationToken) =>
        await ListAsync(DateOnly.MinValue, DateOnly.MaxValue, cancellationToken);

    private static AgendaWeekRetrospective Validate(AgendaWeekRetrospective report, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(timeZone);
        if (report.StartDate.DayOfWeek != DayOfWeek.Monday || !string.Equals(report.TimeZoneId, timeZone.Id, StringComparison.Ordinal))
            throw new ArgumentException("The retrospective week or time zone is invalid.", nameof(report));
        var expected = new AgendaRetrospectiveBuilder().Build(report.StartDate, report.Tasks, report.ClosedAt, timeZone);
        if (!string.Equals(JsonSerializer.Serialize(report, Json), JsonSerializer.Serialize(expected, Json), StringComparison.Ordinal))
            throw new ArgumentException("The retrospective does not match its captured data.", nameof(report));
        return expected;
    }

    private static AgendaWeekRetrospective Read(AgendaWeekRetrospectiveRecord record)
    {
        if (record.FormatVersion != FormatVersion)
            throw new AgendaPersistenceException("The stored retrospective format is unsupported.");
        var timeZone = TimeZoneInfo.FromSerializedString(record.TimeZoneData);
        var report = JsonSerializer.Deserialize<AgendaWeekRetrospective>(record.Content, Json)
            ?? throw new AgendaPersistenceException("The stored retrospective is missing.");
        if (record.StartDate != report.StartDate || record.EndDate != report.EndDate || !record.ClosedAt.EqualsExact(report.ClosedAt) ||
            !string.Equals(record.TimeZoneId, report.TimeZoneId, StringComparison.Ordinal))
            throw new AgendaPersistenceException("The stored retrospective header is inconsistent.");
        return Validate(report, timeZone);
    }

    private static bool InvalidStoredData(Exception exception) => exception is
        DbException or FormatException or InvalidCastException or JsonException or ArgumentException or SerializationException or InvalidTimeZoneException;
}
