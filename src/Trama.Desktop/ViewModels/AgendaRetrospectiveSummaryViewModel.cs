using System.Globalization;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed class AgendaRetrospectiveSummaryViewModel
{
    private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");

    public AgendaRetrospectiveSummaryViewModel(AgendaWeekRetrospective report, AgendaRetrospectiveComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(comparison);
        OpenCount = report.PendingCount + report.InProgressCount;
        CompletedCount = report.CompletedInWeekCount;
        OpenBreakdownLabel = $"{report.PendingCount} pendentes · {report.InProgressCount} em andamento";
        CompletionLabel = report.CompletionPercentage is { } percentage
            ? percentage.ToString("0.#", Portuguese) + "%" : "Sem tarefas";
        ComparisonLabel = comparison.Status switch
        {
            AgendaComparisonStatus.PreviousMissing => "Sem fechamento da semana anterior",
            AgendaComparisonStatus.NoPercentageBase => "Sem base percentual",
            AgendaComparisonStatus.DifferentTimeZones => "Fusos registrados diferentes",
            AgendaComparisonStatus.Available when comparison.PercentagePointsDelta is { } delta => delta == 0
                ? "Sem alteração" : delta.ToString("+0.#;-0.#", Portuguese) + " p.p.",
            _ => throw new ArgumentException("Comparação sem estado ou percentual válido.", nameof(comparison))
        };
        Days = report.Days;
        Categories = report.Categories;
        OpenTasks = Array.AsReadOnly(report.Tasks.Where(item => item.Task.Status is AgendaTaskStatus.Pending or AgendaTaskStatus.InProgress).ToArray());

        var messages = new List<string>();
        AddMessage(report.CompletedWithoutDateCount, comparison.HasUndatedCompletions,
            "conclusões sem data registrada", "A semana anterior possui conclusões sem data registrada.");
        AddMessage(report.CompletedOutsideWeekCount, comparison.HasOutsidePeriodCompletions,
            "conclusões fora do período", "A semana anterior possui conclusões fora do período.");
        AddMessage(report.CarriedInCount, comparison.HasCarriedTasks,
            "tarefas iniciadas antes da semana", "A semana anterior possui tarefas iniciadas antes daquele período.");
        DataQualityMessages = messages.AsReadOnly();

        void AddMessage(int count, bool hasInEitherWeek, string description, string previousMessage)
        {
            if (count > 0) messages.Add($"{count} {description} neste fechamento.");
            else if (hasInEitherWeek) messages.Add(previousMessage);
        }
    }

    public int OpenCount { get; }
    public int CompletedCount { get; }
    public string OpenBreakdownLabel { get; }
    public string CompletionLabel { get; }
    public string ComparisonLabel { get; }
    public IReadOnlyList<AgendaRetrospectiveTask> OpenTasks { get; }
    public IReadOnlyList<AgendaRetrospectiveDay> Days { get; }
    public IReadOnlyList<AgendaRetrospectiveCategory> Categories { get; }
    public IReadOnlyList<string> DataQualityMessages { get; }
}
