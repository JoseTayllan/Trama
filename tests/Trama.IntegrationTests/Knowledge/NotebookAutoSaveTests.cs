using System.ComponentModel;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class NotebookAutoSaveTests
{
    [Fact]
    public void EmptyUntouchedPage_DoesNotWriteAutomatically()
    {
        using var clock = new ManualClock();
        var actions = new AutoSaveActions();
        var editor = new NotebookEditorViewModel(actions, clock);

        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Empty(actions.Saved);
        Assert.False(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditedPage_IsSavedAfterTwoSecondsWithoutChangingItsIdentity()
    {
        using var clock = new ManualClock();
        var actions = new AutoSaveActions();
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Estudo automático" };
        editor.Blocks[0].Text = "Não perder esta anotação.";
        var id = editor.PageId;

        clock.Advance(TimeSpan.FromMilliseconds(1999));
        Assert.Empty(actions.Saved);
        clock.Advance(TimeSpan.FromMilliseconds(1));

        var saved = await actions.WaitForSaveAsync(1);
        await WaitForStateAsync(editor, () => !editor.IsBusy && !editor.HasUnsavedChanges);
        Assert.Equal(id, saved.Id);
        Assert.Equal("Estudo automático", saved.Title);
        Assert.Equal("Não perder esta anotação.", Assert.IsType<NotebookTextBlock>(Assert.Single(saved.Blocks)).Text);
        Assert.Equal(NotebookSaveStatus.Saved, editor.LastSaveStatus);
    }

    [Fact]
    public async Task TypingAgain_RestartsThePauseAndSavesOnlyTheLatestContent()
    {
        using var clock = new ManualClock();
        var actions = new AutoSaveActions();
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Primeira versão" };
        clock.Advance(TimeSpan.FromMilliseconds(1500));
        editor.Title = "Versão mais recente";
        clock.Advance(TimeSpan.FromMilliseconds(1500));
        Assert.Empty(actions.Saved);

        clock.Advance(TimeSpan.FromMilliseconds(500));
        var saved = await actions.WaitForSaveAsync(1);
        await WaitForStateAsync(editor, () => !editor.IsBusy && !editor.HasUnsavedChanges);
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Single(actions.Saved);
        Assert.Equal("Versão mais recente", saved.Title);
    }

    [Fact]
    public async Task AutoSave_PreservesFormattedTextCodeAndImageReferencesInOrder()
    {
        using var clock = new ManualClock();
        var actions = new AutoSaveActions();
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Página completa" };
        editor.Blocks[0].Text = "Texto com estilo";
        Assert.True(editor.FormatTextSelection(editor.Blocks[0].Id, 0, 5, InlineTextStyle.Bold));
        editor.AddCodeCommand.Execute(null);
        editor.Blocks[1].Text = "print(42)";
        editor.Blocks[1].Language = "python";
        using (var source = new MemoryStream([1])) await editor.ImportImageAsync(source);
        editor.Blocks[2].Text = "Legenda do exemplo";

        clock.Advance(TimeSpan.FromSeconds(2));
        var saved = await actions.WaitForSaveAsync(1);

        Assert.Equal(3, saved.Blocks.Count);
        var text = Assert.IsType<NotebookTextBlock>(saved.Blocks[0]);
        Assert.Equal("Texto com estilo", text.Text);
        Assert.Equal(new TextStyleSpan(0, 5, InlineTextStyle.Bold), Assert.Single(text.Styles));
        var code = Assert.IsType<NotebookCodeBlock>(saved.Blocks[1]);
        Assert.Equal("print(42)", code.Code);
        Assert.Equal("python", code.Language);
        var image = Assert.IsType<NotebookImageBlock>(saved.Blocks[2]);
        Assert.Equal(actions.Attachment.Id, image.AttachmentId);
        Assert.Equal("Legenda do exemplo", image.Caption);
    }

    [Fact]
    public async Task ManualSaveBeforeThePause_DoesNotProduceADuplicateAutomaticWrite()
    {
        using var clock = new ManualClock();
        var actions = new AutoSaveActions();
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Salvar agora" };

        await editor.SaveCommand.ExecuteAsync(null);
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Single(actions.Saved);
        Assert.False(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditingDuringAnAutomaticWrite_PreservesTheNewRevisionWithoutConcurrentWrites()
    {
        using var clock = new ManualClock();
        var pending = new TaskCompletionSource<NotebookSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actions = new AutoSaveActions { FirstSave = pending.Task };
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Snapshot anterior" };
        try
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            var first = await actions.WaitForSaveAsync(1);
            editor.Title = "Edição durante a gravação";
            clock.Advance(TimeSpan.FromSeconds(2));
            Assert.Single(actions.Saved);
            Assert.True(editor.HasUnsavedChanges);
            Assert.Equal("Snapshot anterior", first.Title);

            var idle = WaitForStateAsync(editor, () => !editor.IsBusy);
            pending.TrySetResult(new(NotebookSaveStatus.Saved));
            await idle;
            clock.Advance(TimeSpan.FromSeconds(2));
            var second = await actions.WaitForSaveAsync(2);
            await WaitForStateAsync(editor, () => !editor.IsBusy && !editor.HasUnsavedChanges);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal("Edição durante a gravação", second.Title);
            Assert.Equal(1, actions.MaximumConcurrentWrites);
        }
        finally { pending.TrySetResult(new(NotebookSaveStatus.Saved)); }
    }

    [Fact]
    public async Task FailedAutomaticWrite_KeepsTheDraftAndRetriesAfterTheNextEditWithoutALoop()
    {
        using var clock = new ManualClock();
        var actions = new AutoSaveActions { Result = new(NotebookSaveStatus.PersistenceFailed) };
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Preservar conteúdo" };

        clock.Advance(TimeSpan.FromSeconds(2));
        await actions.WaitForSaveAsync(1);
        await WaitForStateAsync(editor, () => !editor.IsBusy && editor.LastSaveStatus == NotebookSaveStatus.PersistenceFailed);
        Assert.True(editor.HasUnsavedChanges);
        Assert.Equal("Preservar conteúdo", editor.Title);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Single(actions.Saved);

        actions.Result = new(NotebookSaveStatus.Saved);
        editor.Title = "Conteúdo revisado";
        clock.Advance(TimeSpan.FromSeconds(2));
        await actions.WaitForSaveAsync(2);
        await WaitForStateAsync(editor, () => !editor.IsBusy && !editor.HasUnsavedChanges);
        Assert.Equal("Conteúdo revisado", actions.Saved[1].Title);
    }

    [Fact]
    public async Task LoadingAnotherPage_CancelsTheOldScheduledWriteAndDoesNotSaveTheLoadedPage()
    {
        using var clock = new ManualClock();
        var loaded = new NotebookSnapshot(Guid.NewGuid(), "Página existente", [new NotebookTextBlock(Guid.NewGuid(), "Conteúdo salvo")]);
        var actions = new AutoSaveActions { Loaded = loaded };
        var editor = new NotebookEditorViewModel(actions, clock) { Title = "Edição anterior" };

        Assert.True(await editor.TryLoadAsync(loaded.Id));
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Empty(actions.Saved);
        Assert.Equal(loaded.Id, editor.PageId);
        Assert.False(editor.HasUnsavedChanges);
    }

    private static async Task WaitForStateAsync(NotebookEditorViewModel editor, Func<bool> predicate)
    {
        if (predicate()) return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed(object? sender, PropertyChangedEventArgs args) { if (predicate()) completion.TrySetResult(); }
        editor.PropertyChanged += Changed;
        try
        {
            if (predicate()) return;
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally { editor.PropertyChanged -= Changed; }
    }

    private sealed class AutoSaveActions : INotebookActions
    {
        private readonly TaskCompletionSource<NotebookSnapshot>[] _observations = Enumerable.Range(0, 8)
            .Select(_ => new TaskCompletionSource<NotebookSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        private int _activeWrites;
        public List<NotebookSnapshot> Saved { get; } = [];
        public NotebookSaveResult Result { get; set; } = new(NotebookSaveStatus.Saved);
        public Task<NotebookSaveResult>? FirstSave { get; init; }
        public NotebookSnapshot? Loaded { get; init; }
        public ImageAttachment Attachment { get; } = new(Guid.NewGuid(), "image/png");
        public int MaximumConcurrentWrites { get; private set; }

        public async Task<NotebookSaveResult> SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(ref _activeWrites);
            MaximumConcurrentWrites = Math.Max(MaximumConcurrentWrites, active);
            var index = Saved.Count;
            Saved.Add(notebook);
            _observations[index].TrySetResult(notebook);
            try { return index == 0 && FirstSave is not null ? await FirstSave : Result; }
            finally { Interlocked.Decrement(ref _activeWrites); }
        }

        public async Task<NotebookSnapshot> WaitForSaveAsync(int count)
        {
            try { return await _observations[count - 1].Task.WaitAsync(TimeSpan.FromSeconds(3)); }
            catch (TimeoutException)
            {
                throw new Xunit.Sdk.XunitException($"Automatic save #{count} was not requested after advancing the test clock; no persistence call was observed.");
            }
        }
        public Task<NotebookSnapshot?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Loaded);
        public Task<ImageAttachment> ImportImageAsync(Stream source, CancellationToken cancellationToken = default) => Task.FromResult(Attachment);
        public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }

    private sealed class ManualClock : TimeProvider, IDisposable
    {
        private readonly object _gate = new();
        private readonly List<ManualTimer> _timers = [];
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(_ticks);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            lock (_gate) _timers.Add(timer);
            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan elapsed)
        {
            lock (_gate) _ticks += elapsed.Ticks;
            for (var iteration = 0; iteration < 1000; iteration++)
            {
                ManualTimer? timer;
                lock (_gate)
                {
                    timer = _timers.FirstOrDefault(item => !item.Disposed && item.Due <= _ticks);
                    if (timer is null) return;
                    timer.Due = timer.Period > 0 ? _ticks + timer.Period : long.MaxValue;
                }
                timer.Callback(timer.State);
            }
            throw new InvalidOperationException("Timer callback loop exceeded the test budget.");
        }

        public void Dispose() { lock (_gate) foreach (var timer in _timers) timer.Dispose(); }

        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            public TimerCallback Callback { get; } = callback;
            public object? State { get; } = state;
            public long Due { get; set; } = long.MaxValue;
            public long Period { get; private set; }
            public bool Disposed { get; private set; }
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (clock._gate)
                {
                    if (Disposed) return false;
                    Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._ticks + dueTime.Ticks;
                    Period = period.Ticks;
                    return true;
                }
            }
            public void Dispose() { lock (clock._gate) Disposed = true; }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
