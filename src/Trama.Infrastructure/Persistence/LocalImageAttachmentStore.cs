using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class LocalImageAttachmentStore : IImageAttachmentStore
{
    private const int MaximumBytes = 10 * 1024 * 1024;
    private readonly string _attachmentsDirectory;

    public LocalImageAttachmentStore(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory) || !Path.IsPathFullyQualified(dataDirectory))
        {
            throw new ArgumentException("An absolute data directory is required.", nameof(dataDirectory));
        }

        _attachmentsDirectory = Path.Combine(Path.GetFullPath(dataDirectory), "attachments");
    }

    public async Task<ImageAttachment> ImportAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.CanRead)
        {
            throw new ArgumentException("A readable stream is required.", nameof(source));
        }

        Directory.CreateDirectory(_attachmentsDirectory);
        var id = Guid.NewGuid();
        var temporaryPath = Path.Combine(_attachmentsDirectory, $".{id:N}.tmp");
        var temporaryCreated = false;
        try
        {
            string mediaType;
            await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, FileOptions.Asynchronous))
            {
                temporaryCreated = true;
                var buffer = new byte[81920];
                var header = new byte[8];
                var headerLength = 0;
                var total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    total += read;
                    if (total > MaximumBytes)
                    {
                        throw new InvalidDataException("The image exceeds the maximum size of 10 MiB.");
                    }

                    var headerBytes = Math.Min(header.Length - headerLength, read);
                    buffer.AsSpan(0, headerBytes).CopyTo(header.AsSpan(headerLength));
                    headerLength += headerBytes;
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                mediaType = DetectMediaType(header.AsSpan(0, headerLength));
                await destination.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, AttachmentPath(id));
            return new ImageAttachment(id, mediaType);
        }
        finally
        {
            if (temporaryCreated)
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public Task<Stream?> OpenReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An attachment identity is required.", nameof(id));
        }

        try
        {
            Stream stream = new FileStream(AttachmentPath(id), FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, FileOptions.Asynchronous);
            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    private string AttachmentPath(Guid id) => Path.Combine(_attachmentsDirectory, $"{id:N}.image");

    private static string DetectMediaType(ReadOnlySpan<byte> header)
    {
        if (header.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return "image/png";
        }

        if (header.Length >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255)
        {
            return "image/jpeg";
        }

        throw new InvalidDataException("A PNG or JPEG image header is required.");
    }
}
