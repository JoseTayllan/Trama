using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class TrashDialog : Window
{
    public TrashDialog()
    {
        InitializeComponent();
        Closing += (_, e) => { if (DataContext is TrashViewModel { IsBusy: true }) e.Cancel = true; };
    }
    public TrashDialog(TrashViewModel viewModel) : this() => DataContext = viewModel;
    private void CloseClicked(object? sender, RoutedEventArgs e) => Close();
}
