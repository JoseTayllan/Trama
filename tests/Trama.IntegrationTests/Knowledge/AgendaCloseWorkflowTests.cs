using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaCloseWorkflowTests
{
    [Fact]
    public async Task NoCandidates_ApprovesWithoutPromptOrCompletionAndKeepsTheEditorAlive()
    {
        using var f = await AgendaCloseFixture.CreateAsync();
        Assert.True(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Prompt.Calls); Assert.Equal(0, f.Actions.CompleteCalls);
        Assert.True(f.Library.Editor.CanEditContent);
    }

    [Theory]
    [InlineData(AgendaCompletionChoice.KeepOpen, true)]
    [InlineData(AgendaCompletionChoice.Cancel, false)]
    public async Task KeepingOpenOrCancelling_DoesNotChangeTheTasks(AgendaCompletionChoice choice, bool approved)
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(choice, []);
        Assert.Equal(approved, await f.Closing.RequestCloseAsync());
        Assert.Equal(1, f.Prompt.Calls); Assert.Equal(0, f.Actions.CompleteCalls); Assert.Equal(task, await f.ReadAsync(task.Id));
        Assert.True(f.Library.Editor.CanEditContent);
    }

    [Fact]
    public async Task CompletingOnlyTheSelection_PreservesOtherTasksAndUsesTheLocalClock()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var selected = await f.SeedAsync(AgendaCloseFixture.Day.AddDays(-1)); var other = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [selected.Id]);
        Assert.True(await f.Closing.RequestCloseAsync());
        Assert.Equal(AgendaCloseFixture.Day, f.Actions.Through); Assert.Equal(f.Clock.Now, f.Actions.CompletedAt);
        Assert.Equal(selected with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(selected.Id));
        Assert.Equal(other, await f.ReadAsync(other.Id)); Assert.Equal(f.Clock.Now, await f.Service.GetLastCompletedAtAsync(selected.Id));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("foreign")]
    public async Task InvalidPromptSelection_StopsClosingWithoutCompleting(string scenario)
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, scenario switch { "empty" => [], "duplicate" => [task.Id, task.Id], _ => [Guid.NewGuid()] });
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Actions.CompleteCalls);
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.True(f.Library.Editor.CanEditContent);
    }

    [Fact]
    public async Task QueryFailure_KeepsTheApplicationOpenAndDoesNotInventAnEmptyDay()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); f.Actions.FailList = true;
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Prompt.Calls); Assert.Equal(0, f.Actions.CompleteCalls);
        Assert.False(string.IsNullOrWhiteSpace(f.Closing.Feedback)); Assert.True(f.Library.Editor.CanEditContent);
    }

    [Fact]
    public async Task CompletionFailure_PreservesBothDiscardedDraftsAndTheEditor()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync(); var editor = f.Library.Editor;
        f.Agenda.Title = "Rascunho da Agenda"; editor.Blocks[0].Text = "Rascunho do caderno";
        f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard); f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard);
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]); f.Actions.FailComplete = true;
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(2, f.Unsaved.Calls);
        Assert.Same(editor, f.Library.Editor); Assert.True(editor.CanEditContent); Assert.True(editor.HasUnsavedChanges);
        Assert.Equal("Rascunho do caderno", editor.Blocks[0].Text); Assert.Equal("Rascunho da Agenda", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges);
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.False(string.IsNullOrWhiteSpace(f.Closing.Feedback));
    }

    [Fact]
    public async Task CancellingTheAgendaDraft_DoesNotQueryOrCompleteTasks()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); await f.SeedAsync(); f.Agenda.Title = "Não sair";
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Actions.ListCalls); Assert.Equal(0, f.Prompt.Calls);
        Assert.Equal("Não sair", f.Agenda.Title); Assert.True(f.Library.Editor.CanEditContent);
    }

    [Fact]
    public async Task CancellingTheNotebookAfterAgendaDiscard_LeavesBothDraftsUsable()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); await f.SeedAsync();
        f.Agenda.Title = "Agenda pendente"; f.Library.Editor.Blocks[0].Text = "Caderno pendente";
        f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard); f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Cancel);
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(2, f.Unsaved.Calls); Assert.Equal(0, f.Actions.ListCalls);
        Assert.Equal("Agenda pendente", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges);
        Assert.True(f.Library.Editor.CanEditContent); Assert.Equal("Caderno pendente", f.Library.Editor.Blocks[0].Text);
    }

    [Fact]
    public async Task PendingCompletion_RejectsReentryAndKeepsTheEditorAlive()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); f.Actions.Gate = gate.Task;
        var closing = f.Closing.RequestCloseAsync();
        try
        {
            await f.Actions.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(f.Closing.IsBusy); Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(1, f.Actions.CompleteCalls);
            Assert.True(f.Library.Editor.CanEditContent); gate.SetResult(); Assert.True(await closing); Assert.False(f.Closing.IsBusy);
        }
        finally { gate.TrySetResult(); }
    }

    [Fact]
    public async Task SavingAnAgendaDraftBeforeClosing_IncludesTheNewTaskInThePrompt()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); f.Agenda.Title = "Criada antes de sair";
        f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Save); f.Prompt.Decision = new(AgendaCompletionChoice.KeepOpen, []);
        Assert.True(await f.Closing.RequestCloseAsync());
        Assert.Equal("Criada antes de sair", Assert.Single(f.Prompt.Candidates).Title); Assert.False(f.Agenda.HasUnsavedChanges);
        Assert.Equal(0, f.Actions.CompleteCalls);
    }

    [Fact]
    public async Task Timestamp_IsCapturedAfterTheCompletionDecisionInsteadOfWhenThePromptOpens()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]); f.Prompt.BeforeReturn = () => f.Clock.Now = f.Clock.Now.AddHours(6);
        Assert.True(await f.Closing.RequestCloseAsync()); Assert.Equal(f.Clock.Now, await f.Service.GetLastCompletedAtAsync(task.Id));
        Assert.Equal(AgendaCloseFixture.Day.AddDays(1), DateOnly.FromDateTime(f.Clock.Now.DateTime));
    }

    [Fact]
    public async Task TaskRescheduledWhilePrompting_CancelsTheWholeCompletionAndKeepsTheEditorAlive()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]);
        f.Prompt.BeforeReturnAsync = async () => { await f.AgendaActions.SaveAsync(task with { PlannedDate = AgendaCloseFixture.Day.AddDays(2) }); };
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.True(f.Library.Editor.CanEditContent);
        Assert.Null(await f.Service.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task FailedDraftSave_StopsBeforeQueryingTheClosingCandidates()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); f.Agenda.Title = " "; f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Save);
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Actions.ListCalls); Assert.True(f.Agenda.HasUnsavedChanges);
    }

    [Fact]
    public async Task MissingCompletionActions_AreRejected()
    {
        using var f = await AgendaCloseFixture.CreateAsync();
        Assert.Throws<ArgumentNullException>(() => new ApplicationCloseCoordinator(f.Library, f.Agenda, null!, f.Prompt, f.Clock));
    }

    [AvaloniaFact]
    public async Task Autosave_IsPausedDuringTheClosingPromptAndResumesAfterCancellation()
    {
        using var f = await AgendaCloseFixture.CreateAsync(autoSave: true); await f.SeedAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); f.Prompt.Gate = gate.Task;
        f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard); f.Library.Editor.Blocks[0].Text = "Rascunho ainda editável";
        var closing = f.Closing.RequestCloseAsync();
        try
        {
            await f.Prompt.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); await Task.Delay(2300);
            Assert.True(f.Library.Editor.HasUnsavedChanges); Assert.Equal("Original", await f.StoredTextAsync());
            gate.SetResult(); Assert.False(await closing);
            await f.UntilAsync(() => !f.Library.Editor.HasUnsavedChanges && !f.Library.Editor.IsBusy);
            Assert.Equal("Rascunho ainda editável", await f.StoredTextAsync());
        }
        finally { gate.TrySetResult(); }
    }

    [AvaloniaFact]
    public async Task ApprovedDiscard_DoesNotFlushAutosaveBeforeTheWindowActuallyCloses()
    {
        using var f = await AgendaCloseFixture.CreateAsync(autoSave: true); await f.SeedAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); f.Prompt.Gate = gate.Task;
        f.Prompt.Decision = new(AgendaCompletionChoice.KeepOpen, []);
        f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard); f.Library.Editor.Blocks[0].Text = "Descartado ao sair";
        var closing = f.Closing.RequestCloseAsync();
        try
        {
            await f.Prompt.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); await Task.Delay(2300);
            gate.SetResult(); Assert.True(await closing); await Task.Delay(150);
            await f.UntilAsync(() => !f.Library.Editor.IsBusy);
            Assert.True(f.Library.Editor.CanEditContent); Assert.True(f.Library.Editor.HasUnsavedChanges);
            Assert.Equal("Original", await f.StoredTextAsync());
        }
        finally { gate.TrySetResult(); }
    }

    [AvaloniaFact]
    public async Task NativeWindow_UsesTheCoordinatorEvenFromHomeAndRespectsCancel()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); await f.SeedAsync(); f.ShowWindow();
        Assert.True(f.Model!.IsHomeActive); f.Window!.Close();
        await f.UntilAsync(() => !f.Window.IsVisible || (f.Prompt.Calls == 1 && !f.Closing.IsBusy));
        Assert.True(f.Window.IsVisible); Assert.Equal(1, f.Prompt.Calls); Assert.True(f.Library.Editor.CanEditContent);
    }

    [AvaloniaFact]
    public async Task NativeWindow_ClosesAfterKeepOpenWithoutCompletingTheTask()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.KeepOpen, []); f.ShowWindow(); f.Window!.Close();
        await f.UntilAsync(() => !f.Window.IsVisible);
        Assert.Equal(1, f.Prompt.Calls); Assert.Equal(0, f.Actions.CompleteCalls); Assert.Equal(task, await f.ReadAsync(task.Id));
    }

    [AvaloniaFact]
    public async Task NativeCompletion_DoesNotAskForAnotherDiscardDecisionAfterWriting()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Agenda.Title = "Rascunho descartado"; f.Library.Editor.Blocks[0].Text = "Caderno descartado";
        f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard); f.Unsaved.Choices.Enqueue(UnsavedChangesChoice.Discard);
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]); f.ShowWindow(); f.Window!.Close();
        await f.UntilAsync(() => !f.Window.IsVisible || f.Unsaved.Calls > 2);
        Assert.False(f.Window.IsVisible); Assert.Equal(2, f.Unsaved.Calls); Assert.Equal(1, f.Prompt.Calls); Assert.Equal(1, f.Actions.CompleteCalls);
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(task.Id));
        Assert.Equal("Original", await f.StoredTextAsync());
    }
}

