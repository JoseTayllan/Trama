using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaWeekUiTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.FromHours(-3));

    [AvaloniaFact]
    public async Task WeekMode_ShowsSevenOrderedDaysWithRangeAndTodayMarker()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync();
        Assert.Equal(Enumerable.Range(0, 7).Select(Monday.AddDays), f.Days.Select(c => ((AgendaWeekDay)c.DataContext!).Date));
        Assert.Contains("02/11/2026", f.Control<TextBlock>("AgendaWeekHeading").Text!); Assert.Contains("08/11/2026", f.Control<TextBlock>("AgendaWeekHeading").Text!);
        Assert.Equal(DateOnly.FromDateTime(Now.DateTime), Assert.IsType<AgendaWeekDay>(Assert.Single(f.Days, c => c.Classes.Contains("today")).DataContext).Date);
        Assert.True(f.Control<Control>("AgendaWeekDays").IsEffectivelyVisible); Assert.False(f.Control<ItemsControl>("AgendaTaskList").IsEffectivelyVisible);
    }

    [AvaloniaTheory]
    [InlineData(-7, "AgendaPreviousWeekButton")]
    [InlineData(7, "AgendaNextWeekButton")]
    public async Task WeekNavigationButtons_LoadTheAdjacentWeek(int offset, string name)
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); f.Click(name);
        await UntilAsync(() => !f.Vm.IsBusy && f.Vm.Week!.StartDate == Monday.AddDays(offset));
        Assert.Equal(Monday.AddDays(offset), Assert.IsType<AgendaWeekDay>(f.Days[0].DataContext).Date); Assert.Equal(Monday, f.Vm.SelectedDay);
    }

    [AvaloniaFact]
    public async Task TodayButton_UsesTheInjectedLocalClockAndKeepsTheDraft()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); f.Click("AgendaNextWeekButton"); await UntilAsync(() => f.Vm.Week!.StartDate == Monday.AddDays(7) && !f.Vm.IsBusy);
        f.Vm.Title = "Não apagar"; f.Click("AgendaTodayButton"); await UntilAsync(() => f.Vm.Week!.StartDate == Monday && !f.Vm.IsBusy);
        Assert.Equal("Não apagar", f.Vm.Title); Assert.Empty(f.Window.OwnedWindows); Assert.True(f.Vm.HasUnsavedChanges);
    }

    [AvaloniaTheory]
    [InlineData(AgendaTaskCategory.Personal, 1)]
    [InlineData(AgendaTaskCategory.Work, 2)]
    [InlineData(AgendaTaskCategory.Study, 3)]
    public async Task CategorySelector_FiltersCardsAndResetRestoresAll(AgendaTaskCategory category, int index)
    {
        using var f = await Fixture.CreateAsync(); foreach (var value in Enum.GetValues<AgendaTaskCategory>()) await f.SeedAsync(Example() with { Category = value });
        await f.OpenWeekAsync(); var filter = f.Control<ComboBox>("AgendaWeekCategoryFilter"); filter.SelectedIndex = index; Pump();
        await UntilAsync(() => f.Cards.Count == 1); Assert.Equal(category, Assert.IsType<AgendaWeekEntry>(Assert.Single(f.Cards).DataContext).Task.Category);
        filter.SelectedIndex = 0; Pump(); await UntilAsync(() => f.Cards.Count == 3); Assert.Null(f.Vm.WeekCategoryFilter);
    }

    [AvaloniaTheory]
    [InlineData(AgendaTaskStatus.Pending, 1)]
    [InlineData(AgendaTaskStatus.InProgress, 2)]
    [InlineData(AgendaTaskStatus.Completed, 3)]
    public async Task StateSelector_FiltersIndependentlyOfTheCategory(AgendaTaskStatus status, int index)
    {
        using var f = await Fixture.CreateAsync(); foreach (var value in Enum.GetValues<AgendaTaskStatus>()) await f.SeedAsync(Example() with { Status = value });
        await f.OpenWeekAsync(); f.Control<ComboBox>("AgendaWeekCategoryFilter").SelectedIndex = 2; f.Control<ComboBox>("AgendaWeekStatusFilter").SelectedIndex = index; Pump();
        await UntilAsync(() => f.Cards.Count == 1); var task = Assert.IsType<AgendaWeekEntry>(Assert.Single(f.Cards).DataContext).Task;
        Assert.Equal(status, task.Status); Assert.Equal(AgendaTaskCategory.Work, task.Category);
    }

    [AvaloniaFact]
    public async Task ModeSwitching_PreservesTheSameNativeEditorAndItsDraft()
    {
        using var f = await Fixture.CreateAsync(); var editor = f.Control<TextBox>("AgendaTitleInput"); editor.Text = "Meu rascunho";
        await f.OpenWeekAsync(); f.Click("AgendaDayModeButton"); await UntilAsync(() => !f.Vm.IsWeekView);
        Assert.Same(editor, f.Control<TextBox>("AgendaTitleInput")); Assert.Equal("Meu rascunho", editor.Text); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.True(f.Control<ItemsControl>("AgendaTaskList").IsEffectivelyVisible); Assert.Empty(f.Window.OwnedWindows);
    }

    [AvaloniaTheory]
    [InlineData(UnsavedChangesChoice.Cancel, false)]
    [InlineData(UnsavedChangesChoice.Discard, true)]
    [InlineData(UnsavedChangesChoice.Save, true)]
    public async Task DayCreateAction_UsesTheExistingDraftDecision(UnsavedChangesChoice choice, bool changed)
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); f.Vm.Title = "Rascunho anterior";
        var button = Assert.Single(f.View.GetVisualDescendants().OfType<Button>(), c => c.Classes.Contains("agenda-week-create") && c.DataContext is AgendaWeekDay day && day.Date == Monday.AddDays(2));
        Click(button); await UntilAsync(() => f.Window.OwnedWindows.OfType<UnsavedChangesDialog>().Any());
        var dialog = Assert.Single(f.Window.OwnedWindows.OfType<UnsavedChangesDialog>());
        Click(Control<Button>(dialog, choice switch { UnsavedChangesChoice.Save => "SaveAndContinueButton", UnsavedChangesChoice.Discard => "DiscardButton", _ => "CancelButton" }));
        await UntilAsync(() => !f.Vm.IsBusy && f.Window.OwnedWindows.Count == 0);
        Assert.Equal(changed ? Monday.AddDays(2) : Monday, f.Vm.SelectedDay); Assert.Equal(changed ? string.Empty : "Rascunho anterior", f.Vm.Title);
        Assert.True(f.Vm.IsWeekView); Assert.Equal(choice == UnsavedChangesChoice.Save ? 1 : 0, (await f.Actions.ListAsync(Monday, Monday)).Count);
    }

    [AvaloniaFact]
    public async Task ClickingAContinuationCard_OpensTheOriginalIdentityAndDates()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedDate = Monday.AddDays(-1), PlannedEndDate = Monday.AddDays(2) }; await f.SeedAsync(task); await f.OpenWeekAsync();
        var card = Assert.Single(f.Cards, c => ((AgendaWeekEntry)c.DataContext!).Date == Monday.AddDays(1)); Click(card);
        await UntilAsync(() => !f.Vm.IsBusy && f.Vm.EditingTaskId == task.Id);
        Assert.Equal(task.Title, f.Control<TextBox>("AgendaTitleInput").Text); Assert.Equal(task.PlannedDate, DateOnly.FromDateTime(f.Vm.PlannedDate!.Value.DateTime));
        Assert.Equal(task.PlannedEndDate, DateOnly.FromDateTime(f.Vm.PlannedEndDate!.Value.DateTime)); Assert.True(f.Button("AgendaDeleteTaskButton").IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData(AgendaTaskStatus.Pending, "○", "Pendente")]
    [InlineData(AgendaTaskStatus.InProgress, "◐", "Em andamento")]
    [InlineData(AgendaTaskStatus.Completed, "✓", "Concluída")]
    public async Task RenderedCards_ExposeStateSymbolAndReadableLabelAlongsideCategory(AgendaTaskStatus status, string symbol, string label)
    {
        using var f = await Fixture.CreateAsync(); await f.SeedAsync(Example() with { Status = status }); await f.OpenWeekAsync(); var card = Assert.Single(f.Cards);
        Assert.Equal(symbol, Assert.Single(card.GetVisualDescendants().OfType<TextBlock>(), c => c.Classes.Contains("agenda-week-status-icon")).Text);
        Assert.Equal(label, Assert.Single(card.GetVisualDescendants().OfType<TextBlock>(), c => c.Classes.Contains("agenda-week-status")).Text);
        Assert.Contains("Trabalho", Text(card)); Assert.True(card.Focusable); Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(card)));
    }

    [AvaloniaFact]
    public async Task CategoryStripe_UsesDynamicPaletteResourcesIndependentlyOfState()
    {
        using var f = await Fixture.CreateAsync(); await f.SeedAsync(Example() with { Status = AgendaTaskStatus.Completed }); await f.OpenWeekAsync();
        var stripe = Assert.Single(Assert.Single(f.Cards).GetVisualDescendants().OfType<Border>(), c => c.Classes.Contains("category-spine"));
        var brush = new SolidColorBrush(Color.FromRgb(70, 120, 180)); f.Window.Resources["TramaAgendaWorkBrush"] = brush; Pump();
        Assert.Equal(brush.Color, Assert.IsAssignableFrom<ISolidColorBrush>(stripe.Background).Color);
        Assert.Contains("Concluída", Text(Assert.Single(f.Cards)));
    }

    [AvaloniaFact]
    public async Task MultiDayCards_ShowContinuityAndOnlyTheHoursOfTheirOwnDay()
    {
        using var f = await Fixture.CreateAsync(); await f.SeedAsync(Example() with { PlannedEndDate = Monday.AddDays(2), StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) }); await f.OpenWeekAsync();
        Assert.Equal(3, f.Cards.Count); var first = f.Cards[0]; var middle = f.Cards[1]; var last = f.Cards[2];
        Assert.Contains("22:00", Text(first)); Assert.DoesNotContain("02:00", Text(first));
        Assert.DoesNotContain("22:00", Text(middle)); Assert.DoesNotContain("02:00", Text(middle)); Assert.Contains("continua", Text(middle), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("02:00", Text(last)); Assert.DoesNotContain("22:00", Text(last)); Assert.Contains("1 tarefa", f.Control<TextBlock>("AgendaWeekCount").Text!);
    }

    [AvaloniaFact]
    public async Task EmptyFilteredWeek_KeepsSevenDaysAndProvidesACreateActionForEach()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); Assert.Empty(f.Cards); Assert.Equal(7, f.Days.Count);
        Assert.True(f.Control<TextBlock>("AgendaWeekEmptyMessage").IsEffectivelyVisible);
        Assert.Equal(7, f.View.GetVisualDescendants().OfType<Button>().Count(c => c.Classes.Contains("agenda-week-create") && c.IsEffectivelyVisible));
    }

    [AvaloniaFact]
    public async Task FailedNavigation_ShowsErrorInsteadOfEmptyAndReloadRecovers()
    {
        using var f = await Fixture.CreateAsync(); await f.SeedAsync(Example()); await f.OpenWeekAsync(); f.Reader.Fail = true;
        f.Click("AgendaNextWeekButton"); await UntilAsync(() => !f.Vm.IsBusy && f.Vm.HasWeekError);
        Assert.Single(f.Cards); Assert.True(f.Control<TextBlock>("AgendaWeekErrorMessage").IsEffectivelyVisible);
        Assert.False(f.Control<TextBlock>("AgendaWeekEmptyMessage").IsEffectivelyVisible); Assert.DoesNotContain("Synthetic", Text(f.View));
        f.Reader.Fail = false; f.Click("AgendaWeekRefreshButton"); await UntilAsync(() => !f.Vm.IsBusy && !f.Vm.HasWeekError); Assert.Single(f.Cards);
    }

    [AvaloniaFact]
    public async Task PendingWeekQuery_DisablesNavigationFiltersAndFormUntilItFinishes()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); f.Reader.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Click("AgendaNextWeekButton");
        try
        {
            Assert.True(f.Vm.IsBusy); Assert.True(f.Control<TextBlock>("AgendaWeekLoadingMessage").IsEffectivelyVisible);
            foreach (var name in new[] { "AgendaPreviousWeekButton", "AgendaNextWeekButton", "AgendaWeekModeButton", "AgendaDayModeButton", "AgendaWeekCategoryFilter", "AgendaWeekStatusFilter", "AgendaSaveButton" })
                Assert.False(f.Control<Control>(name).IsEffectivelyEnabled);
        }
        finally { f.Reader.Gate.TrySetResult(); }
        await UntilAsync(() => !f.Vm.IsBusy); Assert.True(f.Button("AgendaSaveButton").IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task NativeSave_UpdatesCardsAndClearsTheSharedForm()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); f.Control<TextBox>("AgendaTitleInput").Text = "Pela tela semanal"; f.Click("AgendaSaveButton");
        await UntilAsync(() => !f.Vm.IsBusy && f.Vm.EditingTaskId is null && f.Cards.Count == 1);
        Assert.Contains("Pela tela semanal", Text(Assert.Single(f.Cards))); Assert.Equal(string.Empty, f.Control<TextBox>("AgendaTitleInput").Text);
        Assert.Single(await f.Actions.ListAsync(Monday, Monday)); Assert.False(f.Vm.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task WeeklySelection_KeepsDeleteAndTrashActionsDiscoverable()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SeedAsync(task); await f.OpenWeekAsync(); Click(Assert.Single(f.Cards));
        await UntilAsync(() => f.Vm.EditingTaskId == task.Id && !f.Vm.IsBusy);
        Assert.True(f.Button("AgendaDeleteTaskButton").IsEffectivelyVisible); Assert.True(f.Button("AgendaDeleteTaskButton").IsEffectivelyEnabled);
        Assert.True(f.Button("AgendaTrashButton").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task Resizing_PreservesNativeEditorDraftFiltersAndSelectedTask()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.SeedAsync(task); await f.OpenWeekAsync(); Click(Assert.Single(f.Cards)); await UntilAsync(() => f.Vm.EditingTaskId == task.Id && !f.Vm.IsBusy);
        var editor = f.Control<TextBox>("AgendaTitleInput"); editor.Text = "Ainda editando"; f.Control<ComboBox>("AgendaWeekCategoryFilter").SelectedIndex = 2; Pump();
        f.Window.Width = 760; f.Window.Height = 680; Pump(); f.Window.UpdateLayout(); f.Window.Width = 1600; Pump(); f.Window.UpdateLayout();
        Assert.Same(editor, f.Control<TextBox>("AgendaTitleInput")); Assert.Equal("Ainda editando", editor.Text); Assert.Equal(task.Id, f.Vm.EditingTaskId);
        Assert.Equal(AgendaTaskCategory.Work, f.Vm.WeekCategoryFilter); Assert.True(f.Vm.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task CompactWeek_HasNoHorizontalOverflowAndPrimaryActionsRemainReachable()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync(); f.Window.Width = 760; f.Window.Height = 680; Pump(); f.Window.UpdateLayout();
        foreach (var day in f.Days)
        {
            Assert.True(day.Bounds.Width > 0); var point = day.TranslatePoint(default, f.Window)!.Value;
            Assert.InRange(point.X, 0, f.Window.Bounds.Width - day.Bounds.Width + 1);
        }
        foreach (var name in new[] { "AgendaWeekModeButton", "AgendaWeekCategoryFilter", "AgendaWeekStatusFilter", "AgendaSaveButton" })
        {
            var control = f.Control<Control>(name); control.BringIntoView(); Pump(); f.Window.UpdateLayout();
            var point = control.TranslatePoint(default, f.Window)!.Value; Assert.True(control.IsEffectivelyVisible && control.Bounds.Height > 0);
            Assert.InRange(point.X, 0, f.Window.Bounds.Width - control.Bounds.Width + 1); Assert.InRange(point.Y, 0, f.Window.Bounds.Height - control.Bounds.Height + 1);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CalendarEdges_DisableNavigationBeyondRepresentableWeeks(bool upper)
    {
        using var f = await Fixture.CreateAsync(); var anchor = upper ? DateOnly.MaxValue.AddDays(-7) : DateOnly.MinValue;
        Assert.True(await f.Vm.ShowWeekAsync(anchor)); Pump(); var name = upper ? "AgendaNextWeekButton" : "AgendaPreviousWeekButton";
        Assert.False(f.Button(name).IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task WeekActions_ProvideKeyboardFocusAndAccessibleNames()
    {
        using var f = await Fixture.CreateAsync(); await f.OpenWeekAsync();
        foreach (var button in f.View.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("agenda-week-create")))
        {
            Assert.True(button.Focusable); Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
        }
        Assert.True(f.Button("AgendaWeekModeButton").Focusable); Assert.True(f.Button("AgendaWeekModeButton").Focus());
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(f.Control<ComboBox>("AgendaWeekCategoryFilter"))));
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(f.Control<ComboBox>("AgendaWeekStatusFilter"))));
    }

    [AvaloniaFact]
    public async Task LegacyDailyView_DoesNotOfferAnEnabledWeekMode()
    {
        using var f = await Fixture.CreateAsync(legacy: true); var button = f.View.FindControl<Button>("AgendaWeekModeButton");
        Assert.True(button is null || !button.IsEffectivelyVisible || !button.IsEffectivelyEnabled);
        f.Vm.Title = "Compatível"; Assert.True(await f.Vm.SaveAsync()); Assert.Single(await f.Actions.ListAsync(Monday, Monday));
    }

    [AvaloniaFact]
    public async Task ExistingDirectTimeInputs_RemainEditableWithoutTimePickerScrolling()
    {
        using var f = await Fixture.CreateAsync(); Assert.Empty(f.View.GetVisualDescendants().OfType<TimePicker>());
        f.Control<TextBox>("AgendaStartTimeInput").Text = "09:45"; Pump(); Assert.Equal(TimeSpan.FromMinutes(585), f.Vm.StartTime);
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Conteúdo de teste", Monday, new TimeOnly(9, 0), new TimeOnly(10, 0), AgendaTaskCategory.Work);
    private static T Control<T>(Control root, string name) where T : Control { var value = root.FindControl<T>(name); Assert.NotNull(value); return value; }
    private static string Text(Control root) => string.Join("\n", root.GetVisualDescendants().OfType<TextBlock>().Select(c => c.Text));
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
        while (!ready()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), "A interface semanal não terminou a operação."); Pump(); await Task.Delay(10); }
        Pump();
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("AgendaWeekUi", Now.Offset, "Test", "Test");
    }
    private sealed class Reader(IAgendaPeriodReader inner) : IAgendaPeriodReader
    {
        public bool Fail { get; set; }
        public TaskCompletionSource? Gate { get; set; }
        public async Task<IReadOnlyList<AgendaTaskSnapshot>> ListOverlappingAsync(DateOnly from, DateOnly through, CancellationToken cancellationToken = default)
        {
            if (Gate is { } gate) await gate.Task.WaitAsync(cancellationToken);
            if (Fail) throw new AgendaPersistenceException("Synthetic weekly failure");
            return await inner.ListOverlappingAsync(from, through, cancellationToken);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public Window Window { get; } = new() { Width = 1600, Height = 850 };
        public AgendaWorkspaceView View { get; private set; } = null!;
        public AgendaViewModel Vm { get; private set; } = null!;
        public SqliteAgendaActions Actions { get; private set; } = null!;
        public Reader Reader { get; private set; } = null!;
        public IReadOnlyList<Control> Days => View.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("agenda-week-day") && c.DataContext is AgendaWeekDay).ToArray();
        public IReadOnlyList<Button> Cards => View.GetVisualDescendants().OfType<Button>().Where(c => c.Classes.Contains("agenda-week-card") && c.IsEffectivelyVisible).ToArray();
        public static async Task<Fixture> CreateAsync(bool legacy = false)
        {
            var f = new Fixture();
            try
            {
                await using var context = f.Root.CreateContext(); await context.Database.MigrateAsync(); var path = Path.Combine(f.Root.DirectoryPath, "trama.db");
                f.Actions = new(path); f.Reader = new(f.Actions); var prompt = new WindowUnsavedChangesPrompt(f.Window);
                f.Vm = legacy ? new(f.Actions, prompt) : new(f.Actions, prompt, new SqliteAgendaTrashActions(path), new Clock(), f.Reader, new AgendaWeekProjector());
                await f.Vm.InitializeAsync(Monday); f.View = new() { DataContext = f.Vm }; f.Window.Content = f.View; f.Window.Show(); Pump(); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public T Control<T>(string name) where T : Control => AgendaWeekUiTests.Control<T>(View, name);
        public Button Button(string name) => Control<Button>(name);
        public void Click(string name) => AgendaWeekUiTests.Click(Button(name));
        public async Task OpenWeekAsync() { Click("AgendaWeekModeButton"); await UntilAsync(() => Vm.IsWeekView && !Vm.IsBusy); Window.UpdateLayout(); Pump(); }
        public async Task SeedAsync(AgendaTaskSnapshot task) => Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);
        public void Dispose()
        {
            Reader?.Gate?.TrySetResult(); foreach (var child in Window.OwnedWindows.ToArray()) child.Close(); Window.Close(); Pump(); Root.Dispose();
        }
    }
}
