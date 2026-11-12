using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaRetrospectiveViewModel
{
    private readonly IAgendaReflectionActions? _reflectionActions;
    private (string WorkedWell, string Adjustments) _savedReflection = (string.Empty, string.Empty);
    private (string WorkedWell, string Adjustments) _reflection = (string.Empty, string.Empty);
    private string _reflectionFeedback = string.Empty;

    public bool HasReflectionEditor => _reflectionActions is not null && _loaded is not null;
    public bool HasReflectionDraft => HasReflectionEditor && _reflection != _savedReflection;
    public string ReflectionFeedback
    {
        get => _reflectionFeedback;
        private set => SetProperty(ref _reflectionFeedback, value);
    }

    public string WorkedWell
    {
        get => _reflection.WorkedWell;
        set => SetReflection((value ?? string.Empty, _reflection.Adjustments));
    }

    public string Adjustments
    {
        get => _reflection.Adjustments;
        set => SetReflection((_reflection.WorkedWell, value ?? string.Empty));
    }

    internal async Task<bool> SaveReflectionAsync(CancellationToken cancellationToken)
    {
        if (_reflectionActions is null || _loaded is not { } loaded || !HasReflectionDraft) return false;
        var draft = _reflection;
        try
        {
            var result = await _reflectionActions.SaveAsync(loaded.StartDate, draft.WorkedWell, draft.Adjustments, cancellationToken);
            if (result.Status != AgendaReflectionSaveStatus.Saved)
            {
                ReflectionFeedback = "Não foi possível salvar a reflexão. Seu rascunho foi mantido.";
                return false;
            }

            _savedReflection = draft;
            OnPropertyChanged(nameof(HasReflectionDraft));
            ReflectionFeedback = HasReflectionDraft ? "Versão salva. Há alterações mais recentes." : "Reflexão salva nesta semana.";
            return true;
        }
        catch (OperationCanceledException)
        {
            ReflectionFeedback = "Gravação cancelada. Seu rascunho foi mantido.";
            return false;
        }
        catch (Exception)
        {
            ReflectionFeedback = "Não foi possível salvar a reflexão. Seu rascunho foi mantido.";
            return false;
        }
    }

    private void LoadReflection(AgendaWeekReflection? reflection)
    {
        _savedReflection = (reflection?.WorkedWell ?? string.Empty, reflection?.Adjustments ?? string.Empty);
        _reflection = _savedReflection;
        ReflectionFeedback = string.Empty;
        OnPropertyChanged(nameof(WorkedWell));
        OnPropertyChanged(nameof(Adjustments));
        OnPropertyChanged(nameof(HasReflectionDraft));
    }

    private void SetReflection((string WorkedWell, string Adjustments) draft)
    {
        if (!HasReflectionEditor || draft == _reflection) return;
        var previous = _reflection;
        _reflection = draft;
        ReflectionFeedback = HasReflectionDraft ? "Alterações na reflexão ainda não salvas." : string.Empty;
        if (previous.WorkedWell != draft.WorkedWell) OnPropertyChanged(nameof(WorkedWell));
        if (previous.Adjustments != draft.Adjustments) OnPropertyChanged(nameof(Adjustments));
        OnPropertyChanged(nameof(HasReflectionDraft));
    }
}
