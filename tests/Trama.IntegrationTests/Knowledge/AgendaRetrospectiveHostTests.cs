using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveHostTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);

    [Fact]
    public async Task LegacyAgenda_RemainsDailyWithoutRetrospectiveDependencies()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var vm = new AgendaViewModel(services.GetRequiredService<IAgendaActions>(), new Prompt()); await vm.InitializeAsync(Monday);
        Assert.False(Get<bool>(vm, "HasRetrospectiveView")); Assert.False(Get<bool>(vm, "IsRetrospectiveView"));
        Assert.False(await Show(vm)); Assert.True(vm.CanEdit); Assert.Empty(vm.Tasks);
    }

    [Fact]
    public async Task Composition_RegistersComparerLoaderAndAgendaIntegration()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        Assert.NotNull(services.GetService<IAgendaRetrospectiveComparer>()); Assert.NotNull(services.GetService<AgendaRetrospectiveViewModel>());
        var vm = services.GetRequiredService<AgendaViewModel>(); Assert.True(Get<bool>(vm, "HasRetrospectiveView"));
        Assert.NotNull(Get<AgendaRetrospectiveViewModel?>(vm, "Retrospective")); Assert.False(Get<bool>(vm, "IsRetrospectiveView"));
    }

    [Fact]
    public async Task Initialization_DoesNotCreateWeeklyClosures()
    {
        using var root = new NotebookTestWorkspace(); using var services = await DesktopComposition.CreateAsync(root.DirectoryPath);
        var vm = services.GetRequiredService<AgendaViewModel>(); await vm.InitializeAsync(Monday);
        Assert.True(Get<bool>(vm, "HasRetrospectiveView")); Assert.False(Get<bool>(vm, "IsRetrospectiveView"));
        Assert.Empty(await services.GetRequiredService<IAgendaRetrospectiveActions>().ListAsync(DateOnly.MinValue, DateOnly.MaxValue));
    }

    [Fact]
    public async Task OpenFromWeek_PreservesCachedWeekAndDailySelection()
    {
        using var f = await Fixture.OpenAsync(); Assert.True(await f.Agenda.ShowWeekAsync(Monday)); var week = f.Agenda.Week; var daily = f.Agenda.Tasks;
        Assert.True(await Show(f.Agenda)); Assert.True(Get<bool>(f.Agenda, "IsRetrospectiveView")); Assert.False(f.Agenda.IsWeekView);
        Assert.Same(week, f.Agenda.Week); Assert.Same(daily, f.Agenda.Tasks); Assert.Equal(Monday, f.Agenda.SelectedDay);
        Assert.NotNull(f.Loader.Report); Assert.Equal(1, f.Loader.Report.PlannedCount);
    }

    [Fact]
    public async Task OpeningAndLeaving_PreserveAgendaDraftWithoutPromptOrSave()
    {
        using var f = await Fixture.OpenAsync(); var before = await f.Services.GetRequiredService<IAgendaActions>().ListAsync(Monday, Monday);
        f.Agenda.Title = "Rascunho independente"; f.Agenda.Description = "Ainda escrevendo"; f.Agenda.StartTimeText = "09:";
        Assert.True(await Show(f.Agenda)); Assert.True(await f.Agenda.ShowDayAsync()); Assert.True(await f.Agenda.ShowWeekAsync(Monday));
        Assert.Equal("Rascunho independente", f.Agenda.Title); Assert.Equal("Ainda escrevendo", f.Agenda.Description); Assert.Equal("09:", f.Agenda.StartTimeText);
        Assert.True(f.Agenda.HasUnsavedChanges); Assert.Equal(0, f.Prompt.Calls);
        Assert.Equal(before, await f.Services.GetRequiredService<IAgendaActions>().ListAsync(Monday, Monday));
    }

    [Fact]
    public async Task ModuleNavigation_PreservesNotebookEditorAndBothDrafts()
    {
        using var f = await Fixture.OpenAsync(); var library = f.Services.GetRequiredService<LibraryViewModel>(); await library.InitializeAsync();
        var editor = library.Editor; editor.Title = "Anotação não salva"; f.Agenda.Title = "Tarefa não salva";
        var main = new MainWindowViewModel { Library = library, Agenda = f.Agenda }; main.ShowAgendaCommand.Execute(null);
        Assert.True(await Show(f.Agenda)); main.ShowNotebooksCommand.Execute(null); main.ShowAgendaCommand.Execute(null);
        Assert.Same(editor, library.Editor); Assert.Equal("Anotação não salva", editor.Title); Assert.True(editor.HasUnsavedChanges);
        Assert.Equal("Tarefa não salva", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges); Assert.Equal(0, f.Prompt.Calls);
        Assert.True(Get<bool>(f.Agenda, "IsRetrospectiveView"));
    }

    [Fact]
    public async Task ReturnToDay_LeavesRetrospectiveWithoutClearingLoadedReport()
    {
        using var f = await Fixture.OpenAsync(); Assert.True(await Show(f.Agenda)); var report = f.Loader.Report;
        Assert.True(await f.Agenda.ShowDayAsync()); Assert.False(Get<bool>(f.Agenda, "IsRetrospectiveView")); Assert.False(f.Agenda.IsWeekView); Assert.Same(report, f.Loader.Report);
    }

    [Fact]
    public async Task ReturnToWeek_UsesWeeklyModeAndKeepsRetrospectiveResult()
    {
        using var f = await Fixture.OpenAsync(); Assert.True(await Show(f.Agenda)); var report = f.Loader.Report;
        Assert.True(await f.Agenda.ShowWeekAsync(Monday)); Assert.False(Get<bool>(f.Agenda, "IsRetrospectiveView")); Assert.True(f.Agenda.IsWeekView); Assert.Same(report, f.Loader.Report);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOpening_KeepsPreviousDailyOrWeeklyMode(bool fromWeek)
    {
        using var f = await Fixture.OpenAsync(); if (fromWeek) Assert.True(await f.Agenda.ShowWeekAsync(Monday)); f.Actions.Fail = true;
        Assert.False(await Show(f.Agenda)); Assert.False(Get<bool>(f.Agenda, "IsRetrospectiveView")); Assert.Equal(fromWeek, f.Agenda.IsWeekView);
        Assert.True(f.Loader.HasError); Assert.False(f.Agenda.IsBusy); Assert.True(f.Agenda.CanEdit); Assert.NotEmpty(f.Agenda.Feedback);
    }

    [Fact]
    public async Task FailedRetrospectiveNavigation_KeepsVisibleReport()
    {
        using var f = await Fixture.OpenAsync(); Assert.True(await Show(f.Agenda)); var report = f.Loader.Report; f.Actions.Fail = true;
        Assert.False(await Show(f.Agenda, Monday.AddDays(-7))); Assert.True(Get<bool>(f.Agenda, "IsRetrospectiveView")); Assert.Same(report, f.Loader.Report);
    }

    [Fact]
    public async Task BusyLoad_DisablesModuleNavigationUntilCompletion()
    {
        using var f = await Fixture.OpenAsync(); var library = f.Services.GetRequiredService<LibraryViewModel>(); await library.InitializeAsync();
        var main = new MainWindowViewModel { Library = library, Agenda = f.Agenda }; main.ShowAgendaCommand.Execute(null);
        f.Actions.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); var pending = Show(f.Agenda);
        try
        {
            Assert.True(f.Agenda.IsBusy); Assert.False(f.Agenda.CanEdit); Assert.False(main.CanNavigateModules);
            Assert.False(main.ShowNotebooksCommand.CanExecute(null));
        }
        finally { f.Actions.Gate.TrySetResult(); }
        Assert.True(await pending); Assert.True(main.CanNavigateModules); Assert.True(f.Agenda.CanEdit);
    }

    [Fact]
    public async Task BusyLoad_BlocksApplicationCloseBeforePrompting()
    {
        using var f = await Fixture.OpenAsync(); var library = f.Services.GetRequiredService<LibraryViewModel>(); await library.InitializeAsync();
        var prompt = new CompletionPrompt(); using var closing = new ApplicationCloseCoordinator(library, f.Agenda, f.Services.GetRequiredService<IAgendaCompletionActions>(), prompt, f.Clock);
        f.Actions.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); var pending = Show(f.Agenda);
        try { Assert.False(await closing.RequestCloseAsync()); Assert.Equal(0, prompt.Calls); Assert.Equal(0, f.Prompt.Calls); }
        finally { f.Actions.Gate.TrySetResult(); }
        Assert.True(await pending);
    }

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("new")]
    [InlineData("save")]
    public async Task BusyLoad_RejectsConflictingAgendaActions(string operation)
    {
        using var f = await Fixture.OpenAsync(); f.Agenda.Title = "Não gravar durante consulta";
        f.Actions.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); var pending = Show(f.Agenda);
        try
        {
            var result = operation switch { "day" => await f.Agenda.ShowDayAsync(), "week" => await f.Agenda.ShowWeekAsync(Monday), "new" => await f.Agenda.NewTaskAsync(), _ => await f.Agenda.SaveAsync() };
            Assert.False(result); Assert.Equal("Não gravar durante consulta", f.Agenda.Title);
        }
        finally { f.Actions.Gate.TrySetResult(); }
        Assert.True(await pending);
    }

    [Fact]
    public async Task Cancellation_IsForwardedAndDoesNotEnterThePanel()
    {
        using var f = await Fixture.OpenAsync(); f.Actions.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); using var cts = new CancellationTokenSource();
        var pending = Show(f.Agenda, token: cts.Token); cts.Cancel(); Assert.False(await pending);
        Assert.Equal(cts.Token, f.Actions.Token); Assert.False(f.Agenda.IsBusy); Assert.False(Get<bool>(f.Agenda, "IsRetrospectiveView")); Assert.False(f.Loader.HasError);
    }

    [Fact]
    public async Task RefreshThroughAgenda_UsesLoaderAndPreservesDraft()
    {
        using var f = await Fixture.OpenAsync(); Assert.True(await Show(f.Agenda)); f.Agenda.Title = "Continuar depois"; var calls = f.Actions.Calls;
        Assert.True(await Invoke(f.Agenda, "RefreshRetrospectiveAsync", CancellationToken.None));
        Assert.Equal(calls + 1, f.Actions.Calls); Assert.Equal("Continuar depois", f.Agenda.Title); Assert.True(Get<bool>(f.Agenda, "IsRetrospectiveView"));
    }

    [Fact]
    public async Task SavedClosure_RemainsIndependentOfSubsequentTaskEdits()
    {
        using var f = await Fixture.OpenAsync(); Assert.True(await Show(f.Agenda)); var captured = f.Loader.Report;
        var actions = f.Services.GetRequiredService<IAgendaActions>(); var task = Assert.Single(await actions.ListAsync(Monday, Monday));
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions.SaveAsync(task with { Title = "Editada depois" })).Status);
        Assert.True(await Invoke(f.Agenda, "RefreshRetrospectiveAsync", CancellationToken.None));
        Assert.Equal(captured!.ClosedAt, f.Loader.Report!.ClosedAt); Assert.Equal(captured.Tasks, f.Loader.Report.Tasks);
        Assert.Equal("Planejamento fictício", Assert.Single(f.Loader.Report.Tasks).Task.Title);
    }

    private static Task<bool> Show(AgendaViewModel vm, DateOnly? date = null, CancellationToken token = default) => Invoke(vm, "ShowRetrospectiveAsync", date ?? Monday, token);
    private static async Task<bool> Invoke(object vm, string name, params object[] args)
    {
        var method = vm.GetType().GetMethod(name); Assert.NotNull(method); return await Assert.IsAssignableFrom<Task<bool>>(method.Invoke(vm, args));
    }
    private static T Get<T>(object vm, string name)
    {
        var property = vm.GetType().GetProperty(name); Assert.NotNull(property); return (T)property.GetValue(vm)!;
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 9, 15, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(UnsavedChangesChoice.Cancel); }
    }
    private sealed class CompletionPrompt : IAgendaCompletionPrompt
    {
        public int Calls { get; private set; }
        public Task<AgendaCompletionDecision> ChooseAsync(IReadOnlyList<AgendaTaskSnapshot> candidates) { Calls++; return Task.FromResult(new AgendaCompletionDecision(AgendaCompletionChoice.Cancel, [])); }
    }
    private sealed class Actions(IAgendaRetrospectiveActions inner) : IAgendaRetrospectiveActions
    {
        public bool Fail { get; set; }
        public TaskCompletionSource? Gate { get; set; }
        public int Calls { get; private set; }
        public CancellationToken Token { get; private set; }
        public async Task<AgendaWeekRetrospective> GetOrCloseAsync(DateOnly referenceDate, CancellationToken cancellationToken = default)
        {
            Calls++; Token = cancellationToken; if (Gate is { } gate) await gate.Task.WaitAsync(cancellationToken);
            if (Fail) throw new InvalidOperationException("Synthetic closure error."); return await inner.GetOrCloseAsync(referenceDate, cancellationToken);
        }
        public Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default) => inner.GetAsync(weekStart, cancellationToken);
        public Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default) => inner.ListAsync(from, through, cancellationToken);
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public ServiceProvider Services { get; private set; } = null!;
        public Prompt Prompt { get; } = new();
        public Clock Clock { get; } = new();
        public Actions Actions { get; private set; } = null!;
        public AgendaRetrospectiveViewModel Loader { get; private set; } = null!;
        public AgendaViewModel Agenda { get; private set; } = null!;
        public static async Task<Fixture> OpenAsync()
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath, unsavedChangesPrompt: f.Prompt);
                f.Actions = new(f.Services.GetRequiredService<IAgendaRetrospectiveActions>()); f.Loader = new(f.Actions, new AgendaRetrospectiveComparer(), f.Clock);
                var signature = new[] { typeof(IAgendaActions), typeof(IUnsavedChangesPrompt), typeof(IAgendaTrashActions), typeof(TimeProvider), typeof(IAgendaPeriodReader), typeof(IAgendaWeekProjector), typeof(AgendaRetrospectiveViewModel) };
                var ctor = typeof(AgendaViewModel).GetConstructor(signature); Assert.NotNull(ctor);
                f.Agenda = Assert.IsType<AgendaViewModel>(ctor.Invoke([f.Services.GetRequiredService<IAgendaActions>(), f.Prompt, f.Services.GetRequiredService<IAgendaTrashActions>(), f.Clock,
                    f.Services.GetRequiredService<IAgendaPeriodReader>(), f.Services.GetRequiredService<IAgendaWeekProjector>(), f.Loader]));
                Assert.Equal(AgendaTaskSaveStatus.Saved, (await f.Services.GetRequiredService<IAgendaActions>().SaveAsync(new(Guid.NewGuid(), "Planejamento fictício", "Perfil de teste", Monday))).Status);
                await f.Agenda.InitializeAsync(Monday); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public void Dispose() { Services?.Dispose(); Root.Dispose(); }
    }
}
