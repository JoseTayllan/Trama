using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Knowledge;
using Trama.Application.Storage;
using Trama.Desktop.ViewModels;
using Trama.Infrastructure.Persistence;

namespace Trama.Desktop;

public static class DesktopComposition
{
    public static Task<ServiceProvider> CreateAsync(string dataDirectory, CancellationToken cancellationToken = default,
        IUnsavedChangesPrompt? unsavedChangesPrompt = null) =>
        CreateAsync(dataDirectory, enableAutoSave: false, cancellationToken, unsavedChangesPrompt);

    public static async Task<ServiceProvider> CreateAsync(string dataDirectory, bool enableAutoSave, CancellationToken cancellationToken = default,
        IUnsavedChangesPrompt? unsavedChangesPrompt = null)
    {
        var databasePath = await LocalDatabase.InitializeAsync(dataDirectory, cancellationToken);
        var images = new LocalImageAttachmentStore(dataDirectory);
        return new ServiceCollection()
            .AddSingleton<ICreateNote>(new SqliteNoteService(databasePath))
            .AddTransient<NoteEditorViewModel>()
            .AddSingleton<INotebookActions>(new SqliteNotebookActions(databasePath, images))
            .AddSingleton<IMarkdownPageExporter, LocalMarkdownPageExporter>()
            .AddTransient(services => new NotebookEditorViewModel(services.GetRequiredService<INotebookActions>(),
                services.GetRequiredService<IMarkdownPageExporter>(), enableAutoSave ? TimeProvider.System : null))
            .AddSingleton<ILibraryActions>(new SqliteLibraryActions(databasePath, images))
            .AddSingleton<ILocalBackup>(new LocalBackupService(dataDirectory))
            .AddTransient<BackupViewModel>()
            .AddSingleton<IKnowledgeDiscovery>(new SqliteKnowledgeDiscovery(databasePath))
            .AddSingleton<ILibraryTrashActions>(new SqliteLibraryTrashActions(databasePath))
            .AddSingleton<ILibraryManagementActions>(new SqliteLibraryManagement(databasePath))
            .AddSingleton<IUnsavedChangesPrompt>(unsavedChangesPrompt ?? new CancelUnsavedChangesPrompt())
            .AddTransient(services => new LibraryViewModel(services.GetRequiredService<ILibraryActions>(),
                services.GetRequiredService<IUnsavedChangesPrompt>(), services.GetRequiredService<ILibraryManagementActions>(),
                services.GetRequiredService<IKnowledgeDiscovery>(), enableAutoSave ? TimeProvider.System : null,
                services.GetRequiredService<ILibraryTrashActions>(), services.GetRequiredService<IMarkdownPageExporter>()))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
