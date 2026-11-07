using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaViewModel
{
    private readonly IAgendaTrashActions? _trash;
    private readonly TimeProvider _trashClock;

    public bool HasTrash => _trash is not null;
    public bool CanDeleteTask => HasTrash && CanEdit && EditingTaskId is not null;

    public async Task<bool> MoveToTrashAsync(Guid taskId, Func<AgendaTaskSnapshot, Task<bool>> confirm)
    {
        ArgumentNullException.ThrowIfNull(confirm);
        if (_trash is null || IsBusy || taskId == Guid.Empty) return false;
        IsBusy = true;
        try
        {
            var task = await _actions.GetByIdAsync(taskId);
            if (task is null) { Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido."; return false; }
            if (!await confirm(task)) { Feedback = "Exclusão cancelada. O conteúdo foi mantido."; return false; }
            // Recheck before resolving a draft: another action may have removed the task while the prompt was open.
            if (await _actions.GetByIdAsync(taskId) is null)
            {
                Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido."; return false;
            }
            if (taskId == EditingTaskId && !await ResolveChangesAsync())
            {
                Feedback = "Exclusão cancelada. Confira as alterações não salvas."; return false;
            }
            if (await _actions.GetByIdAsync(taskId) is null)
            {
                Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido."; return false;
            }
            await _trash.MoveToTrashAsync(taskId, _trashClock.GetLocalNow());
            // A discarded draft remains available until the mutation actually succeeds.
            if (taskId == EditingTaskId) LoadDraft(null);
            await RefreshAfterTrashMutationAsync("Tarefa enviada para a lixeira. Você pode restaurá-la.");
            return true;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível enviar para a lixeira. Seu rascunho foi mantido."; return false;
        }
        finally { IsBusy = false; }
    }

    public Task<IReadOnlyList<AgendaTaskTrashEntry>> ListTrashAsync() =>
        _trash?.ListAsync() ?? Task.FromResult<IReadOnlyList<AgendaTaskTrashEntry>>(Array.Empty<AgendaTaskTrashEntry>());

    public async Task<bool> RestoreTrashEntryAsync(AgendaTaskTrashEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (_trash is null || IsBusy) return false;
        IsBusy = true;
        try
        {
            await _trash.RestoreAsync(entry.Task.Id);
            await RefreshAfterTrashMutationAsync("Tarefa restaurada com seu planejamento e estado originais.");
            return true;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível restaurar. Confira a lixeira e tente novamente."; return false;
        }
        finally { IsBusy = false; }
    }

    private async Task RefreshAfterTrashMutationAsync(string success)
    {
        try
        {
            Tasks = await _actions.ListAsync(SelectedDay, SelectedDay); HasListError = false; Feedback = success;
        }
        catch (Exception)
        {
            HasListError = true;
            Feedback = success + " Não foi possível atualizar a lista; use Atualizar para consultar.";
        }
        await RefreshActiveWeekAsync();
    }
}
