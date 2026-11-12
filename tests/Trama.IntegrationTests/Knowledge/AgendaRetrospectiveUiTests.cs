using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveUiTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("RetrospectiveUi", TimeSpan.FromHours(-3), "Test", "Test");

    [AvaloniaFact]
    public async Task ModeButton_OpensLastClosedWeekFromDailyView()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync();
        Assert.Equal(Monday, Assert.Single(f.Actions.Opened)); Assert.True(f.C<Control>("AgendaRetrospectivePanel").IsEffectivelyVisible);
        Assert.Contains("active", f.B("AgendaRetrospectiveModeButton").Classes); Assert.DoesNotContain("active", f.B("AgendaDayModeButton").Classes);
        Assert.False(f.Vm.IsWeekView); Assert.True(f.Vm.IsRetrospectiveView);
    }

    [AvaloniaFact]
    public async Task ModeButton_UsesTheClosedWeekBeingBrowsed()
    {
        using var f = await Fixture.CreateAsync(); Assert.True(await f.Vm.ShowWeekAsync(Monday.AddDays(-7))); Pump(); await f.OpenAsync();
        Assert.Equal(Monday.AddDays(-7), Assert.Single(f.Actions.Opened)); Assert.Equal(Monday.AddDays(-7), f.Loader.Report!.StartDate);
    }

    [AvaloniaFact]
    public async Task Panel_HidesDailyAndWeeklyControlsAndKeepsTheSameFormDraft()
    {
        using var f = await Fixture.CreateAsync(); var title = f.C<TextBox>("AgendaTitleInput"); title.Text = "Rascunho ainda incompleto";
        f.C<TextBox>("AgendaStartTimeInput").Text = "09:"; await f.OpenAsync();
        Assert.False(f.C<Control>("AgendaFormPanel").IsEffectivelyVisible); Assert.False(f.C<Control>("AgendaTaskList").IsEffectivelyVisible);
        Assert.False(f.C<Control>("AgendaWeekDays").IsEffectivelyVisible); Assert.False(f.C<Control>("AgendaDayPicker").IsEffectivelyVisible);
        Assert.False(f.B("AgendaDeleteTaskButton").IsEffectivelyVisible); Assert.False(f.B("AgendaNewTaskButton").IsEffectivelyVisible);
        Assert.Same(title, f.C<TextBox>("AgendaTitleInput")); Assert.Equal("Rascunho ainda incompleto", f.Vm.Title); Assert.Equal("09:", f.Vm.StartTimeText);
    }

    [AvaloniaFact]
    public async Task FourIndicators_ShowCapturedCountsAndPercentRatherThanLiveDailyData()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync();
        Assert.Equal("4", f.C<TextBlock>("AgendaRetrospectivePlannedCount").Text); Assert.Equal("2", f.C<TextBlock>("AgendaRetrospectiveCompletedCount").Text);
        Assert.Equal("50%", f.C<TextBlock>("AgendaRetrospectiveCompletion").Text); Assert.Equal("2", f.C<TextBlock>("AgendaRetrospectiveOpenCount").Text);
        Assert.Contains("1 pendentes", f.C<TextBlock>("AgendaRetrospectiveOpenBreakdown").Text!);
        Assert.Contains("1 em andamento", f.C<TextBlock>("AgendaRetrospectiveOpenBreakdown").Text!); Assert.Empty(f.Vm.Tasks);
    }

    [AvaloniaFact]
    public async Task Comparison_ShowsSignedPercentagePoints()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); Assert.Contains("+25 p.p.", f.C<TextBlock>("AgendaRetrospectiveComparison").Text!);
    }

    [AvaloniaFact]
    public async Task DailyChart_HasSevenOrderedDaysAndBothCapturedSeries()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); var rows = f.Rows<AgendaRetrospectiveDay>("retrospective-day");
        Assert.Equal(f.Loader.Report!.Days, rows.Select(c => (AgendaRetrospectiveDay)c.DataContext!));
        Assert.Equal(7, rows.Count);
        foreach (var row in rows)
        {
            var day = (AgendaRetrospectiveDay)row.DataContext!;
            Assert.Equal((double)day.PlannedCount, Assert.Single(row.GetVisualDescendants().OfType<ProgressBar>(), b => b.Classes.Contains("retrospective-planned-bar")).Value);
            Assert.Equal((double)day.CompletedCount, Assert.Single(row.GetVisualDescendants().OfType<ProgressBar>(), b => b.Classes.Contains("retrospective-completed-bar")).Value);
        }
        Assert.Contains("Planejadas", Text(f.Panel)); Assert.Contains("Concluídas", Text(f.Panel));
    }

    [AvaloniaFact]
    public async Task CategoryRows_ShowAllCapturedCategoriesAndCounts()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); var rows = f.Rows<AgendaRetrospectiveCategory>("retrospective-category");
        Assert.Equal(3, rows.Count); Assert.Equal(f.Loader.Report!.Categories, rows.Select(c => (AgendaRetrospectiveCategory)c.DataContext!));
        foreach (var row in rows)
        {
            var category = (AgendaRetrospectiveCategory)row.DataContext!; Assert.Contains($"{category.CompletedCount} de {category.PlannedCount}", Text(row));
        }
        Assert.Contains("Trabalho", Text(f.Panel)); Assert.Contains("Estudo", Text(f.Panel)); Assert.Contains("Pessoal", Text(f.Panel));
    }

    [AvaloniaFact]
    public async Task OpenTasks_ShowCapturedTitlesWithDistinctStatusSymbolsAndLabels()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); var rows = f.Rows<AgendaRetrospectiveTask>("retrospective-open-task");
        Assert.Equal(2, rows.Count); Assert.Contains(rows, c => Text(c).Contains("○") && Text(c).Contains("Pendente"));
        Assert.Contains(rows, c => Text(c).Contains("◐") && Text(c).Contains("Em andamento"));
        Assert.All(rows, row => Assert.Contains(((AgendaRetrospectiveTask)row.DataContext!).Task.Title, Text(row)));
    }

    [AvaloniaFact]
    public async Task DataWarnings_ExposeUndatedOutsideAndCarriedTasks()
    {
        using var f = await Fixture.CreateAsync(); f.Actions.Reports[Monday] = Report(Monday, quality: true); await f.OpenAsync();
        var warning = f.C<Control>("AgendaRetrospectiveWarnings"); Assert.True(warning.IsEffectivelyVisible);
        Assert.Contains("sem data", Text(warning)); Assert.Contains("fora", Text(warning)); Assert.Contains("antes", Text(warning));
    }

    [AvaloniaFact]
    public async Task EmptyClosure_ShowsNoTasksAndNoFakePercentageOrInvalidBars()
    {
        using var f = await Fixture.CreateAsync(); f.Actions.Reports[Monday] = Report(Monday, empty: true); await f.OpenAsync();
        Assert.Equal("Sem tarefas", f.C<TextBlock>("AgendaRetrospectiveCompletion").Text); Assert.Equal("0", f.C<TextBlock>("AgendaRetrospectivePlannedCount").Text);
        Assert.True(f.C<Control>("AgendaRetrospectiveEmpty").IsEffectivelyVisible); Assert.Empty(f.Rows<AgendaRetrospectiveTask>("retrospective-open-task"));
        Assert.All(f.Panel.GetVisualDescendants().OfType<ProgressBar>(), bar => { Assert.True(double.IsFinite(bar.Value)); Assert.True(bar.Maximum > 0); Assert.Equal(0, bar.Value); });
    }

    [AvaloniaFact]
    public async Task MissingPreviousWeek_IsExplainedWithoutCreatingIt()
    {
        using var f = await Fixture.CreateAsync(); f.Actions.Reports.Remove(Monday.AddDays(-7)); await f.OpenAsync();
        Assert.Contains("Sem fechamento da semana anterior", f.C<TextBlock>("AgendaRetrospectiveComparison").Text!); Assert.Single(f.Actions.Opened);
    }

    [AvaloniaFact]
    public async Task Header_ShowsCapturedRangeAndPreservationInformation()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); var heading = f.C<TextBlock>("AgendaRetrospectiveHeading").Text!;
        Assert.Contains("02/11/2026", heading); Assert.Contains("08/11/2026", heading);
        Assert.Contains("preservado", Text(f.Panel), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("09/11/2026", f.C<TextBlock>("AgendaRetrospectiveCapturedAt").Text!);
    }

    [AvaloniaTheory]
    [InlineData(-7, "AgendaRetrospectivePreviousButton")]
    [InlineData(7, "AgendaRetrospectiveNextButton")]
    public async Task NavigationButtons_OpenOnlyTheAdjacentClosedWeek(int offset, string name)
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync();
        if (offset > 0) { f.Click("AgendaRetrospectivePreviousButton"); await Until(() => !f.Vm.IsBusy && f.Loader.Report!.StartDate == Monday.AddDays(-7)); }
        var start = f.Loader.Report!.StartDate; f.Click(name); await Until(() => !f.Vm.IsBusy && f.Loader.Report!.StartDate == start.AddDays(offset));
        Assert.Equal(start.AddDays(offset), f.Actions.Opened[^1]);
    }

    [AvaloniaFact]
    public async Task NextButton_CannotCloseTheCurrentWeek()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); Assert.False(f.B("AgendaRetrospectiveNextButton").IsEffectivelyEnabled);
        Assert.Equal(Monday, Assert.Single(f.Actions.Opened));
    }

    [AvaloniaFact]
    public async Task FirstRepresentableWeek_DisablesPreviousNavigation()
    {
        using var f = await Fixture.CreateAsync(); f.Actions.Reports[DateOnly.MinValue] = Report(DateOnly.MinValue, empty: true);
        Assert.True(await f.Vm.ShowRetrospectiveAsync(DateOnly.MinValue)); Pump();
        Assert.False(f.B("AgendaRetrospectivePreviousButton").IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task RefreshButton_UsesAgendaWrapperAndKeepsDraft()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); f.Vm.Title = "Retomar depois"; var calls = f.Actions.Opened.Count;
        f.Click("AgendaRetrospectiveRefreshButton"); await Until(() => !f.Vm.IsBusy && f.Actions.Opened.Count == calls + 1);
        Assert.Equal("Retomar depois", f.Vm.Title); Assert.Equal(Monday, f.Actions.Opened[^1]);
    }

    [AvaloniaFact]
    public async Task LoadingIndicator_IsVisibleAndDisablesModeAndNavigationActions()
    {
        using var f = await Fixture.CreateAsync(); f.Actions.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            f.Click("AgendaRetrospectiveModeButton"); Assert.True(f.Vm.IsBusy); Assert.True(f.C<Control>("AgendaRetrospectiveLoading").IsEffectivelyVisible);
            Assert.False(f.B("AgendaDayModeButton").IsEffectivelyEnabled); Assert.False(f.B("AgendaWeekModeButton").IsEffectivelyEnabled);
            Assert.False(f.B("AgendaRetrospectiveModeButton").IsEffectivelyEnabled);
        }
        finally { f.Actions.Gate.TrySetResult(); }
        await Until(() => !f.Vm.IsBusy); Assert.False(f.C<Control>("AgendaRetrospectiveLoading").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task FirstLoadError_ShowsFeedbackAndModeButtonAllowsRetry()
    {
        using var f = await Fixture.CreateAsync(); f.Actions.Fail = true; f.Click("AgendaRetrospectiveModeButton"); await Until(() => !f.Vm.IsBusy);
        var error = f.C<Control>("AgendaRetrospectiveError"); Assert.True(error.IsEffectivelyVisible); Assert.NotEmpty(Text(error));
        Assert.DoesNotContain("private-test-path", Text(error)); Assert.False(f.Vm.IsRetrospectiveView);
        f.Actions.Fail = false; await f.OpenAsync(); Assert.False(error.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task FailedNavigation_RetainsCardsAndCapturedRangeWithErrorVisible()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); var heading = f.C<TextBlock>("AgendaRetrospectiveHeading").Text;
        f.Actions.Fail = true; f.Click("AgendaRetrospectivePreviousButton"); await Until(() => !f.Vm.IsBusy);
        Assert.Equal(heading, f.C<TextBlock>("AgendaRetrospectiveHeading").Text); Assert.Equal("4", f.C<TextBlock>("AgendaRetrospectivePlannedCount").Text);
        Assert.True(f.C<Control>("AgendaRetrospectiveError").IsEffectivelyVisible); Assert.True(f.Vm.IsRetrospectiveView);
    }

    [AvaloniaTheory]
    [InlineData("AgendaDayModeButton", false)]
    [InlineData("AgendaWeekModeButton", true)]
    public async Task ReturnToEditor_RestoresTheSameNativeInput(string button, bool weekly)
    {
        using var f = await Fixture.CreateAsync(); var editor = f.C<TextBox>("AgendaTitleInput"); editor.Text = "Não perder"; await f.OpenAsync();
        f.Click(button); await Until(() => !f.Vm.IsBusy && !f.Vm.IsRetrospectiveView); Assert.Equal(weekly, f.Vm.IsWeekView);
        Assert.Same(editor, f.C<TextBox>("AgendaTitleInput")); Assert.Equal("Não perder", editor.Text); Assert.True(f.C<Control>("AgendaFormPanel").IsEffectivelyVisible);
        Assert.False(f.Panel.IsEffectivelyVisible); Assert.Empty(f.Window.OwnedWindows);
    }

    [AvaloniaTheory]
    [InlineData(760)]
    [InlineData(980)]
    [InlineData(1600)]
    public async Task ResponsivePanel_FitsTheViewportAndKeepsEverySectionScrollable(int width)
    {
        using var f = await Fixture.CreateAsync(); f.Window.Width = width; f.Window.Height = 700; await f.OpenAsync(); f.Window.UpdateLayout(); Pump();
        var scroll = f.C<ScrollViewer>("AgendaRetrospectiveScroll"); Assert.True(scroll.IsEffectivelyVisible); Assert.True(scroll.Viewport.Width > 0);
        Assert.True(scroll.Extent.Width <= scroll.Viewport.Width + 1, "O painel não deve exigir rolagem horizontal.");
        foreach (var name in new[] { "AgendaRetrospectiveIndicators", "AgendaRetrospectiveDays", "AgendaRetrospectiveCategories", "AgendaRetrospectiveOpenTasks" })
        {
            var section = f.C<Control>(name); Assert.True(section.IsEffectivelyVisible); Assert.True(section.Bounds.Width <= scroll.Viewport.Width + 1);
        }
        scroll.Offset = new Vector(0, scroll.Extent.Height); f.Window.UpdateLayout(); Pump();
        if (scroll.Extent.Height > scroll.Viewport.Height + 1) Assert.True(scroll.Offset.Y > 0);
        var last = f.C<Control>("AgendaRetrospectiveOpenTasks"); var bottom = last.TranslatePoint(new Point(0, last.Bounds.Height), scroll);
        Assert.NotNull(bottom); Assert.True(bottom.Value.Y <= scroll.Viewport.Height + 2, "A última seção deve ser alcançável pela rolagem.");
    }

    [AvaloniaFact]
    public async Task Actions_HaveAccessibleNamesAndKeyboardFocus()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync();
        foreach (var name in new[] { "AgendaRetrospectiveModeButton", "AgendaRetrospectivePreviousButton", "AgendaRetrospectiveNextButton", "AgendaRetrospectiveRefreshButton" })
        {
            var button = f.B(name); Assert.True(button.Focusable); Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
        }
        Assert.True(f.B("AgendaRetrospectiveRefreshButton").Focus());
    }

    [AvaloniaFact]
    public async Task ChartPalette_RespondsToSemanticResourceChanges()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenAsync(); var bar = Assert.Single(f.Rows<AgendaRetrospectiveDay>("retrospective-day")[0].GetVisualDescendants().OfType<ProgressBar>(), b => b.Classes.Contains("retrospective-completed-bar"));
        var original = Avalonia.Application.Current!.Resources["TramaColorAccent"]; var color = Color.Parse("#D06FBA");
        try { Avalonia.Application.Current.Resources["TramaColorAccent"] = color; Pump(); Assert.Equal(color, Assert.IsAssignableFrom<ISolidColorBrush>(bar.Foreground).Color); }
        finally { Avalonia.Application.Current.Resources["TramaColorAccent"] = original; Pump(); }
    }

    [AvaloniaFact]
    public async Task LegacyAgenda_DoesNotOfferRetrospectiveAndKeepsDailyFormWorking()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var button = f.View.GetVisualDescendants().OfType<Button>().SingleOrDefault(c => c.Name == "AgendaRetrospectiveModeButton");
        Assert.True(button is null || !button.IsEffectivelyVisible || !button.IsEffectivelyEnabled);
        f.C<TextBox>("AgendaTitleInput").Text = "Compatível"; Assert.True(await f.Vm.SaveAsync());
    }

    private static AgendaWeekRetrospective Report(DateOnly start, bool empty = false, bool previous = false, bool quality = false)
    {
        var tasks = new List<AgendaRetrospectiveTask>();
        if (!empty)
        {
            for (var i = 0; i < 4; i++)
            {
                var completed = i < (previous ? 1 : 2); var status = completed ? AgendaTaskStatus.Completed : i == 2 ? AgendaTaskStatus.InProgress : AgendaTaskStatus.Pending;
                var task = new AgendaTaskSnapshot(Guid.NewGuid(), $"Tarefa capturada {i}", "Fictícia", quality && i == 3 ? start.AddDays(-1) : start, Category: (AgendaTaskCategory)(i % 3), Status: quality && i == 2 ? AgendaTaskStatus.Completed : status)
                { PlannedEndDate = quality && i == 3 ? start.AddDays(1) : null };
                DateTimeOffset? actual = completed ? new(start.AddDays(quality && i == 1 ? 7 : 1).ToDateTime(new TimeOnly(10, 0)), Zone.BaseUtcOffset) : null;
                tasks.Add(new(task, actual));
            }
        }
        return new AgendaRetrospectiveBuilder().Build(start, tasks, new(start.AddDays(7).ToDateTime(new TimeOnly(12, 0)), Zone.BaseUtcOffset), Zone);
    }
    private static string Text(Control root) => string.Join("\n", root.GetVisualDescendants().OfType<TextBlock>().Where(c => c.IsEffectivelyVisible).Select(c => c.Text));
    private static void Pump() => Dispatcher.UIThread.RunJobs();
    private static async Task Until(Func<bool> ready)
    {
        var timer = Stopwatch.StartNew();
        while (!ready()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), "A operação do painel não terminou."); Pump(); await Task.Delay(10); }
        Pump();
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 9, 15, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => Zone;
    }
    private sealed class Actions : IAgendaRetrospectiveActions
    {
        public Dictionary<DateOnly, AgendaWeekRetrospective> Reports { get; } = new() { [Monday] = Report(Monday), [Monday.AddDays(-7)] = Report(Monday.AddDays(-7), previous: true) };
        public List<DateOnly> Opened { get; } = [];
        public bool Fail { get; set; }
        public TaskCompletionSource? Gate { get; set; }
        public async Task<AgendaWeekRetrospective> GetOrCloseAsync(DateOnly referenceDate, CancellationToken cancellationToken = default)
        {
            Opened.Add(referenceDate); if (Gate is { } gate) await gate.Task.WaitAsync(cancellationToken);
            if (Fail) throw new InvalidOperationException("private-test-path");
            if (!Reports.TryGetValue(referenceDate, out var report)) Reports[referenceDate] = report = Report(referenceDate, empty: true);
            return report;
        }
        public Task<AgendaWeekRetrospective?> GetAsync(DateOnly weekStart, CancellationToken cancellationToken = default) => Task.FromResult(Reports.GetValueOrDefault(weekStart));
        public Task<IReadOnlyList<AgendaWeekRetrospective>> ListAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Not used by this panel.");
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public ServiceProvider Services { get; private set; } = null!;
        public Window Window { get; } = new() { Width = 1600, Height = 850 };
        public AgendaWorkspaceView View { get; private set; } = null!;
        public Actions Actions { get; } = new();
        public AgendaRetrospectiveViewModel Loader { get; private set; } = null!;
        public AgendaViewModel Vm { get; private set; } = null!;
        public Control Panel => C<Control>("AgendaRetrospectivePanel");
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath); var prompt = new WindowUnsavedChangesPrompt(f.Window); var clock = new Clock();
                f.Loader = new(f.Actions, new AgendaRetrospectiveComparer(), clock);
                f.Vm = legacy ? new(f.Services.GetRequiredService<IAgendaActions>(), prompt) : new(f.Services.GetRequiredService<IAgendaActions>(), prompt,
                    f.Services.GetRequiredService<IAgendaTrashActions>(), clock, f.Services.GetRequiredService<IAgendaPeriodReader>(), new AgendaWeekProjector(), f.Loader);
                await f.Vm.InitializeAsync(Monday.AddDays(7)); f.View = new() { DataContext = f.Vm }; f.Window.Content = f.View; f.Window.Show(); Pump(); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public T C<T>(string name) where T : Control { var control = View.GetVisualDescendants().OfType<T>().SingleOrDefault(c => c.Name == name); Assert.NotNull(control); return control; }
        public Button B(string name) => C<Button>(name);
        public IReadOnlyList<Control> Rows<T>(string className) => View.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains(className) && c.DataContext is T && c.IsEffectivelyVisible).ToArray();
        public void Click(string name)
        {
            var button = B(name); Assert.True(button.IsEffectivelyVisible && button.IsEffectivelyEnabled);
            if (button.Command is { } command) { Assert.True(command.CanExecute(button.CommandParameter)); command.Execute(button.CommandParameter); }
            else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        }
        public async Task OpenAsync() { Click("AgendaRetrospectiveModeButton"); await Until(() => !Vm.IsBusy && Vm.IsRetrospectiveView); Window.UpdateLayout(); Pump(); }
        public void Dispose() { Actions.Gate?.TrySetResult(); foreach (var child in Window.OwnedWindows.ToArray()) child.Close(); Window.Close(); Pump(); Services?.Dispose(); Root.Dispose(); }
    }
}
