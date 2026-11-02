using System.Text.Json;
using System.Text.RegularExpressions;
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

public sealed class MarkdownExportWorkflowTests
{
    [AvaloniaFact]
    public async Task CancelDialog_PreservesTheDraftHistoryAndCreatesNoOutput()
    {
        using var f = await Fixture.CreateAsync();
        f.Prose.Document.Insert(0, "Rascunho "); f.Pump();
        var before = f.Draft();
        Assert.True(f.Prose.CanUndo);
        await f.OpenAsync();
        f.Click(f.Control<Button>("CancelExportButton"));
        await f.UntilAsync(() => !f.Window.OwnedWindows.Any());
        Assert.Equal(before, f.Draft());
        Assert.True(f.Editor.HasUnsavedChanges);
        Assert.True(f.Prose.CanUndo);
        Assert.False(Directory.Exists(f.Destination));
    }

    [AvaloniaFact]
    public async Task Export_UsesTheOpenDraftWithoutSavingOrChangingTheStoredPage()
    {
        using var f = await Fixture.CreateAsync();
        var stored = JsonSerializer.Serialize(await f.Actions.LoadAsync(f.Page.Id));
        f.Editor.Title = "Título em edição";
        f.Prose.Document.Insert(0, "Ainda não salvo: "); f.Pump();
        var draft = f.Draft();
        var markdown = await f.ExportAsync();
        Assert.Contains("# Título em edição", markdown);
        Assert.Contains("Ainda não salvo: Texto salvo", markdown);
        Assert.Equal(draft, f.Draft());
        Assert.True(f.Editor.HasUnsavedChanges);
        Assert.True(f.Prose.CanUndo);
        Assert.Equal(stored, JsonSerializer.Serialize(await f.Actions.LoadAsync(f.Page.Id)));
    }

