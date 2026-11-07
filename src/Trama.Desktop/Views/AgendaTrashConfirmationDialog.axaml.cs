using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Agenda;

namespace Trama.Desktop.Views;

public sealed partial class AgendaTrashConfirmationDialog : Window
{
    public AgendaTrashConfirmationDialog() => InitializeComponent();
    public AgendaTrashConfirmationDialog(AgendaTaskSnapshot task) : this()
    {
        ArgumentNullException.ThrowIfNull(task); DataContext = task;
    }
    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void ConfirmClicked(object? sender, RoutedEventArgs e) => Close(true);
}
