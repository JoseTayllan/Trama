using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Trama.Application.Knowledge;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class LibraryWorkspaceView : UserControl
{
    private bool _changingSelection;
    private bool _managementDialogOpen;
    private bool? _compactSidebar;

    public LibraryWorkspaceView()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            if (Bounds.Width <= 0) return;
            var compact = Bounds.Width < 1000;
            if (_compactSidebar == compact) return;
            _compactSidebar = compact;
            ShowSidebar(!compact);
        };
    }

    private void ToggleLibraryClicked(object? sender, RoutedEventArgs e) => ShowSidebar(!LibrarySidebar.IsVisible);

    private void ShowSidebar(bool visible)
    {
        LibrarySidebar.IsVisible = visible;
        WorkspaceLayout.ColumnDefinitions[0].Width = new GridLength(visible ? 260 : 0);
        ToggleLibraryButton.Content = visible ? "Recolher acervo" : "Acervo";
    }

    public LibraryWorkspaceView(LibraryViewModel viewModel) : this() => DataContext = viewModel;

    private async void LibraryBackupClicked(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is MainWindow window) await window.OpenBackupAsync();
    }

    private async void TrashSpaceClicked(object? sender, RoutedEventArgs e) => await TrashSelectedAsync(LibraryEntryKind.Space);
    private async void TrashNotebookClicked(object? sender, RoutedEventArgs e) => await TrashSelectedAsync(LibraryEntryKind.Notebook);
    private async void TrashPageClicked(object? sender, RoutedEventArgs e) => await TrashSelectedAsync(LibraryEntryKind.Page);

    private Task TrashSelectedAsync(LibraryEntryKind kind) => ShowManagementAsync(async (library, owner) =>
    {
        await library.MoveToTrashAsync(kind, async preview => owner.IsVisible &&
            await new TrashConfirmationDialog(preview).ShowDialog<bool?>(owner) == true);
    });

    private async void OpenTrashClicked(object? sender, RoutedEventArgs e) => await ShowManagementAsync(async (library, owner) =>
    {
        var viewModel = new TrashViewModel(library);
        await viewModel.LoadAsync();
        if (owner.IsVisible) await new TrashDialog(viewModel).ShowDialog(owner);
    });

    private async void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not LibraryViewModel library || !library.CanSearch) return;
        e.Handled = true;
        await library.SearchCommand.ExecuteAsync(null);
    }

    private async void SearchResultSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (SearchResultList.SelectedItem is not PageSearchResult result) return;
        try
        {
            await ChangeSelectionAsync(library => library.OpenSearchResultAsync(result.PageId));
        }
        finally
        {
            SearchResultList.SetCurrentValue(ListBox.SelectedItemProperty, null);
        }
    }

    private async void EditTagsClicked(object? sender, RoutedEventArgs e) => await ShowManagementAsync(async (library, owner) =>
    {
        if (!await library.LoadCurrentPageTagsAsync() || !owner.IsVisible) return;
        var tags = await new PageTagsDialog(library.CurrentPageTags).ShowDialog<IReadOnlyList<string>?>(owner);
        if (tags is not null) await library.SetCurrentPageTagsAsync(tags);
    });

    private async void RenameSpaceClicked(object? sender, RoutedEventArgs e) => await RenameAsync(LibraryEntryKind.Space);
    private async void RenameNotebookClicked(object? sender, RoutedEventArgs e) => await RenameAsync(LibraryEntryKind.Notebook);
    private async void RenamePageClicked(object? sender, RoutedEventArgs e) => await RenameAsync(LibraryEntryKind.Page);

    private Task RenameAsync(LibraryEntryKind kind) => ShowManagementAsync(async (library, owner) =>
    {
        var title = kind switch
        {
            LibraryEntryKind.Space when library.CanManageSpace => library.SelectedSpace?.Title,
            LibraryEntryKind.Notebook when library.CanManageNotebook => library.SelectedNotebook?.Title,
            LibraryEntryKind.Page when library.CanManagePage => library.Editor.Title,
            _ => null
        };
        if (title is null) return;
        var heading = kind switch
        {
            LibraryEntryKind.Space => "Renomear espaço",
            LibraryEntryKind.Notebook => "Renomear caderno",
            _ => "Renomear página"
        };
        var result = await new RenameEntryDialog(heading, title).ShowDialog<string?>(owner);
        if (result is null) return;
        await ChangeSelectionAsync(vm => kind switch
        {
            LibraryEntryKind.Space => vm.RenameSelectedSpaceAsync(result),
            LibraryEntryKind.Notebook => vm.RenameSelectedNotebookAsync(result),
            _ => vm.RenameCurrentPageAsync(result)
        });
    });

    private async void MovePageClicked(object? sender, RoutedEventArgs e) => await ShowManagementAsync(async (library, owner) =>
    {
        if (!library.CanManagePage) return;
        var destinations = await library.ListMoveDestinationsAsync();
        if (!owner.IsVisible) return;
        var result = await new MovePageDialog(destinations).ShowDialog<Guid?>(owner);
        if (result is { } id) await ChangeSelectionAsync(vm => vm.MoveCurrentPageAsync(id));
    });

    private async Task ShowManagementAsync(Func<LibraryViewModel, Window, Task> show)
    {
        if (_managementDialogOpen || DataContext is not LibraryViewModel library || !library.CanNavigate ||
            TopLevel.GetTopLevel(this) is not Window owner) return;
        _managementDialogOpen = true;
        try
        {
            await show(library, owner);
        }
        catch (Exception)
        {
            library.ReportManagementFailure();
        }
        finally
        {
            _managementDialogOpen = false;
        }
    }

    private async void SpaceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SpaceList.SelectedItem is KnowledgeSpace space &&
            DataContext is LibraryViewModel library && library.SelectedSpaceId != space.Id)
            await ChangeSelectionAsync(library => library.SelectSpaceAsync(space.Id));
    }

    private async void NotebookSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NotebookList.SelectedItem is NotebookInfo notebook &&
            DataContext is LibraryViewModel library && library.SelectedNotebookId != notebook.Id)
            await ChangeSelectionAsync(library => library.SelectNotebookAsync(notebook.Id));
    }

    private async void PageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PageList.SelectedItem is KnowledgePageSummary page &&
            DataContext is LibraryViewModel library && library.Editor.PageId != page.Id)
            await ChangeSelectionAsync(library => library.OpenPageAsync(page.Id));
    }

    private async Task ChangeSelectionAsync(Func<LibraryViewModel, Task<bool>> select)
    {
        if (_changingSelection || DataContext is not LibraryViewModel library || !library.CanNavigate) return;
        _changingSelection = true;
        try
        {
            await select(library);
        }
        finally
        {
            SpaceList.SetCurrentValue(ListBox.SelectedItemProperty, library.SelectedSpace);
            NotebookList.SetCurrentValue(ListBox.SelectedItemProperty, library.SelectedNotebook);
            PageList.SetCurrentValue(ListBox.SelectedItemProperty, library.SelectedPage);
            _changingSelection = false;
        }
    }
}
