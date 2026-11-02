using CommunityToolkit.Mvvm.ComponentModel;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed class MarkdownExportViewModel(IMarkdownPageExporter exporter, NotebookSnapshot page,
    Func<Guid, CancellationToken, Task<Stream?>> openImage) : ObservableObject
{
    private string _destination = string.Empty;
    private string _feedback = "Crie uma pasta nova para a página e suas imagens.";
    private bool _isBusy;
    public string Destination { get => _destination; set => SetProperty(ref _destination, value); }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(CanExport)); } }
    public bool CanExport => !IsBusy;
    public void ReportSelectionFailure() => Feedback = "Não foi possível escolher a pasta. Você pode informar o caminho completo no campo de destino.";

    public async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (IsBusy) return;
        IsBusy = true;
        Feedback = "Exportando página…";
        try
        {
            await exporter.ExportAsync(page, Destination.Trim(), openImage, cancellationToken);
            Feedback = "Exportação concluída. A página e suas imagens estão na pasta escolhida.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { Feedback = "Exportação cancelada. Seu conteúdo foi mantido."; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Feedback = "Não foi possível exportar. Use uma pasta nova com caminho completo e confira as imagens e a permissão de escrita."; }
        finally { IsBusy = false; }
    }
}
