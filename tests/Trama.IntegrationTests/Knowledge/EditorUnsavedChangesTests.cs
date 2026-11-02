using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class EditorUnsavedChangesTests
{
    [Fact]
    public void EmptyNewPage_DoesNotRequireDiscardConfirmation()
    {
        var editor = new NotebookEditorViewModel(new NavigationActions());

        Assert.False(editor.HasUnsavedChanges);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("text")]
    [InlineData("language")]
    [InlineData("add")]
    [InlineData("move")]
    [InlineData("remove")]
    [InlineData("image")]
    public async Task EditingOrReorganizingASavedPage_MarksItAsUnsaved(string change)
    {
        var editor = new NotebookEditorViewModel(new NavigationActions()) { Title = "Página salva" };
        editor.AddCodeCommand.Execute(null);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.False(editor.HasUnsavedChanges);

        switch (change)
        {
            case "title": editor.Title = "Título revisado"; break;
            case "text": editor.Blocks[0].Text = "Novo conteúdo"; break;
            case "language": editor.Blocks[1].Language = "python"; break;
            case "add": editor.AddTextCommand.Execute(null); break;
            case "move": editor.MoveBlockDownCommand.Execute(editor.Blocks[0].Id); break;
            case "remove": editor.RemoveBlockCommand.Execute(editor.Blocks[0].Id); break;
            case "image":
                using (var stream = new MemoryStream([1])) await editor.ImportImageAsync(stream);
                break;
        }

        Assert.True(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task FailedSave_DoesNotMarkDraftClean_AndSuccessfulLoadEstablishesACleanPage()
    {
        var actions = new NavigationActions { SaveResult = new(NotebookSaveStatus.PersistenceFailed) };
        var editor = new NotebookEditorViewModel(actions) { Title = "Não perder" };

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.True(editor.HasUnsavedChanges);
        Assert.True(await editor.TryLoadAsync(actions.FirstPage.Id));
        Assert.Equal(actions.FirstPage.Id, editor.PageId);
        Assert.False(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditArrivingDuringSave_RemainsUnsavedAfterTheOlderSnapshotCompletes()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new NavigationActions { PendingSave = pending.Task };
        var editor = new NotebookEditorViewModel(actions) { Title = "Original" };
        var save = editor.SaveCommand.ExecuteAsync(null);
        try
        {
            editor.Title = "Alteração posterior ao snapshot";
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            await save;
        }

        Assert.Equal("Original", Assert.Single(actions.Saved).Page.Title);
        Assert.Equal("Alteração posterior ao snapshot", editor.Title);
        Assert.True(editor.HasUnsavedChanges);
    }
}
