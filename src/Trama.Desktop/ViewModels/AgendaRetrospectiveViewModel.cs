using CommunityToolkit.Mvvm.ComponentModel;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaRetrospectiveViewModel : ObservableObject
{
    private readonly IAgendaRetrospectiveActions _actions;
    private readonly IAgendaRetrospectiveComparer _comparer;
    private readonly TimeProvider _clock;
    private readonly IAgendaTaskReader? _taskReader;
    private LoadedWeek? _loaded;
    private int _loading;
    private bool _hasError;
    private string _feedback = string.Empty;

    public AgendaRetrospectiveViewModel(IAgendaRetrospectiveActions actions, IAgendaRetrospectiveComparer comparer, TimeProvider clock)
        : this(actions, comparer, clock, null)
    {
    }

    public AgendaRetrospectiveViewModel(IAgendaRetrospectiveActions actions, IAgendaRetrospectiveComparer comparer, TimeProvider clock,
        IAgendaReflectionActions? reflectionActions)
        : this(actions, comparer, clock, reflectionActions, null)
    {
    }

    internal AgendaRetrospectiveViewModel(IAgendaRetrospectiveActions actions, IAgendaRetrospectiveComparer comparer, TimeProvider clock,
        IAgendaReflectionActions? reflectionActions, IAgendaTaskReader? taskReader)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(clock);
        _actions = actions;
        _comparer = comparer;
        _clock = clock;
        _reflectionActions = reflectionActions;
        _taskReader = taskReader;
    }

    public AgendaWeekRetrospective? Report => _loaded?.Report;
    public AgendaRetrospectiveSummaryViewModel? Summary => _loaded?.Summary;
    public IReadOnlyDictionary<Guid, AgendaTaskStatus?>? CurrentPendingStatuses => _loaded?.CurrentPendingStatuses;
    public bool IsBusy => Volatile.Read(ref _loading) != 0;
    public bool HasError { get => _hasError; private set => SetProperty(ref _hasError, value); }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }

    public async Task<bool> OpenAsync(DateOnly referenceDate, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _loading, 1, 0) != 0) return false;
        try
        {
            OnPropertyChanged(nameof(IsBusy));
            HasError = false;
            cancellationToken.ThrowIfCancellationRequested();
            var start = WeekStart(referenceDate);
            var today = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
            if (start.DayNumber + 6 >= today.DayNumber)
            {
                Feedback = "Escolha uma semana já encerrada para abrir a retrospectiva.";
                return false;
            }

            Feedback = "Carregando o fechamento da semana…";
            var report = await _actions.GetOrCloseAsync(start, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var previous = start.DayNumber >= 7
                ? await _actions.GetAsync(start.AddDays(-7), cancellationToken) : null;
            cancellationToken.ThrowIfCancellationRequested();
            var comparison = _comparer.Compare(report, previous);
            var summary = new AgendaRetrospectiveSummaryViewModel(report, comparison);
            var reflection = _reflectionActions is null ? null : await _reflectionActions.GetAsync(start, cancellationToken);
            var currentStatuses = await ReadPendingStatusesAsync(summary.OpenTasks, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var keepDraft = _loaded?.StartDate == start && HasReflectionDraft;
            _loaded = new(start, report, summary, currentStatuses);
            if (!keepDraft) LoadReflection(reflection);
            OnPropertyChanged(nameof(Report));
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(CurrentPendingStatuses));
            Feedback = "Fechamento da semana carregado.";
            return true;
        }
        catch (OperationCanceledException)
        {
            Feedback = "Consulta cancelada. O último resultado foi mantido.";
            return false;
        }
        catch (Exception)
        {
            HasError = true;
            Feedback = "Não foi possível carregar o fechamento. O último resultado foi mantido; tente novamente.";
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _loading, 0);
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    public Task<bool> RefreshAsync(CancellationToken cancellationToken = default) => _loaded is { } loaded
        ? OpenAsync(loaded.StartDate, cancellationToken) : Task.FromResult(false);

    private async Task<IReadOnlyDictionary<Guid, AgendaTaskStatus?>?> ReadPendingStatusesAsync(
        IReadOnlyList<AgendaRetrospectiveTask> pendingTasks, CancellationToken cancellationToken)
    {
        if (_taskReader is null) return null;
        var statuses = new Dictionary<Guid, AgendaTaskStatus?>(pendingTasks.Count);
        foreach (var entry in pendingTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await _taskReader.GetByIdAsync(entry.Task.Id, cancellationToken);
            statuses[entry.Task.Id] = current?.Status;
        }

        return new System.Collections.ObjectModel.ReadOnlyDictionary<Guid, AgendaTaskStatus?>(statuses);
    }

    internal async Task<DateOnly?> FindUnrecordedWeekAsync(DateOnly today)
    {
        var currentWeek = WeekStart(today);
        if (currentWeek.DayNumber < 7) return null;

        var previousWeek = currentWeek.AddDays(-7);
        return await _actions.GetAsync(previousWeek) is null ? previousWeek : null;
    }

    internal static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private sealed record LoadedWeek(DateOnly StartDate, AgendaWeekRetrospective Report, AgendaRetrospectiveSummaryViewModel Summary,
        IReadOnlyDictionary<Guid, AgendaTaskStatus?>? CurrentPendingStatuses);
}
