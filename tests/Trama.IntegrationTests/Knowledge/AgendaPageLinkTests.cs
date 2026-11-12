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

public sealed class AgendaPageLinkTests
{
    [AvaloniaFact]
    public async Task SavedTask_PageLinkAndUnlinkSurviveReopeningTheProfile()
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();

        await f.ReloadAgendaAsync();
        Assert.Contains(f.Target.Title, f.Find<TextBlock>("AgendaLinkedPageLabel").Text);

        f.Click("AgendaUnlinkPageButton");
        await f.WaitAsync(() => !f.Agenda.IsBusy);
        await f.ReloadAgendaAsync();

        Assert.False(f.Find<Button>("AgendaOpenLinkedPageButton").IsEffectivelyVisible);
        Assert.True(f.Find<Button>("AgendaLinkPageButton").IsEffectivelyEnabled);
        Assert.Contains(await f.Services.GetRequiredService<IKnowledgeDiscovery>().SearchAsync(f.Target.Title),
            page => page.PageId == f.Target.Id);
    }

    [AvaloniaFact]
    public async Task RenamedAndMovedPage_OpensByIdInItsCurrentNotebook()
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        var actions = f.Services.GetRequiredService<ILibraryActions>();
        var otherSpace = await actions.CreateSpaceAsync("Projetos");
        var otherNotebook = await actions.CreateNotebookAsync(otherSpace.Id, "Referências");
        var management = f.Services.GetRequiredService<ILibraryManagementActions>();
        await management.RenameAsync(LibraryEntryKind.Page, f.Target.Id, "Título atualizado");
        await management.MovePageAsync(f.Target.Id, otherNotebook.Id);

        f.Agenda.Description = "Edição de tarefa em andamento";
        f.Click("AgendaOpenLinkedPageButton");
        await f.WaitAsync(() => f.Main.IsNotebooksActive);

        Assert.Equal(f.Target.Id, f.Library.Editor.PageId);
        Assert.Equal(otherSpace.Id, f.Library.SelectedSpaceId);
        Assert.Equal(otherNotebook.Id, f.Library.SelectedNotebookId);
        Assert.Equal("Título atualizado", f.Library.Editor.Title);
        Assert.Equal("Edição de tarefa em andamento", f.Agenda.Description);
        Assert.True(f.Agenda.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task OpeningLinkedPage_RespectsCancellationOfNotebookDraft()
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        f.Library.Editor.Title = "Rascunho de outra página";

        f.Click("AgendaOpenLinkedPageButton");
        await f.WaitAsync(() => f.Prompt.Calls == 1 && f.Library.CanNavigate && !f.Agenda.IsBusy);

        Assert.True(f.Main.IsAgendaActive);
        Assert.Equal(f.OtherPage.Id, f.Library.Editor.PageId);
        Assert.Equal("Rascunho de outra página", f.Library.Editor.Title);
        Assert.True(f.Library.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task TrashedLinkedPage_ReportsUnavailabilityAndOpensAfterRestore()
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        var trash = f.Services.GetRequiredService<ILibraryTrashActions>();
        await trash.MoveToTrashAsync(LibraryEntryKind.Page, f.Target.Id);
        f.Click("AgendaOpenLinkedPageButton");
        await f.WaitAsync(() => !f.Agenda.IsBusy && f.Agenda.Feedback.Contains("indisponível", StringComparison.OrdinalIgnoreCase));

        Assert.True(f.Main.IsAgendaActive);
        Assert.Equal(f.OtherPage.Id, f.Library.Editor.PageId);

        await trash.RestoreAsync(LibraryEntryKind.Page, f.Target.Id);
        f.Click("AgendaOpenLinkedPageButton");
        await f.WaitAsync(() => f.Main.IsNotebooksActive);
        Assert.Equal(f.Target.Id, f.Library.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task MultipleLinks_CanBeAddedAndSelectedAfterReopeningTheTask()
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        await f.LinkAnotherPageAsync();

        await f.ReloadAgendaAsync();

        var selector = f.Find<ComboBox>("AgendaLinkedPagePicker");
        Assert.True(selector.IsEffectivelyVisible);
        Assert.Equal(2, selector.Items.Count);
        Assert.Contains(selector.Items.OfType<AgendaPageLink>(), link => link.PageId == f.Target.Id);
        Assert.Contains(selector.Items.OfType<AgendaPageLink>(), link => link.PageId == f.OtherPage.Id);
        f.SelectLinkedPage(f.OtherPage.Id);
        f.Click("AgendaOpenLinkedPageButton");
        await f.WaitAsync(() => f.Main.IsNotebooksActive);
        Assert.Equal(f.OtherPage.Id, f.Library.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task UnlinkingTheSelectedPage_KeepsTheOtherPageAndItsLinksFromOtherTasks()
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        await f.LinkAnotherPageAsync();
        var actions = f.Services.GetRequiredService<IAgendaActions>();
        var links = Assert.IsAssignableFrom<IAgendaPageLinks>(actions);
        var otherTaskId = Guid.NewGuid();
        Assert.Equal(AgendaTaskSaveStatus.Saved,
            (await actions.SaveAsync(new(otherTaskId, "Revisar material", "", f.Agenda.Today.AddDays(1)))).Status);
        await links.AddPageLinkAsync(otherTaskId, f.OtherPage.Id);

        f.SelectLinkedPage(f.OtherPage.Id);
        f.Click("AgendaUnlinkPageButton");
        await f.WaitAsync(() => !f.Agenda.IsBusy);
        await f.ReloadAgendaAsync();

        Assert.Equal(f.Target.Id, Assert.Single(await links.ListPageLinksAsync(f.Agenda.EditingTaskId!.Value)).PageId);
        Assert.Equal(f.OtherPage.Id, Assert.Single(await links.ListPageLinksAsync(otherTaskId)).PageId);
        Assert.Contains(f.Target.Title, f.Find<TextBlock>("AgendaLinkedPageLabel").Text);
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task SavedTasks_CanShareAPageWithoutDuplicateLinks(int daysApart)
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        var actions = f.Services.GetRequiredService<IAgendaActions>();
        var links = Assert.IsAssignableFrom<IAgendaPageLinks>(actions);
        var otherTaskId = Guid.NewGuid();
        Assert.Equal(AgendaTaskSaveStatus.Saved,
            (await actions.SaveAsync(new(otherTaskId, "Outra tarefa", "", f.Agenda.Today.AddDays(daysApart)))).Status);

        await links.AddPageLinkAsync(otherTaskId, f.Target.Id);
        await links.AddPageLinkAsync(otherTaskId, f.Target.Id);

        Assert.Equal(f.Target.Id, Assert.Single(await links.ListPageLinksAsync(otherTaskId)).PageId);
        Assert.Equal(f.Target.Id, Assert.Single(await links.ListPageLinksAsync(f.Agenda.EditingTaskId!.Value)).PageId);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CardPageShortcut_AppearsAfterLinkingAndOpensTheOnlyPage(bool weekly)
    {
        using var f = await Fixture.OpenAsync();
        if (weekly) Assert.True(await f.Agenda.ShowWeekAsync(f.Agenda.Today));
        Assert.Empty(f.PageShortcuts());
        await f.LinkPageAsync();
        f.Agenda.Description = "Rascunho da Agenda";

        var shortcut = Assert.Single(f.PageShortcuts());
        Assert.Equal("Abrir páginas vinculadas", Avalonia.Automation.AutomationProperties.GetName(shortcut));
        f.ClickPageShortcut(shortcut);
        await f.WaitAsync(() => f.Main.IsNotebooksActive);

        Assert.Equal(f.Target.Id, f.Library.Editor.PageId);
        Assert.Equal("Rascunho da Agenda", f.Agenda.Description);
        Assert.True(f.Agenda.HasUnsavedChanges);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CardPageShortcut_OffersEveryLinkedPageWhenThereIsMoreThanOne(bool weekly)
    {
        using var f = await Fixture.OpenAsync();
        await f.LinkPageAsync();
        await f.LinkAnotherPageAsync();
        if (weekly) Assert.True(await f.Agenda.ShowWeekAsync(f.Agenda.Today));

        var shortcut = Assert.Single(f.PageShortcuts());
        f.ClickPageShortcut(shortcut);
        await f.WaitAsync(() => shortcut.Flyout is MenuFlyout { IsOpen: true });

        Assert.True(f.Main.IsAgendaActive);
        var items = Assert.IsType<MenuFlyout>(shortcut.Flyout).Items.OfType<MenuItem>().ToArray();
        Assert.Equal(2, items.Length);
        Assert.Contains(items, item => item.Header as string == f.OtherPage.Title);
        var selected = Assert.Single(items, item => item.Header as string == f.Target.Title);
        if (selected.Command is { } command)
        {
            Assert.True(command.CanExecute(selected.CommandParameter));
            command.Execute(selected.CommandParameter);
        }
        else
        {
            selected.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        await f.WaitAsync(() => f.Main.IsNotebooksActive);
        Assert.Equal(f.Target.Id, f.Library.Editor.PageId);
    }

    private sealed class DraftPrompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }

        public Task<UnsavedChangesChoice> ChooseAsync()
        {
            Calls++;
            return Task.FromResult(Choice);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly NotebookTestWorkspace _root = new();
        private readonly Guid _taskId = Guid.NewGuid();
        private ServiceProvider? _reopened;
        private MainWindow _window = null!;
        public ServiceProvider Services { get; private set; } = null!;
        public MainWindowViewModel Main { get; } = new();
        public AgendaViewModel Agenda => Main.Agenda!;
        public LibraryViewModel Library => Main.Library!;
        public DraftPrompt Prompt { get; } = new();
        public NotebookSnapshot Target { get; } = new(Guid.NewGuid(), "Página vinculada", [new NotebookTextBlock(Guid.NewGuid(), "Material de referência")]);
        public NotebookSnapshot OtherPage { get; } = new(Guid.NewGuid(), "Página inicial", [new NotebookTextBlock(Guid.NewGuid(), "Anotações")]);

        public static async Task<Fixture> OpenAsync()
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f._root.DirectoryPath, unsavedChangesPrompt: f.Prompt);
                var actions = f.Services.GetRequiredService<ILibraryActions>();
                var space = await actions.CreateSpaceAsync("Estudos");
                var notebook = await actions.CreateNotebookAsync(space.Id, "Aulas");
                foreach (var page in new[] { f.Target, f.OtherPage })
                    Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(page, notebook.Id)).Status);
                f.Main.Library = f.Services.GetRequiredService<LibraryViewModel>();
                await f.Library.InitializeAsync();
                Assert.True(await f.Library.SelectSpaceAsync(space.Id));
                Assert.True(await f.Library.SelectNotebookAsync(notebook.Id));
                Assert.True(await f.Library.OpenPageAsync(f.OtherPage.Id));

                f.Main.Agenda = f.Services.GetRequiredService<AgendaViewModel>();
                var day = f.Agenda.Today;
                var agendaActions = f.Services.GetRequiredService<IAgendaActions>();
                Assert.Equal(AgendaTaskSaveStatus.Saved, (await agendaActions.SaveAsync(new(f._taskId, "Tarefa de estudo", "", day))).Status);
                await f.Agenda.InitializeAsync(day);
                Assert.True(await f.Agenda.EditTaskAsync(f._taskId));
                f.Main.ShowAgendaCommand.Execute(null);
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

        public async Task ReloadAgendaAsync()
        {
            var next = await DesktopComposition.CreateAsync(_root.DirectoryPath, unsavedChangesPrompt: Prompt);
            var agenda = next.GetRequiredService<AgendaViewModel>();
            await agenda.InitializeAsync(agenda.Today);
            Assert.True(await agenda.EditTaskAsync(_taskId));
            Main.Agenda = agenda;
            Main.ShowAgendaCommand.Execute(null);
            _reopened?.Dispose();
            _reopened = next;
            Pump();
        }

        public async Task LinkPageAsync()
        {
            Click("AgendaLinkPageButton");
            await WaitAsync(() => _window.OwnedWindows.Any(window => window.IsVisible));
            var dialog = Assert.Single(_window.OwnedWindows, window => window.IsVisible);
            var pages = Assert.Single(dialog.GetVisualDescendants().OfType<ListBox>(), control => control.Name == "AgendaPageLinkList");
            await WaitAsync(() => pages.Items.OfType<PageSearchResult>().Any(page => page.PageId == Target.Id));
            pages.SelectedItem = Assert.Single(pages.Items.OfType<PageSearchResult>(), page => page.PageId == Target.Id);
            Pump();
            var confirm = Assert.Single(dialog.GetVisualDescendants().OfType<Button>(), button => button.Name == "ConfirmAgendaPageLinkButton");
            Click(confirm);
            await WaitAsync(() => !dialog.IsVisible && !Agenda.IsBusy);
            Assert.Contains(Target.Title, Find<TextBlock>("AgendaLinkedPageLabel").Text);
        }

        public async Task LinkAnotherPageAsync()
        {
            Click("AgendaLinkPageButton");
            await WaitAsync(() => _window.OwnedWindows.Any(window => window.IsVisible));
            var dialog = Assert.Single(_window.OwnedWindows, window => window.IsVisible);
            var pages = Assert.Single(dialog.GetVisualDescendants().OfType<ListBox>(), control => control.Name == "AgendaPageLinkList");
            await WaitAsync(() => pages.Items.OfType<PageSearchResult>().Any(page => page.PageId == OtherPage.Id));
            Assert.DoesNotContain(pages.Items.OfType<PageSearchResult>(), page => page.PageId == Target.Id);
            pages.SelectedItem = Assert.Single(pages.Items.OfType<PageSearchResult>(), page => page.PageId == OtherPage.Id);
            Pump();
            Click(Assert.Single(dialog.GetVisualDescendants().OfType<Button>(), button => button.Name == "ConfirmAgendaPageLinkButton"));
            await WaitAsync(() => !dialog.IsVisible && !Agenda.IsBusy);
        }

        public void SelectLinkedPage(Guid pageId)
        {
            var selector = Find<ComboBox>("AgendaLinkedPagePicker");
            selector.SelectedItem = Assert.Single(selector.Items.OfType<AgendaPageLink>(), link => link.PageId == pageId);
            Pump();
        }

        public Button[] PageShortcuts()
        {
            Pump();
            return _window.GetVisualDescendants().OfType<Button>()
                .Where(button => button.Name == "AgendaTaskPageShortcut" && button.IsEffectivelyVisible).ToArray();
        }

        public void ClickPageShortcut(Button button) => Click(button);

        public T Find<T>(string name) where T : Control =>
            Assert.Single(_window.GetVisualDescendants().OfType<T>(), control => control.Name == name);

        public void Click(string name) => Click(Find<Button>(name));

        private void Click(Button button)
        {
            Assert.True(button.IsEffectivelyEnabled && button.IsEffectivelyVisible);
            if (button.Command is { } command)
            {
                Assert.True(command.CanExecute(button.CommandParameter));
                command.Execute(button.CommandParameter);
            }
            else
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            Pump();
        }

        private void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
        }

        public async Task WaitAsync(Func<bool> ready)
        {
            var elapsed = Stopwatch.StartNew();
            while (!ready())
            {
                Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5), "A operação de vínculo não terminou.");
                await Task.Delay(10);
                Pump();
            }
            Pump();
        }

        public void Dispose()
        {
            Prompt.Choice = UnsavedChangesChoice.Discard;
            if (_window is not null)
            {
                foreach (var dialog in _window.OwnedWindows.ToArray()) dialog.Close();
                _window.Close();
                Dispatcher.UIThread.RunJobs();
            }

            _reopened?.Dispose();
            Services?.Dispose();
            _root.Dispose();
        }
    }
}
