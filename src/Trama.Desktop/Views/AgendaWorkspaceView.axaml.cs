using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class AgendaWorkspaceView : UserControl
{
    private bool _changingDay;
    private bool _trashDialogOpen;
    private bool _changingWeekFilters;
    private AgendaViewModel? _observedAgenda;
    private int _layoutMode = -1;
    public AgendaWorkspaceView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        AgendaBodyScroll.SizeChanged += (_, _) => UpdateResponsiveLayout();
        DataContextChanged += (_, _) => ObserveAgenda();
        AttachedToVisualTree += (_, _) => ObserveAgenda();
        DetachedFromVisualTree += (_, _) =>
        {
            if (_observedAgenda is not null) _observedAgenda.PropertyChanged -= AgendaChanged;
            _observedAgenda = null;
        };
    }
    private void ObserveAgenda()
    {
        if (_observedAgenda is not null) _observedAgenda.PropertyChanged -= AgendaChanged;
        _observedAgenda = DataContext as AgendaViewModel;
        if (_observedAgenda is not null) _observedAgenda.PropertyChanged += AgendaChanged;
        UpdateResponsiveLayout();
    }
    private void AgendaChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AgendaViewModel.IsWeekView)) UpdateResponsiveLayout();
    }
    private void UpdateResponsiveLayout()
    {
        if (AgendaBody is null) return;
        var weekly = DataContext is AgendaViewModel { IsWeekView: true };
        var mode = !weekly ? 0 : Bounds.Width < 1320 ? 2 : 1;
        AgendaFormPanel.MaxHeight = weekly ? mode == 2 ? Math.Max(320, AgendaBodyScroll.Bounds.Height) : 630 : double.PositiveInfinity;
        if (_layoutMode == mode) return;
        _layoutMode = mode;
        AgendaBody.ColumnDefinitions = new ColumnDefinitions(mode == 2 ? "*" : mode == 1 ? "*,380" : "*,*");
        AgendaBody.RowDefinitions = new RowDefinitions(mode == 2 ? "Auto,Auto" : mode == 1 ? "Auto" : "*");
        Grid.SetColumn(AgendaFormPanel, mode == 2 ? 0 : 1); Grid.SetRow(AgendaFormPanel, mode == 2 ? 1 : 0);
        AgendaBodyScroll.VerticalScrollBarVisibility = weekly ? Avalonia.Controls.Primitives.ScrollBarVisibility.Auto : Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
    }
    private void WeekDayLoaded(object? sender, RoutedEventArgs e) => WeekDayReady(sender, e);
    private void WeekDayReady(object? sender, EventArgs e)
    {
        if (sender is Border { DataContext: AgendaWeekDay day } border && DataContext is AgendaViewModel vm)
            border.Classes.Set("today", day.Date == vm.Today);
    }
    private async void WeekModeClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm) await vm.ShowWeekAsync(vm.Week?.StartDate ?? vm.SelectedDay);
    }
    private async void DayModeClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm) await vm.ShowDayAsync();
    }
    private async void PreviousWeekClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanPreviousWeek: true, Week: { } week } vm) await vm.ShowWeekAsync(week.StartDate.AddDays(-7));
    }
    private async void NextWeekClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanNextWeek: true, Week: { } week } vm) await vm.ShowWeekAsync(week.StartDate.AddDays(7));
    }
    private async void TodayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm) await vm.ShowWeekAsync(vm.Today);
    }
    private async void WeekFiltersChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_changingWeekFilters || AgendaWeekCategoryFilter is null || AgendaWeekStatusFilter is null ||
            DataContext is not AgendaViewModel { IsWeekView: true, CanEdit: true } vm) return;
        _changingWeekFilters = true;
        try
        {
            await vm.SetWeekFiltersAsync(AgendaWeekCategoryFilter.SelectedIndex > 0 ? (AgendaTaskCategory)(AgendaWeekCategoryFilter.SelectedIndex - 1) : null,
                AgendaWeekStatusFilter.SelectedIndex > 0 ? (AgendaTaskStatus)(AgendaWeekStatusFilter.SelectedIndex - 1) : null);
        }
        finally
        {
            AgendaWeekCategoryFilter.SetCurrentValue(ComboBox.SelectedIndexProperty, vm.WeekCategoryIndex);
            AgendaWeekStatusFilter.SetCurrentValue(ComboBox.SelectedIndexProperty, vm.WeekStatusIndex);
            _changingWeekFilters = false;
        }
    }
    private async void WeekTaskClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && sender is Control { DataContext: AgendaWeekEntry entry } && await vm.EditTaskAsync(entry.Task.Id))
            FocusSharedEditor();
    }
    private async void WeekCreateClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && sender is Control { DataContext: AgendaWeekDay day } && await vm.CreateTaskOnDayAsync(day.Date))
            FocusSharedEditor();
    }
    private void FocusSharedEditor()
    {
        AgendaFormPanel.BringIntoView(); AgendaTitleInput.Focus();
    }
    private async void DeleteTaskClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AgendaViewModel { CanDeleteTask: true, EditingTaskId: { } id } vm || TopLevel.GetTopLevel(this) is not Window owner) return;
        await vm.MoveToTrashAsync(id, async task => owner.IsVisible &&
            await new AgendaTrashConfirmationDialog(task).ShowDialog<bool?>(owner) == true);
    }
    private async void OpenTrashClicked(object? sender, RoutedEventArgs e)
    {
        if (_trashDialogOpen || DataContext is not AgendaViewModel { HasTrash: true, CanEdit: true } vm || TopLevel.GetTopLevel(this) is not Window { IsVisible: true } owner) return;
        _trashDialogOpen = true;
        try { await new AgendaTrashDialog(new AgendaTrashViewModel(vm)).ShowDialog(owner); }
        finally { _trashDialogOpen = false; }
    }
    private async void DayChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        if (_changingDay || DataContext is not AgendaViewModel vm || AgendaDayPicker.SelectedDate is not { } date) return;
        var day = DateOnly.FromDateTime(date.DateTime);
        if (day == vm.SelectedDay) return;
        _changingDay = true;
        try { await vm.ChangeDayAsync(day); }
        finally { AgendaDayPicker.SetCurrentValue(DatePicker.SelectedDateProperty, vm.SelectedDayDate); _changingDay = false; }
    }
    private async void PreviousDayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && vm.SelectedDay > DateOnly.MinValue) await vm.ChangeDayAsync(vm.SelectedDay.AddDays(-1));
    }
    private async void NextDayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && vm.SelectedDay < DateOnly.MaxValue) await vm.ChangeDayAsync(vm.SelectedDay.AddDays(1));
    }
    private async void TaskClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && sender is Control { DataContext: AgendaTaskSnapshot task }) await vm.EditTaskAsync(task.Id);
    }
    private void ClearTimeClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanEdit: true } vm) { vm.StartTimeText = string.Empty; vm.EndTimeText = string.Empty; }
    }
    private void ClearEndDateClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel { CanEdit: true } vm) vm.PlannedEndDate = null;
    }
}
