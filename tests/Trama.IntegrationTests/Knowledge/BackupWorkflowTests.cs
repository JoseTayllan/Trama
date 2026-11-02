using Trama.Application.Storage;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Knowledge;

public sealed class BackupWorkflowTests
{
    [Fact]
    public async Task Create_ReportsOutputOnlyAfterSuccess()
    {
        var actions = new BackupActions();
        var viewModel = new BackupViewModel(actions);
        var path = Path.GetFullPath("copy.trama-backup");

        Assert.True(await viewModel.CreateAsync(path));

        Assert.Equal(path, viewModel.LastBackupPath);
        Assert.Equal(path, Assert.Single(actions.Created));
        Assert.False(viewModel.IsBusy);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Feedback));
        Assert.Null(viewModel.LastRestoredDirectory);
    }

    [Fact]
    public async Task Restore_ReportsNewProfileWithoutCreatingAnotherBackup()
    {
        var actions = new BackupActions();
        var viewModel = new BackupViewModel(actions);
        var archive = Path.GetFullPath("copy.trama-backup");
        var destination = Path.GetFullPath("restored-profile");

        Assert.True(await viewModel.RestoreAsync(archive, destination));

        Assert.Equal(destination, viewModel.LastRestoredDirectory);
        Assert.Equal((archive, destination), Assert.Single(actions.Restored));
        Assert.Empty(actions.Created);
        Assert.Null(viewModel.LastBackupPath);
        Assert.False(viewModel.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Failure_DoesNotReplacePreviousSuccessAndHidesInternalDetails(bool restoring)
    {
        var actions = new BackupActions();
        var viewModel = new BackupViewModel(actions);
        var archive = Path.GetFullPath("copy.trama-backup");
        var first = Path.GetFullPath("first-profile");
        Assert.True(await viewModel.CreateAsync(archive));
        Assert.True(await viewModel.RestoreAsync(archive, first));
        actions.Fail = true;

        Assert.False(restoring
            ? await viewModel.RestoreAsync(archive, Path.GetFullPath("second-profile"))
            : await viewModel.CreateAsync(Path.GetFullPath("failed.trama-backup")));

        Assert.Equal(archive, viewModel.LastBackupPath);
        Assert.Equal(first, viewModel.LastRestoredDirectory);
        Assert.DoesNotContain("PRIVATE_TEST_DETAIL", viewModel.Feedback);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Feedback));
        Assert.False(viewModel.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PendingOperation_BlocksDuplicateAndCompetingRequests(bool restoring)
    {
        var pending = new TaskCompletionSource<bool>();
        var actions = new BackupActions { Pending = pending.Task };
        var viewModel = new BackupViewModel(actions);
        var path = Path.GetFullPath("copy.trama-backup");
        var destination = Path.GetFullPath("profile");
        var operation = restoring ? viewModel.RestoreAsync(path, destination) : viewModel.CreateAsync(path);
        try
        {
            Assert.False(operation.IsCompleted);
            Assert.True(viewModel.IsBusy);
            Assert.Null(viewModel.LastBackupPath);
            Assert.Null(viewModel.LastRestoredDirectory);
            Assert.False(await viewModel.CreateAsync(path));
            Assert.False(await viewModel.RestoreAsync(path, destination));
            Assert.Equal(1, actions.Created.Count + actions.Restored.Count);
            pending.SetResult(true);
            Assert.True(await operation);
            Assert.False(viewModel.IsBusy);
        }
        finally
        {
            pending.TrySetResult(true);
            await operation;
        }
    }

    [Fact]
    public void Startup_WithoutSelectionKeepsDefaultProfile()
    {
        Assert.Equal(LocalDatabase.DefaultDirectory, DataDirectoryOptions.Resolve([]));
    }

    [Fact]
    public void Startup_ExplicitAbsoluteProfileCanOpenARestoredLibrary()
    {
        var directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TramaRestoredProfile"));

        Assert.Equal(directory, DataDirectoryOptions.Resolve(["--data-directory", directory]));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("relative")]
    [InlineData("unknown")]
    [InlineData("repeated")]
    public void Startup_InvalidSelectionIsRejectedInsteadOfSilentlyOpeningDefault(string kind)
    {
        var absolute = Path.GetFullPath("restored-profile");
        string[] arguments = kind switch
        {
            "missing" => ["--data-directory"],
            "relative" => ["--data-directory", "relative-profile"],
            "unknown" => ["--unknown"],
            _ => ["--data-directory", absolute, "--data-directory", absolute]
        };

        Assert.ThrowsAny<ArgumentException>(() => DataDirectoryOptions.Resolve(arguments));
    }

    private sealed class BackupActions : ILocalBackup
    {
        public List<string> Created { get; } = [];
        public List<(string Archive, string Destination)> Restored { get; } = [];
        public bool Fail { get; set; }
        public Task? Pending { get; init; }

        public async Task<BackupReceipt> CreateAsync(string archivePath, CancellationToken cancellationToken = default)
        {
            Created.Add(archivePath);
            if (Fail) throw new IOException("PRIVATE_TEST_DETAIL");
            if (Pending is not null) await Pending;
            return new BackupReceipt(archivePath, 2);
        }

        public async Task<RestoreReceipt> RestoreAsync(string archivePath, string destinationDirectory, CancellationToken cancellationToken = default)
        {
            Restored.Add((archivePath, destinationDirectory));
            if (Fail) throw new InvalidDataException("PRIVATE_TEST_DETAIL");
            if (Pending is not null) await Pending;
            return new RestoreReceipt(destinationDirectory);
        }
    }
}
