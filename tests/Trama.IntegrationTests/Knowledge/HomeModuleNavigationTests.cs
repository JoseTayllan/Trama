using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class HomeModuleNavigationTests
{
    [AvaloniaFact]
    public async Task Startup_ShowsHomeAndIdentifiesTheActiveModule()
    {
        using var f = await Fixture.CreateAsync();
        Assert.True(f.Home().IsEffectivelyVisible);
        Assert.Contains("module-active", f.Button("HomeNavigationButton").Classes);
        Assert.DoesNotContain("module-active", f.Button("NotebooksNavigationButton").Classes);
        Assert.True(f.Button("OpenNotebooksButton").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Startup_DoesNotAllowModuleNavigationBeforeStorageIsReady()
    {
        var window = new MainWindow(new MainWindowViewModel()); window.Show();
        try
        {
            Assert.False(RequiredButton(window, "HomeNavigationButton").IsEffectivelyEnabled);
            Assert.False(RequiredButton(window, "NotebooksNavigationButton").IsEffectivelyEnabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task Home_OpenLibraryCardUsesTheExistingLibraryAndPage()
    {
        using var f = await Fixture.CreateAsync();
        var editor = f.Library.Editor;
        f.Click("OpenNotebooksButton");
        Assert.False(f.Home().IsEffectivelyVisible);
        Assert.True(f.LibraryView.IsEffectivelyVisible);
        Assert.Same(f.Library, f.LibraryView.DataContext);
        Assert.Same(editor, f.Library.Editor);
        Assert.Equal(f.Page.Id, f.Library.Editor.PageId);
        Assert.Contains("module-active", f.Button("NotebooksNavigationButton").Classes);
        Assert.Equal(0, f.Prompt.Calls);
    }

    [AvaloniaFact]
    public async Task Navigation_RoundTripPreservesTheVisualEditorSelectionAndDraft()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        var prose = f.Prose; var editor = f.Library.Editor;
        prose.Document.Insert(prose.Text.Length, " nova ideia"); prose.Select(1, 3); f.Pump();
        var before = f.TextState();
        f.Click("HomeNavigationButton");
        Assert.True(f.Home().IsEffectivelyVisible);
        f.Click("NotebooksNavigationButton");
        Assert.Same(editor, f.Library.Editor); Assert.Same(prose, f.Prose);
        Assert.Equal(before, f.TextState());
        Assert.Equal(1, prose.SelectionStart); Assert.Equal(3, prose.SelectionLength);
        Assert.True(prose.CanUndo); Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(0, f.Prompt.Calls);
    }

    [AvaloniaFact]
    public async Task Navigation_PreservesRichFormattingListsAndNativeUndoRedo()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        var prose = f.Prose;
        prose.Document.Insert(prose.Text.Length, " novo"); prose.Select(0, 2);
        Assert.True(prose.ApplyStyle(InlineTextStyle.Italic));
        var changed = f.TextState();
        f.Click("HomeNavigationButton"); f.Click("NotebooksNavigationButton");
        Assert.True(prose.Undo()); Assert.True(prose.Undo()); f.Pump();
        Assert.Equal("Um\nDois", prose.Text);
        Assert.Equal(InlineTextStyle.Bold, Assert.Single(f.Library.Editor.Blocks[0].Styles).Style);
        Assert.True(prose.Redo()); Assert.True(prose.Redo()); f.Pump();
        Assert.Equal(changed, f.TextState());
        Assert.Equal(2, f.Library.Editor.Blocks[0].Lists.Count);
    }

    [AvaloniaFact]
    public async Task Navigation_PreservesPageScrollAndTheSelectedHierarchy()
    {
        using var f = await Fixture.CreateAsync(longPage: true); f.Click("NotebooksNavigationButton");
        var scroll = f.LibraryView.GetVisualDescendants().OfType<ScrollViewer>().Single(control => control.Name == "PageScroll");
        scroll.Offset = new Vector(0, 160); f.Pump();
        var before = scroll.Offset; Assert.True(before.Y > 0);
        f.Click("HomeNavigationButton"); f.Click("NotebooksNavigationButton");
        Assert.Equal(before, scroll.Offset);
        Assert.Equal(f.Space.Id, f.Library.SelectedSpaceId);
        Assert.Equal(f.Notebook.Id, f.Library.SelectedNotebookId);
        Assert.Equal(f.Page.Id, f.Library.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task Navigation_PreservesSearchFiltersAndResults()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        f.Library.SearchQuery = "DOIS"; f.Library.SearchTag = "ESTUDO";
        await f.Library.SearchCommand.ExecuteAsync(null); f.Pump();
        Assert.Equal(f.Page.Id, Assert.Single(f.Library.SearchResults).PageId);
        f.Click("HomeNavigationButton"); f.Click("NotebooksNavigationButton");
        Assert.Equal("DOIS", f.Library.SearchQuery); Assert.Equal("ESTUDO", f.Library.SearchTag);
        Assert.True(f.Library.IsSearchOpen);
        Assert.Equal(f.Page.Id, Assert.Single(f.Library.SearchResults).PageId);
    }

    [AvaloniaFact]
    public async Task Home_ContinueResumesTheDraftWithoutReloadingOrSaving()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        var editor = f.Library.Editor; var prose = f.Prose;
        prose.Document.Insert(prose.Text.Length, " rascunho"); f.Pump();
        f.Click("HomeNavigationButton"); f.Click("ContinueNotebookButton");
        Assert.Same(editor, f.Library.Editor); Assert.Same(prose, f.Prose);
        Assert.Contains("rascunho", prose.Text); Assert.True(prose.CanUndo);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal("Um\nDois", Assert.IsType<NotebookTextBlock>((await f.Actions.LoadAsync(f.Page.Id))!.Blocks[0]).Text);
    }

    [AvaloniaFact]
    public async Task Autosave_ContinuesWithTheNotebookHiddenBehindHome()
    {
        using var f = await Fixture.CreateAsync(autoSave: true); f.Click("NotebooksNavigationButton");
        var prose = f.Prose; prose.Document.Insert(prose.Text.Length, " automático"); f.Pump();
        f.Click("HomeNavigationButton");
        await f.UntilAsync(() => !f.Library.Editor.IsBusy && !f.Library.Editor.HasUnsavedChanges &&
            f.Library.Editor.LastSaveStatus == NotebookSaveStatus.Saved);
        Assert.True(f.Home().IsEffectivelyVisible);
        Assert.Contains("automático", Assert.IsType<NotebookTextBlock>((await f.Actions.LoadAsync(f.Page.Id))!.Blocks[0]).Text);
        f.Click("NotebooksNavigationButton");
        Assert.Same(prose, f.Prose); Assert.True(prose.CanUndo);
    }

    [AvaloniaFact]
    public async Task ManualSave_AndSwitchingModulesDoNotResetTheUndoHistory()
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        var prose = f.Prose; prose.Document.Insert(prose.Text.Length, " salvo"); f.Pump();
        await f.Library.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(NotebookSaveStatus.Saved, f.Library.Editor.LastSaveStatus);
        f.Click("HomeNavigationButton"); f.Click("NotebooksNavigationButton");
        Assert.True(prose.Undo()); f.Pump();
        Assert.Equal("Um\nDois", prose.Text); Assert.True(f.Library.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task Home_DoesNotAdvertiseFunctionalAgendaOrFictionalSummaries()
    {
        using var f = await Fixture.CreateAsync(); Assert.True(f.Home().IsEffectivelyVisible);
        var agendaActions = f.Window.GetVisualDescendants().OfType<Button>()
            .Where(control => control.Name is "AgendaNavigationButton" or "OpenAgendaButton");
        Assert.All(agendaActions, action => Assert.False(action.IsEffectivelyVisible && action.IsEffectivelyEnabled));
        var visibleText = f.Window.GetVisualDescendants().OfType<TextBlock>().Where(control => control.IsEffectivelyVisible)
            .Select(control => control.Text ?? string.Empty);
        Assert.DoesNotContain(visibleText, text => text.Contains("3 tarefas para hoje", StringComparison.Ordinal) ||
            text.Contains("8 h planejadas", StringComparison.Ordinal) || text.Contains("24 páginas", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task Navigation_AndHomeActionsFitTheCompactWindow()
    {
        using var f = await Fixture.CreateAsync();
        f.Window.Width = 860; f.Window.Height = 680; f.Pump();
        foreach (var name in new[] { "HomeNavigationButton", "NotebooksNavigationButton", "OpenNotebooksButton" })
        {
            var button = f.Button(name); Assert.True(button.IsEffectivelyVisible);
            var point = button.TranslatePoint(default, f.Window)!.Value;
            Assert.True(button.Bounds.Width > 0 && button.Bounds.Height > 0);
            Assert.InRange(point.X, 0, f.Window.Bounds.Width - button.Bounds.Width + 1);
            Assert.InRange(point.Y, 0, f.Window.Bounds.Height - button.Bounds.Height + 1);
        }
        f.Click("NotebooksNavigationButton"); Assert.True(f.LibraryView.IsEffectivelyVisible);
        f.Click("HomeNavigationButton"); Assert.True(f.Home().IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task Backup_IsAccessibleFromHomeWithoutChangingTheNotebook()
    {
        using var f = await Fixture.CreateAsync(); Assert.True(f.Home().IsEffectivelyVisible);
        var before = f.TextState();
        f.Click("OpenBackupButton");
        await f.UntilAsync(() => f.Window.OwnedWindows.OfType<BackupDialog>().Any());
        f.Window.OwnedWindows.OfType<BackupDialog>().Single().Close();
        await f.UntilAsync(() => !f.Window.OwnedWindows.Any());
        Assert.Equal(before, f.TextState()); Assert.Equal(0, f.Prompt.Calls);
    }

    [AvaloniaTheory]
    [InlineData(UnsavedChangesChoice.Cancel, false, false)]
    [InlineData(UnsavedChangesChoice.Save, true, true)]
    [InlineData(UnsavedChangesChoice.Discard, true, false)]
    public async Task ClosingFromHome_StillProtectsTheHiddenNotebook(UnsavedChangesChoice choice, bool closed, bool saved)
    {
        using var f = await Fixture.CreateAsync(); f.Click("NotebooksNavigationButton");
        f.Prose.Document.Insert(f.Prose.Text.Length, " pendente"); f.Pump();
        f.Click("HomeNavigationButton"); f.Prompt.Choice = choice;
        f.Window.Close();
        await f.UntilAsync(() => f.Prompt.Calls == 1 && (closed ? !f.Window.IsVisible : f.Window.IsEnabled));
        Assert.Equal(!closed, f.Window.IsVisible);
        var text = Assert.IsType<NotebookTextBlock>((await f.Actions.LoadAsync(f.Page.Id))!.Blocks[0]).Text;
        Assert.Equal(saved ? "Um\nDois pendente" : "Um\nDois", text);
        if (!closed) { Assert.True(f.Home().IsEffectivelyVisible); Assert.True(f.Library.Editor.HasUnsavedChanges); }
    }

    [AvaloniaFact]
    public async Task ExistingStandaloneNotebook_StillRendersWithoutAModuleLibrary()
    {
        using var f = await Fixture.CreateAsync();
        var window = new MainWindow(new MainWindowViewModel { Notebook = f.Library.Editor });
        window.Show(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        try
        {
            var prose = Assert.Single(window.GetVisualDescendants().OfType<NotebookProseEditor>());
            Assert.True(prose.IsEffectivelyVisible); Assert.Equal("Um\nDois", prose.Text);
        }
        finally { window.Close(); }
    }

    private static Button RequiredButton(Control root, string name)
    {
        var button = root.FindControl<Button>(name);
        Assert.True(button is not null, $"A navegação principal ainda não oferece o controle {name}.");
        return button!;
    }

    private sealed class Prompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }
        public Task<UnsavedChangesChoice> ChooseAsync() { Calls++; return Task.FromResult(Choice); }
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public ServiceProvider Services { get; private set; } = null!;
        public ILibraryActions Actions { get; private set; } = null!;
        public LibraryViewModel Library { get; private set; } = null!;
        public Prompt Prompt { get; } = new();
        public MainWindow Window { get; private set; } = null!;
        public KnowledgeSpace Space { get; private set; } = null!;
        public NotebookInfo Notebook { get; private set; } = null!;
        public NotebookSnapshot Page { get; private set; } = null!;
        public LibraryWorkspaceView LibraryView => Assert.Single(Window.GetVisualDescendants().OfType<LibraryWorkspaceView>());
        public NotebookProseEditor Prose => Assert.Single(LibraryView.GetVisualDescendants().OfType<NotebookProseEditor>());

        public static async Task<Fixture> CreateAsync(bool autoSave = false, bool longPage = false)
        {
            var f = new Fixture();
            f.Services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath, autoSave, unsavedChangesPrompt: f.Prompt);
            f.Actions = f.Services.GetRequiredService<ILibraryActions>();
            f.Space = await f.Actions.CreateSpaceAsync("Estudos");
            f.Notebook = await f.Actions.CreateNotebookAsync(f.Space.Id, "Curso C#");
            var body = longPage ? string.Join('\n', Enumerable.Range(0, 120).Select(i => $"Linha {i:000} para conferir a rolagem do caderno.")) : "Um\nDois";
            var second = ParagraphListDocument.Starts(body)[1];
            f.Page = new NotebookSnapshot(Guid.NewGuid(), "Interfaces e contratos", [new NotebookTextBlock(Guid.NewGuid(), body)
                { Styles = [new TextStyleSpan(0, 2, InlineTextStyle.Bold) { FontSize = 24, Foreground = "teal", Highlight = "amber" }],
                  Lists = [new(0, "numbered"), new(second, "numbered")] }]);
            Assert.Equal(NotebookSaveStatus.Saved, (await f.Actions.SaveInNotebookAsync(f.Page, f.Notebook.Id)).Status);
            await f.Services.GetRequiredService<IKnowledgeDiscovery>().SetPageTagsAsync(f.Page.Id, ["estudo"]);
            f.Library = f.Services.GetRequiredService<LibraryViewModel>(); await f.Library.InitializeAsync();
            Assert.True(await f.Library.SelectSpaceAsync(f.Space.Id)); Assert.True(await f.Library.SelectNotebookAsync(f.Notebook.Id));
            Assert.True(await f.Library.OpenPageAsync(f.Page.Id));
            var main = new MainWindowViewModel { Library = f.Library, Backup = f.Services.GetRequiredService<BackupViewModel>() };
            f.Window = new MainWindow(main) { Width = 1200, Height = 850 }; f.Window.Show(); f.Pump();
            return f;
        }

        public Control Home()
        {
            var home = Window.FindControl<Control>("HomeWorkspace");
            Assert.True(home is not null, "A janela ainda não oferece a tela Início.");
            return home!;
        }
        public Button Button(string name) => RequiredButton(Window, name);
        public void Click(string name)
        {
            var button = Button(name); Assert.True(button.IsEffectivelyEnabled && button.IsEffectivelyVisible);
            if (button.Command is { } command) command.Execute(button.CommandParameter);
            else button.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            Pump();
        }
        public string TextState()
        {
            var block = Library.Editor.Blocks[0];
            return JsonSerializer.Serialize(new { block.Text, block.Styles, block.Lists });
        }
        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public async Task UntilAsync(Func<bool> ready)
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            while (!ready()) { Assert.True(System.Diagnostics.Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(10), "A navegação não concluiu no prazo do teste."); Pump(); await Task.Delay(10); }
            Pump();
        }
        public void Dispose()
        {
            Prompt.Choice = UnsavedChangesChoice.Discard;
            foreach (var dialog in Window.OwnedWindows.ToArray()) dialog.Close();
            Window.Close(); Dispatcher.UIThread.RunJobs(); Services.Dispose(); Root.Dispose();
        }
    }
}
