using System.Globalization;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    private int _homeAgendaRequest;
    private HomeAgendaSummary _homeAgenda = HomeAgendaSummary.Empty;
    private bool _isHomeAgendaLoading;
    private bool _hasHomeAgendaError;

    public string HomeDateLabel => (Agenda?.Today ?? DateOnly.FromDateTime(DateTime.Today))
        .ToString("dddd, dd 'de' MMMM", CultureInfo.GetCultureInfo("pt-BR"));
    public bool HasHomeWeekSummary => HasAgenda && !IsHomeAgendaLoading && !HasHomeAgendaError;
    public string HomeWeekCompletion => $"{_homeAgenda.WeekCompleted} de {_homeAgenda.WeekPlanned}";
    public string HomeWeekPlannedHours => FormatHours(_homeAgenda.PlannedMinutes);
    public string HomeWeekActualHours => _homeAgenda.ActualTime is { } duration
        ? duration.TotalMinutes < 1 ? "0 min" : FormatHours((long)duration.TotalMinutes)
        : "Sem registro";

    private static string FormatHours(long duration) => duration switch
    {
        var minutes when minutes % 60 == 0 => $"{minutes / 60} h",
        var minutes when minutes < 60 => $"{minutes} min",
        var minutes => $"{minutes / 60} h {minutes % 60} min"
    };
    public IReadOnlySet<Guid> HomeTodayTasksWithPageLinks => _homeAgenda.TodayWithPageLinks;
    public string HomeTodayCount => HomeTodayTasks.Count == 1 ? "1 tarefa para hoje" : $"{HomeTodayTasks.Count} tarefas para hoje";

    public IReadOnlyList<AgendaTaskSnapshot> HomeTodayTasks => _homeAgenda.Today;
    public IReadOnlyList<AgendaTaskSnapshot> HomeUpcomingTasks => _homeAgenda.Upcoming;
    public bool HasHomeTodayTasks => HomeTodayTasks.Count > 0;
    public bool HasHomeUpcomingTasks => HomeUpcomingTasks.Count > 0;
    public bool IsHomeAgendaEmpty => HasAgenda && !IsHomeAgendaLoading && !HasHomeAgendaError &&
        !HasHomeTodayTasks && !HasHomeUpcomingTasks;
    public bool IsHomeAgendaLoading => _isHomeAgendaLoading;
    public bool HasHomeAgendaError => _hasHomeAgendaError;

    private async Task RefreshHomeAgendaAsync()
    {
        var request = ++_homeAgendaRequest;
        _homeAgenda = HomeAgendaSummary.Empty;
        _hasHomeAgendaError = false;
        var agenda = Agenda;
        _isHomeAgendaLoading = IsHomeActive && agenda is not null;
        NotifyHomeAgenda();
        if (!_isHomeAgendaLoading || agenda is null) return;

        try
        {
            var today = agenda.Today;
            var tasks = await agenda.ReadHomeTasksAsync(today);
            var todayTasks = tasks.Where(task => task.PlannedDate == today).ToArray();
            var linkedTasks = await agenda.ReadTasksWithLinksAsync(todayTasks, CancellationToken.None);
            if (request != _homeAgendaRequest) return;

            var weekStart = AgendaRetrospectiveViewModel.WeekStart(today);
            var actualTime = await agenda.ReadHomeActualHoursAsync(weekStart);
            if (request != _homeAgendaRequest) return;
            var weekTasks = tasks.Where(task => task.PlannedDate >= weekStart && task.PlannedDate.DayNumber - weekStart.DayNumber < 7).ToArray();
            _homeAgenda = new(
                todayTasks,
                tasks.Where(task => task.PlannedDate > today && task.Status != AgendaTaskStatus.Completed).ToArray(),
                weekTasks.Length, weekTasks.Count(task => task.Status == AgendaTaskStatus.Completed),
                (long)tasks.Sum(task => PlannedMinutesInWeek(task, weekStart)), linkedTasks, actualTime);
        }
        catch (Exception)
        {
            if (request == _homeAgendaRequest) _hasHomeAgendaError = true;
        }
        finally
        {
            if (request == _homeAgendaRequest)
            {
                _isHomeAgendaLoading = false;
                NotifyHomeAgenda();
            }
        }
    }

    private static double PlannedMinutesInWeek(AgendaTaskSnapshot task, DateOnly weekStart)
    {
        if (task.StartTime is not { } start || task.EndTime is not { } end) return 0;

        const int minutesPerDay = 24 * 60;
        var startMinute = (task.PlannedDate.DayNumber - weekStart.DayNumber) * (double)minutesPerDay + start.ToTimeSpan().TotalMinutes;
        var endMinute = ((task.PlannedEndDate ?? task.PlannedDate).DayNumber - weekStart.DayNumber) * (double)minutesPerDay + end.ToTimeSpan().TotalMinutes;
        return Math.Max(0, Math.Min(endMinute, 7 * minutesPerDay) - Math.Max(startMinute, 0));
    }

    private void NotifyHomeAgenda()
    {
        OnPropertyChanged(nameof(HomeDateLabel));
        OnPropertyChanged(nameof(HasHomeWeekSummary));
        OnPropertyChanged(nameof(HomeWeekCompletion));
        OnPropertyChanged(nameof(HomeWeekPlannedHours));
        OnPropertyChanged(nameof(HomeWeekActualHours));
        OnPropertyChanged(nameof(HomeTodayTasksWithPageLinks));
        OnPropertyChanged(nameof(HomeTodayCount));
        OnPropertyChanged(nameof(HomeTodayTasks));
        OnPropertyChanged(nameof(HomeUpcomingTasks));
        OnPropertyChanged(nameof(HasHomeTodayTasks));
        OnPropertyChanged(nameof(HasHomeUpcomingTasks));
        OnPropertyChanged(nameof(IsHomeAgendaEmpty));
        OnPropertyChanged(nameof(IsHomeAgendaLoading));
        OnPropertyChanged(nameof(HasHomeAgendaError));
    }

    private sealed record HomeAgendaSummary(IReadOnlyList<AgendaTaskSnapshot> Today, IReadOnlyList<AgendaTaskSnapshot> Upcoming, int WeekPlanned, int WeekCompleted,
        long PlannedMinutes, IReadOnlySet<Guid> TodayWithPageLinks, TimeSpan? ActualTime)
    {
        public static HomeAgendaSummary Empty { get; } = new([], [], 0, 0, 0, new HashSet<Guid>(), null);
    }
}
