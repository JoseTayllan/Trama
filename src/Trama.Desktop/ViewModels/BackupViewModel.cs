using CommunityToolkit.Mvvm.ComponentModel;
using Trama.Application.Storage;

namespace Trama.Desktop.ViewModels;

public sealed class BackupViewModel : ObservableObject
{
    private readonly ILocalBackup _backup;
    private bool _isBusy;
    private string _feedback = "O backup inclui somente dados salvos. A restauração cria uma pasta nova e mantém a biblioteca atual.";
    private string? _lastBackupPath;
    private string? _lastRestoredDirectory;

    public BackupViewModel(ILocalBackup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);
        _backup = backup;
    }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }
    public string? LastBackupPath { get => _lastBackupPath; private set => SetProperty(ref _lastBackupPath, value); }
    public string? LastRestoredDirectory { get => _lastRestoredDirectory; private set => SetProperty(ref _lastRestoredDirectory, value); }

    public Task<bool> CreateAsync(string archivePath) => RunAsync(async () =>
    {
        var result = await _backup.CreateAsync(archivePath);
        LastBackupPath = result.ArchivePath;
        Feedback = "Backup criado com os dados salvos e as imagens referenciadas.";
    });

    public Task<bool> RestoreAsync(string archivePath, string destinationDirectory) => RunAsync(async () =>
    {
        var result = await _backup.RestoreAsync(archivePath, destinationDirectory);
        LastRestoredDirectory = result.DataDirectory;
        Feedback = "Cópia restaurada em uma pasta nova. A biblioteca atual continua aberta.";
    });

    private async Task<bool> RunAsync(Func<Task> operation)
    {
        if (IsBusy) return false;
        IsBusy = true;
        Feedback = "Processando os arquivos. Aguarde a conclusão para fechar este painel.";
        try
        {
            await operation();
            return true;
        }
        catch (InvalidDataException)
        {
            Feedback = "O pacote ou os dados estão incompletos, inválidos ou incompatíveis. A biblioteca atual foi mantida.";
        }
        catch (OperationCanceledException)
        {
            Feedback = "Operação cancelada. A biblioteca atual foi mantida.";
        }
        catch (Exception)
        {
            Feedback = "Não foi possível concluir. Confira os caminhos, permissões e espaço disponível. A biblioteca atual foi mantida.";
        }
        finally
        {
            IsBusy = false;
        }
        return false;
    }

    public void ReportSelectionFailure() => Feedback = "Não foi possível selecionar o arquivo ou a pasta. A biblioteca atual foi mantida.";
}
