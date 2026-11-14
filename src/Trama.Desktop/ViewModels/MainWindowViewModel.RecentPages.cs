using System.Globalization;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    private IReadOnlyList<HomeRecentPage> _homeRecentPages = [];
    private int _recentPagesRequest;
    private bool _isRecentPagesLoading;
    private bool _hasRecentPagesError;

    public IReadOnlyList<HomeRecentPage> HomeRecentPages => _homeRecentPages;
    public Guid? HomeCurrentPageId => HasNotebookToContinue ? Library?.Editor.PageId : null;
    public bool ShowContinueNotebook => HomeCurrentPageId is { } id &&
        !HomeRecentPages.Any(page => page.Page.PageId == id);
    public bool IsRecentPagesLoading => _isRecentPagesLoading;
    public bool HasRecentPagesError => _hasRecentPagesError;
    public bool IsRecentPagesEmpty => !IsRecentPagesLoading && !HasRecentPagesError && HomeRecentPages.Count == 0;

    private async Task RefreshHomeRecentPagesAsync()
    {
        var request = ++_recentPagesRequest;
        _homeRecentPages = [];
        _hasRecentPagesError = false;
        var library = Library;
        _isRecentPagesLoading = library is not null;
        NotifyRecentPages();
        if (library is null) return;

        try
        {
            var pages = await library.ReadRecentPagesAsync();
            var now = DateTimeOffset.Now;
            if (request == _recentPagesRequest)
                _homeRecentPages = pages.Select(page => new HomeRecentPage(page.Page,
                    FormatEditAge(page.EditedAt.ToLocalTime(), now))).ToArray();
        }
        catch (Exception)
        {
            if (request == _recentPagesRequest) _hasRecentPagesError = true;
        }
        finally
        {
            if (request == _recentPagesRequest)
            {
                _isRecentPagesLoading = false;
                NotifyRecentPages();
            }
        }
    }

    internal async Task OpenHomeRecentPageAsync(Guid pageId)
    {
        if (!IsHomeActive || !CanNavigateModules || Library is not { } library) return;
        if (HomeCurrentPageId == pageId)
        {
            SelectModule(Module.Notebooks);
            return;
        }

        var opened = await library.OpenRecentPageAsync(pageId);
        if (!ReferenceEquals(Library, library) || !IsHomeActive) return;
        if (opened) SelectModule(Module.Notebooks);
        else Status = library.Feedback;
    }

    private static string FormatEditAge(DateTimeOffset edited, DateTimeOffset now)
    {
        if (edited.Date == now.Date)
        {
            var elapsed = now - edited;
            if (elapsed.TotalMinutes < 1) return "Agora há pouco";
            if (elapsed.TotalHours < 1) return $"Há {(int)elapsed.TotalMinutes} min";
            return $"Há {(int)elapsed.TotalHours} h";
        }

        if ((now.Date - edited.Date).Days == 1) return "Ontem";
        return edited.ToString(edited.Year == now.Year ? "dd/MM" : "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private void NotifyRecentPages()
    {
        OnPropertyChanged(nameof(HomeRecentPages));
        OnPropertyChanged(nameof(ShowContinueNotebook));
        OnPropertyChanged(nameof(IsRecentPagesLoading));
        OnPropertyChanged(nameof(HasRecentPagesError));
        OnPropertyChanged(nameof(IsRecentPagesEmpty));
    }
}

public sealed record HomeRecentPage(PageSearchResult Page, string EditAge);
