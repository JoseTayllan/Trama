using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaManualCompletionTests
{
    [Fact]
    public async Task ExplicitActualDateAndTime_AreStoredInsteadOfTheConfirmationTime()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync(AgendaCloseFixture.Day.AddDays(-1));
        var actual = new DateTimeOffset(2026, 11, 4, 2, 0, 0, TimeSpan.FromHours(-3));
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]) { CompletedAt = actual };
        Assert.True(await f.Closing.RequestCloseAsync());
        var saved = await f.Service.GetLastCompletedAtAsync(task.Id);
        Assert.Equal(actual, saved); Assert.Equal(actual.Offset, saved!.Value.Offset);
        Assert.Equal(task with { Status = AgendaTaskStatus.Completed }, await f.ReadAsync(task.Id));
    }

    [Fact]
    public async Task FutureActualTime_IsRejectedWithoutCompletingTheTask()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]) { CompletedAt = f.Clock.Now.AddDays(1) };
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Actions.CompleteCalls);
        Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.Service.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task ActualTimeBeforeThePlannedStartDay_IsRejectedWithoutWriting()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.CompleteSelected, [task.Id]) { CompletedAt = f.Clock.Now.AddDays(-1) };
        Assert.False(await f.Closing.RequestCloseAsync()); Assert.Equal(task, await f.ReadAsync(task.Id)); Assert.Null(await f.Service.GetLastCompletedAtAsync(task.Id));
    }

    [Fact]
    public async Task KeepingTasksOpen_IgnoresAnExplicitCompletionTimestamp()
    {
        using var f = await AgendaCloseFixture.CreateAsync(); var task = await f.SeedAsync();
        f.Prompt.Decision = new(AgendaCompletionChoice.KeepOpen, []) { CompletedAt = f.Clock.Now.AddDays(1) };
        Assert.True(await f.Closing.RequestCloseAsync()); Assert.Equal(0, f.Actions.CompleteCalls); Assert.Equal(task, await f.ReadAsync(task.Id));
    }
}
