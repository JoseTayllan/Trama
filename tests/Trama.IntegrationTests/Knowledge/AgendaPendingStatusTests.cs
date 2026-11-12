using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaPendingStatusTests
{
    [AvaloniaFact]
    public async Task ReturningAfterCompletion_ShowsCurrentStatusAndPreservesWeeklyMetrics()
    {
        using var f = await Fixture.OpenAsync();
        var original = f.Agenda.Retrospective!.Report!;
        Assert.True(await f.Agenda.ShowDayAsync());
        Assert.True(await f.Agenda.EditTaskAsync(f.Captured.Id));
        f.Agenda.Status = AgendaTaskStatus.Completed;
        Assert.True(await f.Agenda.SaveAsync());

        Assert.True(await f.Agenda.ShowRetrospectiveAsync(f.Captured.PlannedDate));

        var row = f.RowText();
        Assert.Contains("Atual: Concluída", row);
        Assert.Contains("No fechamento: Pendente", row);
        var report = f.Agenda.Retrospective.Report!;
        Assert.Equal(original.PendingCount, report.PendingCount);
        Assert.Equal(original.CompletedInWeekCount, report.CompletedInWeekCount);
        Assert.Equal(original.CompletionPercentage, report.CompletionPercentage);
        Assert.Equal(AgendaTaskStatus.Pending, Assert.Single(report.Tasks).Task.Status);
    }

    [AvaloniaFact]
    public async Task Refresh_ShowsTheCurrentStatusEvenAfterRescheduling()
    {
        using var f = await Fixture.OpenAsync();
        await f.SaveAsync(f.Captured with { Status = AgendaTaskStatus.InProgress, PlannedDate = f.Agenda.Today.AddDays(2) });

        Assert.True(await f.Agenda.RefreshRetrospectiveAsync());

        var row = f.RowText();
        Assert.Contains("Atual: Em andamento", row);
        Assert.Contains("No fechamento: Pendente", row);
    }

    [AvaloniaFact]
    public async Task Refresh_IdentifiesUnavailableTasksAndRecoversTheirStatusAfterRestore()
    {
        using var f = await Fixture.OpenAsync();
        var trash = f.Services.GetRequiredService<IAgendaTrashActions>();
        await trash.MoveToTrashAsync(f.Captured.Id, DateTimeOffset.UtcNow);

        Assert.True(await f.Agenda.RefreshRetrospectiveAsync());

        var unavailable = f.RowText();
        Assert.Contains("Atual: Indisponível", unavailable);
        Assert.Contains("No fechamento: Pendente", unavailable);

        await trash.RestoreAsync(f.Captured.Id);
        Assert.True(await f.Agenda.RefreshRetrospectiveAsync());
        Assert.Contains("Atual: Pendente", f.RowText());
    }

    private sealed class Fixture : IDisposable
    {
        private readonly NotebookTestWorkspace _root = new();
        private readonly Window _window = new() { Width = 1100, Height = 850 };
        private AgendaWorkspaceView _view = null!;
        public ServiceProvider Services { get; private set; } = null!;
        public AgendaViewModel Agenda { get; private set; } = null!;
        public AgendaTaskSnapshot Captured { get; private set; } = null!;

        public static async Task<Fixture> OpenAsync()
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f._root.DirectoryPath);
                f.Agenda = f.Services.GetRequiredService<AgendaViewModel>();
                var previous = f.Agenda.Today.AddDays(-7);
                var monday = previous.AddDays(-(((int)previous.DayOfWeek + 6) % 7));
                f.Captured = new(Guid.NewGuid(), "Pendência da semana", "", monday);
                await f.SaveAsync(f.Captured);
                await f.Agenda.InitializeAsync(f.Agenda.Today);
                Assert.True(await f.Agenda.ShowRetrospectiveAsync(monday));
                f._view = new() { DataContext = f.Agenda };
                f._window.Content = f._view;
                f._window.Show();
                return f;
            }
            catch
            {
                f.Dispose();
                throw;
            }
        }

        public async Task SaveAsync(AgendaTaskSnapshot task) =>
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await Services.GetRequiredService<IAgendaActions>().SaveAsync(task)).Status);

        public string RowText()
        {
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
            var row = Assert.Single(_view.GetVisualDescendants().OfType<Control>(),
                control => control.IsEffectivelyVisible && control.Classes.Contains("retrospective-open-task"));
            return string.Join("\n", row.GetVisualDescendants().OfType<TextBlock>()
                .Where(label => label.IsEffectivelyVisible).Select(label => label.Text));
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
