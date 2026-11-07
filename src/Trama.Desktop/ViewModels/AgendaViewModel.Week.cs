using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaViewModel
{
    private readonly IAgendaPeriodReader? _periodReader;
    private readonly IAgendaWeekProjector? _weekProjector;
    private IReadOnlyList<AgendaTaskSnapshot> _weekTasks = Array.Empty<AgendaTaskSnapshot>();
    private AgendaWeekSnapshot? _week;
    private bool _isWeekView;
    private bool _hasWeekError;
    private AgendaTaskCategory? _weekCategoryFilter;
    private AgendaTaskStatus? _weekStatusFilter;

    public AgendaViewModel(IAgendaActions actions, IUnsavedChangesPrompt prompt, IAgendaTrashActions? trash,
        TimeProvider? clock, IAgendaPeriodReader periodReader, IAgendaWeekProjector weekProjector)
        : this(actions, prompt, trash, clock)
    {
        ArgumentNullException.ThrowIfNull(periodReader);
        ArgumentNullException.ThrowIfNull(weekProjector);
        _periodReader = periodReader;
        _weekProjector = weekProjector;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Week) or nameof(IsWeekView) or nameof(HasWeekError) or nameof(CanEdit))
            {
                OnPropertyChanged(nameof(WeekHeading)); OnPropertyChanged(nameof(WeekCountLabel));
                OnPropertyChanged(nameof(IsWeekEmpty)); OnPropertyChanged(nameof(CanPreviousWeek)); OnPropertyChanged(nameof(CanNextWeek));
            }
            if (e.PropertyName == nameof(WeekCategoryFilter)) OnPropertyChanged(nameof(WeekCategoryIndex));
            if (e.PropertyName == nameof(WeekStatusFilter)) OnPropertyChanged(nameof(WeekStatusIndex));
        };
    }

    public bool HasWeekView => _periodReader is not null && _weekProjector is not null;
    public DateOnly Today => DateOnly.FromDateTime(_trashClock.GetLocalNow().DateTime);
    public string WeekHeading => Week is { } week ? $"{week.StartDate:dd/MM/yyyy} – {week.EndDate:dd/MM/yyyy}" : "Sua semana";
    public string WeekCountLabel => Week?.UniqueTaskCount == 1 ? "1 tarefa" : $"{Week?.UniqueTaskCount ?? 0} tarefas";
    public bool IsWeekEmpty => Week is { UniqueTaskCount: 0 } && !HasWeekError && !IsBusy;
    public bool CanPreviousWeek => CanEdit && Week is { } week && week.StartDate.DayNumber >= 7;
    public bool CanNextWeek => CanEdit && Week is { } week && week.EndDate.DayNumber <= DateOnly.MaxValue.DayNumber - 7;
    public IReadOnlyList<string> WeekCategoryChoices { get; } = ["Todas as categorias", "Pessoal", "Trabalho", "Estudo"];
    public IReadOnlyList<string> WeekStatusChoices { get; } = ["Todos os estados", "Pendente", "Em andamento", "Concluída"];
    public int WeekCategoryIndex => WeekCategoryFilter is { } category ? (int)category + 1 : 0;
    public int WeekStatusIndex => WeekStatusFilter is { } status ? (int)status + 1 : 0;
    public bool IsWeekView { get => _isWeekView; private set => SetProperty(ref _isWeekView, value); }
    public AgendaWeekSnapshot? Week { get => _week; private set => SetProperty(ref _week, value); }
    public bool HasWeekError { get => _hasWeekError; private set => SetProperty(ref _hasWeekError, value); }
    public AgendaTaskCategory? WeekCategoryFilter { get => _weekCategoryFilter; private set => SetProperty(ref _weekCategoryFilter, value); }
    public AgendaTaskStatus? WeekStatusFilter { get => _weekStatusFilter; private set => SetProperty(ref _weekStatusFilter, value); }

    public async Task<bool> ShowWeekAsync(DateOnly referenceDate)
    {
        if (!HasWeekView || IsBusy) return false;
        IsBusy = true;
        try
        {
            await LoadWeekCoreAsync(referenceDate, CancellationToken.None);
            IsWeekView = true; Feedback = string.Empty; return true;
        }
        catch (Exception)
        {
            HasWeekError = true;
            Feedback = "Não foi possível consultar a semana. A seleção e o rascunho foram mantidos; tente novamente.";
            return false;
        }
        finally { IsBusy = false; }
    }

    public Task<bool> ShowDayAsync()
    {
        if (IsBusy) return Task.FromResult(false);
        IsWeekView = false;
        return Task.FromResult(true);
    }

    public Task<bool> SetWeekFiltersAsync(AgendaTaskCategory? category, AgendaTaskStatus? status)
    {
        if (IsBusy || Week is not { } current || _weekProjector is null) return Task.FromResult(false);
        IsBusy = true;
        try
        {
            var projected = _weekProjector.Build(current.StartDate, _weekTasks, category, status);
            WeekCategoryFilter = category; WeekStatusFilter = status; Week = projected;
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            Feedback = "Não foi possível aplicar os filtros. A semana e o rascunho foram mantidos.";
            return Task.FromResult(false);
        }
        finally { IsBusy = false; }
    }

    public Task<bool> CreateTaskOnDayAsync(DateOnly day)
    {
        if (IsBusy || !IsWeekView || Week is not { } week || day < week.StartDate || day > week.EndDate)
            return Task.FromResult(false);
        return ChangeDayAsync(day);
    }

    private async Task LoadWeekCoreAsync(DateOnly referenceDate, CancellationToken cancellationToken)
    {
        var projector = _weekProjector ?? throw new InvalidOperationException("Weekly projection is unavailable.");
        var reader = _periodReader ?? throw new InvalidOperationException("Weekly reading is unavailable.");
        // Validate the calendar range before I/O; publish only a completely projected result.
        var range = projector.Build(referenceDate, Array.Empty<AgendaTaskSnapshot>());
        var tasks = (await reader.ListOverlappingAsync(range.StartDate, range.EndDate, cancellationToken)).ToArray();
        var projected = projector.Build(referenceDate, tasks, WeekCategoryFilter, WeekStatusFilter);
        _weekTasks = tasks; Week = projected; HasWeekError = false;
    }

    private async Task RefreshActiveWeekAsync(CancellationToken cancellationToken = default)
    {
        if (!IsWeekView || Week is not { } week) return;
        try { await LoadWeekCoreAsync(week.StartDate, cancellationToken); }
        catch (Exception)
        {
            HasWeekError = true;
            Feedback += " Não foi possível atualizar a semana; use Atualizar para consultar novamente.";
        }
    }
}
