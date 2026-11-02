namespace Trama.Application.Knowledge;

public sealed class SaveNotebook
{
    private readonly INotebookRepository _repository;

    public SaveNotebook(INotebookRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<NotebookSaveResult> ExecuteAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notebook);
        ArgumentNullException.ThrowIfNull(notebook.Blocks);
        cancellationToken.ThrowIfCancellationRequested();
        var draft = new NotebookDraft();
        foreach (var block in notebook.Blocks)
        {
            draft.Insert(draft.Blocks.Count, block);
        }

        if (string.IsNullOrWhiteSpace(notebook.Title) && !draft.HasContent)
        {
            return new NotebookSaveResult(NotebookSaveStatus.ValidationFailed);
        }

        try
        {
            await _repository.SaveAsync(notebook, cancellationToken);
            return new NotebookSaveResult(NotebookSaveStatus.Saved);
        }
        catch (NotePersistenceException)
        {
            return new NotebookSaveResult(NotebookSaveStatus.PersistenceFailed);
        }
    }
}
