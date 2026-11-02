namespace Trama.Application.Knowledge;

public sealed record ImageAttachment(Guid Id, string MediaType);

public interface IImageAttachmentStore
{
    Task<ImageAttachment> ImportAsync(Stream source, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(Guid id, CancellationToken cancellationToken = default);
}
