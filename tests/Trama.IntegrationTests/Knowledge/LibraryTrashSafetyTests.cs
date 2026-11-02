using System.ComponentModel;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Headless.XUnit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Desktop.ViewModels;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class LibraryTrashSafetyTests
{
    [Fact]
    public async Task TrashMigration_UpgradesThePreviousSchemaWithoutChangingRichContentOrLocations()
    {
        using var f = await Fixture.CreateAsync(previousSchema: true);
        await using (var before = f.Context(f.Source))
            Assert.Contains(await before.Database.GetPendingMigrationsAsync(), migration => migration.EndsWith("_LibraryTrash", StringComparison.Ordinal));

        await LocalDatabase.InitializeAsync(f.Source);

        await using var context = f.Context(f.Source);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(await context.Set<LibraryTrashRecord>().ToArrayAsync());
        await f.AssertPageAsync(f.Actions, f.Page.Id);
        Assert.Contains(await f.Actions.ListPagesAsync(f.Book.Id), item => item.Id == f.Page.Id);
        Assert.Equal(new[] { "estudo" }, await new SqliteKnowledgeDiscovery(f.Database).GetPageTagsAsync(f.Page.Id));
    }

    [Fact]
    public async Task BackupRoundTrip_PreservesTrashBatchesAndImagesWithoutTheOriginalProfile()
    {
        using var f = await Fixture.CreateAsync();
        await f.Trash.MoveToTrashAsync(LibraryEntryKind.Page, f.Page.Id);
        await f.Trash.MoveToTrashAsync(LibraryEntryKind.Space, f.Space.Id);
        var backup = new LocalBackupService(f.Source);
        await backup.CreateAsync(f.Archive);
        Directory.Move(f.Source, Path.Combine(f.Root.DirectoryPath, "original-unavailable"));

        await backup.RestoreAsync(f.Archive, f.Destination);

        var database = Path.Combine(f.Destination, "trama.db");
        var actions = new SqliteLibraryActions(database, new LocalImageAttachmentStore(f.Destination));
        var trash = new SqliteLibraryTrashActions(database);
        Assert.Equal(2, (await trash.ListAsync()).Count);
        Assert.Empty(await actions.ListSpacesAsync());
        await trash.RestoreAsync(LibraryEntryKind.Space, f.Space.Id);
        Assert.Null(await actions.LoadAsync(f.Page.Id));
        Assert.NotNull(await actions.LoadAsync(f.SecondPage.Id));
        await trash.RestoreAsync(LibraryEntryKind.Page, f.Page.Id);
        await f.AssertPageAsync(actions, f.Page.Id);
    }

    [Fact]
    public async Task Restore_PreTrashBackup_UpgradesItAndKeepsTheOldContentAccessible()
    {
        using var f = await Fixture.CreateAsync(previousSchema: true);
        await f.WriteHistoricalArchiveAsync();

        await new LocalBackupService(f.Source).RestoreAsync(f.Archive, f.Destination);

        await using var context = f.Context(f.Destination);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(await context.Set<LibraryTrashRecord>().ToArrayAsync());
        var actions = new SqliteLibraryActions(Path.Combine(f.Destination, "trama.db"), new LocalImageAttachmentStore(f.Destination));
        await f.AssertPageAsync(actions, f.Page.Id);
    }

    [Fact]
    public async Task Backup_RejectsInvalidRichTextEvenWhenThePageIsInTheTrash()
    {
        using var f = await Fixture.CreateAsync();
        await f.Trash.MoveToTrashAsync(LibraryEntryKind.Space, f.Space.Id);
        await using (var context = f.Context(f.Source))
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE NotebookPages SET FormatVersion = 1 WHERE Id = {f.Page.Id};");

        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Source).CreateAsync(f.Archive));

        Assert.False(File.Exists(f.Archive));
        Assert.Single(await f.Trash.ListAsync());
    }

    [Fact]
    public async Task Backup_RejectsATrashRootWhoseSourceEntityDoesNotExist()
    {
        using var f = await Fixture.CreateAsync();
        await using (var context = f.Context(f.Source))
        {
            context.Set<LibraryTrashRecord>().Add(new LibraryTrashRecord
            {
                Kind = LibraryEntryKind.Space,
                EntryId = Guid.NewGuid(),
                BatchId = Guid.NewGuid(),
                IsRoot = true,
                Title = "Origem ausente",
                DeletedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => new LocalBackupService(f.Source).CreateAsync(f.Archive));

        Assert.False(File.Exists(f.Archive));
        await f.AssertPageAsync(f.Actions, f.Page.Id);
    }

    [Fact]
    public async Task LegacyPage_TrashHidesSearchResultsAndRestoresTheOriginalIdentity()
    {
        using var f = await Fixture.CreateAsync();
        var legacy = new NoteSnapshot(Guid.NewGuid(), "Nota legada", "Conteúdo legado exclusivo");
        await using (var context = f.Context(f.Source))
            await new SqliteNoteRepository(context).AddAsync(legacy);
        await f.Trash.MoveToTrashAsync(LibraryEntryKind.Page, legacy.Id);

        Assert.Null(await f.Actions.LoadAsync(legacy.Id));
        Assert.DoesNotContain(await new SqliteKnowledgeDiscovery(f.Database).SearchAsync("Conteúdo legado exclusivo"), item => item.PageId == legacy.Id);
        await f.Trash.RestoreAsync(LibraryEntryKind.Page, legacy.Id);
        var loaded = await f.Actions.LoadAsync(legacy.Id);
        Assert.NotNull(loaded);
        Assert.Equal(legacy.Id, loaded.Id);
        Assert.Equal(legacy.Body, Assert.IsType<NotebookTextBlock>(Assert.Single(loaded.Blocks)).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledTrashOperation_PreservesThePreviousState(bool restoring)
    {
        using var f = await Fixture.CreateAsync();
        if (restoring) await f.Trash.MoveToTrashAsync(LibraryEntryKind.Space, f.Space.Id);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => restoring
            ? f.Trash.RestoreAsync(LibraryEntryKind.Space, f.Space.Id, cancelled.Token)
            : f.Trash.MoveToTrashAsync(LibraryEntryKind.Space, f.Space.Id, cancelled.Token));

        if (restoring)
        {
            Assert.Single(await f.Trash.ListAsync());
            Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
        }
        else
        {
            Assert.Empty(await f.Trash.ListAsync());
            await f.AssertPageAsync(f.Actions, f.Page.Id);
        }
    }

    [Fact]
    public async Task FailingCascade_RollsBackAllTombstonesAndKeepsTheHierarchyActive()
    {
        using var f = await Fixture.CreateAsync();
        await using (var context = f.Context(f.Source))
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectPageTrash BEFORE INSERT ON LibraryTrash WHEN NEW.Kind = 2 BEGIN SELECT RAISE(ABORT, 'Synthetic page marker failure'); END;");

        await Assert.ThrowsAsync<NotePersistenceException>(() => f.Trash.MoveToTrashAsync(LibraryEntryKind.Space, f.Space.Id));

        await using var reader = f.Context(f.Source);
        Assert.Empty(await reader.Set<LibraryTrashRecord>().ToArrayAsync());
        Assert.Contains(await f.Actions.ListSpacesAsync(), item => item.Id == f.Space.Id);
        Assert.Equal(2, (await f.Actions.ListPagesAsync(f.Book.Id)).Count);
        await f.AssertPageAsync(f.Actions, f.Page.Id);
    }

    [AvaloniaFact]
    public async Task PendingAutosave_AfterDeletionCannotReactivateOrOverwriteTheTrashedPage()
    {
        using var f = await Fixture.CreateAsync();
        using var clock = new ControlledClock();
        using var library = new LibraryViewModel(f.Actions, new NavigationPrompt(), new SqliteLibraryManagement(f.Database),
            new SqliteKnowledgeDiscovery(f.Database), clock, f.Trash);
        await library.InitializeAsync();
        Assert.True(await library.SelectSpaceAsync(f.Space.Id));
        Assert.True(await library.SelectNotebookAsync(f.Book.Id));
        Assert.True(await library.OpenPageAsync(f.Page.Id));
        library.Editor.Title = "Gravação pendente";
        await f.Trash.MoveToTrashAsync(LibraryEntryKind.Page, f.Page.Id);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Saved(object? sender, PropertyChangedEventArgs args)
        {
            if (library.Editor.LastSaveStatus is not null && !library.Editor.IsBusy) completion.TrySetResult();
        }
        library.Editor.PropertyChanged += Saved;
        try
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { library.Editor.PropertyChanged -= Saved; }

        Assert.Equal(NotebookSaveStatus.PersistenceFailed, library.Editor.LastSaveStatus);
        Assert.True(library.Editor.HasUnsavedChanges);
        Assert.Null(await f.Actions.LoadAsync(f.Page.Id));
        await f.Trash.RestoreAsync(LibraryEntryKind.Page, f.Page.Id);
        Assert.Equal(f.Page.Title, (await f.Actions.LoadAsync(f.Page.Id))!.Title);
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public string Source => Path.Combine(Root.DirectoryPath, "source");
        public string Destination => Path.Combine(Root.DirectoryPath, "restored");
        public string Archive => Path.Combine(Root.DirectoryPath, "copy.trama-backup");
        public string Database => Path.Combine(Source, "trama.db");
        public ILibraryActions Actions { get; }
        public ILibraryTrashActions Trash { get; }
        public KnowledgeSpace Space { get; private set; } = null!;
        public NotebookInfo Book { get; private set; } = null!;
        public NotebookSnapshot Page { get; private set; } = null!;
        public NotebookSnapshot SecondPage { get; private set; } = null!;
        public Guid ImageId { get; private set; }

        private Fixture()
        {
            Actions = new SqliteLibraryActions(Database, new LocalImageAttachmentStore(Source));
            Trash = new SqliteLibraryTrashActions(Database);
        }

        public TramaDbContext Context(string path) => new(new DbContextOptionsBuilder<TramaDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(path, "trama.db"), Pooling = false }.ToString()).Options);

        public static async Task<Fixture> CreateAsync(bool previousSchema = false)
        {
            var f = new Fixture();
            Directory.CreateDirectory(f.Source);
            if (previousSchema)
            {
                await using var context = f.Context(f.Source);
                await context.GetService<IMigrator>().MigrateAsync("20261029201153_InlineTextStyles");
            }
            else await LocalDatabase.InitializeAsync(f.Source);
            f.Space = await f.Actions.CreateSpaceAsync("Estudos");
            f.Book = await f.Actions.CreateNotebookAsync(f.Space.Id, "Curso C#");
            using var source = new MemoryStream(NotebookTestWorkspace.Png);
            var image = await f.Actions.ImportImageAsync(source);
            f.ImageId = image.Id;
            f.Page = new NotebookSnapshot(Guid.NewGuid(), "Página com atributos", [
                new NotebookTextBlock(Guid.NewGuid(), "Conteúdo") { Styles = [new TextStyleSpan(0, 8, InlineTextStyle.Bold) { FontSize = 24, Foreground = "teal", Highlight = "amber" }] },
                new NotebookImageBlock(Guid.NewGuid(), image.Id, "Imagem preservada")]);
            f.SecondPage = new NotebookSnapshot(Guid.NewGuid(), "Segunda página", [new NotebookTextBlock(Guid.NewGuid(), "Preservar")]);
            Assert.Equal(NotebookSaveStatus.Saved, (await f.Actions.SaveInNotebookAsync(f.Page, f.Book.Id)).Status);
            Assert.Equal(NotebookSaveStatus.Saved, (await f.Actions.SaveInNotebookAsync(f.SecondPage, f.Book.Id)).Status);
            await new SqliteKnowledgeDiscovery(f.Database).SetPageTagsAsync(f.Page.Id, ["estudo"]);
            return f;
        }

        public async Task AssertPageAsync(ILibraryActions actions, Guid id)
        {
            var page = await actions.LoadAsync(id);
            Assert.NotNull(page);
            Assert.Equal(Page.Title, page.Title);
            Assert.Equal(Assert.IsType<NotebookTextBlock>(Page.Blocks[0]).Styles, Assert.IsType<NotebookTextBlock>(page.Blocks[0]).Styles);
            Assert.Equal(ImageId, Assert.IsType<NotebookImageBlock>(page.Blocks[1]).AttachmentId);
            using var image = await actions.OpenImageAsync(ImageId);
            Assert.NotNull(image);
            using var bytes = new MemoryStream(); await image.CopyToAsync(bytes);
            Assert.Equal(NotebookTestWorkspace.Png, bytes.ToArray());
        }

        public async Task WriteHistoricalArchiveAsync()
        {
            var payload = new Dictionary<string, byte[]>
            {
                ["trama.db"] = await File.ReadAllBytesAsync(Database),
                [$"attachments/{ImageId:N}.image"] = await File.ReadAllBytesAsync(Path.Combine(Source, "attachments", $"{ImageId:N}.image"))
            };
            var files = new List<BackupFileEntry>();
            using var archive = ZipFile.Open(Archive, ZipArchiveMode.Create);
            foreach (var (name, bytes) in payload)
            {
                await using var output = archive.CreateEntry(name).Open();
                await output.WriteAsync(bytes);
                files.Add(new(name, bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()));
            }
            await using var manifest = archive.CreateEntry("manifest.json").Open();
            await JsonSerializer.SerializeAsync(manifest, new BackupManifest(1, files), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        public void Dispose() => Root.Dispose();
    }

    private sealed class ControlledClock : TimeProvider, IDisposable
    {
        private readonly List<ControlledTimer> _timers = [];
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(_ticks);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ControlledTimer(this, callback, state);
            _timers.Add(timer); timer.Change(dueTime, period);
            return timer;
        }
        public void Advance(TimeSpan elapsed)
        {
            _ticks += elapsed.Ticks;
            foreach (var timer in _timers.Where(item => item.Due <= _ticks).ToArray())
            {
                timer.Due = long.MaxValue;
                timer.Callback(timer.State);
            }
        }
        public void Dispose() { foreach (var timer in _timers) timer.Dispose(); }
        private sealed class ControlledTimer(ControlledClock clock, TimerCallback callback, object? state) : ITimer
        {
            private bool _disposed;
            public TimerCallback Callback { get; } = callback;
            public object? State { get; } = state;
            public long Due { get; set; } = long.MaxValue;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (_disposed) return false;
                Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._ticks + dueTime.Ticks;
                return true;
            }
            public void Dispose() { _disposed = true; Due = long.MaxValue; }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
