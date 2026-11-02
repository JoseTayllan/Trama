using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed partial class LibraryViewModel
{
    private readonly ILibraryTrashActions? _trash;

    public bool HasTrash => _trash is not null;
    public bool CanTrashSpace => HasTrash && CanManageSpace;
    public bool CanTrashNotebook => HasTrash && CanManageNotebook;
    public bool CanTrashPage => HasTrash && CanManagePage;

    public Task<bool> MoveToTrashAsync(LibraryEntryKind kind, Func<LibraryTrashPreview, Task<bool>> confirm)
    {
        ArgumentNullException.ThrowIfNull(confirm);
        var id = kind switch
        {
            LibraryEntryKind.Space when CanTrashSpace => SelectedSpaceId,
            LibraryEntryKind.Notebook when CanTrashNotebook => SelectedNotebookId,
            LibraryEntryKind.Page when CanTrashPage => Editor.PageId,
            _ => null
        };
        if (_trash is null || id is null) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var preview = await _trash.PreviewAsync(kind, id.Value);
            if (!await confirm(preview)) return false;
            await _trash.MoveToTrashAsync(kind, id.Value);
            // Dispose the old autosave owner before enabling navigation again.
            if (kind == LibraryEntryKind.Space)
            {
                SelectedSpaceId = null;
                SelectedNotebookId = null;
            }
            else if (kind == LibraryEntryKind.Notebook) SelectedNotebookId = null;
            Editor = CreateEditor(SelectedNotebookId);
            _pages.Clear();
            if (kind != LibraryEntryKind.Page) _notebooks.Clear();
            IsSearchOpen = false;
            SearchResults = Array.Empty<PageSearchResult>();
            await ReloadTrashCatalogAsync();
            Feedback = "Enviado para a lixeira. O conteúdo pode ser restaurado.";
            return true;
        });
    }

    public Task<IReadOnlyList<LibraryTrashEntry>> ListTrashAsync() =>
        _trash?.ListAsync() ?? Task.FromResult<IReadOnlyList<LibraryTrashEntry>>(Array.Empty<LibraryTrashEntry>());

    public Task<bool> RestoreTrashEntryAsync(LibraryTrashEntry entry)
    {
        if (_trash is null || !entry.CanRestore) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            await _trash.RestoreAsync(entry.Kind, entry.Id);
            await ReloadTrashCatalogAsync();
            Feedback = "Restaurado com a organização original.";
            return true;
        }, resolveChanges: false);
    }

    private async Task ReloadTrashCatalogAsync()
    {
        var spaces = await _actions.ListSpacesAsync();
        var books = SelectedSpaceId is { } spaceId ? await _actions.ListNotebooksAsync(spaceId) : Array.Empty<NotebookInfo>();
        var pages = SelectedNotebookId is { } bookId ? await _actions.ListPagesAsync(bookId)
            : SelectedSpaceId is null ? await _actions.ListUnfiledPagesAsync() : Array.Empty<KnowledgePageSummary>();
        Replace(_spaces, spaces);
        Replace(_notebooks, books);
        Replace(_pages, pages);
        NotifySelectionChanged();
    }
}
