using Microsoft.EntityFrameworkCore;

namespace Trama.Infrastructure.Persistence;

internal sealed class NotebookPageEditRecord
{
    public Guid PageId { get; set; }
    public long EditedAtUtcTicks { get; set; }

    internal static async Task RecordAsync(TramaDbContext context, Guid pageId, CancellationToken cancellationToken)
    {
        // Older schema readers and migration fixtures can still save pages before this table exists.
        var hasTable = await context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'NotebookPageEdits'")
            .AnyAsync(count => count > 0, cancellationToken);
        if (!hasTable) return;

        var editedAt = DateTimeOffset.UtcNow.UtcTicks;
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO NotebookPageEdits (PageId, EditedAtUtcTicks) VALUES ({pageId}, {editedAt}) ON CONFLICT (PageId) DO UPDATE SET EditedAtUtcTicks = excluded.EditedAtUtcTicks",
            cancellationToken);
    }
}
