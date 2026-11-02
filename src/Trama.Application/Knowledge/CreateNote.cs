namespace Trama.Application.Knowledge;

public sealed class CreateNote
{
    private readonly INoteRepository _repository;

    public CreateNote(INoteRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<CreateNoteResult> ExecuteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Body))
        {
            return new CreateNoteResult(CreateNoteStatus.ValidationFailed, null);
        }

        var note = new NoteSnapshot(Guid.NewGuid(), request.Title ?? string.Empty, request.Body ?? string.Empty);

        try
        {
            await _repository.AddAsync(note, cancellationToken);
        }
        catch (NotePersistenceException)
        {
            return new CreateNoteResult(CreateNoteStatus.PersistenceFailed, null);
        }

        return new CreateNoteResult(CreateNoteStatus.Created, note);
    }
}
