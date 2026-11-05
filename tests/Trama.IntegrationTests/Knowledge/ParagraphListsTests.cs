using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class ParagraphListsTests
{
    private const string Body = "Um\nDois\nTrês";

    [Theory]
    [InlineData("bullet")]
    [InlineData("numbered")]
    public void Contract_RoundTripPreservesParagraphListMetadataWithoutChangingText(string kind)
    {
        var block = FromWire(Body, [(0, kind), (3, kind), (8, kind)]);
        var draft = new NotebookDraft(); draft.Insert(0, block);

        Assert.Equal(Body, block.Text);
        Assert.Equal(new[] { (0, kind), (3, kind), (8, kind) }, Lists(Assert.IsType<NotebookTextBlock>(Assert.Single(draft.Blocks))));
    }

    [Fact]
    public void Contract_AnEmptyFinalItemCanCarryListFormatting()
    {
        var block = FromWire("Um\n", [(0, "bullet"), (3, "bullet")]);
        var draft = new NotebookDraft(); draft.Insert(0, block);
        Assert.Equal(new[] { (0, "bullet"), (3, "bullet") }, Lists(block));
    }

    [Fact]
    public void Contract_CrlfParagraphStartsUseUtf16Offsets()
    {
        var block = FromWire("Um\r\nDois", [(0, "numbered"), (4, "numbered")]);
        var draft = new NotebookDraft(); draft.Insert(0, block);
        Assert.Equal(new[] { (0, "numbered"), (4, "numbered") }, Lists(block));
    }

    [Fact]
    public void Contract_ListsCoexistWithInlineColorsSizesAndHighlights()
    {
        var block = FromWire(Body, [(0, "bullet")], [RichStyle()]);
        var draft = new NotebookDraft(); draft.Insert(0, block);
        Assert.Equal(RichStyle(), Assert.Single(block.Styles));
        Assert.Equal(new[] { (0, "bullet") }, Lists(block));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(99)]
    public void Contract_RejectsOffsetsThatAreNotParagraphStarts(int offset)
    {
        Assert.ThrowsAny<ArgumentException>(() => new NotebookDraft().Insert(0, FromWire(Body, [(offset, "bullet")])));
    }

    [Fact]
    public void Contract_RejectsTwoListKindsAtTheSameParagraphStart()
    {
        Assert.ThrowsAny<ArgumentException>(() => new NotebookDraft().Insert(0, FromWire(Body, [(0, "bullet"), (0, "numbered")])));
    }

    [Theory]
    [InlineData("none")]
    [InlineData("checklist")]
    [InlineData("NUMBERED")]
    public void Contract_RejectsUnsupportedStoredListKinds(string kind)
    {
        Assert.ThrowsAny<ArgumentException>(() => new NotebookDraft().Insert(0, FromWire(Body, [(0, kind)])));
    }

    [Fact]
    public void ExistingPlainText_AlreadyWorksWithoutListMetadata()
    {
        var block = new NotebookTextBlock(Guid.NewGuid(), Body);
        var draft = new NotebookDraft(); draft.Insert(0, block);
        Assert.Equal(Body, block.Text);
        Assert.Empty(Lists(block, required: false));
    }

    [AvaloniaTheory]
    [InlineData("bullet")]
    [InlineData("numbered")]
    public async Task Toolbar_AppliesListToTheCaretParagraphOnly(string kind)
    {
        using var f = await Fixture.CreateAsync();
        f.Select(4, 0); f.Choose(kind);
        Assert.Equal(Body, f.Prose.Text);
        Assert.Equal(new[] { (3, kind) }, Lists(await f.SaveAndReadAsync()));
    }

    [AvaloniaFact]
    public async Task Toolbar_AppliesOnlyToParagraphsCoveredByTheSelection()
    {
        using var f = await Fixture.CreateAsync();
        f.Select(0, 7); f.Choose("bullet");
        Assert.Equal(new[] { (0, "bullet"), (3, "bullet") }, Lists(await f.SaveAndReadAsync()));
    }

    [AvaloniaFact]
    public async Task ConvertingAndRemovingLists_PreservesTextAndInlineFormatting()
    {
        using var f = await Fixture.CreateAsync(rich: true);
        f.Select(0, Body.Length); f.Choose("bullet");
        Assert.Equal(3, Lists(await f.SaveAndReadAsync()).Length);
        f.Choose("numbered");
        Assert.All(Lists(await f.SaveAndReadAsync()), item => Assert.Equal("numbered", item.Kind));
        f.Choose("none");
        var saved = await f.SaveAndReadAsync();
        Assert.Empty(Lists(saved, required: false));
        Assert.Equal(Body, saved.Text);
        Assert.Equal(RichStyle(), Assert.Single(saved.Styles));
        Assert.Equal(3, await f.VersionAsync());
    }

    [AvaloniaFact]
    public async Task Enter_ContinuesANumberedListWithTheNextParagraph()
    {
        using var f = await Fixture.CreateAsync(body: "Um");
        f.Select(2, 0); f.Choose("numbered"); f.Key(PhysicalKey.Enter);
        Assert.Equal("Um\n", Normalize(f.Prose.Text));
        var next = f.Prose.Document.GetLineByNumber(2).Offset;
        Assert.Equal(new[] { (0, "numbered"), (next, "numbered") }, Lists(await f.SaveAndReadAsync()));
    }

    [AvaloniaFact]
    public async Task Enter_OnAnEmptyItemExitsTheListWithoutAddingAnotherEmptyLine()
    {
        using var f = await Fixture.CreateAsync(body: "Um");
        f.Select(2, 0); f.Choose("bullet"); f.Key(PhysicalKey.Enter);
        var before = f.Prose.Text;
        f.Key(PhysicalKey.Enter);
        Assert.Equal(before, f.Prose.Text);
        Assert.Equal(new[] { (0, "bullet") }, Lists(await f.SaveAndReadAsync()));
    }

    [AvaloniaFact]
    public async Task Enter_InTheMiddleOfAnItemSplitsItAndPreservesExistingStyles()
    {
        using var f = await Fixture.CreateAsync(body: "UmDois", rich: true);
        f.Select(2, 0); f.Choose("numbered"); f.Key(PhysicalKey.Enter);
        Assert.Equal("Um\nDois", Normalize(f.Prose.Text));
        var saved = await f.SaveAndReadAsync();
        Assert.Equal(new[] { (0, "numbered"), (f.Prose.Document.GetLineByNumber(2).Offset, "numbered") }, Lists(saved));
        Assert.Equal(RichStyle(), Assert.Single(saved.Styles));
    }

    [AvaloniaFact]
    public async Task JoiningParagraphs_RemovesObsoleteStartsAndUpdatesRemainingOffsets()
    {
        using var f = await Fixture.CreateAsync();
        f.Select(0, Body.Length); f.Choose("numbered");
        f.Prose.Document.Remove(2, 1); f.Pump();
        Assert.Equal("UmDois\nTrês", f.Prose.Text);
        Assert.Equal(new[] { (0, "numbered"), (7, "numbered") }, Lists(await f.SaveAndReadAsync()));
    }

    [AvaloniaFact]
    public async Task ListFormatting_IsUndoableAndRedoableAfterManualSave()
    {
        using var f = await Fixture.CreateAsync();
        f.Select(0, Body.Length); f.Choose("bullet");
        Assert.Equal(3, Lists(await f.SaveAndReadAsync()).Length);
        Assert.True(f.Prose.Undo());
        Assert.Empty(Lists(await f.SaveAndReadAsync(), required: false));
        Assert.True(f.Prose.Redo());
        Assert.Equal(3, Lists(await f.SaveAndReadAsync()).Length);
        Assert.Equal(Body, f.Prose.Text);
    }

    [AvaloniaFact]
    public async Task Autosave_PersistsListsAndKeepsTheirUndoHistory()
    {
        using var f = await Fixture.CreateAsync(autoSave: true);
        f.Select(0, Body.Length); f.Choose("numbered");
        await Fixture.UntilAsync(() => !f.Editor.IsBusy && !f.Editor.HasUnsavedChanges && f.Editor.LastSaveStatus == NotebookSaveStatus.Saved);
        Assert.Equal(3, Lists(Assert.IsType<NotebookTextBlock>((await f.Actions.LoadAsync(f.Page.Id))!.Blocks[0])).Length);
        Assert.True(f.Prose.CanUndo);
        Assert.True(f.Prose.Undo());
    }

    [AvaloniaFact]
    public async Task FocusingCode_DisablesListFormattingAndDoesNotEditThePage()
    {
        using var f = await Fixture.CreateAsync();
        var picker = f.Picker();
        var code = f.View.GetVisualDescendants().OfType<NotebookCodeEditor>().Single();
        code.TextArea.Focus(); f.Pump();
        Assert.False(picker.IsEffectivelyEnabled);
        Assert.False(f.Editor.HasUnsavedChanges);
        Assert.Equal("print(42)", code.Text);
    }

    [AvaloniaFact]
    public async Task ListControl_IsAvailableInTheExistingCompactFormattingBar()
    {
        using var f = await Fixture.CreateAsync();
        f.Window.Width = 860; f.Pump();
        var picker = f.Picker();
        Assert.False(picker.IsEffectivelyVisible);
        f.View.FindControl<Button>("ToggleFormattingButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); f.Pump();
        Assert.True(picker.IsEffectivelyVisible);
        f.Select(0, 0); f.Choose("bullet");
        var bar = f.View.FindControl<Border>("ProseFormattingBar")!;
        var point = picker.TranslatePoint(default, bar)!.Value;
        Assert.InRange(point.X, 0, bar.Bounds.Width - picker.Bounds.Width + 1);
        Assert.InRange(point.Y, 0, bar.Bounds.Height - picker.Bounds.Height + 1);
    }

    [AvaloniaFact]
    public async Task ExistingEditor_AlreadySavesPlainParagraphsAndRichStylesWithoutLists()
    {
        using var f = await Fixture.CreateAsync(rich: true);
        var block = await f.SaveAndReadAsync();
        Assert.Equal(Body, block.Text);
        Assert.Equal(RichStyle(), Assert.Single(block.Styles));
        Assert.Empty(Lists(block, required: false));
    }

    [Theory]
    [InlineData("bullet")]
    [InlineData("numbered")]
    public async Task Storage_ReopensBothListKindsWithoutChangingCodeOrImages(string kind)
    {
        using var f = await Fixture.CreateAsync(showView: false, initialKind: kind, includeImage: true);
        using var reopened = await DesktopComposition.CreateAsync(f.Source);
        var loaded = await reopened.GetRequiredService<ILibraryActions>().LoadAsync(f.Page.Id);
        Assert.NotNull(loaded);
        Assert.Equal(new[] { (0, kind), (3, kind), (8, kind) }, Lists(Assert.IsType<NotebookTextBlock>(loaded.Blocks[0])));
        Assert.Equal("print(42)", Assert.IsType<NotebookCodeBlock>(loaded.Blocks[1]).Code);
        Assert.Equal(f.ImageId, Assert.IsType<NotebookImageBlock>(loaded.Blocks[2]).AttachmentId);
    }

    [Fact]
    public async Task Storage_PagesWithListsUseVersionFour()
    {
        using var f = await Fixture.CreateAsync(showView: false, initialKind: "bullet");
        Assert.Equal(4, await f.VersionAsync());
    }

    [Fact]
    public async Task BackupAndTrashRoundTrip_PreserveListStructureStylesTagsAndImages()
    {
        using var f = await Fixture.CreateAsync(showView: false, initialKind: "numbered", rich: true, includeImage: true);
        var trash = f.Services.GetRequiredService<ILibraryTrashActions>();
        await trash.MoveToTrashAsync(LibraryEntryKind.Page, f.Page.Id);
        var backup = f.Services.GetRequiredService<ILocalBackup>();
        await backup.CreateAsync(f.Archive);
        await backup.RestoreAsync(f.Archive, f.Destination);
        using var restored = await DesktopComposition.CreateAsync(f.Destination);
        await restored.GetRequiredService<ILibraryTrashActions>().RestoreAsync(LibraryEntryKind.Page, f.Page.Id);
        var actions = restored.GetRequiredService<ILibraryActions>();
        var loaded = await actions.LoadAsync(f.Page.Id);
        Assert.NotNull(loaded);
        var text = Assert.IsType<NotebookTextBlock>(loaded.Blocks[0]);
        Assert.Equal(new[] { (0, "numbered"), (3, "numbered"), (8, "numbered") }, Lists(text));
        Assert.Equal(RichStyle(), Assert.Single(text.Styles));
        Assert.Equal(new[] { "listas" }, await restored.GetRequiredService<IKnowledgeDiscovery>().GetPageTagsAsync(f.Page.Id));
        await using var image = await actions.OpenImageAsync(f.ImageId);
        Assert.NotNull(image);
        using var bytes = new MemoryStream(); await image.CopyToAsync(bytes);
        Assert.Equal(NotebookTestWorkspace.Png, bytes.ToArray());
    }

    [Fact]
    public async Task Storage_RejectsListMetadataMislabeledAsVersionOne()
    {
        using var f = await Fixture.CreateAsync(showView: false, initialKind: "numbered");
        await using (var context = f.Context())
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE NotebookPages SET FormatVersion = 1 WHERE Id = {f.Page.Id};");
        await Assert.ThrowsAsync<NotePersistenceException>(() => f.Actions.LoadAsync(f.Page.Id));
    }

    [Fact]
    public async Task ExistingStorage_AlreadyKeepsPlainPagesAtVersionOne()
    {
        using var f = await Fixture.CreateAsync(showView: false);
        Assert.Equal(1, await f.VersionAsync());
        Assert.Equal(Body, Assert.IsType<NotebookTextBlock>((await f.Actions.LoadAsync(f.Page.Id))!.Blocks[0]).Text);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
    private static TextStyleSpan RichStyle() => new(0, 2, InlineTextStyle.Bold | InlineTextStyle.Italic)
    { FontSize = 24, Foreground = "teal", Highlight = "amber" };

    // Specifies the additive serialization contract without inventing production stubs.
    private static NotebookTextBlock FromWire(string text, (int Offset, string Kind)[]? lists, IReadOnlyList<TextStyleSpan>? styles = null) =>
        JsonSerializer.Deserialize<NotebookTextBlock>(JsonSerializer.Serialize(new
        {
            Id = Guid.NewGuid(),
            Text = text,
            Styles = styles ?? Array.Empty<TextStyleSpan>(),
            Lists = lists?.Select(item => new { item.Offset, item.Kind }).ToArray()
        }))!;

    private static (int Offset, string Kind)[] Lists(NotebookTextBlock block, bool required = true)
    {
        var json = JsonSerializer.SerializeToElement(block);
        var present = json.TryGetProperty("Lists", out var lists) && lists.ValueKind == JsonValueKind.Array;
        if (required) Assert.True(present, "A estrutura de listas ainda não é preservada no bloco de texto.");
        return present ? lists.EnumerateArray().Select(item => (item.GetProperty("Offset").GetInt32(), item.GetProperty("Kind").GetString()!)).ToArray() : [];
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Source => Path.Combine(Root.DirectoryPath, "source");
        public string Archive => Path.Combine(Root.DirectoryPath, "lists.trama-backup");
        public string Destination => Path.Combine(Root.DirectoryPath, "restored");
        public ServiceProvider Services { get; private set; } = null!;
        public ILibraryActions Actions { get; private set; } = null!;
        public NotebookSnapshot Page { get; private set; } = null!;
        public NotebookEditorViewModel Editor { get; private set; } = null!;
        public NotebookEditorView View { get; private set; } = null!;
        public Window Window { get; private set; } = null!;
        public Guid ImageId { get; private set; }
        public NotebookProseEditor Prose => View.GetVisualDescendants().OfType<NotebookProseEditor>().Single();

        public static async Task<Fixture> CreateAsync(string body = Body, bool rich = false, bool autoSave = false,
            bool showView = true, string? initialKind = null, bool includeImage = false)
        {
            var f = new Fixture();
            f.Services = await DesktopComposition.CreateAsync(f.Source, autoSave);
            f.Actions = f.Services.GetRequiredService<ILibraryActions>();
            var space = await f.Actions.CreateSpaceAsync("Estudos");
            var book = await f.Actions.CreateNotebookAsync(space.Id, "Listas");
            var entries = initialKind is null ? null : new[] { (0, initialKind), (3, initialKind), (8, initialKind) };
            var blocks = new List<NotebookBlock> { FromWire(body, entries, rich ? [RichStyle()] : []), new NotebookCodeBlock(Guid.NewGuid(), "print(42)", "python") };
            if (includeImage)
            {
                using var image = new MemoryStream(NotebookTestWorkspace.Png);
                var attachment = await f.Actions.ImportImageAsync(image);
                f.ImageId = attachment.Id;
                blocks.Add(new NotebookImageBlock(Guid.NewGuid(), f.ImageId, "Imagem"));
            }
            f.Page = new(Guid.NewGuid(), "Página de listas", blocks);
            Assert.Equal(NotebookSaveStatus.Saved, (await f.Actions.SaveInNotebookAsync(f.Page, book.Id)).Status);
            await f.Services.GetRequiredService<IKnowledgeDiscovery>().SetPageTagsAsync(f.Page.Id, ["listas"]);
            if (showView)
            {
                f.Editor = f.Services.GetRequiredService<NotebookEditorViewModel>();
                Assert.True(await f.Editor.TryLoadAsync(f.Page.Id));
                f.View = new NotebookEditorView(f.Editor);
                f.Window = new Window { Width = 1100, Height = 850, Content = f.View };
                f.Window.Show(); f.Pump();
            }
            return f;
        }

        public ComboBox Picker()
        {
            var picker = View.FindControl<ComboBox>("ProseListPicker");
            Assert.True(picker is not null, "A barra de formatação ainda não oferece o seletor de listas.");
            return picker!;
        }

        public void Select(int offset, int length) { Prose.TextArea.Focus(); Prose.Select(offset, length); Pump(); }
        public void Choose(string kind)
        {
            var picker = Picker(); picker.Focus();
            picker.SelectedItem = Assert.Single(picker.Items.OfType<ComboBoxItem>(), item => item.Tag?.ToString() == kind);
            Pump();
        }
        public void Key(PhysicalKey key) { Prose.TextArea.Focus(); Window.KeyPressQwerty(key, RawInputModifiers.None); Window.KeyReleaseQwerty(key, RawInputModifiers.None); Pump(); }
        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public async Task<NotebookTextBlock> SaveAndReadAsync()
        {
            await Editor.SaveCommand.ExecuteAsync(null);
            Assert.Equal(NotebookSaveStatus.Saved, Editor.LastSaveStatus);
            return Assert.IsType<NotebookTextBlock>((await Actions.LoadAsync(Page.Id))!.Blocks[0]);
        }
        public TramaDbContext Context() => new(new DbContextOptionsBuilder<TramaDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(Source, "trama.db"), Pooling = false }.ToString()).Options);
        public async Task<int> VersionAsync()
        {
            await using var context = Context();
            return await context.Database.SqlQuery<int>($"SELECT FormatVersion AS Value FROM NotebookPages WHERE Id = {Page.Id}").SingleAsync();
        }
        public static async Task UntilAsync(Func<bool> ready)
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            while (!ready()) { Assert.True(System.Diagnostics.Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(5), "O autosave não concluiu no prazo do teste."); Dispatcher.UIThread.RunJobs(); await Task.Delay(10); }
            Dispatcher.UIThread.RunJobs();
        }
        public void Dispose()
        {
            if (Window is not null) { View.DataContext = null; Window.Close(); }
            Services.Dispose(); Root.Dispose();
        }
    }
}
