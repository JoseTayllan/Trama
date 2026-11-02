using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog() => InitializeComponent();

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close((UnsavedChangesChoice?)UnsavedChangesChoice.Cancel);
    private void DiscardClicked(object? sender, RoutedEventArgs e) => Close((UnsavedChangesChoice?)UnsavedChangesChoice.Discard);
    private void SaveClicked(object? sender, RoutedEventArgs e) => Close((UnsavedChangesChoice?)UnsavedChangesChoice.Save);
}

public sealed class WindowUnsavedChangesPrompt(Window owner) : IUnsavedChangesPrompt
{
    public async Task<UnsavedChangesChoice> ChooseAsync() => !owner.IsVisible
        ? UnsavedChangesChoice.Cancel
        : await new UnsavedChangesDialog().ShowDialog<UnsavedChangesChoice?>(owner) ?? UnsavedChangesChoice.Cancel;
}
