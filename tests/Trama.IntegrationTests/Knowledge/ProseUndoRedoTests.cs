using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class ProseUndoRedoTests
{
    [AvaloniaFact]
    public async Task BoldFormatting_UndoAndRedoRestoreTheStyleWithoutChangingText()
    {
        using var fixture = await EditorFixture.CreateAsync("abcdef");
        fixture.Prose.Select(1, 3);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        Assert.Equal(new TextStyleSpan(1, 3, InlineTextStyle.Bold), Assert.Single(fixture.Block.Styles));

        Assert.True(fixture.Prose.Undo());
        Assert.Equal("abcdef", fixture.Block.Text);
        Assert.Empty(fixture.Block.Styles);
        Assert.True(fixture.Prose.Redo());
        Assert.Equal(new TextStyleSpan(1, 3, InlineTextStyle.Bold), Assert.Single(fixture.Block.Styles));
        Assert.Equal("abcdef", fixture.Prose.Text);
    }

    [AvaloniaFact]
    public async Task OverlappingBoldAndItalic_UndoRestoresThePreviousFormattingStep()
    {
        using var fixture = await EditorFixture.CreateAsync("abcdef");
        fixture.Prose.Select(0, 4);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        var bold = fixture.Block.Styles.ToArray();
        fixture.Prose.Select(2, 4);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Italic));
        var overlap = fixture.Block.Styles.ToArray();

        Assert.True(fixture.Prose.Undo());
        Assert.Equal(bold, fixture.Block.Styles);
        Assert.True(fixture.Prose.Redo());
        Assert.Equal(overlap, fixture.Block.Styles);
        Assert.Equal("abcdef", fixture.Block.Text);
    }

    [AvaloniaFact]
    public async Task RemovingExistingBold_UndoRestoresTheOriginalRange()
    {
        var original = new TextStyleSpan(0, 4, InlineTextStyle.Bold);
        using var fixture = await EditorFixture.CreateAsync("abcdef", [original]);
        fixture.Prose.Select(0, 4);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        Assert.Empty(fixture.Block.Styles);

        Assert.True(fixture.Prose.Undo());
        Assert.Equal(original, Assert.Single(fixture.Block.Styles));
        Assert.True(fixture.Prose.Redo());
        Assert.Empty(fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task DeletingFormattedText_UndoRestoresTextAndItsOriginalStyles()
    {
        var original = new TextStyleSpan(2, 2, InlineTextStyle.Bold | InlineTextStyle.Italic);
        using var fixture = await EditorFixture.CreateAsync("abCDxy", [original]);
        fixture.Prose.Document.Remove(2, 2);
        Assert.Equal("abxy", fixture.Block.Text);

        Assert.True(fixture.Prose.Undo());
        Assert.Equal("abCDxy", fixture.Block.Text);
        Assert.Equal(original, Assert.Single(fixture.Block.Styles));
        Assert.True(fixture.Prose.Redo());
        Assert.Equal("abxy", fixture.Block.Text);
        Assert.Empty(fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task ReplacingFormattedSelection_UndoAndRedoRestoreCompleteStates()
    {
        var original = new TextStyleSpan(2, 2, InlineTextStyle.Bold);
        using var fixture = await EditorFixture.CreateAsync("abCDxy", [original]);
        fixture.Prose.Document.Replace(1, 4, "q");
        var replacementStyles = fixture.Block.Styles.ToArray();
        Assert.Equal("aqy", fixture.Block.Text);

        Assert.True(fixture.Prose.Undo());
        Assert.Equal("abCDxy", fixture.Block.Text);
        Assert.Equal(original, Assert.Single(fixture.Block.Styles));
        Assert.True(fixture.Prose.Redo());
        Assert.Equal("aqy", fixture.Block.Text);
        Assert.Equal(replacementStyles, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task MixedTypingAndFormatting_UndoFollowsTheOrderOfUserActions()
    {
        using var fixture = await EditorFixture.CreateAsync("abcdef");
        fixture.Prose.Document.Insert(0, "X");
        fixture.Prose.Select(1, 3);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));

        Assert.True(fixture.Prose.Undo());
        Assert.Equal("Xabcdef", fixture.Block.Text);
        Assert.Empty(fixture.Block.Styles);
        Assert.True(fixture.Prose.Undo());
        Assert.Equal("abcdef", fixture.Block.Text);
        Assert.True(fixture.Prose.Redo());
        Assert.Equal("Xabcdef", fixture.Block.Text);
        Assert.Empty(fixture.Block.Styles);
        Assert.True(fixture.Prose.Redo());
        Assert.Equal(new TextStyleSpan(1, 3, InlineTextStyle.Bold), Assert.Single(fixture.Block.Styles));
    }

    [AvaloniaFact]
    public async Task Saving_DoesNotEraseHistoryAndUndoMarksThePageForPersistence()
    {
        using var fixture = await EditorFixture.CreateAsync("aula");
        fixture.Prose.Select(0, 4);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        await fixture.Editor.SaveCommand.ExecuteAsync(null);
        Assert.False(fixture.Editor.HasUnsavedChanges);

        Assert.True(fixture.Prose.Undo());
        Assert.True(fixture.Editor.HasUnsavedChanges);
        await fixture.Editor.SaveCommand.ExecuteAsync(null);

        var saved = Assert.IsType<NotebookTextBlock>(Assert.Single(fixture.Actions.Saved[1].Blocks));
        Assert.Equal("aula", saved.Text);
        Assert.Empty(saved.Styles);
        Assert.Equal(fixture.Editor.PageId, fixture.Actions.Saved[1].Id);
    }

    [AvaloniaFact]
    public async Task AutoSave_PreservesUndoHistoryAndPersistsTheUndoneFormatting()
    {
        using var fixture = await EditorFixture.CreateAsync("aula", autoSave: true);
        fixture.Prose.Select(0, 4);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        await fixture.Actions.WaitForSaveAsync(1);
        Assert.False(fixture.Editor.IsBusy);
        Assert.False(fixture.Editor.HasUnsavedChanges);

        Assert.True(fixture.Prose.Undo());
        Assert.True(fixture.Editor.HasUnsavedChanges);
        var saved = await fixture.Actions.WaitForSaveAsync(2);

        Assert.Equal(fixture.Editor.PageId, saved.Id);
        Assert.Empty(Assert.IsType<NotebookTextBlock>(Assert.Single(saved.Blocks)).Styles);
    }

    [AvaloniaFact]
    public async Task TypingAfterUndo_InvalidatesTheAbandonedRedoBranch()
    {
        using var fixture = await EditorFixture.CreateAsync("abc");
        fixture.Prose.Document.Insert(3, "1");
        Assert.True(fixture.Prose.Undo());
        Assert.True(fixture.Prose.CanRedo);
        fixture.Prose.Document.Insert(3, "2");

        Assert.False(fixture.Prose.CanRedo);
        Assert.False(fixture.Prose.Redo());
        Assert.Equal("abc2", fixture.Block.Text);
    }

    [AvaloniaFact]
    public async Task UndoInOneBlock_DoesNotChangeAnotherBlocksHistoryOrContent()
    {
        using var fixture = await EditorFixture.CreateAsync("primeiro", secondText: "segundo");
        var editors = fixture.Window.GetVisualDescendants().OfType<NotebookProseEditor>().ToArray();
        var first = editors.Single(e => ((NotebookBlockEditor)e.DataContext!).Id == fixture.Block.Id);
        var second = editors.Single(e => ((NotebookBlockEditor)e.DataContext!).Id != fixture.Block.Id);
        first.Document.Insert(first.Text.Length, "1");
        second.Document.Insert(second.Text.Length, "2");

        Assert.True(first.Undo());
        Assert.Equal("primeiro", fixture.Editor.Blocks[0].Text);
        Assert.Equal("segundo2", fixture.Editor.Blocks[1].Text);
        Assert.True(second.CanUndo);
        Assert.True(first.Redo());
        Assert.Equal("primeiro1", fixture.Editor.Blocks[0].Text);
        Assert.Equal("segundo2", fixture.Editor.Blocks[1].Text);
    }

    [AvaloniaFact]
    public async Task LoadingAnotherPage_StartsWithNoUndoOrRedoFromThePreviousPage()
    {
        using var fixture = await EditorFixture.CreateAsync("abcdef");
        fixture.Prose.Document.Insert(0, "X");
        Assert.True(fixture.Prose.Undo());
        Assert.True(fixture.Prose.CanRedo);
        var destination = new NotebookSnapshot(Guid.NewGuid(), "Outra página",
            [new NotebookTextBlock(Guid.NewGuid(), "abcdef") { Styles = [new(0, 2, InlineTextStyle.Italic)] }]);
        fixture.Actions.Loaded = destination;

        Assert.True(await fixture.Editor.TryLoadAsync(destination.Id));
        fixture.Pump();

        Assert.False(fixture.Prose.CanUndo);
        Assert.False(fixture.Prose.CanRedo);
        Assert.Equal(destination.Id, fixture.Editor.PageId);
        Assert.Equal(new TextStyleSpan(0, 2, InlineTextStyle.Italic), Assert.Single(fixture.Block.Styles));
    }

    [AvaloniaFact]
    public async Task Keyboard_CtrlZAndCtrlY_UndoAndRedoFormatting()
    {
        using var fixture = await EditorFixture.CreateAsync("abcdef");
        VerifyKeyboard(fixture, PhysicalKey.Y, RawInputModifiers.Control);
    }

    [AvaloniaFact]
    public async Task Keyboard_CtrlZAndCtrlShiftZ_UndoAndRedoFormatting()
    {
        using var fixture = await EditorFixture.CreateAsync("abcdef");
        VerifyKeyboard(fixture, PhysicalKey.Z, RawInputModifiers.Control | RawInputModifiers.Shift);
    }

    private static void VerifyKeyboard(EditorFixture fixture, PhysicalKey redoKey, RawInputModifiers modifiers)
    {
        fixture.Prose.Select(0, 3);
        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        fixture.Prose.TextArea.Focus();
        fixture.Window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
        fixture.Window.KeyReleaseQwerty(PhysicalKey.Z, RawInputModifiers.Control);
        fixture.Pump();
        Assert.Empty(fixture.Block.Styles);

        fixture.Window.KeyPressQwerty(redoKey, modifiers);
        fixture.Window.KeyReleaseQwerty(redoKey, modifiers);
        fixture.Pump();
        Assert.Equal(new TextStyleSpan(0, 3, InlineTextStyle.Bold), Assert.Single(fixture.Block.Styles));
        Assert.Equal("abcdef", fixture.Block.Text);
    }

    private sealed class EditorFixture : IDisposable
    {
        public required NotebookEditorViewModel Editor { get; init; }
        public required HistoryActions Actions { get; init; }
        public required Window Window { get; init; }
        public NotebookBlockEditor Block => Editor.Blocks[0];
        public NotebookProseEditor Prose => Window.GetVisualDescendants().OfType<NotebookProseEditor>()
            .Single(e => ((NotebookBlockEditor)e.DataContext!).Id == Block.Id);

        public static async Task<EditorFixture> CreateAsync(string text, IReadOnlyList<TextStyleSpan>? styles = null,
            bool autoSave = false, string? secondText = null)
        {
            var blocks = new List<NotebookBlock> { new NotebookTextBlock(Guid.NewGuid(), text) { Styles = styles ?? Array.Empty<TextStyleSpan>() } };
            if (secondText is not null) blocks.Add(new NotebookTextBlock(Guid.NewGuid(), secondText));
            var actions = new HistoryActions { Loaded = new(Guid.NewGuid(), "Página de teste", blocks) };
            var editor = autoSave ? new NotebookEditorViewModel(actions, TimeProvider.System) : new NotebookEditorViewModel(actions);
            Assert.True(await editor.TryLoadAsync(actions.Loaded.Id));
            var window = new Window { Width = 1000, Height = 800, Content = new NotebookEditorView(editor) };
            window.Show();
            var fixture = new EditorFixture { Editor = editor, Actions = actions, Window = window };
            fixture.Pump();
            return fixture;
        }

        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Dispose() { Editor.Dispose(); Window.Close(); }
    }

    private sealed class HistoryActions : INotebookActions
    {
        private readonly TaskCompletionSource<NotebookSnapshot>[] _observations = Enumerable.Range(0, 4)
            .Select(_ => new TaskCompletionSource<NotebookSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        public required NotebookSnapshot Loaded { get; set; }
        public List<NotebookSnapshot> Saved { get; } = [];
        public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            Saved.Add(notebook);
            _observations[Saved.Count - 1].TrySetResult(notebook);
            return Task.FromResult(new NotebookSaveResult(NotebookSaveStatus.Saved));
        }
        public Task<NotebookSnapshot> WaitForSaveAsync(int count) => _observations[count - 1].Task.WaitAsync(TimeSpan.FromSeconds(6));
        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<NotebookSnapshot?>(Loaded);
        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }
}
