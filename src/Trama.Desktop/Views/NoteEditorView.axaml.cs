using Avalonia.Controls;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class NoteEditorView : UserControl
{
    public NoteEditorView() => InitializeComponent();

    public NoteEditorView(NoteEditorViewModel viewModel) : this() => DataContext = viewModel;
}
