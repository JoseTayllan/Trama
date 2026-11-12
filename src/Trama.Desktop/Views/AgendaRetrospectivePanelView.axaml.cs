using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class AgendaRetrospectivePanelView : UserControl
{
    public AgendaRetrospectivePanelView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateLayoutColumns();
    }

    private void UpdateLayoutColumns()
    {
        var compact = Bounds.Width < 1100;
        AgendaRetrospectiveIndicators.Columns = Bounds.Width < 1000 ? 2 : 4;
        RetrospectiveDetailsGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "1.65*,*");
        RetrospectiveDetailsGrid.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto" : "Auto");
        Grid.SetColumn(AgendaRetrospectiveCategories, compact ? 0 : 1);
        Grid.SetRow(AgendaRetrospectiveCategories, compact ? 1 : 0);
        RetrospectiveHeader.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "*,Auto");
        RetrospectiveHeader.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto" : "Auto");
        Grid.SetColumn(RetrospectiveHeaderActions, compact ? 0 : 1);
        Grid.SetRow(RetrospectiveHeaderActions, compact ? 1 : 0);
        RetrospectiveBottomGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "1.35*,*");
        RetrospectiveBottomGrid.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto" : "Auto");
        Grid.SetColumn(AgendaRetrospectiveReflection, compact ? 0 : 1);
        Grid.SetRow(AgendaRetrospectiveReflection, compact ? 1 : 0);
    }

    private async void PreviousClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanPreviousRetrospective: true, Retrospective.Report: { } report } vm)
            await vm.ShowRetrospectiveAsync(report.StartDate.AddDays(-7));
    }
    private async void NextClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanNextRetrospective: true, Retrospective.Report: { } report } vm)
            await vm.ShowRetrospectiveAsync(report.StartDate.AddDays(7));
    }
    private async void OpenTaskClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && sender is Button { DataContext: AgendaRetrospectiveTask entry })
            await vm.OpenRetrospectiveTaskAsync(entry.Task.Id);
    }

    private async void SaveReflectionClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm) await vm.SaveReflectionAsync();
    }

    private async void RefreshClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanEdit: true } vm) await vm.RefreshRetrospectiveAsync();
    }
}
