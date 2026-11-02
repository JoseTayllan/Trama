namespace Trama.Application.Storage;

public sealed record BackupReceipt(string ArchivePath, int AttachmentCount);
public sealed record RestoreReceipt(string DataDirectory);
public sealed record BackupFileEntry(string Path, long Length, string Sha256);
public sealed record BackupManifest(int FormatVersion, IReadOnlyList<BackupFileEntry> Files);
public sealed record BackupArchiveLimits(long MaximumExpandedBytes = 1_073_741_824, int MaximumEntries = 10_000);

public interface ILocalBackup
{
    Task<BackupReceipt> CreateAsync(string archivePath, CancellationToken cancellationToken = default);
    Task<RestoreReceipt> RestoreAsync(string archivePath, string destinationDirectory, CancellationToken cancellationToken = default);
}
