using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaViewModel : ObservableObject
{
    private readonly IAgendaActions _actions;
    private readonly IUnsavedChangesPrompt _prompt;
    private Guid _draftId = Guid.NewGuid();
    private Guid? _editingTaskId;
    private Draft _baseline;
    private bool _loadingDraft;
    private bool _initialized;
    private bool _isBusy;
    private DateOnly _selectedDay = DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<AgendaTaskSnapshot> _tasks = Array.Empty<AgendaTaskSnapshot>();
    private string _feedback = string.Empty;
    private bool _hasListError;
    private bool _syncingTimeText;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTimeOffset? _plannedDate = new DateTimeOffset(DateTime.Today);
    [ObservableProperty] private DateTimeOffset? _plannedEndDate;
    [ObservableProperty] private TimeSpan? _startTime;
    [ObservableProperty] private TimeSpan? _endTime;
    [ObservableProperty] private string _startTimeText = string.Empty;
    [ObservableProperty] private string _endTimeText = string.Empty;
    [ObservableProperty] private AgendaTaskCategory _category;
    [ObservableProperty] private AgendaTaskStatus _status;

    public AgendaViewModel(IAgendaActions actions, IUnsavedChangesPrompt prompt)
        : this(actions, prompt, null, null)
    {
    }

    public AgendaViewModel(IAgendaActions actions, IUnsavedChangesPrompt prompt, IAgendaTrashActions? trash, TimeProvider? clock)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(prompt);
        _actions = actions; _prompt = prompt; _baseline = Capture();
        _trash = trash; _trashClock = clock ?? TimeProvider.System;
        NewTaskCommand = new AsyncRelayCommand(async () => { await NewTaskAsync(); }, () => CanEdit);
        SaveCommand = new AsyncRelayCommand(SaveAndPrepareNextAsync, () => CanEdit);
        EditTaskCommand = new AsyncRelayCommand<Guid>(async id => { await EditTaskAsync(id); }, _ => CanEdit);
        ReloadCommand = new AsyncRelayCommand(RefreshAsync, () => CanEdit);
    }

    public IAsyncRelayCommand NewTaskCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand<Guid> EditTaskCommand { get; }
    public IAsyncRelayCommand ReloadCommand { get; }
    public IReadOnlyList<string> CategoryChoices { get; } = ["Pessoal", "Trabalho", "Estudo"];
    public IReadOnlyList<string> StatusChoices { get; } = ["Pendente", "Em andamento", "Concluída"];
    public int CategoryIndex { get => (int)Category; set { if (Enum.IsDefined((AgendaTaskCategory)value)) Category = (AgendaTaskCategory)value; } }
    public int StatusIndex { get => (int)Status; set { if (Enum.IsDefined((AgendaTaskStatus)value)) Status = (AgendaTaskStatus)value; } }
    public DateOnly SelectedDay => _selectedDay;
    public DateTimeOffset SelectedDayDate => CalendarDate(SelectedDay);
    public string DayLabel => SelectedDay.ToString("dddd, dd 'de' MMMM", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    public Guid? EditingTaskId => _editingTaskId;
    public string FormHeading => EditingTaskId is null ? "Nova tarefa" : "Editar tarefa";
    public IReadOnlyList<AgendaTaskSnapshot> Tasks { get => _tasks; private set { if (SetProperty(ref _tasks, value)) OnPropertyChanged(nameof(IsEmpty)); } }
    public bool IsEmpty => !HasListError && Tasks.Count == 0;
    public bool HasListError { get => _hasListError; private set { if (SetProperty(ref _hasListError, value)) OnPropertyChanged(nameof(IsEmpty)); } }
    public bool HasUnsavedChanges => _baseline != Capture();
    public bool CanEdit => !IsBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanEdit));
            NewTaskCommand.NotifyCanExecuteChanged(); SaveCommand.NotifyCanExecuteChanged();
            EditTaskCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
        }
    }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(CanEdit) or nameof(EditingTaskId)) OnPropertyChanged(nameof(CanDeleteTask));
        if (e.PropertyName == nameof(Category)) OnPropertyChanged(nameof(CategoryIndex));
        if (e.PropertyName == nameof(Status)) OnPropertyChanged(nameof(StatusIndex));
        if (!_loadingDraft && e.PropertyName is nameof(Title) or nameof(Description) or nameof(PlannedDate) or nameof(PlannedEndDate) or nameof(StartTime) or nameof(EndTime) or nameof(StartTimeText) or nameof(EndTimeText) or nameof(Category) or nameof(Status))
            OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    partial void OnStartTimeChanged(TimeSpan? value) => SyncTimeText(value, true);
    partial void OnEndTimeChanged(TimeSpan? value) => SyncTimeText(value, false);
    partial void OnStartTimeTextChanged(string value) => SyncTypedTime(value, true);
    partial void OnEndTimeTextChanged(string value) => SyncTypedTime(value, false);

    private void SyncTimeText(TimeSpan? time, bool start)
    {
        if (_syncingTimeText) return;
        _syncingTimeText = true;
        try { if (start) StartTimeText = AgendaTimeInput.Format(time); else EndTimeText = AgendaTimeInput.Format(time); }
        finally { _syncingTimeText = false; }
    }

    private void SyncTypedTime(string text, bool start)
    {
        if (_syncingTimeText || !AgendaTimeInput.TryParse(text, out var time)) return;
        _syncingTimeText = true;
        try { if (start) StartTime = time; else EndTime = time; }
        finally { _syncingTimeText = false; }
    }

    public async Task InitializeAsync(DateOnly day)
    {
        if (IsBusy) return;
        if (!_initialized) { SelectDay(day); LoadDraft(null); _initialized = true; }
        await RefreshAsync();
    }

    public async Task<bool> NewTaskAsync()
    {
        if (IsBusy) return false;
        IsBusy = true;
        try
        {
            if (!await ResolveChangesAsync()) return false;
            LoadDraft(null); Feedback = HasListError ? "Nova tarefa pronta; use Atualizar para conferir a lista." : "Nova tarefa pronta para editar."; return true;
        }
        catch (Exception) { Feedback = "Não foi possível preparar outra tarefa. Seu rascunho foi mantido."; return false; }
        finally { IsBusy = false; }
    }

    public async Task<bool> EditTaskAsync(Guid id)
    {
        if (IsBusy) return false;
        if (id == EditingTaskId) return true;
        IsBusy = true;
        try
        {
            var task = await _actions.GetByIdAsync(id);
            if (task is null) { Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido."; return false; }
            if (!await ResolveChangesAsync()) return false;
            task = await _actions.GetByIdAsync(id);
            if (task is null) { Feedback = "A tarefa não está mais disponível. Seu rascunho foi mantido."; return false; }
            LoadDraft(task); Feedback = "Tarefa aberta para edição."; return true;
        }
        catch (Exception) { Feedback = "Não foi possível abrir a tarefa. Seu rascunho foi mantido."; return false; }
        finally { IsBusy = false; }
    }

    public async Task<bool> ChangeDayAsync(DateOnly day)
    {
        if (IsBusy) return false;
        IsBusy = true;
        try
        {
            if (!await ResolveChangesAsync()) return false;
            var tasks = await _actions.ListAsync(day, day);
            SelectDay(day); Tasks = tasks; HasListError = false; LoadDraft(null); Feedback = string.Empty; return true;
        }
        catch (Exception) { HasListError = true; Feedback = "Não foi possível consultar o dia. Sua seleção e seu rascunho foram mantidos."; return false; }
        finally { IsBusy = false; }
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return false;
        IsBusy = true;
        try { return await SaveCoreAsync(cancellationToken); }
        catch (OperationCanceledException) { Feedback = "Gravação cancelada. Seu rascunho foi mantido."; return false; }
        catch (Exception) { Feedback = "Não foi possível salvar. Seu rascunho foi mantido."; return false; }
        finally { IsBusy = false; }
    }

    private async Task SaveAndPrepareNextAsync()
    {
        if (await SaveAsync() && !HasUnsavedChanges && !HasListError)
            LoadDraft(null);
    }

    public async Task<bool> RequestCloseAsync()
    {
        if (IsBusy) return false;
        IsBusy = true;
        try { return await ResolveChangesAsync(); }
        catch (Exception) { Feedback = "Não foi possível concluir o fechamento. Seu rascunho foi mantido."; return false; }
        finally { IsBusy = false; }
    }

    private async Task<bool> ResolveChangesAsync()
    {
        if (!HasUnsavedChanges) return true;
        return (await _prompt.ChooseAsync()) switch
        {
            UnsavedChangesChoice.Discard => true,
            UnsavedChangesChoice.Save => await SaveCoreAsync(CancellationToken.None) && !HasUnsavedChanges,
            _ => false
        };
    }

    private async Task<bool> SaveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var draft = Capture();
        if (draft.Date is null || !ValidTime(draft.Start) || !ValidTime(draft.End) ||
            !AgendaTimeInput.TryParse(draft.StartText, out _) || !AgendaTimeInput.TryParse(draft.EndText, out _))
        {
            Feedback = "Confira a data e digite horários válidos em HH:mm, de 00:00 a 23:59."; return false;
        }
        var snapshot = new AgendaTaskSnapshot(_draftId, draft.Title, draft.Description, DateOnly.FromDateTime(draft.Date.Value.DateTime),
            draft.Start is { } start ? TimeOnly.FromTimeSpan(start) : null,
            draft.End is { } end ? TimeOnly.FromTimeSpan(end) : null, draft.Category, draft.Status)
        { PlannedEndDate = draft.EndDate is { } endDate ? DateOnly.FromDateTime(endDate.DateTime) : null };
        var result = await _actions.SaveAsync(snapshot, cancellationToken);
        if (result.Status != AgendaTaskSaveStatus.Saved)
        {
            Feedback = result.Status == AgendaTaskSaveStatus.ValidationFailed
                ? "Informe título, data e intervalo válidos para salvar."
                : "Não foi possível salvar. Seu rascunho foi mantido.";
            return false;
        }
        _editingTaskId = snapshot.Id; _baseline = draft;
        OnPropertyChanged(nameof(EditingTaskId)); OnPropertyChanged(nameof(FormHeading)); OnPropertyChanged(nameof(HasUnsavedChanges));
        Feedback = HasUnsavedChanges ? "Versão salva; há alterações mais recentes." : "Tarefa salva.";
        try { Tasks = await _actions.ListAsync(SelectedDay, SelectedDay, cancellationToken); HasListError = false; }
        catch (Exception) { HasListError = true; Feedback = "Tarefa salva; não foi possível atualizar a lista. Use Atualizar para consultar."; }
        await RefreshActiveWeekAsync(cancellationToken);
        return true;
    }

    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            try { Tasks = await _actions.ListAsync(SelectedDay, SelectedDay); HasListError = false; Feedback = string.Empty; }
            catch (Exception) { HasListError = true; Feedback = "Não foi possível consultar as tarefas. Use Atualizar para tentar novamente."; }
            await RefreshActiveWeekAsync();
        }
        finally { IsBusy = false; }
    }

    private void SelectDay(DateOnly day)
    {
        _selectedDay = day;
        OnPropertyChanged(nameof(SelectedDay)); OnPropertyChanged(nameof(SelectedDayDate)); OnPropertyChanged(nameof(DayLabel));
    }

    private void LoadDraft(AgendaTaskSnapshot? task)
    {
        _loadingDraft = true;
        try
        {
            _draftId = task?.Id ?? Guid.NewGuid(); _editingTaskId = task?.Id;
            Title = task?.Title ?? string.Empty; Description = task?.Description ?? string.Empty;
            PlannedDate = CalendarDate(task?.PlannedDate ?? SelectedDay);
            PlannedEndDate = task?.PlannedEndDate is { } endDate ? CalendarDate(endDate) : null;
            StartTime = task?.StartTime?.ToTimeSpan(); EndTime = task?.EndTime?.ToTimeSpan();
            StartTimeText = AgendaTimeInput.Format(StartTime); EndTimeText = AgendaTimeInput.Format(EndTime);
            Category = task?.Category ?? AgendaTaskCategory.Personal; Status = task?.Status ?? AgendaTaskStatus.Pending;
        }
        finally { _loadingDraft = false; }
        _baseline = Capture();
        OnPropertyChanged(nameof(EditingTaskId)); OnPropertyChanged(nameof(FormHeading)); OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    private Draft Capture() => new(Title ?? string.Empty, Description ?? string.Empty, PlannedDate, PlannedEndDate,
        StartTime, EndTime, StartTimeText ?? string.Empty, EndTimeText ?? string.Empty, Category, Status);
    private static bool ValidTime(TimeSpan? time) => time is null || (time >= TimeSpan.Zero && time < TimeSpan.FromDays(1));
    private static DateTimeOffset CalendarDate(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue));
    private sealed record Draft(string Title, string Description, DateTimeOffset? Date, DateTimeOffset? EndDate,
        TimeSpan? Start, TimeSpan? End, string StartText, string EndText,
        AgendaTaskCategory Category, AgendaTaskStatus Status);
}
