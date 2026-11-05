using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaDateControlsTests
{
    private static readonly DateOnly Day = new(2020, 11, 5);
    private static readonly DateTimeOffset Date = new(2020, 11, 5, 0, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public async Task Editing_LoadsThePersistedEndDateAndReadableTimes()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1), StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) };
        await f.Actions.SaveAsync(task); Assert.True(await f.Vm.EditTaskAsync(task.Id));
        Assert.Equal(Day.AddDays(1), DateOnly.FromDateTime(Get<DateTimeOffset?>(f.Vm, "PlannedEndDate")!.Value.DateTime));
        Assert.Equal("22:00", Get<string>(f.Vm, "StartTimeText")); Assert.Equal("02:00", Get<string>(f.Vm, "EndTimeText")); Assert.False(f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task CreatingAnotherTask_ClearsEndDateAndTypedTimes()
    {
        using var f = await Fixture.CreateAsync(); Set(f.Vm, "PlannedEndDate", Date.AddDays(1)); Set(f.Vm, "StartTimeText", "22:00");
        f.Prompt.Choice = UnsavedChangesChoice.Discard; Assert.True(await f.Vm.NewTaskAsync());
        Assert.Null(Get<DateTimeOffset?>(f.Vm, "PlannedEndDate")); Assert.Equal(string.Empty, Get<string>(f.Vm, "StartTimeText")); Assert.Equal(string.Empty, Get<string>(f.Vm, "EndTimeText")); Assert.False(f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task TypedCrossDayTimes_SaveThroughTheRealSqliteService()
    {
        using var f = await Fixture.CreateAsync(); f.Vm.Title = "Estudo noturno";
        Set(f.Vm, "PlannedEndDate", Date.AddDays(1)); Set(f.Vm, "StartTimeText", "22:00"); Set(f.Vm, "EndTimeText", "02:00");
        Assert.True(await f.Vm.SaveAsync()); var saved = Assert.Single(await f.Actions.ListAsync(Day, Day));
        Assert.Equal(Day.AddDays(1), saved.PlannedEndDate); Assert.Equal(new TimeOnly(22, 0), saved.StartTime); Assert.Equal(new TimeOnly(2, 0), saved.EndTime);
    }

    [Fact]
    public async Task ClearingTheEndDate_PreservesAValidSameDayInterval()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { PlannedEndDate = Day.AddDays(1) }; await f.Actions.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id);
        Set(f.Vm, "PlannedEndDate", null); Assert.True(f.Vm.HasUnsavedChanges); Assert.True(await f.Vm.SaveAsync());
        Assert.Equal(task with { PlannedEndDate = null }, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task ChangingOnlyTheEndDate_MarksTheDraftAsUnsaved()
    {
        using var f = await Fixture.CreateAsync(); Assert.False(f.Vm.HasUnsavedChanges);
        Set(f.Vm, "PlannedEndDate", Date.AddDays(1)); Assert.True(f.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task CancelledDayNavigation_KeepsTheEndDateAndIncompleteTimeText()
    {
        using var f = await Fixture.CreateAsync(); Set(f.Vm, "PlannedEndDate", Date.AddDays(1)); Set(f.Vm, "StartTimeText", "09:");
        Assert.False(await f.Vm.ChangeDayAsync(Day.AddDays(2))); Assert.Equal(Day, f.Vm.SelectedDay);
        Assert.Equal(Date.AddDays(1), Get<DateTimeOffset?>(f.Vm, "PlannedEndDate")); Assert.Equal("09:", Get<string>(f.Vm, "StartTimeText"));
    }

    [Fact]
    public async Task FailedSave_KeepsTheEndDateAndTypedTimes()
    {
        using var f = await Fixture.CreateAsync(); f.Vm.Title = "Preservar"; Set(f.Vm, "PlannedEndDate", Date.AddDays(1)); Set(f.Vm, "StartTimeText", "22:00"); Set(f.Vm, "EndTimeText", "02:00");
        await using (var context = f.Root.CreateContext()) await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailDateForm BEFORE INSERT ON AgendaTasks BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        Assert.False(await f.Vm.SaveAsync()); Assert.True(f.Vm.HasUnsavedChanges);
        Assert.Equal(Date.AddDays(1), Get<DateTimeOffset?>(f.Vm, "PlannedEndDate")); Assert.Equal("22:00", Get<string>(f.Vm, "StartTimeText")); Assert.Equal("02:00", Get<string>(f.Vm, "EndTimeText"));
    }

    [Theory]
    [InlineData("00:00", 0, 0)]
    [InlineData("09:05", 9, 5)]
    [InlineData("23:59", 23, 59)]
    public async Task DirectTimeEntry_SavesAllDayBoundaries(string text, int hour, int minute)
    {
        using var f = await Fixture.CreateAsync(); f.Vm.Title = "Horário digitado"; Set(f.Vm, "StartTimeText", text);
        Assert.True(await f.Vm.SaveAsync()); var task = Assert.Single(await f.Actions.ListAsync(Day, Day)); Assert.Equal(new TimeOnly(hour, minute), task.StartTime); Assert.Null(task.EndTime);
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("12:60")]
    [InlineData("9")]
    [InlineData("abc")]
    [InlineData("09:")]
    [InlineData("-1:00")]
    public async Task InvalidOrIncompleteTime_IsKeptAndNeverSavedAsAnEmptyHour(string text)
    {
        using var f = await Fixture.CreateAsync(); f.Vm.Title = "Não perder"; Set(f.Vm, "StartTimeText", text);
        Assert.False(await f.Vm.SaveAsync()); Assert.Equal(text, Get<string>(f.Vm, "StartTimeText"));
        Assert.True(f.Vm.HasUnsavedChanges); Assert.NotEmpty(f.Vm.Feedback); Assert.Empty(await f.Actions.ListAsync(Day, Day));
    }

    [Fact]
    public async Task ClearingTimeText_ClearsTheStoredHour()
    {
        using var f = await Fixture.CreateAsync(); var task = Example() with { EndTime = null }; await f.Actions.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id);
        Set(f.Vm, "StartTimeText", string.Empty); Assert.True(await f.Vm.SaveAsync()); Assert.Equal(task with { StartTime = null }, await f.Actions.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task IncompleteEndTime_DoesNotOverwriteThePreviouslySavedTask()
    {
        using var f = await Fixture.CreateAsync(); var task = Example(); await f.Actions.SaveAsync(task); await f.Vm.EditTaskAsync(task.Id);
        Set(f.Vm, "EndTimeText", "02:"); Assert.False(await f.Vm.SaveAsync()); Assert.Equal(task, await f.Actions.GetByIdAsync(task.Id)); Assert.Equal("02:", Get<string>(f.Vm, "EndTimeText"));
    }

    [AvaloniaFact]
    public async Task NativeForm_UsesDirectTimeInputsAndAnOptionalEndDate()
    {
        using var f = await Fixture.CreateAsync(); var view = new AgendaWorkspaceView { DataContext = f.Vm }; var window = new Window { Content = view, Width = 1100, Height = 760 }; window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs(); Assert.Empty(view.GetVisualDescendants().OfType<TimePicker>());
            var start = view.FindControl<TextBox>("AgendaStartTimeInput"); var end = view.FindControl<TextBox>("AgendaEndTimeInput"); Assert.NotNull(start); Assert.NotNull(end);
            var endDate = view.FindControl<DatePicker>("AgendaEndDateInput"); Assert.NotNull(endDate); Assert.Null(endDate.SelectedDate);
            f.Vm.Title = "Pelo formulário"; start.Text = "22:00"; end.Text = "02:00"; endDate.SelectedDate = Date.AddDays(1); Dispatcher.UIThread.RunJobs();
            await f.Vm.SaveCommand.ExecuteAsync(null); var task = Assert.Single(await f.Actions.ListAsync(Day, Day)); Assert.Equal(Day.AddDays(1), task.PlannedEndDate); Assert.Equal(new TimeOnly(2, 0), task.EndTime);
            Assert.False(f.Vm.HasUnsavedChanges); Assert.Equal(string.Empty, start.Text); Assert.Null(endDate.SelectedDate);
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaFact]
    public async Task CompactForm_KeepsTheDateAndTimeInputsInsideTheViewport()
    {
        using var f = await Fixture.CreateAsync(); var view = new AgendaWorkspaceView { DataContext = f.Vm }; var window = new Window { Content = view, Width = 760, Height = 650 }; window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            foreach (var name in new[] { "AgendaDateInput", "AgendaEndDateInput", "AgendaStartTimeInput", "AgendaEndTimeInput", "AgendaSaveButton" })
            {
                var control = view.FindControl<Control>(name); Assert.NotNull(control); Assert.True(control.IsEffectivelyVisible); Assert.True(control.Bounds.Width > 0);
                var point = control.TranslatePoint(default, view)!.Value; Assert.InRange(point.X, 0, view.Bounds.Width - control.Bounds.Width + 1);
                if (control is TextBox) Assert.InRange(control.Bounds.Height, 24, 40);
            }
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    [Fact]
    public void TaskSummary_DistinguishesTheEndDateInsteadOfShowingOnlyHours()
    {
        var task = Example() with { PlannedEndDate = Day.AddDays(1), StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) };
        var label = Assert.IsType<string>(new AgendaPresentationConverter().Convert(task, typeof(string), "time", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")));
        Assert.Contains("06/11", label); Assert.Contains("22:00", label); Assert.Contains("02:00", label);
    }

    [Fact]
    public void Completion_DefaultKeepsTheAutomaticTimestamp()
    {
        var vm = Completion(); vm.SelectAllCommand.Execute(null); Assert.Null(vm.CreateDecision(AgendaCompletionChoice.CompleteSelected).CompletedAt);
    }

    [Fact]
    public void Completion_CustomDateAndDirectTimeAreReturnedWithTheSelection()
    {
        var vm = Completion(); ConfigureCustom(vm, Date.AddDays(1), "02:15"); vm.SelectAllCommand.Execute(null);
        var decision = vm.CreateDecision(AgendaCompletionChoice.CompleteSelected);
        Assert.Equal(Date.AddDays(1).AddHours(2).AddMinutes(15), decision.CompletedAt); Assert.Single(decision.SelectedTaskIds);
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("09:")]
    [InlineData("")]
    public void Completion_InvalidCustomTime_DisablesCompletionButKeepsTheText(string text)
    {
        var vm = Completion(); vm.SelectAllCommand.Execute(null); ConfigureCustom(vm, Date.AddDays(1), text);
        Assert.False(vm.CanCompleteSelected); Assert.Equal(text, Get<string>(vm, "CompletionTimeText")); Assert.Throws<InvalidOperationException>(() => vm.CreateDecision(AgendaCompletionChoice.CompleteSelected));
    }

    [Fact]
    public void Completion_MissingCustomDate_PreventsCompletion()
    {
        var vm = Completion(); vm.SelectAllCommand.Execute(null); ConfigureCustom(vm, null, "02:15"); Assert.False(vm.CanCompleteSelected);
        Assert.Throws<InvalidOperationException>(() => vm.CreateDecision(AgendaCompletionChoice.CompleteSelected));
    }

    [Fact]
    public void Completion_SwitchingBackToNow_IgnoresIncompleteCustomFields()
    {
        var vm = Completion(); vm.SelectAllCommand.Execute(null); ConfigureCustom(vm, null, "02:"); Set(vm, "UseCustomCompletionTime", false);
        Assert.True(vm.CanCompleteSelected); Assert.Null(vm.CreateDecision(AgendaCompletionChoice.CompleteSelected).CompletedAt);
    }

    [Theory]
    [InlineData(AgendaCompletionChoice.Cancel)]
    [InlineData(AgendaCompletionChoice.KeepOpen)]
    public void Completion_CancelAndKeepOpenIgnoreIncompleteCustomFields(AgendaCompletionChoice choice)
    {
        var vm = Completion(); vm.SelectAllCommand.Execute(null); ConfigureCustom(vm, null, "02:");
        var decision = vm.CreateDecision(choice); Assert.Equal(choice, decision.Choice); Assert.Empty(decision.SelectedTaskIds); Assert.Null(decision.CompletedAt);
    }

    [AvaloniaFact]
    public async Task NativeCompletion_UsesDirectTimeEntryAndReturnsTheChosenInstant()
    {
        var vm = Completion(); vm.SelectAllCommand.Execute(null); var owner = new Window { Width = 1000, Height = 700 }; owner.Show(); var dialog = new AgendaCompletionDialog(vm);
        try
        {
            var result = dialog.ShowDialog<AgendaCompletionDecision?>(owner); Dispatcher.UIThread.RunJobs();
            var toggle = dialog.FindControl<CheckBox>("AgendaCustomCompletionToggle"); Assert.NotNull(toggle); toggle.IsChecked = true; Dispatcher.UIThread.RunJobs();
            var date = dialog.FindControl<DatePicker>("AgendaCompletionDateInput"); var time = dialog.FindControl<TextBox>("AgendaCompletionTimeInput"); Assert.NotNull(date); Assert.NotNull(time);
            date.SelectedDate = Date.AddDays(1); time.Text = "02:15"; Dispatcher.UIThread.RunJobs(); Assert.Empty(dialog.GetVisualDescendants().OfType<TimePicker>());
            dialog.FindControl<Button>("CompleteSelectedButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(Date.AddDays(1).AddHours(2).AddMinutes(15), (await result)!.CompletedAt);
        }
        finally { dialog.Close(); owner.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaFact]
    public async Task CompactCompletion_KeepsCustomInputsAndExitActionsReachable()
    {
        var vm = Completion(); ConfigureCustom(vm, Date.AddDays(1), "02:15"); var owner = new Window { Width = 1000, Height = 700 }; owner.Show(); var dialog = new AgendaCompletionDialog(vm);
        try
        {
            var result = dialog.ShowDialog<AgendaCompletionDecision?>(owner); dialog.Width = 560; dialog.Height = 420; Dispatcher.UIThread.RunJobs(); dialog.UpdateLayout();
            foreach (var name in new[] { "AgendaCompletionDateInput", "AgendaCompletionTimeInput", "CancelCompletionButton", "KeepOpenButton", "CompleteSelectedButton" })
            {
                var control = dialog.FindControl<Control>(name); Assert.NotNull(control); Assert.True(control.IsEffectivelyVisible);
                var point = control.TranslatePoint(default, dialog)!.Value; Assert.True(control.Bounds.Width > 0); Assert.InRange(point.X, 0, dialog.Bounds.Width - control.Bounds.Width + 1);
                Assert.InRange(point.Y, 0, dialog.Bounds.Height - control.Bounds.Height + 1);
            }
            Assert.True(dialog.FindControl<Button>("CancelCompletionButton")!.IsDefault); dialog.Close(); await result;
        }
        finally { dialog.Close(); owner.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    [Fact]
    public async Task ExistingStartOnlyFlow_RemainsPendingAndWithoutAnEnd()
    {
        using var f = await Fixture.CreateAsync(); f.Vm.Title = "Em aberto"; f.Vm.StartTime = TimeSpan.FromHours(18);
        Assert.True(await f.Vm.SaveAsync()); var task = Assert.Single(await f.Actions.ListAsync(Day, Day)); Assert.Null(task.EndTime); Assert.Null(task.PlannedEndDate); Assert.Equal(AgendaTaskStatus.Pending, task.Status);
    }

    private static AgendaCompletionDialogViewModel Completion() => new([Example()]);
    private static void ConfigureCustom(object vm, DateTimeOffset? date, string time)
    {
        Set(vm, "UseCustomCompletionTime", true); Set(vm, "CompletionDate", date); Set(vm, "CompletionTimeText", time);
    }
    private static void Set(object vm, string name, object? value)
    {
        var property = vm.GetType().GetProperty(name); Assert.NotNull(property); Assert.True(property.CanWrite); property.SetValue(vm, value);
    }
    private static T Get<T>(object vm, string name)
    {
        var property = vm.GetType().GetProperty(name); Assert.NotNull(property); return (T)property.GetValue(vm)!;
    }
    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Estudo", Day, new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Study);
    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public Task<UnsavedChangesChoice> ChooseAsync() => Task.FromResult(Choice);
    }
    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public Prompt Prompt { get; } = new();
        public SqliteAgendaActions Actions { get; private set; } = null!;
        public AgendaViewModel Vm { get; private set; } = null!;
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture(); await using var context = f.Root.CreateContext(); await context.Database.MigrateAsync();
            f.Actions = new(Path.Combine(f.Root.DirectoryPath, "trama.db")); f.Vm = new(f.Actions, f.Prompt); await f.Vm.InitializeAsync(Day); return f;
        }
        public void Dispose() => Root.Dispose();
    }
}