internal sealed class AgendaCloseFixture : IDisposable
{
    internal static readonly DateOnly Day = new(2026, 11, 4);
    internal NotebookTestWorkspace Root { get; } = new();
    internal ServiceProvider Services { get; private set; } = null!;
    internal IAgendaActions AgendaActions { get; private set; } = null!;
    internal IAgendaCompletionActions Service { get; private set; } = null!;
    internal ILibraryActions LibraryActions { get; private set; } = null!;
    internal LibraryViewModel Library { get; private set; } = null!;
    internal AgendaViewModel Agenda { get; private set; } = null!;
    internal ApplicationCloseCoordinator Closing { get; private set; } = null!;
    internal CloseActions Actions { get; private set; } = null!;
    internal ClosePrompt Prompt { get; } = new();
    internal DraftPrompt Unsaved { get; } = new();
    internal CloseClock Clock { get; } = new();
    internal NotebookSnapshot Page { get; private set; } = null!;
    internal MainWindow? Window { get; private set; }
    internal MainWindowViewModel? Model { get; private set; }

    internal static async Task<AgendaCloseFixture> CreateAsync(bool autoSave = false)
    {
        var f = new AgendaCloseFixture();
        try
        {
            f.Services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath, autoSave, unsavedChangesPrompt: f.Unsaved);
            f.AgendaActions = f.Services.GetRequiredService<IAgendaActions>(); f.Service = f.Services.GetRequiredService<IAgendaCompletionActions>();
            f.LibraryActions = f.Services.GetRequiredService<ILibraryActions>();
            var space = await f.LibraryActions.CreateSpaceAsync("Estudos"); var book = await f.LibraryActions.CreateNotebookAsync(space.Id, "Caderno fictício");
            f.Page = new(Guid.NewGuid(), "Página", [new NotebookTextBlock(Guid.NewGuid(), "Original")]);
            Assert.Equal(NotebookSaveStatus.Saved, (await f.LibraryActions.SaveInNotebookAsync(f.Page, book.Id)).Status);
            f.Library = f.Services.GetRequiredService<LibraryViewModel>(); await f.Library.InitializeAsync();
            Assert.True(await f.Library.SelectSpaceAsync(space.Id)); Assert.True(await f.Library.SelectNotebookAsync(book.Id)); Assert.True(await f.Library.OpenPageAsync(f.Page.Id));
            f.Agenda = new(f.AgendaActions, f.Unsaved); await f.Agenda.InitializeAsync(Day);
            f.Actions = new(f.Service); f.Closing = new(f.Library, f.Agenda, f.Actions, f.Prompt, f.Clock); return f;
        }
        catch { f.Dispose(); throw; }
    }
    internal async Task<AgendaTaskSnapshot> SeedAsync(DateOnly? day = null)
    {
        var task = new AgendaTaskSnapshot(Guid.NewGuid(), "Tarefa fictícia", "Conteúdo de teste", day ?? Day);
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await AgendaActions.SaveAsync(task)).Status); return task;
    }
    internal Task<AgendaTaskSnapshot?> ReadAsync(Guid id) => AgendaActions.GetByIdAsync(id);
    internal async Task<string> StoredTextAsync() => Assert.IsType<NotebookTextBlock>(Assert.Single((await LibraryActions.LoadAsync(Page.Id))!.Blocks)).Text;
    internal void ShowWindow()
    {
        Model = new() { Library = Library, Agenda = Agenda, Closing = Closing, Backup = Services.GetRequiredService<BackupViewModel>() };
        Window = new(Model) { Width = 1200, Height = 850 }; Window.Show(); Dispatcher.UIThread.RunJobs();
    }
    internal async Task UntilAsync(Func<bool> ready)
    {
        var timer = Stopwatch.StartNew();
        while (!ready()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15), "O fluxo de fechamento não terminou."); Dispatcher.UIThread.RunJobs(); await Task.Delay(10); }
        Dispatcher.UIThread.RunJobs();
    }
    public void Dispose()
    {
        Unsaved.Choices.Clear(); Unsaved.Default = UnsavedChangesChoice.Discard;
        if (Window is not null) { Model!.Closing = null; Window.Close(); Dispatcher.UIThread.RunJobs(); }
        Library?.Dispose(); Closing?.Dispose(); Services?.Dispose(); Root.Dispose();
    }
    internal sealed class CloseClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 11, 4, 22, 30, 0, TimeSpan.FromHours(-3));
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("ClosingTest", Now.Offset, "ClosingTest", "ClosingTest");
    }
    internal sealed class DraftPrompt : IUnsavedChangesPrompt
    {
        internal Queue<UnsavedChangesChoice> Choices { get; } = new();
        internal UnsavedChangesChoice Default { get; set; } = UnsavedChangesChoice.Cancel;
        internal int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(Choices.Count > 0 ? Choices.Dequeue() : Default); }
    }
    internal sealed class ClosePrompt : IAgendaCompletionPrompt
    {
        internal AgendaCompletionDecision Decision { get; set; } = new(AgendaCompletionChoice.Cancel, []);
        internal IReadOnlyList<AgendaTaskSnapshot> Candidates { get; private set; } = [];
        internal int Calls { get; private set; }
        internal Task? Gate { get; set; }
        internal Action? BeforeReturn { get; set; }
        internal Func<Task>? BeforeReturnAsync { get; set; }
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<AgendaCompletionDecision> ChooseAsync(IReadOnlyList<AgendaTaskSnapshot> candidates)
        {
            Calls++; Candidates = candidates; Entered.TrySetResult(); if (Gate is { } gate) await gate;
            BeforeReturn?.Invoke(); if (BeforeReturnAsync is { } action) await action(); return Decision;
        }
    }
    internal sealed class CloseActions(IAgendaCompletionActions inner) : IAgendaCompletionActions
    {
        internal bool FailList { get; set; }
        internal bool FailComplete { get; set; }
        internal int ListCalls { get; private set; }
        internal int CompleteCalls { get; private set; }
        internal DateOnly Through { get; private set; }
        internal DateTimeOffset CompletedAt { get; private set; }
        internal Task? Gate { get; set; }
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<AgendaTaskSnapshot>> ListCandidatesAsync(DateOnly through, CancellationToken cancellationToken = default)
        {
            ListCalls++; Through = through; if (FailList) throw new AgendaPersistenceException("Synthetic query failure.");
            return inner.ListCandidatesAsync(through, cancellationToken);
        }
        public async Task<AgendaTaskCompletionResult> CompleteAsync(IReadOnlyList<AgendaTaskSnapshot> tasks, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
        {
            CompleteCalls++; CompletedAt = completedAt; Entered.TrySetResult(); if (Gate is { } gate) await gate.WaitAsync(cancellationToken);
            return FailComplete ? new(AgendaTaskCompletionStatus.PersistenceFailed) : await inner.CompleteAsync(tasks, completedAt, cancellationToken);
        }
        public Task<DateTimeOffset?> GetLastCompletedAtAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetLastCompletedAtAsync(id, cancellationToken);
    }
}
