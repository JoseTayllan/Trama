using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaStartupRetrospectiveTests
{
    [AvaloniaFact]
    public async Task RealStartup_ProvidesRetrospectiveWithoutCreatingClosuresAutomatically()
    {
        using var session = await Session.OpenAsync(); var agenda = session.ViewModel.Agenda!;
        Assert.True(Property<bool>(agenda, "HasRetrospectiveView")); Assert.False(Property<bool>(agenda, "IsRetrospectiveView"));
        var loader = Property<AgendaRetrospectiveViewModel>(agenda, "Retrospective"); Assert.Null(loader.Report); Assert.False(loader.IsBusy);
        Assert.Empty(await session.Closures.ListAsync(DateOnly.MinValue, DateOnly.MaxValue));
        Assert.Equal(session.Root.DirectoryPath, session.ViewModel.DataDirectory);
    }

    [AvaloniaFact]
    public async Task RealStartup_OpensClosureInTheSelectedProfileAndKeepsBothDrafts()
    {
        using var session = await Session.OpenAsync(); var agenda = session.ViewModel.Agenda!;
        var date = agenda.Today.AddDays(-7); var start = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var task = new AgendaTaskSnapshot(Guid.NewGuid(), "Captura do perfil escolhido", "Fictícia", start);
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await session.Actions.SaveAsync(task)).Status);
        var editor = session.ViewModel.Library!.Editor; editor.Title = "Texto ainda não salvo"; agenda.Title = "Rascunho da tarefa";
        var method = typeof(AgendaViewModel).GetMethod("ShowRetrospectiveAsync"); Assert.NotNull(method);
        Assert.True(await Assert.IsAssignableFrom<Task<bool>>(method.Invoke(agenda, [start, CancellationToken.None])));
        Assert.True(Property<bool>(agenda, "IsRetrospectiveView")); var stored = await session.Closures.GetAsync(start); Assert.NotNull(stored);
        Assert.Equal(task, Assert.Single(stored.Tasks).Task); Assert.Same(editor, session.ViewModel.Library.Editor);
        Assert.Equal("Texto ainda não salvo", editor.Title); Assert.True(editor.HasUnsavedChanges);
        Assert.Equal("Rascunho da tarefa", agenda.Title); Assert.True(agenda.HasUnsavedChanges);
        Assert.Equal("Pronto · armazenamento local", session.ViewModel.Status);
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(name); Assert.NotNull(property); return Assert.IsAssignableFrom<T>(property.GetValue(source));
    }
    private sealed class Session : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        private readonly ClassicDesktopStyleApplicationLifetime _lifetime = new() { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        public MainWindow Window => Assert.IsType<MainWindow>(_lifetime.MainWindow);
        public MainWindowViewModel ViewModel => Assert.IsType<MainWindowViewModel>(Window.DataContext);
        public SqliteAgendaActions Actions => new(Path.Combine(Root.DirectoryPath, "trama.db"));
        public IAgendaRetrospectiveActions Closures => new SqliteAgendaRetrospectiveActions(Path.Combine(Root.DirectoryPath, "trama.db"), TimeProvider.System);
        public static async Task<Session> OpenAsync()
        {
            var session = new Session();
            try
            {
                session._lifetime.Args = ["--data-directory", session.Root.DirectoryPath];
                var app = new App { ApplicationLifetime = session._lifetime }; app.OnFrameworkInitializationCompleted(); session.Window.Show();
                var timer = Stopwatch.StartNew();
                while (session.ViewModel.Status != "Pronto · armazenamento local")
                {
                    Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15), "A inicialização real não terminou.");
                    Dispatcher.UIThread.RunJobs(); await Task.Delay(10);
                }
                Dispatcher.UIThread.RunJobs(); Assert.NotNull(session.ViewModel.Agenda); return session;
            }
            catch { session.Dispose(); throw; }
        }
        public void Dispose()
        {
            if (_lifetime.MainWindow is { } window) { window.DataContext = null; foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
            _lifetime.Dispose(); Dispatcher.UIThread.RunJobs(); Root.Dispose();
        }
    }
}
