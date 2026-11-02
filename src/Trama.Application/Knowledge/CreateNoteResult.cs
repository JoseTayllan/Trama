namespace Trama.Application.Knowledge;

public enum CreateNoteStatus
{
    Created,
    ValidationFailed,
    PersistenceFailed
}

public sealed record CreateNoteResult(CreateNoteStatus Status, NoteSnapshot? Note);
