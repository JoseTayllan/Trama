using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trama.Desktop.Views;

public sealed partial class RenameEntryDialog : Window
{
    public RenameEntryDialog() => InitializeComponent();

    public RenameEntryDialog(string heading, string title) : this()
    {
        Heading.Text = heading;
        Title = heading;
        EntryTitleInput.Text = title;
        Opened += (_, _) => { EntryTitleInput.Focus(); EntryTitleInput.SelectAll(); };
    }

    private void TitleChanged(object? sender, TextChangedEventArgs e)
    {
        if (ConfirmRenameButton is null || ValidationMessage is null) return;
        var valid = !string.IsNullOrWhiteSpace(EntryTitleInput.Text);
        ConfirmRenameButton.IsEnabled = valid;
        ValidationMessage.Text = valid ? string.Empty : "Informe um nome para continuar.";
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close((string?)null);

    private void ConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(EntryTitleInput.Text)) Close(EntryTitleInput.Text.Trim());
    }
}
