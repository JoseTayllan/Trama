using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class AgendaTrashDialog : Window
{
    public AgendaTrashDialog()
    {
        InitializeComponent();
        Opened += async (_, _) => { if (DataContext is AgendaTrashViewModel vm) await vm.LoadAsync(); };
        Closing += (_, e) => { if (DataContext is AgendaTrashViewModel { IsBusy: true }) e.Cancel = true; };
    }
    public AgendaTrashDialog(AgendaTrashViewModel viewModel) : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel); DataContext = viewModel;
    }
    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AgendaTrashViewModel { IsBusy: true }) Close();
    }
}
