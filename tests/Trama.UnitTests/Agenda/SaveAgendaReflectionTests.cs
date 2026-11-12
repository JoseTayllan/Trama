using System.Reflection;
using System.Runtime.ExceptionServices;
using Trama.Application.Agenda;

namespace Trama.UnitTests.Agenda;

public sealed class SaveAgendaReflectionTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Save_PreservesBothTextsAndUsesTheInjectedUtcTimestamp()
    {
        var repository = new Repository(); var clock = new Clock();
        var worked = "Avancei no C#\nMantive o foco. ✓"; var adjust = "  Reservar mais tempo\r\nRevisar referências 📚  ";
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await Execute(Create(repository, clock), Monday, worked, adjust)).Status);
        var saved = Assert.Single(repository.Stored).Value;
        Assert.Equal(Monday, saved.WeekStart); Assert.Equal(worked, saved.WorkedWell); Assert.Equal(adjust, saved.Adjustments);
        Assert.Equal(Now, saved.UpdatedAt); Assert.Equal(TimeSpan.Zero, saved.UpdatedAt.Offset); Assert.Equal(1, clock.Reads);
        Assert.Equal(new[] { Monday }, repository.Queries);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "Ajustar prioridades")]
    [InlineData("Funcionou bem", null)]
    public async Task OptionalNullFields_AreStoredAsEmptyStrings(string? worked, string? adjust)
    {
        var repository = new Repository(); var result = await Execute(Create(repository, new Clock()), Monday, worked, adjust);
        Assert.Equal(AgendaReflectionSaveStatus.Saved, result.Status); var saved = Assert.Single(repository.Stored).Value;
        Assert.Equal(worked ?? string.Empty, saved.WorkedWell); Assert.Equal(adjust ?? string.Empty, saved.Adjustments);
    }

    [Fact]
    public async Task ClearingBothFields_UpdatesTheSameWeekInsteadOfLeavingOldText()
    {
        var repository = new Repository(); repository.Stored[Monday] = new(Monday, "Texto anterior", "Ajuste anterior", Now.AddDays(-1));
        var result = await Execute(Create(repository, new Clock()), Monday, string.Empty, string.Empty);
        Assert.Equal(AgendaReflectionSaveStatus.Saved, result.Status); var saved = Assert.Single(repository.Stored).Value;
        Assert.Empty(saved.WorkedWell); Assert.Empty(saved.Adjustments); Assert.Equal(1, repository.Writes);
    }

    [Fact]
    public async Task SavingAgain_ReplacesOnlyThatWeeksReflection()
    {
        var repository = new Repository(); var clock = new Clock(); var useCase = Create(repository, clock);
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await Execute(useCase, Monday, "Primeiro", "Primeiro ajuste")).Status);
        clock.UtcNow = Now.AddHours(1);
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await Execute(useCase, Monday, "Revisado", "Outro ajuste")).Status);
        var saved = Assert.Single(repository.Stored).Value; Assert.Equal("Revisado", saved.WorkedWell); Assert.Equal("Outro ajuste", saved.Adjustments);
        Assert.Equal(clock.UtcNow, saved.UpdatedAt); Assert.Equal(2, repository.Writes);
    }

    [Fact]
    public async Task DifferentWeeks_RemainIndependent()
    {
        var repository = new Repository(); var useCase = Create(repository, new Clock());
        await Execute(useCase, Monday, "Semana A", "Ajuste A"); await Execute(useCase, Monday.AddDays(-7), "Semana B", "Ajuste B");
        Assert.Equal(2, repository.Stored.Count); Assert.Equal("Semana A", repository.Stored[Monday].WorkedWell);
        Assert.Equal("Semana B", repository.Stored[Monday.AddDays(-7)].WorkedWell);
    }

    [Fact]
    public async Task LongTextAndWhitespace_AreNotTruncatedOrNormalized()
    {
        var repository = new Repository(); var text = " \n" + new string('á', 20_000) + "\t  ";
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await Execute(Create(repository, new Clock()), Monday, text, "   ")).Status);
        Assert.Equal(text, repository.Stored[Monday].WorkedWell); Assert.Equal("   ", repository.Stored[Monday].Adjustments);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task NonMondayKey_IsRejectedWithoutQueryOrClockRead(int offset)
    {
        var repository = new Repository(); var clock = new Clock { ThrowOnRead = true };
        var result = await Execute(Create(repository, clock), Monday.AddDays(offset), "Texto", "Ajuste");
        Assert.Equal(AgendaReflectionSaveStatus.ValidationFailed, result.Status); Assert.Empty(repository.Queries); Assert.Equal(0, repository.Writes);
    }

    [Fact]
    public async Task MissingClosure_IsReportedWithoutCreatingOrSavingAnything()
    {
        var repository = new Repository { HasClosure = false }; var clock = new Clock { ThrowOnRead = true };
        var result = await Execute(Create(repository, clock), Monday, "Texto", "Ajuste");
        Assert.Equal(AgendaReflectionSaveStatus.ClosureMissing, result.Status); Assert.Equal(new[] { Monday }, repository.Queries);
        Assert.Empty(repository.Stored); Assert.Equal(0, repository.Writes);
    }

    [Fact]
    public async Task FirstRepresentableMonday_DoesNotRequireLookingBackwards()
    {
        var repository = new Repository(); var result = await Execute(Create(repository, new Clock()), DateOnly.MinValue, "Texto", "");
        Assert.Equal(AgendaReflectionSaveStatus.Saved, result.Status); Assert.Equal(DateOnly.MinValue, Assert.Single(repository.Stored).Key);
    }

    [Fact]
    public async Task CancellationBeforeExecution_DoesNotReadRepositoryOrClock()
    {
        var repository = new Repository(); var clock = new Clock { ThrowOnRead = true }; var useCase = Create(repository, clock);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(useCase, Monday, "Texto", "Ajuste", cts.Token));
        Assert.Empty(repository.Queries); Assert.Equal(0, repository.Writes);
    }

    [Fact]
    public async Task CancellationDuringClosureRead_IsPropagatedAndPreventsWrite()
    {
        var repository = new Repository(); using var cts = new CancellationTokenSource(); repository.AfterQuery = cts.Cancel;
        var useCase = Create(repository, new Clock { ThrowOnRead = true });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(useCase, Monday, "Texto", "Ajuste", cts.Token));
        Assert.Equal(0, repository.Writes);
    }

    [Fact]
    public async Task CallerToken_IsPassedToTheClosureReadAndWrite()
    {
        var repository = new Repository(); using var cts = new CancellationTokenSource();
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await Execute(Create(repository, new Clock()), Monday, "Texto", "Ajuste", cts.Token)).Status);
        Assert.Equal(cts.Token, repository.QueryToken); Assert.Equal(cts.Token, repository.SaveToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistenceFailure_IsReportedAndAllowsRetry(bool duringWrite)
    {
        var repository = new Repository { FailQuery = !duringWrite, FailSave = duringWrite }; var useCase = Create(repository, new Clock());
        Assert.Equal(AgendaReflectionSaveStatus.PersistenceFailed, (await Execute(useCase, Monday, "Texto", "Ajuste")).Status);
        Assert.Empty(repository.Stored); repository.FailQuery = false; repository.FailSave = false;
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await Execute(useCase, Monday, "Texto", "Ajuste")).Status);
    }

    [Fact]
    public async Task CancellationFromWrite_IsNotReportedAsPersistenceFailure()
    {
        var repository = new Repository { CancelSave = true }; var useCase = Create(repository, new Clock());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(useCase, Monday, "Texto", "Ajuste")); Assert.Empty(repository.Stored);
    }

    [Fact]
    public async Task UnexpectedProgrammingError_IsNotHiddenAsAStorageFailure()
    {
        var repository = new Repository { UnexpectedFailure = true }; var useCase = Create(repository, new Clock());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(useCase, Monday, "Texto", "Ajuste")); Assert.Empty(repository.Stored);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RequiredDependencies_AreRejectedWhenNull(int index)
    {
        object?[] args = [new Repository(), new Clock()]; args[index] = null;
        Assert.Throws<ArgumentNullException>(() => Create(args));
    }

    private static object Create(params object?[] args)
    {
        var type = typeof(IAgendaReflectionRepository).Assembly.GetType("Trama.Application.Agenda.SaveAgendaReflection"); Assert.NotNull(type);
        try { var value = Activator.CreateInstance(type, args); Assert.NotNull(value); return value; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static async Task<AgendaReflectionSaveResult> Execute(object useCase, DateOnly week, string? worked, string? adjust, CancellationToken token = default)
    {
        var method = useCase.GetType().GetMethod("ExecuteAsync"); Assert.NotNull(method);
        try { return await Assert.IsAssignableFrom<Task<AgendaReflectionSaveResult>>(method.Invoke(useCase, [week, worked, adjust, token])); }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private sealed class Clock : TimeProvider
    {
        public int Reads { get; private set; }
        public bool ThrowOnRead { get; set; }
        public DateTimeOffset UtcNow { get; set; } = Now;
        public override DateTimeOffset GetUtcNow() { Reads++; return ThrowOnRead ? throw new InvalidOperationException("Clock must not be read.") : UtcNow; }
    }
    private sealed class Repository : IAgendaReflectionRepository
    {
        public Dictionary<DateOnly, AgendaWeekReflection> Stored { get; } = [];
        public List<DateOnly> Queries { get; } = [];
        public int Writes { get; private set; }
        public bool HasClosure { get; set; } = true;
        public bool FailQuery { get; set; }
        public bool FailSave { get; set; }
        public bool CancelSave { get; set; }
        public bool UnexpectedFailure { get; set; }
        public Action? AfterQuery { get; set; }
        public CancellationToken QueryToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Task<bool> HasClosureAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Queries.Add(weekStart); QueryToken = cancellationToken;
            if (FailQuery) throw new AgendaPersistenceException("Synthetic read error"); AfterQuery?.Invoke(); return Task.FromResult(HasClosure);
        }
        public Task SaveAsync(AgendaWeekReflection reflection, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Writes++; SaveToken = cancellationToken;
            if (CancelSave) throw new OperationCanceledException(); if (FailSave) throw new AgendaPersistenceException("Synthetic write error");
            if (UnexpectedFailure) throw new InvalidOperationException("Synthetic programming error"); Stored[reflection.WeekStart] = reflection; return Task.CompletedTask;
        }
    }
}
