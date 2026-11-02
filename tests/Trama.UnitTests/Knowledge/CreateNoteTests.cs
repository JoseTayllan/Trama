using Trama.Application.Knowledge;

namespace Trama.UnitTests.Knowledge;

public sealed class CreateNoteTests
{
    [Fact]
    public async Task ExecuteAsync_WithOnlyTitle_CreatesNote()
    {
        var repository = new RecordingNoteRepository();
        var createNote = new CreateNote(repository);
        var request = new CreateNoteRequest("Study C#", string.Empty);

        var result = await createNote.ExecuteAsync(request);

        Assert.Equal(CreateNoteStatus.Created, result.Status);
        var note = Assert.IsType<NoteSnapshot>(result.Note);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(request.Title, note.Title);
        Assert.Equal(request.Body, note.Body);
        Assert.Equal(note, Assert.Single(repository.AddAttempts));
    }

    [Fact]
    public async Task ExecuteAsync_WithOnlyBody_CreatesNote()
    {
        var repository = new RecordingNoteRepository();
        var createNote = new CreateNote(repository);
        var request = new CreateNoteRequest(string.Empty, "Review async and await.");

        var result = await createNote.ExecuteAsync(request);

        Assert.Equal(CreateNoteStatus.Created, result.Status);
        var note = Assert.IsType<NoteSnapshot>(result.Note);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(request.Title, note.Title);
        Assert.Equal(request.Body, note.Body);
        Assert.Equal(note, Assert.Single(repository.AddAttempts));
    }

    [Fact]
    public async Task ExecuteAsync_WithTitleAndBody_CreatesNoteWithoutProjectOrTags()
    {
        var repository = new RecordingNoteRepository();
        var createNote = new CreateNote(repository);
        var request = new CreateNoteRequest("Trama idea", "Capture notes before organizing them.");

        var result = await createNote.ExecuteAsync(request);

        Assert.Equal(CreateNoteStatus.Created, result.Status);
        var note = Assert.IsType<NoteSnapshot>(result.Note);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(request.Title, note.Title);
        Assert.Equal(request.Body, note.Body);
        Assert.Equal(note, Assert.Single(repository.AddAttempts));
    }

    [Fact]
    public async Task ExecuteAsync_WithSpacesAndCode_PreservesExactContent()
    {
        var repository = new RecordingNoteRepository();
        var createNote = new CreateNote(repository);
        const string title = "  Async example\t ";
        const string body = "\r\n```csharp\r\n    await SaveAsync();\r\n```\n\t ";

        var result = await createNote.ExecuteAsync(new CreateNoteRequest(title, body));

        Assert.Equal(CreateNoteStatus.Created, result.Status);
        var note = Assert.IsType<NoteSnapshot>(result.Note);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(title, note.Title);
        Assert.Equal(body, note.Body);
        Assert.Equal(note, Assert.Single(repository.AddAttempts));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData("", null)]
    public async Task ExecuteAsync_WithBothFieldsEmpty_ReturnsValidationFailureWithoutSaving(
        string? title,
        string? body)
    {
        var repository = new RecordingNoteRepository();
        var createNote = new CreateNote(repository);

        var result = await createNote.ExecuteAsync(new CreateNoteRequest(title, body));

        Assert.Equal(CreateNoteStatus.ValidationFailed, result.Status);
        Assert.Null(result.Note);
        Assert.Empty(repository.AddAttempts);
    }

    [Theory]
    [InlineData("   ", "\t\r\n")]
    [InlineData("\t", " ")]
    [InlineData("\r\n", "\n")]
    [InlineData("", " \t")]
    [InlineData("\r\n ", null)]
    public async Task ExecuteAsync_WithBothFieldsBlank_ReturnsValidationFailureWithoutSaving(
        string? title,
        string? body)
    {
        var repository = new RecordingNoteRepository();
        var createNote = new CreateNote(repository);

        var result = await createNote.ExecuteAsync(new CreateNoteRequest(title, body));

        Assert.Equal(CreateNoteStatus.ValidationFailed, result.Status);
        Assert.Null(result.Note);
        Assert.Empty(repository.AddAttempts);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSavingFails_ReturnsPersistenceFailureWithoutSuccess()
    {
        var repository = new RecordingNoteRepository
        {
            Failure = new NotePersistenceException("Simulated storage failure.")
        };
        var createNote = new CreateNote(repository);

        var result = await createNote.ExecuteAsync(new CreateNoteRequest("Study plan", "Read chapter one."));

        Assert.Equal(CreateNoteStatus.PersistenceFailed, result.Status);
        Assert.Null(result.Note);
        Assert.Single(repository.AddAttempts);
    }

    private sealed class RecordingNoteRepository : INoteRepository
    {
        public List<NoteSnapshot> AddAttempts { get; } = [];

        public NotePersistenceException? Failure { get; init; }

        public Task AddAsync(NoteSnapshot note, CancellationToken cancellationToken = default)
        {
            AddAttempts.Add(note);

            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
