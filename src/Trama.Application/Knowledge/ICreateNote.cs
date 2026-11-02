namespace Trama.Application.Knowledge;

public interface ICreateNote
{
    Task<CreateNoteResult> ExecuteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);
}
