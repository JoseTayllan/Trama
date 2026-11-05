using System.Diagnostics;
using Avalonia;
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

public sealed class AgendaNavigationTests
{
    private static readonly DateOnly Day = new(2026, 11, 3);

    [AvaloniaFact]
    public async Task NativeForm_CreatesATaskInTheSameLocalProfile()
    {
        using var f = await Fixture.CreateAsync(initialize: false);
        var navigation = f.Button("AgendaNavigationButton");
        Assert.True(navigation.IsEffectivelyEnabled, "A Agenda deve estar acessível quando seu ViewModel estiver disponível.");
        await f.Agenda.InitializeAsync(Day);
        Assert.True(f.Button("OpenAgendaButton").IsEffectivelyEnabled);
        f.Click("OpenAgendaButton");
        var title = f.Control<TextBox>("AgendaTitleInput"); title.Text = "Criada pela interface";
        f.Control<TextBox>("AgendaDescriptionInput").Text = "Anotação fictícia";
        f.Control<DatePicker>("AgendaDateInput").SelectedDate = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero);
        f.Control<TextBox>("AgendaStartTimeInput").Text = "18:00";
        f.Control<TextBox>("AgendaEndTimeInput").Text = "19:00";
        f.Click("AgendaSaveButton");
        await f.UntilAsync(() => !f.Agenda.IsBusy && f.Agenda.EditingTaskId is null && !f.Agenda.HasUnsavedChanges && f.Agenda.Tasks.Count == 1);
        var saved = Assert.Single(await f.Actions.ListAsync(Day, Day));
        Assert.Equal("Criada pela interface", saved.Title); Assert.Equal("Anotação fictícia", saved.Description);
        Assert.Equal(new TimeOnly(18, 0), saved.StartTime); Assert.Equal(new TimeOnly(19, 0), saved.EndTime);
        Assert.Equal(saved, Assert.IsType<AgendaTaskSnapshot>(Assert.Single(f.Control<ItemsControl>("AgendaTaskList").Items)));
        Assert.True(f.Window.FindControl<Control>("AgendaWorkspace")!.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task SwitchingModules_KeepsBothDraftsAndTheNativeNotebookHistory()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        var prose = f.Prose; prose.Document.Insert(prose.Text.Length, " pendente"); prose.Select(1, 3); f.Pump();
        f.Click("AgendaNavigationButton"); f.Agenda.Title = "Rascunho da agenda";
        f.Click("HomeNavigationButton"); f.Click("AgendaNavigationButton");
        Assert.Equal("Rascunho da agenda", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges);
        f.Click("NotebooksNavigationButton");
        Assert.Same(prose, f.Prose); Assert.Equal(1, prose.SelectionStart); Assert.Equal(3, prose.SelectionLength);
        Assert.True(prose.CanUndo); Assert.Contains("pendente", prose.Text); Assert.Equal(0, f.Prompt.Calls);
    }

    [AvaloniaTheory]
    [InlineData(UnsavedChangesChoice.Cancel, false, false)]
    [InlineData(UnsavedChangesChoice.Discard, true, false)]
    [InlineData(UnsavedChangesChoice.Save, true, true)]
    public async Task ClosingFromHome_ProtectsTheHiddenAgendaDraft(UnsavedChangesChoice choice, bool closed, bool saved)
    {
        using var f = await Fixture.CreateAsync(); f.Click("AgendaNavigationButton");
        f.Agenda.Title = "Tarefa antes de fechar"; f.Click("HomeNavigationButton"); f.Prompt.Choice = choice;
        f.Window.Close();
        await f.UntilAsync(() => f.Prompt.Calls == 1 && (closed ? !f.Window.IsVisible : f.Window.IsEnabled));
        Assert.Equal(!closed, f.Window.IsVisible);
        Assert.Equal(saved ? 1 : 0, (await f.Actions.ListAsync(Day, Day)).Count);
        if (!closed)
        {
            Assert.Equal("Tarefa antes de fechar", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges);
            Assert.True(f.Library.Editor.CanEditContent);
        }
    }

