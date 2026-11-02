using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NotebookLayoutRefinementTests
{
    [AvaloniaFact]
    public async Task EveryBlock_IncludingProse_HasItsOwnSequentialMarginMarker()
    {
        using var fixture = await Fixture.CreateAsync();

        Assert.Equal(new[] { "01", "02", "03", "04" }, fixture.Markers().Select(label => label.Text));
        Assert.All(fixture.Markers(), label => Assert.True(label.IsEffectivelyVisible));
        Assert.Equal(fixture.Library.Editor.Blocks.Select(block => block.Id),
            fixture.Markers().Select(label => Assert.IsType<NotebookBlockEditor>(label.DataContext).Id));
        Assert.False(fixture.Library.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task BlockMarkers_StayOnTheContinuousMarginInsteadOfDriftingIntoTheContent()
    {
        using var fixture = await Fixture.CreateAsync();
        var line = Assert.IsType<Border>(fixture.EditorView.FindControl<Border>("NotebookMarginLine"));
        var linePoint = line.TranslatePoint(default, fixture.EditorView)!.Value;
        var dots = fixture.EditorView.GetVisualDescendants().OfType<Border>()
            .Where(border => border.Name == "BlockMarkerDot").ToArray();

        Assert.Equal(4, dots.Length);
        foreach (var dot in dots)
        {
            var point = dot.TranslatePoint(default, fixture.EditorView)!.Value;
            Assert.InRange(Math.Abs(point.X + dot.Bounds.Width / 2 - linePoint.X - line.Bounds.Width / 2), 0, 1);
        }
    }

    [AvaloniaFact]
    public async Task ReorderingAndRemovingBlocks_UpdatesAllMarkerNumbersAndIdentities()
    {
        using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Library.Editor;
        var moved = editor.Blocks[3].Id;
        editor.MoveBlockUpCommand.Execute(moved);
        editor.RemoveBlockCommand.Execute(editor.Blocks[1].Id);
        fixture.Pump();

        Assert.Equal(new[] { "01", "02", "03" }, fixture.Markers().Select(label => label.Text));
        Assert.All(fixture.Markers(), label => Assert.True(label.IsEffectivelyVisible));
        Assert.Equal(editor.Blocks.Select(block => block.Id),
            fixture.Markers().Select(label => Assert.IsType<NotebookBlockEditor>(label.DataContext).Id));
    }

    [AvaloniaFact]
    public async Task Sidebar_CanBeCollapsedAndReopenedWithoutLosingTheDraftOrSelection()
    {
        using var fixture = await Fixture.CreateAsync();
        var prose = fixture.Prose;
        prose.TextArea.Focus();
        prose.Document.Insert(prose.Text.Length, " rascunho");
        prose.Select(1, 3);
        var sidebar = fixture.Control("LibrarySidebar");
        Assert.True(sidebar.IsEffectivelyVisible);

        fixture.Click("ToggleLibraryButton");
        Assert.False(sidebar.IsEffectivelyVisible);
        fixture.Click("ToggleLibraryButton");

        Assert.True(sidebar.IsEffectivelyVisible);
        Assert.Same(prose, fixture.Prose);
        Assert.Equal(1, prose.SelectionStart);
        Assert.Equal(3, prose.SelectionLength);
        Assert.True(prose.CanUndo);
        Assert.Contains("rascunho", fixture.Library.Editor.Blocks[0].Text);
        Assert.Equal(fixture.PageId, fixture.Library.Editor.PageId);
    }

    [AvaloniaFact]
    public async Task CompactWindow_CollapsesTheSidebarAndAllowsOpeningItAgain()
    {
        using var fixture = await Fixture.CreateAsync();
        fixture.Window.Width = 860;
        fixture.Window.Height = 680;
        fixture.Pump();

        Assert.False(fixture.Control("LibrarySidebar").IsEffectivelyVisible);
        var toggle = fixture.Control("ToggleLibraryButton");
        Assert.True(toggle.IsEffectivelyVisible);
        fixture.Click("ToggleLibraryButton");
        Assert.True(fixture.Control("LibrarySidebar").IsEffectivelyVisible);
        fixture.Click("ToggleLibraryButton");
        Assert.False(fixture.Control("LibrarySidebar").IsEffectivelyVisible);
        Assert.True(fixture.EditorView.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task ExpandedSearchFilters_ScrollWithTheSidebarInsteadOfDisplacingItsNavigation()
    {
        using var fixture = await Fixture.CreateAsync();
        var input = fixture.Control("SearchTagInput");
        var filters = Assert.Single(input.GetLogicalAncestors().OfType<Expander>());
        filters.IsExpanded = true;
        fixture.Window.Height = 680;
        fixture.Pump();

        var scroll = Assert.Single(input.GetVisualAncestors().OfType<ScrollViewer>(),
            viewer => viewer.Name == "LibrarySidebarScroll");
        Assert.Contains(fixture.Control("PageList"), scroll.GetVisualDescendants());
        Assert.InRange(scroll.Bounds.Height, 100, fixture.Window.Bounds.Height);
        filters.IsExpanded = false;
        fixture.Pump();
        Assert.False(input.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task SearchFilters_CanBeOpenedAndClosedByKeyboardWithoutLosingCriteria()
    {
        using var fixture = await Fixture.CreateAsync();
        fixture.Library.SearchQuery = "contratos";
        fixture.Library.SearchTag = "ESTUDO";
        var input = fixture.Control("SearchTagInput");
        var filters = Assert.Single(input.GetLogicalAncestors().OfType<Expander>());
        var toggle = Assert.Single(filters.GetVisualDescendants().OfType<ToggleButton>());
        Assert.True(toggle.Focus());

        fixture.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        fixture.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        fixture.Pump();
        Assert.True(filters.IsExpanded);
        fixture.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        fixture.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        fixture.Pump();

        Assert.False(filters.IsExpanded);
        Assert.False(input.IsEffectivelyVisible);
        Assert.Equal("contratos", fixture.Library.SearchQuery);
        Assert.Equal("ESTUDO", fixture.Library.SearchTag);
        Assert.False(fixture.Library.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task CompactFormatting_IsReopenableAndDoesNotConsumeMultipleRowsWhenCollapsed()
    {
        using var fixture = await Fixture.CreateAsync();
        fixture.Window.Width = 860;
        fixture.Pump();
        var bar = fixture.EditorView.FindControl<Border>("ProseFormattingBar")!;
        var picker = fixture.EditorView.FindControl<ComboBox>("ProseForegroundPicker")!;

        Assert.InRange(bar.Bounds.Height, 1, 52);
        Assert.False(picker.IsEffectivelyVisible);
        fixture.ClickEditor("ToggleFormattingButton");
        Assert.True(picker.IsEffectivelyVisible);
        fixture.ClickEditor("ToggleFormattingButton");
        Assert.False(picker.IsEffectivelyVisible);
        Assert.InRange(bar.Bounds.Height, 1, 52);
    }

    [AvaloniaFact]
    public async Task ReopeningFormatting_PreservesItsProseTargetAndAppliesToTheOriginalSelection()
    {
        using var fixture = await Fixture.CreateAsync();
        fixture.Window.Width = 860;
        fixture.Pump();
        fixture.Prose.TextArea.Focus();
        fixture.Prose.Select(1, 3);
        fixture.ClickEditor("ToggleFormattingButton");
        var picker = fixture.EditorView.FindControl<ComboBox>("ProseForegroundPicker")!;
        Assert.True(picker.IsEffectivelyVisible);
        picker.Focus();
        picker.SelectedItem = picker.Items.OfType<ComboBoxItem>().Single(item => item.Tag?.ToString() == "teal");
        fixture.Pump();

        var style = Assert.Single(fixture.Library.Editor.Blocks[0].Styles);
        Assert.Equal((1, 3, "teal"), (style.Offset, style.Length, style.Foreground));
        Assert.Equal((1, 3), (fixture.Prose.SelectionStart, fixture.Prose.SelectionLength));
        Assert.True(fixture.Prose.CanUndo);
    }

    [AvaloniaFact]
    public async Task BlockMenu_ReplacesTheExposedArrowAndCrossButtons()
    {
        using var fixture = await Fixture.CreateAsync();
        var buttons = fixture.EditorView.GetVisualDescendants().OfType<Button>().ToArray();

        Assert.DoesNotContain(buttons, button => button.Content as string is "↑" or "↓" or "×");
        Assert.Equal(4, buttons.Count(button => button.Name == "BlockActionsButton" && button.IsEffectivelyVisible));
    }

    [AvaloniaFact]
    public async Task BlockMenu_CanMoveAndRemoveItsOwnBlockWithoutChangingTheOtherContent()
    {
        using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Library.Editor;
        var target = editor.Blocks[1];
        var untouched = editor.Blocks[0].Text;
        var button = fixture.EditorView.GetVisualDescendants().OfType<Button>()
            .SingleOrDefault(control => control.Name == "BlockActionsButton" && control.DataContext == target);
        Assert.NotNull(button);
        button.Flyout!.ShowAt(button);
        fixture.Pump();
        var flyout = Assert.IsType<MenuFlyout>(button.Flyout);
        var earlier = Assert.Single(flyout.Items.OfType<MenuItem>(), item => item.Header as string == "Antecipar bloco");
        Assert.True(earlier.Command?.CanExecute(earlier.CommandParameter));
        earlier.Command!.Execute(earlier.CommandParameter);
        fixture.Pump();
        Assert.Same(target, editor.Blocks[0]);
        var remove = Assert.Single(flyout.Items.OfType<MenuItem>(), item => item.Header as string == "Remover bloco");
        Assert.True(remove.Command?.CanExecute(remove.CommandParameter));
        remove.Command!.Execute(remove.CommandParameter);
        fixture.Pump();
        Assert.DoesNotContain(editor.Blocks, block => block.Id == target.Id);
        Assert.Equal(untouched, editor.Blocks[0].Text);
        Assert.True(editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task Resizing_AlreadyKeepsTheSameEditorAndUnsavedContent()
    {
        using var fixture = await Fixture.CreateAsync();
        var prose = fixture.Prose;
        prose.Document.Insert(prose.Text.Length, " continuar");
        fixture.Window.Width = 860;
        fixture.Pump();
        fixture.Window.Width = 1340;
        fixture.Pump();

        Assert.Same(prose, fixture.Prose);
        Assert.Contains("continuar", fixture.Library.Editor.Blocks[0].Text);
        Assert.True(prose.CanUndo);
    }

    private sealed class Fixture : IDisposable
    {
        public required NotebookTestWorkspace Data { get; init; }
        public required ServiceProvider Services { get; init; }
        public required LibraryViewModel Library { get; init; }
        public required LibraryWorkspaceView View { get; init; }
        public required Window Window { get; init; }
        public required Guid PageId { get; init; }
        public NotebookEditorView EditorView => View.GetVisualDescendants().OfType<NotebookEditorView>().Single();
        public NotebookProseEditor Prose => EditorView.GetVisualDescendants().OfType<NotebookProseEditor>()
            .Single(control => control.DataContext == Library.Editor.Blocks.First(block => block.IsText));

        public static async Task<Fixture> CreateAsync()
        {
            var data = new NotebookTestWorkspace();
            var services = await DesktopComposition.CreateAsync(data.DirectoryPath);
            var actions = services.GetRequiredService<ILibraryActions>();
            var space = await actions.CreateSpaceAsync("Estudos");
            var notebook = await actions.CreateNotebookAsync(space.Id, "Curso C#");
            var page = new NotebookSnapshot(Guid.NewGuid(), "Refino", [
                new NotebookTextBlock(Guid.NewGuid(), "abcdef"),
                new NotebookCodeBlock(Guid.NewGuid(), "Console.WriteLine(42);", "csharp"),
                new NotebookTextBlock(Guid.NewGuid(), "Outro parágrafo"),
                new NotebookCodeBlock(Guid.NewGuid(), "print(42)", "python")]);
            Assert.Equal(NotebookSaveStatus.Saved, (await actions.SaveInNotebookAsync(page, notebook.Id)).Status);
            var library = services.GetRequiredService<LibraryViewModel>();
            await library.InitializeAsync();
            Assert.True(await library.SelectSpaceAsync(space.Id));
            Assert.True(await library.SelectNotebookAsync(notebook.Id));
            Assert.True(await library.OpenPageAsync(page.Id));
            var view = new LibraryWorkspaceView(library);
            var window = new Window { Width = 1340, Height = 940, Content = view };
            window.Show();
            var fixture = new Fixture { Data = data, Services = services, Library = library, View = view, Window = window, PageId = page.Id };
            fixture.Pump();
            return fixture;
        }

        public TextBlock[] Markers() => EditorView.GetVisualDescendants().OfType<TextBlock>()
            .Where(label => label.Name == "BlockOrderLabel").ToArray();
        public Control Control(string name) => Assert.IsAssignableFrom<Control>(View.FindControl<Control>(name));
        public void Click(string name) { Assert.IsType<Button>(Control(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(); }
        public void ClickEditor(string name) { Assert.IsType<Button>(EditorView.FindControl<Button>(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(); }
        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Dispose() { View.DataContext = null; Window.Close(); Services.Dispose(); Data.Dispose(); }
    }
}
