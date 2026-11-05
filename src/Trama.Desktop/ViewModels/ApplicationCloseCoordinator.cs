using CommunityToolkit.Mvvm.ComponentModel;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed class ApplicationCloseCoordinator : ObservableObject, IDisposable
{
    private readonly LibraryViewModel _library;
    private readonly AgendaViewModel _agenda;
    private readonly IAgendaCompletionActions _actions;
    private readonly IAgendaCompletionPrompt _prompt;
    private readonly TimeProvider _clock;
    private IDisposable? _autoSavePause;
    private bool _isBusy;
    private bool _approved;
    private bool _disposed;
    private string _feedback = string.Empty;

    public ApplicationCloseCoordinator(LibraryViewModel library, AgendaViewModel agenda,
        IAgendaCompletionActions actions, IAgendaCompletionPrompt prompt, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(agenda);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(clock);
        _library = library; _agenda = agenda; _actions = actions; _prompt = prompt; _clock = clock;
    }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }

    public async Task<bool> RequestCloseAsync()
    {
        if (_disposed || IsBusy || _approved) return false;
        if (!_library.CanNavigate || _agenda.IsBusy)
        {
            Feedback = "Aguarde a operação atual antes de sair."; return false;
        }
        IsBusy = true; Feedback = string.Empty;
        _autoSavePause = _library.Editor.PauseAutoSave();
        try
        {
            if (!await _agenda.RequestCloseAsync() || !await _library.PrepareCloseAsync())
            {
                Feedback = "Saída cancelada. Seus rascunhos foram mantidos."; return false;
            }
            var day = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
            var candidates = await _actions.ListCandidatesAsync(day);
            if (_disposed) return false;
            if (candidates.Count == 0) { _approved = true; return true; }
            var decision = await _prompt.ChooseAsync(candidates);
            if (_disposed) return false;
            if (decision.Choice == AgendaCompletionChoice.Cancel)
            {
                Feedback = "Saída cancelada. Você pode continuar trabalhando."; return false;
            }
            if (decision.Choice == AgendaCompletionChoice.KeepOpen) { _approved = true; return true; }
            var ids = decision.SelectedTaskIds?.ToArray();
            if (decision.Choice != AgendaCompletionChoice.CompleteSelected || ids is null || ids.Length == 0 ||
                ids.Distinct().Count() != ids.Length || ids.Any(id => !candidates.Any(task => task.Id == id)))
            {
                Feedback = "Selecione tarefas válidas antes de encerrar."; return false;
            }
            var byId = candidates.ToDictionary(task => task.Id);
            var selected = ids.Select(id => byId[id]).ToArray();
            var now = _clock.GetLocalNow();
            var completedAt = decision.CompletedAt ?? now;
            var completionDay = DateOnly.FromDateTime(completedAt.DateTime);
            if (completedAt > now || selected.Any(task => task.PlannedDate > completionDay))
            {
                Feedback = "Informe um encerramento até agora e a partir do dia planejado de cada tarefa."; return false;
            }
            var result = await _actions.CompleteAsync(selected, completedAt);
            if (result.Status != AgendaTaskCompletionStatus.Completed)
            {
                Feedback = "Não foi possível encerrar as tarefas. A saída foi cancelada; confira a Agenda e tente novamente."; return false;
            }
            _approved = true; return true;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível preparar a saída. Suas tarefas e seus rascunhos foram mantidos."; return false;
        }
        finally
        {
            IsBusy = false;
            // Approval is provisional until the window actually closes. Do not autosave a discarded draft in that interval.
            if (!_approved) ResumeAutoSave();
        }
    }

    internal void CancelPendingClose()
    {
        if (IsBusy) return;
        _approved = false; ResumeAutoSave();
    }

    private void ResumeAutoSave()
    {
        var pause = _autoSavePause; _autoSavePause = null; pause?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; ResumeAutoSave();
    }
}
