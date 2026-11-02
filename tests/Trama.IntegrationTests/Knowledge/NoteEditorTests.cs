using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NoteEditorTests
{
    [Fact]
    public async Task SaveCommand_OnSuccess_PreservesInputAndClearsTheSavedDraft()
    {
        var action = new RecordingAction();
        var editor = new NoteEditorViewModel(action) { Title = "  Café 🧵  ", Body = "\n    await SaveAsync();\t" };

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal(new CreateNoteRequest("  Café 🧵  ", "\n    await SaveAsync();\t"), Assert.Single(action.Requests));
        Assert.Equal(CreateNoteStatus.Created, editor.LastStatus);
        Assert.Equal(action.Result.Note, editor.LastSavedNote);
        Assert.Equal(string.Empty, editor.Title);
        Assert.Equal(string.Empty, editor.Body);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.False(editor.IsSaving);
    }

    [Theory]
    [InlineData(CreateNoteStatus.ValidationFailed)]
    [InlineData(CreateNoteStatus.PersistenceFailed)]
    public async Task SaveCommand_OnFailure_PreservesDraftAndReportsTheFailure(CreateNoteStatus status)
    {
        var action = new RecordingAction { Result = new CreateNoteResult(status, null) };
        var editor = new NoteEditorViewModel(action) { Title = " Draft ", Body = " Keep this\n" };

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal(status, editor.LastStatus);
        Assert.Null(editor.LastSavedNote);
        Assert.Equal(" Draft ", editor.Title);
        Assert.Equal(" Keep this\n", editor.Body);
        Assert.False(string.IsNullOrWhiteSpace(editor.Feedback));
        Assert.False(editor.IsSaving);
    }

    [Fact]
    public async Task SaveCommand_WhilePending_DisablesAnotherSaveAndResetsBusyStateAfterwards()
    {
        var pending = new TaskCompletionSource<CreateNoteResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var action = new RecordingAction { Pending = pending.Task };
        var editor = new NoteEditorViewModel(action) { Title = "Pending" };

        var save = editor.SaveCommand.ExecuteAsync(null);
        try
        {
            Assert.True(editor.IsSaving);
            Assert.False(editor.SaveCommand.CanExecute(null));
            Assert.Single(action.Requests);
        }
        finally
        {
            pending.TrySetResult(action.Result);
            await save;
        }

        Assert.False(editor.IsSaving);
        Assert.True(editor.SaveCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task EditorView_BindsTitleBodySaveAndFeedback()
    {
        var action = new RecordingAction();
        var editor = new NoteEditorViewModel(action);
        var view = new NoteEditorView(editor);
        var window = new Window { Content = view };
        try
        {
            window.Show();
            var title = Assert.IsType<TextBox>(view.FindControl<TextBox>("TitleInput"));
            var body = Assert.IsType<TextBox>(view.FindControl<TextBox>("BodyInput"));
            var save = Assert.IsType<Button>(view.FindControl<Button>("SaveButton"));
            var feedback = Assert.IsType<TextBlock>(view.FindControl<TextBlock>("SaveFeedback"));
            title.Text = "Title from view";
            body.Text = "Body from view";

            Assert.Same(editor.SaveCommand, save.Command);
            await editor.SaveCommand.ExecuteAsync(null);

            Assert.Equal(new CreateNoteRequest("Title from view", "Body from view"), Assert.Single(action.Requests));
            Assert.Equal(editor.Feedback, feedback.Text);
            Assert.Equal(string.Empty, title.Text);
            Assert.Equal(string.Empty, body.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task DesktopComposition_SavesThroughTheEditorAndPreservesTheNoteAfterReopening()
    {
        using var storage = new TemporaryStorage();
        NoteSnapshot saved;
        using (var services = await DesktopComposition.CreateAsync(storage.DataDirectory))
        {
            var editor = services.GetRequiredService<NoteEditorViewModel>();
            editor.Title = "Persistir";
            editor.Body = " Preserve after restart.\n";
            await editor.SaveCommand.ExecuteAsync(null);
            Assert.Equal(CreateNoteStatus.Created, editor.LastStatus);
            saved = Assert.IsType<NoteSnapshot>(editor.LastSavedNote);
        }

        using var reopenedServices = await DesktopComposition.CreateAsync(storage.DataDirectory);
        Assert.NotNull(reopenedServices.GetRequiredService<NoteEditorViewModel>());
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(storage.DataDirectory, "trama.db"),
            Pooling = false
        }.ToString();
        await using var context = new TramaDbContext(new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connectionString).Options);
        var recovered = await new SqliteNoteRepository(context).GetByIdAsync(saved.Id);
        Assert.Equal(saved, recovered);
        Assert.Equal("Persistir", recovered?.Title);
        Assert.Equal(" Preserve after restart.\n", recovered?.Body);
    }

    [Fact]
    public async Task DesktopComposition_WhenDirectoryCannotBeCreated_PropagatesFailureWithoutFallback()
    {
        using var storage = new TemporaryStorage();
        File.WriteAllText(storage.BlockingFile, "synthetic blocker");

        await Assert.ThrowsAsync<IOException>(
            () => DesktopComposition.CreateAsync(Path.Combine(storage.BlockingFile, "data")));

        Assert.Equal("synthetic blocker", File.ReadAllText(storage.BlockingFile));
        Assert.False(Directory.Exists(storage.DataDirectory));
    }

    private sealed class RecordingAction : ICreateNote
    {
        public List<CreateNoteRequest> Requests { get; } = [];
        public CreateNoteResult Result { get; init; } = new(CreateNoteStatus.Created, new NoteSnapshot(Guid.NewGuid(), "Saved", "Body"));
        public Task<CreateNoteResult>? Pending { get; init; }

        public Task<CreateNoteResult> ExecuteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Pending ?? Task.FromResult(Result);
        }
    }

    private sealed class TemporaryStorage : IDisposable
    {
        private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("trama-editor-");
        public string DataDirectory => Path.Combine(_root.FullName, "data");
        public string BlockingFile => Path.Combine(_root.FullName, "block");

        public void Dispose()
        {
            if (Directory.Exists(DataDirectory))
            {
                foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
                {
                    File.Delete(Path.Combine(DataDirectory, "trama.db") + suffix);
                }

                Directory.Delete(DataDirectory);
            }

            File.Delete(BlockingFile);
            _root.Delete();
        }
    }
}
