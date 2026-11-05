using System.Globalization;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaOpenEndedTaskTests
{
    private static readonly DateOnly Day = new(2026, 11, 4);

    [Theory]
    [InlineData(AgendaTaskStatus.Pending)]
    [InlineData(AgendaTaskStatus.InProgress)]
    public async Task StartWithoutEnd_IsSavedAndReopenedWithoutCompletingTheTask(AgendaTaskStatus status)
    {
        using var root = new NotebookTestWorkspace();
        var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var task = Example() with { EndTime = null, Status = status };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SqliteAgendaActions(path).SaveAsync(task)).Status);
        var reopened = new SqliteAgendaActions(path);
        Assert.Equal(task, await reopened.GetByIdAsync(task.Id));
        Assert.Equal(task, Assert.Single(await reopened.ListAsync(Day, Day)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveButton_AfterConfirmedCreationOrEdit_PreparesACleanNewForm(bool editing)
    {
        using var root = new NotebookTestWorkspace();
        var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var actions = new SqliteAgendaActions(path);
        var task = Example();
        var vm = new AgendaViewModel(actions, new RejectUnexpectedPrompt());
        await vm.InitializeAsync(Day);
        if (editing)
        {
            Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions.SaveAsync(task)).Status);
            Assert.True(await vm.EditTaskAsync(task.Id));
        }
        vm.Title = "Tarefa salva pelo botão";
        vm.StartTime = task.StartTime!.Value.ToTimeSpan();
        vm.EndTime = task.EndTime!.Value.ToTimeSpan();
        vm.Status = AgendaTaskStatus.Pending;
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await actions.ListAsync(Day, Day));
        Assert.Equal("Tarefa salva pelo botão", saved.Title);
        Assert.Equal(AgendaTaskStatus.Pending, saved.Status);
        if (editing) Assert.Equal(task.Id, saved.Id);
        Assert.Null(vm.EditingTaskId);
        Assert.Equal(string.Empty, vm.Title);
        Assert.Equal(string.Empty, vm.Description);
        Assert.Null(vm.StartTime);
        Assert.Null(vm.EndTime);
        Assert.Equal(Day, DateOnly.FromDateTime(vm.PlannedDate!.Value.DateTime));
        Assert.False(vm.HasUnsavedChanges);
        Assert.Equal(saved, Assert.Single(vm.Tasks));
        Assert.True(await vm.RequestCloseAsync());
    }

    [Fact]
    public async Task FailedSaveButton_KeepsTheDraftInsteadOfClearingIt()
    {
        using var root = new NotebookTestWorkspace();
        var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var actions = new SqliteAgendaActions(path);
        var vm = new AgendaViewModel(actions, new RejectUnexpectedPrompt());
        await vm.InitializeAsync(Day);
        vm.Title = "Rascunho inválido";
        vm.StartTime = TimeSpan.FromHours(19);
        vm.EndTime = TimeSpan.FromHours(18);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Rascunho inválido", vm.Title);
        Assert.Equal(TimeSpan.FromHours(19), vm.StartTime);
        Assert.Equal(TimeSpan.FromHours(18), vm.EndTime);
        Assert.True(vm.HasUnsavedChanges);
        Assert.Empty(await actions.ListAsync(Day, Day));
    }

    [Fact]
    public void StartWithoutEnd_IsDisplayedAsAnOpenIntervalInsteadOfUntimed()
    {
        var text = new AgendaPresentationConverter().Convert(Example() with { EndTime = null }, typeof(string), "time", CultureInfo.GetCultureInfo("pt-BR"));
        Assert.Equal("19:27 · sem fim", text);
    }

    [Theory]
    [InlineData(null, 20)]
    [InlineData(20, 19)]
    public async Task EndWithoutStartOrBeforeStart_RemainsInvalid(int? start, int end)
    {
        using var root = new NotebookTestWorkspace();
        var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var actions = new SqliteAgendaActions(path);
        var task = Example() with { StartTime = start is { } hour ? new TimeOnly(hour, 0) : null, EndTime = new TimeOnly(end, 0) };
        Assert.Equal(AgendaTaskSaveStatus.ValidationFailed, (await actions.SaveAsync(task)).Status);
        Assert.Empty(await actions.ListAsync(Day, Day));
    }

    [Fact]
    public async Task BackupAndRestore_PreserveAnOpenTaskWithoutInventingAnEnd()
    {
        using var root = new NotebookTestWorkspace();
        using var destination = new NotebookTestWorkspace();
        var path = await LocalDatabase.InitializeAsync(root.DirectoryPath);
        var task = Example() with { EndTime = null };
        Assert.Equal(AgendaTaskSaveStatus.Saved, (await new SqliteAgendaActions(path).SaveAsync(task)).Status);
        var archive = Path.Combine(destination.DirectoryPath, "agenda.zip");
        var restored = Path.Combine(destination.DirectoryPath, "restored");
        var backup = new LocalBackupService(root.DirectoryPath);
        await backup.CreateAsync(archive);
        await backup.RestoreAsync(archive, restored);
        Assert.Equal(task, await new SqliteAgendaActions(Path.Combine(restored, "trama.db")).GetByIdAsync(task.Id));
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Trabalho de exemplo", "Dados fictícios", Day,
        new TimeOnly(19, 27), new TimeOnly(20, 0), AgendaTaskCategory.Work, AgendaTaskStatus.Pending);

    private sealed class RejectUnexpectedPrompt : IUnsavedChangesPrompt
    {
        public Task<UnsavedChangesChoice> ChooseAsync() => throw new InvalidOperationException("Não deveria pedir confirmação para um formulário limpo.");
    }
}
