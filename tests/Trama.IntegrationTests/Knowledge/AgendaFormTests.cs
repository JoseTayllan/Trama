using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaFormTests
{
    private static readonly DateOnly Day = new(2026, 11, 3);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initialize_LoadsSelectedDayAndStartsACleanDraft(bool seeded)
    {
        var actions = new Actions(); var task = Example();
        if (seeded) actions.Stored[task.Id] = task;
        var vm = new AgendaViewModel(actions, new Prompt()); await vm.InitializeAsync(Day);
        Assert.Equal(Day, vm.SelectedDay); Assert.False(vm.HasUnsavedChanges); Assert.False(vm.IsBusy);
        Assert.Equal(Day, DateOnly.FromDateTime(vm.PlannedDate!.Value.DateTime));
        Assert.Equal(seeded ? new[] { task } : Array.Empty<AgendaTaskSnapshot>(), vm.Tasks);
    }

    [Fact]
    public async Task Create_PreservesFieldsAndLocalCalendarDateInsteadOfUtcDate()
    {
        var f = await Fixture(); var vm = f.Vm;
        vm.Title = "  Estudar C# 🧵  "; vm.Description = "\r\n    exemplo\t";
        vm.PlannedDate = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.FromHours(14));
        vm.StartTime = TimeSpan.FromHours(18); vm.EndTime = TimeSpan.FromHours(19);
        vm.Category = AgendaTaskCategory.Study; vm.Status = AgendaTaskStatus.InProgress;
        Assert.True(vm.HasUnsavedChanges); Assert.True(await vm.SaveAsync());
        var saved = Assert.Single(f.Actions.Stored.Values);
        Assert.NotEqual(Guid.Empty, saved.Id); Assert.Equal(saved.Id, vm.EditingTaskId);
        Assert.Equal(vm.Title, saved.Title); Assert.Equal(vm.Description, saved.Description); Assert.Equal(Day, saved.PlannedDate);
        Assert.Equal(new TimeOnly(18, 0), saved.StartTime); Assert.Equal(new TimeOnly(19, 0), saved.EndTime);
        Assert.Equal(AgendaTaskCategory.Study, saved.Category); Assert.Equal(AgendaTaskStatus.InProgress, saved.Status);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditAndReschedule_KeepTheExistingIdentity()
    {
        var task = Example(); var f = await Fixture(task); Assert.True(await f.Vm.EditTaskAsync(task.Id));
        f.Vm.Title = "Reagendada"; f.Vm.PlannedDate = new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);
        f.Vm.StartTime = null; f.Vm.EndTime = null;
        Assert.True(await f.Vm.SaveAsync());
        Assert.Equal(task.Id, Assert.Single(f.Actions.Stored.Values).Id);
        Assert.Equal(Day.AddDays(1), f.Actions.Stored[task.Id].PlannedDate); Assert.Empty(f.Vm.Tasks);
        Assert.False(f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task CompleteAndReopen_KeepThePlannedDateAndHours()
    {
        var task = Example(); var f = await Fixture(task); Assert.True(await f.Vm.EditTaskAsync(task.Id));
        f.Vm.Status = AgendaTaskStatus.Completed; Assert.True(await f.Vm.SaveAsync());
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, f.Actions.Stored[task.Id]);
        f.Vm.Status = AgendaTaskStatus.Pending; Assert.True(await f.Vm.SaveAsync());
        Assert.Equal(task, f.Actions.Stored[task.Id]);
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, false, false)]
    [InlineData(UnsavedChangesChoice.Discard, true, false)]
    [InlineData(UnsavedChangesChoice.Save, true, true)]
    public async Task NewTask_ResolvesTheExistingDraft(UnsavedChangesChoice choice, bool changed, bool saved)
    {
        var f = await Fixture(); f.Vm.Title = "Rascunho"; f.Prompt.Choice = choice;
        Assert.Equal(changed, await f.Vm.NewTaskAsync()); Assert.Equal(1, f.Prompt.Calls);
        Assert.Equal(saved ? 1 : 0, f.Actions.Stored.Count);
        Assert.Equal(changed ? string.Empty : "Rascunho", f.Vm.Title);
        Assert.Equal(!changed, f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task OpeningAnotherTask_CancelKeepsTheCurrentDraft()
    {
        var first = Example(); var second = Example() with { Title = "Outra" }; var f = await Fixture(first, second);
        Assert.True(await f.Vm.EditTaskAsync(first.Id)); f.Vm.Title = "Alteração pendente";
        Assert.False(await f.Vm.EditTaskAsync(second.Id));
        Assert.Equal(first.Id, f.Vm.EditingTaskId); Assert.Equal("Alteração pendente", f.Vm.Title);
        Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(first, f.Actions.Stored[first.Id]);
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, false, false)]
    [InlineData(UnsavedChangesChoice.Discard, true, false)]
    [InlineData(UnsavedChangesChoice.Save, true, true)]
    public async Task ChangingDay_ResolvesDraftBeforeReplacingTheSelection(UnsavedChangesChoice choice, bool changed, bool saved)
    {
        var f = await Fixture(); f.Vm.Title = "Antes de trocar o dia"; f.Prompt.Choice = choice;
        Assert.Equal(changed, await f.Vm.ChangeDayAsync(Day.AddDays(1)));
        Assert.Equal(changed ? Day.AddDays(1) : Day, f.Vm.SelectedDay);
        Assert.Equal(saved ? 1 : 0, f.Actions.Stored.Count);
        if (saved) Assert.Equal(Day, Assert.Single(f.Actions.Stored.Values).PlannedDate);
        if (!changed) Assert.Equal("Antes de trocar o dia", f.Vm.Title);
    }

    [Fact]
    public async Task InvalidTitle_DoesNotSaveOrLoseTheOtherFields()
    {
        var f = await Fixture(); f.Vm.Title = "   "; f.Vm.Description = "Manter a descrição";
        Assert.False(await f.Vm.SaveAsync()); Assert.Empty(f.Actions.Stored);
        Assert.Equal("Manter a descrição", f.Vm.Description); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.False(string.IsNullOrWhiteSpace(f.Vm.Feedback));
    }

    [Fact]
    public async Task MissingDate_IsRejectedWithoutLosingTheDraft()
    {
        var f = await Fixture(); f.Vm.Title = "Manter tarefa sem data"; f.Vm.PlannedDate = null;
        Assert.False(await f.Vm.SaveAsync()); Assert.Empty(f.Actions.Stored);
        Assert.Equal("Manter tarefa sem data", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.False(string.IsNullOrWhiteSpace(f.Vm.Feedback));
    }

    [Fact]
    public async Task FailedDayQuery_DoesNotDiscardTheDraftOrChangeTheSelectedDay()
    {
        var f = await Fixture(); f.Vm.Title = "Manter rascunho ao falhar consulta";
        f.Prompt.Choice = UnsavedChangesChoice.Discard; f.Actions.FailList = true;
        Assert.False(await f.Vm.ChangeDayAsync(Day.AddDays(1)));
        Assert.Equal(Day, f.Vm.SelectedDay); Assert.Equal("Manter rascunho ao falhar consulta", f.Vm.Title);
        Assert.True(f.Vm.HasUnsavedChanges); Assert.False(f.Vm.IsBusy);
    }

    [Fact]
    public async Task PersistenceFailure_KeepsIdentityAndDraftForRetry()
    {
        var task = Example(); var f = await Fixture(task); Assert.True(await f.Vm.EditTaskAsync(task.Id));
        f.Vm.Title = "Manter alteração"; f.Actions.FailSave = true;
        Assert.False(await f.Vm.SaveAsync()); Assert.Equal(task.Id, f.Vm.EditingTaskId);
        Assert.Equal("Manter alteração", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges); Assert.False(f.Vm.IsBusy);
        f.Actions.FailSave = false; Assert.True(await f.Vm.SaveAsync());
        Assert.Single(f.Actions.Stored); Assert.Equal("Manter alteração", f.Actions.Stored[task.Id].Title);
    }

    [Fact]
    public async Task RefreshFailureAfterCommit_DoesNotCreateAnotherTaskOnTheNextSave()
    {
        var f = await Fixture(); f.Vm.Title = "Gravação confirmada"; f.Actions.FailList = true;
        Assert.True(await f.Vm.SaveAsync()); var id = Assert.Single(f.Actions.Stored.Values).Id;
        Assert.Equal(id, f.Vm.EditingTaskId); Assert.False(f.Vm.HasUnsavedChanges);
        Assert.False(string.IsNullOrWhiteSpace(f.Vm.Feedback));
        f.Actions.FailList = false; f.Vm.Title = "Mesmo ID"; Assert.True(await f.Vm.SaveAsync());
        Assert.Equal(id, Assert.Single(f.Actions.Stored.Values).Id);
    }

    [Fact]
    public async Task PendingSave_BlocksDuplicateSaveAndClosingWithoutPrompting()
    {
        var f = await Fixture(); f.Vm.Title = "Aguardar gravação";
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); f.Actions.Gate = gate.Task;
        var save = f.Vm.SaveAsync();
        try
        {
            await f.Actions.SaveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(f.Vm.IsBusy); Assert.False(f.Vm.CanEdit);
            Assert.False(await f.Vm.SaveAsync()); Assert.False(await f.Vm.RequestCloseAsync());
            Assert.Equal(1, f.Actions.SaveCalls); Assert.Equal(0, f.Prompt.Calls);
            gate.SetResult(); Assert.True(await save); Assert.False(f.Vm.IsBusy);
        }
        finally { gate.TrySetResult(); }
    }

    [Fact]
    public async Task EditsAfterTheSaveSnapshot_AreKeptDirtyForTheNextSave()
    {
        var f = await Fixture(); f.Vm.Title = "Versão enviada";
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); f.Actions.Gate = gate.Task;
        var save = f.Vm.SaveAsync();
        try
        {
            await f.Actions.SaveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)); f.Vm.Title = "Edição mais recente";
            gate.SetResult(); Assert.True(await save);
            Assert.Equal("Versão enviada", Assert.Single(f.Actions.Stored.Values).Title);
            Assert.Equal("Edição mais recente", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges);
            Assert.True(await f.Vm.SaveAsync()); Assert.Single(f.Actions.Stored);
            Assert.Equal("Edição mais recente", Assert.Single(f.Actions.Stored.Values).Title);
        }
        finally { gate.TrySetResult(); }
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, false, false)]
    [InlineData(UnsavedChangesChoice.Discard, true, false)]
    [InlineData(UnsavedChangesChoice.Save, true, true)]
    public async Task Close_RespectsTheDecisionWithoutErasingTheDraftPrematurely(UnsavedChangesChoice choice, bool approved, bool saved)
    {
        var f = await Fixture(); f.Vm.Title = "Rascunho ao fechar"; f.Prompt.Choice = choice;
        Assert.Equal(approved, await f.Vm.RequestCloseAsync()); Assert.Equal(saved ? 1 : 0, f.Actions.Stored.Count);
        Assert.Equal("Rascunho ao fechar", f.Vm.Title);
        Assert.Equal(!saved, f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task Close_SaveFailureKeepsTheWindowDecisionUnapproved()
    {
        var f = await Fixture(); f.Vm.Title = "Não perder"; f.Prompt.Choice = UnsavedChangesChoice.Save; f.Actions.FailSave = true;
        Assert.False(await f.Vm.RequestCloseAsync()); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.Equal("Não perder", f.Vm.Title);
    }

    [Fact]
    public async Task MissingTask_DoesNotDiscardTheCurrentDraft()
    {
        var f = await Fixture(); f.Vm.Title = "Manter rascunho";
        Assert.False(await f.Vm.EditTaskAsync(Guid.NewGuid()));
        Assert.Equal("Manter rascunho", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.Equal(0, f.Prompt.Calls);
    }

    [Fact]
    public void NullActions_IsRejected() => Assert.Throws<ArgumentNullException>(() => new AgendaViewModel(null!, new Prompt()));

    [Fact]
    public void NullPrompt_IsRejected() => Assert.Throws<ArgumentNullException>(() => new AgendaViewModel(new Actions(), null!));

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Estudar interfaces", "Conteúdo fictício", Day,
        new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Study);

    private static async Task<(AgendaViewModel Vm, Actions Actions, Prompt Prompt)> Fixture(params AgendaTaskSnapshot[] tasks)
    {
        var actions = new Actions(); foreach (var task in tasks) actions.Stored[task.Id] = task;
        var prompt = new Prompt(); var vm = new AgendaViewModel(actions, prompt); await vm.InitializeAsync(Day);
        return (vm, actions, prompt);
    }

    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(Choice); }
    }

    private sealed class Actions : IAgendaActions
    {
        public Dictionary<Guid, AgendaTaskSnapshot> Stored { get; } = [];
        public bool FailSave { get; set; }
        public bool FailList { get; set; }
        public Task? Gate { get; set; }
        public int SaveCalls { get; private set; }
        public TaskCompletionSource SaveEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<AgendaTaskSaveResult> SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
        {
            SaveCalls++; SaveEntered.TrySetResult();
            if (Gate is not null) await Gate.WaitAsync(cancellationToken);
            if (FailSave) return new(AgendaTaskSaveStatus.PersistenceFailed);
            return await new SaveAgendaTask(new Writer(this)).ExecuteAsync(task, cancellationToken);
        }
        public Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Stored.GetValueOrDefault(id));
        public Task<IReadOnlyList<AgendaTaskSnapshot>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
        {
            if (FailList) throw new AgendaPersistenceException("Synthetic list failure.");
            return Task.FromResult<IReadOnlyList<AgendaTaskSnapshot>>(Stored.Values.Where(task => task.PlannedDate >= from && task.PlannedDate <= through)
                .OrderBy(task => task.PlannedDate).ThenBy(task => task.StartTime is null).ThenBy(task => task.StartTime)
                .ThenBy(task => task.Title).ThenBy(task => task.Id).ToArray());
        }
        private sealed class Writer(Actions owner) : IAgendaTaskRepository
        {
            public Task SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
            {
                owner.Stored[task.Id] = task; return Task.CompletedTask;
            }
        }
    }
}
