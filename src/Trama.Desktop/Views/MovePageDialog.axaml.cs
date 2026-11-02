using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class MovePageDialog : Window
{
    public MovePageDialog() => InitializeComponent();

    public MovePageDialog(IReadOnlyList<NotebookDestination> destinations) : this()
    {
        DestinationList.ItemsSource = destinations;
        if (destinations.Count == 0) DestinationHint.Text = "Nenhum outro caderno disponível. Crie um caderno na biblioteca e tente novamente.";
    }

    private void DestinationChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ConfirmMoveButton is not null) ConfirmMoveButton.IsEnabled = DestinationList.SelectedItem is NotebookDestination;
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close((Guid?)null);

    private void ConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (DestinationList.SelectedItem is NotebookDestination destination) Close((Guid?)destination.Notebook.Id);
    }
}
