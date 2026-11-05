using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog() => InitializeComponent();
    public UnsavedChangesDialog(string message) : this() => ChangesMessage.Text = message;

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close((UnsavedChangesChoice?)UnsavedChangesChoice.Cancel);
    private void DiscardClicked(object? sender, RoutedEventArgs e) => Close((UnsavedChangesChoice?)UnsavedChangesChoice.Discard);
    private void SaveClicked(object? sender, RoutedEventArgs e) => Close((UnsavedChangesChoice?)UnsavedChangesChoice.Save);
}

public sealed class WindowUnsavedChangesPrompt(Window owner, string? message = null) : IUnsavedChangesPrompt
{
    public async Task<UnsavedChangesChoice> ChooseAsync() => !owner.IsVisible
        ? UnsavedChangesChoice.Cancel
        : await (message is null ? new UnsavedChangesDialog() : new UnsavedChangesDialog(message))
            .ShowDialog<UnsavedChangesChoice?>(owner) ?? UnsavedChangesChoice.Cancel;
}