    [AvaloniaFact]
    public async Task CancellingTheLastCloseDecision_KeepsBothDraftsUsable()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        var prose = f.Prose; prose.Document.Insert(prose.Text.Length, " caderno pendente");
        f.Click("AgendaNavigationButton"); f.Agenda.Title = "Agenda pendente";
        f.Prompt.Choices.Enqueue(UnsavedChangesChoice.Discard); f.Prompt.Choices.Enqueue(UnsavedChangesChoice.Cancel);
        f.Window.Close(); await f.UntilAsync(() => f.Prompt.Calls == 2 && f.Window.IsEnabled);
        Assert.True(f.Window.IsVisible); Assert.Equal("Agenda pendente", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges);
        Assert.Contains("caderno pendente", prose.Text); Assert.True(f.Library.Editor.CanEditContent);
        f.Click("NotebooksNavigationButton"); Assert.Same(prose, f.Prose);
        prose.Document.Insert(prose.Text.Length, " ainda editável"); Assert.Contains("ainda editável", f.Library.Editor.Blocks[0].Text);
    }

    [AvaloniaFact]
    public async Task NotebookAutosave_ContinuesWhileAgendaIsVisible()
    {
        using var f = await Fixture.CreateAsync(autoSave: true); f.Click("NotebooksNavigationButton");
        f.Prose.Document.Insert(f.Prose.Text.Length, " automático"); f.Click("AgendaNavigationButton");
        await f.UntilAsync(() => !f.Library.Editor.IsBusy && !f.Library.Editor.HasUnsavedChanges);
        Assert.True(f.Window.FindControl<Control>("AgendaWorkspace")!.IsEffectivelyVisible);
        var page = await f.LibraryActions.LoadAsync(f.Page.Id);
        Assert.Contains("automático", Assert.IsType<NotebookTextBlock>(page!.Blocks[0]).Text);
    }

    [AvaloniaFact]
    public async Task CompactWindow_ProvidesVisibleTitleAndSaveActions()
    {
        using var f = await Fixture.CreateAsync(); f.Window.Width = 860; f.Window.Height = 680; f.Pump();
        f.Click("AgendaNavigationButton");
        foreach (var name in new[] { "AgendaTitleInput", "AgendaSaveButton", "AgendaNewTaskButton" })
        {
            var control = f.Control<Control>(name); Assert.True(control.IsEffectivelyVisible);
            var position = control.TranslatePoint(default, f.Window)!.Value;
            Assert.True(control.Bounds.Width > 0 && control.Bounds.Height > 0);
            Assert.InRange(position.X, 0, f.Window.Bounds.Width - control.Bounds.Width + 1);
            Assert.InRange(position.Y, 0, f.Window.Bounds.Height - control.Bounds.Height + 1);
        }
    }

    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public Queue<UnsavedChangesChoice> Choices { get; } = new();
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(Choices.Count > 0 ? Choices.Dequeue() : Choice); }
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public ServiceProvider Services { get; private set; } = null!;
        public IAgendaActions Actions { get; private set; } = null!;
        public ILibraryActions LibraryActions { get; private set; } = null!;
        public LibraryViewModel Library { get; private set; } = null!;
        public AgendaViewModel Agenda { get; private set; } = null!;
        public NotebookSnapshot Page { get; private set; } = null!;
        public MainWindow Window { get; private set; } = null!;
        public Prompt Prompt { get; } = new();
        public NotebookProseEditor Prose => Assert.Single(Window.GetVisualDescendants().OfType<NotebookProseEditor>());

        public static async Task<Fixture> CreateAsync(bool initialize = true, bool autoSave = false)
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath, autoSave, unsavedChangesPrompt: f.Prompt);
                f.Actions = f.Services.GetRequiredService<IAgendaActions>(); f.LibraryActions = f.Services.GetRequiredService<ILibraryActions>();
                var space = await f.LibraryActions.CreateSpaceAsync("Estudos");
                var book = await f.LibraryActions.CreateNotebookAsync(space.Id, "Curso C#");
                f.Page = new NotebookSnapshot(Guid.NewGuid(), "Página de teste", [new NotebookTextBlock(Guid.NewGuid(), "Um\nDois")]);
                Assert.Equal(NotebookSaveStatus.Saved, (await f.LibraryActions.SaveInNotebookAsync(f.Page, book.Id)).Status);
                f.Library = f.Services.GetRequiredService<LibraryViewModel>(); await f.Library.InitializeAsync();
                Assert.True(await f.Library.SelectSpaceAsync(space.Id)); Assert.True(await f.Library.SelectNotebookAsync(book.Id));
                Assert.True(await f.Library.OpenPageAsync(f.Page.Id));
                f.Agenda = new AgendaViewModel(f.Actions, f.Prompt);
                if (initialize) await f.Agenda.InitializeAsync(Day);
                f.Window = new MainWindow(new MainWindowViewModel { Library = f.Library, Agenda = f.Agenda, Backup = f.Services.GetRequiredService<BackupViewModel>() })
                { Width = 1200, Height = 850 };
                f.Window.Show(); f.Pump(); return f;
            }
            catch { f.Dispose(); throw; }
        }

        public T Control<T>(string name) where T : Control
        {
            var control = Window.FindControl<T>(name) ?? Window.GetVisualDescendants().OfType<T>().FirstOrDefault(item => item.Name == name);
            Assert.True(control is not null, $"Controle da Agenda ausente: {name}."); return control!;
        }
        public Button Button(string name) => Control<Button>(name);
        public void Click(string name)
        {
            var button = Button(name); Assert.True(button.IsEffectivelyEnabled && button.IsEffectivelyVisible);
            if (button.Command is { } command) command.Execute(button.CommandParameter);
            else button.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            Pump();
        }
        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public async Task UntilAsync(Func<bool> ready)
        {
            var timer = Stopwatch.StartNew();
            while (!ready()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15), "A operação não concluiu no prazo."); Pump(); await Task.Delay(10); }
            Pump();
        }
        public void Dispose()
        {
            Prompt.Choices.Clear(); Prompt.Choice = UnsavedChangesChoice.Discard;
            if (Window is not null) { Window.Close(); Dispatcher.UIThread.RunJobs(); }
            Library?.Dispose(); Services?.Dispose(); Root.Dispose();
        }
    }
}
