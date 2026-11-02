using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NotebookEditorView
{
    private bool _exportingMarkdown;
    private async void ExportMarkdownClicked(object? sender, RoutedEventArgs e)
    {
        if (_exportingMarkdown || DataContext is not NotebookEditorViewModel { IsBusy: false, SupportsMarkdownExport: true } editor ||
            TopLevel.GetTopLevel(this) is not Window owner) return;
        _exportingMarkdown = true;
        using var pause = editor.PauseAutoSave();
        try
        {
            var dialog = new MarkdownExportDialog(editor.CreateMarkdownExport())
            {
                Width = Math.Min(620, Math.Max(320, owner.Bounds.Width - 32)),
                Height = Math.Min(420, Math.Max(340, owner.Bounds.Height - 32))
            };
            await dialog.ShowDialog(owner);
        }
        finally { _exportingMarkdown = false; }
    }
}
