using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaStartupWeekTests
{
    [AvaloniaFact]
    public async Task RealStartup_ExposesWeekAndReadsOverlappingTasksFromTheSelectedProfile()
    {
        using var session = await StartupSession.OpenAsync(); var vm = session.ViewModel.Agenda!; var day = vm.SelectedDay;
        var task = new AgendaTaskSnapshot(Guid.NewGuid(), "Tarefa fictícia da inicialização", "Mesmo perfil", day.AddDays(-1),
            new TimeOnly(9, 0), new TimeOnly(10, 0), AgendaTaskCategory.Work)
        { PlannedEndDate = day.AddDays(1) };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await session.Actions.SaveAsync(task)).Status);
        Click(session.Button("AgendaNavigationButton"));
        var week = session.Button("AgendaWeekModeButton"); Assert.True(week.IsEffectivelyVisible, "A inicialização real deve habilitar Semana, assim como a composição testada.");
        Assert.True(vm.HasWeekView); Click(week); await UntilAsync(() => vm.IsWeekView && !vm.IsBusy);
        Assert.Contains(vm.Week!.Days.SelectMany(d => d.Entries), entry => entry.Task == task && entry.Date == day);
        Assert.Equal(session.Root.DirectoryPath, session.ViewModel.DataDirectory);
    }

    [AvaloniaFact]
    public async Task RealStartup_SavesThroughTheNativeFormAndShowsTheSameTaskInWeekMode()
    {
        using var session = await StartupSession.OpenAsync(); Click(session.Button("AgendaNavigationButton")); var vm = session.ViewModel.Agenda!;
        session.Control<TextBox>("AgendaTitleInput").Text = "Criada após iniciar o aplicativo";
        Click(session.Button("AgendaSaveButton")); await UntilAsync(() => !vm.IsBusy && vm.EditingTaskId is null && vm.Tasks.Count == 1);
        var saved = Assert.Single(await session.Actions.ListAsync(vm.SelectedDay, vm.SelectedDay));
        var button = session.Button("AgendaWeekModeButton"); Assert.True(button.IsEffectivelyVisible, "Semana deve estar disponível na janela criada por App.");
        Click(button); await UntilAsync(() => vm.IsWeekView && !vm.IsBusy);
        Assert.Equal(saved, Assert.Single(vm.Week!.Days.SelectMany(d => d.Entries)).Task);
        Assert.False(vm.HasUnsavedChanges); Assert.True(session.Button("AgendaTrashButton").IsEffectivelyVisible);
    }

    private static void Click(Button button)
    {
        Assert.True(button.IsEffectivelyVisible && button.IsEffectivelyEnabled);
        if (button.Command is { } command) { Assert.True(command.CanExecute(button.CommandParameter)); command.Execute(button.CommandParameter); }
        else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }
    private static async Task UntilAsync(Func<bool> ready)
    {
        var timer = Stopwatch.StartNew();
        while (!ready()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15), "A inicialização/operação real não terminou."); Dispatcher.UIThread.RunJobs(); await Task.Delay(10); }
        Dispatcher.UIThread.RunJobs();
    }
    private sealed class StartupSession : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        private readonly ClassicDesktopStyleApplicationLifetime _lifetime = new() { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        public MainWindow Window => Assert.IsType<MainWindow>(_lifetime.MainWindow);
        public MainWindowViewModel ViewModel => Assert.IsType<MainWindowViewModel>(Window.DataContext);
        public SqliteAgendaActions Actions => new(Path.Combine(Root.DirectoryPath, "trama.db"));
        public static async Task<StartupSession> OpenAsync()
        {
            var session = new StartupSession();
            try
            {
                session._lifetime.Args = ["--data-directory", session.Root.DirectoryPath];
                var app = new App { ApplicationLifetime = session._lifetime };
                app.OnFrameworkInitializationCompleted(); session.Window.Show();
                await UntilAsync(() => session.ViewModel.Status == "Pronto · armazenamento local");
                Assert.NotNull(session.ViewModel.Agenda); return session;
            }
            catch { session.Dispose(); throw; }
        }
        public T Control<T>(string name) where T : Control
        {
            var value = Window.GetVisualDescendants().OfType<T>().SingleOrDefault(c => c.Name == name); Assert.NotNull(value); return value;
        }
        public Button Button(string name) => Control<Button>(name);
        public void Dispose()
        {
            if (_lifetime.MainWindow is { } window) { window.DataContext = null; foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
            _lifetime.Dispose(); Dispatcher.UIThread.RunJobs(); Root.Dispose();
        }
    }
}
