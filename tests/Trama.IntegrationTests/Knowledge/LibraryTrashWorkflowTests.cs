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

public sealed class LibraryTrashWorkflowTests
{
    [AvaloniaTheory]
    [InlineData(LibraryEntryKind.Page)]
    [InlineData(LibraryEntryKind.Notebook)]
    [InlineData(LibraryEntryKind.Space)]
    public async Task MoveToTrashAndRestore_PreservesIdsHierarchyStylesTagsAndImages(LibraryEntryKind kind)
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(kind);

        Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
        Assert.DoesNotContain(await f.Actions.ListPagesAsync(f.Book.Id), page => page.Id == f.Page.Id);
        Assert.NotNull(await f.Actions.LoadAsync(f.OtherPage.Id));
        Assert.Contains(await f.Actions.ListNotebooksAsync(f.OtherSpace.Id), item => item.Id == f.OtherBook.Id);
        await f.RestoreAsync(f.Title(kind));

        await f.AssertOriginalPageAsync();
        Assert.Contains(await f.Actions.ListSpacesAsync(), item => item.Id == f.Space.Id);
        Assert.Contains(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Id == f.Book.Id);
        Assert.Contains(await f.Actions.ListPagesAsync(f.Book.Id), item => item.Id == f.Page.Id);
    }

    [AvaloniaTheory]
    [InlineData(LibraryEntryKind.Page)]
    [InlineData(LibraryEntryKind.Notebook)]
    [InlineData(LibraryEntryKind.Space)]
    public async Task CancellingConfirmation_LeavesTheEntireHierarchyActive(LibraryEntryKind kind)
    {
        using var f = await Fixture.CreateAsync();
        var dialog = await f.RequestDeletionAsync(kind);
        Fixture.Invoke(Assert.IsType<Button>(dialog.FindControl<Button>("CancelTrashButton")));
        await Fixture.UntilAsync(() => Task.FromResult(!dialog.IsVisible));

        await f.AssertOriginalPageAsync();
        Assert.Contains(await f.Actions.ListSpacesAsync(), item => item.Id == f.Space.Id);
        Assert.Contains(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Id == f.Book.Id);
        Assert.Contains(await f.Actions.ListPagesAsync(f.Book.Id), item => item.Id == f.Page.Id);
    }

    [AvaloniaFact]
    public async Task SpaceDeletionAndRestoration_IncludesAllItsActiveNotebooksAndPages()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Space);

        Assert.DoesNotContain(await f.Actions.ListSpacesAsync(), item => item.Id == f.Space.Id);
        Assert.Empty(await f.Actions.ListNotebooksAsync(f.Space.Id));
        Assert.Null(await f.Actions.LoadAsync(f.SecondPage.Id));
        Assert.Contains(await f.Actions.ListSpacesAsync(), item => item.Id == f.OtherSpace.Id);
        Assert.NotNull(await f.Actions.LoadAsync(f.OtherPage.Id));
        await f.RestoreAsync(f.Space.Title);

        Assert.Equal(2, (await f.Actions.ListNotebooksAsync(f.Space.Id)).Count);
        Assert.NotNull(await f.Actions.LoadAsync(f.SecondPage.Id));
        await f.AssertOriginalPageAsync();
    }

    [AvaloniaFact]
    public async Task RestoringNotebook_DoesNotRestoreAPageDeletedBeforeTheNotebook()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Page);
        Assert.True(await f.Library.SelectNotebookAsync(f.Book.Id));
        await f.DeleteAsync(LibraryEntryKind.Notebook);
        await f.RestoreAsync(f.Book.Title);

        Assert.Contains(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Id == f.Book.Id);
        Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
        Assert.DoesNotContain(await f.Actions.ListPagesAsync(f.Book.Id), item => item.Id == f.Page.Id);
    }

    [AvaloniaFact]
    public async Task RestoringSpace_DoesNotRestoreANotebookDeletedBeforeTheSpace()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Notebook);
        Assert.True(await f.Library.SelectSpaceAsync(f.Space.Id));
        await f.DeleteAsync(LibraryEntryKind.Space);
        await f.RestoreAsync(f.Space.Title);

        Assert.DoesNotContain(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Id == f.Book.Id);
        Assert.Contains(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Id == f.SecondBook.Id);
        Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
        Assert.NotNull(await f.Actions.LoadAsync(f.SecondPage.Id));
    }

    [AvaloniaFact]
    public async Task PageRestoration_IsBlockedWhileItsNotebookIsInTheTrash()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Page);
        Assert.True(await f.Library.SelectNotebookAsync(f.Book.Id));
        await f.DeleteAsync(LibraryEntryKind.Notebook);
        var dialog = await f.OpenTrashAsync();
        f.SelectTrashEntry(dialog, f.Page.Title);

        Assert.False(Assert.IsType<Button>(dialog.FindControl<Button>("RestoreTrashEntryButton")).IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(dialog.FindControl<TextBlock>("TrashFeedback")?.Text));
        Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
    }

    [AvaloniaFact]
    public async Task NotebookRestoration_IsBlockedWhileItsSpaceIsInTheTrash()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Notebook);
        Assert.True(await f.Library.SelectSpaceAsync(f.Space.Id));
        await f.DeleteAsync(LibraryEntryKind.Space);
        var dialog = await f.OpenTrashAsync();
        f.SelectTrashEntry(dialog, f.Book.Title);

        Assert.False(Assert.IsType<Button>(dialog.FindControl<Button>("RestoreTrashEntryButton")).IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(dialog.FindControl<TextBlock>("TrashFeedback")?.Text));
        Assert.DoesNotContain(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Id == f.Book.Id);
    }

    [AvaloniaFact]
    public async Task TrashedPages_AreExcludedFromTextAndExactTagSearches()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Space);

        Assert.DoesNotContain(await f.Discovery.SearchAsync("Conteúdo único"), item => item.PageId == f.Page.Id);
        Assert.DoesNotContain(await f.Discovery.SearchAsync("", "ESTUDO"), item => item.PageId == f.Page.Id);
        await f.RestoreAsync(f.Space.Title);
        Assert.Contains(await f.Discovery.SearchAsync("", "ESTUDO"), item => item.PageId == f.Page.Id);
    }

    [AvaloniaFact]
    public async Task CancellingTheUnsavedChangesPrompt_PreservesTheOpenDraftAndItsHistory()
    {
        using var f = await Fixture.CreateAsync(UnsavedChangesChoice.Cancel);
        var prose = f.View.GetVisualDescendants().OfType<NotebookProseEditor>().Single();
        prose.Document.Insert(prose.Text.Length, " rascunho");
        prose.Select(1, 3);
        f.BeginDeletion(LibraryEntryKind.Space);
        await Fixture.UntilAsync(() => Task.FromResult(f.Prompt.Calls == 1));

        Assert.Contains("rascunho", f.Library.Editor.Blocks[0].Text);
        Assert.Same(prose, f.View.GetVisualDescendants().OfType<NotebookProseEditor>().Single());
        Assert.True(prose.CanUndo);
        Assert.Equal((1, 3), (prose.SelectionStart, prose.SelectionLength));
        Assert.Contains(await f.Actions.ListSpacesAsync(), item => item.Id == f.Space.Id);
        Assert.NotNull(await f.Actions.LoadAsync(f.Page.Id));
    }

    [AvaloniaFact]
    public async Task SavingBeforeDeletion_PreservesTheLatestDraftWhenRestored()
    {
        using var f = await Fixture.CreateAsync(UnsavedChangesChoice.Save);
        f.Library.Editor.Title = "Rascunho final";
        await f.DeleteAsync(LibraryEntryKind.Space);
        await f.RestoreAsync(f.Space.Title);

        Assert.Equal(1, f.Prompt.Calls);
        Assert.Equal("Rascunho final", (await f.Actions.LoadAsync(f.Page.Id))!.Title);
    }

    [AvaloniaFact]
    public async Task ANewComposition_SeesThePersistedTrashInsteadOfAnInMemoryDeletion()
    {
        using var f = await Fixture.CreateAsync();
        await f.DeleteAsync(LibraryEntryKind.Space);
        using var fresh = await DesktopComposition.CreateAsync(f.Data.DirectoryPath);
        var actions = fresh.GetRequiredService<ILibraryActions>();

        Assert.DoesNotContain(await actions.ListSpacesAsync(), item => item.Id == f.Space.Id);
        Assert.Null(await actions.LoadAsync(f.Page.Id));
        await f.RestoreAsync(f.Space.Title);
        Assert.NotNull(await actions.LoadAsync(f.Page.Id));
    }

    [AvaloniaFact]
    public async Task SavingAStaleSnapshot_CannotResurrectATrashedPage()
    {
        using var f = await Fixture.CreateAsync();
        var stale = f.Page;
        await f.DeleteAsync(LibraryEntryKind.Page);

        Assert.NotEqual(NotebookSaveStatus.Saved, (await f.Actions.SaveInNotebookAsync(stale, f.Book.Id)).Status);
        Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
        await f.RestoreAsync(f.Page.Title);
        await f.AssertOriginalPageAsync();
    }

    [AvaloniaFact]
    public async Task ExistingStorage_AlreadyPreservesStylesTagsAndImageBytes()
    {
        using var f = await Fixture.CreateAsync();
        await f.AssertOriginalPageAsync();
    }

    [AvaloniaFact]
    public async Task ExistingHierarchy_AlreadySeparatesNotebooksWithTheSameTitleBySpace()
    {
        using var f = await Fixture.CreateAsync();
        Assert.Equal(f.Book.Id, Assert.Single(await f.Actions.ListNotebooksAsync(f.Space.Id), item => item.Title == f.Book.Title).Id);
        Assert.Equal(f.OtherBook.Id, Assert.Single(await f.Actions.ListNotebooksAsync(f.OtherSpace.Id)).Id);
        Assert.NotEqual(f.Book.Id, f.OtherBook.Id);
    }

    private sealed class Fixture : IDisposable
    {
        public required NotebookTestWorkspace Data { get; init; }
        public required ServiceProvider Services { get; init; }
        public required ILibraryActions Actions { get; init; }
        public required IKnowledgeDiscovery Discovery { get; init; }
        public required NavigationPrompt Prompt { get; init; }
        public required LibraryViewModel Library { get; init; }
        public required LibraryWorkspaceView View { get; init; }
        public required Window Window { get; init; }
        public required KnowledgeSpace Space { get; init; }
        public required KnowledgeSpace OtherSpace { get; init; }
        public required NotebookInfo Book { get; init; }
        public required NotebookInfo SecondBook { get; init; }
        public required NotebookInfo OtherBook { get; init; }
        public required NotebookSnapshot Page { get; init; }
        public required NotebookSnapshot SecondPage { get; init; }
        public required NotebookSnapshot OtherPage { get; init; }
        public required Guid ImageId { get; init; }

        public static async Task<Fixture> CreateAsync(UnsavedChangesChoice choice = UnsavedChangesChoice.Cancel)
        {
            var data = new NotebookTestWorkspace();
            var prompt = new NavigationPrompt(choice);
            var services = await DesktopComposition.CreateAsync(data.DirectoryPath, unsavedChangesPrompt: prompt);
            var actions = services.GetRequiredService<ILibraryActions>();
            var discovery = services.GetRequiredService<IKnowledgeDiscovery>();
            var space = await actions.CreateSpaceAsync("Estudos");
            var otherSpace = await actions.CreateSpaceAsync("Trabalho");
            var book = await actions.CreateNotebookAsync(space.Id, "Curso C#");
            var secondBook = await actions.CreateNotebookAsync(space.Id, "Pesquisa");
            var otherBook = await actions.CreateNotebookAsync(otherSpace.Id, "Curso C#");
            using var source = new MemoryStream(NotebookTestWorkspace.Png);
            var image = await actions.ImportImageAsync(source);
            var page = new NotebookSnapshot(Guid.NewGuid(), "Página principal", [
                new NotebookTextBlock(Guid.NewGuid(), "Conteúdo único") { Styles = [new TextStyleSpan(0, 8, InlineTextStyle.Bold) { FontSize = 24, Foreground = "teal", Highlight = "amber" }] },
                new NotebookCodeBlock(Guid.NewGuid(), "Console.WriteLine(42);", "csharp"),
                new NotebookImageBlock(Guid.NewGuid(), image.Id, "Legenda")]);
            var secondPage = new NotebookSnapshot(Guid.NewGuid(), "Outra página", [new NotebookTextBlock(Guid.NewGuid(), "Outro assunto")]);
            var otherPage = new NotebookSnapshot(Guid.NewGuid(), "Página do trabalho", [new NotebookTextBlock(Guid.NewGuid(), "Preservar")]);
            foreach (var (snapshot, notebook) in new[] { (page, book), (secondPage, secondBook), (otherPage, otherBook) })
                Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(snapshot, notebook.Id)).Status);
            await discovery.SetPageTagsAsync(page.Id, ["estudo"]);
            var library = services.GetRequiredService<LibraryViewModel>();
            await library.InitializeAsync();
            Assert.True(await library.SelectSpaceAsync(space.Id));
            Assert.True(await library.SelectNotebookAsync(book.Id));
            Assert.True(await library.OpenPageAsync(page.Id));
            var view = new LibraryWorkspaceView(library);
            var window = new Window { Width = 1340, Height = 940, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            return new Fixture
            {
                Data = data,
                Services = services,
                Actions = actions,
                Discovery = discovery,
                Prompt = prompt,
                Library = library,
                View = view,
                Window = window,
                Space = space,
                OtherSpace = otherSpace,
                Book = book,
                SecondBook = secondBook,
                OtherBook = otherBook,
                Page = page,
                SecondPage = secondPage,
                OtherPage = otherPage,
                ImageId = image.Id
            };
        }

        public string Title(LibraryEntryKind kind) => kind switch { LibraryEntryKind.Space => Space.Title, LibraryEntryKind.Notebook => Book.Title, _ => Page.Title };

        public void BeginDeletion(LibraryEntryKind kind)
        {
            var name = kind switch { LibraryEntryKind.Space => "MoveSpaceToTrashButton", LibraryEntryKind.Notebook => "MoveNotebookToTrashButton", _ => "MovePageToTrashButton" };
            var button = View.FindControl<Button>(name);
            Assert.True(button is not null, $"A interface ainda não oferece a ação de lixeira: {name}.");
            Invoke(button!);
        }

        public async Task<Window> RequestDeletionAsync(LibraryEntryKind kind)
        {
            BeginDeletion(kind);
            await UntilAsync(() => Task.FromResult(Window.OwnedWindows.Any(dialog => dialog.FindControl<Button>("ConfirmTrashButton") is not null)));
            return Assert.Single(Window.OwnedWindows, dialog => dialog.FindControl<Button>("ConfirmTrashButton") is not null);
        }

        public async Task DeleteAsync(LibraryEntryKind kind)
        {
            var dialog = await RequestDeletionAsync(kind);
            Invoke(Assert.IsType<Button>(dialog.FindControl<Button>("ConfirmTrashButton")));
            await UntilAsync(async () => !dialog.IsVisible && !Library.IsBusy && await Actions.LoadAsync(Page.Id) is null);
        }

        public async Task<Window> OpenTrashAsync()
        {
            var button = View.FindControl<Button>("OpenTrashButton");
            Assert.True(button is not null, "A biblioteca ainda não oferece acesso à lixeira.");
            Invoke(button!);
            await UntilAsync(() => Task.FromResult(Window.OwnedWindows.Any(dialog => dialog.FindControl<ListBox>("TrashEntriesList") is not null)));
            return Assert.Single(Window.OwnedWindows, dialog => dialog.FindControl<ListBox>("TrashEntriesList") is not null);
        }

        public void SelectTrashEntry(Window dialog, string title)
        {
            var list = Assert.IsType<ListBox>(dialog.FindControl<ListBox>("TrashEntriesList"));
            var container = Assert.Single(list.GetVisualDescendants().OfType<ListBoxItem>(),
                item => item.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == title));
            list.SelectedItem = container.DataContext;
            Dispatcher.UIThread.RunJobs(); dialog.UpdateLayout();
        }

        public async Task RestoreAsync(string title)
        {
            var dialog = await OpenTrashAsync();
            SelectTrashEntry(dialog, title);
            var restore = Assert.IsType<Button>(dialog.FindControl<Button>("RestoreTrashEntryButton"));
            Assert.True(restore.IsEnabled);
            Invoke(restore);
            await UntilAsync(async () => title == Space.Title ? (await Actions.ListSpacesAsync()).Any(item => item.Id == Space.Id)
                : title == Book.Title ? (await Actions.ListNotebooksAsync(Space.Id)).Any(item => item.Id == Book.Id)
                : await Actions.LoadAsync(Page.Id) is not null);
            dialog.Close();
            Dispatcher.UIThread.RunJobs();
        }

        public async Task AssertOriginalPageAsync()
        {
            var loaded = await Actions.LoadAsync(Page.Id);
            Assert.NotNull(loaded);
            Assert.Equal(Page.Title, loaded.Title);
            Assert.Equal(Page.Blocks.Select(block => block.Id), loaded.Blocks.Select(block => block.Id));
            Assert.Equal(Assert.IsType<NotebookTextBlock>(Page.Blocks[0]).Styles, Assert.IsType<NotebookTextBlock>(loaded.Blocks[0]).Styles);
            Assert.Equal("Console.WriteLine(42);", Assert.IsType<NotebookCodeBlock>(loaded.Blocks[1]).Code);
            Assert.Equal(ImageId, Assert.IsType<NotebookImageBlock>(loaded.Blocks[2]).AttachmentId);
            Assert.Equal(new[] { "estudo" }, await Discovery.GetPageTagsAsync(Page.Id));
            using var image = await Actions.OpenImageAsync(ImageId);
            Assert.NotNull(image);
            using var copied = new MemoryStream(); await image.CopyToAsync(copied);
            Assert.Equal(NotebookTestWorkspace.Png, copied.ToArray());
        }

        public static void Invoke(Button button)
        {
            Assert.True(button.IsEffectivelyVisible && button.IsEnabled);
            if (button.Command is { } command) { Assert.True(command.CanExecute(button.CommandParameter)); command.Execute(button.CommandParameter); }
            else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }

        public static async Task UntilAsync(Func<Task<bool>> ready)
        {
            var end = DateTime.UtcNow.AddSeconds(5);
            while (!await ready())
            {
                Assert.True(DateTime.UtcNow < end, "A operação da interface não chegou ao estado esperado em cinco segundos.");
                Dispatcher.UIThread.RunJobs(); await Task.Delay(10);
            }
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            foreach (var dialog in Window.OwnedWindows.ToArray()) dialog.Close();
            View.DataContext = null; Window.Close(); Services.Dispose(); Data.Dispose();
        }
    }
}
