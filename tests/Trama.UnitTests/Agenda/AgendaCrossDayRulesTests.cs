using System.Text.Json;
using Trama.Application.Agenda;

namespace Trama.UnitTests.Agenda;

public sealed class AgendaCrossDayRulesTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);

    [Theory]
    [InlineData(22, 0, 2, 0, 1)]
    [InlineData(23, 59, 0, 0, 1)]
    [InlineData(18, 0, 9, 0, 2)]
    public void PositiveRangesAcrossDays_AreValidWithoutRewritingTheirHours(int startHour, int startMinute, int endHour, int endMinute, int days)
    {
        var task = Example() with { StartTime = new TimeOnly(startHour, startMinute), EndTime = new TimeOnly(endHour, endMinute), PlannedEndDate = Day.AddDays(days) };
        Assert.True(AgendaTaskValidation.IsValid(task));
        Assert.Equal(new TimeOnly(endHour, endMinute), task.EndTime); Assert.Equal(Day.AddDays(days), task.PlannedEndDate);
    }

    [Fact]
    public void DateOnlyRange_DoesNotInventTimes()
    {
        var task = Example() with { StartTime = null, EndTime = null, PlannedEndDate = Day.AddDays(1) };
        Assert.True(AgendaTaskValidation.IsValid(task)); Assert.Null(task.StartTime); Assert.Null(task.EndTime);
    }

    [Fact]
    public void PlannedEndBeforeTheStartDate_IsRejectedEvenWithIncreasingHours()
    {
        Assert.False(AgendaTaskValidation.IsValid(Example() with { PlannedEndDate = Day.AddDays(-1) }));
    }

    [Theory]
    [InlineData(18, 18)]
    [InlineData(19, 18)]
    public void ExplicitSameDay_NonPositiveRangesRemainInvalid(int start, int end)
    {
        Assert.False(AgendaTaskValidation.IsValid(Example() with { PlannedEndDate = Day, StartTime = new TimeOnly(start, 0), EndTime = new TimeOnly(end, 0) }));
    }

    [Fact]
    public void ExplicitSameDay_PositiveRangeRemainsValid()
        => Assert.True(AgendaTaskValidation.IsValid(Example() with { PlannedEndDate = Day }));

    [Fact]
    public void MissingEndDate_StillMeansSameDayInsteadOfInferringTomorrow()
        => Assert.False(AgendaTaskValidation.IsValid(Example() with { StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) }));

    [Fact]
    public void EndTimeWithoutStartTime_RemainsInvalidEvenWithALaterEndDate()
        => Assert.False(AgendaTaskValidation.IsValid(Example() with { StartTime = null, PlannedEndDate = Day.AddDays(1) }));

    [Fact]
    public void PlannedEndDateWithoutEndTime_AllowsAnUnspecifiedDeadlineHour()
        => Assert.True(AgendaTaskValidation.IsValid(Example() with { EndTime = null, PlannedEndDate = Day.AddDays(1) }));

    [Fact]
    public async Task SavingAcrossMidnight_ForwardsTheEndDateAndIdentityToTheRepository()
    {
        var repository = new RecordingRepository();
        var task = Example() with { StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0), PlannedEndDate = Day.AddDays(1) };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SaveAgendaTask(repository).ExecuteAsync(task)).Status);
        Assert.Same(task, repository.Saved);
    }

    [Fact]
    public void NullEndDate_IsOmittedFromLegacyJson()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Example()));
        Assert.False(json.RootElement.TryGetProperty(nameof(AgendaTaskSnapshot.PlannedEndDate), out _));
    }

    [Fact]
    public void ExplicitEndDate_IsPreservedByJsonRoundTrip()
    {
        var task = Example() with { PlannedEndDate = Day.AddDays(1) };
        Assert.Equal(task, JsonSerializer.Deserialize<AgendaTaskSnapshot>(JsonSerializer.Serialize(task)));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Estudo noturno", "Conteúdo fictício", Day, new TimeOnly(18, 0), new TimeOnly(19, 0));

    private sealed class RecordingRepository : IAgendaTaskRepository
    {
        public AgendaTaskSnapshot? Saved { get; private set; }
        public Task SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default) { Saved = task; return Task.CompletedTask; }
    }
}
