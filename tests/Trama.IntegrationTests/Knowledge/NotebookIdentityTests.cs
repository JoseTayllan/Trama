using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NotebookIdentityTests
{
    [AvaloniaFact]
    public async Task PageContext_IdentifiesSpaceNotebookAndPageWithExplicitHeadings()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.Equal("ESPAÇO", fixture.Text("SpaceContextHeading"));
        Assert.Equal("CADERNO", fixture.Text("NotebookContextHeading"));
        Assert.Equal("PÁGINA", fixture.Text("PageContextHeading"));
        fixture.AssertPath("Estudos", "Curso C#", "Interfaces e contratos");
    }

    [AvaloniaFact]
    public async Task EditingTitle_UpdatesThePageContextWithoutSavingOrChangingItsParents()
    {
        using var fixture = await Fixture.CreateAsync();

        fixture.Library.Editor.Title = "Meu resumo em andamento";
        fixture.Pump();

        fixture.AssertPath("Estudos", "Curso C#", "Meu resumo em andamento");
        Assert.True(fixture.Library.Editor.HasUnsavedChanges);
        Assert.Equal("Interfaces e contratos", (await fixture.Actions.LoadAsync(fixture.Page.Id))!.Title);
    }

    [AvaloniaFact]
    public async Task BlankDraftTitle_ShowsAnExplicitUntitledPage()
    {
        using var fixture = await Fixture.CreateAsync();

        fixture.Library.Editor.Title = " \t ";
        fixture.Pump();

        fixture.AssertPath("Estudos", "Curso C#", "Sem título");
    }

    [AvaloniaFact]
    public async Task RenamingSpace_UpdatesThePathAndPreservesTheActiveDraftSelectionAndHistory()
    {
        using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Library.Editor;
        var prose = fixture.Prose;
        prose.TextArea.Focus();
        prose.Document.Insert(prose.Text.Length, " nova ideia");
        prose.Select(1, 3);

        Assert.True(await fixture.Library.RenameSelectedSpaceAsync("Pesquisa"));
        fixture.Pump();

        fixture.AssertPath("Pesquisa", "Curso C#", "Interfaces e contratos");
        Assert.Same(editor, fixture.Library.Editor);
        Assert.Same(prose, fixture.Prose);
        Assert.Equal(1, prose.SelectionStart);
        Assert.Equal(3, prose.SelectionLength);
        Assert.True(prose.CanUndo);
        Assert.Contains("nova ideia", fixture.Library.Editor.Blocks[0].Text);
        Assert.Equal(0, fixture.Prompt.Calls);
    }

    [AvaloniaFact]
    public async Task RenamingNotebook_UpdatesThePathWithoutReplacingTheEditor()
    {
        using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Library.Editor;
        editor.Title = "Rascunho de estudo";

        Assert.True(await fixture.Library.RenameSelectedNotebookAsync("C# avançado"));
        fixture.Pump();

        fixture.AssertPath("Estudos", "C# avançado", "Rascunho de estudo");
        Assert.Same(editor, fixture.Library.Editor);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(0, fixture.Prompt.Calls);
    }

    [AvaloniaFact]
    public async Task RenamingPage_UpdatesThePathAndPreservesSavedFormatting()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.True(await fixture.Library.RenameCurrentPageAsync("Contratos do sistema"));
        fixture.Pump();

        fixture.AssertPath("Estudos", "Curso C#", "Contratos do sistema");
        Assert.Equal(fixture.Page.Id, fixture.Library.Editor.PageId);
        Assert.Equal(((NotebookTextBlock)fixture.Page.Blocks[0]).Styles, fixture.Library.Editor.Blocks[0].Styles);
        Assert.False(fixture.Library.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task MovingToAnEquallyNamedNotebookInAnotherSpace_ChangesThePathAndSaveDestination()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.True(await fixture.Library.MoveCurrentPageAsync(fixture.OtherNotebook.Id));
        fixture.Pump();

        fixture.AssertPath("Trabalho", "Curso C#", "Interfaces e contratos");
        fixture.Library.Editor.Blocks[0].Text = "Atualização no destino";
        await fixture.Library.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Contains(await fixture.Actions.ListPagesAsync(fixture.OtherNotebook.Id), page => page.Id == fixture.Page.Id);
        Assert.DoesNotContain(await fixture.Actions.ListPagesAsync(fixture.Notebook.Id), page => page.Id == fixture.Page.Id);
    }

    [AvaloniaFact]
    public async Task OpeningAnotherPage_UpdatesOnlyThePagePartOfThePath()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.True(await fixture.Library.OpenPageAsync(fixture.SecondPage.Id));
        fixture.Pump();

        fixture.AssertPath("Estudos", "Curso C#", "Classes e objetos");
        Assert.Equal(fixture.SecondPage.Id, fixture.Library.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task UnfiledPage_DoesNotRetainThePreviouslySelectedSpaceOrNotebook()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.True(await fixture.Library.ShowUnfiledAsync());
        Assert.True(await fixture.Library.OpenPageAsync(fixture.UnfiledPage.Id));
        fixture.Pump();

        fixture.AssertPath("Não organizadas", "Sem caderno", "Ideia solta");
        Assert.Null(fixture.Library.SelectedSpaceId);
        Assert.Null(fixture.Library.SelectedNotebookId);
    }

    [AvaloniaFact]
    public async Task SpaceWithoutSelectedNotebook_ShowsAnExplicitEmptyContext()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.True(await fixture.Library.SelectSpaceAsync(fixture.OtherSpace.Id));
        fixture.Pump();

        fixture.AssertPath("Trabalho", "Escolha um caderno", "Nenhuma página aberta");
        Assert.False(fixture.View.FindControl<NotebookEditorView>("CurrentPageEditor")!.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task CancellingNavigation_PreservesThePathAndUnsavedDraft()
    {
        using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Library.Editor;
        editor.Title = "Rascunho que fica";

        Assert.False(await fixture.Library.SelectSpaceAsync(fixture.OtherSpace.Id));
        fixture.Pump();

        fixture.AssertPath("Estudos", "Curso C#", "Rascunho que fica");
        Assert.Same(editor, fixture.Library.Editor);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(1, fixture.Prompt.Calls);
    }

    [AvaloniaFact]
    public async Task OpeningUnavailablePage_PreservesTheCurrentPath()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.False(await fixture.Library.OpenPageAsync(Guid.NewGuid()));
        fixture.Pump();

        fixture.AssertPath("Estudos", "Curso C#", "Interfaces e contratos");
        Assert.Equal(fixture.Page.Id, fixture.Library.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task CompactContext_KeepsAllLevelsVisibleAndExposesFullLongTitles()
    {
        using var fixture = await Fixture.CreateAsync();
        const string space = "Estudos e pesquisas de engenharia de software 🧵";
        const string notebook = "Curso de C# — fundamentos, arquitetura e prática";
        const string page = "Interfaces, contratos e responsabilidade dos componentes";
        Assert.True(await fixture.Library.RenameSelectedSpaceAsync(space));
        Assert.True(await fixture.Library.RenameSelectedNotebookAsync(notebook));
        Assert.True(await fixture.Library.RenameCurrentPageAsync(page));
        fixture.Window.Width = 860;
        fixture.Pump();

        fixture.AssertPath(space, notebook, page);
        var trail = Assert.IsType<Border>(fixture.View.FindControl<Border>("PageContextTrail"));
        foreach (var (name, title) in new[]
        {
            ("CurrentSpaceLabel", space), ("CurrentNotebookLabel", notebook), ("CurrentPageLabel", page)
        })
        {
            var label = fixture.Label(name);
            Assert.True(label.IsEffectivelyVisible);
            Assert.Equal(title, ToolTip.GetTip(label));
            var point = label.TranslatePoint(default, trail);
            Assert.NotNull(point);
            Assert.True(point.Value.X >= 0 && point.Value.Y >= 0);
            Assert.True(point.Value.X + label.Bounds.Width <= trail.Bounds.Width + 1);
            Assert.True(point.Value.Y + label.Bounds.Height <= trail.Bounds.Height + 1);
        }
        Assert.False(fixture.Library.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task ExistingSidebar_AlreadyReflectsTheSelectedHierarchyByIdentity()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.Equal(fixture.Space.Id, Assert.IsType<KnowledgeSpace>(fixture.View.FindControl<ListBox>("SpaceList")!.SelectedItem).Id);
        Assert.Equal(fixture.Notebook.Id, Assert.IsType<NotebookInfo>(fixture.View.FindControl<ListBox>("NotebookList")!.SelectedItem).Id);
        Assert.Equal(fixture.Page.Id, Assert.IsType<KnowledgePageSummary>(fixture.View.FindControl<ListBox>("PageList")!.SelectedItem).Id);
    }

    [AvaloniaFact]
    public async Task Resizing_AlreadyPreservesTheProseInstanceSelectionAndFormattingHistory()
    {
        using var fixture = await Fixture.CreateAsync();
        var prose = fixture.Prose;
        prose.TextArea.Focus();
        prose.Select(1, 3);
        Assert.True(prose.ApplyStyle(InlineTextStyle.Italic));
        Assert.True(prose.Undo());
        var styles = fixture.Library.Editor.Blocks[0].Styles.ToArray();

        fixture.Window.Width = 860;
        fixture.Pump();

        Assert.Same(prose, fixture.Prose);
        Assert.Equal(1, prose.SelectionStart);
        Assert.Equal(3, prose.SelectionLength);
        Assert.True(prose.CanRedo);
        Assert.Equal(styles, fixture.Library.Editor.Blocks[0].Styles);
    }

    private sealed class Fixture : IDisposable
    {
        public required NotebookTestWorkspace Data { get; init; }
        public required ServiceProvider Services { get; init; }
        public required ILibraryActions Actions { get; init; }
        public required NavigationPrompt Prompt { get; init; }
        public required LibraryViewModel Library { get; init; }
        public required LibraryWorkspaceView View { get; init; }
        public required Window Window { get; init; }
        public required KnowledgeSpace Space { get; init; }
        public required KnowledgeSpace OtherSpace { get; init; }
        public required NotebookInfo Notebook { get; init; }
        public required NotebookInfo OtherNotebook { get; init; }
        public required NotebookSnapshot Page { get; init; }
        public required NotebookSnapshot SecondPage { get; init; }
        public required NotebookSnapshot UnfiledPage { get; init; }
        public NotebookProseEditor Prose => View.GetVisualDescendants().OfType<NotebookProseEditor>().Single();
        public TextBlock Label(string name) => Assert.IsType<TextBlock>(View.FindControl<TextBlock>(name));
        public string? Text(string name) => Label(name).Text;

        public static async Task<Fixture> CreateAsync()
        {
            var data = new NotebookTestWorkspace();
            var prompt = new NavigationPrompt();
            var services = await DesktopComposition.CreateAsync(data.DirectoryPath, unsavedChangesPrompt: prompt);
            var actions = services.GetRequiredService<ILibraryActions>();
            var space = await actions.CreateSpaceAsync("Estudos");
            var otherSpace = await actions.CreateSpaceAsync("Trabalho");
            var notebook = await actions.CreateNotebookAsync(space.Id, "Curso C#");
            var otherNotebook = await actions.CreateNotebookAsync(otherSpace.Id, "Curso C#");
            var page = new NotebookSnapshot(Guid.NewGuid(), "Interfaces e contratos",
                [new NotebookTextBlock(Guid.NewGuid(), "abcdef")
                { Styles = [new TextStyleSpan(0, 6, InlineTextStyle.Bold) { FontSize = 18, Foreground = "blue" }] }]);
            var secondPage = new NotebookSnapshot(Guid.NewGuid(), "Classes e objetos", [new NotebookTextBlock(Guid.NewGuid(), "Segundo conteúdo")]);
            var unfiled = new NotebookSnapshot(Guid.NewGuid(), "Ideia solta", [new NotebookTextBlock(Guid.NewGuid(), "Não organizada")]);
            Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(page, notebook.Id)).Status);
            Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(secondPage, notebook.Id)).Status);
            Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(unfiled, null)).Status);
            var library = services.GetRequiredService<LibraryViewModel>();
            await library.InitializeAsync();
            Assert.True(await library.SelectSpaceAsync(space.Id));
            Assert.True(await library.SelectNotebookAsync(notebook.Id));
            Assert.True(await library.OpenPageAsync(page.Id));
            var view = new LibraryWorkspaceView(library);
            var window = new Window { Width = 1340, Height = 940, Content = view };
            window.Show();
            var fixture = new Fixture
            {
                Data = data,
                Services = services,
                Actions = actions,
                Prompt = prompt,
                Library = library,
                View = view,
                Window = window,
                Space = space,
                OtherSpace = otherSpace,
                Notebook = notebook,
                OtherNotebook = otherNotebook,
                Page = page,
                SecondPage = secondPage,
                UnfiledPage = unfiled
            };
            fixture.Pump();
            return fixture;
        }

        public void AssertPath(string space, string notebook, string page)
        {
            Assert.Equal(space, Text("CurrentSpaceLabel"));
            Assert.Equal(notebook, Text("CurrentNotebookLabel"));
            Assert.Equal(page, Text("CurrentPageLabel"));
        }

        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Dispose() { View.DataContext = null; Window.Close(); Services.Dispose(); Data.Dispose(); }
    }
}
