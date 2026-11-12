using System.Diagnostics;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaPendingReviewTests
{
    [AvaloniaTheory]
    [InlineData(760)]
    [InlineData(1600)]
    public async Task PendingRows_OfferAnAccessibleOpenAction(int width)
    {
        using var f = await Fixture.OpenAsync(width);
        var button = f.OpenButton();
        Assert.True(button.IsEffectivelyVisible);
        Assert.True(button.IsEffectivelyEnabled);
        var name = AutomationProperties.GetName(button) ?? string.Empty;
        Assert.Contains("Abrir tarefa", name);
        Assert.Contains(f.Captured.Title, name);
    }

    [AvaloniaFact]
    public async Task OpenPendingTask_UsesIdAndShowsCurrentValuesOnItsCurrentDay()
    {
        using var f = await Fixture.OpenAsync();
        var date = f.Agenda.Today.AddDays(2);
        await f.SaveAsync(f.Captured with { Id = Guid.NewGuid(), PlannedDate = date, Description = "Outra tarefa com o título antigo" });
        var current = f.Captured with { Title = "Título atualizado", Description = "Descrição atual", PlannedDate = date, Status = AgendaTaskStatus.InProgress };
        await f.SaveAsync(current);

        await f.OpenPendingTaskAsync();

        Assert.True(f.Agenda.IsDayView);
        Assert.Equal(date, f.Agenda.SelectedDay);
        Assert.Equal(current.Id, f.Agenda.EditingTaskId);
        Assert.Equal(current.Title, f.Agenda.Title);
        Assert.Equal(current.Description, f.Agenda.Description);
        Assert.Equal(current.Status, f.Agenda.Status);
        Assert.Contains(f.Agenda.Tasks, task => task.Id == current.Id);
        Assert.Equal(current, await f.Actions.GetByIdAsync(current.Id));
        Assert.Equal(f.Captured, Assert.Single(f.Agenda.Retrospective!.Summary!.OpenTasks).Task);
        var savedReport = await f.Services.GetRequiredService<IAgendaRetrospectiveActions>().GetAsync(f.Week);
        Assert.NotNull(savedReport);
        Assert.Equal(f.Captured, Assert.Single(savedReport.Tasks).Task);
    }

    [AvaloniaFact]
    public async Task OpenPendingTask_OpensEvenWhenNowCompleted()
    {
        using var f = await Fixture.OpenAsync();
        await f.SaveAsync(f.Captured with { Status = AgendaTaskStatus.Completed });

        await f.OpenPendingTaskAsync();

        Assert.True(f.Agenda.IsDayView);
        Assert.Equal(f.Captured.Id, f.Agenda.EditingTaskId);
        Assert.Equal(AgendaTaskStatus.Completed, f.Agenda.Status);
        Assert.Equal(AgendaTaskStatus.Pending, Assert.Single(f.Agenda.Retrospective!.Summary!.OpenTasks).Task.Status);
    }

    [AvaloniaFact]
    public async Task TrashedTask_KeepsThePanelAndTaskDraftWithoutPrompting()
    {
        using var f = await Fixture.OpenAsync();
        f.Agenda.Title = "Meu rascunho";
        var report = f.Agenda.Retrospective!.Report;
        await f.TrashAsync();

        await f.OpenPendingTaskAsync();

        Assert.True(f.Agenda.IsRetrospectiveView);
        Assert.Equal("Meu rascunho", f.Agenda.Title);
        Assert.True(f.Agenda.HasUnsavedChanges);
        Assert.Equal(0, f.Prompt.Calls);
        Assert.Same(report, f.Agenda.Retrospective.Report);
        Assert.Contains("não está mais disponível", f.Agenda.Feedback, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await f.Actions.GetByIdAsync(f.Captured.Id));
    }

    [AvaloniaTheory]
    [InlineData(UnsavedChangesChoice.Cancel)]
    [InlineData(UnsavedChangesChoice.Discard)]
    [InlineData(UnsavedChangesChoice.Save)]
    public async Task OpenPendingTask_ResolvesTheExistingTaskDraft(UnsavedChangesChoice choice)
    {
        using var f = await Fixture.OpenAsync();
        var draftDay = f.Agenda.SelectedDay;
        f.Agenda.Title = "Meu rascunho";
        f.Prompt.Choice = choice;

        await f.OpenPendingTaskAsync();

        Assert.Equal(1, f.Prompt.Calls);
        var tasks = await f.Actions.ListAsync(draftDay, draftDay);
        if (choice == UnsavedChangesChoice.Cancel)
        {
            Assert.True(f.Agenda.IsRetrospectiveView);
            Assert.Equal(draftDay, f.Agenda.SelectedDay);
            Assert.Null(f.Agenda.EditingTaskId);
            Assert.Equal("Meu rascunho", f.Agenda.Title);
            Assert.True(f.Agenda.HasUnsavedChanges);
        }
        else
        {
            Assert.True(f.Agenda.IsDayView);
            Assert.Equal(f.Captured.Id, f.Agenda.EditingTaskId);
            Assert.False(f.Agenda.HasUnsavedChanges);
        }

        if (choice == UnsavedChangesChoice.Save)
            Assert.Contains(tasks, task => task.Title == "Meu rascunho");
        else
            Assert.DoesNotContain(tasks, task => task.Title == "Meu rascunho");
    }

    [AvaloniaFact]
    public async Task OpenPendingTask_KeepsTheReflectionDraftOnReturn()
    {
        using var f = await Fixture.OpenAsync();
        f.Find<TextBox>("AgendaReflectionWorkedInput").Text = "Reflexão em andamento";
        Dispatcher.UIThread.RunJobs();

        await f.OpenPendingTaskAsync();

        Assert.True(f.Agenda.IsDayView);
        Assert.Equal(0, f.Prompt.Calls);
        Assert.True(await f.Agenda.ShowRetrospectiveAsync(f.Week));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Reflexão em andamento", f.Find<TextBox>("AgendaReflectionWorkedInput").Text);
        Assert.Null(await f.Services.GetRequiredService<IAgendaReflectionActions>().GetAsync(f.Week));
    }

    [AvaloniaFact]
    public async Task TaskTrashedDuringConfirmation_DoesNotDiscardTheDraftOrSwitchModes()
    {
        using var f = await Fixture.OpenAsync();
        var draftDay = f.Agenda.SelectedDay;
        f.Agenda.Title = "Rascunho protegido";
        f.Prompt.Choice = UnsavedChangesChoice.Discard;
        f.Prompt.BeforeChoice = f.TrashAsync;

        await f.OpenPendingTaskAsync();

        Assert.Equal(1, f.Prompt.Calls);
        Assert.True(f.Agenda.IsRetrospectiveView);
        Assert.Equal(draftDay, f.Agenda.SelectedDay);
        Assert.Equal("Rascunho protegido", f.Agenda.Title);
        Assert.True(f.Agenda.HasUnsavedChanges);
        Assert.Contains("não está mais disponível", f.Agenda.Feedback, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class DraftPrompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public Func<Task>? BeforeChoice { get; set; }
        public int Calls { get; private set; }

        public async Task<UnsavedChangesChoice> ChooseAsync()
        {
            Calls++;
            if (BeforeChoice is { } before) await before();
            return Choice;
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly NotebookTestWorkspace _root = new();
        private readonly Window _window = new() { Height = 850 };
        private AgendaWorkspaceView _view = null!;
        public ServiceProvider Services { get; private set; } = null!;
        public AgendaViewModel Agenda { get; private set; } = null!;
        public IAgendaActions Actions => Services.GetRequiredService<IAgendaActions>();
        public DraftPrompt Prompt { get; } = new();
        public AgendaTaskSnapshot Captured { get; private set; } = null!;
        public DateOnly Week => Captured.PlannedDate;

        public static async Task<Fixture> OpenAsync(int width = 1600)
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f._root.DirectoryPath, unsavedChangesPrompt: f.Prompt);
                f.Agenda = f.Services.GetRequiredService<AgendaViewModel>();
                var day = f.Agenda.Today.AddDays(-7);
                var monday = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
                f.Captured = new(Guid.NewGuid(), "Pendência capturada", "Descrição histórica", monday);
                await f.SaveAsync(f.Captured);
                await f.Agenda.InitializeAsync(f.Agenda.Today);
                Assert.True(await f.Agenda.ShowRetrospectiveAsync(monday));
                Assert.Single(f.Agenda.Retrospective!.Summary!.OpenTasks);
                f._view = new() { DataContext = f.Agenda };
                f._window.Width = width;
                f._window.Content = f._view;
                f._window.Show();
                Dispatcher.UIThread.RunJobs();
                f._window.UpdateLayout();
                return f;
            }
            catch
            {
                f.Dispose();
                throw;
            }
        }

        public async Task SaveAsync(AgendaTaskSnapshot task) =>
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await Actions.SaveAsync(task)).Status);

        public Task TrashAsync() => Services.GetRequiredService<IAgendaTrashActions>().MoveToTrashAsync(Captured.Id, DateTimeOffset.UtcNow);

        public T Find<T>(string name) where T : Control =>
            Assert.Single(_view.GetVisualDescendants().OfType<T>(), control => control.Name == name);

        public Button OpenButton() => Find<Button>("AgendaRetrospectiveOpenTaskButton");

        public async Task OpenPendingTaskAsync()
        {
            var button = OpenButton();
            Assert.True(button.IsEffectivelyVisible && button.IsEffectivelyEnabled);
            if (button.Command is { } command)
            {
                Assert.True(command.CanExecute(button.CommandParameter));
                command.Execute(button.CommandParameter);
            }
            else
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            var elapsed = Stopwatch.StartNew();
            do
            {
                Dispatcher.UIThread.RunJobs();
                if (!Agenda.IsBusy) return;
                await Task.Delay(10);
            }
            while (elapsed.Elapsed < TimeSpan.FromSeconds(5));

            Assert.Fail("A abertura da tarefa não terminou.");
        }

        public void Dispose()
        {
            _window.Close();
            Dispatcher.UIThread.RunJobs();
            Services?.Dispose();
            _root.Dispose();
        }
    }
}


