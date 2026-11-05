using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NotebookScrollingRegressionTests
{
    [AvaloniaTheory]
    [InlineData(false, -1)]
    [InlineData(false, 1)]
    [InlineData(true, -1)]
    [InlineData(true, 1)]
    public async Task WheelOverBlock_ScrollsThePageInBothDirections(bool code, int direction)
    {
        using var f = await Fixture.CreateAsync(code);
        f.Scroll.Offset = new Vector(0, 650); f.Pump();
        var before = f.Scroll.Offset.Y;
        var point = f.Text.TextArea.TranslatePoint(new Point(80, 700), f.Window)!.Value;
        Assert.InRange(point.Y, 100, f.Window.Height - 60);
        f.Window.MouseWheel(point, new Vector(0, direction)); f.Pump();
        Assert.True(direction < 0 ? f.Scroll.Offset.Y > before : f.Scroll.Offset.Y < before,
            $"A roda sobre o bloco deveria mover a página: antes={before}, depois={f.Scroll.Offset.Y}, direção={direction}.");
        Assert.Equal(f.Body, f.Text.Text);
    }

    [AvaloniaFact]
    public async Task SelectingVisibleTextFarFromTop_DoesNotJumpToTheStartOfTheBlock()
    {
        using var f = await Fixture.CreateAsync(false);
        f.Text.TextArea.Focus(); f.Pump();
        f.Scroll.Offset = new Vector(0, 900); f.Pump();
        var before = f.Scroll.Offset.Y;
        // Select an actual visible line using pointer input, not a synthetic selection notification.
        var view = f.Text.TextArea.TextView;
        var line = view.GetVisualPosition(new AvaloniaEdit.TextViewPosition(60, 1), AvaloniaEdit.Rendering.VisualYPosition.LineMiddle);
        var point = view.TranslatePoint(new Point(line.X + 5, line.Y), f.Window)!.Value;
        Assert.InRange(point.Y, 100, f.Window.Height - 60);
        f.Window.MouseDown(point, MouseButton.Left);
        f.Window.MouseMove(point + new Vector(90, 0));
        f.Window.MouseUp(point + new Vector(90, 0), MouseButton.Left); f.Pump();
        Assert.True(f.Text.SelectionLength > 0);
        Assert.InRange(f.Scroll.Offset.Y, before - 2, before + 2);
        Assert.Equal(f.Body, f.Text.Text);
    }

    [AvaloniaFact]
    public async Task SelectionAndFormattingOnALowerLine_KeepTheVisiblePageAndHistory()
    {
        using var f = await Fixture.CreateAsync(false);
        var prose = Assert.IsType<NotebookProseEditor>(f.Text);
        prose.TextArea.Focus();
        f.Scroll.Offset = new Vector(0, 900); f.Pump();
        var before = f.Scroll.Offset.Y;
        var offset = prose.Document.GetLineByNumber(60).Offset;
        prose.Select(offset, 10); f.Pump();
        Assert.InRange(f.Scroll.Offset.Y, before - 2, before + 2);
        Assert.True(prose.ApplyStyle(InlineTextStyle.Bold)); f.Pump();
        Assert.InRange(f.Scroll.Offset.Y, before - 2, before + 2);
        Assert.Equal(offset, prose.SelectionStart); Assert.Equal(10, prose.SelectionLength);
        Assert.True(prose.Undo()); f.Pump();
        Assert.Empty(f.Editor.Blocks[0].Styles);
        Assert.Equal(f.Body, prose.Text);
    }

    private sealed class Fixture : IDisposable
    {
        public required Window Window { get; init; }
        public required NotebookEditorViewModel Editor { get; init; }
        public required NotebookEditorView View { get; init; }
        public required string Body { get; init; }
        public ScrollViewer Scroll => View.FindControl<ScrollViewer>("PageScroll")!;
        public TextEditor Text => Window.GetVisualDescendants().OfType<TextEditor>().Single();
        public static async Task<Fixture> CreateAsync(bool code)
        {
            var body = string.Join('\n', Enumerable.Range(1, 120).Select(i => $"Linha {i:000}: conteúdo fictício para testar seleção e rolagem."));
            NotebookBlock block = code ? new NotebookCodeBlock(Guid.NewGuid(), body, "plaintext") : new NotebookTextBlock(Guid.NewGuid(), body);
            var actions = new Actions(new NotebookSnapshot(Guid.NewGuid(), "Rolagem", [block]));
            var editor = new NotebookEditorViewModel(actions);
            Assert.True(await editor.TryLoadAsync(actions.Page.Id));
            var view = new NotebookEditorView(editor);
            var window = new Window { Width = 1100, Height = 800, Content = view };
            window.Show();
            var f = new Fixture { Window = window, Editor = editor, View = view, Body = body }; f.Pump();
            return f;
        }
        public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Dispose() { Editor.Dispose(); Window.Close(); }
    }

    private sealed class Actions(NotebookSnapshot page) : INotebookActions
    {
        public NotebookSnapshot Page => page;
        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<NotebookSnapshot?>(page);
        public Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default) => Task.FromResult(new NotebookSaveResult(NotebookSaveStatus.Saved));
        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }
}
