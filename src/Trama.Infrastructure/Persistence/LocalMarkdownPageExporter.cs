using System.Text;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class LocalMarkdownPageExporter : IMarkdownPageExporter
{
    public async Task ExportAsync(NotebookSnapshot page, string destination,
        Func<Guid, CancellationToken, Task<Stream?>> openImage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(openImage);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("An absolute destination is required.", nameof(destination));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
        var parent = Path.GetDirectoryName(target);
        if (parent is null || !Directory.Exists(parent) || Directory.Exists(target) || File.Exists(target))
            throw new IOException("Choose a new folder inside an existing directory.");
        var draft = new NotebookDraft();
        foreach (var block in page.Blocks)
        {
            draft.Insert(draft.Blocks.Count, block);
            if (block is NotebookTextBlock text) _ = new InlineTextDocument(text.Text, text.Styles);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var staging = Path.Combine(parent, ".trama-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var paths = new Dictionary<Guid, string>();
            foreach (var id in page.Blocks.OfType<NotebookImageBlock>().Select(image => image.AttachmentId).Distinct())
            {
                await using var source = await openImage(id, cancellationToken) ?? throw new IOException("An image is unavailable.");
                var header = new byte[8];
                var length = await source.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
                var extension = length == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
                    : length >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255 ? ".jpg"
                    : throw new IOException("Unsupported image format.");
                var relative = "imagens/" + id.ToString("N") + extension;
                Directory.CreateDirectory(Path.Combine(staging, "imagens"));
                await using (var file = new FileStream(Path.Combine(staging, relative), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await file.WriteAsync(header.AsMemory(0, length), cancellationToken);
                    await source.CopyToAsync(file, cancellationToken);
                }
                paths.Add(id, relative);
            }
            var markdown = MarkdownPageFormatter.Format(page, paths);
            await File.WriteAllTextAsync(Path.Combine(staging, "pagina.md"), markdown, new UTF8Encoding(false), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(staging, target);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }
}
