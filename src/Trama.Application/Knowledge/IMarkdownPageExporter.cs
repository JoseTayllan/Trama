namespace Trama.Application.Knowledge;

public interface IMarkdownPageExporter
{
    Task ExportAsync(NotebookSnapshot page, string destination,
        Func<Guid, CancellationToken, Task<Stream?>> openImage, CancellationToken cancellationToken = default);
}
