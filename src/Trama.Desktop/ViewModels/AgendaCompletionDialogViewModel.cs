using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public enum AgendaCompletionChoice { Cancel, KeepOpen, CompleteSelected }
public sealed record AgendaCompletionDecision(AgendaCompletionChoice Choice, IReadOnlyList<Guid> SelectedTaskIds)
{
    public DateTimeOffset? CompletedAt { get; init; }
}

public interface IAgendaCompletionPrompt
{
    Task<AgendaCompletionDecision> ChooseAsync(IReadOnlyList<AgendaTaskSnapshot> candidates);
}

public sealed class AgendaCompletionItemViewModel(AgendaTaskSnapshot task) : ObservableObject
{
    private bool _isSelected;
    public AgendaTaskSnapshot Task { get; } = task ?? throw new ArgumentNullException(nameof(task));
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public sealed partial class AgendaCompletionDialogViewModel : ObservableObject
{
    [ObservableProperty] private bool _useCustomCompletionTime;
    [ObservableProperty] private DateTimeOffset? _completionDate = new DateTimeOffset(DateTime.Today);
    [ObservableProperty] private string _completionTimeText = DateTime.Now.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    public AgendaCompletionDialogViewModel(IReadOnlyList<AgendaTaskSnapshot> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        Items = candidates.Select(task => new AgendaCompletionItemViewModel(task)).ToArray();
        foreach (var item in Items)
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(AgendaCompletionItemViewModel.IsSelected)) return;
                OnPropertyChanged(nameof(SelectedCount)); OnPropertyChanged(nameof(CanCompleteSelected)); OnPropertyChanged(nameof(SelectionLabel));
            };
        SelectAllCommand = new RelayCommand(() => { foreach (var item in Items) item.IsSelected = true; });
        ClearSelectionCommand = new RelayCommand(() => { foreach (var item in Items) item.IsSelected = false; });
    }

    public IReadOnlyList<AgendaCompletionItemViewModel> Items { get; }
    public int SelectedCount => Items.Count(item => item.IsSelected);
    public bool CanCompleteSelected => SelectedCount > 0 && (!UseCustomCompletionTime ||
        (CompletionDate is not null && AgendaTimeInput.TryParse(CompletionTimeText, out var time) && time is not null));
    public string CompletionInputHint => UseCustomCompletionTime
        ? "Informe quando terminou. O planejamento será preservado."
        : "O horário será registrado ao confirmar a saída.";

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(UseCustomCompletionTime) or nameof(CompletionDate) or nameof(CompletionTimeText))
        {
            OnPropertyChanged(nameof(CanCompleteSelected)); OnPropertyChanged(nameof(CompletionInputHint));
        }
    }
    public string SelectionLabel => SelectedCount == 0 ? "Nenhuma selecionada" : $"{SelectedCount} selecionada{(SelectedCount == 1 ? string.Empty : "s")} de {Items.Count}";
    public IRelayCommand SelectAllCommand { get; }
    public IRelayCommand ClearSelectionCommand { get; }
    public AgendaCompletionDecision CreateDecision(AgendaCompletionChoice choice)
    {
        if (!Enum.IsDefined(choice)) throw new ArgumentOutOfRangeException(nameof(choice));
        if (choice != AgendaCompletionChoice.CompleteSelected) return new(choice, []);
        var ids = Items.Where(item => item.IsSelected).Select(item => item.Task.Id).ToArray();
        if (ids.Length == 0 || ids.Distinct().Count() != ids.Length)
            throw new InvalidOperationException("Select at least one distinct task to complete.");
        if (!CanCompleteSelected) throw new InvalidOperationException("Enter a valid completion date and time.");
        DateTimeOffset? completedAt = null;
        if (UseCustomCompletionTime)
        {
            AgendaTimeInput.TryParse(CompletionTimeText, out var time);
            var date = CompletionDate!.Value;
            completedAt = new DateTimeOffset(date.Date + time!.Value, date.Offset);
        }
        return new(choice, ids) { CompletedAt = completedAt };
    }
}
