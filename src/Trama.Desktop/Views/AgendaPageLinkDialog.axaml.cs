using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Knowledge;

namespace Trama.Desktop.Views;

public sealed partial class AgendaPageLinkDialog : Window
{
    private readonly IReadOnlyList<PageSearchResult> _pages = [];

    public AgendaPageLinkDialog() => InitializeComponent();

    public AgendaPageLinkDialog(IReadOnlyList<PageSearchResult> pages) : this()
    {
        _pages = pages;
        AgendaPageLinkList.ItemsSource = pages;
    }

    private void FilterChanged(object? sender, TextChangedEventArgs e)
    {
        var term = PageFilter.Text?.Trim();
        if (string.IsNullOrEmpty(term))
        {
            AgendaPageLinkList.ItemsSource = _pages;
            return;
        }
        var comparer = CultureInfo.CurrentCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        AgendaPageLinkList.ItemsSource = _pages.Where(page =>
            comparer.IndexOf(page.Title, term, options) >= 0 ||
            comparer.IndexOf(page.LocationLabel, term, options) >= 0).ToArray();
    }

    private void PageSelected(object? sender, SelectionChangedEventArgs e)
    {
        ConfirmAgendaPageLinkButton.IsEnabled = AgendaPageLinkList.SelectedItem is PageSearchResult;
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (AgendaPageLinkList.SelectedItem is PageSearchResult page) Close(page.PageId);
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}
