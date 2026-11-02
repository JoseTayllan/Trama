using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class WritingWorkflowTests
{
    [Fact]
    public async Task Paste_AppendsImageOwnsStreamAndKeepsWritingAndIdentity()
    {
        var actions = new WritingActions();
        var editor = new NotebookEditorViewModel(actions) { Title = "Minha aula" };
        var id = editor.PageId;
        editor.Blocks[0].Text = "Conteúdo já escrito";
        var stream = new TrackedStream(NotebookTestWorkspace.Png);

        Assert.Equal(ImagePasteStatus.Imported, await editor.PasteImageAsync(new ClipboardSource(() => Task.FromResult<Stream?>(stream))));

        Assert.Equal(id, editor.PageId);
        Assert.Equal("Minha aula", editor.Title);
        Assert.Equal("Conteúdo já escrito", editor.Blocks[0].Text);
        Assert.Equal(actions.ImportedIds.Single(), editor.Blocks[1].AttachmentId);
        Assert.Equal(NotebookBlockKind.Image, editor.Blocks[1].Kind);
        Assert.True(stream.Disposed);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Empty(actions.Saved);
    }

    [Fact]
    public async Task Paste_WithoutBitmapReturnsNoImageAndPreservesNativeTextPaste()
    {
        var actions = new WritingActions();
        var editor = new NotebookEditorViewModel(actions);
        var original = Assert.Single(editor.Blocks);

        Assert.Equal(ImagePasteStatus.NoImage, await editor.PasteImageAsync(new ClipboardSource(() => Task.FromResult<Stream?>(null))));

        Assert.Same(original, Assert.Single(editor.Blocks));
        Assert.False(editor.HasUnsavedChanges);
        Assert.Equal(0, actions.ImportCalls);
    }

    [Fact]
    public async Task Paste_SourceFailureKeepsDraftAndHidesInternalDetails()
    {
        var editor = new NotebookEditorViewModel(new WritingActions()) { Title = "Não perder" };
        editor.Blocks[0].Text = "Rascunho";

        Assert.Equal(ImagePasteStatus.Failed, await editor.PasteImageAsync(new ClipboardSource(() => throw new IOException("PRIVATE_TEST_DETAIL"))));

        Assert.Equal("Não perder", editor.Title);
        Assert.Equal("Rascunho", Assert.Single(editor.Blocks).Text);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", editor.Feedback);
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task Paste_InvalidImageIsDisposedWithoutAddingABlock()
    {
        var editor = new NotebookEditorViewModel(new WritingActions());
        var stream = new TrackedStream([0]);

        Assert.Equal(ImagePasteStatus.Failed, await editor.PasteImageAsync(new ClipboardSource(() => Task.FromResult<Stream?>(stream))));

        Assert.Single(editor.Blocks);
        Assert.True(stream.Disposed);
        Assert.False(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task Saving_BlocksPasteAndDropBeforeOpeningAnySource()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>();
        var actions = new WritingActions { PendingSave = pending.Task };
        var editor = new NotebookEditorViewModel(actions) { Title = "Salvando" };
        var save = editor.SaveCommand.ExecuteAsync(null);
        var clipboard = new ClipboardSource(() => throw new InvalidOperationException("Must not read while busy"));
        var opened = 0;
        try
        {
            Assert.Equal(ImagePasteStatus.Busy, await editor.PasteImageAsync(clipboard));
            Assert.False(await editor.ImportDroppedImagesAsync([new EditorImageInput("image.png", _ =>
            {
                opened++;
                return Task.FromResult<Stream>(new MemoryStream(NotebookTestWorkspace.Png));
            })]));
            Assert.Equal(0, clipboard.Calls);
            Assert.Equal(0, opened);
            Assert.Equal(0, actions.ImportCalls);
        }
        finally
        {
            pending.SetResult(new NotebookSaveResult(NotebookSaveStatus.Saved));
            await save;
        }
    }

    [Fact]
    public async Task PendingPaste_BlocksAnotherRequestAndRestoresEditingAfterCompletion()
    {
        var pending = new TaskCompletionSource<Stream?>();
        var editor = new NotebookEditorViewModel(new WritingActions());
        var clipboard = new ClipboardSource(() => pending.Task);
        var paste = editor.PasteImageAsync(clipboard);
        try
        {
            Assert.False(paste.IsCompleted);
            Assert.True(editor.IsBusy);
            Assert.False(editor.SaveCommand.CanExecute(null));
            Assert.Equal(ImagePasteStatus.Busy, await editor.PasteImageAsync(clipboard));
            Assert.Equal(1, clipboard.Calls);
            pending.SetResult(new TrackedStream(NotebookTestWorkspace.Png));
            Assert.Equal(ImagePasteStatus.Imported, await paste);
            Assert.False(editor.IsBusy);
        }
        finally
        {
            pending.TrySetResult(null);
            await paste;
        }
    }

    [Fact]
    public async Task Drop_KeepsValidImagesInOrderAndReportsRejectedItemsWithoutLosingText()
    {
        var actions = new WritingActions();
        var editor = new NotebookEditorViewModel(actions);
        editor.Blocks[0].Text = "Explicação";
        var first = new TrackedStream(NotebookTestWorkspace.Png);
        var invalid = new TrackedStream([0]);
        var second = new TrackedStream(NotebookTestWorkspace.Jpeg);

        Assert.True(await editor.ImportDroppedImagesAsync([
            new EditorImageInput("one.png", _ => Task.FromResult<Stream>(first)),
            new EditorImageInput("invalid.bin", _ => Task.FromResult<Stream>(invalid)),
            new EditorImageInput("unreadable.png", _ => throw new IOException("PRIVATE_TEST_DETAIL")),
            new EditorImageInput("two.jpeg", _ => Task.FromResult<Stream>(second))
        ]));

        Assert.Equal("Explicação", editor.Blocks[0].Text);
        Assert.Equal(actions.ImportedIds, editor.Blocks.Skip(1).Select(block => block.AttachmentId.GetValueOrDefault()));
        Assert.Equal(3, editor.Blocks.Count);
        Assert.True(first.Disposed && invalid.Disposed && second.Disposed);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", editor.Feedback);
        Assert.Contains("2", editor.Feedback);
        Assert.Empty(actions.Saved);
    }

    [Fact]
    public async Task Format_TogglesSelectedStyleMarksDirtyAndSavesWithoutChangingText()
    {
        var actions = new WritingActions();
        var editor = new NotebookEditorViewModel(actions);
        var block = Assert.Single(editor.Blocks);
        block.Text = "Texto com estilo";
        await editor.SaveCommand.ExecuteAsync(null);

        Assert.True(editor.FormatTextSelection(block.Id, 0, 5, InlineTextStyle.Bold));
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal(new TextStyleSpan(0, 5, InlineTextStyle.Bold), Assert.Single(block.Styles));
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.False(editor.HasUnsavedChanges);
        Assert.Equal("Texto com estilo", block.Text);
        Assert.Equal(block.Styles, Assert.IsType<NotebookTextBlock>(Assert.Single(actions.Saved.Last().Blocks)).Styles);
        Assert.True(editor.FormatTextSelection(block.Id, 0, 5, InlineTextStyle.Bold));
        Assert.Empty(block.Styles);
    }

    [Fact]
    public void Format_DoesNotAlterCodeOrFormatAnEmptySelection()
    {
        var editor = new NotebookEditorViewModel(new WritingActions());
        editor.AddCodeCommand.Execute(null);
        var code = editor.Blocks[1];
        code.Text = "    print(42)\t\n";

        Assert.False(editor.FormatTextSelection(code.Id, 0, 5, InlineTextStyle.Bold));
        Assert.False(editor.FormatTextSelection(editor.Blocks[0].Id, 0, 0, InlineTextStyle.Italic));
        Assert.Equal("    print(42)\t\n", code.Text);
        Assert.Empty(code.Styles);
    }

    [Fact]
    public void Edit_ShiftsInlineStylesAndPreservesTheSameBlockIdentity()
    {
        var editor = new NotebookEditorViewModel(new WritingActions());
        var block = editor.Blocks[0];
        block.Text = "abcd";
        Assert.True(editor.FormatTextSelection(block.Id, 2, 2, InlineTextStyle.Italic));

        Assert.True(editor.EditText(block.Id, 0, 0, "XX"));

        Assert.Same(block, Assert.Single(editor.Blocks));
        Assert.Equal("XXabcd", block.Text);
        Assert.Equal(new TextStyleSpan(4, 2, InlineTextStyle.Italic), Assert.Single(block.Styles));
    }

    private sealed class ClipboardSource(Func<Task<Stream?>> open) : IClipboardImageSource
    {
        public int Calls { get; private set; }
        public Task<Stream?> OpenImageAsync(CancellationToken cancellationToken = default) { Calls++; return open(); }
    }

    private sealed class TrackedStream(byte[] bytes) : MemoryStream(bytes)
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class WritingActions : INotebookActions
    {
        public List<NotebookSnapshot> Saved { get; } = [];
        public List<Guid> ImportedIds { get; } = [];
        public int ImportCalls { get; private set; }
        public Task<NotebookSaveResult>? PendingSave { get; init; }
        public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            Saved.Add(notebook);
            return PendingSave ?? Task.FromResult(new NotebookSaveResult(NotebookSaveStatus.Saved));
        }
        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<NotebookSnapshot?>(null);
        public async Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default)
        {
            ImportCalls++;
            var header = new byte[1];
            if (await source.ReadAsync(header, cancellationToken) == 0 || header[0] == 0) throw new InvalidDataException("Synthetic invalid image");
            var id = Guid.NewGuid(); ImportedIds.Add(id);
            return new ImageAttachment(id, "image/png");
        }
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }
}
