using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;
using Trama.Application.Storage;

namespace Trama.Infrastructure.Persistence;

public sealed partial class LocalBackupService
{
    private async Task<BackupManifest> ReadManifestAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        if (archive.Entries.Count > _limits.MaximumEntries || archive.Entries.Count < 2)
            throw new InvalidDataException("Invalid backup entry count.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long declaredSize = 0;
        foreach (var entry in archive.Entries)
        {
            if (!ValidName(entry.FullName) || !names.Add(entry.FullName)) throw new InvalidDataException("Invalid or duplicated backup path.");
            if (entry.Length < 0 || entry.Length > _limits.MaximumExpandedBytes - declaredSize)
                throw new InvalidDataException("Expanded backup size limit exceeded.");
            declaredSize += entry.Length;
        }
        var metadata = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("Missing backup manifest.");
        if (metadata.Length > MaximumManifestBytes) throw new InvalidDataException("Backup manifest is too large.");
        BackupManifest manifest;
        try
        {
            await using var stream = metadata.Open();
            using var bytes = new MemoryStream();
            var copied = await CopyAsync(stream, bytes, Math.Min(MaximumManifestBytes, _limits.MaximumExpandedBytes), cancellationToken);
            if (copied.Length != metadata.Length) throw new InvalidDataException("Invalid manifest length.");
            manifest = JsonSerializer.Deserialize<BackupManifest>(bytes.ToArray(), JsonOptions)
                ?? throw new InvalidDataException("Missing backup manifest.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Invalid backup manifest.", exception);
        }
        if (manifest.FormatVersion != 1 || manifest.Files is null || manifest.Files.Count != archive.Entries.Count - 1)
            throw new InvalidDataException("Unsupported backup format or file list.");
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files)
        {
            if (file is null || file.Path is null || file.Path == "manifest.json" || !ValidName(file.Path) || !files.Add(file.Path) ||
                file.Length < 0 || file.Sha256 is null || file.Sha256.Length != 64 || file.Sha256.Any(character => !char.IsAsciiHexDigitLower(character)))
                throw new InvalidDataException("Invalid backup file metadata.");
            var entry = archive.GetEntry(file.Path);
            if (entry is null || entry.Length != file.Length) throw new InvalidDataException("Backup entry does not match its manifest.");
        }
        if (!files.Contains("trama.db")) throw new InvalidDataException("Missing backup database.");
        return manifest;
    }

    private static bool ValidName(string name)
    {
        if (name is "manifest.json" or "trama.db") return true;
        const string prefix = "attachments/";
        const string suffix = ".image";
        if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal)) return false;
        var id = name[prefix.Length..^suffix.Length];
        return Guid.TryParseExact(id, "N", out var parsed) && parsed != Guid.Empty && id == parsed.ToString("N");
    }

    private static async Task<IReadOnlyList<Guid>> ValidateDatabaseAsync(string path, CancellationToken cancellationToken, bool upgrade = false)
    {
        try
        {
            await using var connection = new SqliteConnection(Connection(path, upgrade ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly));
            await connection.OpenAsync(cancellationToken);
            await using (var check = connection.CreateCommand())
            {
                check.CommandText = "PRAGMA quick_check;";
                if (!string.Equals(await check.ExecuteScalarAsync(cancellationToken) as string, "ok", StringComparison.Ordinal))
                    throw new InvalidDataException("Invalid SQLite database.");
                check.CommandText = "PRAGMA foreign_key_check;";
                await using var foreignKeys = await check.ExecuteReaderAsync(cancellationToken);
                if (await foreignKeys.ReadAsync(cancellationToken)) throw new InvalidDataException("Invalid database references.");
            }
            var options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection).Options;
            await using var context = new TramaDbContext(options);
            var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Order(StringComparer.Ordinal).ToArray();
            var supported = context.Database.GetMigrations().Order(StringComparer.Ordinal).ToArray();
            if (applied.Length == 0 || !applied.SequenceEqual(supported.Take(applied.Length))) throw new InvalidDataException("Unsupported database schema.");
            if (applied.Length != supported.Length)
            {
                if (!upgrade) throw new InvalidDataException("Update the source profile before creating a backup.");
                await context.Database.MigrateAsync(cancellationToken);
            }
            var pages = await context.Set<NotebookPageRecord>().AsNoTracking().Select(page => page.Id).ToArrayAsync(cancellationToken);
            var reader = new SqliteNotebookRepository(context);
            foreach (var id in pages)
                if (await reader.ReadStoredForBackupAsync(id, cancellationToken) is null)
                    throw new InvalidDataException("Missing stored page.");
            await ValidateTrashAsync(context, cancellationToken);
            await new SqliteAgendaTaskRepository(context).ValidateForBackupAsync(cancellationToken);
            if (await context.Set<PageTagRecord>().AnyAsync(tag =>
                !context.Set<NotebookPageRecord>().Any(page => page.Id == tag.PageId) &&
                !context.Set<NoteRecord>().Any(note => note.Id == tag.PageId), cancellationToken))
                throw new InvalidDataException("Tags refer to a missing page.");
            if (await context.Set<PageLocationRecord>().AnyAsync(location =>
                !context.Set<NotebookPageRecord>().Any(page => page.Id == location.PageId) &&
                !context.Set<NoteRecord>().Any(note => note.Id == location.PageId), cancellationToken))
                throw new InvalidDataException("The catalog refers to a missing page.");
            var images = await context.Set<NotebookBlockRecord>().AsNoTracking().Where(block => block.Kind == "image")
                .Select(block => block.AttachmentId).Distinct().ToArrayAsync(cancellationToken);
            if (images.Any(id => id is null || id == Guid.Empty)) throw new InvalidDataException("Invalid image reference.");
            return images.Select(id => id.GetValueOrDefault()).Order().ToArray();
        }
        catch (SqliteException exception)
        {
            throw new InvalidDataException("The backup database could not be validated.", exception);
        }
        catch (Trama.Application.Knowledge.NotePersistenceException exception)
        {
            throw new InvalidDataException("The backup contains invalid page data.", exception);
        }
        catch (Trama.Application.Agenda.AgendaPersistenceException exception)
        {
            throw new InvalidDataException("The backup contains invalid agenda data.", exception);
        }
    }

    private static async Task ValidateTrashAsync(TramaDbContext context, CancellationToken cancellationToken)
    {
        var entries = await context.Set<LibraryTrashRecord>().AsNoTracking().ToArrayAsync(cancellationToken);
        if (entries.Length == 0) return;
        var spaces = (await context.Set<KnowledgeSpaceRecord>().AsNoTracking().Select(item => item.Id).ToArrayAsync(cancellationToken)).ToHashSet();
        var books = await context.Set<NotebookRecord>().AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
        var pages = (await context.Set<NotebookPageRecord>().AsNoTracking().Select(item => item.Id).ToArrayAsync(cancellationToken)).ToHashSet();
        pages.UnionWith(await context.Set<NoteRecord>().AsNoTracking().Select(item => item.Id).ToArrayAsync(cancellationToken));
        var locations = await context.Set<PageLocationRecord>().AsNoTracking().ToDictionaryAsync(item => item.PageId, cancellationToken);
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.EntryId == Guid.Empty || entry.BatchId == Guid.Empty || !Enum.IsDefined(entry.Kind))
                throw new InvalidDataException("Invalid trash identity.");
            var exists = entry.Kind switch
            {
                LibraryEntryKind.Space => spaces.Contains(entry.EntryId),
                LibraryEntryKind.Notebook => books.ContainsKey(entry.EntryId),
                LibraryEntryKind.Page => pages.Contains(entry.EntryId),
                _ => false
            };
            if (!exists) throw new InvalidDataException("Trash entry refers to missing content.");
        }
        foreach (var batch in entries.GroupBy(item => item.BatchId))
        {
            var roots = batch.Where(item => item.IsRoot).ToArray();
            if (roots.Length != 1) throw new InvalidDataException("Invalid trash operation root.");
            var root = roots[0];
            var batchBooks = batch.Where(item => item.Kind == LibraryEntryKind.Notebook).Select(item => item.EntryId).ToHashSet();
            if (root.Kind == LibraryEntryKind.Page && batch.Count() != 1)
                throw new InvalidDataException("A page trash operation contains unrelated entries.");
            foreach (var child in batch.Where(item => !item.IsRoot))
            {
                var valid = root.Kind switch
                {
                    LibraryEntryKind.Space when child.Kind == LibraryEntryKind.Notebook => books[child.EntryId].SpaceId == root.EntryId,
                    LibraryEntryKind.Space when child.Kind == LibraryEntryKind.Page => locations.TryGetValue(child.EntryId, out var location)
                        && batchBooks.Contains(location.NotebookId),
                    LibraryEntryKind.Notebook when child.Kind == LibraryEntryKind.Page => locations.TryGetValue(child.EntryId, out var location)
                        && location.NotebookId == root.EntryId,
                    _ => false
                };
                if (!valid) throw new InvalidDataException("Trash operation contains unrelated entries.");
            }
        }
    }
}
