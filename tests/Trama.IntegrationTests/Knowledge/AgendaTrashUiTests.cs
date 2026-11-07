using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaTrashUiTests
{
    private static readonly DateOnly Day = new(2026, 11, 6);
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 20, 0, 0, TimeSpan.FromHours(-3));

    [AvaloniaFact]
    public async Task DeleteAction_IsDisabledForANewDraftAndEnabledForASavedTask()
    {
        using var f = await Fixture.CreateAsync(); var button = f.Button("AgendaDeleteTaskButton"); Assert.False(button.IsEffectivelyEnabled);
        var task = await f.SeedAsync(); await f.Vm.EditTaskAsync(task.Id); Pump(); Assert.True(button.IsEffectivelyEnabled); Assert.True(f.Button("AgendaTrashButton").IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task Confirmation_IdentifiesTheTaskAndDefaultsToCancel()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); var dialog = await f.RequestDeletionAsync(task);
        Assert.Contains(task.Title, Text(dialog)); Assert.Contains("restaur", Text(dialog), StringComparison.OrdinalIgnoreCase);
        Assert.True(Control<Button>(dialog, "CancelAgendaTrashButton").IsCancel); Assert.True(Control<Button>(dialog, "CancelAgendaTrashButton").IsDefault);
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Click(Control<Button>(dialog, "CancelAgendaTrashButton")); await f.WaitIdleAsync();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelOrTitleBarClose_KeepsTheTaskActive(bool titleBar)
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); var dialog = await f.RequestDeletionAsync(task);
        if (titleBar) dialog.Close(); else Click(Control<Button>(dialog, "CancelAgendaTrashButton"));
        await f.WaitIdleAsync(); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Empty(await f.Trash.ListAsync()); Assert.Equal(task.Id, f.Vm.EditingTaskId);
    }

    [AvaloniaTheory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskStatus.Completed)]
    public async Task ConfirmedDeletion_HidesTheTaskAndCleansTheAffectedEditor(AgendaTaskStatus status)
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(status); var dialog = await f.RequestDeletionAsync(task);
        Click(Control<Button>(dialog, "ConfirmAgendaTrashButton")); await f.WaitIdleAsync();
        Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Equal(task, Assert.Single(await f.Trash.ListAsync()).Task);
        Assert.Null(f.Vm.EditingTaskId); Assert.False(f.Vm.HasUnsavedChanges); Assert.Empty(f.Vm.Tasks);
    }

    [AvaloniaTheory]
    [InlineData(UnsavedChangesChoice.Cancel)]
    [InlineData(UnsavedChangesChoice.Discard)]
    [InlineData(UnsavedChangesChoice.Save)]
    public async Task DeletingADirtyTask_UsesTheExistingUnsavedChangesDialog(UnsavedChangesChoice choice)
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); await f.Vm.EditTaskAsync(task.Id); f.Vm.Description = "Rascunho mantido";
        var dialog = await f.RequestDeletionAsync(task); Click(Control<Button>(dialog, "ConfirmAgendaTrashButton"));
        await UntilAsync(() => f.Window.OwnedWindows.OfType<UnsavedChangesDialog>().Any());
        var unsaved = Assert.Single(f.Window.OwnedWindows.OfType<UnsavedChangesDialog>());
        Click(Control<Button>(unsaved, choice switch { UnsavedChangesChoice.Save => "SaveAndContinueButton", UnsavedChangesChoice.Discard => "DiscardButton", _ => "CancelButton" })); await f.WaitIdleAsync();
        if (choice == UnsavedChangesChoice.Cancel)
        {
            Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Equal("Rascunho mantido", f.Vm.Description); Assert.True(f.Vm.HasUnsavedChanges);
        }
        else Assert.Equal(choice == UnsavedChangesChoice.Save ? task with { Description = "Rascunho mantido" } : task, Assert.Single(await f.Trash.ListAsync()).Task);
    }

    [AvaloniaFact]
    public async Task DuplicateDeleteClick_DoesNotOpenAnotherConfirmation()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); var dialog = await f.RequestDeletionAsync(task);
        f.Button("AgendaDeleteTaskButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Assert.Single(f.Window.OwnedWindows, item => item.GetType().Name == "AgendaTrashConfirmationDialog");
        Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Click(Control<Button>(dialog, "CancelAgendaTrashButton")); await f.WaitIdleAsync();
    }

    [AvaloniaFact]
    public async Task TaskDeletedBeforeTheClick_DoesNotOpenConfirmationOrLoseTheDraft()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); await f.Vm.EditTaskAsync(task.Id); f.Vm.Description = "Preservar";
        await f.Trash.MoveToTrashAsync(task.Id, Now); Click(f.Button("AgendaDeleteTaskButton")); await UntilAsync(() => !f.Vm.IsBusy);
        Assert.Empty(f.Window.OwnedWindows); Assert.Equal("Preservar", f.Vm.Description); Assert.True(f.Vm.HasUnsavedChanges); Assert.NotEmpty(f.Vm.Feedback);
    }

    [AvaloniaFact]
    public async Task EmptyTrash_HasNoSelectionAndRestorationDisabled()
    {
        using var f = await Fixture.CreateAsync(); var dialog = await f.OpenTrashAsync(); var list = Control<ListBox>(dialog, "AgendaTrashEntriesList");
        Assert.Empty(list.Items); Assert.Null(list.SelectedItem); Assert.False(Control<Button>(dialog, "RestoreAgendaTrashButton").IsEffectivelyEnabled);
        Assert.True(Control<TextBlock>(dialog, "AgendaTrashEmptyMessage").IsEffectivelyVisible); Assert.Contains("Agenda", dialog.Title!);
    }

    [AvaloniaFact]
    public async Task TrashListsOtherDaysWithStatePlanningAndCompletionMetadata()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(day: Day.AddDays(-1));
        await f.Completion.CompleteAsync([task], Now.AddHours(-2)); await f.Trash.MoveToTrashAsync(task.Id, Now);
        var dialog = await f.OpenTrashAsync(); var entry = Assert.IsType<AgendaTaskTrashEntry>(Assert.Single(Control<ListBox>(dialog, "AgendaTrashEntriesList").Items));
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, entry.Task); Assert.Equal(Now.AddHours(-2), entry.LastCompletedAt);
        var text = Text(dialog); Assert.Contains(task.Title, text); Assert.Contains("Concluída", text); Assert.Contains("Trabalho", text); Assert.Contains("05/11/2026", text); Assert.Contains("06/11/2026", text);
        Assert.Null(Control<ListBox>(dialog, "AgendaTrashEntriesList").SelectedItem);
    }

    [AvaloniaFact]
    public async Task RestoreButton_RestoresOnlyTheSelectedTaskAndKeepsAnotherDraft()
    {
        using var f = await Fixture.CreateAsync(); var first = await f.SeedAsync(); var second = await f.SeedAsync();
        await f.Trash.MoveToTrashAsync(first.Id, Now); await f.Trash.MoveToTrashAsync(second.Id, Now.AddMinutes(-1));
        f.Vm.Title = "Rascunho independente"; f.Vm.StartTimeText = "09:";
        var dialog = await f.OpenTrashAsync(); var list = Control<ListBox>(dialog, "AgendaTrashEntriesList"); list.SelectedItem = list.Items.Cast<AgendaTaskTrashEntry>().Single(item => item.Task.Id == first.Id); Pump();
        Click(Control<Button>(dialog, "RestoreAgendaTrashButton")); await UntilAsync(() => !Busy(dialog) && list.Items.Count == 1);
        Assert.Equal(first, await f.Actions.GetByIdAsync(first.Id)); Assert.Null(await f.Actions.GetByIdAsync(second.Id)); Assert.Null(list.SelectedItem);
        Assert.Equal("Rascunho independente", f.Vm.Title); Assert.Equal("09:", f.Vm.StartTimeText); Assert.True(f.Vm.HasUnsavedChanges); Assert.Equal(Day, f.Vm.SelectedDay);
    }

    [AvaloniaFact]
    public async Task FailedRestoration_KeepsSelectionAndAllowsRetry()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); await f.Trash.MoveToTrashAsync(task.Id, Now); var dialog = await f.OpenTrashAsync();
        var list = Control<ListBox>(dialog, "AgendaTrashEntriesList"); list.SelectedItem = Assert.Single(list.Items); Pump(); f.Gate.FailRestore = true;
        Click(Control<Button>(dialog, "RestoreAgendaTrashButton")); await UntilAsync(() => !Busy(dialog));
        Assert.Single(list.Items); Assert.NotNull(list.SelectedItem); Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.NotEmpty(Control<TextBlock>(dialog, "AgendaTrashFeedback").Text!);
        f.Gate.FailRestore = false; Click(Control<Button>(dialog, "RestoreAgendaTrashButton")); await UntilAsync(() => !Busy(dialog) && list.Items.Count == 0); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id));
    }

    [AvaloniaFact]
    public async Task FailedLoad_ShowsFeedbackAndRefreshRecoversTheList()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); await f.Trash.MoveToTrashAsync(task.Id, Now); f.Gate.FailList = true;
        var dialog = await f.OpenTrashAsync(); Assert.False(Control<Button>(dialog, "RestoreAgendaTrashButton").IsEffectivelyEnabled);
        Assert.False(Control<TextBlock>(dialog, "AgendaTrashEmptyMessage").IsEffectivelyVisible);
        var text = Control<TextBlock>(dialog, "AgendaTrashFeedback").Text!; Assert.NotEmpty(text); Assert.DoesNotContain("Synthetic", text);
        f.Gate.FailList = false; Click(Control<Button>(dialog, "RefreshAgendaTrashButton")); await UntilAsync(() => !Busy(dialog) && Control<ListBox>(dialog, "AgendaTrashEntriesList").Items.Count == 1);
    }

    [AvaloniaFact]
    public async Task PendingLoad_BlocksClosingUntilItFinishes()
    {
        using var f = await Fixture.CreateAsync(); f.Gate.ListGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Click(f.Button("AgendaTrashButton")); await UntilAsync(() => f.FindDialog("AgendaTrashDialog") is not null);
        var dialog = f.FindDialog("AgendaTrashDialog")!;
        try
        {
            Assert.True(Busy(dialog)); Assert.True(Control<TextBlock>(dialog, "AgendaTrashLoadingMessage").IsEffectivelyVisible);
            Assert.False(Control<Button>(dialog, "CloseAgendaTrashButton").IsEffectivelyEnabled); dialog.Close(); Assert.True(dialog.IsVisible);
        }
        finally { f.Gate.ListGate.TrySetResult(); }
        await UntilAsync(() => !Busy(dialog)); Click(Control<Button>(dialog, "CloseAgendaTrashButton")); Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public async Task PendingRestore_BlocksClosingAndDuplicateSubmission()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); await f.Trash.MoveToTrashAsync(task.Id, Now); var dialog = await f.OpenTrashAsync();
        var list = Control<ListBox>(dialog, "AgendaTrashEntriesList"); list.SelectedItem = Assert.Single(list.Items); Pump(); f.Gate.RestoreGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var restore = Control<Button>(dialog, "RestoreAgendaTrashButton"); Click(restore);
        try
        {
            await f.Gate.RestoreEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)); Assert.True(Busy(dialog)); Assert.False(restore.IsEffectivelyEnabled);
            dialog.Close(); Assert.True(dialog.IsVisible); restore.Command?.Execute(restore.CommandParameter); Assert.Equal(1, f.Gate.RestoreCalls);
        }
        finally { f.Gate.RestoreGate.TrySetResult(); }
        await UntilAsync(() => !Busy(dialog) && list.Items.Count == 0); Assert.True(Control<Button>(dialog, "CloseAgendaTrashButton").IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task ClosingTrashWithoutRestoring_DoesNotMutateAnything()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(); await f.Trash.MoveToTrashAsync(task.Id, Now); var dialog = await f.OpenTrashAsync();
        Click(Control<Button>(dialog, "CloseAgendaTrashButton")); await UntilAsync(() => !dialog.IsVisible);
        Assert.Null(await f.Actions.GetByIdAsync(task.Id)); Assert.Equal(task, Assert.Single(await f.Trash.ListAsync()).Task); Assert.Equal(0, f.Gate.RestoreCalls);
    }

    [AvaloniaFact]
    public async Task CompactDialogs_KeepPrimaryActionsInsideTheWindow()
    {
        using var f = await Fixture.CreateAsync(); var task = await f.SeedAsync(title: new string('A', 180)); var confirm = await f.RequestDeletionAsync(task);
        confirm.Width = 480; confirm.Height = 320; Pump(); confirm.UpdateLayout(); AssertInside(confirm, "CancelAgendaTrashButton", "ConfirmAgendaTrashButton");
        Click(Control<Button>(confirm, "CancelAgendaTrashButton")); await f.WaitIdleAsync();
        var trash = await f.OpenTrashAsync(); trash.Width = 520; trash.Height = 420; Pump(); trash.UpdateLayout();
        AssertInside(trash, "CloseAgendaTrashButton", "RefreshAgendaTrashButton", "RestoreAgendaTrashButton");
    }

    [AvaloniaFact]
    public async Task LegacyViewModelWithoutTrash_DoesNotExposeEnabledTrashActions()
    {
        using var f = await Fixture.CreateAsync(legacy: true);
        foreach (var name in new[] { "AgendaTrashButton", "AgendaDeleteTaskButton" })
        {
            var button = f.View.FindControl<Button>(name); Assert.True(button is null || !button.IsEffectivelyVisible || !button.IsEffectivelyEnabled);
        }
        f.Vm.Title = "Cadastro preservado"; Assert.True(await f.Vm.SaveAsync()); Assert.Single(await f.Actions.ListAsync(Day, Day));
    }

    private static T Control<T>(Control root, string name) where T : Control { var item = root.FindControl<T>(name); Assert.NotNull(item); return item; }
    private static bool Busy(Window dialog) { var property = dialog.DataContext?.GetType().GetProperty("IsBusy"); Assert.NotNull(property); return (bool)property.GetValue(dialog.DataContext)!; }
    private static string Text(Control root) => string.Join("\n", root.GetVisualDescendants().OfType<TextBlock>().Select(item => item.Text));
    private static void Pump() => Dispatcher.UIThread.RunJobs();
    private static void Click(Button button)
    {
        Assert.True(button.IsEffectivelyVisible && button.IsEffectivelyEnabled);
        if (button.Command is { } command) { Assert.True(command.CanExecute(button.CommandParameter)); command.Execute(button.CommandParameter); }
        else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();
    }
    private static async Task UntilAsync(Func<bool> ready)
    {
        var timer = Stopwatch.StartNew();
        while (!ready()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), "O diálogo não terminou a operação."); Pump(); await Task.Delay(10); }
        Pump();
    }
    private static void AssertInside(Window window, params string[] names)
    {
        foreach (var name in names)
        {
            var control = Control<Control>(window, name); Assert.True(control.IsEffectivelyVisible); Assert.True(control.Bounds.Width > 0 && control.Bounds.Height > 0);
            var point = control.TranslatePoint(default, window)!.Value; Assert.InRange(point.X, 0, window.Bounds.Width - control.Bounds.Width + 1); Assert.InRange(point.Y, 0, window.Bounds.Height - control.Bounds.Height + 1);
        }
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("TrashUiTest", Now.Offset, "Test", "Test");
    }
    private sealed class GatedTrash(IAgendaTrashActions inner) : IAgendaTrashActions
    {
        public bool FailList { get; set; }
        public bool FailRestore { get; set; }
        public TaskCompletionSource? ListGate { get; set; }
        public TaskCompletionSource? RestoreGate { get; set; }
        public TaskCompletionSource RestoreEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int RestoreCalls { get; private set; }
        public Task MoveToTrashAsync(Guid id, DateTimeOffset at, CancellationToken ct = default) => inner.MoveToTrashAsync(id, at, ct);
        public async Task<IReadOnlyList<AgendaTaskTrashEntry>> ListAsync(CancellationToken ct = default)
        {
            if (ListGate is { } gate) await gate.Task.WaitAsync(ct); if (FailList) throw new AgendaPersistenceException("Synthetic list failure"); return await inner.ListAsync(ct);
        }
        public async Task RestoreAsync(Guid id, CancellationToken ct = default)
        {
            RestoreCalls++; RestoreEntered.TrySetResult(); if (RestoreGate is { } gate) await gate.Task.WaitAsync(ct);
            if (FailRestore) throw new AgendaPersistenceException("Synthetic restore failure"); await inner.RestoreAsync(id, ct);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public Window Window { get; } = new() { Width = 1100, Height = 760 };
        public AgendaWorkspaceView View { get; private set; } = null!;
        public AgendaViewModel Vm { get; private set; } = null!;
        public SqliteAgendaActions Actions { get; private set; } = null!;
        public SqliteAgendaTrashActions Trash { get; private set; } = null!;
        public SqliteAgendaCompletionActions Completion { get; private set; } = null!;
        public GatedTrash Gate { get; private set; } = null!;
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture();
            try
            {
                await using var context = f.Root.CreateContext(); await context.Database.MigrateAsync(); var path = Path.Combine(f.Root.DirectoryPath, "trama.db");
                f.Actions = new(path); f.Trash = new(path); f.Completion = new(path); f.Gate = new(f.Trash); var prompt = new WindowUnsavedChangesPrompt(f.Window);
                f.Vm = legacy ? new(f.Actions, prompt) : new(f.Actions, prompt, f.Gate, new Clock()); await f.Vm.InitializeAsync(Day);
                f.View = new() { DataContext = f.Vm }; f.Window.Content = f.View; f.Window.Show(); Pump(); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public Button Button(string name) => Control<Button>(View, name);
        public Window? FindDialog(string name) => Window.OwnedWindows.SingleOrDefault(item => item.GetType().Name == name);
        public async Task<AgendaTaskSnapshot> SeedAsync(AgendaTaskStatus status = AgendaTaskStatus.Pending, DateOnly? day = null, string? title = null)
        {
            var task = new AgendaTaskSnapshot(Guid.NewGuid(), title ?? "Estudar C#", "Conteúdo fictício", day ?? Day, new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Work, status) { PlannedEndDate = (day ?? Day).AddDays(1) };
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status); await Vm.ReloadCommand.ExecuteAsync(null); return task;
        }
        public async Task<Window> RequestDeletionAsync(AgendaTaskSnapshot task)
        {
            Assert.True(await Vm.EditTaskAsync(task.Id)); Click(Button("AgendaDeleteTaskButton")); await UntilAsync(() => FindDialog("AgendaTrashConfirmationDialog") is not null); return FindDialog("AgendaTrashConfirmationDialog")!;
        }
        public async Task<Window> OpenTrashAsync()
        {
            Click(Button("AgendaTrashButton")); await UntilAsync(() => FindDialog("AgendaTrashDialog") is not null);
            var dialog = FindDialog("AgendaTrashDialog")!; await UntilAsync(() => !Busy(dialog)); dialog.UpdateLayout(); Pump(); return dialog;
        }
        public Task WaitIdleAsync() => UntilAsync(() => !Vm.IsBusy && Window.OwnedWindows.Count == 0);
        public void Dispose()
        {
            Gate?.ListGate?.TrySetResult(); Gate?.RestoreGate?.TrySetResult();
            var timer = Stopwatch.StartNew();
            while (Window.OwnedWindows.Any(item => item.DataContext?.GetType().GetProperty("IsBusy")?.GetValue(item.DataContext) is true) && timer.Elapsed < TimeSpan.FromSeconds(5)) { Pump(); Thread.Sleep(5); }
            foreach (var child in Window.OwnedWindows.ToArray()) child.Close(); Window.Close(); Pump(); Root.Dispose();
        }
    }
}
