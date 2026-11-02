using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed partial class NotebookEditorViewModel
{
    private readonly IMarkdownPageExporter? _markdownExporter;

    public NotebookEditorViewModel(INotebookActions actions, IMarkdownPageExporter markdownExporter, TimeProvider? autoSaveClock = null) : this(actions)
    {
        ArgumentNullException.ThrowIfNull(markdownExporter);
        _markdownExporter = markdownExporter;
        _autoSaveClock = autoSaveClock;
        if (autoSaveClock is not null) Feedback = "Salvamento automático após 2 segundos de pausa. Você também pode usar Salvar página.";
    }

    public bool SupportsMarkdownExport => _markdownExporter is not null;

    public MarkdownExportViewModel CreateMarkdownExport()
    {
        if (_markdownExporter is null) throw new InvalidOperationException("Markdown export is not configured.");
        var snapshot = new NotebookSnapshot(PageId, Title, _blocks.Select(ToSnapshot).ToArray());
        return new MarkdownExportViewModel(_markdownExporter, snapshot, OpenImageAsync);
    }
}
