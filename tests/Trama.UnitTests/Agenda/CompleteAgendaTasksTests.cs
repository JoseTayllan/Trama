using Trama.Application.Agenda;

namespace Trama.UnitTests.Agenda;

public sealed class CompleteAgendaTasksTests
{
    private static readonly DateTimeOffset ClosedAt = new(2026, 11, 4, 0, 30, 0, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    public async Task SelectedOpenTasks_UseOneBatchAndTheActualClosureTimestamp(AgendaTaskStatus status)
    {
        var repository = new Repository();
        var yesterday = Example() with { PlannedDate = new DateOnly(2026, 11, 3), StartTime = new TimeOnly(23, 0), EndTime = null, Status = status };
        var today = Example() with { Status = status, StartTime = null, EndTime = null };
        using var cancellation = new CancellationTokenSource();
        var result = await new CompleteAgendaTasks(repository).ExecuteAsync([yesterday, today], ClosedAt, cancellation.Token);
        Assert.Equal(AgendaTaskCompletionStatus.Completed, result.Status);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(new[] { yesterday.Id, today.Id }, repository.Ids);
        Assert.Equal(ClosedAt, repository.CompletedAt);
        Assert.Equal(ClosedAt.Offset, repository.CompletedAt.Offset);
        Assert.Equal(cancellation.Token, repository.Token);
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("future")]
    [InlineData("empty-id")]
    [InlineData("unknown-status")]
    [InlineData("unknown-category")]
    public async Task InvalidSelection_IsRejectedBeforeAnyWrite(string scenario)
    {
        var repository = new Repository(); var invalid = Example();
        invalid = scenario switch
        {
            "completed" => invalid with { Status = AgendaTaskStatus.Completed },
            "future" => invalid with { PlannedDate = new DateOnly(2026, 11, 5) },
            "empty-id" => invalid with { Id = Guid.Empty },
            "unknown-status" => invalid with { Status = (AgendaTaskStatus)99 },
            _ => invalid with { Category = (AgendaTaskCategory)99 }
        };
        var result = await new CompleteAgendaTasks(repository).ExecuteAsync([Example(), invalid], ClosedAt);
        Assert.Equal(AgendaTaskCompletionStatus.ValidationFailed, result.Status);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task EmptySelection_IsRejectedWithoutWriting()
    {
        var repository = new Repository();
        var result = await new CompleteAgendaTasks(repository).ExecuteAsync([], ClosedAt);
        Assert.Equal(AgendaTaskCompletionStatus.ValidationFailed, result.Status);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task DuplicateIdentity_IsRejectedWithoutWriting()
    {
        var repository = new Repository(); var task = Example();
        var result = await new CompleteAgendaTasks(repository).ExecuteAsync([task, task], ClosedAt);
        Assert.Equal(AgendaTaskCompletionStatus.ValidationFailed, result.Status);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Eligibility_UsesLocalClosureDayInsteadOfTheUtcDate()
    {
        var repository = new Repository();
        var localClosure = new DateTimeOffset(2026, 11, 4, 0, 30, 0, TimeSpan.FromHours(14));
        var result = await new CompleteAgendaTasks(repository).ExecuteAsync([Example()], localClosure);
        Assert.Equal(AgendaTaskCompletionStatus.Completed, result.Status);
        Assert.Equal(localClosure, repository.CompletedAt);
        Assert.Equal(localClosure.Offset, repository.CompletedAt.Offset);
    }

    [Fact]
    public async Task ExpectedPersistenceFailure_IsReportedAsFailure()
    {
        var repository = new Repository { Failure = new AgendaPersistenceException("Synthetic failure.") };
        var result = await new CompleteAgendaTasks(repository).ExecuteAsync([Example()], ClosedAt);
        Assert.Equal(AgendaTaskCompletionStatus.PersistenceFailed, result.Status);
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task CancellationBeforeStarting_DoesNotWrite()
    {
        var repository = new Repository(); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CompleteAgendaTasks(repository).ExecuteAsync([Example()], ClosedAt, cancellation.Token));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task CancellationDuringWriting_IsNotReportedAsCompletion()
    {
        var repository = new Repository { Failure = new OperationCanceledException() };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CompleteAgendaTasks(repository).ExecuteAsync([Example()], ClosedAt));
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task UnexpectedFailure_IsNotConvertedIntoSuccess()
    {
        var error = new InvalidOperationException("Synthetic failure."); var repository = new Repository { Failure = error };
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => new CompleteAgendaTasks(repository).ExecuteAsync([Example()], ClosedAt));
        Assert.Same(error, thrown);
    }

    [Fact]
    public async Task Completion_IsOnlyReportedAfterTheRepositoryFinishes()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Repository { Gate = gate.Task };
        var operation = new CompleteAgendaTasks(repository).ExecuteAsync([Example()], ClosedAt);
        try
        {
            await repository.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(operation.IsCompleted);
            gate.SetResult();
            Assert.Equal(AgendaTaskCompletionStatus.Completed, (await operation).Status);
        }
        finally { gate.TrySetResult(); }
    }

    [Fact]
    public void MissingRepository_IsRejected()
        => Assert.Throws<ArgumentNullException>(() => new CompleteAgendaTasks(null!));

    [Fact]
    public async Task MissingSelection_IsRejectedWithoutWriting()
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<ArgumentNullException>(() => new CompleteAgendaTasks(repository).ExecuteAsync(null!, ClosedAt));
        Assert.Equal(0, repository.Calls);
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Sem dados pessoais", new DateOnly(2026, 11, 4));

    private sealed class Repository : IAgendaTaskCompletionRepository
    {
        public int Calls { get; private set; }
        public IReadOnlyList<Guid> Ids { get; private set; } = [];
        public DateTimeOffset CompletedAt { get; private set; }
        public CancellationToken Token { get; private set; }
        public Exception? Failure { get; init; }
        public Task? Gate { get; init; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task CompleteAsync(IReadOnlyList<Guid> taskIds, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
        {
            Calls++; Ids = taskIds.ToArray(); CompletedAt = completedAt; Token = cancellationToken; Entered.TrySetResult();
            if (Gate is { } gate) await gate.WaitAsync(cancellationToken);
            if (Failure is { } error) throw error;
        }
    }
}
