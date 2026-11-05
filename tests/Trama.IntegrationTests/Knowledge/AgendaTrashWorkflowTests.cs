using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaTrashWorkflowTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 20, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public async Task Composition_RegistersTheTrashServiceInTheSameProfile()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var trash = services.GetService<IAgendaTrashActions>(); Assert.NotNull(trash);
        var actions = services.GetRequiredService<IAgendaActions>(); var task = Example(); await actions.SaveAsync(task);
        await trash.MoveToTrashAsync(task.Id, Now); Assert.Null(await actions.GetByIdAsync(task.Id)); Assert.Equal(task, Assert.Single(await trash.ListAsync()).Task);
    }

    [Fact]
    public async Task ExistingConstructor_StillSupportsSavingWithoutTrashDependencies()
    {
        using var root = new NotebookTestWorkspace(); await using (var context = root.CreateContext()) await context.Database.MigrateAsync();
        var actions = new SqliteAgendaActions(Path.Combine(root.DirectoryPath, "trama.db")); var vm = new AgendaViewModel(actions, new Prompt());
        await vm.InitializeAsync(Day); vm.Title = "Compatibilidade"; Assert.True(await vm.SaveAsync()); Assert.Single(await actions.ListAsync(Day, Day));
    }

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskStatus.Completed)]
    public async Task ConfirmedDeletion_HidesTheTaskAndPreservesItsContent(AgendaTaskStatus status)
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { Status = status, PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task); await f.Vm.ReloadCommand.ExecuteAsync(null);
        AgendaTaskSnapshot? preview = null;
        Assert.True(await MoveAsync(f.Vm, task.Id, item => { preview = item; return Task.FromResult(true); }));
        Assert.Equal(task, preview); Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(f.Vm.Tasks);
        var entry = Assert.Single(await f.Trash.ListAsync()); Assert.Equal(task, entry.Task); Assert.Equal(Now, entry.DeletedAt); Assert.Equal(Now.Offset, entry.DeletedAt.Offset);
    }

    [Fact]
    public async Task CancelledConfirmation_DoesNotPromptForOrChangeTheDraft()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id); f.Vm.Title = "Não perder";
        Assert.False(await MoveAsync(f.Vm, task.Id, _ => Task.FromResult(false)));
        Assert.Equal(0, f.Prompt.Calls); Assert.Equal("Não perder", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await f.Trash.ListAsync());
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, false)]
    [InlineData(UnsavedChangesChoice.Discard, true)]
    [InlineData(UnsavedChangesChoice.Save, true)]
    public async Task DeletingTheEditedTask_ResolvesUnsavedChangesWithoutLosingContent(UnsavedChangesChoice choice, bool expected)
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id);
        f.Vm.Description = "Edição não salva"; f.Vm.StartTimeText = "18:30"; f.Prompt.Choice = choice;
        Assert.Equal(expected, await MoveAsync(f.Vm, task.Id, _ => Task.FromResult(true))); Assert.Equal(1, f.Prompt.Calls);
        if (!expected)
        {
            Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Equal("Edição não salva", f.Vm.Description); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(task.Id, f.Vm.EditingTaskId);
        }
        else
        {
            var trashed = Assert.Single(await f.Trash.ListAsync()).Task;
            Assert.Equal(choice == UnsavedChangesChoice.Save ? task with { Description = "Edição não salva", StartTime = new TimeOnly(18, 30) } : task, trashed);
            Assert.Null(f.Vm.EditingTaskId); Assert.False(f.Vm.HasUnsavedChanges); Assert.Equal(string.Empty, f.Vm.Title); Assert.Null(f.Vm.PlannedEndDate);
        }
    }

    [Fact]
    public async Task DeletingAnotherTask_DoesNotDiscardOrPromptForTheCurrentDraft()
    {
        using var f = await Fixture.CreateAsync(); var first = Example(); var other = Example(); await f.SaveAsync(first); await f.SaveAsync(other); await f.Vm.EditTaskAsync(first.Id);
        f.Vm.Title = "Meu rascunho";
        Assert.True(await MoveAsync(f.Vm, other.Id, _ => Task.FromResult(true))); Assert.Equal(0, f.Prompt.Calls);
        Assert.Equal(first.Id, f.Vm.EditingTaskId); Assert.Equal("Meu rascunho", f.Vm.Title); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(first, await f.Actions.GetByIdAsync(first.Id));
    }

    [Fact]
    public async Task FailedDeletion_KeepsTheDraftAndReturnsToAnEditableState()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id); f.Vm.Description = "Manter"; f.Prompt.Choice = UnsavedChangesChoice.Discard;
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailWorkflowDelete BEFORE UPDATE OF DeletedAt ON AgendaTasks BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        Assert.False(await MoveAsync(f.Vm, task.Id, _ => Task.FromResult(true))); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
        Assert.Equal("Manter", f.Vm.Description); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(task.Id, f.Vm.EditingTaskId); Assert.False(f.Vm.IsBusy); Assert.True(f.Vm.CanEdit); Assert.NotEmpty(f.Vm.Feedback);
    }

    [Fact]
    public async Task FailedDraftSave_PreventsDeletion()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id);
        f.Vm.Title = ""; f.Prompt.Choice = UnsavedChangesChoice.Save;
        Assert.False(await MoveAsync(f.Vm, task.Id, _ => Task.FromResult(true))); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await f.Trash.ListAsync()); Assert.True(f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task MissingTask_IsNotConfirmedOrDeleted()
    {
        using var f = await Fixture.CreateAsync(); var calls = 0;
        Assert.False(await MoveAsync(f.Vm, Guid.NewGuid(), _ => { calls++; return Task.FromResult(true); }));
        Assert.Equal(0, calls); Assert.Empty(await f.Trash.ListAsync()); Assert.True(f.Vm.CanEdit);
    }

    [Fact]
    public async Task TaskDeletedDuringConfirmation_IsRecheckedWithoutClearingTheDraft()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id); f.Vm.Description = "Preservar";
        Assert.False(await MoveAsync(f.Vm, task.Id, async _ => { await f.Trash.MoveToTrashAsync(task.Id, Now.AddMinutes(-1)); return true; }));
        Assert.Equal("Preservar", f.Vm.Description); Assert.Equal(task.Id, f.Vm.EditingTaskId); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(Now.AddMinutes(-1), Assert.Single(await f.Trash.ListAsync()).DeletedAt);
    }

    [Fact]
    public async Task PendingConfirmation_BlocksReentryNavigationAndClosing()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = MoveAsync(f.Vm, task.Id, _ => { entered.SetResult(); return gate.Task; });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); Assert.True(f.Vm.IsBusy); Assert.False(f.Vm.CanEdit);
            Assert.False(await MoveAsync(f.Vm, task.Id, _ => throw new InvalidOperationException("Reentry")));
            Assert.False(await f.Vm.ChangeDayAsync(Day.AddDays(1))); Assert.False(await f.Vm.RequestCloseAsync());
            gate.SetResult(false); Assert.False(await operation); Assert.True(f.Vm.CanEdit);
        }
        finally { gate.TrySetResult(false); }
    }

    [Fact]
    public async Task ConfirmationFailure_DoesNotHideTheTaskOrLeaveTheFormBusy()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task);
        Assert.False(await MoveAsync(f.Vm, task.Id, _ => throw new InvalidOperationException("Synthetic confirmation failure")));
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.True(f.Vm.CanEdit); Assert.Empty(await f.Trash.ListAsync()); Assert.NotEmpty(f.Vm.Feedback);
    }

    [Fact]
    public async Task TrashList_IncludesOtherDaysAndPreservesTheLastCompletion()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedDate = Day.AddDays(-3), PlannedEndDate = Day.AddDays(-2) }; await f.SaveAsync(task);
        await f.Completion.CompleteAsync([task], Now.AddDays(-1)); await f.Trash.MoveToTrashAsync(task.Id, Now);
        var entry = Assert.Single(await ListAsync(f.Vm)); Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, entry.Task); Assert.Equal(Now.AddDays(-1), entry.LastCompletedAt);
        Assert.Equal(Day, f.Vm.SelectedDay);
    }

    [Fact]
    public async Task Restoring_PreservesOriginalPlanningStateAndCompletionWithoutChangingTheSelectedDay()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedDate = Day.AddDays(-3), PlannedEndDate = Day.AddDays(-2) }; await f.SaveAsync(task);
        await f.Completion.CompleteAsync([task], Now.AddDays(-1)); await f.Trash.MoveToTrashAsync(task.Id, Now);
        Assert.True(await RestoreAsync(f.Vm, Assert.Single(await f.Trash.ListAsync())));
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await ListAsync(f.Vm));
        Assert.Equal(Now.AddDays(-1), await f.Completion.GetLastCompletedAtAsync(task.Id)); Assert.Equal(Day, f.Vm.SelectedDay); Assert.Empty(f.Vm.Tasks);
    }

    [Fact]
    public async Task RestoringTodaysTask_RefreshesTheListWithoutOverwritingAnotherDraft()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, Now);
        f.Vm.Title = "Rascunho independente"; f.Vm.StartTimeText = "09:";
        Assert.True(await RestoreAsync(f.Vm, Assert.Single(await f.Trash.ListAsync())));
        Assert.Equal(task, Assert.Single(f.Vm.Tasks)); Assert.Equal("Rascunho independente", f.Vm.Title); Assert.Equal("09:", f.Vm.StartTimeText); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(0, f.Prompt.Calls);
    }

    [Fact]
    public async Task FailedRestoration_LeavesTheEntryInTheTrashAndPreservesTheDraft()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Trash.MoveToTrashAsync(task.Id, Now); f.Vm.Title = "Manter";
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailWorkflowRestore BEFORE UPDATE OF DeletedAt ON AgendaTasks WHEN NEW.DeletedAt IS NULL BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        Assert.False(await RestoreAsync(f.Vm, Assert.Single(await f.Trash.ListAsync()))); Assert.Single(await ListAsync(f.Vm)); Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Equal("Manter", f.Vm.Title); Assert.True(f.Vm.CanEdit);
    }

    [Fact]
    public async Task FailedListRefreshAfterDeletion_DoesNotReportTheMutationAsUnperformedOrResurrectTheEditor()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id);
        f.Actions.FailList = true;
        Assert.True(await MoveAsync(f.Vm, task.Id, _ => Task.FromResult(true))); Assert.True(f.Vm.HasListError); Assert.Null(f.Vm.EditingTaskId); Assert.False(f.Vm.HasUnsavedChanges);
        Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Single(await f.Trash.ListAsync()); Assert.NotEmpty(f.Vm.Feedback);
    }

    private static Task<bool> MoveAsync(AgendaViewModel vm, Guid id, Func<AgendaTaskSnapshot, Task<bool>> confirm) =>
        Invoke<Task<bool>>(vm, "MoveToTrashAsync", [typeof(Guid), typeof(Func<AgendaTaskSnapshot, Task<bool>>)], [id, confirm]);
    private static Task<IReadOnlyList<AgendaTaskTrashEntry>> ListAsync(AgendaViewModel vm) => Invoke<Task<IReadOnlyList<AgendaTaskTrashEntry>>>(vm, "ListTrashAsync", [], []);
    private static Task<bool> RestoreAsync(AgendaViewModel vm, AgendaTaskTrashEntry entry) => Invoke<Task<bool>>(vm, "RestoreTrashEntryAsync", [typeof(AgendaTaskTrashEntry)], [entry]);
    private static T Invoke<T>(object target, string name, Type[] types, object?[] args)
    {
        var method = target.GetType().GetMethod(name, types); Assert.NotNull(method); return Assert.IsAssignableFrom<T>(method.Invoke(target, args));
    }
    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Planejamento preservado", Day, new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Work);
    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(Choice); }
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("AgendaTrashWorkflowTest", Now.Offset, "Test", "Test");
    }
    private sealed class Actions(SqliteAgendaActions inner) : IAgendaActions
    {
        public bool FailList { get; set; }
        public Task<AgendaTaskSaveResult> SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default) => inner.SaveAsync(task, cancellationToken);
        public Task<AgendaTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);
        public Task<IReadOnlyList<AgendaTaskSnapshot>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default) => FailList
            ? throw new AgendaPersistenceException("Synthetic list failure") : inner.ListAsync(from, through, cancellationToken);
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public Prompt Prompt { get; } = new();
        public Actions Actions { get; private set; } = null!;
        public SqliteAgendaTrashActions Trash { get; private set; } = null!;
        public SqliteAgendaCompletionActions Completion { get; private set; } = null!;
        public AgendaViewModel Vm { get; private set; } = null!;
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            try
            {
                await using var context = f.Root.CreateContext(); await context.Database.MigrateAsync(); var path = Path.Combine(f.Root.DirectoryPath, "trama.db");
                f.Actions = new(new SqliteAgendaActions(path)); f.Trash = new(path); f.Completion = new(path);
                var ctor = typeof(AgendaViewModel).GetConstructor([typeof(IAgendaActions), typeof(IUnsavedChangesPrompt), typeof(IAgendaTrashActions), typeof(TimeProvider)]);
                Assert.NotNull(ctor); f.Vm = Assert.IsType<AgendaViewModel>(ctor.Invoke([f.Actions, f.Prompt, f.Trash, new Clock()])); await f.Vm.InitializeAsync(Day); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public async Task SaveAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        public void Dispose() => Root.Dispose();
    }
}
