using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class WindowClosingTests
{
    [Fact]
    public async Task CleanPage_AllowsClosingWithoutPromptingOrSaving()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, prompt);

        Assert.True(await library.RequestCloseAsync());

        Assert.Equal(0, prompt.Calls);
        Assert.Empty(actions.Saved);
    }

    [Fact]
    public async Task Cancel_KeepsTheDraftAndItsSelectedNotebook()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(actions, prompt);
        var editor = library.Editor;
        editor.Title = "Não perder ao fechar";

        Assert.False(await library.RequestCloseAsync());

        Assert.Same(editor, library.Editor);
        Assert.Equal(actions.Notebook.Id, library.SelectedNotebookId);
        Assert.Equal("Não perder ao fechar", editor.Title);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Empty(actions.Saved);
        Assert.Equal(1, prompt.Calls);
        Assert.False(library.IsBusy);
    }

    [Fact]
    public async Task Discard_AllowsClosingWithoutWritingOrClearingTheEditor()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt(UnsavedChangesChoice.Discard);
        var library = await ReadyAsync(actions, prompt);
        var editor = library.Editor;
        editor.Title = "Descartar apenas ao fechar";

        Assert.True(await library.RequestCloseAsync());

        Assert.Same(editor, library.Editor);
        Assert.Equal("Descartar apenas ao fechar", editor.Title);
        Assert.Empty(actions.Saved);
        Assert.Equal(1, prompt.Calls);
    }

    [Fact]
    public async Task Save_AllowsClosingOnlyAfterSavingTheCurrentPageInItsNotebook()
    {
        var actions = new NavigationActions();
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        var editor = library.Editor;
        editor.Title = "Página salva antes de sair";
        editor.Blocks[0].Text = "  Conteúdo 🧵\r\n";

        Assert.True(await library.RequestCloseAsync());

        var saved = Assert.Single(actions.Saved);
        Assert.Equal(editor.PageId, saved.Page.Id);
        Assert.Equal(actions.Notebook.Id, saved.NotebookId);
        Assert.Equal(editor.Title, saved.Page.Title);
        Assert.Equal("  Conteúdo 🧵\r\n", Assert.IsType<NotebookTextBlock>(Assert.Single(saved.Page.Blocks)).Text);
        Assert.False(editor.HasUnsavedChanges);
    }

    [Theory]
    [InlineData(NotebookSaveStatus.ValidationFailed)]
    [InlineData(NotebookSaveStatus.PersistenceFailed)]
    public async Task FailedSave_DoesNotAuthorizeClosingOrClearTheDraft(NotebookSaveStatus failure)
    {
        var actions = new NavigationActions { SaveResult = new(failure) };
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        var editor = library.Editor;
        editor.Title = "Manter na falha";

        Assert.False(await library.RequestCloseAsync());

        Assert.Same(editor, library.Editor);
        Assert.Equal("Manter na falha", editor.Title);
        Assert.True(editor.HasUnsavedChanges);
        Assert.False(library.IsBusy);
    }

    [Fact]
    public async Task SaveChoice_DoesNotAuthorizeClosingWhileTheWriteIsPending()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new NavigationActions { PendingSave = pending.Task };
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        library.Editor.Title = "Aguardar o banco";
        var close = library.RequestCloseAsync();
        try
        {
            Assert.False(close.IsCompleted);
            Assert.Single(actions.Saved);
            Assert.False(library.CanNavigate);
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
        }

        Assert.True(await close);
    }

    [Fact]
    public async Task EditAfterTheSaveSnapshot_PreventsClosingWithNewChangesStillPending()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new NavigationActions { PendingSave = pending.Task };
        var library = await ReadyAsync(actions, new NavigationPrompt(UnsavedChangesChoice.Save));
        library.Editor.Title = "Título enviado";
        var close = library.RequestCloseAsync();
        try
        {
            library.Editor.Title = "Edição posterior ao envio";
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
        }

        Assert.False(await close);
        Assert.Equal("Título enviado", Assert.Single(actions.Saved).Page.Title);
        Assert.Equal("Edição posterior ao envio", library.Editor.Title);
        Assert.True(library.Editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task RepeatedCloseRequests_DoNotOpenAnotherConfirmation()
    {
        var decision = new TaskCompletionSource<UnsavedChangesChoice>(TaskCreationOptions.RunContinuationsAsynchronously);
        var prompt = new ControlledPrompt(decision.Task);
        var library = await ReadyAsync(new NavigationActions(), prompt);
        library.Editor.Title = "Uma confirmação por vez";
        var first = library.RequestCloseAsync();
        try
        {
            Assert.False(await library.RequestCloseAsync());
            Assert.Equal(1, prompt.Calls);
        }
        finally
        {
            decision.TrySetResult(UnsavedChangesChoice.Cancel);
        }

        Assert.False(await first);
    }

    [Fact]
    public async Task AlreadySaving_BlocksTheCloseAttemptAndAllowsRetryAfterCompletion()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt(UnsavedChangesChoice.Discard);
        var library = await ReadyAsync(actions, prompt);
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        actions.PendingSave = pending.Task;
        library.Editor.Title = "Gravação já iniciada";
        var saving = library.Editor.SaveCommand.ExecuteAsync(null);
        try
        {
            Assert.False(await library.RequestCloseAsync());
            Assert.Equal(0, prompt.Calls);
            Assert.Single(actions.Saved);
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            await saving;
        }

        Assert.True(await library.RequestCloseAsync());
    }

    [Fact]
    public async Task ConfirmationFailure_KeepsTheDraftAndDoesNotExposeErrorDetails()
    {
        var prompt = new ControlledPrompt(Task.FromException<UnsavedChangesChoice>(new IOException("PRIVATE_TEST_DETAIL")));
        var library = await ReadyAsync(new NavigationActions(), prompt);
        library.Editor.Title = "Preservar";

        Assert.False(await library.RequestCloseAsync());

        Assert.Equal("Preservar", library.Editor.Title);
        Assert.True(library.Editor.HasUnsavedChanges);
        Assert.False(string.IsNullOrWhiteSpace(library.Feedback));
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", library.Feedback);
        Assert.False(library.IsBusy);
    }

    [AvaloniaFact]
    public async Task WindowClose_WhenCanceled_KeepsTheWindowVisibleAndDoesNotRaiseClosed()
    {
        var prompt = new NavigationPrompt();
        var library = await ReadyAsync(new NavigationActions(), prompt);
        library.Editor.Title = "Janela deve continuar aberta";
        var window = new MainWindow(new MainWindowViewModel { Library = library });
        var closed = false;
        window.Closed += (_, _) => closed = true;
        try
        {
            window.Show();
            window.Close();
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.IsVisible);
            Assert.False(closed);
            Assert.True(Assert.IsAssignableFrom<Control>(window.Content).IsEnabled);
            Assert.Equal(1, prompt.Calls);
            Assert.True(library.Editor.HasUnsavedChanges);
        }
        finally
        {
            window.DataContext = null;
            if (window.IsVisible) window.Close();
        }
    }

    [AvaloniaFact]
    public async Task WindowClose_WhenDiscarded_ConfirmsOnceAndClosesWithoutSaving()
    {
        var actions = new NavigationActions();
        var prompt = new NavigationPrompt(UnsavedChangesChoice.Discard);
        var library = await ReadyAsync(actions, prompt);
        library.Editor.Title = "Fechar sem salvar";
        var window = new MainWindow(new MainWindowViewModel { Library = library });
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closedCount = 0;
        window.Closed += (_, _) => { closedCount++; closed.TrySetResult(); };
        try
        {
            window.Show();
            window.Close();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, prompt.Calls);
            Assert.Equal(1, closedCount);
            Assert.Empty(actions.Saved);
            Assert.False(window.IsVisible);
        }
        finally
        {
            window.DataContext = null;
            if (window.IsVisible) window.Close();
        }
    }

    [AvaloniaFact]
    public async Task WindowClose_WaitsForSavingAndIgnoresRepeatedCloseClicks()
    {
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new NavigationActions { PendingSave = pending.Task };
        var prompt = new NavigationPrompt(UnsavedChangesChoice.Save);
        var library = await ReadyAsync(actions, prompt);
        library.Editor.Title = "Salvar antes de encerrar";
        var window = new MainWindow(new MainWindowViewModel { Library = library });
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closedCount = 0;
        window.Closed += (_, _) => { closedCount++; closed.TrySetResult(); };
        try
        {
            window.Show();
            window.Close();
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.IsVisible);
            Assert.False(closed.Task.IsCompleted);
            Assert.False(Assert.IsAssignableFrom<Control>(window.Content).IsEnabled);
            Assert.Equal(1, prompt.Calls);
            Assert.Single(actions.Saved);

            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, closedCount);
            Assert.False(library.Editor.HasUnsavedChanges);
            Assert.False(window.IsVisible);
        }
        finally
        {
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            window.DataContext = null;
            if (window.IsVisible) window.Close();
        }
    }

    private static async Task<LibraryViewModel> ReadyAsync(NavigationActions actions, IUnsavedChangesPrompt prompt)
    {
        var library = new LibraryViewModel(actions, prompt);
        await library.InitializeAsync();
        await library.SelectSpaceAsync(actions.Course.Id);
        await library.SelectNotebookAsync(actions.Notebook.Id);
        return library;
    }

    private sealed class ControlledPrompt(Task<UnsavedChangesChoice> decision) : IUnsavedChangesPrompt
    {
        public int Calls { get; private set; }

        public Task<UnsavedChangesChoice> ChooseAsync()
        {
            Calls++;
            return decision;
        }
    }
}
