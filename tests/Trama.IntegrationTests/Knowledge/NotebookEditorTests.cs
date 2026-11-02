using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NotebookEditorTests
{
    [AvaloniaFact]
    public void View_BindsWritingActionsAndScrollsThePageWhenTheTextGrows()
    {
        var editor = new NotebookEditorViewModel(new RecordingActions());
        var view = new NotebookEditorView(editor);
        var window = new Window { Width = 900, Height = 680, Content = view };
        try
        {
            window.Show();
            var title = Assert.IsType<TextBox>(view.FindControl<TextBox>("NotebookTitleInput"));
            var addText = Assert.IsType<Button>(view.FindControl<Button>("AddTextButton"));
            var addCode = Assert.IsType<Button>(view.FindControl<Button>("AddCodeButton"));
            var save = Assert.IsType<Button>(view.FindControl<Button>("SaveNotebookButton"));
            var pageScroll = Assert.IsType<ScrollViewer>(view.FindControl<ScrollViewer>("PageScroll"));
            title.Text = "Título escrito na página";
            Assert.Equal(title.Text, editor.Title);
            Assert.Same(editor.AddTextCommand, addText.Command);
            Assert.Same(editor.AddCodeCommand, addCode.Command);
            Assert.Same(editor.SaveCommand, save.Command);
            Assert.Equal(ScrollBarVisibility.Auto, pageScroll.VerticalScrollBarVisibility);

            editor.Blocks[0].Text = string.Concat(Enumerable.Repeat("Linha de um estudo extenso\n", 1000));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.True(pageScroll.Extent.Height > pageScroll.Viewport.Height,
                "Long writing must grow the page instead of remaining inside a fixed-height text field.");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task Composition_SavesAndReloadsTextCodeAndAnImportedImageThroughTheEditor()
    {
        using var workspace = new NotebookTestWorkspace();
        using var services = await DesktopComposition.CreateAsync(workspace.DirectoryPath);
        var editor = services.GetRequiredService<NotebookEditorViewModel>();
        editor.Title = "  Página real  ";
        editor.Blocks[0].Text = "Explicação 🧵\r\n";
        editor.AddCodeCommand.Execute(null);
        var code = editor.Blocks.Last();
        code.Text = "    print(42)\n";
        code.Language = "python";
        using var image = new MemoryStream(NotebookTestWorkspace.Png);
        await editor.ImportImageAsync(image);
        var attachmentId = editor.Blocks.Last().AttachmentId;

        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(NotebookSaveStatus.Saved, editor.LastSaveStatus);
        var reloaded = services.GetRequiredService<NotebookEditorViewModel>();
        await reloaded.LoadAsync(editor.PageId);

        Assert.NotSame(editor, reloaded);
        Assert.Equal(editor.PageId, reloaded.PageId);
        Assert.Equal("  Página real  ", reloaded.Title);
        Assert.Equal(3, reloaded.Blocks.Count);
        Assert.Equal("Explicação 🧵\r\n", reloaded.Blocks[0].Text);
        Assert.Equal("    print(42)\n", reloaded.Blocks[1].Text);
        Assert.Equal("python", reloaded.Blocks[1].Language);
        Assert.NotNull(attachmentId);
        Assert.Equal(attachmentId, reloaded.Blocks[2].AttachmentId);
        await using var reopenedImage = await services.GetRequiredService<INotebookActions>().OpenImageAsync(attachmentId.Value);
        Assert.NotNull(reopenedImage);
        using var copiedImage = new MemoryStream();
        await reopenedImage.CopyToAsync(copiedImage);
        Assert.Equal(NotebookTestWorkspace.Png, copiedImage.ToArray());
    }

    [Fact]
    public void NewPage_StartsWithAnEmptyTextBlockReadyForWriting()
    {
        var editor = new NotebookEditorViewModel(new RecordingActions());

        Assert.NotEqual(Guid.Empty, editor.PageId);
        var block = Assert.Single(editor.Blocks);
        Assert.Equal(NotebookBlockKind.Text, block.Kind);
        Assert.Equal(string.Empty, block.Text);
        Assert.NotEqual(Guid.Empty, block.Id);
    }

    [Fact]
    public void AddMoveAndRemove_PreserveBlockIdentityAndContent()
    {
        var editor = new NotebookEditorViewModel(new RecordingActions());
        editor.AddCodeCommand.Execute(null);
        var code = editor.Blocks.Last();
        Assert.Equal(NotebookBlockKind.Code, code.Kind);
        code.Text = "def dobro(x):\n\treturn x * 2\n";
        code.Language = "python";
        editor.AddTextCommand.Execute(null);
        var conclusion = editor.Blocks.Last();
        conclusion.Text = "Conclusão 🧵";

        editor.MoveBlockUpCommand.Execute(conclusion.Id);
        Assert.Same(conclusion, editor.Blocks[1]);
        editor.MoveBlockDownCommand.Execute(conclusion.Id);
        Assert.Same(conclusion, editor.Blocks[2]);
        editor.RemoveBlockCommand.Execute(editor.Blocks[0].Id);

        Assert.Equal(new[] { code, conclusion }, editor.Blocks);
        Assert.Equal("def dobro(x):\n\treturn x * 2\n", code.Text);
        Assert.Equal("python", code.Language);
    }

    [Fact]
    public async Task Save_KeepsPageOpenAndUsesTheSameIdentityForFurtherEdits()
    {
        var actions = new RecordingActions();
        var editor = new NotebookEditorViewModel(actions) { Title = "  Meu caderno  " };
        var text = Assert.Single(editor.Blocks);
        text.Text = string.Concat(Enumerable.Repeat("  Estudo 🧵\r\n", 3000));
        editor.AddCodeCommand.Execute(null);
        var code = editor.Blocks.Last();
        code.Text = "    await SaveAsync();\t\n";
        code.Language = "csharp";

        await editor.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(actions.Saved);
        Assert.Equal(editor.PageId, saved.Id);
        Assert.Equal(editor.Title, saved.Title);
        Assert.Equal(text.Text, Assert.IsType<NotebookTextBlock>(saved.Blocks[0]).Text);
        Assert.Equal(new NotebookCodeBlock(code.Id, code.Text, "csharp"), saved.Blocks[1]);
        Assert.Equal(NotebookSaveStatus.Saved, editor.LastSaveStatus);
        Assert.Equal(2, editor.Blocks.Count);
        Assert.Equal("  Meu caderno  ", editor.Title);
        text.Text = "Revisado";
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(saved.Id, actions.Saved[1].Id);
        Assert.NotEqual(text.Text, Assert.IsType<NotebookTextBlock>(saved.Blocks[0]).Text);
    }

    [Theory]
    [InlineData(NotebookSaveStatus.ValidationFailed)]
    [InlineData(NotebookSaveStatus.PersistenceFailed)]
    public async Task Save_FailurePreservesTheEntireDraft(NotebookSaveStatus status)
    {
        var editor = new NotebookEditorViewModel(new RecordingActions { SaveResult = new(status) }) { Title = "  Manter  " };
        var text = Assert.Single(editor.Blocks);
        text.Text = "Não perder\n";

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal(status, editor.LastSaveStatus);
        Assert.Equal("  Manter  ", editor.Title);
        Assert.Same(text, Assert.Single(editor.Blocks));
        Assert.Equal("Não perder\n", text.Text);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task Save_WhilePendingBlocksAnotherSaveAndStructuralEdits()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new RecordingActions { PendingSave = pending.Task };
        var editor = new NotebookEditorViewModel(actions) { Title = "Em andamento" };
        var save = editor.SaveCommand.ExecuteAsync(null);
        try
        {
            Assert.True(editor.IsBusy);
            Assert.False(editor.SaveCommand.CanExecute(null));
            Assert.False(editor.AddTextCommand.CanExecute(null));
            Assert.False(editor.AddCodeCommand.CanExecute(null));
            Assert.False(editor.RemoveBlockCommand.CanExecute(editor.Blocks[0].Id));
            Assert.False(editor.MoveBlockUpCommand.CanExecute(editor.Blocks[0].Id));
            Assert.False(editor.MoveBlockDownCommand.CanExecute(editor.Blocks[0].Id));
            Assert.Null(editor.LastSaveStatus);
            Assert.Single(actions.Saved);
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            await save;
        }

        Assert.False(editor.IsBusy);
        Assert.True(editor.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Save_UnexpectedErrorDoesNotExposeDetailsOrDiscardTheDraft()
    {
        var editor = new NotebookEditorViewModel(new RecordingActions { SaveFailure = new IOException("PRIVATE_TEST_DETAIL") })
        {
            Title = "Manter"
        };
        var original = Assert.Single(editor.Blocks);
        original.Text = "Não perder";

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Same(original, Assert.Single(editor.Blocks));
        Assert.Equal("Manter", editor.Title);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", editor.Feedback);
        Assert.NotEqual(NotebookSaveStatus.Saved, editor.LastSaveStatus);
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task ImportImage_AddsManagedReferenceAndCaptionWithoutDisposingTheSource()
    {
        var actions = new RecordingActions();
        var editor = new NotebookEditorViewModel(actions);
        using var source = new MemoryStream([1, 2, 3]);

        await editor.ImportImageAsync(source);

        var image = editor.Blocks.Last();
        Assert.Equal(NotebookBlockKind.Image, image.Kind);
        Assert.Equal(actions.Attachment.Id, image.AttachmentId);
        Assert.Same(source, actions.ImportedSource);
        Assert.True(source.CanRead);
        image.Text = "  Resultado do estudo  ";
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(new NotebookImageBlock(image.Id, actions.Attachment.Id, image.Text), actions.Saved.Single().Blocks.Last());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImportImage_InvalidImageOrCancellationKeepsTheDraft(bool canceled)
    {
        var actions = new RecordingActions
        {
            ImportFailure = canceled ? new OperationCanceledException() : new InvalidDataException("PRIVATE_TEST_DETAIL")
        };
        var editor = new NotebookEditorViewModel(actions) { Title = "Rascunho" };
        var original = Assert.Single(editor.Blocks);
        original.Text = "Texto anterior";
        using var source = new MemoryStream([1]);

        await editor.ImportImageAsync(source);

        Assert.Same(original, Assert.Single(editor.Blocks));
        Assert.Equal("Texto anterior", original.Text);
        Assert.Equal("Rascunho", editor.Title);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", editor.Feedback);
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task ImportImage_WhilePendingBlocksSaveAndStructuralEdits()
    {
        var pending = new TaskCompletionSource<ImageAttachment>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new RecordingActions { PendingImport = pending.Task };
        var editor = new NotebookEditorViewModel(actions);
        using var source = new MemoryStream([1]);
        var import = editor.ImportImageAsync(source);
        try
        {
            Assert.True(editor.IsBusy);
            Assert.False(editor.SaveCommand.CanExecute(null));
            Assert.False(editor.AddTextCommand.CanExecute(null));
            Assert.False(editor.AddCodeCommand.CanExecute(null));
        }
        finally
        {
            pending.TrySetResult(actions.Attachment);
            await import;
        }

        Assert.False(editor.IsBusy);
        Assert.True(editor.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Load_ReplacesThePageWithItsStoredTitleBlocksAndIdentity()
    {
        NotebookBlock[] blocks =
        [
            new NotebookTextBlock(Guid.NewGuid(), "Texto 🧵\n"),
            new NotebookCodeBlock(Guid.NewGuid(), "    print(1)\r\n", "python"),
            new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Print")
        ];
        var page = new NotebookSnapshot(Guid.NewGuid(), "  Página salva  ", blocks);
        var editor = new NotebookEditorViewModel(new RecordingActions { Loaded = page });

        await editor.LoadAsync(page.Id);

        Assert.Equal(page.Id, editor.PageId);
        Assert.Equal(page.Title, editor.Title);
        Assert.Equal(blocks.Select(block => block.Id), editor.Blocks.Select(block => block.Id));
        Assert.Equal(new[] { NotebookBlockKind.Text, NotebookBlockKind.Code, NotebookBlockKind.Image }, editor.Blocks.Select(block => block.Kind));
        Assert.Equal("    print(1)\r\n", editor.Blocks[1].Text);
        Assert.Equal("python", editor.Blocks[1].Language);
        Assert.Equal("Print", editor.Blocks[2].Text);
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task Load_MissingPageKeepsTheCurrentDraft()
    {
        var editor = new NotebookEditorViewModel(new RecordingActions()) { Title = "Não perder" };
        var id = editor.PageId;
        var text = Assert.Single(editor.Blocks);
        text.Text = "Ainda não salvo";

        await editor.LoadAsync(Guid.NewGuid());

        Assert.Equal(id, editor.PageId);
        Assert.Equal("Não perder", editor.Title);
        Assert.Same(text, Assert.Single(editor.Blocks));
        Assert.Equal("Ainda não salvo", text.Text);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task Load_FailurePreservesTheCurrentPageAndDoesNotExposeErrorDetails()
    {
        var editor = new NotebookEditorViewModel(new RecordingActions { LoadFailure = new NotePersistenceException("PRIVATE_TEST_DETAIL") })
        {
            Title = "Manter título"
        };
        var id = editor.PageId;
        var original = Assert.Single(editor.Blocks);
        original.Text = "Manter conteúdo";

        await editor.LoadAsync(Guid.NewGuid());

        Assert.Equal(id, editor.PageId);
        Assert.Equal("Manter título", editor.Title);
        Assert.Same(original, Assert.Single(editor.Blocks));
        Assert.Equal("Manter conteúdo", original.Text);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", editor.Feedback);
        Assert.False(editor.IsBusy);
    }

    private sealed class RecordingActions : INotebookActions
    {
        public List<NotebookSnapshot> Saved { get; } = [];
        public NotebookSaveResult SaveResult { get; init; } = new(NotebookSaveStatus.Saved);
        public Task<NotebookSaveResult>? PendingSave { get; init; }
        public Task<ImageAttachment>? PendingImport { get; init; }
        public Exception? SaveFailure { get; init; }
        public Exception? ImportFailure { get; init; }
        public Exception? LoadFailure { get; init; }
        public NotebookSnapshot? Loaded { get; init; }
        public ImageAttachment Attachment { get; } = new(Guid.NewGuid(), "image/png");
        public Stream? ImportedSource { get; private set; }

        public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            Saved.Add(notebook);
            return SaveFailure is null ? PendingSave ?? Task.FromResult(SaveResult) : Task.FromException<NotebookSaveResult>(SaveFailure);
        }

        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) =>
            LoadFailure is null ? Task.FromResult(Loaded) : Task.FromException<NotebookSnapshot?>(LoadFailure);

        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default)
        {
            ImportedSource = source;
            return ImportFailure is null ? PendingImport ?? Task.FromResult(Attachment) : Task.FromException<ImageAttachment>(ImportFailure);
        }

        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
