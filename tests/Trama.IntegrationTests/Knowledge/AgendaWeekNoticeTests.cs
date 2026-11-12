using System.Diagnostics;
using System.Globalization;
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

public sealed class AgendaWeekNoticeTests
{
    [AvaloniaTheory]
    [InlineData(760)]
    [InlineData(1600)]
    public async Task OpeningAgenda_OffersOnlyTheMostRecentEndedWeekWithoutRecordingIt(int width)
    {
        using var f = await Fixture.OpenAsync(width);
        var notice = f.Notice;
        Assert.NotNull(notice);
        Assert.True(notice.IsEffectivelyVisible);
        var text = string.Join(" ", notice.GetVisualDescendants().OfType<TextBlock>().Select(label => label.Text));
        Assert.Contains(f.LastWeek.ToString("dd/MM", CultureInfo.InvariantCulture), text);
        Assert.Contains(f.LastWeek.AddDays(6).ToString("dd/MM", CultureInfo.InvariantCulture), text);
        Assert.True(f.Agenda.IsDayView);
        Assert.Null(f.Agenda.Retrospective!.Report);
        Assert.Null(await f.Reports.GetAsync(f.LastWeek));
        Assert.Null(await f.Reports.GetAsync(f.LastWeek.AddDays(-7)));
    }

    [AvaloniaFact]
    public async Task NoticeClick_OpensItsWeekAndPreservesTheTaskDraft()
    {
        using var f = await Fixture.OpenAsync();
        Assert.True(await f.Agenda.ShowWeekAsync(f.LastWeek.AddDays(-14)));
        f.Agenda.Title = "Rascunho em andamento";

        await f.OpenNoticeAsync();

        Assert.True(f.Agenda.IsRetrospectiveView);
        Assert.Equal(f.LastWeek, f.Agenda.Retrospective!.Report!.StartDate);
        Assert.NotNull(await f.Reports.GetAsync(f.LastWeek));
        Assert.Null(await f.Reports.GetAsync(f.LastWeek.AddDays(-7)));
        Assert.Equal("Rascunho em andamento", f.Agenda.Title);
        Assert.True(f.Agenda.HasUnsavedChanges);
        Assert.True(await f.Agenda.ShowDayAsync());
        Assert.False(f.Notice?.IsEffectivelyVisible ?? false);
    }

    [AvaloniaFact]
    public async Task PreviouslyRecordedWeek_DoesNotOfferTheNoticeAgain()
    {
        using var f = await Fixture.OpenAsync(alreadyRecorded: true);

        Assert.False(f.Notice?.IsEffectivelyVisible ?? false);
        Assert.True(f.Agenda.IsDayView);
        Assert.Null(f.Agenda.Retrospective!.Report);
        Assert.NotNull(await f.Reports.GetAsync(f.LastWeek));
        Assert.Null(await f.Reports.GetAsync(f.LastWeek.AddDays(-7)));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly NotebookTestWorkspace _root = new();
        private readonly Window _window = new() { Height = 850 };
        private ServiceProvider _services = null!;
        private AgendaWorkspaceView _view = null!;

        public AgendaViewModel Agenda { get; private set; } = null!;
        public IAgendaRetrospectiveActions Reports => _services.GetRequiredService<IAgendaRetrospectiveActions>();
        public DateOnly LastWeek { get; private set; }
        public Control? Notice => _view.GetVisualDescendants().OfType<Control>().SingleOrDefault(control => control.Name == "AgendaWeekClosedNotice");

        public static async Task<Fixture> OpenAsync(int width = 1100, bool alreadyRecorded = false)
        {
            var f = new Fixture();
            try
            {
                f._services = await DesktopComposition.CreateAsync(f._root.DirectoryPath);
                f.Agenda = f._services.GetRequiredService<AgendaViewModel>();
                var today = f.Agenda.Today;
                f.LastWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7) - 7);
                if (alreadyRecorded) await f.Reports.GetOrCloseAsync(f.LastWeek);

                await f.Agenda.InitializeAsync(today);
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

        public async Task OpenNoticeAsync()
        {
            var button = Assert.Single(_view.GetVisualDescendants().OfType<Button>(),
                control => control.Name == "AgendaOpenWeekNoticeButton");
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

            Assert.Fail("A abertura da retrospectiva não terminou.");
        }

        public void Dispose()
        {
            _window.Close();
            Dispatcher.UIThread.RunJobs();
            _services?.Dispose();
            _root.Dispose();
        }
    }
}
