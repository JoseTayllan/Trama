using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaReflectionServiceTests
{
    private static readonly DateOnly Week = new(2026, 11, 2);
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAndReadAcrossInstances_UsesTheInjectedClockAndPreservesText()
    {
        using var root = new NotebookTestWorkspace();
        var database = await InitializeAsync(root);
        var actions = Create(database);
        const string text = "  Avancei no C#\nMantive o foco. ✓  ";
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await actions.SaveAsync(Week, text, null)).Status);
        Assert.Equal(new AgendaWeekReflection(Week, text, "", Now), await Create(database).GetAsync(Week));
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await actions.SaveAsync(Week, "", "Novo ajuste")).Status);
        Assert.Equal(new AgendaWeekReflection(Week, "", "Novo ajuste", Now), await Create(database).GetAsync(Week));
    }

    [Fact]
    public async Task DesktopComposition_UsesTheSelectedProfileForBothClosureAndReflection()
    {
        using var first = new NotebookTestWorkspace();
        using var second = new NotebookTestWorkspace();
        using var firstServices = await DesktopComposition.CreateAsync(first.DirectoryPath);
        using var secondServices = await DesktopComposition.CreateAsync(second.DirectoryPath);
        var firstActions = firstServices.GetRequiredService<IAgendaReflectionActions>();
        var secondActions = secondServices.GetRequiredService<IAgendaReflectionActions>();
        var closure = new AgendaWeekRetrospectiveFixture();
        await closure.AddAsync(first);
        await closure.AddAsync(second);

        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await firstActions.SaveAsync(Week, "Perfil A", null)).Status);
        Assert.Null(await secondActions.GetAsync(Week));
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await secondActions.SaveAsync(Week, "Perfil B", null)).Status);
        Assert.Equal("Perfil A", (await firstActions.GetAsync(Week))?.WorkedWell);
        Assert.Equal("Perfil B", (await secondActions.GetAsync(Week))?.WorkedWell);
    }

    [Fact]
    public async Task MissingClosure_ReturnsTheUseCaseResultWithoutCreatingAReflection()
    {
        using var root = new NotebookTestWorkspace();
        var database = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var actions = Create(database);
        Assert.Equal(AgendaReflectionSaveStatus.ClosureMissing, (await actions.SaveAsync(Week, "Texto", null)).Status);
        Assert.Null(await actions.GetAsync(Week));
        Assert.Null(await new SqliteAgendaRetrospectiveActions(database, new FixedClock()).GetAsync(Week));
    }

    [Fact]
    public async Task MissingDatabase_FailsWithoutCreatingAFileAndCanBeRetried()
    {
        using var root = new NotebookTestWorkspace();
        var database = Path.Combine(root.DirectoryPath, "trama.db");
        var actions = Create(database);
        await Assert.ThrowsAsync<AgendaPersistenceException>(() => actions.GetAsync(Week));
        Assert.Equal(AgendaReflectionSaveStatus.PersistenceFailed, (await actions.SaveAsync(Week, "Texto", null)).Status);
        Assert.False(File.Exists(database));

        Assert.Equal(database, await InitializeAsync(root));
        Assert.Equal(AgendaReflectionSaveStatus.Saved, (await actions.SaveAsync(Week, "Texto", null)).Status);
        Assert.Equal("Texto", (await actions.GetAsync(Week))?.WorkedWell);
    }

    [Fact]
    public async Task InvalidWeek_IsRejectedBeforeAccessingTheDatabase()
    {
        using var root = new NotebookTestWorkspace();
        var actions = Create(Path.Combine(root.DirectoryPath, "trama.db"));
        var tuesday = Week.AddDays(1);
        Assert.Equal(AgendaReflectionSaveStatus.ValidationFailed, (await actions.SaveAsync(tuesday, "Texto", null)).Status);
        await Assert.ThrowsAsync<ArgumentException>(() => actions.GetAsync(tuesday));
    }

    [Fact]
    public async Task CancelledOperations_PropagateCancellationWithoutSaving()
    {
        using var root = new NotebookTestWorkspace();
        var actions = Create(await InitializeAsync(root));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => actions.SaveAsync(Week, "Texto", null, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => actions.GetAsync(Week, cancellation.Token));
        Assert.Null(await actions.GetAsync(Week));
    }

    [Fact]
    public async Task ConcurrentOperationsOnOneService_PreserveWholeReflectionsForBothWeeks()
    {
        using var root = new NotebookTestWorkspace();
        var actions = Create(await InitializeAsync(root));
        var earlier = Week.AddDays(-7);
        await new AgendaWeekRetrospectiveFixture().AddAsync(root, earlier);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(async () =>
        {
            await gate.Task;
            return await actions.SaveAsync(Week, "Primeira", "Ajuste A");
        });
        var second = Task.Run(async () =>
        {
            await gate.Task;
            return await actions.SaveAsync(earlier, "Segunda", "Ajuste B");
        });
        gate.SetResult();
        Assert.All(await Task.WhenAll(first, second), result => Assert.Equal(AgendaReflectionSaveStatus.Saved, result.Status));
        Assert.Equal(new AgendaWeekReflection(Week, "Primeira", "Ajuste A", Now), await actions.GetAsync(Week));
        Assert.Equal(new AgendaWeekReflection(earlier, "Segunda", "Ajuste B", Now), await actions.GetAsync(earlier));
    }

    private static async Task<string> InitializeAsync(NotebookTestWorkspace root)
    {
        var database = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        await new AgendaWeekRetrospectiveFixture().AddAsync(root);
        return database;
    }

    private static IAgendaReflectionActions Create(string database)
    {
        var type = typeof(SqliteAgendaActions).Assembly.GetType("Trama.Infrastructure.Persistence.SqliteAgendaReflectionActions");
        Assert.NotNull(type);
        return Assert.IsAssignableFrom<IAgendaReflectionActions>(Activator.CreateInstance(type, database, new FixedClock()));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class AgendaWeekRetrospectiveFixture
    {
        public async Task AddAsync(NotebookTestWorkspace root, DateOnly? week = null)
        {
            await using var context = root.CreateContext();
            var report = new AgendaRetrospectiveBuilder().Build(week ?? Week, [], Now, TimeZoneInfo.Utc);
            Assert.True(await new SqliteAgendaRetrospectiveRepository(context).TryAddAsync(report, TimeZoneInfo.Utc));
        }
    }
}
