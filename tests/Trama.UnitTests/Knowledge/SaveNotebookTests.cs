using Trama.Application.Knowledge;

namespace Trama.UnitTests.Knowledge;

public sealed class SaveNotebookTests
{
    [Fact]
    public async Task EmptyTitleAndWhitespaceBlocks_AreRejectedWithoutWriting()
    {
        var repository = new RecordingRepository();
        var notebook = new NotebookSnapshot(Guid.NewGuid(), " \t", [new NotebookTextBlock(Guid.NewGuid(), "\n ")]);

        var result = await new SaveNotebook(repository).ExecuteAsync(notebook);

        Assert.Equal(NotebookSaveStatus.ValidationFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task TitleOnlyPage_IsSavedWithoutChangingItsTitle()
    {
        var repository = new RecordingRepository();
        var notebook = new NotebookSnapshot(Guid.NewGuid(), "  Estudo 🧵  ", []);

        var result = await new SaveNotebook(repository).ExecuteAsync(notebook);

        Assert.Equal(NotebookSaveStatus.Saved, result.Status);
        Assert.Same(notebook, Assert.Single(repository.Saved));
    }

    [Fact]
    public async Task ImageOnlyPage_IsValidWithoutARequiredCaption()
    {
        var repository = new RecordingRepository();
        var notebook = new NotebookSnapshot(Guid.NewGuid(), "", [new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "")]);

        var result = await new SaveNotebook(repository).ExecuteAsync(notebook);

        Assert.Equal(NotebookSaveStatus.Saved, result.Status);
        Assert.Same(notebook, Assert.Single(repository.Saved));
    }

    [Fact]
    public async Task CodeOnlyPage_IsSavedWithoutNormalizingItsCode()
    {
        var repository = new RecordingRepository();
        var code = new NotebookCodeBlock(Guid.NewGuid(), "\r\n    await SaveAsync();\t\n", "csharp");
        var notebook = new NotebookSnapshot(Guid.NewGuid(), "", [code]);

        var result = await new SaveNotebook(repository).ExecuteAsync(notebook);

        Assert.Equal(NotebookSaveStatus.Saved, result.Status);
        Assert.Equal(code, Assert.Single(Assert.Single(repository.Saved).Blocks));
    }

    [Fact]
    public async Task PersistenceFailure_ReturnsFailureInsteadOfSuccess()
    {
        var repository = new RecordingRepository { Failure = new NotePersistenceException("Synthetic failure.") };
        var notebook = new NotebookSnapshot(Guid.NewGuid(), "Manter", []);

        var result = await new SaveNotebook(repository).ExecuteAsync(notebook);

        Assert.Equal(NotebookSaveStatus.PersistenceFailed, result.Status);
    }

    [Fact]
    public async Task PreCancellation_DoesNotReachTheRepository()
    {
        var repository = new RecordingRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SaveNotebook(repository).ExecuteAsync(
            new NotebookSnapshot(Guid.NewGuid(), "Título", []), cancellation.Token));

        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Save_DoesNotReportSuccessBeforeTheRepositoryCompletes()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new RecordingRepository { Pending = pending.Task };
        var save = new SaveNotebook(repository).ExecuteAsync(new NotebookSnapshot(Guid.NewGuid(), "Estudo", []));
        try
        {
            Assert.False(save.IsCompleted);
            Assert.Single(repository.Saved);
        }
        finally
        {
            pending.TrySetResult();
        }

        Assert.Equal(NotebookSaveStatus.Saved, (await save).Status);
    }

    private sealed class RecordingRepository : INotebookRepository
    {
        public List<NotebookSnapshot> Saved { get; } = [];
        public Exception? Failure { get; init; }
        public Task? Pending { get; init; }

        public Task SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Saved.Add(notebook);
            return Pending ?? Task.CompletedTask;
        }

        public Task<NotebookSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
