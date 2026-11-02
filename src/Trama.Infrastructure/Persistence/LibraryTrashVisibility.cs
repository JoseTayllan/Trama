using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

internal static class LibraryTrashVisibility
{
    // Older backups and migration fixtures may be read before the trash migration.
    internal static Task<bool> HasTableAsync(TramaDbContext context, CancellationToken cancellationToken) =>
        context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'LibraryTrash'")
            .AnyAsync(count => count > 0, cancellationToken);

    internal static async Task<bool> IsHiddenAsync(TramaDbContext context, LibraryEntryKind kind, Guid id, CancellationToken cancellationToken) =>
        await HasTableAsync(context, cancellationToken) &&
        await context.Set<LibraryTrashRecord>().AnyAsync(item => item.Kind == kind && item.EntryId == id, cancellationToken);

    internal static async Task RequireActiveAsync(TramaDbContext context, LibraryEntryKind kind, Guid id, CancellationToken cancellationToken)
    {
        if (await IsHiddenAsync(context, kind, id, cancellationToken))
            throw new NotePersistenceException("The entry is in the trash and cannot be modified.");
    }
}
