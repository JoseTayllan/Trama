using System.Diagnostics;
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

public sealed class HomeRecentPagesTests
{
    [AvaloniaFact]
    public async Task RecentPages_ShowSavedTitleLocationAndEditTimeAfterReopeningProfile()
    {
        using var profile = new NotebookTestWorkspace();
        var page = NewPage("Contratos da API");
        using (var writing = await DesktopComposition.CreateAsync(profile.DirectoryPath))
        {
            var actions = writing.GetRequiredService<ILibraryActions>();
            var space = await actions.CreateSpaceAsync("Estudos");
            var notebook = await actions.CreateNotebookAsync(space.Id, "Curso C#");
            await SaveAsync(actions, page, notebook.Id);
        }

        using var view = await Fixture.OpenAsync(profile.DirectoryPath);
        var row = await view.FindPageAsync(page.Title);
        var labels = row.GetVisualDescendants().OfType<TextBlock>().Select(label => label.Text).ToArray();
        Assert.Contains("Estudos / Curso C#", labels);
        Assert.Contains("Agora há pouco", labels);
        Assert.True(row.IsEffectivelyEnabled);
        if (row.Command is { } command) command.Execute(row.CommandParameter);
        else row.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        await view.WaitAsync(() => view.Main.IsNotebooksActive);
        Assert.Equal(page.Id, view.Main.Library!.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task RecentPages_RefreshAfterReturningHomeAndExcludeTrashedPages()
    {
        using var profile = new NotebookTestWorkspace();
        var removed = NewPage("Página removida");
        using var view = await Fixture.OpenAsync(profile.DirectoryPath);
        view.Main.ShowNotebooksCommand.Execute(null);
        Assert.True(view.Main.IsNotebooksActive);
        await SaveAsync(view.Actions, removed);
        await view.Trash.MoveToTrashAsync(LibraryEntryKind.Page, removed.Id);
        var active = NewPage("Página recém-salva");
        await SaveAsync(view.Actions, active);

        view.Main.ShowHomeCommand.Execute(null);

        var row = await view.FindPageAsync(active.Title);
        Assert.True(row.IsEffectivelyVisible);
        Assert.Single(view.PageButtons());
        Assert.DoesNotContain(view.PageButtons(), button => Fixture.HasTitle(button, removed.Title));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentRecentPage_ShowsOneTitleAndResumesTheSameEditor(bool dirty)
    {
        using var profile = new NotebookTestWorkspace();
        using var f = await Fixture.OpenAsync(profile.DirectoryPath);
        var page = NewPage("Página já aberta");
        await SaveAsync(f.Actions, page);
        var library = f.Main.Library!;
        f.Main.ShowNotebooksCommand.Execute(null);
        await library.InitializeAsync();
        Assert.True(await library.OpenPageAsync(page.Id));
        var editor = library.Editor;
        if (dirty) editor.Title = "Rascunho ainda aberto";
        f.Main.ShowHomeCommand.Execute(null);

        var row = await f.FindPageAsync(page.Title);
        var labels = f.CardLabels(row);
        Assert.Single(labels, label => label == page.Title);
        Assert.Contains(dirty ? "Rascunho em andamento" : "Página aberta", labels);

        if (row.Command is { } command) command.Execute(row.CommandParameter);
        else row.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        await f.WaitAsync(() => f.Main.IsNotebooksActive);
        Assert.Same(editor, library.Editor);
        Assert.Equal(dirty, editor.HasUnsavedChanges);
        if (dirty) Assert.Equal("Rascunho ainda aberto", editor.Title);
    }

    private static NotebookSnapshot NewPage(string title) =>
        new(Guid.NewGuid(), title, [new NotebookTextBlock(Guid.NewGuid(), "Material de estudo")]);

    private static async Task SaveAsync(ILibraryActions actions, NotebookSnapshot page, Guid? notebookId = null) =>
        Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(page, notebookId)).Status);

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly MainWindow _window;
        public MainWindowViewModel Main { get; }
        public ILibraryActions Actions => _services.GetRequiredService<ILibraryActions>();
        public ILibraryTrashActions Trash => _services.GetRequiredService<ILibraryTrashActions>();

        private Fixture(ServiceProvider services, MainWindowViewModel main)
        {
            _services = services;
            Main = main;
            _window = new MainWindow(main) { Width = 1200, Height = 850 };
        }

        public static async Task<Fixture> OpenAsync(string directory)
        {
            var services = await DesktopComposition.CreateAsync(directory);
            try
            {
                var library = services.GetRequiredService<LibraryViewModel>();
                await library.InitializeAsync();
                var f = new Fixture(services, new MainWindowViewModel { Library = library });
                f._window.Show();
                return f;
            }
            catch
            {
                services.Dispose();
                throw;
            }
        }

        public async Task<Button> FindPageAsync(string title)
        {
            Pump();
            Assert.Single(_window.GetVisualDescendants().OfType<ItemsControl>(),
                control => control.Name == "HomeRecentPages");
            await WaitAsync(() => PageButtons().Any(button => HasTitle(button, title)));
            return Assert.Single(PageButtons(), button => HasTitle(button, title));
        }

        public string?[] CardLabels(Button row)
        {
            var card = Assert.Single(row.GetVisualAncestors().OfType<Border>(), border => border.Classes.Contains("home-card"));
            return card.GetVisualDescendants().OfType<TextBlock>()
                .Where(label => label.IsEffectivelyVisible).Select(label => label.Text).ToArray();
        }

        public Button[] PageButtons() => _window.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Name == "HomeRecentPageButton" && button.IsEffectivelyVisible).ToArray();

        public static bool HasTitle(Button button, string title) =>
            button.GetVisualDescendants().OfType<TextBlock>().Any(label => label.Text == title);

        public async Task WaitAsync(Func<bool> ready)
        {
            var elapsed = Stopwatch.StartNew();
            Pump();
            while (!ready())
            {
                Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5), "As páginas recentes não foram atualizadas.");
                await Task.Delay(10);
                Pump();
            }
        }

        private void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
        }

        public void Dispose()
        {
            _window.Close();
            Dispatcher.UIThread.RunJobs();
            _services.Dispose();
        }
    }
}
