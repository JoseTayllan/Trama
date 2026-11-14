using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class HomeAgendaSummaryTests
{
    [AvaloniaFact]
    public async Task HomeShowsTodayAndUpcomingOpenTasksFromTheLocalProfile()
    {
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var actions = services.GetRequiredService<IAgendaActions>();
            await SaveTaskAsync(actions, "Ler capítulo hoje", today);
            await SaveTaskAsync(actions, "Reunião amanhã", today.AddDays(1));
            await SaveTaskAsync(actions, "Entrega no sétimo dia", today.AddDays(7));
            await SaveTaskAsync(actions, "Compromisso fora do período", today.AddDays(8));
            await SaveTaskAsync(actions, "Compromisso já concluído", today.AddDays(2), AgendaTaskStatus.Completed);
            var deleted = await SaveTaskAsync(actions, "Tarefa removida", today);
            await services.GetRequiredService<IAgendaTrashActions>().MoveToTrashAsync(deleted, DateTimeOffset.UtcNow);
        });

        Assert.Equal(["Ler capítulo hoje"], await f.ReadTitlesAsync("HomeAgendaTodayTasks"));
        Assert.Equal(["Reunião amanhã", "Entrega no sétimo dia"], await f.ReadTitlesAsync("HomeAgendaUpcomingTasks", count: 2));
        Assert.True(f.Find<Button>("OpenAgendaButton").IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task ReturningHomeRefreshesTodayWithoutChangingTheSelectedDayOrDraft()
    {
        using var f = await Fixture.CreateAsync((services, today) =>
            SaveTaskAsync(services.GetRequiredService<IAgendaActions>(), "Tarefa de hoje", today));
        Assert.Equal(["Tarefa de hoje"], await f.ReadTitlesAsync("HomeAgendaTodayTasks"));

        f.Main.ShowAgendaCommand.Execute(null);
        var tomorrow = f.Agenda.Today.AddDays(1);
        Assert.True(await f.Agenda.ChangeDayAsync(tomorrow));
        f.Agenda.Title = "Rascunho de amanhã";
        await SaveTaskAsync(f.Actions, "Nova tarefa de hoje", f.Agenda.Today);
        f.Main.ShowHomeCommand.Execute(null);

        var titles = await f.ReadTitlesAsync("HomeAgendaTodayTasks", count: 2);
        Assert.Contains("Tarefa de hoje", titles);
        Assert.Contains("Nova tarefa de hoje", titles);
        Assert.Equal(tomorrow, f.Agenda.SelectedDay);
        Assert.Equal("Rascunho de amanhã", f.Agenda.Title);
        Assert.True(f.Agenda.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task EmptyProfileShowsAnEmptySummaryAndKeepsTheAgendaShortcut()
    {
        using var f = await Fixture.CreateAsync();

        Assert.Empty(await f.ReadTitlesAsync("HomeAgendaTodayTasks", count: 0));
        Assert.Empty(await f.ReadTitlesAsync("HomeAgendaUpcomingTasks", count: 0));
        var empty = f.Find<TextBlock>("HomeAgendaEmpty");
        await f.WaitAsync(() => empty.IsEffectivelyVisible);
        Assert.False(string.IsNullOrWhiteSpace(empty.Text));
        var open = f.Find<Button>("OpenAgendaButton");
        Assert.True(open.IsEffectivelyVisible && open.IsEffectivelyEnabled);
        Assert.NotNull(open.Command);
        open.Command.Execute(open.CommandParameter);
        Assert.True(f.Main.IsAgendaActive);
    }

    [AvaloniaFact]
    public async Task HomeAgendaShowsTheDateAndReadableTaskDetails()
    {
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var actions = services.GetRequiredService<IAgendaActions>();
            var task = new AgendaTaskSnapshot(Guid.NewGuid(), "Estudar interfaces", "", today, new TimeOnly(14, 0),
                Category: AgendaTaskCategory.Study, Status: AgendaTaskStatus.InProgress);
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions.SaveAsync(task)).Status);
            await SaveTaskAsync(actions, "Organizar referências", today);
        });

        Assert.Equal(["Estudar interfaces", "Organizar referências"],
            await f.ReadTitlesAsync("HomeAgendaTodayTasks", count: 2));
        var header = f.Find<TextBlock>("HomeDateLabel");
        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        Assert.Equal(f.Agenda.Today.ToString("dddd, dd 'de' MMMM", culture),
            header.Text, ignoreCase: true);

        var list = f.Find<ItemsControl>("HomeAgendaTodayTasks");
        var labels = list.GetVisualDescendants().OfType<TextBlock>().Where(label => label.IsEffectivelyVisible).ToArray();
        Assert.Contains(labels, label => label.Text?.Contains("14:00", StringComparison.Ordinal) == true);
        Assert.Contains(labels, label => label.Text == "Sem horário");
        Assert.Contains(labels, label => label.Text == "Estudo");
        Assert.Contains(labels, label => label.Text?.Contains("Em andamento", StringComparison.Ordinal) == true);
    }

    [AvaloniaFact]
    public async Task HomeWeeklySummaryUsesTheCurrentWeekAndActualTaskCounts()
    {
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var actions = services.GetRequiredService<IAgendaActions>();
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            await SaveTaskAsync(actions, "Concluída nesta semana", monday, AgendaTaskStatus.Completed);
            await SaveTaskAsync(actions, "Planejada nesta semana", monday.AddDays(6));
            await SaveTaskAsync(actions, "Concluída na semana passada", monday.AddDays(-1), AgendaTaskStatus.Completed);
            await SaveTaskAsync(actions, "Planejada para outra semana", monday.AddDays(7));
        });

        var summary = f.Find<TextBlock>("HomeAgendaWeekCompletion");
        await f.WaitAsync(() => summary.IsEffectivelyVisible && summary.Text == "1 de 2");
        Assert.True(f.Main.IsHomeActive);
    }

    [AvaloniaFact]
    public async Task HomeWeeklyHours_CountOnlyScheduledTimeInsideTheWeek()
    {
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var actions = services.GetRequiredService<IAgendaActions>();
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var timed = new AgendaTaskSnapshot(Guid.NewGuid(), "Sessão", "", monday, new(9, 0), new(10, 30),
                Status: AgendaTaskStatus.Completed);
            var scheduled = new[]
            {
                timed,
                timed with { Id = Guid.NewGuid(), PlannedDate = monday.AddDays(-1), PlannedEndDate = monday, StartTime = new(23, 0), EndTime = new(1, 0) },
                timed with { Id = Guid.NewGuid(), PlannedDate = monday.AddDays(6), PlannedEndDate = monday.AddDays(7), StartTime = new(23, 0), EndTime = new(2, 0) },
                timed with { Id = Guid.NewGuid(), EndTime = null },
                timed with { Id = Guid.NewGuid(), PlannedDate = monday.AddDays(7) }
            };
            foreach (var task in scheduled)
                Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions.SaveAsync(task)).Status);
        });

        var planned = f.Find<TextBlock>("HomeAgendaWeekPlannedHours");
        await f.WaitAsync(() => planned.IsEffectivelyVisible && planned.Text == "3 h 30 min");
        var actual = f.Find<TextBlock>("HomeAgendaWeekActualHours");
        Assert.True(actual.IsEffectivelyVisible);
        Assert.Equal("Sem registro", actual.Text);
    }

    [AvaloniaFact]
    public async Task HomeWeeklyHours_WithoutCompleteIntervalsShowsZeroPlannedHours()
    {
        using var f = await Fixture.CreateAsync((services, today) =>
            SaveTaskAsync(services.GetRequiredService<IAgendaActions>(), "Tarefa sem horário", today));

        var planned = f.Find<TextBlock>("HomeAgendaWeekPlannedHours");
        await f.WaitAsync(() => planned.IsEffectivelyVisible && planned.Text == "0 h");
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TodayNotesShortcut_OpensTheLinkedPageOrOffersAChoice(bool multiple)
    {
        var target = new NotebookSnapshot(Guid.NewGuid(), "Interfaces em C#",
            [new NotebookTextBlock(Guid.NewGuid(), "Anotações de estudo")]);
        var other = target with { Id = Guid.NewGuid(), Title = "Referências" };
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var actions = services.GetRequiredService<IAgendaActions>();
            var id = await SaveTaskAsync(actions, "Revisar C#", today);
            await SaveTaskAsync(actions, "Tarefa sem anotações", today);
            var pages = services.GetRequiredService<ILibraryActions>();
            Assert.Equal(NotebookSaveStatus.Saved, (await pages.SaveInNotebookAsync(target, null)).Status);
            var links = Assert.IsAssignableFrom<IAgendaPageLinks>(actions);
            await links.AddPageLinkAsync(id, target.Id);
            if (multiple)
            {
                Assert.Equal(NotebookSaveStatus.Saved, (await pages.SaveInNotebookAsync(other, null)).Status);
                await links.AddPageLinkAsync(id, other.Id);
            }
        });

        await f.ReadTitlesAsync("HomeAgendaTodayTasks", count: 2);
        var list = f.Find<ItemsControl>("HomeAgendaTodayTasks");
        var button = Assert.Single(list.GetVisualDescendants().OfType<Button>(),
            item => item.Name == "HomeAgendaOpenNotesButton" && item.IsEffectivelyVisible);
        Assert.True(button.IsEffectivelyEnabled);
        Assert.Contains(button.GetVisualDescendants().OfType<TextBlock>(), label => label.Text == "Abrir anotações");

        if (button.Command is { } command)
        {
            Assert.True(command.CanExecute(button.CommandParameter));
            command.Execute(button.CommandParameter);
        }
        else
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        if (multiple)
        {
            await f.WaitAsync(() => button.Flyout is MenuFlyout { IsOpen: true });
            Assert.True(f.Main.IsHomeActive);
            var menu = Assert.IsType<MenuFlyout>(button.Flyout);
            Assert.Equal(2, menu.Items.Count);
            var item = Assert.Single(menu.Items.OfType<MenuItem>(), entry => entry.Header as string == target.Title);
            if (item.Command is { } open) open.Execute(item.CommandParameter);
            else item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        await f.WaitAsync(() => f.Main.IsNotebooksActive);
        Assert.Equal(target.Id, f.Main.Library!.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task SavingACompletedTask_RecordsActualTimeWithoutOverwritingThePlannedEnd()
    {
        AgendaTaskSnapshot? task = null;
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            task = new(Guid.NewGuid(), "Preparar material", "", today.AddDays(-2), new TimeOnly(9, 0), new TimeOnly(10, 0))
            {
                PlannedEndDate = today.AddDays(-1)
            };
            Assert.Equal(AgendaTaskSaveStatus.Saved,
                (await services.GetRequiredService<IAgendaActions>().SaveAsync(task)).Status);
        });

        var original = Assert.IsType<AgendaTaskSnapshot>(task);
        Assert.True(await f.Agenda.EditTaskAsync(original.Id));
        f.Agenda.Status = AgendaTaskStatus.Completed;
        var before = DateTimeOffset.UtcNow;
        Assert.True(await f.Agenda.SaveAsync());
        var after = DateTimeOffset.UtcNow;

        var completedAt = await f.Completions.GetLastCompletedAtAsync(original.Id);
        Assert.NotNull(completedAt);
        Assert.InRange(completedAt.Value, before, after);
        Assert.Equal(original with { Status = AgendaTaskStatus.Completed }, await f.Actions.GetByIdAsync(original.Id));

        f.Agenda.Description = "Ajuste na descrição";
        Assert.True(await f.Agenda.SaveAsync());
        Assert.Equal(completedAt, await f.Completions.GetLastCompletedAtAsync(original.Id));

        f.Agenda.Status = AgendaTaskStatus.InProgress;
        Assert.True(await f.Agenda.SaveAsync());
        Assert.Equal(completedAt, await f.Completions.GetLastCompletedAtAsync(original.Id));

        f.Agenda.Status = AgendaTaskStatus.Completed;
        var reopenedBefore = DateTimeOffset.UtcNow;
        Assert.True(await f.Agenda.SaveAsync());
        var completedAgain = await f.Completions.GetLastCompletedAtAsync(original.Id);
        Assert.NotNull(completedAgain);
        Assert.InRange(completedAgain.Value, reopenedBefore, DateTimeOffset.UtcNow);
        Assert.Equal(original with { Status = AgendaTaskStatus.Completed, Description = "Ajuste na descrição" },
            await f.Actions.GetByIdAsync(original.Id));
    }

    [AvaloniaFact]
    public async Task HomeActualHours_RecordsTimeWhenTaskIsStartedAndThenCompleted()
    {
        var taskId = Guid.NewGuid();
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var task = new AgendaTaskSnapshot(taskId, "Estudar C#", "", today, new(8, 0), new(10, 0));
            Assert.Equal(AgendaTaskSaveStatus.Saved,
                (await services.GetRequiredService<IAgendaActions>().SaveAsync(task)).Status);
        });
        f.Main.ShowAgendaCommand.Execute(null);
        Assert.True(await f.Agenda.EditTaskAsync(taskId));
        f.Agenda.Status = AgendaTaskStatus.InProgress;
        Assert.True(await f.Agenda.SaveAsync());

        f.Main.ShowHomeCommand.Execute(null);
        var actual = f.Find<TextBlock>("HomeAgendaWeekActualHours");
        await f.WaitAsync(() => f.Main.HasHomeWeekSummary);
        Assert.Equal("Sem registro", actual.Text);

        f.Main.ShowAgendaCommand.Execute(null);
        f.Agenda.Status = AgendaTaskStatus.Completed;
        Assert.True(await f.Agenda.SaveAsync());
        f.Main.ShowHomeCommand.Execute(null);

        await f.WaitAsync(() => f.Main.HasHomeWeekSummary);
        Assert.Equal("0 min", actual.Text);
        Assert.Equal("2 h", f.Find<TextBlock>("HomeAgendaWeekPlannedHours").Text);
    }

    [AvaloniaFact]
    public async Task HomeActualHours_CompletionWithoutRecordedStartDoesNotUsePlannedHours()
    {
        var taskId = Guid.NewGuid();
        using var f = await Fixture.CreateAsync(async (services, today) =>
        {
            var task = new AgendaTaskSnapshot(taskId, "Revisar material", "", today, new(8, 0), new(10, 0));
            Assert.Equal(AgendaTaskSaveStatus.Saved,
                (await services.GetRequiredService<IAgendaActions>().SaveAsync(task)).Status);
        });
        f.Main.ShowAgendaCommand.Execute(null);
        Assert.True(await f.Agenda.EditTaskAsync(taskId));
        f.Agenda.Status = AgendaTaskStatus.Completed;
        Assert.True(await f.Agenda.SaveAsync());
        Assert.NotNull(await f.Completions.GetLastCompletedAtAsync(taskId));
        f.Main.ShowHomeCommand.Execute(null);

        var actual = f.Find<TextBlock>("HomeAgendaWeekActualHours");
        await f.WaitAsync(() => f.Main.HasHomeWeekSummary);
        Assert.Equal("Sem registro", actual.Text);
        Assert.Equal("2 h", f.Find<TextBlock>("HomeAgendaWeekPlannedHours").Text);
    }

    private static async Task<Guid> SaveTaskAsync(IAgendaActions actions, string title, DateOnly day,
        AgendaTaskStatus status = AgendaTaskStatus.Pending)
    {
        var id = Guid.NewGuid();
        Assert.Equal(AgendaTaskSaveStatus.Saved,
            (await actions.SaveAsync(new(id, title, "", day, Status: status))).Status);
        return id;
    }

    private sealed class DiscardPrompt : IUnsavedChangesPrompt
    {
        public Task<UnsavedChangesChoice> ChooseAsync() => Task.FromResult(UnsavedChangesChoice.Discard);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly NotebookTestWorkspace _root = new();
        private ServiceProvider? _services;
        private MainWindow? _window;
        public MainWindowViewModel Main { get; } = new();
        public AgendaViewModel Agenda => Main.Agenda!;
        public IAgendaActions Actions => _services!.GetRequiredService<IAgendaActions>();
        public IAgendaCompletionActions Completions => _services!.GetRequiredService<IAgendaCompletionActions>();

        public static async Task<Fixture> CreateAsync(Func<ServiceProvider, DateOnly, Task>? seed = null)
        {
            var f = new Fixture();
            try
            {
                f._services = await DesktopComposition.CreateAsync(f._root.DirectoryPath, unsavedChangesPrompt: new DiscardPrompt());
                var agenda = f._services.GetRequiredService<AgendaViewModel>();
                var library = f._services.GetRequiredService<LibraryViewModel>();
                if (seed is not null) await seed(f._services, agenda.Today);
                await library.InitializeAsync();
                await agenda.InitializeAsync(agenda.Today);
                f.Main.Library = library;
                f.Main.Agenda = agenda;
                f._window = new MainWindow(f.Main) { Width = 1200, Height = 850 };
                f._window.Show();
                f.Pump();
                return f;
            }
            catch
            {
                f.Dispose();
                throw;
            }
        }

        public async Task<string[]> ReadTitlesAsync(string name, int count = 1)
        {
            var list = Find<ItemsControl>(name);
            await WaitAsync(() => list.Items.Count >= count);
            return list.Items.OfType<AgendaTaskSnapshot>().Select(task => task.Title).ToArray();
        }

        public T Find<T>(string name) where T : Control
        {
            Pump();
            return Assert.Single(_window!.GetVisualDescendants().OfType<T>(), control => control.Name == name);
        }

        public async Task WaitAsync(Func<bool> ready)
        {
            var elapsed = Stopwatch.StartNew();
            while (!ready())
            {
                Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5), "O resumo da Agenda não foi atualizado.");
                await Task.Delay(10);
                Pump();
            }
            Pump();
        }

        private void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            _window?.UpdateLayout();
        }

        public void Dispose()
        {
            _window?.Close();
            Dispatcher.UIThread.RunJobs();
            _services?.Dispose();
            _root.Dispose();
        }
    }
}
