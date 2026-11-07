using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaWeekNavigationTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);

    [Fact]
    public async Task LegacyConstructor_StillProvidesTheDailyAgenda()
    {
        var actions = new Actions(); var task = Example(); actions.Stored[task.Id] = task;
        var vm = new AgendaViewModel(actions, new Prompt()); await vm.InitializeAsync(Monday);
        Assert.Equal(task, Assert.Single(vm.Tasks)); Assert.False(vm.HasUnsavedChanges); Assert.True(vm.CanEdit);
    }

    [Fact]
    public async Task WeeklyDependencies_EnableTheFeatureWithoutChangingInitialDailyMode()
    {
        var f = await Fixture.CreateAsync(); Assert.True(Property<bool>(f.Vm, "HasWeekView"));
        Assert.False(Property<bool>(f.Vm, "IsWeekView")); Assert.Null(Property<AgendaWeekSnapshot?>(f.Vm, "Week"));
        Assert.False(Property<bool>(f.Vm, "HasWeekError")); Assert.Equal(Monday, f.Vm.SelectedDay);
    }

    [Fact]
    public async Task ShowWeek_UsesOverlapReaderAndIncludesPriorStartsWithoutChangingDailyList()
    {
        var task = Example() with { PlannedDate = Monday.AddDays(-1), PlannedEndDate = Monday.AddDays(2) };
        var f = await Fixture.CreateAsync(task); var dailyCalls = f.Actions.DailyCalls;
        Assert.True(await Invoke(f.Vm, "ShowWeekAsync", Monday.AddDays(3)));
        Assert.Equal((Monday, Monday.AddDays(6)), Assert.Single(f.Actions.Periods));
        Assert.Equal(task, Assert.Single(Week(f.Vm).Days[0].Entries).Task); Assert.Empty(f.Vm.Tasks);
        Assert.Equal(dailyCalls, f.Actions.DailyCalls); Assert.Equal(Monday, f.Vm.SelectedDay); Assert.True(Property<bool>(f.Vm, "IsWeekView"));
    }

    [Fact]
    public async Task BrowsingWeeks_PreservesTheDraftAndDoesNotPromptOrSave()
    {
        var f = await Fixture.CreateAsync(); f.Vm.Title = "Rascunho independente"; f.Vm.StartTimeText = "09:";
        Assert.True(await Invoke(f.Vm, "ShowWeekAsync", Monday)); Assert.True(await Invoke(f.Vm, "ShowWeekAsync", Monday.AddDays(7)));
        Assert.Equal(Monday.AddDays(7), Week(f.Vm).StartDate); Assert.Equal(Monday, f.Vm.SelectedDay);
        Assert.Equal("Rascunho independente", f.Vm.Title); Assert.Equal("09:", f.Vm.StartTimeText); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.Equal(0, f.Prompt.Calls); Assert.Empty(f.Actions.Stored);
    }

    [Fact]
    public async Task FailedFirstWeekLoad_KeepsDailyModeAndAllowsRetry()
    {
        var f = await Fixture.CreateAsync(); f.Vm.Title = "Não perder"; f.Actions.FailPeriod = true;
        Assert.False(await Invoke(f.Vm, "ShowWeekAsync", Monday)); Assert.False(Property<bool>(f.Vm, "IsWeekView"));
        Assert.True(Property<bool>(f.Vm, "HasWeekError")); Assert.Null(Property<AgendaWeekSnapshot?>(f.Vm, "Week"));
        Assert.Equal("Não perder", f.Vm.Title); Assert.True(f.Vm.CanEdit); Assert.NotEmpty(f.Vm.Feedback);
        f.Actions.FailPeriod = false; Assert.True(await Invoke(f.Vm, "ShowWeekAsync", Monday)); Assert.False(Property<bool>(f.Vm, "HasWeekError"));
    }

    [Fact]
    public async Task FailedWeekNavigation_RetainsLastSuccessfulWeekAndFilters()
    {
        var f = await Fixture.CreateAsync(Example()); await Invoke(f.Vm, "ShowWeekAsync", Monday);
        await Filter(f.Vm, AgendaTaskCategory.Work, AgendaTaskStatus.Pending); var previous = Week(f.Vm);
        f.Actions.FailPeriod = true; Assert.False(await Invoke(f.Vm, "ShowWeekAsync", Monday.AddDays(7)));
        Assert.Equal(previous, Week(f.Vm)); Assert.True(Property<bool>(f.Vm, "IsWeekView")); Assert.True(Property<bool>(f.Vm, "HasWeekError"));
        Assert.Equal(AgendaTaskCategory.Work, Property<AgendaTaskCategory?>(f.Vm, "WeekCategoryFilter")); Assert.Equal(Monday, f.Vm.SelectedDay);
    }

    [Theory]
    [InlineData(AgendaTaskCategory.Personal)]
    [InlineData(AgendaTaskCategory.Work)]
    [InlineData(AgendaTaskCategory.Study)]
    public async Task CategoryFilters_ReprojectCachedDataWithoutQueryingOrTouchingTheDraft(AgendaTaskCategory category)
    {
        var tasks = Enum.GetValues<AgendaTaskCategory>().Select(c => Example() with { Category = c }).ToArray();
        var f = await Fixture.CreateAsync(tasks); await Invoke(f.Vm, "ShowWeekAsync", Monday); var calls = f.Actions.Periods.Count;
        f.Vm.Title = "Meu rascunho"; Assert.True(await Filter(f.Vm, category, null));
        Assert.Equal(category, Assert.Single(Week(f.Vm).Days[0].Entries).Task.Category); Assert.Equal(1, Week(f.Vm).UniqueTaskCount);
        Assert.Equal(category, Property<AgendaTaskCategory?>(f.Vm, "WeekCategoryFilter")); Assert.Equal(calls, f.Actions.Periods.Count);
        Assert.Equal("Meu rascunho", f.Vm.Title); Assert.Equal(0, f.Prompt.Calls);
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskStatus.Completed)]
    public async Task StateFilters_PreserveCategoryAndCanBeClearedWithoutLosingCachedTasks(AgendaTaskStatus status)
    {
        var tasks = Enum.GetValues<AgendaTaskStatus>().Select(s => Example() with { Status = s }).ToArray();
        var f = await Fixture.CreateAsync(tasks); await Invoke(f.Vm, "ShowWeekAsync", Monday);
        Assert.True(await Filter(f.Vm, AgendaTaskCategory.Work, status)); var task = Assert.Single(Week(f.Vm).Days[0].Entries).Task;
        Assert.Equal(status, task.Status); Assert.Equal(AgendaTaskCategory.Work, task.Category);
        Assert.Equal(status, Property<AgendaTaskStatus?>(f.Vm, "WeekStatusFilter")); Assert.True(await Filter(f.Vm, null, null));
        Assert.Equal(3, Week(f.Vm).UniqueTaskCount); Assert.Single(f.Actions.Periods);
    }

    [Fact]
    public async Task Filters_AreCombinedAndPersistWhenChangingWeeks()
    {
        var wanted = Example() with { PlannedDate = Monday.AddDays(7), Category = AgendaTaskCategory.Study, Status = AgendaTaskStatus.InProgress };
        var f = await Fixture.CreateAsync(wanted, wanted with { Id = Guid.NewGuid(), Category = AgendaTaskCategory.Work }, wanted with { Id = Guid.NewGuid(), Status = AgendaTaskStatus.Completed });
        await Invoke(f.Vm, "ShowWeekAsync", Monday); Assert.True(await Filter(f.Vm, AgendaTaskCategory.Study, AgendaTaskStatus.InProgress));
        Assert.All(Week(f.Vm).Days, day => Assert.Empty(day.Entries)); Assert.True(await Invoke(f.Vm, "ShowWeekAsync", Monday.AddDays(7)));
        Assert.Equal(wanted, Assert.Single(Week(f.Vm).Days[0].Entries).Task); Assert.Equal(1, Week(f.Vm).UniqueTaskCount);
    }

    [Fact]
    public async Task InvalidFilter_DoesNotChangeCurrentWeekOrSelectedFilters()
    {
        var f = await Fixture.CreateAsync(Example()); await Invoke(f.Vm, "ShowWeekAsync", Monday); var previous = Week(f.Vm);
        Assert.False(await Filter(f.Vm, (AgendaTaskCategory)99, null)); Assert.Equal(previous, Week(f.Vm));
        Assert.Null(Property<AgendaTaskCategory?>(f.Vm, "WeekCategoryFilter")); Assert.True(f.Vm.CanEdit);
    }

    [Fact]
    public async Task ReturnToDay_PreservesDraftSelectedDayAndWeekFilters()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); await Filter(f.Vm, AgendaTaskCategory.Study, null);
        f.Vm.Title = "Ainda escrevendo"; var calls = f.Actions.Periods.Count;
        Assert.True(await Invoke(f.Vm, "ShowDayAsync")); Assert.False(Property<bool>(f.Vm, "IsWeekView"));
        Assert.Equal("Ainda escrevendo", f.Vm.Title); Assert.Equal(Monday, f.Vm.SelectedDay); Assert.Equal(0, f.Prompt.Calls);
        Assert.Equal(AgendaTaskCategory.Study, Property<AgendaTaskCategory?>(f.Vm, "WeekCategoryFilter")); Assert.Equal(calls, f.Actions.Periods.Count);
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, false, false)]
    [InlineData(UnsavedChangesChoice.Discard, true, false)]
    [InlineData(UnsavedChangesChoice.Save, true, true)]
    public async Task CreateOnWeekDay_ResolvesDraftThenUsesTheChosenDay(UnsavedChangesChoice choice, bool changed, bool saved)
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Antes de criar"; f.Prompt.Choice = choice;
        Assert.Equal(changed, await Invoke(f.Vm, "CreateTaskOnDayAsync", Monday.AddDays(4))); Assert.Equal(1, f.Prompt.Calls);
        Assert.Equal(changed ? Monday.AddDays(4) : Monday, f.Vm.SelectedDay); Assert.Equal(changed ? string.Empty : "Antes de criar", f.Vm.Title);
        Assert.Equal(saved ? 1 : 0, f.Actions.Stored.Count); Assert.True(Property<bool>(f.Vm, "IsWeekView"));
        if (changed) { Assert.Equal(Monday.AddDays(4), DateOnly.FromDateTime(f.Vm.PlannedDate!.Value.DateTime)); Assert.False(f.Vm.HasUnsavedChanges); }
        if (saved) { Assert.Equal(Monday, Assert.Single(f.Actions.Stored.Values).PlannedDate); Assert.Equal(1, Week(f.Vm).UniqueTaskCount); }
    }

    [Fact]
    public async Task FailedSaveBeforeCreatingOnAnotherDay_KeepsDraftAndSelection()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Manter"; f.Prompt.Choice = UnsavedChangesChoice.Save; f.Actions.FailSave = true;
        Assert.False(await Invoke(f.Vm, "CreateTaskOnDayAsync", Monday.AddDays(1))); Assert.Equal("Manter", f.Vm.Title);
        Assert.Equal(Monday, f.Vm.SelectedDay); Assert.True(f.Vm.HasUnsavedChanges); Assert.Empty(f.Actions.Stored);
    }

    [Fact]
    public async Task FailedDayQueryAfterDiscardDecision_DoesNotEraseTheDraft()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Manter"; f.Prompt.Choice = UnsavedChangesChoice.Discard; f.Actions.FailDaily = true;
        Assert.False(await Invoke(f.Vm, "CreateTaskOnDayAsync", Monday.AddDays(1))); Assert.Equal("Manter", f.Vm.Title);
        Assert.Equal(Monday, f.Vm.SelectedDay); Assert.True(f.Vm.HasUnsavedChanges); Assert.True(f.Vm.CanEdit);
    }

    [Fact]
    public async Task CreateOutsideTheVisibleWeek_IsRejectedWithoutPrompting()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Manter";
        Assert.False(await Invoke(f.Vm, "CreateTaskOnDayAsync", Monday.AddDays(7))); Assert.Equal(0, f.Prompt.Calls);
        Assert.Equal(Monday, f.Vm.SelectedDay); Assert.Equal("Manter", f.Vm.Title);
    }

    [Fact]
    public async Task OpenContinuation_EditsTheOriginalTaskAndKeepsItsPlanning()
    {
        var task = Example() with { PlannedDate = Monday.AddDays(-2), PlannedEndDate = Monday.AddDays(2) };
        var f = await Fixture.CreateAsync(task); await Invoke(f.Vm, "ShowWeekAsync", Monday);
        var entry = Assert.Single(Week(f.Vm).Days[1].Entries); Assert.True(await f.Vm.EditTaskAsync(entry.Task.Id));
        Assert.Equal(task.Id, f.Vm.EditingTaskId); Assert.Equal(task.PlannedDate, DateOnly.FromDateTime(f.Vm.PlannedDate!.Value.DateTime));
        Assert.Equal(task.PlannedEndDate, DateOnly.FromDateTime(f.Vm.PlannedEndDate!.Value.DateTime)); Assert.True(Property<bool>(f.Vm, "IsWeekView"));
    }

    [Fact]
    public async Task SavingRescheduledTask_RefreshesWeekWithoutChangingItsIdentity()
    {
        var task = Example(); var f = await Fixture.CreateAsync(task); await Invoke(f.Vm, "ShowWeekAsync", Monday); await f.Vm.EditTaskAsync(task.Id);
        f.Vm.PlannedDate = Date(Monday.AddDays(2)); f.Vm.Status = AgendaTaskStatus.Completed; Assert.True(await f.Vm.SaveAsync());
        Assert.Empty(Week(f.Vm).Days[0].Entries); var entry = Assert.Single(Week(f.Vm).Days[2].Entries);
        Assert.Equal(task.Id, entry.Task.Id); Assert.Equal(AgendaTaskStatus.Completed, entry.Task.Status); Assert.Equal(1, Week(f.Vm).UniqueTaskCount);
    }

    [Fact]
    public async Task SaveButton_RefreshesWeekAndPreparesAnotherCleanDraft()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Nova tarefa";
        await f.Vm.SaveCommand.ExecuteAsync(null); Assert.Equal(1, Week(f.Vm).UniqueTaskCount); Assert.Single(f.Actions.Stored);
        Assert.Equal(string.Empty, f.Vm.Title); Assert.Null(f.Vm.EditingTaskId); Assert.False(f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task FailedWeekRefreshAfterSave_DoesNotUndoCommitOrDuplicateOnRetry()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Salva"; f.Actions.FailPeriod = true;
        Assert.True(await f.Vm.SaveAsync()); var id = Assert.Single(f.Actions.Stored.Values).Id;
        Assert.Equal(id, f.Vm.EditingTaskId); Assert.True(Property<bool>(f.Vm, "HasWeekError")); Assert.False(f.Vm.HasUnsavedChanges); Assert.NotEmpty(f.Vm.Feedback);
        f.Actions.FailPeriod = false; f.Vm.Title = "Mesmo ID"; Assert.True(await f.Vm.SaveAsync());
        Assert.Single(f.Actions.Stored); Assert.Equal(id, Assert.Single(Week(f.Vm).Days[0].Entries).Task.Id); Assert.False(Property<bool>(f.Vm, "HasWeekError"));
    }

    [Fact]
    public async Task TrashAndRestoration_RefreshTheWeekWithoutLosingAnotherDraft()
    {
        var task = Example(); var f = await Fixture.CreateAsync(task); await Invoke(f.Vm, "ShowWeekAsync", Monday); f.Vm.Title = "Outro rascunho";
        Assert.True(await f.Vm.MoveToTrashAsync(task.Id, _ => Task.FromResult(true))); Assert.Equal(0, Week(f.Vm).UniqueTaskCount);
        Assert.True(await f.Vm.RestoreTrashEntryAsync(Assert.Single(await f.Actions.ListAsync()))); Assert.Equal(1, Week(f.Vm).UniqueTaskCount);
        Assert.Equal("Outro rascunho", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(0, f.Prompt.Calls);
    }

    [Fact]
    public async Task ReloadCommand_RefreshesTheActiveWeekWhileKeepingFiltersAndDraft()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); await Filter(f.Vm, AgendaTaskCategory.Work, AgendaTaskStatus.Pending);
        var task = Example(); f.Actions.Stored[task.Id] = task; f.Vm.Title = "Meu rascunho"; await f.Vm.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(task, Assert.Single(Week(f.Vm).Days[0].Entries).Task); Assert.Equal("Meu rascunho", f.Vm.Title); Assert.Equal(0, f.Prompt.Calls);
        Assert.Equal(AgendaTaskStatus.Pending, Property<AgendaTaskStatus?>(f.Vm, "WeekStatusFilter"));
    }

    [Fact]
    public async Task PendingWeekLoad_BlocksReentryMutationsAndClosingUntilItCompletes()
    {
        var f = await Fixture.CreateAsync(); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); f.Actions.PeriodGate = gate.Task;
        var operation = Invoke(f.Vm, "ShowWeekAsync", Monday);
        try
        {
            await f.Actions.PeriodEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)); Assert.True(f.Vm.IsBusy); Assert.False(f.Vm.CanEdit);
            Assert.False(await Invoke(f.Vm, "ShowWeekAsync", Monday.AddDays(7))); Assert.False(await Invoke(f.Vm, "ShowDayAsync"));
            Assert.False(await Filter(f.Vm, null, null)); Assert.False(await Invoke(f.Vm, "CreateTaskOnDayAsync", Monday));
            Assert.False(await f.Vm.SaveAsync()); Assert.False(await f.Vm.ChangeDayAsync(Monday.AddDays(1))); Assert.False(await f.Vm.NewTaskAsync()); Assert.False(await f.Vm.RequestCloseAsync());
            Assert.False(await f.Vm.MoveToTrashAsync(Guid.NewGuid(), _ => throw new InvalidOperationException("Must not confirm"))); Assert.Equal(0, f.Prompt.Calls);
            gate.SetResult(); Assert.True(await operation); Assert.True(f.Vm.CanEdit);
        }
        finally { gate.TrySetResult(); }
    }

    [Fact]
    public async Task UnrepresentableWeek_IsRejectedBeforeQueryAndKeepsCurrentWeek()
    {
        var f = await Fixture.CreateAsync(); await Invoke(f.Vm, "ShowWeekAsync", Monday); var previous = Week(f.Vm); var calls = f.Actions.Periods.Count;
        Assert.False(await Invoke(f.Vm, "ShowWeekAsync", DateOnly.MaxValue)); Assert.Equal(previous, Week(f.Vm)); Assert.Equal(calls, f.Actions.Periods.Count); Assert.True(f.Vm.CanEdit);
    }

    [Fact]
    public async Task WeekAndModeChanges_NotifyBindings()
    {
        var f = await Fixture.CreateAsync(); var names = new List<string?>(); f.Vm.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        await Invoke(f.Vm, "ShowWeekAsync", Monday); await Filter(f.Vm, AgendaTaskCategory.Study, AgendaTaskStatus.Completed);
        Assert.Contains("Week", names); Assert.Contains("IsWeekView", names); Assert.Contains("WeekCategoryFilter", names); Assert.Contains("WeekStatusFilter", names);
    }

    [Fact]
    public async Task Composition_EnablesWeeklyQueriesAndEditingInTheSameSqliteProfile()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var vm = services.GetRequiredService<AgendaViewModel>(); Assert.True(Property<bool>(vm, "HasWeekView"));
        var task = Example() with { PlannedDate = Monday.AddDays(-1), PlannedEndDate = Monday.AddDays(1) };
        await services.GetRequiredService<IAgendaActions>().SaveAsync(task); await vm.InitializeAsync(Monday);
        Assert.True(await Invoke(vm, "ShowWeekAsync", Monday)); Assert.Equal(task, Assert.Single(Week(vm).Days[0].Entries).Task);
        await vm.EditTaskAsync(task.Id); vm.Status = AgendaTaskStatus.Completed; Assert.True(await vm.SaveAsync());
        Assert.Equal(AgendaTaskStatus.Completed, (await services.GetRequiredService<IAgendaActions>().GetByIdAsync(task.Id))!.Status);
        Assert.Equal(AgendaTaskStatus.Completed, Assert.Single(Week(vm).Days[0].Entries).Task.Status);
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending, "○", "Pendente")]
    [InlineData(AgendaTaskStatus.InProgress, "◐", "Em andamento")]
    [InlineData(AgendaTaskStatus.Completed, "✓", "Concluída")]
    public void StatePresentation_ProvidesDistinctSymbolsAndTextWithoutChangingCategory(AgendaTaskStatus status, string symbol, string label)
    {
        var converter = new AgendaPresentationConverter();
        Assert.Equal(symbol, converter.Convert(status, typeof(string), "status-icon", CultureInfo.InvariantCulture));
        Assert.Equal(label, converter.Convert(status, typeof(string), "status", CultureInfo.InvariantCulture));
        Assert.Equal("Trabalho", converter.Convert(AgendaTaskCategory.Work, typeof(string), "category", CultureInfo.InvariantCulture));
    }

    private static DateTimeOffset Date(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue));
    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Conteúdo de teste", Monday, new TimeOnly(9, 0), new TimeOnly(10, 0), AgendaTaskCategory.Work);
    private static AgendaWeekSnapshot Week(AgendaViewModel vm) => Assert.IsType<AgendaWeekSnapshot>(Property<object?>(vm, "Week"));
    private static T Property<T>(AgendaViewModel vm, string name)
    {
        var property = typeof(AgendaViewModel).GetProperty(name); Assert.NotNull(property); return (T)property.GetValue(vm)!;
    }
    private static async Task<bool> Invoke(AgendaViewModel vm, string name, params object?[] args)
    {
        var method = typeof(AgendaViewModel).GetMethod(name); Assert.NotNull(method);
        return await Assert.IsAssignableFrom<Task<bool>>(method.Invoke(vm, args));
    }
    private static Task<bool> Filter(AgendaViewModel vm, AgendaTaskCategory? category, AgendaTaskStatus? status) => Invoke(vm, "SetWeekFiltersAsync", category, status);

    private sealed class Fixture
    {
        public Actions Actions { get; } = new();
        public Prompt Prompt { get; } = new();
        public AgendaViewModel Vm { get; private set; } = null!;
        public static async Task<Fixture> CreateAsync(params AgendaTaskSnapshot[] tasks)
        {
            var f = new Fixture(); foreach (var task in tasks) f.Actions.Stored[task.Id] = task;
            var constructor = typeof(AgendaViewModel).GetConstructor([typeof(IAgendaActions), typeof(IUnsavedChangesPrompt), typeof(IAgendaTrashActions), typeof(TimeProvider), typeof(IAgendaPeriodReader), typeof(IAgendaWeekProjector)]);
            Assert.NotNull(constructor);
            f.Vm = Assert.IsType<AgendaViewModel>(constructor.Invoke([f.Actions, f.Prompt, f.Actions, TimeProvider.System, f.Actions, new AgendaWeekProjector()]));
            await f.Vm.InitializeAsync(Monday); return f;
        }
    }
    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(Choice); }
    }
    private sealed class Actions : IAgendaActions, IAgendaPeriodReader, IAgendaTrashActions
    {
        public Dictionary<Guid, AgendaTaskSnapshot> Stored { get; } = [];
        private readonly Dictionary<Guid, AgendaTaskTrashEntry> _trash = [];
        public List<(DateOnly, DateOnly)> Periods { get; } = [];
        public bool FailPeriod { get; set; }
        public bool FailDaily { get; set; }
        public bool FailSave { get; set; }
        public int DailyCalls { get; private set; }
        public Task? PeriodGate { get; set; }
        public TaskCompletionSource PeriodEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Stored.GetValueOrDefault(id));
        public Task<AgendaTaskSaveResult> SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
        {
            if (FailSave) return Task.FromResult(new AgendaTaskSaveResult(AgendaTaskSaveStatus.PersistenceFailed));
            if (!AgendaTaskValidation.IsValid(task)) return Task.FromResult(new AgendaTaskSaveResult(AgendaTaskSaveStatus.ValidationFailed));
            Stored[task.Id] = task; return Task.FromResult(new AgendaTaskSaveResult(AgendaTaskSaveStatus.Saved));
        }
        public Task<IReadOnlyList<AgendaTaskSnapshot>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
        {
            DailyCalls++; if (FailDaily) throw new AgendaPersistenceException("Synthetic daily failure");
            return Task.FromResult<IReadOnlyList<AgendaTaskSnapshot>>(Stored.Values.Where(t => t.PlannedDate >= from && t.PlannedDate <= through).ToArray());
        }
        public async Task<IReadOnlyList<AgendaTaskSnapshot>> ListOverlappingAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
        {
            Periods.Add((from, through)); PeriodEntered.TrySetResult();
            if (PeriodGate is not null) await PeriodGate.WaitAsync(cancellationToken);
            if (FailPeriod) throw new AgendaPersistenceException("Synthetic period failure");
            return Stored.Values.Where(t => t.PlannedDate <= through && (t.PlannedEndDate ?? t.PlannedDate) >= from).ToArray();
        }
        public Task MoveToTrashAsync(Guid id, DateTimeOffset deletedAt, CancellationToken cancellationToken = default)
        {
            _trash[id] = new(Stored[id], deletedAt, null); Stored.Remove(id); return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AgendaTaskTrashEntry>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AgendaTaskTrashEntry>>(_trash.Values.ToArray());
        public Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Stored[id] = _trash[id].Task; _trash.Remove(id); return Task.CompletedTask;
        }
    }
}
