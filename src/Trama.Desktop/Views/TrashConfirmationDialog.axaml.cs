using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Knowledge;

namespace Trama.Desktop.Views;

public sealed partial class TrashConfirmationDialog : Window
{
    public TrashConfirmationDialog() => InitializeComponent();
    public TrashConfirmationDialog(LibraryTrashPreview preview) : this()
    {
        TrashHeading.Text = $"Enviar “{preview.Title}” para a lixeira?";
        TrashImpact.Text = $"Itens afetados: {preview.Spaces} espaço(s), {preview.Notebooks} caderno(s) e {preview.Pages} página(s).";
    }
    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void ConfirmClicked(object? sender, RoutedEventArgs e) => Close(true);
}
