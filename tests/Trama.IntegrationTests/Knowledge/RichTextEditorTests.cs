using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class RichTextEditorTests
{
    private const string SizePicker = "ProseFontSizePicker";
    private const string ColorPicker = "ProseForegroundPicker";
    private const string HighlightPicker = "ProseHighlightPicker";
    private const string ClearButton = "ClearProseFormattingButton";

    [AvaloniaFact]
    public async Task Toolbar_OffersOneSetOfControlsForAllProseBlocks()
    {
        using var fixture = await Fixture.CreateAsync(secondText: true);

        foreach (var name in new[] { SizePicker, ColorPicker, HighlightPicker, ClearButton })
            Assert.Single(fixture.View.GetVisualDescendants().OfType<Control>(), control => control.Name == name);
    }

    [AvaloniaFact]
    public async Task SizePicker_ChangesOnlyTheSelectionAndPreservesOtherAttributes()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(2, 2);

        fixture.Choose(SizePicker, "32");

        Assert.Equal("abcdef", fixture.Block.Text);
        Assert.Equal(new[] { RichSpan(0, 2), RichSpan(2, 2) with { FontSize = 32 }, RichSpan(4, 2) }, fixture.Block.Styles);
        Assert.True(fixture.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task ForegroundPicker_ChangesOnlyTheSelectionAndPreservesOtherAttributes()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        fixture.Choose(ColorPicker, "blue");

        Assert.Equal(new[] { RichSpan(0, 1), RichSpan(1, 3) with { Foreground = "blue" }, RichSpan(4, 2) }, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task HighlightPicker_ChangesOnlyTheSelectionAndPreservesOtherAttributes()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        fixture.Choose(HighlightPicker, "violet");

        Assert.Equal(new[] { RichSpan(0, 1), RichSpan(1, 3) with { Highlight = "violet" }, RichSpan(4, 2) }, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task DefaultSize_RemovesOnlyExplicitSizeFromTheSelection()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        fixture.Choose(SizePicker, "default");

        Assert.Equal(new[] { RichSpan(0, 1), RichSpan(1, 3) with { FontSize = null }, RichSpan(4, 2) }, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task DefaultForeground_RemovesOnlyExplicitColorFromTheSelection()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        fixture.Choose(ColorPicker, "default");

        Assert.Equal(new[] { RichSpan(0, 1), RichSpan(1, 3) with { Foreground = null }, RichSpan(4, 2) }, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task NoHighlight_RemovesOnlyHighlightFromTheSelection()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        fixture.Choose(HighlightPicker, "none");

        Assert.Equal(new[] { RichSpan(0, 1), RichSpan(1, 3) with { Highlight = null }, RichSpan(4, 2) }, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task ClearFormatting_RemovesAllSelectedAttributesWithoutDeletingText()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        fixture.Clear();

        Assert.Equal("abcdef", fixture.Block.Text);
        Assert.Equal(new[] { RichSpan(0, 1), RichSpan(4, 2) }, fixture.Block.Styles);
        Assert.Equal(1, fixture.Prose.SelectionStart);
        Assert.Equal(3, fixture.Prose.SelectionLength);
        Assert.True(fixture.Prose.TextArea.IsKeyboardFocusWithin);
    }

    [AvaloniaFact]
    public async Task EmptySelection_DoesNotFormatMarkDirtyOrCreateHistory()
    {
        using var fixture = await Fixture.CreateAsync();
        fixture.Select(3, 0);

        fixture.Choose(SizePicker, "24");
        fixture.Clear();

        Assert.Empty(fixture.Block.Styles);
        Assert.False(fixture.Editor.HasUnsavedChanges);
        Assert.False(fixture.Prose.CanUndo);
        Assert.Equal("abcdef", fixture.Block.Text);
    }

    [AvaloniaFact]
    public async Task ReselectingTheCurrentValue_DoesNotCreateDirtyStateOrHistory()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(0, 6);

        fixture.Choose(SizePicker, "24");

        Assert.False(fixture.Editor.HasUnsavedChanges);
        Assert.False(fixture.Prose.CanUndo);
        Assert.Equal(RichSpan(), Assert.Single(fixture.Block.Styles));
    }

    [AvaloniaFact]
    public async Task MixedSizes_ShowNoSingleChoiceAndApplyingSizeKeepsTheDifferentColors()
    {
        using var fixture = await Fixture.CreateAsync(
            [RichSpan(0, 3) with { FontSize = 16 }, RichSpan(3, 3) with { Foreground = "blue" }]);
        fixture.Select(0, 6);
        Assert.Null(fixture.Picker(SizePicker).SelectedItem);

        fixture.Choose(SizePicker, "18");

        Assert.Equal(new[] { RichSpan(0, 3) with { FontSize = 18 }, RichSpan(3, 3) with { FontSize = 18, Foreground = "blue" } }, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task SelectingLoadedText_SynchronizesTheToolbarWithoutEditingThePage()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);

        Assert.Equal("24", Assert.IsType<ComboBoxItem>(fixture.Picker(SizePicker).SelectedItem).Tag?.ToString());
        Assert.Equal("teal", Assert.IsType<ComboBoxItem>(fixture.Picker(ColorPicker).SelectedItem).Tag?.ToString());
        Assert.Equal("amber", Assert.IsType<ComboBoxItem>(fixture.Picker(HighlightPicker).SelectedItem).Tag?.ToString());
        Assert.False(fixture.Editor.HasUnsavedChanges);
        Assert.False(fixture.Prose.CanUndo);
    }

    [AvaloniaFact]
    public async Task SwitchingActiveProseBlock_FormatsOnlyTheNewTarget()
    {
        using var fixture = await Fixture.CreateAsync(secondText: true);
        fixture.Select(0, 3);
        var second = fixture.View.GetVisualDescendants().OfType<NotebookProseEditor>()
            .Single(prose => ((NotebookBlockEditor)prose.DataContext!).Id != fixture.Block.Id);
        second.TextArea.Focus();
        second.Select(1, 3);
        fixture.Pump();

        fixture.Choose(ColorPicker, "blue", second);

        Assert.Empty(fixture.Block.Styles);
        Assert.Equal(new TextStyleSpan(1, 3, InlineTextStyle.None) { Foreground = "blue" },
            Assert.Single(fixture.Editor.Blocks[1].Styles));
    }

    [AvaloniaFact]
    public async Task FocusingCode_DisablesProseFormattingAndLeavesThePageUntouched()
    {
        using var fixture = await Fixture.CreateAsync(includeCode: true);
        fixture.Select(0, 3);
        var code = Assert.Single(fixture.View.GetVisualDescendants().OfType<NotebookCodeEditor>());
        code.TextArea.Focus();
        fixture.Pump();

        Assert.False(fixture.Picker(SizePicker).IsEffectivelyEnabled);
        Assert.False(fixture.Picker(ColorPicker).IsEffectivelyEnabled);
        Assert.False(fixture.Picker(HighlightPicker).IsEffectivelyEnabled);
        Assert.False(fixture.ClearControl.IsEffectivelyEnabled);
        Assert.False(fixture.Editor.HasUnsavedChanges);
        Assert.Empty(fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task KeyboardUndoRedo_RestoresSizeAndTheOtherAttributesTogether()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);
        fixture.Choose(SizePicker, "32");
        var updated = fixture.Block.Styles.ToArray();

        fixture.Key(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.Equal(RichSpan(), Assert.Single(fixture.Block.Styles));
        fixture.Key(PhysicalKey.Y, RawInputModifiers.Control);
        Assert.Equal(updated, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task ClearingFormatting_IsOneUndoableAction()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);
        fixture.Clear();
        var cleared = fixture.Block.Styles.ToArray();

        Assert.True(fixture.Prose.Undo());
        Assert.Equal(RichSpan(), Assert.Single(fixture.Block.Styles));
        Assert.False(fixture.Prose.CanUndo);
        Assert.True(fixture.Prose.Redo());
        Assert.Equal(cleared, fixture.Block.Styles);
    }

    [AvaloniaFact]
    public async Task SaveThenUndo_PreservesHistoryAndSavesTheRestoredAttributes()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);
        fixture.Choose(ColorPicker, "blue");
        await fixture.Editor.SaveCommand.ExecuteAsync(null);
        Assert.False(fixture.Editor.HasUnsavedChanges);

        Assert.True(fixture.Prose.Undo());
        Assert.True(fixture.Editor.HasUnsavedChanges);
        await fixture.Editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal(RichSpan(), Assert.Single(Assert.IsType<NotebookTextBlock>(fixture.Actions.Saved[^1].Blocks[0]).Styles));
        Assert.True(fixture.Prose.CanRedo);
    }

    [AvaloniaFact]
    public async Task AutoSave_PersistsToolbarFormattingAndRetainsUndoHistory()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()], autoSave: true);
        fixture.Select(0, 6);
        fixture.Choose(HighlightPicker, "violet");

        var saved = await fixture.Actions.FirstSave.Task.WaitAsync(TimeSpan.FromSeconds(8));

        Assert.Equal(fixture.Editor.PageId, saved.Id);
        Assert.Equal(RichSpan() with { Highlight = "violet" },
            Assert.Single(Assert.IsType<NotebookTextBlock>(saved.Blocks[0]).Styles));
        Assert.True(fixture.Prose.CanUndo);
    }

    [AvaloniaFact]
    public async Task NewFormattingAfterUndo_AbandonsThePreviousRedoBranch()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(1, 3);
        fixture.Choose(SizePicker, "32");
        Assert.True(fixture.Prose.Undo());
        fixture.Pump();
        Assert.True(fixture.Prose.CanRedo);

        fixture.Choose(ColorPicker, "blue");

        Assert.False(fixture.Prose.CanRedo);
        Assert.Equal(24, fixture.Block.Styles[1].FontSize);
        Assert.Equal("blue", fixture.Block.Styles[1].Foreground);
    }

    [AvaloniaFact]
    public async Task LoadingAnotherPage_DoesNotApplyThePreviousToolbarState()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(0, 6);
        _ = fixture.Picker(SizePicker);
        var destination = new NotebookSnapshot(Guid.NewGuid(), "Outra página",
            [new NotebookTextBlock(Guid.NewGuid(), "abcdef")]);
        fixture.Actions.Loaded = destination;

        Assert.True(await fixture.Editor.TryLoadAsync(destination.Id));
        fixture.Pump();
        fixture.Select(0, 6);

        Assert.Equal("default", Assert.IsType<ComboBoxItem>(fixture.Picker(SizePicker).SelectedItem).Tag?.ToString());
        Assert.Empty(fixture.Block.Styles);
        Assert.False(fixture.Editor.HasUnsavedChanges);
        Assert.False(fixture.Prose.CanUndo);
    }

    [AvaloniaFact]
    public async Task LoadedSize_IsAppliedToTheSelectedVisualRun()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan(1, 3)]);

        var run = fixture.Prose.TextArea.TextView.GetVisualLine(1)!.Elements
            .Single(element => element.RelativeTextOffset <= 2 && element.RelativeTextOffset + element.DocumentLength > 2);

        Assert.Equal(24, run.TextRunProperties.FontRenderingEmSize);
    }

    [AvaloniaFact]
    public async Task LoadedForeground_UsesTheSemanticBrush()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan(1, 3)]);
        fixture.Window.Resources["TramaProseForegroundTealBrush"] = new SolidColorBrush(Colors.Red);
        fixture.Prose.TextArea.TextView.Redraw();
        fixture.Pump();

        var run = fixture.Prose.TextArea.TextView.GetVisualLine(1)!.Elements
            .Single(element => element.RelativeTextOffset <= 2 && element.RelativeTextOffset + element.DocumentLength > 2);

        Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(run.TextRunProperties.ForegroundBrush).Color);
    }

    [AvaloniaFact]
    public async Task LoadedHighlight_UsesTheSemanticBrush()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan(1, 3)]);
        fixture.Window.Resources["TramaProseHighlightAmberBrush"] = new SolidColorBrush(Colors.Gold);
        fixture.Prose.TextArea.TextView.Redraw();
        fixture.Pump();

        var run = fixture.Prose.TextArea.TextView.GetVisualLine(1)!.Elements
            .Single(element => element.RelativeTextOffset <= 2 && element.RelativeTextOffset + element.DocumentLength > 2);

        Assert.Equal(Colors.Gold, Assert.IsAssignableFrom<ISolidColorBrush>(run.TextRunProperties.BackgroundBrush).Color);
    }

    [AvaloniaFact]
    public async Task DeletingEnrichedText_UndoAlreadyRestoresAllMetadata()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Prose.Document.Remove(1, 3);

        Assert.True(fixture.Prose.Undo());

        Assert.Equal("abcdef", fixture.Block.Text);
        Assert.Equal(RichSpan(), Assert.Single(fixture.Block.Styles));
    }

    [AvaloniaFact]
    public async Task ExistingBoldAction_AlreadyPreservesEnrichedAttributesThroughHistory()
    {
        using var fixture = await Fixture.CreateAsync([RichSpan()]);
        fixture.Select(0, 6);

        Assert.True(fixture.Prose.ApplyStyle(InlineTextStyle.Bold));
        Assert.Equal(RichSpan() with { Style = InlineTextStyle.Italic }, Assert.Single(fixture.Block.Styles));
        Assert.True(fixture.Prose.Undo());
        Assert.Equal(RichSpan(), Assert.Single(fixture.Block.Styles));
    }

    private static TextStyleSpan RichSpan(int offset = 0, int length = 6) => new(offset, length,
        InlineTextStyle.Bold | InlineTextStyle.Italic)
    { FontSize = 24, Foreground = "teal", Highlight = "amber" };

    private sealed class Fixture : IDisposable
    {
        public required NotebookEditorViewModel Editor { get; init; }
        public required EditorActions Actions { get; init; }
        public required NotebookEditorView View { get; init; }
        public required Window Window { get; init; }
        public NotebookBlockEditor Block => Editor.Blocks[0];
        public NotebookProseEditor Prose => View.GetVisualDescendants().OfType<NotebookProseEditor>()
            .Single(prose => ((NotebookBlockEditor)prose.DataContext!).Id == Block.Id);
        public Button ClearControl => Assert.IsType<Button>(View.FindControl<Button>(ClearButton));
        public ComboBox Picker(string name) => Assert.IsType<ComboBox>(View.FindControl<ComboBox>(name));

        public static async Task<Fixture> CreateAsync(IReadOnlyList<TextStyleSpan>? styles = null,
            bool secondText = false, bool includeCode = false, bool autoSave = false)
        {
            var blocks = new List<NotebookBlock> { new NotebookTextBlock(Guid.NewGuid(), "abcdef") { Styles = styles ?? [] } };
            if (secondText) blocks.Add(new NotebookTextBlock(Guid.NewGuid(), "segundo"));
            if (includeCode) blocks.Add(new NotebookCodeBlock(Guid.NewGuid(), "print(42)", "python"));
            var actions = new EditorActions { Loaded = new(Guid.NewGuid(), "Aula", blocks) };
            var editor = autoSave ? new NotebookEditorViewModel(actions, TimeProvider.System) : new NotebookEditorViewModel(actions);
            Assert.True(await editor.TryLoadAsync(actions.Loaded.Id));
            var view = new NotebookEditorView(editor);
            var window = new Window { Width = 1100, Height = 850, Content = view };
            window.Show();
            var fixture = new Fixture { Editor = editor, Actions = actions, View = view, Window = window };
            fixture.Pump();
            return fixture;
        }

        public void Select(int offset, int length)
        {
            Prose.TextArea.Focus();
            Prose.Select(offset, length);
            Pump();
        }

        public void Choose(string name, string value, NotebookProseEditor? target = null)
        {
            target ??= Prose;
            var offset = target.SelectionStart;
            var length = target.SelectionLength;
            var picker = Picker(name);
            var choice = Assert.Single(picker.Items.OfType<ComboBoxItem>(), item => item.Tag?.ToString() == value);
            picker.Focus();
            picker.SelectedItem = choice;
            Pump();
            Assert.Equal(offset, target.SelectionStart);
            Assert.Equal(length, target.SelectionLength);
            // A no-op (including an already selected value) need not move focus.
            if (Editor.HasUnsavedChanges) Assert.True(target.TextArea.IsKeyboardFocusWithin);
        }

        public void Clear()
        {
            ClearControl.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
        }

        public void Key(PhysicalKey key, RawInputModifiers modifiers)
        {
            Prose.TextArea.Focus();
            Window.KeyPressQwerty(key, modifiers);
            Window.KeyReleaseQwerty(key, modifiers);
            Pump();
        }

        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Dispose() { Editor.Dispose(); Window.Close(); }
    }

    private sealed class EditorActions : INotebookActions
    {
        public required NotebookSnapshot Loaded { get; set; }
        public List<NotebookSnapshot> Saved { get; } = [];
        public TaskCompletionSource<NotebookSnapshot> FirstSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            Saved.Add(notebook);
            FirstSave.TrySetResult(notebook);
            return Task.FromResult(new NotebookSaveResult(NotebookSaveStatus.Saved));
        }
        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<NotebookSnapshot?>(Loaded);
        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }
}
