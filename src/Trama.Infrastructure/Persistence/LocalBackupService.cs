using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Trama.Application.Storage;

namespace Trama.Infrastructure.Persistence;

public sealed partial class LocalBackupService : ILocalBackup
{
    private const int MaximumManifestBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _dataDirectory;
    private readonly BackupArchiveLimits _limits;

    public LocalBackupService(string dataDirectory, BackupArchiveLimits? limits = null)
    {
        _dataDirectory = Absolute(dataDirectory);
        _limits = limits ?? new BackupArchiveLimits();
        if (_limits.MaximumEntries <= 0 || _limits.MaximumExpandedBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }

    public async Task<BackupReceipt> CreateAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        archivePath = Absolute(archivePath);
        RefuseExisting(archivePath);
        var parent = ExistingParent(archivePath);
        var stage = Path.Combine(parent, $".trama-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stage);
        try
        {
            var snapshot = Path.Combine(stage, "trama.db");
            await Task.Run(() => Snapshot(snapshot), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var images = await ValidateDatabaseAsync(snapshot, cancellationToken);
            if (images.Count + 2 > _limits.MaximumEntries) throw new InvalidDataException("Too many backup entries.");
            var files = new List<BackupFileEntry>();
            long used = 0;
            await using (var database = File.OpenRead(snapshot))
            {
                if (database.Length > _limits.MaximumExpandedBytes) throw new InvalidDataException("Backup size limit exceeded.");
                files.Add(new BackupFileEntry("trama.db", database.Length, Hex(await SHA256.HashDataAsync(database, cancellationToken))));
                used = database.Length;
            }
            Directory.CreateDirectory(Path.Combine(stage, "attachments"));
            foreach (var id in images)
            {
                var name = $"attachments/{id:N}.image";
                var sourcePath = Path.Combine(_dataDirectory, "attachments", $"{id:N}.image");
                if (!File.Exists(sourcePath) || File.GetAttributes(sourcePath).HasFlag(FileAttributes.ReparsePoint))
                    throw new InvalidDataException("A referenced image is missing or is not a regular file.");
                await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
                await using var destination = new FileStream(StagePath(stage, name), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                var copied = await CopyAsync(source, destination, _limits.MaximumExpandedBytes - used, cancellationToken);
                used += copied.Length;
                files.Add(new BackupFileEntry(name, copied.Length, copied.Hash));
            }
            var manifest = JsonSerializer.SerializeToUtf8Bytes(new BackupManifest(1, files), JsonOptions);
            if (manifest.Length > MaximumManifestBytes || manifest.Length > _limits.MaximumExpandedBytes - used)
                throw new InvalidDataException("Backup size limit exceeded.");
            var package = Path.Combine(stage, "package.tmp");
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    await using var input = File.OpenRead(StagePath(stage, file.Path));
                    await using var output = archive.CreateEntry(file.Path, CompressionLevel.Optimal).Open();
                    await input.CopyToAsync(output, cancellationToken);
                }
                await using var metadata = archive.CreateEntry("manifest.json", CompressionLevel.Optimal).Open();
                await metadata.WriteAsync(manifest, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(package, archivePath, overwrite: false);
            return new BackupReceipt(archivePath, images.Count);
        }
        finally
        {
            Cleanup(stage, parent);
        }
    }

    public async Task<RestoreReceipt> RestoreAsync(string archivePath, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        archivePath = Absolute(archivePath);
        destinationDirectory = Absolute(destinationDirectory);
        RefuseExisting(destinationDirectory);
        var parent = ExistingParent(destinationDirectory);
        var stage = Path.Combine(parent, $".trama-restore-{Guid.NewGuid():N}");
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            var manifest = await ReadManifestAsync(archive, cancellationToken);
            Directory.CreateDirectory(stage);
            Directory.CreateDirectory(Path.Combine(stage, "attachments"));
            var used = archive.GetEntry("manifest.json")!.Length;
            foreach (var file in manifest.Files)
            {
                var entry = archive.GetEntry(file.Path) ?? throw new InvalidDataException("Missing backup file.");
                await using var input = entry.Open();
                await using var output = new FileStream(StagePath(stage, file.Path), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                var copied = await CopyAsync(input, output, _limits.MaximumExpandedBytes - used, cancellationToken);
                used += copied.Length;
                if (copied.Length != file.Length || copied.Hash != file.Sha256) throw new InvalidDataException("Backup integrity check failed.");
            }
            var references = await ValidateDatabaseAsync(Path.Combine(stage, "trama.db"), cancellationToken, upgrade: true);
            var expectedImages = references.Select(id => $"attachments/{id:N}.image").ToHashSet(StringComparer.Ordinal);
            var packagedImages = manifest.Files.Where(file => file.Path != "trama.db").Select(file => file.Path).ToHashSet(StringComparer.Ordinal);
            if (!expectedImages.SetEquals(packagedImages)) throw new InvalidDataException("Image references do not match the package.");
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(stage, destinationDirectory);
            return new RestoreReceipt(destinationDirectory);
        }
        finally
        {
            Cleanup(stage, parent);
        }
    }

    private void Snapshot(string destination)
    {
        var sourcePath = Path.Combine(_dataDirectory, "trama.db");
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("The source database does not exist.");
        // ReadWrite (without Create) lets SQLite clean up its own transient WAL sidecars on close.
        // Never delete source WAL/SHM files ourselves: they may belong to an active writer.
        using var source = new SqliteConnection(Connection(sourcePath, SqliteOpenMode.ReadWrite));
        using var target = new SqliteConnection(Connection(destination, SqliteOpenMode.ReadWriteCreate));
        source.Open();
        target.Open();
        source.BackupDatabase(target);
        using var command = target.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=DELETE;";
        command.ExecuteNonQuery();
    }

    private static string Connection(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false }.ToString();

    private static string Absolute(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("An absolute path is required.", nameof(path));
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static void RefuseExisting(string path)
    {
        if (File.Exists(path) || Directory.Exists(path)) throw new IOException("The destination already exists.");
    }

    private static string ExistingParent(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (parent is null || !Directory.Exists(parent)) throw new DirectoryNotFoundException("Select an existing parent directory.");
        return parent;
    }

    private static string StagePath(string stage, string name) => Path.Combine(stage, name.Replace('/', Path.DirectorySeparatorChar));
    private static string Hex(byte[] hash) => Convert.ToHexString(hash).ToLowerInvariant();

    private static async Task<(long Length, string Hash)> CopyAsync(Stream input, Stream output, long limit, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (read > limit - total) throw new InvalidDataException("Expanded backup size limit exceeded.");
            total += read;
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return (total, Hex(hash.GetHashAndReset()));
    }

    private static void Cleanup(string stage, string parent)
    {
        // Delete only the operation-owned sibling directory, never a payload-provided path.
        var resolved = Path.GetFullPath(stage);
        if (!string.Equals(Path.GetDirectoryName(resolved), parent, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(resolved).StartsWith(".trama-", StringComparison.Ordinal))
            throw new IOException("Invalid staging directory.");
        if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
    }
}
