using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class AgendaWorkspaceView : UserControl
{
    private bool _changingDay;
    public AgendaWorkspaceView() => InitializeComponent();
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
