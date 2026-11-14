using Trama.Application.Agenda;

namespace Trama.Desktop.ViewModels;

public sealed partial class AgendaViewModel
{
    private IReadOnlyList<AgendaPageLink> _pageLinks = [];
    private AgendaPageLink? _selectedPageLink;
    private IReadOnlySet<Guid> _dayTasksWithPageLinks = new HashSet<Guid>();
    private IReadOnlySet<Guid> _weekTasksWithPageLinks = new HashSet<Guid>();

    public IReadOnlySet<Guid> DayTasksWithPageLinks
    {
        get => _dayTasksWithPageLinks;
        private set => SetProperty(ref _dayTasksWithPageLinks, value);
    }

    public IReadOnlySet<Guid> WeekTasksWithPageLinks
    {
        get => _weekTasksWithPageLinks;
        private set => SetProperty(ref _weekTasksWithPageLinks, value);
    }
    private IAgendaPageLinks? PageLinks => _actions as IAgendaPageLinks;
    public bool HasPageLinkSupport => PageLinks is not null;
    public bool CanManagePageLink => CanEdit && HasPageLinkSupport && EditingTaskId.HasValue;
    public bool HasLinkedPage => _pageLinks.Count > 0;
    public bool HasMultipleLinkedPages => _pageLinks.Count > 1;
    public bool HasSingleLinkedPage => _pageLinks.Count == 1;
    public IReadOnlyList<AgendaPageLink> LinkedPages => _pageLinks;
    public string LinkedPageLabel => SelectedPageLink?.Title ?? "Página vinculada";

    public AgendaPageLink? SelectedPageLink
    {
        get => _selectedPageLink;
        set
        {
            if (value is not null && !_pageLinks.Contains(value)) return;
            if (SetProperty(ref _selectedPageLink, value)) OnPropertyChanged(nameof(LinkedPageLabel));
        }
    }

    private async Task<IReadOnlyList<AgendaTaskSnapshot>> ReadDayTasksAsync(DateOnly day, CancellationToken cancellationToken = default)
    {
        var tasks = await _actions.ListAsync(day, day, cancellationToken);
        DayTasksWithPageLinks = await ReadTasksWithLinksAsync(tasks, cancellationToken);
        return tasks;
    }

    internal async Task<IReadOnlySet<Guid>> ReadTasksWithLinksAsync(IEnumerable<AgendaTaskSnapshot> tasks,
        CancellationToken cancellationToken) =>
        PageLinks is { } links
            ? await links.ListTasksWithPageLinksAsync(tasks.Select(task => task.Id).ToArray(), cancellationToken)
            : new HashSet<Guid>();

    private void UpdateCardLinkVisibility(Guid taskId)
    {
        DayTasksWithPageLinks = Update(DayTasksWithPageLinks);
        WeekTasksWithPageLinks = Update(WeekTasksWithPageLinks);

        IReadOnlySet<Guid> Update(IReadOnlySet<Guid> current)
        {
            var updated = current.ToHashSet();
            var changed = HasLinkedPage ? updated.Add(taskId) : updated.Remove(taskId);
            return changed ? updated : current;
        }
    }

    internal async Task<IReadOnlyList<AgendaPageLink>> ReadCardPageLinksAsync(Guid taskId)
    {
        if (IsBusy) return [];
        IsBusy = true;
        try { return await ReadPageLinksAsync(taskId); }
        catch (Exception)
        {
            Feedback = "Não foi possível consultar as páginas vinculadas.";
            return [];
        }
        finally { IsBusy = false; }
    }

    private async Task<IReadOnlyList<AgendaPageLink>> ReadPageLinksAsync(Guid taskId) =>
        PageLinks is { } links ? await links.ListPageLinksAsync(taskId) : [];

    private void SetPageLinks(IReadOnlyList<AgendaPageLink> links, Guid? selectPageId = null)
    {
        _pageLinks = links;
        OnPropertyChanged(nameof(LinkedPages));
        _selectedPageLink = links.FirstOrDefault(link => link.PageId == selectPageId) ?? links.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedPageLink));
        OnPropertyChanged(nameof(LinkedPageLabel));
        OnPropertyChanged(nameof(HasLinkedPage));
        OnPropertyChanged(nameof(HasMultipleLinkedPages));
        OnPropertyChanged(nameof(HasSingleLinkedPage));
    }

    internal async Task LinkPageAsync(Func<Task<Guid?>> selectPage)
    {
        if (!CanManagePageLink || EditingTaskId is not { } taskId || PageLinks is not { } links) return;
        IsBusy = true;
        try
        {
            if (await selectPage() is not { } pageId) return;
            await links.AddPageLinkAsync(taskId, pageId);
            SetPageLinks(await links.ListPageLinksAsync(taskId), pageId);
            UpdateCardLinkVisibility(taskId);
            Feedback = "Página vinculada à tarefa.";
        }
        catch (Exception) { Feedback = "Não foi possível vincular a página. Confira se a tarefa e a página ainda estão disponíveis."; }
        finally { IsBusy = false; }
    }

    internal async Task UnlinkPageAsync()
    {
        if (!CanManagePageLink || EditingTaskId is not { } taskId || PageLinks is not { } links ||
            SelectedPageLink is not { } page) return;
        IsBusy = true;
        try
        {
            await links.RemovePageLinkAsync(taskId, page.PageId);
            SetPageLinks(await links.ListPageLinksAsync(taskId));
            UpdateCardLinkVisibility(taskId);
            Feedback = "Vínculo removido.";
        }
        catch (Exception) { Feedback = "Não foi possível remover o vínculo."; }
        finally { IsBusy = false; }
    }

    internal Task<bool> OpenLinkedPageAsync(Func<Guid, Task<string?>> openPage) =>
        CanManagePageLink && SelectedPageLink is { } page
            ? OpenLinkedPageAsync(page.PageId, openPage)
            : Task.FromResult(false);

    internal async Task<bool> OpenLinkedPageAsync(Guid pageId, Func<Guid, Task<string?>> openPage)
    {
        if (IsBusy) return false;
        IsBusy = true;
        try
        {
            var error = await openPage(pageId);
            Feedback = error ?? "Página aberta nos Cadernos.";
            return error is null;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível abrir a página vinculada.";
            return false;
        }
        finally { IsBusy = false; }
    }
}
