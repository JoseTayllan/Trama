namespace Trama.Application.Knowledge;

public interface INoteRepository
{
    Task AddAsync(NoteSnapshot note, CancellationToken cancellationToken = default);
}