    [AvaloniaFact]
    public async Task Export_NewUnsavedPageDoesNotRequireAStoredIdentity()
    {
        using var f = await Fixture.CreateAsync(newPage: true);
        f.Prose.Document.Insert(0, "Primeira ideia"); f.Pump();
        var markdown = await f.ExportAsync();
        Assert.Contains("# Sem título", markdown);
        Assert.Contains("Primeira ideia", markdown);
        Assert.Null(await f.Actions.LoadAsync(f.Editor.PageId));
        Assert.True(f.Editor.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public async Task Export_PreservesTextCodeImageAndBlockOrder()
    {
        using var f = await Fixture.CreateAsync(body: "Primeiro texto", code: "Console.WriteLine(42);", images: true, trailingText: true);
        var markdown = await f.ExportAsync();
        var first = markdown.IndexOf("Primeiro texto", StringComparison.Ordinal);
        var code = markdown.IndexOf("Console.WriteLine(42);", StringComparison.Ordinal);
        var image = markdown.IndexOf("![", StringComparison.Ordinal);
        var last = markdown.IndexOf("Último texto", StringComparison.Ordinal);
        Assert.True(first >= 0 && code > first && image > code && last > image);
        Assert.Contains("```csharp\nConsole.WriteLine(42);\n```", markdown);
    }

    [AvaloniaFact]
    public async Task Export_MapsParagraphListsAndRestartsNumberingAfterPlainParagraphs()
    {
        const string text = "Um\nDois\nIntervalo\nTrês\nQuatro";
        using var f = await Fixture.CreateAsync(body: text, lists:
            [new(0, "numbered"), new(3, "numbered"), new(18, "numbered"), new(23, "bullet")]);
        var markdown = await f.ExportAsync();
        Assert.Contains("1. Um\n2. Dois", markdown);
        Assert.Contains("Intervalo", markdown);
        Assert.Contains("1. Três", markdown);
        Assert.Contains("- Quatro", markdown);
        Assert.Equal(text, f.Editor.Blocks[0].Text);
    }

    [AvaloniaFact]
    public async Task Export_PreservesBoldItalicAndTheirCombination()
    {
        using var f = await Fixture.CreateAsync(body: "Negrito Itálico Ambos", styles:
            [new(0, 7, InlineTextStyle.Bold), new(8, 7, InlineTextStyle.Italic), new(16, 5, InlineTextStyle.Bold | InlineTextStyle.Italic)]);
        var markdown = await f.ExportAsync();
        Assert.Contains("**Negrito** *Itálico* ***Ambos***", markdown);
    }

    [AvaloniaFact]
    public async Task Export_DocumentsPortableTextWithoutInjectingTramaColorOrSizeMarkup()
    {
        using var f = await Fixture.CreateAsync(body: "Conteúdo colorido", styles:
            [new(0, 8, InlineTextStyle.None) { FontSize = 24, Foreground = "teal", Highlight = "amber" }]);
        var markdown = await f.ExportAsync();
        Assert.Contains("Conteúdo colorido", markdown);
        Assert.DoesNotContain("<span", markdown);
        Assert.DoesNotContain("font-size", markdown);
        Assert.DoesNotContain("teal", markdown);
        Assert.Equal(24, Assert.Single(f.Editor.Blocks[0].Styles).FontSize);
    }

    [AvaloniaTheory]
    [InlineData("[abrir](javascript:alert(1))", @"\[abrir\]\(javascript:alert\(1\)\)")]
    [InlineData("<script>alert(1)</script>", @"\<script\>alert\(1\)\</script\>")]
    [InlineData("**literal** e _literal_", @"\*\*literal\*\* e \_literal\_")]
    public async Task Export_EscapesLiteralMarkdownAndHtml(string body, string escaped)
    {
        using var f = await Fixture.CreateAsync(body: body);
        Assert.Contains(escaped, await f.ExportAsync());
        Assert.Equal(body, f.Editor.Blocks[0].Text);
    }

    [AvaloniaFact]
    public async Task Export_UsesALongerFenceWhenCodeContainsBackticks()
    {
        const string code = "var texto = \"```\";\nConsole.WriteLine(texto);";
        using var f = await Fixture.CreateAsync(code: code);
        Assert.Contains("````csharp\n" + code + "\n````", await f.ExportAsync());
    }

    [AvaloniaFact]
    public async Task Export_DoesNotTreatAnUnsafeLanguageLabelAsMarkdown()
    {
        using var f = await Fixture.CreateAsync(code: "exemplo", language: "csharp\n<script>");
        var markdown = await f.ExportAsync();
        Assert.Contains("```text\nexemplo\n```", markdown);
        Assert.DoesNotContain("<script>", markdown);
    }

    [AvaloniaTheory]
    [InlineData(false, ".png")]
    [InlineData(true, ".jpg")]
    public async Task Export_CopiesImagesWithRelativeLinksAndCorrectBytes(bool jpeg, string extension)
    {
        using var f = await Fixture.CreateAsync(images: true, jpeg: jpeg);
        var markdown = await f.ExportAsync();
        var relative = Assert.Single(ImagePaths(markdown));
        Assert.StartsWith("imagens/", relative);
        Assert.EndsWith(extension, relative);
        Assert.False(Path.IsPathRooted(relative));
        Assert.DoesNotContain("..", relative);
        Assert.Equal(jpeg ? NotebookTestWorkspace.Jpeg : NotebookTestWorkspace.Png,
            await File.ReadAllBytesAsync(Path.Combine(f.Destination, relative)));
        Assert.Contains(@"![Captura \[local\]]", markdown);
    }

    [AvaloniaFact]
    public async Task Export_ReusesOneFileWhenTwoBlocksReferenceTheSameImage()
    {
        using var f = await Fixture.CreateAsync(images: true, repeatImage: true);
        var markdown = await f.ExportAsync();
        var references = ImagePaths(markdown);
        Assert.Equal(2, references.Length);
        Assert.Equal(references[0], references[1]);
        Assert.Single(Directory.GetFiles(Path.Combine(f.Destination, "imagens")));
    }

    [AvaloniaFact]
    public async Task Export_WithoutImagesCreatesOnlyTheMarkdownFileAndNoLocalPathLeaks()
    {
        using var f = await Fixture.CreateAsync();
        var markdown = await f.ExportAsync();
        Assert.Equal("pagina.md", Path.GetFileName(Assert.Single(Directory.GetFiles(f.Destination, "*", SearchOption.AllDirectories))));
        Assert.False(Directory.Exists(Path.Combine(f.Destination, "imagens")));
        Assert.DoesNotContain(f.Root.DirectoryPath, markdown);
        Assert.DoesNotContain("trama.db", markdown);
    }

    [AvaloniaFact]
    public async Task Export_MissingImageReportsFailureWithoutPublishingAPartialFolder()
    {
        using var f = await Fixture.CreateAsync(images: true);
        await f.OpenAsync(); f.Gate.Mode = ImageMode.Missing;
        await f.ExportFailureAsync();
        Assert.False(Directory.Exists(f.Destination));
        Assert.Empty(Directory.GetFileSystemEntries(f.OutputParent));
        Assert.NotNull(await f.Actions.LoadAsync(f.Page.Id));
    }

    [AvaloniaFact]
    public async Task Export_ImageReadFailureCleansTemporaryFilesAndDisposesTheStream()
    {
        using var f = await Fixture.CreateAsync(images: true);
        await f.OpenAsync(); f.Gate.Mode = ImageMode.Failure;
        await f.ExportFailureAsync();
        Assert.NotEmpty(f.Gate.ExportStreams);
        Assert.All(f.Gate.ExportStreams, stream => Assert.True(stream.Disposed));
        Assert.Empty(Directory.GetFileSystemEntries(f.OutputParent));
    }

    [AvaloniaFact]
    public async Task Export_CancellationDuringImageCopyRemovesPartialOutputAndKeepsTheDraft()
    {
        using var f = await Fixture.CreateAsync(images: true);
        await f.OpenAsync(); f.Gate.Mode = ImageMode.Blocked;
        var before = f.Draft();
        f.SetDestination(); f.Click(f.Control<Button>("ConfirmExportButton"));
        await f.UntilAsync(() => f.Gate.ExportStreams.Any(stream => stream.ReadStarted));
        f.Click(f.Control<Button>("CancelExportButton"));
        await f.UntilAsync(() => !f.Window.OwnedWindows.Any() && f.Gate.ExportStreams.All(stream => stream.Disposed));
        Assert.Contains(f.Gate.ExportStreams, stream => stream.CancellationObserved);
        Assert.Empty(Directory.GetFileSystemEntries(f.OutputParent));
        Assert.Equal(before, f.Draft());
    }

    [AvaloniaFact]
    public async Task Export_RefusesAnExistingDestinationAndPreservesAllExistingFiles()
    {
        using var f = await Fixture.CreateAsync();
        Directory.CreateDirectory(f.Destination);
        var sentinel = Path.Combine(f.Destination, "pagina.md");
        await File.WriteAllTextAsync(sentinel, "Arquivo do usuário");
        await f.OpenAsync(); await f.ExportFailureAsync();
        Assert.Equal("Arquivo do usuário", await File.ReadAllTextAsync(sentinel));
        Assert.Single(Directory.GetFiles(f.Destination));
        Assert.Single(Directory.GetFileSystemEntries(f.OutputParent));
    }

    [AvaloniaFact]
    public async Task Export_RejectsARelativeDestinationWithoutWritingToTheWorkingDirectory()
    {
        using var f = await Fixture.CreateAsync();
        await f.OpenAsync();
        var relative = "trama-relative-export-" + Guid.NewGuid().ToString("N");
        await f.ExportFailureAsync(relative);
        Assert.False(Directory.Exists(relative));
        Assert.Empty(Directory.GetFileSystemEntries(f.OutputParent));
    }

    [AvaloniaFact]
    public async Task Export_AHostileTitleCannotBecomeAnOutputPathOrAnExtraHeading()
    {
        using var f = await Fixture.CreateAsync();
        f.Editor.Title = "../../fora\n# Outro título";
        var markdown = await f.ExportAsync();
        Assert.Equal("pagina.md", Path.GetFileName(Assert.Single(Directory.GetFiles(f.Destination))));
        Assert.Single(markdown.Split('\n'), line => line.StartsWith("# ", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(f.Root.DirectoryPath, "fora")));
    }

    [AvaloniaFact]
    public async Task Export_IsAvailableInACompactEditorAndItsDialogFitsTheWindow()
    {
        using var f = await Fixture.CreateAsync();
        f.Window.Width = 860; f.Pump();
        await f.OpenAsync();
        Assert.True(f.ExportButton().IsEffectivelyVisible);
        Assert.InRange(f.Dialog.Bounds.Width, 1, f.Window.Bounds.Width);
        Assert.InRange(f.Dialog.Bounds.Height, 1, f.Window.Bounds.Height);
        Assert.True(f.Control<TextBox>("ExportDestinationInput").IsEffectivelyVisible);
        Assert.True(f.Control<Button>("ConfirmExportButton").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task Export_DisposesImageStreamsAfterSuccessfulPublication()
    {
        using var f = await Fixture.CreateAsync(images: true);
        await f.OpenAsync(); f.Gate.Mode = ImageMode.Track;
        await f.CompleteExportAsync();
        Assert.NotEmpty(f.Gate.ExportStreams);
        Assert.All(f.Gate.ExportStreams, stream => Assert.True(stream.Disposed));
    }

    [Fact]
    public void ExistingModel_AlreadyKeepsLiteralTextSeparateFromListAndStyleMetadata()
    {
        const string text = "Um\nDois";
        var block = new NotebookTextBlock(Guid.NewGuid(), text)
        { Lists = [new(0, "bullet"), new(3, "bullet")], Styles = [new(0, 2, InlineTextStyle.Bold)] };
        var draft = new NotebookDraft(); draft.Insert(0, block);
        var copy = JsonSerializer.Deserialize<NotebookTextBlock>(JsonSerializer.Serialize(block))!;
        Assert.Equal(text, copy.Text);
        Assert.Equal(block.Lists, copy.Lists);
        Assert.Equal(block.Styles, copy.Styles);
    }

    [AvaloniaFact]
    public async Task ExistingStorage_AlreadyPreservesThePageAndItsImageBytes()
    {
        using var f = await Fixture.CreateAsync(images: true);
        var page = await f.Actions.LoadAsync(f.Page.Id);
        Assert.NotNull(page);
        Assert.Equal("Texto salvo", Assert.IsType<NotebookTextBlock>(page.Blocks[0]).Text);
        await using var stream = await f.Actions.OpenImageAsync(f.ImageId);
        Assert.NotNull(stream);
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes);
        Assert.Equal(NotebookTestWorkspace.Png, bytes.ToArray());
    }

    private static string[] ImagePaths(string markdown) => Regex.Matches(markdown, @"!\[.*?\]\(([^)]+)\)")
        .Select(match => match.Groups[1].Value).ToArray();

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Source => Path.Combine(Root.DirectoryPath, "source");
        public string OutputParent => Path.Combine(Root.DirectoryPath, "saídas");
        public string Destination => Path.Combine(OutputParent, "Página exportada");
        public ServiceProvider Services { get; private set; } = null!;
        public ILibraryActions Actions { get; private set; } = null!;
        public NotebookSnapshot Page { get; private set; } = null!;
        public NotebookEditorViewModel Editor { get; private set; } = null!;
        public ExportImageActions Gate { get; private set; } = null!;
        public NotebookEditorView View { get; private set; } = null!;
        public Window Window { get; private set; } = null!;
        public Window Dialog => Assert.Single(Window.OwnedWindows, window => window.Name == "ExportMarkdownDialog");
        public Guid ImageId { get; private set; }
        public NotebookProseEditor Prose => View.GetVisualDescendants().OfType<NotebookProseEditor>().Single();

        public static async Task<Fixture> CreateAsync(string body = "Texto salvo", IReadOnlyList<TextStyleSpan>? styles = null,
            IReadOnlyList<ParagraphListItem>? lists = null, string? code = null, string language = "csharp",
            bool images = false, bool jpeg = false, bool repeatImage = false, bool trailingText = false, bool newPage = false)
        {
            var f = new Fixture(); Directory.CreateDirectory(f.OutputParent);
            f.Services = await DesktopComposition.CreateAsync(f.Source);
            f.Actions = f.Services.GetRequiredService<ILibraryActions>();
            var blocks = new List<NotebookBlock> { new NotebookTextBlock(Guid.NewGuid(), body)
                { Styles = styles ?? [], Lists = lists ?? [] } };
            if (code is not null) blocks.Add(new NotebookCodeBlock(Guid.NewGuid(), code, language));
            if (images)
            {
                using var input = new MemoryStream(jpeg ? NotebookTestWorkspace.Jpeg : NotebookTestWorkspace.Png);
                var image = await f.Actions.ImportImageAsync(input); f.ImageId = image.Id;
                blocks.Add(new NotebookImageBlock(Guid.NewGuid(), image.Id, "Captura [local]"));
                if (repeatImage) blocks.Add(new NotebookImageBlock(Guid.NewGuid(), image.Id, "Segunda referência"));
            }
            if (trailingText) blocks.Add(new NotebookTextBlock(Guid.NewGuid(), "Último texto"));
            f.Page = new(Guid.NewGuid(), "Página de estudo", blocks);
            Assert.Equal(NotebookSaveStatus.Saved, (await f.Actions.SaveAsync(f.Page)).Status);
            f.Gate = new ExportImageActions(f.Services.GetRequiredService<INotebookActions>());
            f.Editor = ActivatorUtilities.CreateInstance<NotebookEditorViewModel>(f.Services, f.Gate);
            if (!newPage) Assert.True(await f.Editor.TryLoadAsync(f.Page.Id));
            f.View = new NotebookEditorView(f.Editor);
            f.Window = new Window { Width = 1100, Height = 850, Content = f.View };
            f.Window.Show(); f.Pump();
            return f;
        }

        public Button ExportButton()
        {
            var button = View.FindControl<Button>("ExportMarkdownButton");
            Assert.True(button is not null, "A página aberta ainda não oferece exportação em Markdown.");
            return button!;
        }
        public async Task OpenAsync()
        {
            var button = ExportButton();
            Assert.True(button.IsEffectivelyVisible && button.IsEffectivelyEnabled);
            Click(button);
            await UntilAsync(() => Window.OwnedWindows.Any(window => window.Name == "ExportMarkdownDialog"));
        }
        public T Control<T>(string name) where T : Control => Assert.IsType<T>(Dialog.FindControl<T>(name));
        public void SetDestination(string? destination = null) => Control<TextBox>("ExportDestinationInput").Text = destination ?? Destination;
        public async Task<string> ExportAsync() { await OpenAsync(); return await CompleteExportAsync(); }
        public async Task<string> CompleteExportAsync()
        {
            SetDestination(); Click(Control<Button>("ConfirmExportButton"));
            await UntilAsync(() => Control<Button>("ConfirmExportButton").IsEnabled &&
                Control<TextBlock>("ExportFeedback").Text?.Contains("concluída", StringComparison.OrdinalIgnoreCase) == true);
            return (await File.ReadAllTextAsync(Path.Combine(Destination, "pagina.md"))).Replace("\r\n", "\n", StringComparison.Ordinal);
        }
        public async Task ExportFailureAsync(string? destination = null)
        {
            SetDestination(destination); Click(Control<Button>("ConfirmExportButton"));
            await UntilAsync(() => Control<Button>("ConfirmExportButton").IsEnabled &&
                Control<TextBlock>("ExportFeedback").Text?.Contains("Não", StringComparison.OrdinalIgnoreCase) == true);
        }
        public string Draft() => JsonSerializer.Serialize(new { Editor.Title, Editor.Blocks });
        public void Click(Button button) { Assert.True(button.IsEffectivelyEnabled); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(); }
        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public async Task UntilAsync(Func<bool> ready)
        {
            var end = DateTime.UtcNow.AddSeconds(10);
            while (!ready()) { Assert.True(DateTime.UtcNow < end, "A operação de exportação não concluiu no prazo do teste."); Pump(); await Task.Delay(10); }
            Pump();
        }
        public void Dispose()
        {
            foreach (var dialog in Window.OwnedWindows.ToArray()) dialog.Close();
            View.DataContext = null; Window.Close(); Editor.Dispose(); Services.Dispose(); Root.Dispose();
        }
    }

    private enum ImageMode { Normal, Missing, Failure, Blocked, Track }

    private sealed class ExportImageActions(INotebookActions inner) : INotebookActions
    {
        public ImageMode Mode { get; set; }
        public List<ExportImageStream> ExportStreams { get; } = [];
        public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot page, CancellationToken cancellationToken = default) => inner.SaveAsync(page, cancellationToken);
        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => inner.LoadAsync(id, cancellationToken);
        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => inner.ImportImageAsync(source, cancellationToken);
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (Mode == ImageMode.Normal) return inner.OpenImageAsync(id, cancellationToken);
            if (Mode == ImageMode.Missing) return Task.FromResult<Stream?>(null);
            var stream = new ExportImageStream(Mode); ExportStreams.Add(stream);
            return Task.FromResult<Stream?>(stream);
        }
    }

    private sealed class ExportImageStream(ImageMode mode) : MemoryStream(NotebookTestWorkspace.Png)
    {
        public bool Disposed { get; private set; }
        public bool ReadStarted { get; private set; }
        public bool CancellationObserved { get; private set; }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await BeforeReadAsync(cancellationToken);
            await base.CopyToAsync(destination, bufferSize, cancellationToken);
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await BeforeReadAsync(cancellationToken);
            return base.Read(buffer.Span);
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        private async Task BeforeReadAsync(CancellationToken cancellationToken)
        {
            ReadStarted = true;
            if (mode == ImageMode.Failure) throw new IOException("Synthetic export read failure.");
            if (mode == ImageMode.Blocked)
            {
                try { await Task.Delay(Timeout.Infinite, cancellationToken); }
                catch (OperationCanceledException) { CancellationObserved = true; throw; }
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (mode == ImageMode.Failure) throw new IOException("Synthetic export read failure.");
            return base.Read(buffer, offset, count);
        }
        public override int Read(Span<byte> buffer)
        {
            if (mode == ImageMode.Failure) throw new IOException("Synthetic export read failure.");
            return base.Read(buffer);
        }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
