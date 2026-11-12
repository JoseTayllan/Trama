using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaViewModel
{
    private bool _isRetrospectiveView;
    private DateOnly? _unrecordedWeek;
    private readonly IUnsavedChangesPrompt? _reflectionPrompt;

    public AgendaViewModel(IAgendaActions actions, IUnsavedChangesPrompt prompt, IAgendaTrashActions? trash,
        TimeProvider? clock, IAgendaPeriodReader periodReader, IAgendaWeekProjector weekProjector,
        AgendaRetrospectiveViewModel retrospective)
        : this(actions, prompt, trash, clock, periodReader, weekProjector, retrospective, null)
    {
    }

    public AgendaViewModel(IAgendaActions actions, IUnsavedChangesPrompt prompt, IAgendaTrashActions? trash,
        TimeProvider? clock, IAgendaPeriodReader periodReader, IAgendaWeekProjector weekProjector,
        AgendaRetrospectiveViewModel retrospective, IUnsavedChangesPrompt? reflectionPrompt)
        : this(actions, prompt, trash, clock, periodReader, weekProjector)
    {
        ArgumentNullException.ThrowIfNull(retrospective);
        Retrospective = retrospective;
        _reflectionPrompt = reflectionPrompt ?? prompt;
        retrospective.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AgendaRetrospectiveViewModel.Report)) NotifyRetrospectivePresentation();
            if (e.PropertyName == nameof(AgendaRetrospectiveViewModel.HasReflectionDraft))
                OnPropertyChanged(nameof(CanSaveReflection));
        };
    }

    public AgendaRetrospectiveViewModel? Retrospective { get; }
    public bool CanEditReflection => CanEdit && Retrospective?.HasReflectionEditor == true;
    public bool CanSaveReflection => CanEditReflection && Retrospective?.HasReflectionDraft == true;
    public bool HasRetrospectiveView => Retrospective is not null;
    public bool IsRetrospectiveView
    {
        get => _isRetrospectiveView;
        private set
        {
            if (SetProperty(ref _isRetrospectiveView, value)) OnPropertyChanged(nameof(ShowClosedWeekNotice));
        }
    }

    public bool ShowClosedWeekNotice => _unrecordedWeek.HasValue && !IsRetrospectiveView;
    public string ClosedWeekNotice => _unrecordedWeek is { } week
        ? FormattableString.Invariant($"Semana encerrada · {week:dd/MM/yyyy} a {week.AddDays(6):dd/MM/yyyy}")
        : string.Empty;
    public bool IsDayView => !IsWeekView && !IsRetrospectiveView;
    public bool ShowTaskDelete => HasTrash && !IsRetrospectiveView;
    public bool CanPreviousRetrospective => CanEdit && Retrospective?.Report is { StartDate.DayNumber: >= 7 };
    public bool CanNextRetrospective => CanEdit && Retrospective?.Report is { } report && report.StartDate.DayNumber + 13 < Today.DayNumber;
    public string RetrospectiveHeading => Retrospective?.Report is { } report ? $"{report.StartDate:dd/MM/yyyy} – {report.EndDate:dd/MM/yyyy}" : "Fechamento semanal";
    public string RetrospectiveCapturedAt => Retrospective?.Report is { } report ? $"Registrado em {report.ClosedAt:dd/MM/yyyy 'às' HH:mm} · fechamento preservado" : string.Empty;
    public double RetrospectiveChartMaximum => Retrospective?.Report is { } report ? Math.Max(1, report.Days.Max(day => Math.Max(day.PlannedCount, day.CompletedCount))) : 1;
    public bool IsRetrospectiveEmpty => Retrospective?.Report is { PlannedCount: 0 };

    private void NotifyRetrospectivePresentation()
    {
        OnPropertyChanged(nameof(CanEditReflection));
        OnPropertyChanged(nameof(CanSaveReflection));
        OnPropertyChanged(nameof(CanPreviousRetrospective)); OnPropertyChanged(nameof(CanNextRetrospective));
        OnPropertyChanged(nameof(RetrospectiveHeading)); OnPropertyChanged(nameof(RetrospectiveCapturedAt));
        OnPropertyChanged(nameof(RetrospectiveChartMaximum)); OnPropertyChanged(nameof(IsRetrospectiveEmpty));
    }

    internal async Task<bool> OpenRetrospectiveTaskAsync(Guid taskId)
    {
        if (!IsRetrospectiveView || IsBusy) return false;
        IsBusy = true;
        try
        {
            var task = await _actions.GetByIdAsync(taskId);
            if (task is null)
            {
                Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido.";
                return false;
            }

            var keepDraft = EditingTaskId == taskId && HasUnsavedChanges;
            if (!keepDraft && HasUnsavedChanges)
            {
                if (!await ResolveChangesAsync()) return false;
                task = await _actions.GetByIdAsync(taskId);
                if (task is null)
                {
                    Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido.";
                    return false;
                }
            }

            var day = task.PlannedDate;
            var tasks = await ReadDayTasksAsync(day);
            var current = tasks.FirstOrDefault(item => item.Id == taskId);
            if (current is null)
            {
                Feedback = "A tarefa não está mais disponível neste dia. Seu rascunho foi mantido.";
                return false;
            }

            var links = keepDraft ? null : await ReadPageLinksAsync(current.Id);
            SelectDay(day);
            Tasks = tasks;
            HasListError = false;
            if (!keepDraft) LoadDraft(current, links);
            IsWeekView = false;
            IsRetrospectiveView = false;
            Feedback = "Tarefa aberta para edição.";
            return true;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível abrir a tarefa. Seu rascunho foi mantido.";
            return false;
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> SaveReflectionAsync(CancellationToken cancellationToken = default)
    {
        if (!CanSaveReflection || Retrospective is not { } retrospective) return false;
        IsBusy = true;
        try { return await retrospective.SaveReflectionAsync(cancellationToken); }
        finally { IsBusy = false; }
    }

    private async Task<bool> ResolveReflectionDraftAsync(CancellationToken cancellationToken)
    {
        if (Retrospective is not { HasReflectionDraft: true } retrospective || _reflectionPrompt is null) return true;
        var choice = await _reflectionPrompt.ChooseAsync();
        return choice switch
        {
            UnsavedChangesChoice.Discard => true,
            UnsavedChangesChoice.Save => await retrospective.SaveReflectionAsync(cancellationToken) && !retrospective.HasReflectionDraft,
            _ => false
        };
    }

    internal Task<bool> OpenClosedWeekNoticeAsync() => ShowClosedWeekNotice && _unrecordedWeek is { } week
        ? ShowRetrospectiveAsync(week)
        : Task.FromResult(false);

    private async Task RefreshClosedWeekNoticeAsync()
    {
        if (Retrospective is not { } retrospective) return;

        try { SetUnrecordedWeek(await retrospective.FindUnrecordedWeekAsync(Today)); }
        catch (Exception) { SetUnrecordedWeek(null); }
    }

    private void SetUnrecordedWeek(DateOnly? week)
    {
        if (_unrecordedWeek == week) return;
        _unrecordedWeek = week;
        OnPropertyChanged(nameof(ShowClosedWeekNotice));
        OnPropertyChanged(nameof(ClosedWeekNotice));
    }

    public Task<bool> ShowRetrospectiveAsync(DateOnly referenceDate, CancellationToken cancellationToken = default) =>
        LoadRetrospectiveAsync(referenceDate, cancellationToken);

    public Task<bool> RefreshRetrospectiveAsync(CancellationToken cancellationToken = default) =>
        LoadRetrospectiveAsync(null, cancellationToken);

    private async Task<bool> LoadRetrospectiveAsync(DateOnly? referenceDate, CancellationToken cancellationToken)
    {
        if (Retrospective is not { } retrospective || IsBusy) return false;
        IsBusy = true;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (referenceDate is { } target && retrospective.Report is { } report &&
                AgendaRetrospectiveViewModel.WeekStart(target) != report.StartDate &&
                !await ResolveReflectionDraftAsync(cancellationToken))
                return false;

            var loaded = referenceDate is { } date
                ? await retrospective.OpenAsync(date, cancellationToken)
                : await retrospective.RefreshAsync(cancellationToken);
            Feedback = retrospective.Feedback;
            if (!loaded) return false;
            if (retrospective.Report?.StartDate == _unrecordedWeek) SetUnrecordedWeek(null);
            IsWeekView = false;
            IsRetrospectiveView = true;
            return true;
        }
        catch (OperationCanceledException)
        {
            Feedback = "Consulta cancelada. Seu rascunho foi mantido.";
            return false;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível trocar de semana. Seu rascunho foi mantido.";
            return false;
        }
        finally { IsBusy = false; }
    }
}
