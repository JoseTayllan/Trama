using CommunityToolkit.Mvvm.Input;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed partial class LibraryViewModel
{
    private readonly IKnowledgeDiscovery? _discovery;
    private IReadOnlyList<PageSearchResult> _searchResults = Array.Empty<PageSearchResult>();
    private IReadOnlyList<string> _currentPageTags = Array.Empty<string>();
    private string _searchQuery = string.Empty;
    private string _searchTag = string.Empty;
    private bool _isSearchOpen;

    public string SearchQuery { get => _searchQuery; set => SetProperty(ref _searchQuery, value ?? string.Empty); }
    public string SearchTag { get => _searchTag; set => SetProperty(ref _searchTag, value ?? string.Empty); }
    public bool CanSearch => CanNavigate && _discovery is not null;
    public bool CanEditTags => CanSearch && SelectedPage is not null;
    public bool IsSearchOpen { get => _isSearchOpen; private set => SetProperty(ref _isSearchOpen, value); }
    public IAsyncRelayCommand SearchCommand { get; }
    public IRelayCommand ClearSearchCommand { get; }
    public string SearchSummary => SearchResults.Count == 0 ? "Nenhuma página encontrada." : $"{SearchResults.Count} página(s) encontrada(s)";
    public string TagsSummary => CurrentPageTags.Count == 0 ? "Sem tags" : string.Join(" · ", CurrentPageTags);
    public IReadOnlyList<PageSearchResult> SearchResults
    {
        get => _searchResults;
        private set
        {
            SetProperty(ref _searchResults, value);
            OnPropertyChanged(nameof(SearchSummary));
        }
    }
    public IReadOnlyList<string> CurrentPageTags
    {
        get => _currentPageTags;
        private set
        {
            SetProperty(ref _currentPageTags, value);
            OnPropertyChanged(nameof(TagsSummary));
        }
    }

    public Task<bool> SearchAsync(string query, string? tag = null)
    {
        if (_discovery is null) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var results = await _discovery.SearchAsync(query, tag);
            SearchResults = results;
            IsSearchOpen = true;
            Feedback = "Busca nas páginas salvas. Abra um resultado para continuar o estudo.";
            return true;
        }, resolveChanges: false);
    }

    public Task<bool> OpenSearchResultAsync(Guid pageId)
    {
        if (_discovery is null || !SearchResults.Any(result => result.PageId == pageId)) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var spaces = await _actions.ListSpacesAsync();
            Guid? spaceId = null;
            Guid? notebookId = null;
            IReadOnlyList<NotebookInfo> notebooks = Array.Empty<NotebookInfo>();
            IReadOnlyList<KnowledgePageSummary> pages = await _actions.ListUnfiledPagesAsync();
            var found = pages.Any(page => page.Id == pageId);
            foreach (var space in spaces)
            {
                if (found) break;
                var books = await _actions.ListNotebooksAsync(space.Id);
                foreach (var book in books)
                {
                    var candidates = await _actions.ListPagesAsync(book.Id);
                    if (!candidates.Any(page => page.Id == pageId)) continue;
                    spaceId = space.Id;
                    notebookId = book.Id;
                    notebooks = books;
                    pages = candidates;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Feedback = "Este resultado não está mais disponível. Atualize a busca; seu conteúdo foi mantido.";
                return false;
            }
            var candidate = CreateEditor(notebookId);
            if (!await candidate.TryLoadAsync(pageId)) return false;
            var tags = await _discovery.GetPageTagsAsync(pageId);
            Replace(_spaces, spaces);
            Replace(_notebooks, notebooks);
            Replace(_pages, pages);
            SelectedSpaceId = spaceId;
            SelectedNotebookId = notebookId;
            Editor = candidate;
            CurrentPageTags = tags;
            Feedback = "Resultado aberto na localização atual da página.";
            return true;
        });
    }

    public Task<bool> LoadCurrentPageTagsAsync()
    {
        if (!CanEditTags || _discovery is null) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var tags = await _discovery.GetPageTagsAsync(Editor.PageId);
            CurrentPageTags = tags;
            return true;
        }, resolveChanges: false);
    }

    public Task<bool> SetCurrentPageTagsAsync(IReadOnlyList<string> tags)
    {
        if (!CanEditTags || _discovery is null) return Task.FromResult(false);
        return NavigateAsync(async () =>
        {
            var normalized = PageTags.Normalize(tags);
            await _discovery.SetPageTagsAsync(Editor.PageId, normalized);
            CurrentPageTags = normalized;
            Feedback = "Tags atualizadas. Seu rascunho foi mantido.";
            return true;
        }, resolveChanges: false);
    }
}
