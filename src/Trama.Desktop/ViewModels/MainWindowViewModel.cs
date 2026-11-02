using CommunityToolkit.Mvvm.ComponentModel;

namespace Trama.Desktop.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private NoteEditorViewModel? _editor;
    private NotebookEditorViewModel? _notebook;
    private LibraryViewModel? _library;
    private BackupViewModel? _backup;
    private string _dataDirectory = string.Empty;
    private string _status = "Preparando o armazenamento local…";

    public string Heading => "Biblioteca pessoal";

    public string Description => "Um espaço para pensamentos, estudos e trechos de código. Organize depois, escreva agora.";

    public NoteEditorViewModel? Editor { get => _editor; set => SetProperty(ref _editor, value); }
    public NotebookEditorViewModel? Notebook
    {
        get => _notebook;
        set { if (SetProperty(ref _notebook, value)) OnPropertyChanged(nameof(Workspace)); }
    }
    public LibraryViewModel? Library
    {
        get => _library;
        set
        {
            if (SetProperty(ref _library, value))
            {
                OnPropertyChanged(nameof(Workspace));
                OnPropertyChanged(nameof(HasLibrary));
            }
        }
    }
    public bool HasLibrary => Library is not null;
    public object? Workspace => (object?)Library ?? Notebook;
    public bool HasBackup => Backup is not null;
    public BackupViewModel? Backup
    {
        get => _backup;
        set { if (SetProperty(ref _backup, value)) OnPropertyChanged(nameof(HasBackup)); }
    }
    public string DataDirectory { get => _dataDirectory; set => SetProperty(ref _dataDirectory, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
}
