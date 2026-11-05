using Trama.Application.Agenda;

namespace Trama.UnitTests.Agenda;

public sealed class SaveAgendaTaskTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n ")]
    public async Task MissingTitle_IsRejectedWithoutWriting(string? title)
    {
        var repository = new RecordingRepository();
        var result = await new SaveAgendaTask(repository).ExecuteAsync(Example() with { Title = title! });
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task EmptyIdentity_IsRejectedWithoutWriting()
    {
        var repository = new RecordingRepository();
        var result = await new SaveAgendaTask(repository).ExecuteAsync(Example() with { Id = Guid.Empty });
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Theory]
    [InlineData(null, 19)]
    [InlineData(18, 18)]
    [InlineData(19, 18)]
    public async Task IncompleteOrNonPositiveTimeRange_IsRejectedWithoutWriting(int? start, int? end)
    {
        var repository = new RecordingRepository();
        var task = Example() with
        {
            StartTime = start.HasValue ? new TimeOnly(start.Value, 0) : null,
            EndTime = end.HasValue ? new TimeOnly(end.Value, 0) : null
        };
        var result = await new SaveAgendaTask(repository).ExecuteAsync(task);
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task UnknownCategory_IsRejectedWithoutWriting()
    {
        var repository = new RecordingRepository();
        var result = await new SaveAgendaTask(repository).ExecuteAsync(Example() with { Category = (AgendaTaskCategory)999 });
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task UnknownStatus_IsRejectedWithoutWriting()
    {
        var repository = new RecordingRepository();
        var result = await new SaveAgendaTask(repository).ExecuteAsync(Example() with { Status = (AgendaTaskStatus)999 });
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Theory]
    [InlineData(AgendaTaskCategory.Personal, AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskCategory.Work, AgendaTaskStatus.InProgress)]
    [InlineData(AgendaTaskCategory.Study, AgendaTaskStatus.Completed)]
    public async Task ValidTask_PreservesIdentityContentDateCategoryAndStatus(AgendaTaskCategory category, AgendaTaskStatus status)
    {
        var repository = new RecordingRepository();
        var task = Example() with { Title = "  Estudar C# 🧵  ", Description = "Primeira linha\n    código e #tags sem modificação\t", Category = category, Status = status };
        var result = await new SaveAgendaTask(repository).ExecuteAsync(task);
        Assert.Equal(AgendaTaskSaveStatus.Saved, result.Status);
        Assert.Same(task, Assert.Single(repository.Saved));
    }

    [Fact]
    public async Task DateWithoutTime_IsValidAndDoesNotInventAnAppointment()
    {
        var repository = new RecordingRepository();
        var task = Example() with { StartTime = null, EndTime = null };
        var result = await new SaveAgendaTask(repository).ExecuteAsync(task);
        Assert.Equal(AgendaTaskSaveStatus.Saved, result.Status);
        var saved = Assert.Single(repository.Saved);
        Assert.Null(saved.StartTime); Assert.Null(saved.EndTime);
        Assert.Equal(task.PlannedDate, saved.PlannedDate);
    }

    [Fact]
    public async Task PastDate_IsAllowedWithoutMovingItToToday()
    {
        var repository = new RecordingRepository();
        var task = Example() with { PlannedDate = new DateOnly(2020, 1, 2) };
        var result = await new SaveAgendaTask(repository).ExecuteAsync(task);
        Assert.Equal(AgendaTaskSaveStatus.Saved, result.Status);
        Assert.Equal(task, Assert.Single(repository.Saved));
    }

    [Fact]
    public async Task Rescheduling_PreservesTheTaskIdentityAndOtherFields()
    {
        var repository = new RecordingRepository(); var save = new SaveAgendaTask(repository);
        var original = Example();
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await save.ExecuteAsync(original)).Status);
        var moved = original with { PlannedDate = original.PlannedDate.AddDays(7), StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(21, 0) };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await save.ExecuteAsync(moved)).Status);
        Assert.Equal(2, repository.Saved.Count);
        Assert.Equal(original.Id, repository.Saved[1].Id);
        Assert.Equal(moved, repository.Saved[1]);
        Assert.Equal(original, repository.Saved[0]);
    }

    [Fact]
    public async Task CompletingAndReopening_PreserveTheOriginalSchedule()
    {
        var repository = new RecordingRepository(); var save = new SaveAgendaTask(repository);
        var original = Example();
        var completed = original with { Status = AgendaTaskStatus.Completed };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await save.ExecuteAsync(completed)).Status);
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await save.ExecuteAsync(completed with { Status = AgendaTaskStatus.Pending })).Status);
        Assert.Equal(completed, repository.Saved[0]); Assert.Equal(original, repository.Saved[1]);
    }

    [Fact]
    public async Task ExpectedPersistenceFailure_DoesNotReportSuccess()
    {
        var repository = new RecordingRepository { Failure = new AgendaPersistenceException("Synthetic failure.") };
        var result = await new SaveAgendaTask(repository).ExecuteAsync(Example());
        Assert.Equal(AgendaTaskSaveStatus.PersistenceFailed, result.Status);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task PreCancellation_DoesNotReachTheRepository()
    {
        var repository = new RecordingRepository(); using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SaveAgendaTask(repository).ExecuteAsync(Example(), source.Token));
        Assert.Equal(0, repository.Calls); Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task CancellationDuringWrite_IsPropagatedAndDoesNotReportSuccess()
    {
        using var source = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new RecordingRepository { OnWrite = async token => { entered.SetResult(); await Task.Delay(Timeout.Infinite, token); } };
        var operation = new SaveAgendaTask(repository).ExecuteAsync(Example(), source.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); source.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Equal(source.Token, repository.LastToken); Assert.Empty(repository.Saved);
        }
        finally { source.Cancel(); }
    }

    [Fact]
    public async Task Save_AwaitsTheRepositoryBeforeReportingSuccess()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new RecordingRepository { OnWrite = _ => { entered.SetResult(); return gate.Task; } };
        var operation = new SaveAgendaTask(repository).ExecuteAsync(Example());
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(operation.IsCompleted); Assert.Empty(repository.Saved);
            gate.SetResult();
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await operation).Status);
            Assert.Single(repository.Saved);
        }
        finally { gate.TrySetResult(); }
    }

    [Fact]
    public async Task UnexpectedFailure_IsNotDisguisedAsAValidationError()
    {
        var failure = new InvalidOperationException("Synthetic defect.");
        var repository = new RecordingRepository { Failure = failure };
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => new SaveAgendaTask(repository).ExecuteAsync(Example())));
    }

    [Fact]
    public async Task NullSnapshot_IsRejectedBeforeWriting()
    {
        var repository = new RecordingRepository();
        await Assert.ThrowsAsync<ArgumentNullException>(() => new SaveAgendaTask(repository).ExecuteAsync(null!));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public void NullRepository_IsRejectedAtConstruction()
        => Assert.Throws<ArgumentNullException>(() => new SaveAgendaTask(null!));

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Estudar interfaces", "Revisar exemplos de C#.",
        new DateOnly(2026, 11, 2), new TimeOnly(18, 0), new TimeOnly(19, 0), AgendaTaskCategory.Study);

    private sealed class RecordingRepository : IAgendaTaskRepository
    {
        public List<AgendaTaskSnapshot> Saved { get; } = [];
        public Exception? Failure { get; init; }
        public Func<CancellationToken, Task>? OnWrite { get; init; }
        public int Calls { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public async Task SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default)
        {
            Calls++; LastToken = cancellationToken;
            if (OnWrite is not null) await OnWrite(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            Saved.Add(task);
        }
    }
}
