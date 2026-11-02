using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trama.Desktop.Views;

public sealed partial class PageTagsDialog : Window
{
    public PageTagsDialog() => InitializeComponent();

    public PageTagsDialog(IReadOnlyList<string> tags) : this()
    {
        PageTagsInput.Text = string.Join(Environment.NewLine, tags);
        Opened += (_, _) => PageTagsInput.Focus();
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close((IReadOnlyList<string>?)null);

    private void SaveClicked(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<string> tags = (PageTagsInput.Text ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Close(tags);
    }
}
